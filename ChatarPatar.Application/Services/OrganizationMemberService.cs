using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.OrganizationMember;
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

internal class OrganizationMemberService : IOrganizationMemberService
{
    private readonly IRepositoryManager _repositories;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<OrganizationMemberService> _logger;

    public OrganizationMemberService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor, IPermissionService permissionService, ILogger<OrganizationMemberService> logger)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
        _logger = logger;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<PagedResult<OrganizationMemberDto>> GetMembersAsync(Guid orgId, MemberQueryParams queryParams)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var isMember = await _repositories.OrganizationMemberRepository
            .GetOrgMemberAsync(userId: authUserId, orgId: orgId)
            .AnyAsync();

        if (!isMember)
            throw new NotFoundAppException("Organization");

        var baseQuery = _repositories.OrganizationMemberRepository
            .GetOrgMembersQuery(orgId, queryParams.Search, queryParams.Role);

        var totalCount = await baseQuery.CountAsync();

        var items = await baseQuery
            .AsNoTracking()
            .PaginateOffset(queryParams.PageSize, queryParams.PageNumber)
            .ProjectTo<OrganizationMemberDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<OrganizationMemberDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
    }

    public async Task<OrganizationMemberDto> GetOrganizationMemberAsync(Guid orgId, Guid membershipId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        // Caller must be an active member of the org
        var isMember = await _repositories.OrganizationMemberRepository
            .GetOrgMemberAsync(userId: authUserId, orgId: orgId)
            .AnyAsync();

        if (!isMember)
            throw new NotFoundAppException("Organization");

        var membership = await _repositories.OrganizationMemberRepository
            .GetMemberByIdInOrg(membershipId: membershipId, orgId: orgId)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (membership is null)
            throw new NotFoundAppException("Organization membership");

        return _mapper.Map<OrganizationMemberDto>(membership);
    }

    public async Task AddOrganizationMemberAsync(Guid orgId, AddOrganizationMemberDto dto)
    {
        await _validationService.ValidateAsync<AddOrganizationMemberDto>(dto);

        var user = await _repositories.UserRepository
            .GetById(id: dto.UserId)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (user is null)
            throw new NotFoundAppException("User");

        var hasMembership = await _repositories.OrganizationMemberRepository
            .GetOrgMemberAsync(orgId: orgId, userId: dto.UserId)
            .AsNoTracking()
            .AnyAsync();

        if (hasMembership)
            throw new DuplicateEntryAppException("User is already a member of this organization");

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var memberEntity = _mapper.Map<OrganizationMember>(dto);

        memberEntity.OrgId = orgId;
        memberEntity.InvitedByUserId = authUserId;
        memberEntity.JoinedAt = DateTime.UtcNow;

        await _repositories.OrganizationMemberRepository.AddAsync(memberEntity);
        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateOrganizationMemberRoleAsync(Guid orgId, Guid membershipId, UpdateOrganizationMemberRoleDto dto)
    {
        await _validationService.ValidateAsync<UpdateOrganizationMemberRoleDto>(dto);

        var membership = await _repositories.OrganizationMemberRepository
            .GetByIdInOrg(id: membershipId, orgId: orgId)
            .FirstOrDefaultAsync();

        if (membership is null)
            throw new NotFoundAppException("Organization membership");

        if (membership.Role == dto.Role)
            return;

        if (membership.Role == OrganizationRoleEnum.OrgOwner)
            throw new InvalidDataAppException("The organization owner role can't change from here");

        membership.Role = dto.Role;

        await _repositories.UnitOfWork.SaveChangesAsync();

        TryInvalidatePermissions(membership.UserId, "Failed to invalidate permissions for user {UserId} after organization role change");
    }

    public async Task RemoveMemberAsync(Guid orgId, Guid membershipId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var membership = await _repositories.OrganizationMemberRepository
            .GetByIdInOrg(id: membershipId, orgId: orgId)
            .FirstOrDefaultAsync();

        if (membership is null)
            throw new NotFoundAppException("Organization membership");

        // Owners cannot be removed — they must transfer ownership first
        if (membership.Role == OrganizationRoleEnum.OrgOwner)
            throw new InvalidDataAppException("The organization owner cannot be removed. Transfer ownership first.");

        if (membership.UserId == authUserId)
            throw new InvalidDataAppException("You cannot remove yourself. Use the leave organization action instead.");

        await ExecuteCascadeRemovalAsync(membership, orgId: orgId, actorId: authUserId);

        TryInvalidatePermissions(membership.UserId, "Error while invalidating permissions for user {UserId}");
    }

    public async Task TransferOrganizationOwnershipAsync(Guid orgId, Guid membershipId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var authUserMembership = await _repositories.OrganizationMemberRepository
            .GetOrgMemberAsync(userId: authUserId, orgId: orgId)
            .FirstOrDefaultAsync();

        if (authUserMembership is null)
            throw new NotFoundAppException("Organization membership");

        if (authUserMembership.Role != OrganizationRoleEnum.OrgOwner)
            throw new InvalidDataAppException("You must be the organization owner to transfer ownership");

        if (authUserMembership.Id == membershipId)
            throw new InvalidDataAppException("Cannot transfer ownership to yourself");

        var requestedMembership = await _repositories.OrganizationMemberRepository
            .GetByIdInOrg(id: membershipId, orgId: orgId)
            .FirstOrDefaultAsync();

        if (requestedMembership is null)
            throw new NotFoundAppException("Organization membership");

        if (requestedMembership.Role == OrganizationRoleEnum.OrgOwner)
            throw new InvalidDataAppException("Target user is already the owner");

        await using var transaction = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            requestedMembership.Role = OrganizationRoleEnum.OrgOwner;
            authUserMembership.Role = OrganizationRoleEnum.OrgAdmin;

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();

            await transaction.CommitAsync();

            // Only write audit logs AFTER commit succeeds.
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        TryInvalidatePermissions(authUserMembership.UserId, "Error while invalidating permissions for user {UserId}");
        TryInvalidatePermissions(requestedMembership.UserId, "Error while invalidating permissions for user {UserId}");
    }

    public async Task LeaveOrganizationAsync(Guid orgId)
    {
        // TODO: Remove Team, Channel membership too
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var membership = await _repositories.OrganizationMemberRepository
            .GetOrgMemberAsync(userId: authUserId, orgId: orgId)
            .FirstOrDefaultAsync();

        if (membership is null)
            throw new NotFoundAppException("Organization membership");

        // Owners cannot be removed — they must transfer ownership first
        if (membership.Role == OrganizationRoleEnum.OrgOwner)
            throw new InvalidDataAppException("You cannot leave the organization as the owner. Transfer ownership first.");

        await ExecuteCascadeRemovalAsync(membership, orgId: orgId, actorId: authUserId);

        TryInvalidatePermissions(membership.UserId, "Error while invalidating permissions for user {UserId}");
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
    //  SHARED CASCADE REMOVAL LOGIC
    //
    //  Full execution order:
    //
    //  Phase 1 — Resolve sole-admin teams (one query)
    //    For teams where target is only admin:
    //      a) If other members exist → auto-promote next senior (ExecuteUpdate)
    //      b) If no members → archive team + archive all its channels (ExecuteUpdate x2)
    //         + bulk delete all channel members in those channels (ExecuteUpdate)
    //
    //  Phase 2 — Resolve sole-moderator private channels (one query)
    //    For private channels where target is only moderator:
    //      a) If other channel members exist → auto-promote next senior (ExecuteUpdate)
    //      b) If no other channel members    → archive channel (ExecuteUpdate)
    //         NOTE: team may still be alive — team membership and private channel
    //         membership are independent. A TeamMember is not a ChannelMember of a
    //         private channel unless explicitly added.
    //
    //  Phase 3 — Bulk remove all team + channel memberships (ExecuteUpdate x2)
    //
    //  Phase 4 — Soft-delete org membership row (tracked, SaveChangesWithoutAuditAsync)
    //
    //  Phase 5 — Queue manual audit entries for all bulk operations
    //
    //  Phase 6 — Commit + flush audit logs
    //
    //  All phases run inside a single transaction. No entity rows are loaded
    //  into memory for bulk operations — only the tiny resolution result sets.
    // ══════════════════════════════════════════════════════════════════════════

    private async Task ExecuteCascadeRemovalAsync(OrganizationMember membership, Guid orgId, Guid actorId)
    {
        var targetUserId = membership.UserId;
        var now = DateTime.UtcNow;

        // Audit summary accumulators
        var teamPromotions = new List<(Guid TeamId, string TeamName, Guid PromotedUserId)>();
        var teamsAutoArchived = new List<(Guid TeamId, string TeamName)>();
        var channelPromotions = new List<(Guid ChannelId, string ChannelName, Guid PromotedUserId)>();
        var channelsAutoArchived = new List<(Guid ChannelId, string ChannelName)>();

        int cascadeTeamMembersRemoved = 0;
        int cascadeChannelsArchived = 0;
        int cascadeChannelMembersRemoved = 0;
        int teamMembershipsRemoved = 0;
        int channelMembershipsRemoved = 0;

        await using var transaction = await _repositories.UnitOfWork.BeginTransactionAsync();
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
            var freshMembership = await _repositories.OrganizationMemberRepository
                .GetByIdWithUpdateLockAsync(membership.Id);

            if (freshMembership is null || freshMembership.IsDeleted)
            {
                // Row was already deleted by a concurrent request — nothing to do.
                await transaction.RollbackAsync();
                return;
            }

            // ── Phase 1: Sole-admin team resolution ──────────────────────────────
            // Only relevant for OrgMember / OrgGuest.
            // OrgAdmin / OrgOwner have wildcard org-level permissions — their teams
            // are never truly orphaned regardless of their TeamMember rows.
            if (freshMembership.Role is OrganizationRoleEnum.OrgMember or OrganizationRoleEnum.OrgGuest)
            {
                var soleAdminTeams = await _repositories.TeamMemberRepository
                    .GetSoleAdminTeamsWithNextSeniorMemberAsync(targetUserId, orgId);

                foreach (var team in soleAdminTeams)
                {
                    // ── Promotion with fallback ───────────────────────────────────
                    // The candidate (NextSeniorMembershipId) may have been concurrently
                    // deleted between the resolution query and this call.
                    // PromoteTeamMemberAsync guards with !IsDeleted and returns rows affected.
                    // If 0 → candidate is gone → fall through to team cleanup.
                    bool promoted = false;

                    if (team.NextSeniorMembershipId is not null)
                    {
                        int rows = await _repositories.CascadeCleanupRepository
                            .PromoteTeamMemberAsync(membershipId: team.NextSeniorMembershipId.Value, actorId, now);

                        if (rows > 0)
                        {
                            promoted = true;
                            teamPromotions.Add((team.TeamId, team.TeamName, team.NextSeniorMemberId!.Value));
                        }
                        // rows == 0: candidate was concurrently deleted → fall through to cleanup
                    }

                    if (!promoted)
                    {
                        // No other members (or candidate vanished) — full team cascade:
                        //   CleanupTeamAsync: soft-deletes TeamMembers, archives Channels,
                        //                     soft-deletes ChannelMembers (pure SQL JOIN, no id list)
                        //   ArchiveTeamAsync: marks the team itself archived
                        var result = await _repositories.CascadeCleanupRepository
                            .CleanupTeamAsync(team.TeamId, actorId, now);

                        await _repositories.CascadeCleanupRepository
                            .ArchiveTeamAsync(team.TeamId, actorId, now);

                        cascadeTeamMembersRemoved += result.TeamMembersRemoved;
                        cascadeChannelsArchived += result.ChannelsArchived;
                        cascadeChannelMembersRemoved += result.ChannelMembersRemoved;
                        teamsAutoArchived.Add((team.TeamId, team.TeamName));
                    }
                }
            }

            // ── Phase 2: Sole-moderator private channel resolution ───────────────
            // Only private channels have ChannelMember rows.
            // Public channels use team membership for implicit access — nothing to do.
            //
            // NextSeniorMemberId null means no other ChannelMember row exists in this
            // channel. This IS reachable even when the team survived Phase 1: a private
            // channel only contains members who were explicitly added, so a TeamMember
            // with no ChannelMember row in a private channel is invisible to it.
            var soleModeratorChannels = await _repositories.ChannelMemberRepository
                .GetSoleModeratorPrivateChannelsWithNextSeniorMemberAsync(targetUserId, orgId);

            foreach (var channel in soleModeratorChannels)
            {
                // ── Promotion with fallback ───────────────────────────────────────
                // Same pattern as Phase 1: candidate may be concurrently deleted.
                bool promoted = false;

                if (channel.NextSeniorMembershipId is not null)
                {
                    int rows = await _repositories.CascadeCleanupRepository
                        .PromoteChannelMemberAsync(membershipId: channel.NextSeniorMembershipId.Value, actorId, now);

                    if (rows > 0)
                    {
                        promoted = true;
                        channelPromotions.Add((channel.ChannelId, channel.ChannelName, channel.NextSeniorMemberId!.Value));
                    }
                    // rows == 0: candidate was concurrently deleted → fall through to cleanup
                }

                if (!promoted)
                {
                    // Channel becomes empty (or candidate vanished).
                    // CleanupChannelAsync archives the channel and deletes its members
                    // as an atomic consecutive pair inside the same transaction.
                    var result = await _repositories.CascadeCleanupRepository
                        .CleanupChannelAsync(channel.ChannelId, actorId, now);

                    channelsAutoArchived.Add((channel.ChannelId, channel.ChannelName));
                    cascadeChannelMembersRemoved += result.ChannelMembersRemoved;
                }
            }

            // ── Phase 3: Bulk remove remaining memberships ───────────────────────
            // MUST run after Phases 1 and 2. Rows already soft-deleted by cleanup
            // phases are skipped by the WHERE !IsDeleted predicate automatically.
            teamMembershipsRemoved = await _repositories.CascadeCleanupRepository
                .BulkRemoveUserTeamMembershipsAsync(targetUserId, orgId, actorId, now);

            channelMembershipsRemoved = await _repositories.CascadeCleanupRepository
                .BulkRemoveUserChannelMembershipsAsync(targetUserId, orgId, actorId, now);

            // ── Phase 4: Soft-delete the org membership row (tracked entity) ─────
            freshMembership.IsDeleted = true;
            freshMembership.DeletedAt = now;
            freshMembership.DeletedBy = actorId;

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();

            // ── Phase 5: Queue manual audit entries ───────────────────────────────
            if (teamMembershipsRemoved > 0)
                _repositories.UnitOfWork.QueueManualAuditLog(new AuditLogRequest(
                    tableName: "TeamMembers",
                    recordId: null,
                    before: new { TargetUserId = targetUserId, OrgId = orgId },
                    after: new { IsDeleted = true, AffectedRows = teamMembershipsRemoved },
                    changeState: EntityState.Modified));

            if (channelMembershipsRemoved > 0)
                _repositories.UnitOfWork.QueueManualAuditLog(new AuditLogRequest(
                    tableName: "ChannelMembers",
                    recordId: null,
                    before: new { TargetUserId = targetUserId, OrgId = orgId },
                    after: new { IsDeleted = true, AffectedRows = channelMembershipsRemoved },
                    changeState: EntityState.Modified));

            if (teamPromotions.Count > 0)
                _repositories.UnitOfWork.QueueManualAuditLog(new AuditLogRequest(
                    tableName: "TeamMembers",
                    recordId: null,
                    before: new { Reason = "SoleAdminRemoved" },
                    after: new { AutoPromotions = teamPromotions },
                    changeState: EntityState.Modified));

            if (teamsAutoArchived.Count > 0)
                _repositories.UnitOfWork.QueueManualAuditLog(new AuditLogRequest(
                    tableName: "Teams",
                    recordId: null,
                    before: new { Reason = "TeamBecameEmptyOrCandidateVanished" },
                    after: new
                    {
                        AutoArchived = teamsAutoArchived,
                        CascadeTeamMembers = cascadeTeamMembersRemoved,
                        CascadeChannelsArchived = cascadeChannelsArchived,
                        CascadeChannelMembers = cascadeChannelMembersRemoved
                    },
                    changeState: EntityState.Modified));

            if (channelPromotions.Count > 0)
                _repositories.UnitOfWork.QueueManualAuditLog(new AuditLogRequest(
                    tableName: "ChannelMembers",
                    recordId: null,
                    before: new { Reason = "SoleModeratorRemoved" },
                    after: new { AutoPromotions = channelPromotions },
                    changeState: EntityState.Modified));

            if (channelsAutoArchived.Count > 0)
                _repositories.UnitOfWork.QueueManualAuditLog(new AuditLogRequest(
                    tableName: "Channels",
                    recordId: null,
                    before: new { Reason = "ChannelBecameEmptyOrCandidateVanished" },
                    after: new { AutoArchived = channelsAutoArchived },
                    changeState: EntityState.Modified));

            // ── Phase 6: Commit + flush ───────────────────────────────────────────
            await transaction.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        _logger.LogInformation(
            "User {TargetUserId} removed from org {OrgId} by actor {ActorId}. " +
            "Bulk removed: {TeamMemberships} team, {ChannelMemberships} channel membership(s). " +
            "Auto-promoted: {TeamPromotions} team admin, {ChannelPromotions} channel moderator. " +
            "Auto-archived: {TeamsArchived} team(s) [{CascadeTeamMembers} team members, " +
            "{CascadeChannels} channels, {CascadeChannelMembers} channel members cascaded], " +
            "{ChannelsArchived} private channel(s).",
            targetUserId, orgId, actorId,
            teamMembershipsRemoved, channelMembershipsRemoved,
            teamPromotions.Count, channelPromotions.Count,
            teamsAutoArchived.Count, cascadeTeamMembersRemoved,
            cascadeChannelsArchived, cascadeChannelMembersRemoved,
            channelsAutoArchived.Count);

        TryInvalidatePermissions(targetUserId, "Failed to invalidate permissions for user {UserId} after org removal");
    }

    #endregion
}
