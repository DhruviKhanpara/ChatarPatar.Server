using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.ChannelMember;

public class AddChannelMemberDto
{
    public Guid UserId { get; set; }
    public ChannelRoleEnum Role { get; set; } = ChannelRoleEnum.ChannelMember;
}
