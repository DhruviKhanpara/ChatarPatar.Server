namespace ChatarPatar.Application.DTOs.Message.Reaction;

public class MessageReactionToggleDto
{
    /// <summary>
    /// The emoji to add or remove. Accepts the raw Unicode character ("👍") or a shortcode (":thumbsup:")
    /// </summary>
    public string Emoji { get; set; } = null!;
}
