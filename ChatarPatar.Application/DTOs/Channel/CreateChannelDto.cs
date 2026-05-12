using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.Channel;

public class CreateChannelDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public ChannelTypeEnum Type { get; set; } = ChannelTypeEnum.Text;
    public bool IsPrivate { get; set; } = false;
}
