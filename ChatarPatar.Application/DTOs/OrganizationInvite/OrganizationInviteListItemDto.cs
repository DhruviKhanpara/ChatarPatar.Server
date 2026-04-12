using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.OrganizationInvite;

public class OrganizationInviteListItemDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public OrganizationRoleEnum Role { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid InvitedBy { get; set; }
    public string InvitedByName { get; set; } = null!;
}
