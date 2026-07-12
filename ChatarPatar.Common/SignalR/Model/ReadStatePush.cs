namespace ChatarPatar.Common.SignalR.Model;

public class ReadStatePush
{
    public Guid? ChannelId { get; set; }
    public Guid? ConversationId { get; set; }
    public int UnreadCount { get; set; }
    public int MentionCount { get; set; }
    public DateTime LastMessageAt { get; set; }
}
