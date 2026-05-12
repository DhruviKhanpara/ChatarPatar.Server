using ChatarPatar.Application.DTOs.Common;

namespace ChatarPatar.Application.DTOs.Channel;

public class ChannelQueryParams : PaginationParams
{
    /// <summary>
    /// Filter by channel name (partial, case-insensitive).
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// When true, returns only archived channels;
    /// when false, only active ones;
    /// null returns all.
    /// </summary>
    public bool? IsArchived { get; set; }

    /// <summary>
    /// When true, includes private channels the caller belongs to.
    /// TeamAdmins / OrgAdmins see all private channels.
    /// </summary>
    public bool IncludePrivate { get; set; } = false;
}
