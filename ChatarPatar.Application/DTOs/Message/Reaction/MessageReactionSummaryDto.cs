namespace ChatarPatar.Application.DTOs.Message.Reaction;

public class MessageReactionSummaryDto
{
    /// <summary>
    /// The emoji character or shortcode, e.g. "👍" or ":thumbsup:"
    /// </summary>
    public string Emoji { get; set; } = null!;

    /// <summary>
    /// Total number of users who reacted with this emoji.
    /// </summary>
    public int Count { get; set; }

    public bool ReactedByMe { get; set; }

    public List<string> PreviewNames { get; set; } = [];
}
