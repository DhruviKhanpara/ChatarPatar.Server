namespace ChatarPatar.Application.DTOs.ReadState;

public class ReadStateDto
{
    public Guid? ChannelId { get; set; }
    public Guid? ConversationId { get; set; }
    public int UnreadCount { get; set; }
    public int MentionCount { get; set; }
    public long LastReadSequenceNumber { get; set; }
    public Guid? LastReadMessageId { get; set; }
    public DateTime? LastReadAt { get; set; }
}
