using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Conversation;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.AppLogging.Model.LogRequest;
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

namespace ChatarPatar.Application.Services;

internal class ConversationService : IConversationService
{
    private readonly IRepositoryManager _repositories;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IExternalServiceManager _externalServiceManager;
    private readonly ILogger<ConversationService> _logger;
    private readonly IOutboxBackgroundQueue _queue;

    public ConversationService(IRepositoryManager repositories, IValidationService validationService, IHttpContextAccessor httpContextAccessor, IExternalServiceManager externalServiceManager, ILogger<ConversationService> logger, IOutboxBackgroundQueue queue)
    {
        _repositories = repositories;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _externalServiceManager = externalServiceManager;
        _logger = logger;
        _queue = queue;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<PagedResult<ConversationDto>> GetConversationsAsync(PaginationParams paginationParams)
    {
        var userId = Guid.Parse(_httpContext.GetUserId());

        var baseQuery = _repositories.ConversationRepository.GetUserConversationsQuery(userId);

        var totalCount = await baseQuery.CountAsync();

        var conversations = await baseQuery
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .PaginateOffset(paginationParams.PageSize, paginationParams.PageNumber)
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Type,
                c.Name,
                c.CreatedAt,

                LogoThumbnailUrl = c.LogoFile != null
                    ? c.LogoFile.ThumbnailUrl
                    : null,

                PeerUser = c.DirectParticipantAId == userId ? c.DirectParticipantB : c.DirectParticipantA,

                CallerParticipant = c.ConversationParticipants
                    .Where(p => p.UserId == userId && !p.HasLeft)
                    .Select(p => new
                    {
                        p.Role,
                        p.JoinedAt
                    })
                    .FirstOrDefault(),

                ActiveParticipantCount = c.ConversationParticipants.Count(p => !p.HasLeft)
            })
            .Select(c => new ConversationDto
            {
                Id = c.Id,
                Type = c.Type,
                Name = c.Name,
                LogoThumbnailUrl = c.LogoThumbnailUrl,
                CreatedAt = c.CreatedAt,

                ParticipantCount = c.Type == ConversationTypeEnum.Direct ? 2 : c.ActiveParticipantCount,
                Role = c.Type == ConversationTypeEnum.Direct || c.CallerParticipant == null ? null : c.CallerParticipant.Role,
                JoinedAt = c.Type == ConversationTypeEnum.Direct || c.CallerParticipant == null ? null : c.CallerParticipant.JoinedAt,

                Peer = c.Type == ConversationTypeEnum.Direct
                    ? new DirectPeerDto
                    {
                        UserId = c.PeerUser!.Id,
                        Name = c.PeerUser!.Name,
                        UserName = c.PeerUser!.Username,
                        AvatarThumbnailUrl = c.PeerUser!.AvatarFile != null ? c.PeerUser!.AvatarFile.ThumbnailUrl : null
                    }
                    : null
            })
            .ToListAsync();

        return new PagedResult<ConversationDto>(conversations, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
    }

    public async Task<ConversationDto> GetConversationAsync(Guid conversationId)
    {
        var userId = Guid.Parse(_httpContext.GetUserId());

        var conversation = await LoadAndMapConversationAsync(conversationId, userId);

        if (conversation is null)
            throw new NotFoundAppException("Conversation");

        return conversation;
    }

    public async Task<DirectConversationLookupDto> LookupDirectConversationAsync(Guid targetUserId)
    {
        var userId = Guid.Parse(_httpContext.GetUserId());

        if (targetUserId == userId)
            throw new InvalidDataAppException("You cannot open a Direct conversation with yourself.");

        // Load target user's public info
        var targetUser = await _repositories.UserRepository
            .GetById(id: targetUserId)
            .AsNoTracking()
            .Include(u => u.AvatarFile)
            .FirstOrDefaultAsync();

        if (targetUser is null)
            throw new NotFoundAppException("User");

        var peer = new DirectPeerDto
        {
            UserId = targetUser.Id,
            Name = targetUser.Name,
            UserName = targetUser.Username,
            AvatarThumbnailUrl = targetUser.AvatarFile?.ThumbnailUrl
        };

        // Check if a DM already exists
        var existing = await _repositories.ConversationRepository
            .GetDirectConversationAsync(userId, targetUserId);

        return new DirectConversationLookupDto
        {
            ConversationId = existing?.Id,
            Peer = peer
        };
    }

