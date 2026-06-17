using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.ConversationParticipant;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Application.Services;

internal class ConversationParticipantService : IConversationParticipantService
{
    private readonly IRepositoryManager _repositories;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<ConversationParticipantService> _logger;

    public ConversationParticipantService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor, IPermissionService permissionService, ILogger<ConversationParticipantService> logger)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
        _logger = logger;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<PagedResult<ConversationParticipantDto>> GetParticipantsAsync(Guid conversationId, PaginationParams paginationParams)
    {
        var userId = Guid.Parse(_httpContext.GetUserId());

        // Verify it's a Group the caller belongs to
        var conversation = await _repositories.ConversationRepository
            .GetByIdForUser(conversationId, userId)
            .FirstOrDefaultAsync();

        if (conversation is null)
            throw new NotFoundAppException("Conversation");

        if (conversation.Type != ConversationTypeEnum.Group)
            throw new InvalidDataAppException("This operation is only allowed for Group conversations.");

        var baseQuery = _repositories.ConversationParticipantRepository
            .GetActiveParticipantsQuery(conversationId)
            .OrderBy(p => p.JoinedAt)
                .ThenBy(p => p.Id);

        var totalCount = await baseQuery.CountAsync();

        var items = await baseQuery
            .AsNoTracking()
            .PaginateOffset(paginationParams.PageSize, paginationParams.PageNumber)
            .ProjectTo<ConversationParticipantDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<ConversationParticipantDto>(items, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
    }

    public async Task AddParticipantAsync(Guid conversationId, AddConversationParticipantDto dto)
    {
        await _validationService.ValidateAsync<AddConversationParticipantDto>(dto);

        var userId = Guid.Parse(_httpContext.GetUserId());

        if (dto.UserId == userId)
            throw new InvalidDataAppException("You cannot add yourself to the conversation.");

        // Enforce max participant cap before adding
        var currentCount = await _repositories.ConversationParticipantRepository
            .GetActiveParticipantsQuery(conversationId)
            .CountAsync();

        if (currentCount >= ValidationConstants.Conversation.MaxGroupParticipantsCount)
            throw new InvalidDataAppException($"Group has reached the maximum limit of {ValidationConstants.Conversation.MaxGroupParticipantsCount} participants.");

        var existingParticipant = await _repositories.ConversationParticipantRepository
            .FindByCondition(p => p.ConversationId == conversationId && p.UserId == dto.UserId)
            .FirstOrDefaultAsync();

        if (existingParticipant is not null)
        {
            if (!existingParticipant.HasLeft)
                throw new DuplicateEntryAppException("User is already a participant of this conversation.");

            // Re-join: reactivate their old row
            existingParticipant.HasLeft = false;
            existingParticipant.LeftAt = null;
            existingParticipant.RejoinedBy = userId;
            existingParticipant.RejoinedAt = DateTime.UtcNow;
            existingParticipant.Role = ConversationParticipantRoleEnum.GroupMember;

            // Reset cursor to the current message high-water mark so messages
            // sent while they were away don't appear as unread, and the
            // sequence number stays consistent with the zero unread count.
            await _repositories.ReadStateRepository.ResetForConversationRejoinAsync(dto.UserId, conversationId);
        }
        else
        {
            var userExists = await _repositories.UserRepository
                .AnyAsync(u => u.Id == dto.UserId && !u.IsDeleted);

            if (!userExists)
                throw new NotFoundAppException("User");

            await _repositories.ConversationParticipantRepository.AddAsync(new ConversationParticipant
            {
                ConversationId = conversationId,
                UserId = dto.UserId,
                AddedBy = userId,
                Role = ConversationParticipantRoleEnum.GroupMember,
                JoinedAt = DateTime.UtcNow
            });

            // First time joining — seed a ReadState row.
            await _repositories.ReadStateRepository.SeedForConversationAsync(dto.UserId, conversationId);

            // Notify the newly added participant
            await _repositories.NotificationRepository.AddAsync(new NotificationEntity
            {
                RecipientId = dto.UserId,
                Type = NotificationTypeEnum.AddedToGroup,
                ActorId = userId,
                ConversationId = conversationId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        var addedUser = await _repositories.UserRepository
            .FindByCondition(u => u.Id == dto.UserId)
            .Select(u => u.Name)
            .FirstAsync();

        var action = existingParticipant is not null ? "rejoined" : "was added to";

        var systemMessage = new Message
        {
            ClientMessageId = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = userId,

            MessageType = MessageTypeEnum.System,
            Content = $"{addedUser} {action} the Group.",
            
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repositories.MessageRepository.AddAsync(systemMessage);

        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateParticipantRoleAsync(Guid conversationId, Guid participantId, UpdateConversationParticipantRoleDto dto)
    {
        await _validationService.ValidateAsync<UpdateConversationParticipantRoleDto>(dto);

        var userId = Guid.Parse(_httpContext.GetUserId());

        var participant = await _repositories.ConversationParticipantRepository
            .GetByIdAsync(participantId, conversationId);

        if (participant is null || participant.HasLeft)
            throw new NotFoundAppException("Participant");

        if (participant.UserId == userId)
            throw new InvalidDataAppException("You cannot change your own role.");

        if (participant.Role == dto.Role)
            return;

        participant.Role = dto.Role;

        await _repositories.UnitOfWork.SaveChangesAsync();

        TryInvalidatePermissions(participant.UserId, "Failed to invalidate permissions for user {UserId} after group member role change");
    }

    public async Task LeaveConversationAsync(Guid conversationId)
    {
        var userId = Guid.Parse(_httpContext.GetUserId());
        var userName = _httpContext.GetUserName();

        var participant = await _repositories.ConversationParticipantRepository
            .GetActiveParticipant(userId, conversationId)
            .FirstOrDefaultAsync();

        if (participant is null)
            throw new NotFoundAppException("Participant");

        // Block last GroupAdmin from leaving without promoting someone else
        if (participant.Role == ConversationParticipantRoleEnum.GroupAdmin)
        {
            var anotherAdminExists = await _repositories.ConversationParticipantRepository
                .AnyAsync(p =>
                    p.ConversationId == conversationId &&
                    p.UserId != userId &&
                    !p.HasLeft &&
                    p.Role == ConversationParticipantRoleEnum.GroupAdmin);

            if (!anotherAdminExists)
                throw new InvalidDataAppException("You are the only Group Admin. Promote another participant before leaving.");
        }

        // Enforce min participant cap — group must stay at MinGroupParticipantsCount or above
        await EnsureParticipantCanBeRemoved(conversationId);

        participant.HasLeft = true;
        participant.LeftAt = DateTime.UtcNow;

        var systemMessage = new Message
        {
            ClientMessageId = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = userId,

            MessageType = MessageTypeEnum.System,
            Content = $"{userName} left the Group.",

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repositories.MessageRepository.AddAsync(systemMessage);

        await _repositories.UnitOfWork.SaveChangesAsync();

        TryInvalidatePermissions(participant.UserId, "Failed to invalidate permissions for user {UserId} after leaving the group");
    }

    public async Task RemoveParticipantAsync(Guid conversationId, Guid participantId)
    {
        var userId = Guid.Parse(_httpContext.GetUserId());

        var participant = await _repositories.ConversationParticipantRepository
            .GetByIdAsync(participantId, conversationId);

        if (participant is null || participant.HasLeft)
            throw new NotFoundAppException("Participant");

        if (participant.UserId == userId)
            throw new InvalidDataAppException("Use the leave endpoint to remove yourself.");

        // Enforce min participant cap — group must stay at MinGroupParticipantsCount or above
        await EnsureParticipantCanBeRemoved(conversationId);

        participant.HasLeft = true;
        participant.LeftAt = DateTime.UtcNow;

        var removedUser = await _repositories.UserRepository
            .FindByCondition(u => u.Id == participant.UserId)
            .Select(u => u.Name)
            .FirstAsync();

        var systemMessage = new Message
        {
            ClientMessageId = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = userId,

            MessageType = MessageTypeEnum.System,
            Content = $"{removedUser} removed from the Group.",

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repositories.MessageRepository.AddAsync(systemMessage);

        await _repositories.UnitOfWork.SaveChangesAsync();

        TryInvalidatePermissions(participant.UserId, "Failed to invalidate permissions for user {UserId} after removing the group");
    }

    #region Private Section

    private void TryInvalidatePermissions(Guid userId, string errorTemplate)
    {
        try
        {
            _permissionService.InvalidateUserPermissions(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorTemplate, userId);
        }
    }

    private async Task EnsureParticipantCanBeRemoved(Guid conversationId)
    {
        var currentCount = await _repositories.ConversationParticipantRepository
            .GetActiveParticipantsQuery(conversationId)
            .CountAsync();

        if (currentCount <= ValidationConstants.Conversation.MinGroupParticipantsCount)
        {
            throw new InvalidDataAppException($"Group must have at least {ValidationConstants.Conversation.MinGroupParticipantsCount} participants.");
        }
    }

    #endregion
}
