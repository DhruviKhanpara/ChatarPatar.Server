namespace ChatarPatar.Application.DTOs.Message.Pin;

public class PinnedMessageListItemDto
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }

    public Guid PinnedByUserId { get; set; }
    public string PinnedByUserName { get; set; } = null!;
    public DateTime PinnedAt { get; set; }

    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = null!;

    public string? ContentSnapshot { get; set; }
}
