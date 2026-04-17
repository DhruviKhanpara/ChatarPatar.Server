using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.OrganizationMember;

public class UpdateOrganizationMemberRoleDto
{
    public OrganizationRoleEnum Role { get; set; } = OrganizationRoleEnum.OrgMember;
}
