using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.TeamMember;

public class MemberQueryParams : PaginationParams
{
    /// <summary>
    /// Filter by name or username (case-insensitive, partial match).
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by a specific role. Null returns all roles.
    /// </summary>
    public TeamRoleEnum? Role { get; set; }
}
