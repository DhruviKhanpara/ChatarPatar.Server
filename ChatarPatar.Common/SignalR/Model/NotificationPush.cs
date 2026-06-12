using ChatarPatar.Common.Enums;

namespace ChatarPatar.Common.SignalR.Model;

public class NotificationPush
{
    public Guid Id { get; set; }
    public NotificationTypeEnum Type { get; set; }
    public Guid? ActorId { get; set; }
    public Guid? MessageId { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid? ConversationId { get; set; }
    public string? Preview { get; set; }
    public DateTime CreatedAt { get; set; }
}
