using AutoMapper;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Conversation;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
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
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IExternalServiceManager _externalServiceManager;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor, IExternalServiceManager externalServiceManager, ILogger<ConversationService> logger)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _externalServiceManager = externalServiceManager;
        _logger = logger;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<PagedResult<ConversationDto>> GetConversationsAsync(PaginationParams paginationParams)
    {
        var userId = Guid.Parse(_httpContext.GetUserId());

        var baseQuery = _repositories.ConversationRepository.GetUserConversationsQuery(userId);

        var totalCount = await baseQuery.CountAsync();

        var conversations = await baseQuery
            .OrderByDescending(c => c.CreatedAt)
            .PaginateOffset(paginationParams.PageSize, paginationParams.PageNumber)
            .AsNoTracking()
            .Include(c => c.LogoFile)
            .Include(c => c.DirectParticipantA).ThenInclude(u => u!.AvatarFile)
            .Include(c => c.DirectParticipantB).ThenInclude(u => u!.AvatarFile)
            .Include(c => c.ConversationParticipants.Where(p => !p.HasLeft))
                .ThenInclude(p => p.User).ThenInclude(u => u.AvatarFile)
            .ToListAsync();

        var items = conversations.Select(c => MapConversation(c, userId)).ToList();

        return new PagedResult<ConversationDto>(items, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
    }

    public async Task<ConversationDto> GetConversationAsync(Guid conversationId)
    {
        var userId = Guid.Parse(_httpContext.GetUserId());

        var conversation = await LoadConversationForUserAsync(conversationId, userId);

        if (conversation is null)
            throw new NotFoundAppException("Conversation");

        return MapConversation(conversation, userId);
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
        {
            var loaded = await LoadConversationForUserAsync(existing.Id, userId);
            return MapConversation(loaded!, userId);
        }

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

        try
        {
            await _repositories.ConversationRepository.AddAsync(conversation);
            await _repositories.UnitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsDirectConversationUniqueViolation())
        {
            var existingConversation = await _repositories.ConversationRepository
                .GetDirectConversationAsync(userId, dto.TargetUserId);

            if (existingConversation is null)
                throw;

            return await LoadAndMapConversationAsync(existingConversation.Id, userId);
        }

        return await LoadAndMapConversationAsync(conversation.Id, userId);
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

        await _repositories.ConversationRepository.AddAsync(conversation);
        await _repositories.UnitOfWork.SaveChangesAsync();

        return await LoadAndMapConversationAsync(conversation.Id, userId);
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

        FileUploadResult? uploadResult = null;
        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            if (conv.LogoFileId != null)
            {
                var existingLogo = await _repositories.FileRepository.GetByIdAsync(conv.LogoFileId.Value).FirstOrDefaultAsync();

                if (existingLogo != null)
                    existingLogo.IsDeleted = true;
            }

            var publicId = CloudinaryPublicId.ConversationLogo(conv.Id);
            uploadResult = await _externalServiceManager.CloudinaryService.UploadProfileAssetAsync(dto.File, CloudinaryPath.Conversation(conversationId).Profile(), publicId);

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

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();

            if (uploadResult != null)
            {
                try { await _externalServiceManager.CloudinaryService.DeleteFileAsync(uploadResult.PublicId); }
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

        string? oldPublicId = null;
        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            if (conv.LogoFile != null)
            {
                oldPublicId = conv.LogoFile.PublicId;
                conv.LogoFile.IsDeleted = true;
            }

            conv.LogoFileId = null;

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        if (oldPublicId != null)
        {
            try { await _externalServiceManager.CloudinaryService.DeleteFileAsync(oldPublicId); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete conversation logo from Cloudinary. PublicId: {PublicId}", oldPublicId);
            }
        }
    }

    #region Private section

    private Task<Conversation?> LoadConversationForUserAsync(Guid conversationId, Guid userId)
    {
        return _repositories.ConversationRepository
            .GetByIdForUser(conversationId, userId)
            .AsNoTracking()
            .Include(c => c.LogoFile)
            .Include(c => c.DirectParticipantA).ThenInclude(u => u!.AvatarFile)
            .Include(c => c.DirectParticipantB).ThenInclude(u => u!.AvatarFile)
            .Include(c => c.ConversationParticipants.Where(p => !p.HasLeft))
                .ThenInclude(p => p.User).ThenInclude(u => u.AvatarFile)
            .FirstOrDefaultAsync();
    }

    private ConversationDto MapConversation(Conversation conversation, Guid callerId)
    {
        var dto = _mapper.Map<ConversationDto>(conversation);

        if (conversation.Type == ConversationTypeEnum.Direct)
        {
            // Peer is whichever side isn't the caller
            var peerUser = conversation.DirectParticipantAId == callerId
                ? conversation.DirectParticipantB
                : conversation.DirectParticipantA;

            if (peerUser is not null)
            {
                dto.Peer = new DirectPeerDto
                {
                    UserId = peerUser.Id,
                    Name = peerUser.Name,
                    UserName = peerUser.Username,
                    AvatarThumbnailUrl = peerUser.AvatarFile?.ThumbnailUrl
                };
            }

            dto.ParticipantCount = 2;
            dto.Role = null; // No roles in Direct DMs
            dto.JoinedAt = null; // No joining date for Direct DMs
        }
        else
        {
            var callerParticipant = conversation.ConversationParticipants
                .FirstOrDefault(p => p.UserId == callerId && !p.HasLeft);

            dto.Role = callerParticipant?.Role;
            dto.JoinedAt = callerParticipant?.JoinedAt;
            dto.ParticipantCount = conversation.ConversationParticipants.Count(p => !p.HasLeft);
        }

        return dto;
    }

    private async Task<ConversationDto> LoadAndMapConversationAsync(Guid conversationId, Guid userId)
    {
        var conversation = await LoadConversationForUserAsync(conversationId, userId);
        return MapConversation(conversation!, userId);
    }

    #endregion
}
