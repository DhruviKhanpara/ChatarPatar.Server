using System.Collections.Concurrent;

namespace ChatarPatar.Application.Services.SignalR;

/// <summary>
/// Singleton that tracks how many active SignalR connections each user has.
/// Needed so we only set Offline when the LAST tab/device disconnects,
/// not on every individual disconnect.
///
/// Thread-safe: all operations use ConcurrentDictionary atomic APIs.
/// </summary>

public sealed class UserConnectionTracker
{
    // userId → active connection count
    private readonly ConcurrentDictionary<Guid, int> _connections = new();

    /// <summary>
    /// Increments the connection count for this user.
    /// Returns true if this is the user's FIRST connection (was offline before).
    /// </summary>
    public bool AddConnection(Guid userId)
    {
        var newCount = _connections.AddOrUpdate(userId, 1, (_, existing) => existing + 1);
        return newCount == 1; // true = first connection
    }

    /// <summary>
    /// Decrements the connection count for this user.
    /// Returns true if this was the user's LAST connection (now fully offline).
    /// </summary>
    public bool RemoveConnection(Guid userId)
    {
        if (!_connections.TryGetValue(userId, out var current))
            return true; // not tracked — treat as last

        if (current <= 1)
        {
            _connections.TryRemove(userId, out _);
            return true; // last connection gone
        }

        _connections.TryUpdate(userId, current - 1, current);
        return false; // still has other connections open
    }

    public int GetConnectionCount(Guid userId)
        => _connections.TryGetValue(userId, out var count) ? count : 0;

    public bool IsOnline(Guid userId)
        => _connections.TryGetValue(userId, out var count) && count > 0;
}
