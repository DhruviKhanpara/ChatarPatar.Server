using ChatarPatar.Application.DTOs.ChannelMember;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Common.Models;

namespace ChatarPatar.Application.ServiceContracts;

public interface IChannelMemberService
{
    Task<PagedResult<ChannelMemberDto>> GetMembersAsync(Guid orgId, Guid teamId, Guid channelId, PaginationParams paginationParams);
    Task AddChannelMemberAsync(Guid orgId, Guid teamId, Guid channelId, AddChannelMemberDto dto);
    Task UpdateChannelMemberRoleAsync(Guid orgId, Guid teamId, Guid channelId, Guid membershipId, UpdateChannelMemberRoleDto dto);
    Task RemoveChannelMemberAsync(Guid orgId, Guid teamId, Guid channelId, Guid membershipId);
    Task LeaveChannelAsync(Guid orgId, Guid teamId, Guid channelId);
}
