namespace ChatarPatar.Application.DTOs.User;

public class AuthUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? ProfilePhotoUrl { get; set; }
}
