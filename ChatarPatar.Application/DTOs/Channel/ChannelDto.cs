using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.Channel;

public class ChannelDto
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public ChannelTypeEnum Type { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Returned when listing channels for the calling user — includes their role in the channel.
/// </summary>
public class ChannelWithRoleDto : ChannelDto
{
    public ChannelRoleEnum? Role { get; set; }
    public DateTime? JoinedAt { get; set; }
    public bool? IsMuted { get; set; }
}
