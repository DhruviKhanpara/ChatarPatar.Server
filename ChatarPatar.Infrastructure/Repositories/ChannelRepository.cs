using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class ChannelRepository : BaseSoftDeleteRepository<Channel>, IChannelRepository
{
    public ChannelRepository(AppDbContext context) : base(context) { }

    public IQueryable<Channel> GetByIdInTeam(Guid channelId, Guid teamId, Guid orgId) =>
        FindByCondition(c => c.Id == channelId && c.TeamId == teamId && c.OrgId == orgId);

    public IQueryable<Channel> GetChannelsQuery(Guid teamId, Guid orgId, Guid callerId, bool callerHasElevatedAccess, string? search = null, bool? isArchived = null, bool includePrivate = true)
    {
        var query = FindByCondition(c => c.TeamId == teamId && c.OrgId == orgId).AsQueryable();

        if (!includePrivate)
        {
            query = query.Where(c => !c.IsPrivate);
        }
        else if (!callerHasElevatedAccess)
        {
            // Non-admins see public channels + private channels they are a member of
            query = query.Where(c =>
                !c.IsPrivate ||
                c.ChannelMembers.Any(m => m.UserId == callerId && !m.IsDeleted));
        }
        // Team admins (and org admins) see everything — no filter needed

        if (isArchived.HasValue)
            query = query.Where(c => c.IsArchived == isArchived.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term));
        }

        return query.OrderBy(c => c.Name);
    }

    public async Task<bool> NameExistsInTeamAsync(Guid teamId, string name, Guid? excludeChannelId = null)
    {
        var query = FindByCondition(c => c.TeamId == teamId && c.Name.ToLower() == name.Trim().ToLower());

        if (excludeChannelId.HasValue)
            query = query.Where(c => c.Id != excludeChannelId.Value);

        return await query.AnyAsync();
    }

    public async Task<bool> IsActiveMembershipAsync(Guid userId, Guid channelId)
    {
        return await FindByCondition(c => c.Id == channelId && !c.IsDeleted)
            .Select(c => new
            {
                c.IsPrivate,
                IsExplicitMember = c.ChannelMembers.Any(m => m.UserId == userId && !m.IsDeleted),
                IsActiveTeamMember = c.Team.TeamMembers.Any(m => m.UserId == userId && !m.IsDeleted),
                OrgRole = c.Organization.OrganizationMembers
                    .Where(m => m.UserId == userId && !m.IsDeleted)
                    .Select(m => (OrganizationRoleEnum?)m.Role)
                    .FirstOrDefault(),
                TeamRole = c.Team.TeamMembers
                    .Where(m => m.UserId == userId && !m.IsDeleted)
                    .Select(m => (TeamRoleEnum?)m.Role)
                    .FirstOrDefault()
            })
            .Select(x =>
                !x.IsPrivate
                    ? x.IsActiveTeamMember
                    : x.IsExplicitMember
                        || x.OrgRole == OrganizationRoleEnum.OrgOwner
                        || x.OrgRole == OrganizationRoleEnum.OrgAdmin
                        || x.TeamRole == TeamRoleEnum.TeamAdmin)
            .FirstOrDefaultAsync();
    }
}

