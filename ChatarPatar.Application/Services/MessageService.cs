using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Application.DTOs.Message.Pin;
using ChatarPatar.Application.DTOs.Message.Reaction;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.ExternalServiceContracts;
using ChatarPatar.Infrastructure.Helpers;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ChatarPatar.Application.Services;

internal class MessageService : IMessageService
{
    private readonly IRepositoryManager _repositories;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MessageService> _logger;
    private readonly IMapper _mapper;
    private readonly IOutboxBackgroundQueue _queue;
    private readonly IExternalServiceManager _externalServiceManager;

    public MessageService(IRepositoryManager repositories, IValidationService validationService, IHttpContextAccessor httpContextAccessor, ILogger<MessageService> logger, IMapper mapper, IOutboxBackgroundQueue queue, IExternalServiceManager externalServiceManager)
    {
        _repositories = repositories;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _mapper = mapper;
        _queue = queue;
        _externalServiceManager = externalServiceManager;
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

        // ── Step 1: Idempotency ────────────────────────────────────────────
        var existing = await TryGetExistingMessageAsync(senderId, dto.ClientMessageId, channelId);

        if (existing is not null)
            return existing;

        // ── Step 2: Announcement channel guard ────────────────────────────
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

        // ── Step 3: Thread root validation ────────────────────────────────
        await ValidateThreadRootAsync(dto.ThreadRootMessageId, channelId);

        // ── Step 4: Attachment validation ─────────────────────────────────
        List<FileEntity> attachedFiles = await ValidateAttachmentsAsync(dto.FileIds, senderId);

        // ── Step 5: Derive MessageType ─────────────────────────────────────
        var messageType = DeriveMessageType(dto.Content, attachedFiles.Select(x => x.FileType));

        // ── Step 6: DB transaction ─────────────────────────────────────────
        var message = CreateMessage(dto, senderId, messageType, channelId);

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
            // Worker reads it and fans out ReadState updates for all members.
            var outboxPayload = new MessageSentChannelPayload
            {
                MessageId = message.Id,
                SequenceNumber = message.SequenceNumber,
                ChannelId = channelId,
                SenderId = senderId,
                MentionedUserIds = dto.MentionedUserIds,
                InitiatedBy = senderId.ToString(),
            };

            await _repositories.OutboxMessageRepository.AddAsync(new OutboxMessage
            {
                Type = MessageSentChannelPayload.OutboxType,
                Payload = JsonConvert.SerializeObject(outboxPayload),
                IsProcessed = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = senderId,
                IsDeleted = false
            });

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();

            _queue.Enqueue();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        // SignalR: wire here once Hub is ready
        // _hubContext.Clients.Group(groupKey).SendAsync("MessageReceived", dto);

        return await GetMessageDto(message.Id);
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

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            if (fileIdsToRemove.Count > 0)
                filesToCleanup = await RemoveAttachmentsAsync(messageId, fileIdsToRemove, authUserId);

            if (newFiles.Count > 0)
                await AttachFilesAsync(message.Entity, fileIdsToAdd, newFiles, channelId);

            await ReorderAttachmentsAsync(messageId, dto.FileIds);

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

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        if (filesToCleanup.Any())
            await FileCleanupFromCloudinary(filesToCleanup, messageId);

        return await GetMessageDto(messageId);
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

        return await ToggleReactionAsync(messageId, channelId, null, authUserId, dto.Emoji);
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

            return _mapper.Map<PinnedMessageResponseDto>(pin);
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
    }

    #endregion

    #region Conversation Message

    public async Task<CursorPagedResult<MessageDto>> GetConversationMessagesAsync(Guid conversationId, MessageQueryParams queryParams)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var conversationExists = await _repositories.ConversationRepository
            .GetByIdForUser(conversationId, authUserId)
            .AnyAsync();

        if (!conversationExists)
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
        var message = CreateMessage(dto, senderId, messageType, null, conversationId);

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            // 5a — Message row
            await _repositories.MessageRepository.AddAsync(message);
            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();

            // 5b — Flip pending files → attached, set scope, clear ExpiresAt
            await AttachFilesAsync(message, dto.FileIds, attachedFiles, null, conversationId);

            // 5c — Mentions
            await CreateMentionsAsync(message, dto.MentionedUserIds, null, conversationId);

            // 5d — Thread root counters
            await UpdateThreadCountersAsync(dto.ThreadRootMessageId);

            // 5e — Post-save side effects
            // Conversation: synchronous ReadState update.
            // Participants are a small bounded set so fanout is cheap here.
            var conversation = await _repositories.ConversationRepository
                .FindByCondition(c => c.Id == conversationId)
                .Select(c => new
                {
                    c.Type,
                    c.DirectParticipantAId,
                    c.DirectParticipantBId
                })
                .FirstOrDefaultAsync()
                ?? throw new NotFoundAppException("Conversation");

            var otherParticipantIds = conversation.Type == ConversationTypeEnum.Direct
                ? ResolveDirectDmOtherParticipant(conversation.DirectParticipantAId, conversation.DirectParticipantBId, senderId)
                : await _repositories.ConversationParticipantRepository
                    .GetActiveParticipantsQuery(conversationId)
                    .Where(p => p.UserId != senderId)
                    .Select(p => p.UserId)
                    .ToHashSetAsync();

