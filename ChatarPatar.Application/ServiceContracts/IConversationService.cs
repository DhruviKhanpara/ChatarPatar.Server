using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Conversation;
using ChatarPatar.Common.Models;

namespace ChatarPatar.Application.ServiceContracts;

public interface IConversationService
{
    /// <summary>
    /// Returns all conversations (Direct + Group) the caller participates in.
    /// </summary>
    Task<PagedResult<ConversationDto>> GetConversationsAsync(PaginationParams paginationParams);

    /// <summary>
    /// Returns a single conversation the caller is part of.
    /// </summary>
    Task<ConversationDto> GetConversationAsync(Guid conversationId);

    /// <summary>
    /// Checks whether a Direct DM already exists with the target user.
    /// Always returns peer info so the frontend can render the temp page.
    /// ConversationId is null when no DM exists yet.
    /// </summary>
    Task<DirectConversationLookupDto> LookupDirectConversationAsync(Guid targetUserId);

    /// <summary>
    /// Creates a Direct DM with the target user.
    /// Idempotent — returns the existing conversation if one already exists.
    /// </summary>
    Task<ConversationDto> CreateDirectConversationAsync(CreateDirectConversationDto dto);

    /// <summary>
    /// Creates a new Group DM. Caller becomes GroupAdmin automatically.
    /// </summary>
    Task<ConversationDto> CreateGroupConversationAsync(CreateGroupConversationDto dto);
    
    /// <summary>
    /// Upload Group conversation Logo.
    /// </summary>
    Task UpdateGroupConversationLogoAsync(Guid conversationId, ImageUploadDto dto);

    /// <summary>
    /// Renames a Group DM. Need proper permission for this.
    /// </summary>
    Task UpdateGroupConversationAsync(Guid conversationId, UpdateGroupConversationDto dto);

    /// <summary>
    /// Remove Group conversation Logo.
    /// </summary>
    Task RemoveGroupConversationLogoAsync(Guid conversationId);
}
