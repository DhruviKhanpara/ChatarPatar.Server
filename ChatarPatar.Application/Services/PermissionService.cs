using ChatarPatar.Application.RepositoryContracts;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ChatarPatar.Application.Services;

internal class PermissionService : IPermissionService
{
    private readonly IRepositoryManager _repositories;
    private readonly IMemoryCache _cache;

    public PermissionService(IRepositoryManager repositories, IMemoryCache cache)
    {
        _repositories = repositories;
        _cache = cache;
    }

    public async Task<bool> HasPermissionAsync(PermissionContext ctx, string[] permissions, PermissionCheckLogicEnum logic)
    {
        var permKey = string.Join(",", permissions.OrderBy(x => x));
        var cacheKey = $"perm:{ctx.UserId}:{ctx.OrgId}:{ctx.TeamId}:{ctx.ChannelId}:{ctx.ConversationId}:{logic}:{permKey}";
        if (_cache.TryGetValue(cacheKey, out bool cached)) return cached;

        var result = await ComputePermissionAsync(ctx, permissions, logic);

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
            SlidingExpiration = TimeSpan.FromSeconds(10)
        });

        return result;
    }

    #region Private Section

    private async Task<bool> ComputePermissionAsync(PermissionContext ctx, string[] permissions, PermissionCheckLogicEnum logic)
    {
        if (ctx.ChannelId.HasValue && !ctx.TeamId.HasValue)
            throw new ArgumentException("teamId is required when channelId is provided.");

        // 1. Load org role — always present
        var orgRole = await _repositories.OrganizationMemberRepository
            .GetOrgMemberAsync(ctx.UserId, ctx.OrgId)
            .AsNoTracking()
            .Select(x => (OrganizationRoleEnum?)x.Role)
            .FirstOrDefaultAsync();

        if (orgRole == null) return false;

        var combined = new HashSet<string>(32);

        // 2. Add org-level permissions
        if (RolePermissions.OrganizationRolePermissions.TryGetValue((OrganizationRoleEnum)orgRole, out var orgPerms))
            combined.UnionWith(orgPerms);

        if (combined.Contains("*"))
            return true; // wildcard short-circuit for org only

        // 3. Add team-level permissions (scoped to THIS team only)
        if (ctx.TeamId != null)
        {
            // Verify the team actually belongs to the claimed org
            var teamBelongsToOrg = await _repositories.TeamRepository
                .FindByCondition(x => x.Id == ctx.TeamId && x.OrgId == ctx.OrgId)
                .AsNoTracking()
                .Select(x => new
                {
                    Role = x.TeamMembers
                    .Where(m => !m.IsDeleted && m.UserId == ctx.UserId)
                    .Select(m => (TeamRoleEnum?)m.Role)
                    .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (teamBelongsToOrg == null) return false;

            if (teamBelongsToOrg.Role != null && RolePermissions.TeamRolePermissions.TryGetValue((TeamRoleEnum)teamBelongsToOrg.Role, out var teamPerms))
                combined.UnionWith(teamPerms);
        }

        // 4. Add channel-level permissions (private channels only)
        if (ctx.ChannelId is not null)
        {
            var channel = await _repositories.ChannelRepository
                .FindByCondition(x => x.Id == ctx.ChannelId && x.OrgId == ctx.OrgId && x.TeamId == ctx.TeamId)
                .AsNoTracking()
                .Select(x => new
                {
                    x.IsPrivate,
                    Role = x.ChannelMembers
                        .Where(m => !m.IsDeleted && m.UserId == ctx.UserId)
                        .Select(m => (ChannelRoleEnum?)m.Role)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (channel == null) return false; // channel doesn't exist

            if (channel.IsPrivate)
            {
                // Private: must have explicit ChannelMembers row
                if (channel.Role == null) return false; // not a member of this private channel

                if (RolePermissions.ChannelRolePermissions.TryGetValue(channel.Role.Value, out var channelPerms))
                    combined.UnionWith(channelPerms);
            }
            else
            {
                // Public: access is implied by TeamMember — permissions already added in step 3
                // No additional channel-level permissions needed unless they have an explicit row

                // Only add channel-level overrides if they exist (e.g. ChannelModerator on a public channel)
                if (channel.Role != null && RolePermissions.ChannelRolePermissions.TryGetValue(channel.Role.Value, out var channelPerms))
                    combined.UnionWith(channelPerms);

                // If no explicit row, they already have team-level permissions from step 3 — that's enough
            }
        }

        // 5. Add conversation-level permissions
        if (ctx.ConversationId is not null)
        {
            // Verify the conversation actually belongs to the claimed org
            var convBelongsToOrg = await _repositories.ConversationRepository
                .FindByCondition(x => x.Id == ctx.ConversationId && x.OrgId == ctx.OrgId)
                .AsNoTracking()
                .Select(x => new
                {
                    Role = x.ConversationParticipants
                    .Where(m => !m.HasLeft && m.UserId == ctx.UserId)
                    .Select(m => (ConversationParticipantRoleEnum?)m.Role)
                    .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (convBelongsToOrg == null) return false;

            if (convBelongsToOrg.Role != null && RolePermissions.ConversationRolePermissions.TryGetValue((ConversationParticipantRoleEnum)convBelongsToOrg.Role, out var convPerms))
                combined.UnionWith(convPerms);
        }

        return logic switch
        {
            PermissionCheckLogicEnum.All => permissions.All(combined.Contains),
            PermissionCheckLogicEnum.Any => permissions.Any(combined.Contains),
            _ => throw new AppException($"{nameof(logic)} not configured")
        };
    }

    #endregion
}