namespace ChatarPatar.Common.SignalR.Model;

/// <summary>
/// Pushed to a conversation group when one recipient's client acks that it
/// actually received a message (Direct DM: Message.DmDeliveredAt;
/// Group DM: MessageReceipts.DeliveredAt for that recipient).
/// </summary>
public sealed class MessageDeliveredPush
{
    public Guid ConversationId { get; set; }
    public Guid MessageId { get; set; }

    /// <summary>The participant who just received the message.</summary>
    public Guid RecipientUserId { get; set; }

    public DateTime DeliveredAt { get; set; }
}
