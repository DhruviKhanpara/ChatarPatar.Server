using ChatarPatar.Application.DTOs.Channel;
using ChatarPatar.Common.Models;

namespace ChatarPatar.Application.ServiceContracts;

public interface IChannelService
{
    Task<PagedResult<ChannelWithRoleDto>> GetChannelsAsync(Guid orgId, Guid teamId, ChannelQueryParams queryParams);
    Task<ChannelDto> GetChannelAsync(Guid orgId, Guid teamId, Guid channelId);
    Task CreateChannelAsync(Guid orgId, Guid teamId, CreateChannelDto dto);
    Task UpdateChannelAsync(Guid orgId, Guid teamId, Guid channelId, UpdateChannelDto dto);
    Task ArchiveChannelAsync(Guid orgId, Guid teamId, Guid channelId);
    Task UnarchiveChannelAsync(Guid orgId, Guid teamId, Guid channelId);
}
