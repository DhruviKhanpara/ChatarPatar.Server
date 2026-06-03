using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ChatarPatar.Application.Services;

internal class MessageService : IMessageService
{
    private readonly IRepositoryManager _repositories;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MessageService> _logger;
    private readonly IMapper _mapper; 
    private readonly IOutboxBackgroundQueue _queue;

    public MessageService(IRepositoryManager repositories, IValidationService validationService, IHttpContextAccessor httpContextAccessor, ILogger<MessageService> logger, IMapper mapper, IOutboxBackgroundQueue queue)
    {
        _repositories = repositories;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _mapper = mapper;
        _queue = queue;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

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

        var hasMore = messages.Count > pageSize;

        if (hasMore)
            messages.RemoveAt(pageSize);

        long? nextCursor = null;

        if (hasMore && messages.Count > 0)
            nextCursor = messages.Last().SequenceNumber;

        return new CursorPagedResult<MessageDto>(messages, hasMore, nextCursor);
    }

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
        var messageType = DeriveMessageType(dto.Content, attachedFiles);

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
                .Select(x => new { x.Id, x.Type })
                .FirstOrDefaultAsync()
                ?? throw new NotFoundAppException("Channel");

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
        var messageType = DeriveMessageType(dto.Content, attachedFiles);

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

    #region Private Section

    private async Task<MessageDto?> TryGetExistingMessageAsync(Guid senderId, Guid clientMessageId, Guid? channelId = null, Guid? conversationId = null)
    {
        var existing = await _repositories.MessageRepository
            .FindByClientMessageIdAsync(senderId, clientMessageId, channelId, conversationId)
            .Select(x => new {x.Id})
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

    private static MessageTypeEnum DeriveMessageType(string? content, List<FileEntity> files)
    {
        var hasText = !string.IsNullOrWhiteSpace(content);
        var hasFiles = files.Count > 0;

        if (hasText)
            return MessageTypeEnum.Text;

        if (!hasFiles)
            throw new InvalidDataAppException("Message must contain text or at least one attachment.");

        return files.All(f => f.FileType == FileTypeEnum.Image) ? MessageTypeEnum.Image : MessageTypeEnum.File;
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

    #endregion
}
