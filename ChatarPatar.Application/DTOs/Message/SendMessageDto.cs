namespace ChatarPatar.Application.DTOs.Message;

public sealed class SendMessageDto
{
    /// <summary>
    /// Client-generated GUID used for idempotency.
    /// </summary>
    public Guid ClientMessageId { get; set; }

    /// <summary>
    /// Message body. Nullable — a message may be attachment-only.
    /// At least one of Content or FileIds must be provided.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Ids of files already uploaded.
    /// Order is preserved as DisplayOrder in MessageAttachments.
    /// </summary>
    public List<Guid> FileIds { get; set; } = [];

    /// <summary>
    /// User Ids explicitly mentioned in the message body (e.g. @someone).
    /// </summary>
    public List<Guid> MentionedUserIds { get; set; } = [];

    /// <summary>
    /// Set to the root message Id when replying in a thread.
    /// </summary>
    public Guid? ThreadRootMessageId { get; set; }
}

public sealed class SendMessageRequest
{
    public Guid ClientMessageId { get; set; }
    public string? Content { get; set; }
    public List<Guid> FileIds { get; set; } = [];
    public List<Guid> MentionedUserIds { get; set; } = [];
}