namespace ChatarPatar.Application.DTOs.Message;

public class EditMessageDto
{
    public string? Content { get; set; }

    /// <summary>
    /// The complete desired set of FileIds after the edit.
    /// Pass all file IDs you want to keep (already attached) PLUS any new ones (pending).
    /// Files that were on the original message but are absent here will be removed.
    /// </summary>
    public List<Guid> FileIds { get; set; } = [];

    /// <summary>
    /// The complete desired set of mentioned user IDs after the edit.
    /// Pass all IDs you still want mentioned; users absent here will be un-mentioned.
    /// </summary>
    public List<Guid> MentionedUserIds { get; set; } = [];
}
