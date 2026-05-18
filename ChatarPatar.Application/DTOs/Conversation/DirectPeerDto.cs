namespace ChatarPatar.Application.DTOs.Conversation;

/// <summary>
/// The other side of a Direct DM.
/// </summary>
public class DirectPeerDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? AvatarThumbnailUrl { get; set; }
}
