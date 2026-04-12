using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.OrganizationInvite;

public class InviteQueryParams : PaginationParams
{
    /// <summary>
    /// Filter by invited email (partial, case-insensitive).
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by the role the invite was sent for. Null returns all roles.
    /// </summary>
    public OrganizationRoleEnum? Role { get; set; }
}
