using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.Notification;

public sealed class NotificationDto
{
    public Guid Id { get; set; }
    public NotificationTypeEnum Type { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? Preview { get; set; }

    public Guid? ActorId { get; set; }
    public string? ActorName { get; set; }
    public string? ActorAvatarThumbnailUrl { get; set; }

    public Guid? MessageId { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? ConversationId { get; set; }
}
