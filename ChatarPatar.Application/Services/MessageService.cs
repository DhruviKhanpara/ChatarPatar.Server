using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Application.DTOs.Message.Pin;
using ChatarPatar.Application.DTOs.Message.Reaction;
using ChatarPatar.Application.DTOs.ReadState;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Application.ServiceContracts.SignalR;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.AppLogging.Model.LogRequest;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Common.SignalR.Model;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Helpers;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Application.Services;

internal class MessageService : IMessageService
{
    private readonly IRepositoryManager _repositories;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MessageService> _logger;
    private readonly IMapper _mapper;
    private readonly IOutboxBackgroundQueue _queue;
    private readonly ISignalRService _signalR;

    public MessageService(IRepositoryManager repositories, IValidationService validationService, IHttpContextAccessor httpContextAccessor, ILogger<MessageService> logger, IMapper mapper, IOutboxBackgroundQueue queue, ISignalRService signalR)
    {
        _repositories = repositories;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _mapper = mapper;
        _queue = queue;
        _signalR = signalR;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    #region Channel Message

    public async Task<CursorPagedResult<MessageDto>> GetChannelMessagesAsync(Guid orgId, Guid teamId, Guid channelId, MessageQueryParams queryParams)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var callerContext = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .AsNoTracking()
            .Select(t => new
            {
                OrgRole = t.Organization.OrganizationMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (OrganizationRoleEnum?)m.Role)
                    .FirstOrDefault(),
                TeamRole = t.TeamMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (TeamRoleEnum?)m.Role)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (callerContext == null || callerContext.OrgRole == null)
            throw new NotFoundAppException("Channel");

        var callerIsAdmin =
            callerContext.OrgRole is OrganizationRoleEnum.OrgOwner or OrganizationRoleEnum.OrgAdmin
            || callerContext.TeamRole is TeamRoleEnum.TeamAdmin;

        var channelQuery = _repositories.ChannelRepository
            .GetByIdInTeam(channelId, teamId, orgId)
            .AsNoTracking();

        if (!callerIsAdmin)
            channelQuery = channelQuery.Where(c =>
                !c.IsPrivate ||
                c.ChannelMembers.Any(m => m.UserId == authUserId && !m.IsDeleted));

        var channelExists = await channelQuery.AnyAsync();

        if (!channelExists)
            throw new NotFoundAppException("Channel");

        var pageSize = Math.Clamp(queryParams.PageSize, 1, 100);

        var messages = await _repositories.MessageRepository
            .GetChannelMessagesQuery(channelId, queryParams.BeforeSequence, queryParams.ThreadRootMessageId)
            .AsNoTracking()
            .Take(pageSize + 1)
            .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        // Stamp ReactedByMe for the calling user across all returned messages
        await StampReactionsAsync(messages, authUserId);

        var hasMore = messages.Count > pageSize;

        if (hasMore)
            messages.RemoveAt(pageSize);

        long? nextCursor = null;

        if (hasMore && messages.Count > 0)
            nextCursor = messages.Last().SequenceNumber;

        return new CursorPagedResult<MessageDto>(messages, hasMore, nextCursor);
    }

    public async Task<MessageDto> SendChannelMessageAsync(Guid orgId, Guid teamId, Guid channelId, SendMessageDto dto)
    {
        await _validationService.ValidateAsync(dto);

        var senderId = Guid.Parse(_httpContext.GetUserId());

        // ── Step 1: Announcement channel guard ────────────────────────────
        // Only Moderator+ may post to Announcement channels. Channel type is only known here.
        var channel = await _repositories.ChannelRepository
                .FindByCondition(c => c.Id == channelId)
                .Select(x => new { x.Id, x.Type, x.IsArchived, IsTeamArchive = x.Team.IsArchived })
                .FirstOrDefaultAsync()
                ?? throw new NotFoundAppException("Channel");

        if (channel.IsTeamArchive)
            throw new InvalidDataAppException("Cannot send message to an archived team channel.");

        if (channel.IsArchived)
            throw new InvalidDataAppException("Cannot send message from an archived channel.");

        if (channel.Type == ChannelTypeEnum.Announcement)
        {
            var isElevated =
                await _repositories.ChannelMemberRepository
                    .AnyAsync(m =>
                        m.ChannelId == channelId &&
                        m.UserId == senderId &&
                        m.Role == ChannelRoleEnum.ChannelModerator)
                || await _repositories.TeamMemberRepository
                    .AnyAsync(m =>
                        m.TeamId == teamId &&
                        m.UserId == senderId &&
                        m.Role == TeamRoleEnum.TeamAdmin)
                || await _repositories.OrganizationMemberRepository
                    .AnyAsync(m =>
                        m.OrgId == orgId &&
                        m.UserId == senderId &&
                        (m.Role == OrganizationRoleEnum.OrgOwner ||
                         m.Role == OrganizationRoleEnum.OrgAdmin));

            if (!isElevated)
                throw new ForbiddenAppException("Only channel moderators can post in announcement channels.");
        }

        // ── Step 2: Idempotency ────────────────────────────────────────────
        var existing = await TryGetExistingMessageAsync(senderId, dto.ClientMessageId, channelId);

        if (existing is not null)
            return existing;

        // ── Step 3: Thread root validation ────────────────────────────────
        await ValidateThreadRootAsync(dto.ThreadRootMessageId, channelId);

        // ── Step 4: Attachment validation ─────────────────────────────────
        List<FileEntity> attachedFiles = await ValidateAttachmentsAsync(dto.FileIds, senderId);

        // ── Step 5: Derive MessageType ─────────────────────────────────────
        var messageType = DeriveMessageType(dto.Content, attachedFiles.Select(x => x.FileType));

        // ── Step 6: DB transaction ─────────────────────────────────────────
        var message = CreateMessage(dto, senderId, messageType, channelId);
        var authUserName = _httpContext.GetUserName()
            ?? _httpContext.GetUserEmail()
            ?? _httpContext.GetUserId()
            ?? "System";

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            // 6a — Message row
            await _repositories.MessageRepository.AddAsync(message);
            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();

            // 6b — Flip pending files → attached, set scope, clear ExpiresAt
            await AttachFilesAsync(message, dto.FileIds, attachedFiles, channelId);

            // 6c — Mentions
            await CreateMentionsAsync(message, dto.MentionedUserIds, channelId);

            // 6d — Thread root counters
            await UpdateThreadCountersAsync(dto.ThreadRootMessageId);

            // 6e — Post-save side effects
            // Channel: write Outbox row then signal the background worker.
            // Worker reads it and fans out ReadState updates for all members,
            // and creates Mention / ThreadReply notifications.

            // Resolve thread root sender if this is a reply — needed by the outbox
            // handler to create a ThreadReply notification without an extra DB query there.
            Guid? threadRootSenderId = null;
            if (dto.ThreadRootMessageId.HasValue)
            {
                threadRootSenderId = await _repositories.MessageRepository
                    .FindByCondition(m => m.Id == dto.ThreadRootMessageId.Value)
                    .Select(m => (Guid?)m.SenderId)
                    .FirstOrDefaultAsync();
            }

            var contentPreview = BuildContentSnapshot(dto.Content, attachedFiles.FirstOrDefault());

            var outboxMessage = OutboxMessageFactory.BuildChannelSendMessage(dto.MentionedUserIds, channelId, message.Id, message.SequenceNumber, senderId, authUserName, dto.ThreadRootMessageId, threadRootSenderId, contentPreview);

            await _repositories.OutboxMessageRepository.AddAsync(outboxMessage);

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();

            _queue.Enqueue();
        }
        catch (DbUpdateException ex) when (ex.IsMessageSendUniqueViolation())
        {
            var existMessage = await TryGetExistingMessageAsync(senderId, dto.ClientMessageId, channelId);

            if (existMessage is not null)
                return existMessage;

            throw;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        var messageDto = await GetMessageDto(message.Id);

        try { await _signalR.BroadcastChannelMessageAsync(channelId, messageDto); }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastChannelMessage failed. MessageId={Id}", message.Id); }

        // Push thread counter update to root message viewers
        if (dto.ThreadRootMessageId.HasValue)
        {
            var root = await _repositories.MessageRepository
                .FindByCondition(m => m.Id == dto.ThreadRootMessageId.Value)
                .Select(m => new { m.ReplyCount, m.LastReplyAt })
                .FirstOrDefaultAsync();

            if (root?.LastReplyAt is not null)
            {
                try { await _signalR.BroadcastChannelThreadUpdateAsync(channelId, new ThreadUpdatePush(dto.ThreadRootMessageId.Value, root.ReplyCount, root.LastReplyAt.Value)); }
                catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastChannelThreadUpdate failed."); }
            }
        }

        return messageDto;
    }

