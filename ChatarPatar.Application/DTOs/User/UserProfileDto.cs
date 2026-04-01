namespace ChatarPatar.Application.DTOs.User;

public class UserProfileDto : UserProfileSummaryDto
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserProfileSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarThumbnailUrl { get; set; }
}