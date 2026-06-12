using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.ServiceContracts.SignalR;
using ChatarPatar.Application.Services.SignalR;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.HttpUserDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatarPatar.API.SignalR.Hubs;

/// <summary>
/// Main SignalR hub.
///
/// Responsibilities:
///   - Group join/leave (channels, conversations, personal feed)
///   - Presence lifecycle (connect/disconnect) → delegates to IPresenceService
///   - Typing relay → fire-and-forget, never persisted
///
/// Auth: JWT bearer token.
/// The browser cannot send Authorization headers over WebSocket, so the
/// token is passed as ?access_token=... query parameter.
/// </summary>

[Authorize]
public class ChatHub : Hub<IChatClient>
{
    private readonly IPresenceService _presenceService;
    private readonly ISignalRService _signalRService;
    private readonly UserConnectionTracker _connectionTracker;

    public ChatHub(IPresenceService presenceService, ISignalRService signalRService, UserConnectionTracker connectionTracker)
    {
        _presenceService = presenceService;
        _signalRService = signalRService;
        _connectionTracker = connectionTracker;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        var userIdStr = Context.User!.GetUserId()
            ?? throw new HubException("Unauthorized: user Id claim missing.");

        var userId = Guid.Parse(userIdStr);

        // Always join the personal feed group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        // Track connection count; only go Online on the FIRST connection
        var isFirstConnection = _connectionTracker.AddConnection(userId);

        if (isFirstConnection)
        {
            // Persist Online status via service (no repo here)
            await _presenceService.OnUserConnectedAsync(userId);

            // Broadcast to others who share channels/conversations with this user
            var statusDto = await _presenceService.GetStatusAsync(userId);
            await _signalRService.BroadcastPresenceAsync(userId, statusDto);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdStr = Context.User!.GetUserId();

        if (userIdStr is null)
        {
            await base.OnDisconnectedAsync(exception);
            return;
        }

        var userId = Guid.Parse(userIdStr);

        // Only go Offline when the LAST tab/device disconnects
        var isLastConnection = _connectionTracker.RemoveConnection(userId);

        if (isLastConnection)
        {
            await _presenceService.OnUserDisconnectedAsync(userId);

            var statusDto = await _presenceService.GetStatusAsync(userId);
            await _signalRService.BroadcastPresenceAsync(userId, statusDto);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ── Group management (client calls these after opening a view) ────────

    /// <summary>
    /// Client calls this when it opens a channel view.
    /// Joins the group so it receives MessageReceived, TypingIndicator, etc.
    /// </summary>
    public Task JoinChannel(Guid channelId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}");

    public Task LeaveChannel(Guid channelId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");

    /// <summary>
    /// Client calls this when it opens a DM or group conversation view.
    /// </summary>
    public Task JoinConversation(Guid conversationId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{conversationId}");

    public Task LeaveConversation(Guid conversationId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv:{conversationId}");

    // ── Typing indicator ──────────────────────────────────────────────────

    /// <summary>
    /// Relays a typing indicator to all OTHER members of the group.
    /// Fire-and-forget — never persisted to DB.
    /// </summary>
    /// <param name="channelOrConversationId">The channel or conversation being typed in.</param>
    /// <param name="isChannel">true = channel group, false = conversation group.</param>
    /// <param name="isTyping">true = started typing, false = stopped.</param>
    public async Task SendTyping(Guid channelOrConversationId, bool isChannel, bool isTyping)
    {
        var userIdStr = Context.User!.GetUserId()
            ?? throw new HubException("Unauthorized: user ID claim missing.");

        var userId = Guid.Parse(userIdStr);
        var userName = Context.User!.GetUserName() ?? string.Empty;
        var groupKey = isChannel
            ? $"channel:{channelOrConversationId}"
            : $"conv:{channelOrConversationId}";

        // OthersInGroup: the sender doesn't see their own typing indicator
        await Clients.OthersInGroup(groupKey)
            .UserTyping(channelOrConversationId, userId, userName, isTyping);
    }

    // ── Custom status (user picks "Busy" / "Do Not Disturb" from UI) ──────

    /// <summary>
    /// Persists the user's chosen custom status and broadcasts the change.
    /// </summary>
    public async Task SetStatus(PresenceStatusEnum status, CustomPresenceStatusEnum? customStatus)
    {
        var userIdStr = Context.User!.GetUserId()
            ?? throw new HubException("Unauthorized: user ID claim missing.");

        var userId = Guid.Parse(userIdStr);

        await _presenceService.SetCustomStatusAsync(userId, status, customStatus);

        var statusDto = await _presenceService.GetStatusAsync(userId);
        await _signalRService.BroadcastPresenceAsync(userId, statusDto);
    }
}
