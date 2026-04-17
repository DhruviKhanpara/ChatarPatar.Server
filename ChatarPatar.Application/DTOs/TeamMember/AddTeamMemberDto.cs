using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.TeamMember;

public class AddTeamMemberDto
{
    public Guid UserId { get; set; }
    public TeamRoleEnum Role { get; set; } = TeamRoleEnum.TeamMember;
}
