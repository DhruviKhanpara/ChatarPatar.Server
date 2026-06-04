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
    public async Task ArchiveChannelAsync(Guid channelId, Guid actorId, DateTime now) =>
        await _context.Channels
            .Where(c => c.Id == channelId && !c.IsArchived && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsArchived, true)
                .SetProperty(c => c.ArchivedAt, now)
                .SetProperty(c => c.ArchivedBy, actorId));

    /// <inheritdoc />
    public async Task<int> ArchiveTeamAsync(Guid teamId, Guid actorId, DateTime now)
    {
        // Archive the team and all its active channels.
        // Memberships are preserved — consistent with normal archive behaviour.

        // Step 1: Archive the team itself.
        await _context.Teams
            .Where(t => t.Id == teamId && !t.IsArchived && !t.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsArchived, true)
                .SetProperty(t => t.ArchivedAt, now)
                .SetProperty(t => t.ArchivedBy, actorId));

        // Step 2: Archive all active channels in the team.
        var channelArchiveCount = await _context.Channels
            .Where(c => c.TeamId == teamId && !c.IsArchived && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsArchived, true)
                .SetProperty(c => c.ArchivedAt, now)
                .SetProperty(c => c.ArchivedBy, actorId));

        return channelArchiveCount;
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
    public async Task<int> RemoveUserChannelMembershipsInOrgAsync(Guid userId, Guid orgId, Guid actorId, DateTime now) =>
        await _context.ChannelMembers
            .Where(cm => cm.UserId == userId && !cm.IsDeleted && cm.Channel.OrgId == orgId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(cm => cm.IsDeleted, true)
                .SetProperty(cm => cm.DeletedAt, now)
                .SetProperty(cm => cm.DeletedBy, actorId));

    /// <inheritdoc />
    public async Task<int> BulkRemoveUserChannelMembershipsInTeamAsync(Guid userId, Guid teamId, Guid actorId, DateTime now) =>
        await _context.ChannelMembers
            .Where(cm => cm.UserId == userId && !cm.IsDeleted && cm.Channel.TeamId == teamId)
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
}
