namespace ChatarPatar.Application.DTOs.Message;

public class MessageQueryParams
{
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Cursor-based: return messages with SequenceNumber less than BeforeSequence.
    /// Omit for the first page (returns the latest messages).
    /// </summary>
    public long? BeforeSequence { get; set; }

    /// <summary>
    /// Thread replies under this root message are returned.
    /// </summary>
    public Guid? ThreadRootMessageId { get; set; }
}
