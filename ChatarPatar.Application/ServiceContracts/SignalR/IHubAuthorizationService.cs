namespace ChatarPatar.Application.ServiceContracts.SignalR;

public interface IHubAuthorizationService
{
    /// <summary>
    /// True if the user can join the SignalR group for this channel:
    ///   - Public channel  → active member of the channel's Team
    ///   - Private channel → active ChannelMember row, OR OrgOwner/OrgAdmin,
    ///                       OR TeamAdmin of that channel's team
    /// Result is cached per (userId, channelId)
    /// </summary>
    Task<bool> CanAccessChannelAsync(Guid userId, Guid channelId);

    /// <summary>
    /// True if the user is an active participant of this conversation:
    ///   - Direct → user is one of the two DirectParticipant ids
    ///   - Group  → user has an active (HasLeft = false) ConversationParticipant row
    /// Cached the same way as CanAccessChannelAsync.
    /// </summary>
    Task<bool> CanAccessConversationAsync(Guid userId, Guid conversationId);
}
