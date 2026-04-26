using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.Team;

public class TeamDto
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string? IconThumbnailUrl { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Returned when listing teams for the calling user — includes their role in the team.
/// </summary>
public class TeamWithRoleDto : TeamDto
{
    public TeamRoleEnum? Role { get; set; }
    public DateTime? JoinedAt { get; set; }
    public bool? IsMuted { get; set; }
}
