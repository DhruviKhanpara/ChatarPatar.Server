using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.OrganizationInvite;

public class SendInviteDto
{
    public string Email { get; set; } = null!;
    public OrganizationRoleEnum Role { get; set; } = OrganizationRoleEnum.OrgMember;
}
