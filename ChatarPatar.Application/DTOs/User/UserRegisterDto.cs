using ChatarPatar.Application.DTOs.Organization;

namespace ChatarPatar.Application.DTOs.User;

public class UserRegisterDto
{
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Password { get; set; } = null!;

    // ── one of these two must be provided ──
    public string? InviteToken { get; set; }
    public CreateOrganizationDto? NewOrg { get; set; }
}
