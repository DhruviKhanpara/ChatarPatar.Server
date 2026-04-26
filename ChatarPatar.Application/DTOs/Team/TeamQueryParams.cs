using ChatarPatar.Application.DTOs.Common;

namespace ChatarPatar.Application.DTOs.Team;

public class TeamQueryParams : PaginationParams
{
    /// <summary>
    /// Filter by team name (partial, case-insensitive).
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// When true, returns only archived teams; 
    /// when false, only active ones; 
    /// null returns all.
    /// </summary>
    public bool? IsArchived { get; set; }

    /// <summary>
    /// When true, includes private teams the caller belongs to. 
    /// Admins see all private teams.
    /// </summary>
    public bool IncludePrivate { get; set; } = false;
}