            var mentionedSet = dto.MentionedUserIds.ToHashSet();

            // 5f — Delivery state for Group conversations
            if (conversation.Type == ConversationTypeEnum.Group)
            {
                if (otherParticipantIds.Count + 1 <= ValidationConstants.Conversation.GroupReceiptThreshold)
                {
                    // Small group: seed one MessageReceipt row per other active participant.
                    // DeliveredAt seeded now as a placeholder — SignalR will set the real delivery time.
                    var receipts = otherParticipantIds
                        .Select(participantId => new MessageReceipt
                        {
                            MessageId = message.Id,
                            UserId = participantId,
                            DeliveredAt = DateTime.UtcNow,
                            SeenAt = null,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        })
                        .ToList();

                    await _repositories.MessageReceiptRepository.AddRangeAsync(receipts);
                }
            }

            var readStates = await _repositories.ReadStateRepository
                    .FindByCondition(rs => otherParticipantIds.Contains(rs.UserId) && rs.ConversationId == conversationId)
                    .ToListAsync();

            // ReadState rows are guaranteed to exist for all participants.
            // They are provisioned during conversation membership creation.
            foreach (var readState in readStates)
            {
                readState.UnreadCount++;
                if (mentionedSet.Contains(readState.UserId))
                    readState.MentionCount++;

                readState.UpdatedAt = DateTime.UtcNow;
            }

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        // SignalR: wire here once Hub is ready
        // _hubContext.Clients.Group(groupKey).SendAsync("MessageReceived", dto);

        return await GetMessageDto(message.Id);
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

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            if (fileIdsToRemove.Count > 0)
                filesToCleanup = await RemoveAttachmentsAsync(messageId, fileIdsToRemove, authUserId);

            if (newFiles.Count > 0)
                await AttachFilesAsync(message.Entity, fileIdsToAdd, newFiles, null, conversationId);

            await ReorderAttachmentsAsync(messageId, dto.FileIds);

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

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        if (filesToCleanup.Any())
            await FileCleanupFromCloudinary(filesToCleanup, messageId);

        return await GetMessageDto(messageId);
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

        return await ToggleReactionAsync(messageId, null, conversationId, authUserId, dto.Emoji);
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

            return _mapper.Map<PinnedMessageResponseDto>(pin);
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

            DmStatus = conversationId.HasValue
                ? DmMessageStatusEnum.Sent
                : null,

            CreatedAt = DateTime.UtcNow,
        };
    }

    private async Task AttachFilesAsync(Message message, List<Guid> fileIds, List<FileEntity> attachedFiles, Guid? channelId = null, Guid? conversationId = null)
    {
        if (fileIds.Count == 0)
            return;

        foreach (var file in attachedFiles)
        {
            file.Status = FileStatusEnum.Attached;
            file.ExpiresAt = null;
            file.ChannelId = channelId;
            file.ConversationId = conversationId;
        }

        var attachments = fileIds
            .Select((fileId, idx) => new MessageAttachment
            {
                MessageId = message.Id,
                FileId = fileId,
                DisplayOrder = idx,
            })
            .ToList();

        await _repositories.MessageAttachmentRepository.AddRangeAsync(attachments);
    }

    private async Task CreateMentionsAsync(Message message, List<Guid> mentionedUserIds, Guid? channelId = null, Guid? conversationId = null)
    {
        if (mentionedUserIds.Count == 0)
            return;

        var mentions = mentionedUserIds
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

        var root = await _repositories.MessageRepository
            .FindByCondition(m => m.Id == threadRootMessageId.Value)
            .FirstAsync();

        root.ReplyCount++;
        root.LastReplyAt = DateTime.UtcNow;
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

    private async Task ReorderAttachmentsAsync(Guid messageId, List<Guid> orderedFileIds)
    {
        if (orderedFileIds.Count == 0)
            return;

        var surviving = await _repositories.MessageAttachmentRepository
            .FindByCondition(a => a.MessageId == messageId)
            .ToListAsync();

        var desiredOrder = orderedFileIds
            .Select((fileId, idx) => (fileId, idx))
            .ToDictionary(x => x.fileId, x => x.idx);

        foreach (var attachment in surviving)
        {
            if (desiredOrder.TryGetValue(attachment.FileId, out var newOrder))
                attachment.DisplayOrder = newOrder;
        }
    }

    private async Task RemoveMentionsAsync(List<Guid> mentionIds)
    {
        var mentionsToDelete = await _repositories.MessageMentionRepository
            .FindByCondition(mn => mentionIds.Contains(mn.Id))
            .ToListAsync();

        _repositories.MessageMentionRepository.RemoveRange(mentionsToDelete);
    }

    private async Task FileCleanupFromCloudinary(List<(string PublicId, FileTypeEnum FileType)> filesToCleanup, Guid messageId)
    {
        foreach (var file in filesToCleanup)
        {
            try
            {
                await _externalServiceManager.CloudinaryService.DeleteFileAsync(file.PublicId, file.FileType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to delete message attachment from Cloudinary. MessageId: {MessageId}, PublicId: {PublicId}",
                    messageId,
                    file.PublicId);
            }
        }
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

    #endregion
}
