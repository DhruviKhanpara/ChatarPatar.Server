using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.ConversationParticipant;
using ChatarPatar.Common.Models;

namespace ChatarPatar.Application.ServiceContracts;

public interface IConversationParticipantService
{
    /// <summary>
    /// Lists active participants of a group conversation the caller is part of.
    /// </summary>
    Task<PagedResult<ConversationParticipantDto>> GetParticipantsAsync(Guid conversationId, PaginationParams paginationParams);

    /// <summary>
    /// Adds a user to the group.
    /// </summary>
    Task AddParticipantAsync(Guid conversationId, AddConversationParticipantDto dto);

    /// <summary>
    /// Updates a participant's role. Cannot change own role.
    /// </summary>
    Task UpdateParticipantRoleAsync(Guid conversationId, Guid participantId, UpdateConversationParticipantRoleDto dto);

    /// <summary>
    /// Caller leaves the group voluntarily.
    /// Last GroupAdmin must promote someone else before leaving.
    /// </summary>
    Task LeaveConversationAsync(Guid conversationId);

    /// <summary>
    /// removes another participant from the group.
    /// </summary>
    Task RemoveParticipantAsync(Guid conversationId, Guid participantId);
}