    public async Task<ConversationDto> CreateDirectConversationAsync(CreateDirectConversationDto dto)
    {
        await _validationService.ValidateAsync<CreateDirectConversationDto>(dto);

        var userId = Guid.Parse(_httpContext.GetUserId());

        if (dto.TargetUserId == userId)
            throw new InvalidDataAppException("You cannot start a Direct conversation with yourself.");

        // return existing if already present
        var existing = await _repositories.ConversationRepository
            .GetDirectConversationAsync(userId, dto.TargetUserId);

        if (existing is not null)
            return await LoadAndMapConversationAsync(existing.Id, userId) ?? new ConversationDto();

        // Verify target user exists
        var targetExists = await _repositories.UserRepository
            .AnyAsync(u => u.Id == dto.TargetUserId && !u.IsDeleted);

        if (!targetExists)
            throw new NotFoundAppException("User");

        // Normalize order so the unique index works
        var (userA, userB) = ConversationHelper.Normalize(userId, dto.TargetUserId);

        var conversation = new Conversation
        {
            Type = ConversationTypeEnum.Direct,
            DirectParticipantAId = userA,
            DirectParticipantBId = userB
        };

        Conversation? existingConversation = null;
        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            await _repositories.ConversationRepository.AddAsync(conversation);
            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();

            // Seed ReadState for both participants. New conversation so sequence starts at 0.
            // ReadState is UI state, not an auditable business decision — suppress.
            await _repositories.ReadStateRepository.SeedForConversationAsync(userId, conversation.Id, true);
            await _repositories.ReadStateRepository.SeedForConversationAsync(dto.TargetUserId, conversation.Id, true);
            
            // Notify the target user that a new DM was started with them
            var dmNotification = new NotificationEntity
            {
                RecipientId = dto.TargetUserId,
                Type = NotificationTypeEnum.DirectMessage,
                ActorId = userId,
                ConversationId = conversation.Id,
                Preview = null,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _repositories.NotificationRepository.AddAsync(dmNotification);

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync(suppressRowAudit: true);

            _repositories.UnitOfWork.QueueManualAuditLog(new AuditLogRequest(
                tableName: "Conversations",
                eventName: "DirectConversationCreated",
                payload: new
                {
                    ConversationId = conversation.Id,
                    CreatedBy = userId,
                    ParticipantCount = 2,
                    ParticipantIds = new[] { dto.TargetUserId, userId }
                }));

            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch (DbUpdateException ex) when (ex.IsDirectConversationUniqueViolation())
        {
            await tx.RollbackAsync();

            existingConversation = await _repositories.ConversationRepository
                .GetDirectConversationAsync(userId, dto.TargetUserId);

            if (existingConversation is null)
                throw;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        var conversationId = existingConversation?.Id ?? conversation.Id;
        return await LoadAndMapConversationAsync(conversationId, userId) ?? new ConversationDto();
    }

    public async Task<ConversationDto> CreateGroupConversationAsync(CreateGroupConversationDto dto)
    {
        await _validationService.ValidateAsync<CreateGroupConversationDto>(dto);

        var userId = Guid.Parse(_httpContext.GetUserId());

        if (dto.ParticipantUserIds.Contains(userId))
            throw new InvalidDataAppException("Do not include yourself in ParticipantUserIds — you are added automatically as GroupAdmin.");

        var normalizedParticipantIds = dto.ParticipantUserIds.Distinct().ToList();

        var foundCount = await _repositories.UserRepository
            .FindByCondition(u => normalizedParticipantIds.Contains(u.Id) && !u.IsDeleted)
            .CountAsync();

        if (foundCount != normalizedParticipantIds.Count)
            throw new NotFoundAppException("One or more participants were not found.");

        var conversation = new Conversation
        {
            Type = ConversationTypeEnum.Group,
            Name = dto.Name.Trim(),
            ConversationParticipants = normalizedParticipantIds
                .Select(id => new ConversationParticipant
                {
                    UserId = id,
                    AddedBy = userId,
                    Role = ConversationParticipantRoleEnum.GroupMember,
                    JoinedAt = DateTime.UtcNow
                })
                .Append(new ConversationParticipant
                {
                    UserId = userId,
                    AddedBy = userId,
                    Role = ConversationParticipantRoleEnum.GroupAdmin,
                    JoinedAt = DateTime.UtcNow
                })
                .ToList()
        };

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            await _repositories.ConversationRepository.AddAsync(conversation);

            // The Conversation row + all ConversationParticipant rows are saved here.
            // We suppress row-level audit because N participants from one user action
            // should produce exactly one BulkEvent entry, not N+1 entries.
            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync(suppressRowAudit: true);

            // Seed ReadState for every participant (creator + all invited users). New conversation so sequence starts at 0/global max for everyone.
            // ReadState is UI state — never audit it.
            var allParticipantIds = normalizedParticipantIds.Append(userId).ToList();
            foreach (var participantId in allParticipantIds)
                await _repositories.ReadStateRepository.SeedForConversationAsync(participantId, conversation.Id, true);

            // Notify all non-creator participants they were added to a group
            var groupNotifications = normalizedParticipantIds
                .Select(participantId => new NotificationEntity
                {
                    RecipientId = participantId,
                    Type = NotificationTypeEnum.AddedToGroup,
                    ActorId = userId,
                    ConversationId = conversation.Id,
                    Preview = conversation.Name,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            if (groupNotifications.Any())
                await _repositories.NotificationRepository.AddRangeAsync(groupNotifications);

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync(suppressRowAudit: true);

            // One BulkEvent entry for the whole operation
            _repositories.UnitOfWork.QueueManualAuditLog(new AuditLogRequest(
                tableName: "Conversations",
                eventName: "GroupConversationCreated",
                payload: new
                {
                    ConversationId = conversation.Id,
                    Name = conversation.Name,
                    CreatedBy = userId,
                    ParticipantCount = allParticipantIds.Count(),
                    ParticipantIds = allParticipantIds
                }));

            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return await LoadAndMapConversationAsync(conversation.Id, userId) ?? new ConversationDto();
    }

    public async Task UpdateGroupConversationLogoAsync(Guid conversationId, ImageUploadDto dto)
    {
        await _validationService.ValidateAsync<ImageUploadDto>(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());
        var fileType = dto.File.ValidateFile(FileUsageContextEnum.Conversation_Logo);

        var conv = await _repositories.ConversationRepository
            .GetByIdForUser(conversationId, authUserId)
            .FirstOrDefaultAsync();

        if (conv is null || conv.Type != ConversationTypeEnum.Group)
            throw new NotFoundAppException("Conversation");

        if (conv.LogoFileId != null)
        {
            var existingLogo = await _repositories.FileRepository.GetByIdAsync(conv.LogoFileId.Value).FirstOrDefaultAsync();

            if (existingLogo != null)
                existingLogo.IsDeleted = true;
        }

        var publicId = CloudinaryPublicId.ConversationLogo(conv.Id);
        var uploadResult = await _externalServiceManager.CloudinaryService.UploadProfileAssetAsync(dto.File, CloudinaryPath.Conversation(conversationId).Profile(), publicId);

        conv.LogoFile = new FileEntity
        {
            UploadedByUserId = authUserId,
            ConversationId = conversationId,
            UsageContext = FileUsageContextEnum.Conversation_Logo,

            PublicId = uploadResult.PublicId,
            Url = uploadResult.Url,
            ThumbnailUrl = uploadResult.ThumbnailUrl,

            SizeInBytes = dto.File.Length,
            OriginalName = dto.File.FileName,
            MimeType = dto.File.ContentType,
            FileType = fileType,
        };

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();

            if (uploadResult != null)
            {
                try { await _externalServiceManager.CloudinaryService.DeleteFileAsync(uploadResult.PublicId, FileTypeEnum.Image); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete conversation logo from Cloudinary. PublicId: {PublicId}", uploadResult.PublicId);
                }
            }

            throw;
        }
    }

    public async Task UpdateGroupConversationAsync(Guid conversationId, UpdateGroupConversationDto dto)
    {
        await _validationService.ValidateAsync<UpdateGroupConversationDto>(dto);

        var userId = Guid.Parse(_httpContext.GetUserId());

        var conversation = await _repositories.ConversationRepository
            .GetByIdForUser(conversationId, userId)
            .FirstOrDefaultAsync();

        if (conversation is null)
            throw new NotFoundAppException("Conversation");

        if (conversation.Type != ConversationTypeEnum.Group)
            throw new InvalidDataAppException("Only Group conversations can be renamed.");

        conversation.Name = dto.Name.Trim();

        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task RemoveGroupConversationLogoAsync(Guid conversationId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var conv = await _repositories.ConversationRepository
            .GetByIdForUser(conversationId, authUserId)
            .Include(x => x.LogoFile)
            .FirstOrDefaultAsync();

        if (conv is null || conv.Type != ConversationTypeEnum.Group)
            throw new NotFoundAppException("Conversation");

        if (conv.LogoFileId == null)
            return;

        var authUserName = _httpContext.GetUserName()
            ?? _httpContext.GetUserEmail()
            ?? _httpContext.GetUserId()
            ?? "System";

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            if (conv.LogoFile != null)
            {
                conv.LogoFile.IsDeleted = true;

                var outboxMessage = OutboxMessageFactory.BuildCloudinaryDeleteMessage(conv.LogoFile.PublicId, FileTypeEnum.Image, authUserId, authUserName);
                await _repositories.OutboxMessageRepository.AddAsync(outboxMessage);
            }

            conv.LogoFileId = null;

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
    }

    #region Private section

    private async Task<ConversationDto?> LoadAndMapConversationAsync(Guid conversationId, Guid userId)
    {
        var baseQuery = _repositories.ConversationRepository
            .GetByIdForUser(conversationId, userId);

        var conversation = await baseQuery
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Type,
                c.Name,
                c.CreatedAt,

                LogoThumbnailUrl = c.LogoFile != null
                    ? c.LogoFile.ThumbnailUrl
                    : null,

                PeerUser = c.DirectParticipantAId == userId ? c.DirectParticipantB : c.DirectParticipantA,

                CallerParticipant = c.ConversationParticipants
                    .Where(p => p.UserId == userId && !p.HasLeft)
                    .Select(p => new
                    {
                        p.Role,
                        p.JoinedAt
                    })
                    .FirstOrDefault(),

                ActiveParticipantCount = c.ConversationParticipants.Count(p => !p.HasLeft)
            })
            .Select(c => new ConversationDto
            {
                Id = c.Id,
                Type = c.Type,
                Name = c.Name,
                LogoThumbnailUrl = c.LogoThumbnailUrl,
                CreatedAt = c.CreatedAt,

                ParticipantCount = c.Type == ConversationTypeEnum.Direct ? 2 : c.ActiveParticipantCount,
                Role = c.Type == ConversationTypeEnum.Direct || c.CallerParticipant == null ? null : c.CallerParticipant.Role,
                JoinedAt = c.Type == ConversationTypeEnum.Direct || c.CallerParticipant == null ? null : c.CallerParticipant.JoinedAt,

                Peer = c.Type == ConversationTypeEnum.Direct
                    ? new DirectPeerDto
                    {
                        UserId = c.PeerUser!.Id,
                        Name = c.PeerUser!.Name,
                        UserName = c.PeerUser!.Username,
                        AvatarThumbnailUrl = c.PeerUser!.AvatarFile != null ? c.PeerUser!.AvatarFile.ThumbnailUrl : null
                    }
                    : null
            })
            .FirstOrDefaultAsync();

        return conversation;
    }

    #endregion
}
