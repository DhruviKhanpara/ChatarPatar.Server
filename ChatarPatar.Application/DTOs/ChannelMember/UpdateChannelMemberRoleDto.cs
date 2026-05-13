using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.ChannelMember;

public class UpdateChannelMemberRoleDto
{
    public ChannelRoleEnum Role { get; set; } = ChannelRoleEnum.ChannelMember;
}
