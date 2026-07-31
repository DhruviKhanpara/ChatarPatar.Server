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
    private readonly IServiceManager _services;
    private readonly ISignalRService _signalRService;
    private readonly UserConnectionTracker _connectionTracker;
    private readonly IHubAuthorizationService _hubAuth;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IServiceManager services, ISignalRService signalRService, UserConnectionTracker connectionTracker, IHubAuthorizationService hubAuth, ILogger<ChatHub> logger)
    {
        _services = services;
        _signalRService = signalRService;
        _connectionTracker = connectionTracker;
        _hubAuth = hubAuth;
        _logger = logger;
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
            await _services.PresenceService.OnUserConnectedAsync(userId);

            // Broadcast to others who share channels/conversations with this user
            var statusDto = await _services.PresenceService.GetStatusAsync(userId);
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
            await _services.PresenceService.OnUserDisconnectedAsync(userId);

            var statusDto = await _services.PresenceService.GetStatusAsync(userId);
            await _signalRService.BroadcastPresenceAsync(userId, statusDto);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ── Group management (client calls these after opening a view) ────────

    /// <summary>
    /// Client calls this when it opens a channel view.
    /// Joins the group so it receives MessageReceived, TypingIndicator, etc.
    /// Throws HubException if the caller has no access to this channel.
    /// </summary>
    public async Task JoinChannel(Guid channelId)
    {
        var userId = Guid.Parse(Context.User!.GetUserId()
            ?? throw new HubException("Unauthorized: user Id claim missing."));

        var hasAccess = await _hubAuth.CanAccessChannelAsync(userId, channelId);
        if (!hasAccess)
        {
            _logger.LogWarning(
                "[HUB] Rejected JoinChannel — ConnectionId={ConnectionId} UserId={UserId} ChannelId={ChannelId}",
                Context.ConnectionId, userId, channelId);

            throw new HubException("You do not have access to this channel.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}");
    }

    public Task LeaveChannel(Guid channelId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");

    /// <summary>
    /// Client calls this when it opens a DM or group conversation view.
    /// Throws HubException if the caller is not an active participant.
    /// </summary>
    public async Task JoinConversation(Guid conversationId)
    {
        var userId = Guid.Parse(Context.User!.GetUserId()
            ?? throw new HubException("Unauthorized: user Id claim missing."));

        var hasAccess = await _hubAuth.CanAccessConversationAsync(userId, conversationId);
        if (!hasAccess)
        {
            _logger.LogWarning(
               "[HUB] Rejected JoinConversation — ConnectionId={ConnectionId} UserId={UserId} ConversationId={ConversationId}",
               Context.ConnectionId, userId, conversationId);

            throw new HubException("You do not have access to this conversation.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{conversationId}");
    }

    public Task LeaveConversation(Guid conversationId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv:{conversationId}");

    /// <summary>
    /// Client calls this for each user visible in their sidebar
    /// to receive that user's presence updates.
    /// Called after connect, once the sidebar loads.
    /// </summary>
    public Task JoinPresence(Guid watchedUserId)
    => Groups.AddToGroupAsync(Context.ConnectionId, $"presence:{watchedUserId}");

    public Task LeavePresence(Guid watchedUserId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"presence:{watchedUserId}");

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
            ?? throw new HubException("Unauthorized: user Id claim missing.");

        var userId = Guid.Parse(userIdStr);
        var userName = Context.User!.GetUserName() ?? string.Empty;
        var groupKey = isChannel
            ? $"channel:{channelOrConversationId}"
            : $"conv:{channelOrConversationId}";

        // OthersInGroup: the sender doesn't see their own typing indicator
        await Clients.OthersInGroup(groupKey)
            .UserTyping(channelOrConversationId, userId, userName, isTyping);
    }

    // ── Delivery ack ────────────────────────────────────────────────────────

    /// <summary>
    /// Client calls this the moment it actually receives a MessageReceived push
    /// for a conversation message (Direct DM or small Group DM). Best-effort:
    /// swallows failures rather than tearing down the connection, since a lost
    /// ack just means the tick updates a little later via the next Seen batch.
    /// </summary>
    public async Task AckMessageDelivered(Guid conversationId, Guid messageId)
    {
        var userIdStr = Context.User!.GetUserId()
            ?? throw new HubException("Unauthorized: user Id claim missing.");

        var userId = Guid.Parse(userIdStr);

        try
        {
            await _services.MessageService.MarkMessageDeliveredAsync(conversationId, messageId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[HUB] AckMessageDelivered failed — UserId={UserId} ConversationId={ConversationId} MessageId={MessageId}",
                userId, conversationId, messageId);
        }
    }

    /// <summary>
    /// Client calls this in real time when the conversation is open/focused and
    /// the user has actually seen messages up to (and including) upToMessageId
    /// — independent of the REST "mark read" endpoint, which stamps the same
    /// Seen state. Best-effort: swallows failures rather than tearing down the
    /// connection.
    /// </summary>
    public async Task AckMessagesSeen(Guid conversationId, Guid upToMessageId)
    {
        var userIdStr = Context.User!.GetUserId()
            ?? throw new HubException("Unauthorized: user Id claim missing.");

        var userId = Guid.Parse(userIdStr);

        try
        {
            await _services.MessageService.MarkMessagesSeenAsync(conversationId, upToMessageId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[HUB] AckMessagesSeen failed — UserId={UserId} ConversationId={ConversationId} UpToMessageId={UpToMessageId}",
                userId, conversationId, upToMessageId);
        }
    }

    // ── Custom status (user picks "Busy" / "Do Not Disturb" from UI) ──────

    /// <summary>
    /// Persists the user's chosen custom status and broadcasts the change.
    /// </summary>
    public async Task SetStatus(PresenceStatusEnum status, CustomPresenceStatusEnum? customStatus)
    {
        var userIdStr = Context.User!.GetUserId()
            ?? throw new HubException("Unauthorized: user Id claim missing.");

        var userId = Guid.Parse(userIdStr);

        await _services.PresenceService.SetCustomStatusAsync(userId, status, customStatus);

        var statusDto = await _services.PresenceService.GetStatusAsync(userId);
        await _signalRService.BroadcastPresenceAsync(userId, statusDto);
    }
}
