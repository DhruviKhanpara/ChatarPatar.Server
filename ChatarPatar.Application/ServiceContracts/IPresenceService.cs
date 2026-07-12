using ChatarPatar.Common.Enums;
using ChatarPatar.Common.SignalR.Model;

namespace ChatarPatar.Application.ServiceContracts;

public interface IPresenceService
{
    /// <summary>
    /// Called when a user's first connection opens.
    /// Sets UserStatus = Online and returns the list of org-member user IDs
    /// who should receive the presence broadcast (so the Hub can push to them).
    /// </summary>
    Task OnUserConnectedAsync(Guid userId);

    /// <summary>
    /// Called when a user's last connection closes.
    /// Sets UserStatus = Offline and returns the same broadcast target list.
    /// </summary>
    Task OnUserDisconnectedAsync(Guid userId);

    /// <summary>
    /// Explicit status change (e.g. user picks "Do Not Disturb" from UI).
    /// </summary>
    Task SetCustomStatusAsync(Guid userId, PresenceStatusEnum status, CustomPresenceStatusEnum? customStatus);

    /// <summary>
    /// Returns the current UserStatus row for a user
    /// </summary>
    Task<PresencePush> GetStatusAsync(Guid userId);
}
