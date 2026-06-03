namespace ChatarPatar.Application.DTOs.Message;

public sealed class MessageMentionDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Username { get; set; } = null!;
}
