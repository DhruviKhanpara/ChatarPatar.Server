using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.TeamMember;

public class UpdateTeamMemberRoleDto
{
    public TeamRoleEnum Role { get; set; } = TeamRoleEnum.TeamMember;
}
