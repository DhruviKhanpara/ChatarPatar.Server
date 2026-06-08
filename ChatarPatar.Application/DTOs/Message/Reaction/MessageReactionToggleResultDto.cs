namespace ChatarPatar.Application.DTOs.Message.Reaction;

public class MessageReactionToggleResultDto
{
    public string Emoji { get; set; } = null!;

    public bool Added { get; set; }

    /// <summary>
    /// The updated summary for this emoji after the toggle,
    /// so the client can patch just this emoji's row in its local state.
    /// Null when the reaction was removed and Count reached zero.
    /// </summary>
    public MessageReactionSummaryDto? UpdatedSummary { get; set; }
}
