namespace ChatarPatar.Common.SignalR.Model;

/// <summary>
/// Pushed to a conversation group when one participant's client marks a batch
/// of messages as seen — typically fired from the existing "mark read" flow
/// (Direct DM: Message.DmSeenAt; Group DM: MessageReceipts.SeenAt for that user).
/// </summary>
public sealed class MessageSeenPush
{
    public Guid ConversationId { get; set; }

    /// <summary>Every message id that was newly marked seen in this batch.</summary>
    public List<Guid> MessageIds { get; set; } = [];

    /// <summary>The participant who just saw these messages.</summary>
    public Guid RecipientUserId { get; set; }

    public DateTime SeenAt { get; set; }
}
