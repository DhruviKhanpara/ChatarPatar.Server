using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class ChannelMemberRepository : BaseSoftDeleteRepository<ChannelMember>, IChannelMemberRepository
{
    public ChannelMemberRepository(AppDbContext context) : base(context) { }

    public IQueryable<ChannelMember> GetChannelMemberAsync(Guid userId, Guid channelId) => 
        FindByCondition(x => x.UserId == userId && x.ChannelId == channelId);

    public IQueryable<ChannelMember> GetByIdInChannel(Guid membershipId, Guid channelId) =>
        FindByCondition(x => x.Id == membershipId && x.ChannelId == channelId);

    public async Task<List<SoleModeratorChannelResult>> GetSoleModeratorPrivateChannelsWithNextSeniorMemberAsync(
    Guid userId, Guid orgId)
    {
        // Only private channels have ChannelMember rows.
        // Public channels have implicit access for all TeamMembers — no rows to worry about.
        //
        // We want channels where:
        //   1. Channel is private and active (not archived, not deleted)
        //   2. Target user has a ChannelModerator row
        //   3. No OTHER active ChannelModerator exists
        //
        // For each qualifying channel we project:
        //   - next senior member (for promotion, null if channel would become empty)
        //   - total active member count (to detect empty-after-removal channels)

        return await FindByCondition(m =>
                m.UserId == userId
                && m.Role == ChannelRoleEnum.ChannelModerator
                && m.Channel.OrgId == orgId
                && m.Channel.IsPrivate
                && !m.Channel.IsArchived
                && !m.Channel.ChannelMembers.Any(other =>
                    other.UserId != userId
                    && other.Role == ChannelRoleEnum.ChannelModerator
                    && !other.IsDeleted))
            .Select(m => new SoleModeratorChannelResult(
                m.ChannelId,
                m.Channel.Name,
                m.Channel.TeamId,
                // Next senior member (excluding departing user)
                m.Channel.ChannelMembers
                    .Where(cm => cm.UserId != userId && !cm.IsDeleted)
                    .OrderBy(cm => cm.JoinedAt)
                    .Select(cm => (Guid?)cm.UserId)
                    .FirstOrDefault(),
                m.Channel.ChannelMembers
                    .Where(cm => cm.UserId != userId && !cm.IsDeleted)
                    .OrderBy(cm => cm.JoinedAt)
                    .Select(cm => (Guid?)cm.Id)
                    .FirstOrDefault()
            ))
            .AsNoTracking()
            .ToListAsync();
    }
}