using ChatarPatar.Common.Enums;

namespace ChatarPatar.Common.SignalR.Model;

public sealed class PresencePush
{
    public Guid UserId { get; set; }
    public PresenceStatusEnum Status { get; set; }
    public CustomPresenceStatusEnum? CustomStatus { get; set; }
    public DateTime LastSeenAt { get; set; }
}
