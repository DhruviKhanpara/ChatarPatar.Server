using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

/// <inheritdoc />
internal class CascadeCleanupRepository : ICascadeCleanupRepository
{
    private readonly AppDbContext _context;

    public CascadeCleanupRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ChannelCleanupResult> CleanupChannelAsync(Guid channelId, Guid actorId, DateTime now)
    {
        // Step 1: Archive the channel record.
        await _context.Channels
            .Where(c => c.Id == channelId && !c.IsArchived && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsArchived, true)
                .SetProperty(c => c.ArchivedAt, now)
                .SetProperty(c => c.ArchivedBy, actorId));

        // Step 2: Soft-delete all active ChannelMembers.
        int channelMembersRemoved = await _context.ChannelMembers
            .Where(cm => cm.ChannelId == channelId && !cm.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(cm => cm.IsDeleted, true)
                .SetProperty(cm => cm.DeletedAt, now)
                .SetProperty(cm => cm.DeletedBy, actorId));

        return new ChannelCleanupResult(channelMembersRemoved);
    }

    /// <inheritdoc />
    public async Task<TeamCleanupResult> CleanupTeamAsync(Guid teamId, Guid actorId, DateTime now)
    {
        // Step 1: Soft-delete all active TeamMembers — one UPDATE statement.
        int teamMembersRemoved = await _context.TeamMembers
            .Where(tm => tm.TeamId == teamId && !tm.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(tm => tm.IsDeleted, true)
                .SetProperty(tm => tm.DeletedAt, now)
                .SetProperty(tm => tm.DeletedBy, actorId));

        // Step 2: Archive all active channels — one UPDATE statement.
        int channelsArchived = await _context.Channels
            .Where(c => c.TeamId == teamId && !c.IsArchived && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsArchived, true)
                .SetProperty(c => c.ArchivedAt, now)
                .SetProperty(c => c.ArchivedBy, actorId));

        // Step 3: Soft-delete all ChannelMembers across ALL channels in this team.
        int channelMembersRemoved = await _context.ChannelMembers
            .Where(cm => cm.Channel.TeamId == teamId && !cm.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(cm => cm.IsDeleted, true)
                .SetProperty(cm => cm.DeletedAt, now)
                .SetProperty(cm => cm.DeletedBy, actorId));

        return new TeamCleanupResult(teamMembersRemoved, channelsArchived, channelMembersRemoved);
    }

    /// <inheritdoc />
    public async Task<int> BulkRemoveUserTeamMembershipsAsync(Guid userId, Guid orgId, Guid actorId, DateTime now) =>
        await _context.TeamMembers
            .Where(tm => tm.UserId == userId && !tm.IsDeleted && tm.Team.OrgId == orgId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(tm => tm.IsDeleted, true)
                .SetProperty(tm => tm.DeletedAt, now)
                .SetProperty(tm => tm.DeletedBy, actorId));

    /// <inheritdoc />
    public async Task<int> BulkRemoveUserChannelMembershipsAsync(Guid userId, Guid orgId, Guid actorId, DateTime now) =>
        await _context.ChannelMembers
            .Where(cm => cm.UserId == userId && !cm.IsDeleted && cm.Channel.OrgId == orgId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(cm => cm.IsDeleted, true)
                .SetProperty(cm => cm.DeletedAt, now)
                .SetProperty(cm => cm.DeletedBy, actorId));

    /// <inheritdoc />
    public async Task<int> PromoteTeamMemberAsync(Guid membershipId, Guid actorId, DateTime now) =>
        await _context.TeamMembers
            .Where(tm => tm.Id == membershipId && !tm.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(tm => tm.Role, TeamRoleEnum.TeamAdmin)
                .SetProperty(tm => tm.UpdatedAt, now)
                .SetProperty(tm => tm.UpdatedBy, actorId));

    /// <inheritdoc />
    public async Task<int> PromoteChannelMemberAsync(Guid membershipId, Guid actorId, DateTime now) =>
        await _context.ChannelMembers
            .Where(cm => cm.Id == membershipId && !cm.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(cm => cm.Role, ChannelRoleEnum.ChannelModerator)
                .SetProperty(cm => cm.UpdatedAt, now)
                .SetProperty(cm => cm.UpdatedBy, actorId));

    /// <inheritdoc />
    public async Task ArchiveTeamAsync(Guid teamId, Guid actorId, DateTime now) =>
        await _context.Teams
            .Where(t => t.Id == teamId && !t.IsArchived && !t.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsArchived, true)
                .SetProperty(t => t.ArchivedAt, now)
                .SetProperty(t => t.ArchivedBy, actorId));
}
