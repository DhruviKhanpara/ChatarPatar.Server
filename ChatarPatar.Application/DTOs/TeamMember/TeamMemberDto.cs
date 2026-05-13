using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.TeamMember;

public class TeamMemberDto
{
    public Guid MembershipId { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? AvatarThumbnailUrl { get; set; }
    public TeamRoleEnum Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
