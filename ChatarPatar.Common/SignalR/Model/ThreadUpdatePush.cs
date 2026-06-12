namespace ChatarPatar.Common.SignalR.Model;

public sealed class ThreadUpdatePush
{
    public ThreadUpdatePush(Guid rootMessageId, int replyCount, DateTime lastReplyAt)
    {
        RootMessageId = rootMessageId;
        ReplyCount = replyCount;
        LastReplyAt = lastReplyAt;
    }

    public Guid RootMessageId { get; set; }
    public int ReplyCount { get; set; }
    public DateTime LastReplyAt { get; set; }
}
