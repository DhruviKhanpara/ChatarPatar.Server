using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.OrganizationMember;

public class AddOrganizationMemberDto
{
    public Guid UserId { get; set; }
    public OrganizationRoleEnum Role { get; set; } = OrganizationRoleEnum.OrgMember;
}
