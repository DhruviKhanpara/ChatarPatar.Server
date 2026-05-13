using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.ChannelMember;

public class ChannelMemberDto
{
    public Guid? MembershipId { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? AvatarThumbnailUrl { get; set; }
    public ChannelRoleEnum Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsMuted { get; set; }
}
