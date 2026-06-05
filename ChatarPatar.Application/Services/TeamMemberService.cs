using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.TeamMember;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.AppLogging.Model.LogRequest;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Application.Services;

internal class TeamMemberService : ITeamMemberService
{
    private readonly IRepositoryManager _repositories;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<TeamMemberService> _logger;

    public TeamMemberService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor, IPermissionService permissionService, ILogger<TeamMemberService> logger)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
        _logger = logger;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<PagedResult<TeamMemberDto>> GetMembersAsync(Guid orgId, Guid teamId, MemberQueryParams queryParams)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        // Resolve caller org/team access
        var callerContext = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .AsNoTracking()
            .Select(t => new
            {
                CallerTeamRole = t.TeamMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (TeamRoleEnum?)m.Role)
                    .FirstOrDefault(),
                CallerOrgRole = t.Organization.OrganizationMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (OrganizationRoleEnum?)m.Role)
                    .FirstOrDefault(),
                t.IsPrivate
            })
            .FirstOrDefaultAsync();

        if (callerContext is null || callerContext.CallerOrgRole is null)
            throw new NotFoundAppException("Team");

        var callerHasElevatedAccess =
            callerContext.CallerOrgRole is OrganizationRoleEnum.OrgOwner or OrganizationRoleEnum.OrgAdmin
            || callerContext.CallerTeamRole is TeamRoleEnum.TeamAdmin;

        // Private teams require explicit membership unless caller has elevated access
        if (callerContext.IsPrivate && !callerHasElevatedAccess)
        {
            if (callerContext.CallerTeamRole is null)
                throw new NotFoundAppException("Team");
        }

        var query = _repositories.TeamMemberRepository
            .GetTeamMembersQuery(teamId, queryParams.Search, queryParams.Role);

        var totalCount = await query.CountAsync();

        var members = await query
            .AsNoTracking()
            .PaginateOffset(queryParams.PageSize, queryParams.PageNumber)
            .ProjectTo<TeamMemberDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<TeamMemberDto>(members, totalCount, queryParams.PageNumber, queryParams.PageSize);
    }

    public async Task AddTeamMemberAsync(Guid orgId, Guid teamId, AddTeamMemberDto dto)
    {
        await _validationService.ValidateAsync<AddTeamMemberDto>(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var context = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .AsNoTracking()
            .Select(t => new
            {
                t.IsArchived,
                TargetIsOrgMember = t.Organization.OrganizationMembers
                    .Any(m => m.UserId == dto.UserId && !m.IsDeleted),
                AlreadyTeamMember = t.TeamMembers
                    .Any(m => m.UserId == dto.UserId && !m.IsDeleted),
                CallerTeamRole = t.TeamMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (TeamRoleEnum?)m.Role)
                    .FirstOrDefault(),
                CallerOrgRole = t.Organization.OrganizationMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (OrganizationRoleEnum?)m.Role)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (context is null)
            throw new NotFoundAppException("Team");

        if (context.IsArchived)
            throw new InvalidDataAppException("Cannot add members to an archived team.");

        if (!context.TargetIsOrgMember)
            throw new InvalidDataAppException("User must be a member of the organization before being added to a team.");

        if (context.AlreadyTeamMember)
            throw new DuplicateEntryAppException("User is already a member of this team.");

        if (dto.Role == TeamRoleEnum.TeamAdmin)
        {
            var callerIsOrgAdmin =
                context.CallerOrgRole is OrganizationRoleEnum.OrgOwner or OrganizationRoleEnum.OrgAdmin;

            var callerIsTeamAdmin =
                context.CallerTeamRole is TeamRoleEnum.TeamAdmin;

            if (!callerIsOrgAdmin && !callerIsTeamAdmin)
                throw new ForbiddenAppException("Only a team admin or org admin can add a member with the TeamAdmin role.");
        }

        var memberEntity = _mapper.Map<TeamMember>(dto);

        memberEntity.TeamId = teamId;
        memberEntity.InvitedByUserId = authUserId;
        memberEntity.JoinedAt = DateTime.UtcNow;

        await _repositories.TeamMemberRepository.AddAsync(memberEntity);

        var publicChannelIds = await _repositories.ChannelRepository
            .FindByCondition(c => c.TeamId == teamId && !c.IsArchived && !c.IsPrivate && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();

            // Seed a ReadState row for every public channel in this team so the new
            // member's unread cursor starts at the current high-water mark for each.
            // Private channels only get a ReadState when explicitly added (see ChannelMemberService).
            // ReadState is UI state — suppress row-level audit (could be N rows for N public channels).
            if (publicChannelIds.Any())
            {
                await _repositories.ReadStateRepository.SeedForChannelsAsync([dto.UserId], publicChannelIds);
                await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync(suppressRowAudit: true);
            }

            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateTeamMemberRoleAsync(Guid orgId, Guid teamId, Guid membershipId, UpdateTeamMemberRoleDto dto)
    {
        await _validationService.ValidateAsync<UpdateTeamMemberRoleDto>(dto);

        var context = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .AsNoTracking()
            .Select(t => new
            {
                t.IsArchived,
                Membership = t.TeamMembers.Where(m => m.Id == membershipId).Select(m => new { m.UserId, m.Role }).FirstOrDefault(),
                AdminCount = t.TeamMembers.Count(m => m.Role == TeamRoleEnum.TeamAdmin && !m.IsDeleted)
            })
            .FirstOrDefaultAsync();

        if (context is null)
            throw new NotFoundAppException("Team");

        if (context.IsArchived)
            throw new InvalidDataAppException("Cannot update members of an archived team.");

        if (context.Membership is null)
            throw new NotFoundAppException("Team membership");

        if (context.Membership.Role == dto.Role)
            return;

        if (context.Membership.Role == TeamRoleEnum.TeamAdmin && dto.Role != TeamRoleEnum.TeamAdmin && context.AdminCount <= 1)
            throw new InvalidDataAppException("This user is the only admin of the team. Assign another admin before changing their role.");

        var membership = await _repositories.TeamMemberRepository
            .GetByIdInTeam(membershipId, teamId)
            .FirstOrDefaultAsync();

        if (membership is null)
            throw new NotFoundAppException("Team membership");

        membership.Role = dto.Role;

        await _repositories.UnitOfWork.SaveChangesAsync();

        TryInvalidatePermissions(context.Membership.UserId, "Failed to invalidate permissions for user {UserId} after team role change");
    }

    public async Task RemoveTeamMemberAsync(Guid orgId, Guid teamId, Guid membershipId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var context = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .AsNoTracking()
            .Select(t => new
            {
                t.IsArchived,
                Membership = t.TeamMembers.Where(m => m.Id == membershipId).Select(m => new { m.UserId, m.Role }).FirstOrDefault(),
                AdminCount = t.TeamMembers.Count(m => m.Role == TeamRoleEnum.TeamAdmin && !m.IsDeleted)
            })
            .FirstOrDefaultAsync();

        if (context is null)
            throw new NotFoundAppException("Team");

        if (context.IsArchived)
            throw new InvalidDataAppException("Cannot remove members from an archived team.");

        if (context.Membership is null)
            throw new NotFoundAppException("Team membership");

        if (context.Membership.UserId == authUserId)
            throw new InvalidDataAppException("You cannot remove yourself. Use the leave team action instead.");

        if (context.Membership.Role == TeamRoleEnum.TeamAdmin && context.AdminCount <= 1)
            throw new InvalidDataAppException("Cannot remove the only admin of the team. Assign another admin first.");

        var membership = await _repositories.TeamMemberRepository
            .GetByIdInTeam(membershipId, teamId)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (membership is null)
            throw new NotFoundAppException("Team membership");

        await ExecuteCascadeRemovalAsync(membership, teamId, actorId: authUserId);

        TryInvalidatePermissions(membership.UserId, "Failed to invalidate permissions for user {UserId} after team member removal");
    }

    public async Task LeaveTeamAsync(Guid orgId, Guid teamId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var context = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .AsNoTracking()
            .Select(t => new
            {
                t.IsArchived,
                Membership = t.TeamMembers.Where(m => m.UserId == authUserId).Select(m => new { m.Id, m.Role }).FirstOrDefault(),
                AdminCount = t.TeamMembers.Count(m => m.Role == TeamRoleEnum.TeamAdmin && !m.IsDeleted)
            })
            .FirstOrDefaultAsync();

        if (context is null)
            throw new NotFoundAppException("Team");

        if (context.IsArchived)
            throw new InvalidDataAppException("Cannot leave an archived team.");

        if (context.Membership is null)
            throw new NotFoundAppException("Team membership");

        if (context.Membership.Role == TeamRoleEnum.TeamAdmin && context.AdminCount <= 1)
            throw new InvalidDataAppException("You are the only admin of this team. Assign another admin before leaving.");

        var membership = await _repositories.TeamMemberRepository
            .GetByIdInTeam(context.Membership.Id, teamId)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (membership is null)
            throw new NotFoundAppException("Team membership");

        await ExecuteCascadeRemovalAsync(membership, teamId, actorId: authUserId);

        TryInvalidatePermissions(authUserId, "Failed to invalidate permissions for user {UserId} after leaving team");
    }

    #region Private Section

    private void TryInvalidatePermissions(Guid userId, string errorTemplate)
    {
        try
        {
            _permissionService.InvalidateUserPermissions(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorTemplate, userId);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SHARED CASCADE REMOVAL LOGIC
    //
    // Phase 1 — Resolve sole-moderator private channels within this team
    //   For private channels where target is only moderator:
    //   a) If other channel members exist → auto-promote next senior
    //   b) If no other channel members    → archive channel (memberships preserved)
    //
    // Phase 2 — Bulk remove all private channel memberships in this team
    //
    // Phase 3 — Soft-delete the TeamMember row (tracked entity)
    //
    // Phase 4 — Queue manual audit entries for bulk operations
    //
    // Phase 5 — Commit + flush audit logs
    //
    // All phases run inside a single transaction.
    // ReadState rows are intentionally left — harmless stale counters,
    // permission checks prevent the user ever reading those channels again.
    // ══════════════════════════════════════════════════════════════════════════

    private async Task ExecuteCascadeRemovalAsync(TeamMember membership, Guid teamId, Guid actorId)
    {
        var targetUserId = membership.UserId;
        var now = DateTime.UtcNow;

        var channelPromotions = new List<(Guid ChannelId, string ChannelName, Guid PromotedUserId)>();
        var channelsAutoArchived = new List<(Guid ChannelId, string ChannelName)>();
        int channelMembershipsRemoved = 0;

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            // ── Idempotency guard — UPDLOCK + HOLDLOCK ────────────────────────────
            // Re-fetch the membership row inside the transaction with an update lock.
            // This serializes concurrent remove/leave calls for the same member:
            //   — The first call acquires the lock and proceeds.
            //   — A second concurrent call blocks here until the first commits.
            //   — After the first commits, the second finds IsDeleted=true and exits.
            // Without this, two concurrent calls can both pass the pre-transaction
            // check and independently run all cascade phases, causing duplicate
            // promotions, double audit entries, and double permission invalidation.
            var freshMembership = await _repositories.TeamMemberRepository
                .GetByIdWithUpdateLockAsync(membership.Id);

            if (freshMembership is null || freshMembership.IsDeleted)
            {
                // Row was already deleted by a concurrent request — nothing to do.
                await tx.RollbackAsync();
                return;
            }

            // ── Phase 1: Sole-moderator private channel resolution ───────────
            var soleModeratorChannels = await _repositories.ChannelMemberRepository
                .GetSoleModeratorPrivateChannelsInTeamWithNextSeniorMemberAsync(targetUserId, teamId);

            foreach (var channel in soleModeratorChannels)
            {
                bool promoted = false;

                if (channel.NextSeniorMembershipId is not null)
                {
                    int rows = await _repositories.CascadeCleanupRepository
                        .PromoteChannelMemberAsync(channel.NextSeniorMembershipId.Value, actorId, now);

                    if (rows > 0)
                    {
                        promoted = true;
                        channelPromotions.Add((channel.ChannelId, channel.ChannelName, channel.NextSeniorMemberId!.Value));
                    }
                }

                if (!promoted)
                {
                    // No other members (or candidate vanished) — archive channel only.
                    // Memberships preserved, consistent with normal archive behaviour.
                    await _repositories.CascadeCleanupRepository
                        .ArchiveChannelAsync(channel.ChannelId, actorId, now);

                    channelsAutoArchived.Add((channel.ChannelId, channel.ChannelName));
                }
            }

            // ── Phase 2: Bulk remove the departing user's channel memberships ─
            // MUST run after Phase 1 so promotion/archive decisions are already made.
            channelMembershipsRemoved = await _repositories.CascadeCleanupRepository
                .BulkRemoveUserChannelMembershipsInTeamAsync(targetUserId, teamId, actorId, now);

            // ── Phase 3: Soft-delete the TeamMember row (tracked entity) ─────
            freshMembership.IsDeleted = true;
            freshMembership.DeletedAt = now;
            freshMembership.DeletedBy = actorId;

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();

            // ── Phase 4: Queue manual audit entries ───────────────────────────
            if (channelMembershipsRemoved > 0)
                _repositories.UnitOfWork.QueueManualAuditLog(new AuditLogRequest(
                    tableName: "ChannelMembers",
                    eventName: "BulkChannelMembershipsRemovedOnTeamRemoval",
                    payload: new { TargetUserId = targetUserId, TeamId = teamId, AffectedRows = channelMembershipsRemoved }));

            if (channelPromotions.Count > 0)
                _repositories.UnitOfWork.QueueManualAuditLog(new AuditLogRequest(
                    tableName: "ChannelMembers",
                    eventName: "AutoPromotionsOnTeamMemberRemoval",
                    payload: new { AutoPromotions = channelPromotions }));

            if (channelsAutoArchived.Count > 0)
                _repositories.UnitOfWork.QueueManualAuditLog(new AuditLogRequest(
                    tableName: "Channels",
                    eventName: "AutoArchivedOnTeamMemberRemoval",
                    payload: new { AutoArchived = channelsAutoArchived }));

            // ── Phase 5: Commit + flush ───────────────────────────────────────
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        _logger.LogInformation(
            "User {TargetUserId} removed from team {TeamId} by actor {ActorId}. "
            + "Channel memberships removed: {ChannelMemberships}. "
            + "Auto-promoted: {ChannelPromotions} channel moderator(s). "
            + "Auto-archived: {ChannelsArchived} private channel(s).",
            targetUserId, teamId, actorId,
            channelMembershipsRemoved,
            channelPromotions.Count,
            channelsAutoArchived.Count);
    }

    #endregion
}