    public async Task<MessageDto> EditChannelMessageAsync(Guid orgId, Guid teamId, Guid channelId, Guid messageId, EditMessageDto dto)
    {
        await _validationService.ValidateAsync(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var message = await _repositories.MessageRepository
            .GetByIdInChannel(messageId, channelId)
            .Select(m => new
            {
                Entity = m,
                m.SenderId,
                ChannelIsArchived = m.Channel!.IsArchived,
                TeamIsArchived = m.Channel.Team.IsArchived,
                CurrentFiles = m.MessageAttachments.Select(a => new { a.FileId, a.File.FileType }).ToList(),
                CurrentMentions = m.MessageMentions.Select(mn => new { mn.Id, mn.MentionedUserId }).ToList(),
            })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundAppException("Message");

        if (message.TeamIsArchived)
            throw new InvalidDataAppException("Cannot edit a message in an archived team channel.");

        if (message.ChannelIsArchived)
            throw new InvalidDataAppException("Cannot edit a message in an archived channel.");

        if (message.SenderId != authUserId)
            throw new ForbiddenAppException("You can only edit your own messages.");

        var currentFileIdSet = message.CurrentFiles.Select(a => a.FileId).ToHashSet();
        var desiredFileIdSet = dto.FileIds.ToHashSet();

        var fileIdsToRemove = currentFileIdSet.Except(desiredFileIdSet).ToList();
        var fileIdsToAdd = desiredFileIdSet.Except(currentFileIdSet).ToList();

        List<FileEntity> newFiles = fileIdsToAdd.Count > 0
            ? await ValidateAttachmentsAsync(fileIdsToAdd, authUserId)
            : [];

        var finalFileTypes = message.CurrentFiles
            .Where(f => desiredFileIdSet.Contains(f.FileId))
            .Select(f => f.FileType)
            .Concat(newFiles.Select(f => f.FileType));

        var messageType = DeriveMessageType(dto.Content, finalFileTypes);

        var currentMentionUserIds = message.CurrentMentions.Select(mn => mn.MentionedUserId).ToHashSet();
        var desiredMentionUserIds = dto.MentionedUserIds.ToHashSet();

        var mentionIdsToRemove = message.CurrentMentions
            .Where(mn => !desiredMentionUserIds.Contains(mn.MentionedUserId))
            .Select(mn => mn.Id)
            .ToList();

        var mentionUserIdsToAdd = desiredMentionUserIds.Except(currentMentionUserIds).ToList();

        List<(string PublicId, FileTypeEnum FileType)> filesToCleanup = [];
        var authUserName = _httpContext.GetUserName()
            ?? _httpContext.GetUserEmail()
            ?? _httpContext.GetUserId()
            ?? "System";

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            if (fileIdsToRemove.Count > 0)
                filesToCleanup = await RemoveAttachmentsAsync(messageId, fileIdsToRemove, authUserId);

            await AttachFilesAsync(message.Entity, dto.FileIds, newFiles, channelId);

            if (mentionIdsToRemove.Count > 0)
                await RemoveMentionsAsync(mentionIdsToRemove);

            if (mentionUserIdsToAdd.Count > 0)
                await CreateMentionsAsync(message.Entity, mentionUserIdsToAdd, channelId);

            var entity = message.Entity;
            entity.Content = dto.Content?.Trim();
            entity.MessageType = messageType;
            entity.IsEdited = true;
            entity.EditedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // ── Enqueue Cloudinary deletes inside the transaction ──────────────
            // If the transaction rolls back these outbox rows roll back too,
            // so we never delete assets for files that are still attached.

            var outboxMessages = filesToCleanup
                .Select(file => OutboxMessageFactory.BuildCloudinaryDeleteMessage(file.PublicId, file.FileType, authUserId, authUserName))
                .ToList();

            await _repositories.OutboxMessageRepository.AddRangeAsync(outboxMessages);

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();

            // Signal background worker if there's anything to process
            if (filesToCleanup.Count > 0)
                _queue.Enqueue();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        var messageDto = await GetMessageDto(messageId);
        
        try { await _signalR.BroadcastChannelMessageEditedAsync(channelId, messageDto); }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastChannelMessageEdited failed."); }
        
        return messageDto;
    }

    /// <summary>
    /// Toggles an emoji reaction on a channel message.
    /// If the calling user has already reacted with this emoji → removes it.
    /// If they have not yet reacted → adds it.
    /// </summary>
    public async Task<MessageReactionToggleResultDto> ToggleChannelMessageReactionAsync(Guid orgId, Guid teamId, Guid channelId, Guid messageId, MessageReactionToggleDto dto)
    {
        await _validationService.ValidateAsync(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var messageExists = await _repositories.MessageRepository
            .GetByIdInChannel(messageId, channelId)
            .AnyAsync();

        if (!messageExists)
            throw new NotFoundAppException("Message");

        var result = await ToggleReactionAsync(messageId, channelId, null, authUserId, dto.Emoji);

        try { await _signalR.BroadcastChannelReactionAsync(channelId, messageId, result); }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastChannelReaction failed."); }

        return result;
    }

    public async Task<PinnedMessageResponseDto> PinChannelMessageAsync(Guid channelId, Guid messageId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var message = await _repositories.MessageRepository
            .GetByIdInChannel(messageId, channelId)
            .Select(x => new
            {
                x.Content,
                FirstAttachment = x.MessageAttachments
                    .Select(ma => ma.File)
                    .FirstOrDefault(),
                ChannelIsArchived = x.Channel.IsArchived,
                TeamIsArchived = x.Channel.Team.IsArchived
            })
            .FirstOrDefaultAsync();

        if (message == null)
            throw new NotFoundAppException("Message");

        if (message.TeamIsArchived)
            throw new InvalidDataAppException("Cannot pin in an archived team channel.");

        if (message.ChannelIsArchived)
            throw new InvalidDataAppException("Cannot pin in an archived channel.");

        var existPin = await _repositories.PinnedMessageRepository
            .ActivePinInChannel(messageId, channelId)
            .AsNoTracking()
            .SingleOrDefaultAsync();

        if (existPin is not null)
            return _mapper.Map<PinnedMessageResponseDto>(existPin);

        var contentSnapshot = BuildContentSnapshot(message.Content, message.FirstAttachment);

        var pin = new PinnedMessage
        {
            MessageId = messageId,
            ChannelId = channelId,
            PinnedByUserId = authUserId,
            ContentSnapshot = contentSnapshot
        };

        try
        {
            await _repositories.PinnedMessageRepository.AddAsync(pin);
            await _repositories.UnitOfWork.SaveChangesAsync();

            var pinDto = _mapper.Map<PinnedMessageResponseDto>(pin);

            try { await _signalR.BroadcastChannelPinAsync(channelId, pinDto); }
            catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastChannelPin failed."); }

            return pinDto;
        }
        catch (DbUpdateException ex) when (ex.IsPinnedMessagePerChannelUniqueViolation())
        {
            var concurrentPin = await _repositories.PinnedMessageRepository
                .ActivePinInChannel(messageId, channelId)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            if (concurrentPin is null)
                throw;

            return _mapper.Map<PinnedMessageResponseDto>(concurrentPin);
        }
    }

    public async Task<ReadStateDto> MarkChannelMessageReadAsync(Guid orgId, Guid teamId, Guid channelId, Guid messageId)
    => await MarkChannelMessageAsync(orgId, teamId, channelId, messageId, markUnread: false);

    public async Task<ReadStateDto> MarkChannelMessageUnreadAsync(Guid orgId, Guid teamId, Guid channelId, Guid messageId)
        => await MarkChannelMessageAsync(orgId, teamId, channelId, messageId, markUnread: true);

    public async Task DeleteChannelMessageAsync(Guid orgId, Guid teamId, Guid channelId, Guid messageId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var message = await _repositories.MessageRepository
            .GetByIdInChannel(messageId, channelId)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundAppException("Message");

        if (message.IsDeleted)
            throw new NotFoundAppException("Message");

        if (message.SenderId != authUserId)
            throw new ForbiddenAppException("You can only delete your own messages.");

        message.IsDeleted = true;
        message.DeletedBy = authUserId;
        message.DeletedAt = DateTime.UtcNow;
        message.UpdatedAt = DateTime.UtcNow;

        await _repositories.UnitOfWork.SaveChangesAsync();

        try { await _signalR.BroadcastChannelMessageDeletedAsync(channelId, messageId, authUserId); }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastChannelMessageDeleted failed."); }
    }

    public async Task ForceDeleteChannelMessageAsync(Guid orgId, Guid teamId, Guid channelId, Guid messageId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var message = await _repositories.MessageRepository
            .GetByIdInChannel(messageId, channelId)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundAppException("Message");

        if (message.IsDeleted)
            throw new NotFoundAppException("Message");

        if (message.SenderId == authUserId)
            throw new InvalidDataAppException("Use the delete-own endpoint to delete your own messages.");

        message.IsDeleted = true;
        message.DeletedBy = authUserId;
        message.DeletedAt = DateTime.UtcNow;
        message.UpdatedAt = DateTime.UtcNow;

        await _repositories.UnitOfWork.SaveChangesAsync();

        try { await _signalR.BroadcastChannelMessageDeletedAsync(channelId, messageId, authUserId); }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastChannelMessageDeleted failed."); }
    }

    #endregion

    #region Conversation Message

    public async Task<CursorPagedResult<MessageDto>> GetConversationMessagesAsync(Guid conversationId, MessageQueryParams queryParams)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var conversationContext = await _repositories.ConversationRepository
            .GetByIdForUser(conversationId, authUserId)
            .Select(c => new { Role = (ConversationTypeEnum?)c.Type, ParticipantCount = c.ConversationParticipants.Count() })
            .FirstOrDefaultAsync();

        if (conversationContext is null)
            throw new NotFoundAppException("Conversation");

        var pageSize = Math.Clamp(queryParams.PageSize, 1, 100);

        var messages = await _repositories.MessageRepository
            .GetConversationMessagesQuery(conversationId, queryParams.BeforeSequence, queryParams.ThreadRootMessageId)
            .Take(pageSize + 1)
            .AsNoTracking()
            .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        // Stamp ReactedByMe for the calling user across all returned messages
        await StampReactionsAsync(messages, authUserId);

        // Group conversations only — Direct DMs already carry DmDeliveredAt/DmSeenAt
        // straight off the Message row via the projection above.
        if (conversationContext.Role == ConversationTypeEnum.Group && conversationContext.ParticipantCount <= ValidationConstants.Conversation.GroupReceiptThreshold)
            await StampGroupTicksAsync(messages, authUserId);

        var hasMore = messages.Count > pageSize;

        if (hasMore)
            messages.RemoveAt(pageSize);

        long? nextCursor = null;

        if (hasMore && messages.Count > 0)
            nextCursor = messages.Last().SequenceNumber;

        return new CursorPagedResult<MessageDto>(messages, hasMore, nextCursor);
    }

    public async Task<MessageDto> SendConversationMessageAsync(Guid conversationId, SendMessageDto dto)
    {
        await _validationService.ValidateAsync(dto);

        var senderId = Guid.Parse(_httpContext.GetUserId());

        // ── Step 1: Idempotency ────────────────────────────────────────────
        var existing = await TryGetExistingMessageAsync(senderId, dto.ClientMessageId, null, conversationId);

        if (existing is not null)
            return existing;

        // ── Step 2: Thread root validation ────────────────────────────────
        await ValidateThreadRootAsync(dto.ThreadRootMessageId, null, conversationId);

        // ── Step 3: Attachment validation ─────────────────────────────────
        List<FileEntity> attachedFiles = await ValidateAttachmentsAsync(dto.FileIds, senderId);

        // ── Step 4: Derive MessageType ─────────────────────────────────────
        var messageType = DeriveMessageType(dto.Content, attachedFiles.Select(x => x.FileType));

        // ── Step 5: DB transaction ─────────────────────────────────────────
        var conversation = await _repositories.ConversationRepository
            .GetByIdForUser(conversationId, senderId)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundAppException("Conversation");

        var otherParticipantIds = conversation.Type == ConversationTypeEnum.Direct
            ? ResolveDirectDmOtherParticipant(conversation.DirectParticipantAId, conversation.DirectParticipantBId, senderId)
            : await _repositories.ConversationParticipantRepository
                .GetActiveParticipantsQuery(conversationId)
                .Where(p => p.UserId != senderId)
                .Select(p => p.UserId)
                .ToHashSetAsync();

        var activeParticipantIds = await _repositories.UserRepository
            .GetByIds(otherParticipantIds)
            .Select(x => x.Id)
            .ToHashSetAsync();

        if (activeParticipantIds.Count != otherParticipantIds.Count)
        {
            var missingIds = otherParticipantIds
                .Except(activeParticipantIds)
                .ToList();

            _logger.LogWarning("One or more conversation participants could not be found. Missing: {MissingIds}", string.Join(", ", missingIds));
        }

        var message = CreateMessage(dto, senderId, messageType, null, conversationId);

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            // 5a — Message row
            await _repositories.MessageRepository.AddAsync(message);
            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();

            // Update lastMessageAt in conversation
            conversation.LastMessageAt = message.CreatedAt;

            // 5b — Flip pending files → attached, set scope, clear ExpiresAt
            await AttachFilesAsync(message, dto.FileIds, attachedFiles, null, conversationId);

            // 5c — Mentions
            await CreateMentionsAsync(message, dto.MentionedUserIds, null, conversationId);

            // 5d — Thread root counters
            await UpdateThreadCountersAsync(dto.ThreadRootMessageId);

            // 5e — Post-save side effects
            // Conversation: synchronous ReadState update.
            // Participants are a small bounded set so fanout is cheap here.

            var mentionedSet = dto.MentionedUserIds.ToHashSet();
            var now = DateTime.UtcNow;

            // 5f — Delivery state for Group conversations
            if (conversation.Type == ConversationTypeEnum.Group)
            {
                if (activeParticipantIds.Count + 1 <= ValidationConstants.Conversation.GroupReceiptThreshold)
                {
                    // Small group: seed one MessageReceipt row per other active participant.
                    // DeliveredAt seeded now as a placeholder — SignalR will set the real delivery time.
                    var receipts = activeParticipantIds
                        .Select(participantId => new MessageReceipt
                        {
                            MessageId = message.Id,
                            UserId = participantId,
                            SeenAt = null,
                            CreatedAt = now,
                            UpdatedAt = now
                        })
                        .ToList();

                    await _repositories.MessageReceiptRepository.AddRangeAsync(receipts);
                }
            }

            // ReadState rows are guaranteed to exist for all participants.
            // They are provisioned during conversation membership creation.
            foreach (var participantId in activeParticipantIds)
            {
                await _repositories.ReadStateRepository.IncrementUnreadAsync(
                    userId: participantId,
                    conversationId: conversationId,
                    incrementMention: mentionedSet.Contains(participantId));
            }

            // ── Inline notifications for conversation messages ─────────────
            // Mention notifications — one per mentioned user (excluding sender)
            var contentPreviewConv = BuildContentSnapshot(dto.Content, attachedFiles.FirstOrDefault());

            var conversationNotifications = mentionedSet
                .Where(x => x != senderId && activeParticipantIds.Contains(x))
                .Select(x => new NotificationEntity()
                {
                    RecipientId = x,
                    Type = NotificationTypeEnum.Mention,
                    ActorId = senderId,
                    MessageId = message.Id,
                    ConversationId = conversationId,
                    Preview = contentPreviewConv,
                    IsRead = false,
                    CreatedAt = now
                })
                .ToList();

            // ThreadReply notification — notify root sender if different from current sender
            if (dto.ThreadRootMessageId.HasValue)
            {
                var rootSenderId = await _repositories.MessageRepository
                    .FindByCondition(m => m.Id == dto.ThreadRootMessageId.Value)
                    .Select(m => (Guid?)m.SenderId)
                    .FirstOrDefaultAsync();

                if (rootSenderId.HasValue
                    && rootSenderId.Value != senderId
                    && activeParticipantIds.Contains(rootSenderId.Value))
                {
                    conversationNotifications.Add(new NotificationEntity
                    {
                        RecipientId = rootSenderId.Value,
                        Type = NotificationTypeEnum.ThreadReply,
                        ActorId = senderId,
                        MessageId = message.Id,
                        ConversationId = conversationId,
                        Preview = contentPreviewConv,
                        IsRead = false,
                        CreatedAt = now
                    });
                }
            }

            if (conversationNotifications.Any())
                await _repositories.NotificationRepository.AddRangeAsync(conversationNotifications);

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch (DbUpdateException ex) when (ex.IsMessageSendUniqueViolation())
        {
            var existMessage = await TryGetExistingMessageAsync(senderId, dto.ClientMessageId, null, conversationId);

            if (existMessage is not null)
                return existMessage;

            throw;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        var messageDto = await GetMessageDto(message.Id);

        try { await _signalR.BroadcastConversationMessageAsync(conversationId, messageDto); }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastConversationMessage failed. MessageId={Id}", message.Id); }

        if (dto.ThreadRootMessageId.HasValue)
        {
            var root = await _repositories.MessageRepository
                .FindByCondition(m => m.Id == dto.ThreadRootMessageId.Value)
                .Select(m => new { m.ReplyCount, m.LastReplyAt })
                .FirstOrDefaultAsync();

            if (root?.LastReplyAt is not null)
            {
                try { await _signalR.BroadcastConversationThreadUpdateAsync(conversationId, new ThreadUpdatePush(dto.ThreadRootMessageId.Value, root.ReplyCount, root.LastReplyAt.Value)); }
                catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastConversationThreadUpdate failed."); }
            }
        }

        return messageDto;
    }

    public async Task<MessageDto> EditConversationMessageAsync(Guid conversationId, Guid messageId, EditMessageDto dto)
    {
        await _validationService.ValidateAsync(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var message = await _repositories.MessageRepository
            .GetByIdInConversation(messageId, conversationId)
            .Select(m => new
            {
                Entity = m,
                m.SenderId,
                CurrentFiles = m.MessageAttachments.Select(a => new { a.FileId, a.File.FileType }).ToList(),
                CurrentMentions = m.MessageMentions.Select(mn => new { mn.Id, mn.MentionedUserId }).ToList(),
            })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundAppException("Message");

        if (message.SenderId != authUserId)
            throw new ForbiddenAppException("You can only edit your own messages.");

        var currentFileIdSet = message.CurrentFiles.Select(a => a.FileId).ToHashSet();
        var desiredFileIdSet = dto.FileIds.ToHashSet();

        var fileIdsToRemove = currentFileIdSet.Except(desiredFileIdSet).ToList();
        var fileIdsToAdd = desiredFileIdSet.Except(currentFileIdSet).ToList();

        List<FileEntity> newFiles = fileIdsToAdd.Count > 0
            ? await ValidateAttachmentsAsync(fileIdsToAdd, authUserId)
            : [];

        var finalFileTypes = message.CurrentFiles
            .Where(f => desiredFileIdSet.Contains(f.FileId))
            .Select(f => f.FileType)
            .Concat(newFiles.Select(f => f.FileType));

        var messageType = DeriveMessageType(dto.Content, finalFileTypes);

        var currentMentionUserIds = message.CurrentMentions.Select(mn => mn.MentionedUserId).ToHashSet();
        var desiredMentionUserIds = dto.MentionedUserIds.ToHashSet();

        var mentionIdsToRemove = message.CurrentMentions
            .Where(mn => !desiredMentionUserIds.Contains(mn.MentionedUserId))
            .Select(mn => mn.Id)
            .ToList();

        var mentionUserIdsToAdd = desiredMentionUserIds.Except(currentMentionUserIds).ToList();

        List<(string PublicId, FileTypeEnum FileType)> filesToCleanup = [];
        var authUserName = _httpContext.GetUserName()
            ?? _httpContext.GetUserEmail()
            ?? _httpContext.GetUserId()
            ?? "System";

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            if (fileIdsToRemove.Count > 0)
                filesToCleanup = await RemoveAttachmentsAsync(messageId, fileIdsToRemove, authUserId);

            await AttachFilesAsync(message.Entity, dto.FileIds, newFiles, null, conversationId);

            if (mentionIdsToRemove.Count > 0)
                await RemoveMentionsAsync(mentionIdsToRemove);

            if (mentionUserIdsToAdd.Count > 0)
                await CreateMentionsAsync(message.Entity, mentionUserIdsToAdd, null, conversationId);

            var entity = message.Entity;
            entity.Content = dto.Content?.Trim();
            entity.MessageType = messageType;
            entity.IsEdited = true;
            entity.EditedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // ── Enqueue Cloudinary deletes inside the transaction ──────────────
            // If the transaction rolls back these outbox rows roll back too,
            // so we never delete assets for files that are still attached.

            var outboxMessages = filesToCleanup
                .Select(file => OutboxMessageFactory.BuildCloudinaryDeleteMessage(file.PublicId, file.FileType, authUserId, authUserName))
                .ToList();

            await _repositories.OutboxMessageRepository.AddRangeAsync(outboxMessages);

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();

            // Signal background worker if there's anything to process
            if (filesToCleanup.Count > 0)
                _queue.Enqueue();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        var messageDto = await GetMessageDto(messageId);

        try { await _signalR.BroadcastConversationMessageEditedAsync(conversationId, messageDto); }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastConversationMessageEdited failed."); }

        return messageDto;
    }

    /// <summary>
    /// Toggles an emoji reaction on a conversation message.
    /// If the calling user has already reacted with this emoji → removes it.
    /// If they have not yet reacted → adds it.
    /// </summary>
    public async Task<MessageReactionToggleResultDto> ToggleConversationMessageReactionAsync(Guid conversationId, Guid messageId, MessageReactionToggleDto dto)
    {
        await _validationService.ValidateAsync(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var messageExists = await _repositories.MessageRepository
            .GetByIdInConversation(messageId, conversationId)
            .AnyAsync();

        if (!messageExists)
            throw new NotFoundAppException("Message");

        var result = await ToggleReactionAsync(messageId, null, conversationId, authUserId, dto.Emoji);
        
        try { await _signalR.BroadcastConversationReactionAsync(conversationId, messageId, result); }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastConversationReaction failed."); }
        
        return result;
    }

    public async Task<PinnedMessageResponseDto> PinConversationMessageAsync(Guid conversationId, Guid messageId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var message = await _repositories.MessageRepository
            .GetByIdInConversation(messageId, conversationId)
            .Select(x => new
            {
                x.Content,
                FirstAttachment = x.MessageAttachments
                    .Select(ma => ma.File)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (message == null)
            throw new NotFoundAppException("Message");

        var existPin = await _repositories.PinnedMessageRepository
            .ActivePinInConversation(messageId, conversationId)
            .AsNoTracking()
            .SingleOrDefaultAsync();

        if (existPin is not null)
            return _mapper.Map<PinnedMessageResponseDto>(existPin);

        var contentSnapshot = BuildContentSnapshot(message.Content, message.FirstAttachment);

        var pin = new PinnedMessage
        {
            MessageId = messageId,
            ConversationId = conversationId,
            PinnedByUserId = authUserId,
            ContentSnapshot = contentSnapshot
        };

        try
        {
            await _repositories.PinnedMessageRepository.AddAsync(pin);
            await _repositories.UnitOfWork.SaveChangesAsync();

            var pinDto = _mapper.Map<PinnedMessageResponseDto>(pin);

            try { await _signalR.BroadcastConversationPinAsync(conversationId, pinDto); }
            catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastChannelPin failed."); }

            return pinDto;
        }
        catch (DbUpdateException ex) when (ex.IsPinnedMessagePerConversationUniqueViolation())
        {
            var concurrentPin = await _repositories.PinnedMessageRepository
                .ActivePinInConversation(messageId, conversationId)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            if (concurrentPin is null)
                throw;

            return _mapper.Map<PinnedMessageResponseDto>(concurrentPin);
        }
    }

    public async Task<ReadStateDto> MarkConversationMessageReadAsync(Guid conversationId, Guid messageId)
    => await MarkConversationMessageAsync(conversationId, messageId, markUnread: false);

    public async Task<ReadStateDto> MarkConversationMessageUnreadAsync(Guid conversationId, Guid messageId)
        => await MarkConversationMessageAsync(conversationId, messageId, markUnread: true);

    public async Task MarkMessageDeliveredAsync(Guid conversationId, Guid messageId, Guid ackingUserId)
    {
        if (!await _repositories.ConversationRepository.IsActiveParticipantAsync(ackingUserId, conversationId))
            return;

        var conversationType = await _repositories.ConversationRepository
            .FindByCondition(c => c.Id == conversationId)
            .Select(c => (ConversationTypeEnum?)c.Type)
            .FirstOrDefaultAsync();

        if (conversationType is null)
            return;

        var updated = conversationType == ConversationTypeEnum.Direct
            ? await _repositories.MessageRepository.MarkDmDeliveredAsync(messageId, conversationId, ackingUserId)
            : await _repositories.MessageReceiptRepository.MarkDeliveredAsync(messageId, ackingUserId);

        if (!updated)
            return;

        try
        {
            await _signalR.BroadcastConversationMessageDeliveredAsync(conversationId, new MessageDeliveredPush
            {
                ConversationId = conversationId,
                MessageId = messageId,
                RecipientUserId = ackingUserId,
                DeliveredAt = DateTime.UtcNow
            });
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastConversationMessageDelivered failed."); }
    }

    /// <summary>
    /// Called from the SignalR hub when a client marks messages as seen in real
    /// time (e.g. conversation is open and focused) — independent of the REST
    /// "mark read" endpoint, which also calls into <see cref="MarkConversationReadAndSeenAsync"/>
    /// under the hood so both paths behave identically.
    /// </summary>
    public async Task MarkMessagesSeenAsync(Guid conversationId, Guid upToMessageId, Guid ackingUserId)
    {
        if (!await _repositories.ConversationRepository.IsActiveParticipantAsync(ackingUserId, conversationId))
            return; // best-effort ack — don't throw over a race (e.g. user just left)

        var conversationType = await _repositories.ConversationRepository
            .FindByCondition(c => c.Id == conversationId)
            .Select(c => (ConversationTypeEnum?)c.Type)
            .FirstOrDefaultAsync();

        if (conversationType is null)
            return;

        var sequenceNumber = await _repositories.MessageRepository
            .FindByCondition(m => m.Id == upToMessageId && m.ConversationId == conversationId)
            .Select(m => (long?)m.SequenceNumber)
            .FirstOrDefaultAsync();

        if (sequenceNumber is null)
            return;

        await MarkConversationReadAndSeenAsync(conversationId, upToMessageId, ackingUserId, conversationType, sequenceNumber.Value);
    }

    public async Task DeleteConversationMessageAsync(Guid conversationId, Guid messageId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var message = await _repositories.MessageRepository
            .GetByIdInConversation(messageId, conversationId)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundAppException("Message");

        if (message.IsDeleted)
            throw new NotFoundAppException("Message");

        if (message.SenderId != authUserId)
            throw new ForbiddenAppException("You can only delete your own messages.");

        message.IsDeleted = true;
        message.DeletedBy = authUserId;
        message.DeletedAt = DateTime.UtcNow;
        message.UpdatedAt = DateTime.UtcNow;

        await _repositories.UnitOfWork.SaveChangesAsync();

        try { await _signalR.BroadcastConversationMessageDeletedAsync(conversationId, messageId, authUserId); }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastConversationMessageDeleted failed."); }
    }

    public async Task ForceDeleteConversationMessageAsync(Guid conversationId, Guid messageId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var message = await _repositories.MessageRepository
            .GetByIdInConversation(messageId, conversationId)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundAppException("Message");

        if (message.IsDeleted)
            throw new NotFoundAppException("Message");

        if (message.SenderId == authUserId)
            throw new InvalidDataAppException("Use the delete-own endpoint to delete your own messages.");

        message.IsDeleted = true;
        message.DeletedBy = authUserId;
        message.DeletedAt = DateTime.UtcNow;
        message.UpdatedAt = DateTime.UtcNow;

        await _repositories.UnitOfWork.SaveChangesAsync();

        try { await _signalR.BroadcastConversationMessageDeletedAsync(conversationId, messageId, authUserId); }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastConversationMessageDeleted failed."); }
    }

    #endregion

    #region Private Section

    private async Task<MessageDto?> TryGetExistingMessageAsync(Guid senderId, Guid clientMessageId, Guid? channelId = null, Guid? conversationId = null)
    {
        var existing = await _repositories.MessageRepository
            .FindByClientMessageIdAsync(senderId, clientMessageId, channelId, conversationId)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync();

        if (existing is null)
            return null;

        _logger.LogWarning("Duplicate ClientMessageId {ClientMessageId} from sender {SenderId}. Returning existing message {MessageId}.", clientMessageId, senderId, existing.Id);

        return await GetMessageDto(existing.Id);
    }

    private async Task ValidateThreadRootAsync(Guid? threadRootMessageId, Guid? channelId = null, Guid? conversationId = null)
    {
        if (!threadRootMessageId.HasValue)
            return;

        var root = await _repositories.MessageRepository
            .GetThreadRootAsync(threadRootMessageId.Value, channelId, conversationId);

        if (root is null)
            throw new InvalidDataAppException("Thread root message not found or has been deleted.");

        if (root.ThreadRootMessageId is not null)
            throw new InvalidDataAppException("Cannot reply to a thread reply. Maximum thread depth is 1.");
    }

    private async Task<List<FileEntity>> ValidateAttachmentsAsync(List<Guid> fileIds, Guid senderId)
    {
        if (fileIds.Count == 0)
            return [];

        var attachedFiles = await _repositories.FileRepository
            .GetPendingAttachmentsByIdsAsync(fileIds, senderId);

        if (attachedFiles.Count != fileIds.Count)
        {
            var missingIds = fileIds
                .Except(attachedFiles.Select(f => f.Id))
                .ToList();

            throw new InvalidDataAppException($"One or more files could not be attached. Missing: {string.Join(", ", missingIds)}");
        }

        return attachedFiles;
    }

    private Message CreateMessage(SendMessageDto dto, Guid senderId, MessageTypeEnum messageType, Guid? channelId = null, Guid? conversationId = null)
    {
        return new Message
        {
            ClientMessageId = dto.ClientMessageId,
            ChannelId = channelId,
            ConversationId = conversationId,
            SenderId = senderId,

            ThreadRootMessageId = dto.ThreadRootMessageId,
            Content = dto.Content?.Trim(),
            MessageType = messageType,

            CreatedAt = DateTime.UtcNow,
        };
    }

    private async Task AttachFilesAsync(Message message, List<Guid> fileIds, List<FileEntity> attachedFiles, Guid? channelId = null, Guid? conversationId = null)
    {
        if (fileIds.Count == 0)
            return;

        var surviving = await _repositories.MessageAttachmentRepository
            .FindByCondition(a => a.MessageId == message.Id)
            .ToListAsync();

        var desiredOrder = fileIds
            .Select((fileId, idx) => (fileId, idx))
            .ToDictionary(x => x.fileId, x => x.idx);

        foreach (var attachment in surviving)
        {
            if (desiredOrder.TryGetValue(attachment.FileId, out var newOrder))
                attachment.DisplayOrder = newOrder;
        }

        foreach (var file in attachedFiles)
        {
            file.Status = FileStatusEnum.Attached;
            file.ExpiresAt = null;
            file.ChannelId = channelId;
            file.ConversationId = conversationId;
        }

        var attachments = attachedFiles
            .Select(x =>
            {
                var attachment = new MessageAttachment
                {
                    MessageId = message.Id,
                    FileId = x.Id
                };

                if (desiredOrder.TryGetValue(attachment.FileId, out var newOrder))
                    attachment.DisplayOrder = newOrder;

                return attachment;
            })
            .ToList();

        await _repositories.MessageAttachmentRepository.AddRangeAsync(attachments);
    }

    private async Task CreateMentionsAsync(Message message, List<Guid> mentionedUserIds, Guid? channelId = null, Guid? conversationId = null)
    {
        if (mentionedUserIds.Count == 0)
            return;

        var uniqueUserIds = mentionedUserIds.Distinct().ToList();

        var activeMentionedUserIds = await _repositories.UserRepository
            .GetByIds(uniqueUserIds)
            .Select(x => x.Id)
            .ToListAsync();

        if (activeMentionedUserIds.Count != uniqueUserIds.Count)
        {
            var missingIds = uniqueUserIds
                .Except(activeMentionedUserIds)
                .ToList();

            throw new InvalidDataAppException($"One or more mentioned user could not be found. Missing: {string.Join(", ", missingIds)}");
        }

        var mentions = uniqueUserIds
            .Distinct()
            .Select(uid => new MessageMention
            {
                MessageId = message.Id,
                MentionedUserId = uid,
                ChannelId = channelId,
                ConversationId = conversationId,
            })
            .ToList();

        await _repositories.MessageMentionRepository.AddRangeAsync(mentions);
    }

    private async Task UpdateThreadCountersAsync(Guid? threadRootMessageId)
    {
        if (!threadRootMessageId.HasValue)
            return;

        await _repositories.MessageRepository
            .IncrementReplyCountAsync(threadRootMessageId.Value, DateTime.UtcNow);

        _repositories.UnitOfWork.QueueManualAuditLog(new AuditLogRequest(
            tableName: "Message",
            eventName: "ReplyCountIncremented",
            payload: new
            {
                MessageId = threadRootMessageId.Value
            }));
    }

    private async Task<List<(string PublicId, FileTypeEnum FileType)>> RemoveAttachmentsAsync(Guid messageId, List<Guid> fileIdsToRemove, Guid authUserId)
    {
        var attachmentsToDelete = await _repositories.MessageAttachmentRepository
            .FindByCondition(a => a.MessageId == messageId && fileIdsToRemove.Contains(a.FileId))
            .ToListAsync();

        _repositories.MessageAttachmentRepository.RemoveRange(attachmentsToDelete);

        var filesToDelete = await _repositories.FileRepository
            .FindByCondition(f => fileIdsToRemove.Contains(f.Id) && !f.IsDeleted)
            .ToListAsync();

        foreach (var file in filesToDelete)
        {
            file.IsDeleted = true;
        }

        return filesToDelete.Select(x => (x.PublicId, x.FileType)).ToList();
    }

    private async Task RemoveMentionsAsync(List<Guid> mentionIds)
    {
        var mentionsToDelete = await _repositories.MessageMentionRepository
            .FindByCondition(mn => mentionIds.Contains(mn.Id))
            .ToListAsync();

        _repositories.MessageMentionRepository.RemoveRange(mentionsToDelete);
    }

    private static MessageTypeEnum DeriveMessageType(string? content, IEnumerable<FileTypeEnum> fileTypes)
    {
        var hasText = !string.IsNullOrWhiteSpace(content);

        if (hasText)
            return MessageTypeEnum.Text;

        var types = fileTypes.ToList();

        if (types.Count == 0)
            throw new InvalidDataAppException("Message must contain text or at least one attachment.");

        return fileTypes.All(f => f == FileTypeEnum.Image) ? MessageTypeEnum.Image : MessageTypeEnum.File;
    }

    private static HashSet<Guid> ResolveDirectDmOtherParticipant(Guid? directParticipantAId, Guid? directParticipantBId, Guid senderId)
    {
        if (directParticipantAId is null || directParticipantBId is null)
            throw new AppException("Direct conversation is missing participant references.");

        var otherId = directParticipantAId == senderId
            ? directParticipantBId.Value
            : directParticipantAId.Value;

        return [otherId];
    }

    private async Task<MessageDto> GetMessageDto(Guid id)
    {
        return await _repositories.MessageRepository
            .FindByCondition(m => m.Id == id)
            .AsNoTracking()
            .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
            .FirstAsync();
    }

    private static string BuildContentSnapshot(string? content, FileEntity? attachment)
    {
        return !string.IsNullOrWhiteSpace(content)
        ? content.Truncate(500)
        : attachment switch
        {
            null => "[Attachment]",
            { FileType: FileTypeEnum.Image, OriginalName: var n } => $"📷 {n}",
            { FileType: FileTypeEnum.Video, OriginalName: var n } => $"🎥 {n}",
            { FileType: FileTypeEnum.Audio, OriginalName: var n } => $"🎵 {n}",
            { FileType: FileTypeEnum.Document, OriginalName: var n } => $"📄 {n}",
            { OriginalName: var n } => $"📎 {n}"
        };
    }

    /// <summary>
    /// Core toggle logic shared by both channel and conversation variants.
    ///
    /// Flow:
    ///   1. Check if the user already has this reaction on this message.
    ///   2a. If yes → remove it (hard-delete; reactions have no soft-delete).
    ///   2b. If no  → add it. Catches the unique constraint race and treats it
    ///               as an idempotent "already added" rather than a 500.
    ///   3. Build and return the updated per-emoji summary.
    /// </summary>
    private async Task<MessageReactionToggleResultDto> ToggleReactionAsync(Guid messageId, Guid? channelId, Guid? conversationId, Guid authUserId, string emoji)
    {
        var existingReaction = await _repositories.MessageReactionRepository
            .FindByCondition(r =>
                r.MessageId == messageId &&
                r.UserId == authUserId &&
                r.Emoji == emoji)
            .FirstOrDefaultAsync();

        bool added;

        if (existingReaction is not null)
        {
            _repositories.MessageReactionRepository.Remove(existingReaction);
            await _repositories.UnitOfWork.SaveChangesAsync();
            added = false;
        }
        else
        {
            var reaction = new MessageReaction
            {
                MessageId = messageId,
                UserId = authUserId,
                Emoji = emoji,
                CreatedAt = DateTime.UtcNow,
            };

            await _repositories.MessageReactionRepository.AddAsync(reaction);

            try
            {
                await _repositories.UnitOfWork.SaveChangesAsync();
                added = true;
            }
            catch (DbUpdateException ex) when (ex.IsReactionUniqueViolation())
            {
                _repositories.UnitOfWork.DetachEntity(reaction);
                added = true;
            }
        }

        // ── Build updated summary for this emoji ──────────────────────────
        var updatedSummary = await BuildReactionSummaryForEmojiAsync(messageId, emoji, authUserId);

        return new MessageReactionToggleResultDto
        {
            Emoji = emoji,
            Added = added,
            UpdatedSummary = updatedSummary?.Count > 0 ? updatedSummary : null,
        };
    }

    /// <summary>
    /// Builds the reaction summary for a single emoji on a message.
    /// Used after a toggle to return the updated state to the client.
    /// Returns null if no reactions exist for that emoji any more.
    /// </summary>
    private async Task<MessageReactionSummaryDto?> BuildReactionSummaryForEmojiAsync(Guid messageId, string emoji, Guid authUserId)
    {
        var reactors = await _repositories.MessageReactionRepository
            .FindByCondition(r => r.MessageId == messageId && r.Emoji == emoji)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.UserId, r.User.Name })
            .ToListAsync();

        if (reactors.Count == 0)
            return null;

        return new MessageReactionSummaryDto
        {
            Emoji = emoji,
            Count = reactors.Count,
            ReactedByMe = reactors.Any(r => r.UserId == authUserId),
            PreviewNames = reactors
                .Take(5)
                .Select(r => r.Name)
                .ToList(),
        };
    }

    /// <summary>
    /// After ProjectTo, reactions are grouped but ReactedByMe is false for everyone
    /// (AutoMapper has no auth context). This method does a single batch query to
    /// find which (messageId, emoji) pairs the calling user has reacted to, then
    /// stamps the flag in memory.
    /// </summary>
    private async Task StampReactionsAsync(List<MessageDto> messages, Guid authUserId)
    {
        if (messages.Count == 0)
            return;

        var messageIds = messages.Select(m => m.Id).ToList();

        var rawReactions = await _repositories.MessageReactionRepository
            .FindByCondition(r => messageIds.Contains(r.MessageId))
            .OrderBy(r => r.CreatedAt)
            .Select(r => new
            {
                r.MessageId,
                r.UserId,
                r.Emoji,
                r.User.Name,
            })
            .ToListAsync();

        if (rawReactions.Count == 0)
            return;

        var grouped = rawReactions
            .GroupBy(r => r.MessageId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(r => r.Emoji)
                       .Select(eg => new MessageReactionSummaryDto
                       {
                           Emoji = eg.Key,
                           Count = eg.Count(),
                           ReactedByMe = eg.Any(r => r.UserId == authUserId),
                           PreviewNames = eg.Take(5).Select(r => r.Name).ToList(),
                       })
                       .ToList()
            );

        foreach (var message in messages)
        {
            if (grouped.TryGetValue(message.Id, out var reactionSummaries))
                message.Reactions = reactionSummaries;
        }
    }

    /// <summary>
    /// Populates GroupDeliveredAt/GroupSeenAt for the caller's own messages in a
    /// Group conversation. Only the sender's messages get ticks — you don't need
    /// a tick on a message you received. "Delivered to all" / "seen by all":
    /// null unless every other participant's receipt has reached that stage.
    /// </summary>
    private async Task StampGroupTicksAsync(List<MessageDto> messages, Guid authUserId)
    {
        var ownMessageIds = messages
            .Where(m => m.SenderId == authUserId)
            .Select(m => m.Id)
            .ToList();

        if (ownMessageIds.Count == 0)
            return;

        var receipts = await _repositories.MessageReceiptRepository
            .FindByCondition(r => ownMessageIds.Contains(r.MessageId))
            .Select(r => new { r.MessageId, r.DeliveredAt, r.SeenAt })
            .AsNoTracking()
            .ToListAsync();

        if (receipts.Count == 0)
            return;

        var grouped = receipts.GroupBy(r => r.MessageId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var message in messages)
        {
            if (!grouped.TryGetValue(message.Id, out var messageReceipts) || messageReceipts.Count == 0)
                continue;

            message.GroupDeliveredAt = messageReceipts.All(r => r.DeliveredAt.HasValue)
                ? messageReceipts.Max(r => r.DeliveredAt)
                : null;

            message.GroupSeenAt = messageReceipts.All(r => r.SeenAt.HasValue)
                ? messageReceipts.Max(r => r.SeenAt)
                : null;
        }
    }

    private async Task<ReadStateDto> MarkChannelMessageAsync(Guid orgId, Guid teamId, Guid channelId, Guid messageId, bool markUnread)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        if (!await _repositories.ChannelRepository.IsActiveMembershipAsync(authUserId, channelId))
            throw new ForbiddenAppException();

        var sequenceNumber = await _repositories.MessageRepository
            .FindByCondition(m => m.Id == messageId && m.ChannelId == channelId && m.Channel != null && m.Channel.OrgId == orgId && m.Channel.TeamId == teamId)
            .Select(m => (long?)m.SequenceNumber)
            .FirstOrDefaultAsync();

        if (sequenceNumber is null)
            throw new NotFoundAppException("Message");

        var updatedReadState = markUnread
            ? await _repositories.ReadStateRepository.MarkAsUnreadAsync(authUserId, channelId, null, messageId, sequenceNumber.Value)
            : await _repositories.ReadStateRepository.MarkAsReadAsync(authUserId, channelId, null, messageId, sequenceNumber.Value);

        var readState = await _repositories.ReadStateRepository
            .FindByCondition(rs => rs.UserId == authUserId && rs.ChannelId == channelId)
            .AsNoTracking()
            .FirstOrDefaultAsync()
            ?? throw new NotFoundAppException("Channel membership");

        if (updatedReadState is not null)
        {
            await _signalR.PushReadStateBadgeAsync(authUserId, new ReadStatePush
            {
                ChannelId = channelId,
                UnreadCount = readState.UnreadCount,
                MentionCount = readState.MentionCount,
                LastMessageAt = readState.LastReadAt ?? DateTime.UtcNow
            });
        }

        return MapReadState(readState);
    }

    private async Task<ReadStateDto> MarkConversationMessageAsync(Guid conversationId, Guid messageId, bool markUnread)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        if (!await _repositories.ConversationRepository.IsActiveParticipantAsync(authUserId, conversationId))
            throw new ForbiddenAppException();

        var conversationType = await _repositories.ConversationRepository
            .FindByCondition(c => c.Id == conversationId)
            .Select(c => c.Type)
            .FirstOrDefaultAsync();

        var sequenceNumber = await _repositories.MessageRepository
            .FindByCondition(m => m.Id == messageId && m.ConversationId == conversationId)
            .Select(m => (long?)m.SequenceNumber)
            .FirstOrDefaultAsync();

        if (sequenceNumber is null)
            throw new NotFoundAppException("Message");

        ReadState readState;

        if (markUnread)
        {
            var updatedReadState = await _repositories.ReadStateRepository.MarkAsUnreadAsync(authUserId, null, conversationId, messageId, sequenceNumber.Value);

            // Marking unread only resets this user's own badge — it never revokes a Seen
            // receipt the other side(s) already got, matching how the badge/receipt split
            // works everywhere else (WhatsApp/Slack included).
            readState = await _repositories.ReadStateRepository
                .FindByCondition(rs => rs.UserId == authUserId && rs.ConversationId == conversationId)
                .AsNoTracking()
                .FirstOrDefaultAsync()
                ?? throw new NotFoundAppException("Conversation membership");

            if (updatedReadState is not null)
            {
                try
                {
                    await _signalR.PushReadStateBadgeAsync(authUserId, new ReadStatePush
                    {
                        ConversationId = conversationId,
                        UnreadCount = readState.UnreadCount,
                        MentionCount = readState.MentionCount,
                        LastMessageAt = readState.LastReadAt ?? DateTime.UtcNow
                    });
                }
                catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] PushReadStateBadge failed."); }
            }
        }
        else
        {
            // Same path the SignalR "AckMessagesSeen" hub method uses — advances
            // ReadState (UnreadCount/MentionCount/LastReadSequenceNumber/etc.) AND
            // stamps per-message Seen state together, so REST and hub stay in sync.
            readState = await MarkConversationReadAndSeenAsync(conversationId, messageId, authUserId, conversationType, sequenceNumber.Value);
        }

        return MapReadState(readState);
    }

    /// <summary>
    /// Shared by the SignalR "AckMessagesSeen" hub path and the REST "mark read"
    /// endpoint. Advances the caller's ReadState — UnreadCount, MentionCount,
    /// LastReadSequenceNumber, LastReadMessageId, LastReadAt — via the existing
    /// ReadStateRepository.MarkAsReadAsync, stamps per-message Seen state
    /// (DmSeenAt / MessageReceipts.SeenAt) via StampSeenAndBroadcastAsync, and
    /// pushes the updated badge back to the caller.
    /// </summary>
    private async Task<ReadState> MarkConversationReadAndSeenAsync(Guid conversationId, Guid messageId, Guid userId, ConversationTypeEnum? conversationType, long sequenceNumber)
    {
        var updatedReadState = await _repositories.ReadStateRepository.MarkAsReadAsync(userId, null, conversationId, messageId, sequenceNumber);

        await StampSeenAndBroadcastAsync(conversationId, conversationType, userId, sequenceNumber);

        var readState = await _repositories.ReadStateRepository
            .FindByCondition(rs => rs.UserId == userId && rs.ConversationId == conversationId)
            .AsNoTracking()
            .FirstOrDefaultAsync()
            ?? throw new NotFoundAppException("Conversation membership");

        if (updatedReadState is not null)
        {
            try
            {
                await _signalR.PushReadStateBadgeAsync(userId, new ReadStatePush
                {
                    ConversationId = conversationId,
                    UnreadCount = readState.UnreadCount,
                    MentionCount = readState.MentionCount,
                    LastMessageAt = readState.LastReadAt ?? DateTime.UtcNow
                });
            }
            catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] PushReadStateBadge failed."); }
        }

        return readState;
    }

    /// <summary>
    /// Shared by both the REST "mark read" flow and the SignalR "AckMessagesSeen"
    /// hub method. Stamps DmSeenAt (Direct) or MessageReceipts.SeenAt (Group) on
    /// every not-yet-seen message up to the given sequence, sent by someone else,
    /// then broadcasts the result to the conversation group.
    /// </summary>
    private async Task StampSeenAndBroadcastAsync(Guid conversationId, ConversationTypeEnum? conversationType, Guid ackingUserId, long upToSequence)
    {
        var seenMessageIds = conversationType == ConversationTypeEnum.Direct
            ? await _repositories.MessageRepository.MarkDmSeenUpToAsync(conversationId, ackingUserId, upToSequence)
            : await _repositories.MessageReceiptRepository.MarkSeenUpToAsync(conversationId, ackingUserId, upToSequence);

        if (seenMessageIds.Count == 0)
            return;

        try
        {
            await _signalR.BroadcastConversationMessageSeenAsync(conversationId, new MessageSeenPush
            {
                ConversationId = conversationId,
                MessageIds = seenMessageIds,
                RecipientUserId = ackingUserId,
                SeenAt = DateTime.UtcNow
            });
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastConversationMessageSeen failed."); }
    }

    private static ReadStateDto MapReadState(ReadState rs) => new()
    {
        ChannelId = rs.ChannelId,
        ConversationId = rs.ConversationId,
        UnreadCount = rs.UnreadCount,
        MentionCount = rs.MentionCount,
        LastReadSequenceNumber = rs.LastReadSequenceNumber,
        LastReadMessageId = rs.LastReadMessageId,
        LastReadAt = rs.LastReadAt
    };

    #endregion
}
