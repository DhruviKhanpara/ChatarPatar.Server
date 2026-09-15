namespace ChatarPatar.Application.DTOs.Message.Pin;

public class PinnedMessageResponseDto
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid PinnedByUserId { get; set; }
    public DateTime PinnedAt { get; set; }
    public Guid? UnPinnedByUserId { get; set; }
    public DateTime? UnPinnedAt { get; set; }
}
