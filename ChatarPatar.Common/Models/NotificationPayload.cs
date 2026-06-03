using ChatarPatar.Common.EmailNotification.Model;

namespace ChatarPatar.Common.Models;

public abstract class NotificationPayload
{
    public string? InitiatedBy { get; set; }
}

public sealed class EmailPayload : NotificationPayload
{
    public EmailPayload() { }

    public EmailPayload(EmailNotificationRequest request)
    {
        Request = request;
    }

    public EmailNotificationRequest Request { get; set; } = null!;
}

public sealed class MessageSentChannelPayload : NotificationPayload
{
    /// <summary>
    /// The string stored in OutboxMessages.Type.
    /// Must match <see cref="MessageSentChannelOutboxHandler.MessageType"/>.
    /// </summary>
    public const string OutboxType = "MessageSent.Channel";

    public Guid MessageId { get; set; }
    public long SequenceNumber { get; set; }
    public Guid ChannelId { get; set; }
    public Guid SenderId { get; set; }
    public List<Guid> MentionedUserIds { get; set; } = [];
}
