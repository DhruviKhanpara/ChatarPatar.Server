using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.ChannelMember;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
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

internal class ChannelMemberService : IChannelMemberService
{
    private readonly IRepositoryManager _repositories;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<ChannelMemberService> _logger;

    public ChannelMemberService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor, IPermissionService permissionService, ILogger<ChannelMemberService> logger)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
        _logger = logger;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<PagedResult<ChannelMemberDto>> GetMembersAsync(Guid orgId, Guid teamId, Guid channelId, PaginationParams paginationParams)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var callerContext = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .AsNoTracking()
            .Select(t => new
            {
                OrgRole = t.Organization.OrganizationMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (OrganizationRoleEnum?)m.Role)
                    .FirstOrDefault(),
                TeamRole = t.TeamMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (TeamRoleEnum?)m.Role)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (callerContext == null || callerContext.OrgRole == null)
            throw new NotFoundAppException("Channel");

        var callerHasElevatedAccess =
            callerContext.OrgRole is OrganizationRoleEnum.OrgOwner or OrganizationRoleEnum.OrgAdmin
            || callerContext.TeamRole is TeamRoleEnum.TeamAdmin;

        var channel = await _repositories.ChannelRepository
            .GetByIdInTeam(channelId, teamId, orgId)
            .AsNoTracking()
            .Select(c => new { c.IsPrivate })
            .FirstOrDefaultAsync();

        if (channel is null)
            throw new NotFoundAppException("Channel");

        IQueryable<ChannelMemberDto> query;

        // Private channels require explicit membership unless caller is an org admin
        if (channel.IsPrivate)
        {
            // Non-org-admins must be explicit channel members
            if (!callerHasElevatedAccess)
            {
                var isMember = await _repositories.ChannelMemberRepository
                    .GetChannelMemberAsync(authUserId, channelId)
                    .AnyAsync();

                if (!isMember)
                    throw new NotFoundAppException("Channel");
            }

            query = _repositories.ChannelMemberRepository
                .FindByCondition(m => m.ChannelId == channelId)
                .AsNoTracking()
                .ProjectTo<ChannelMemberDto>(_mapper.ConfigurationProvider);
        }
        else
        {
            // Public channel membership is inherited from the team
            var teamMemberQuery = _repositories.TeamMemberRepository
                .FindByCondition(m => m.TeamId == teamId);

            // Non-org-admins must belong to the team
            if (!callerHasElevatedAccess)
            {
                var isTeamMember = await teamMemberQuery
                    .AnyAsync(m => m.UserId == authUserId);

                if (!isTeamMember)
                    throw new NotFoundAppException("Channel");
            }

            query = teamMemberQuery
                .AsNoTracking()
                .ProjectTo<ChannelMemberDto>(_mapper.ConfigurationProvider);
        }

        var totalCount = await query.CountAsync();

        var members = await query
            .PaginateOffset(paginationParams.PageSize, paginationParams.PageNumber)
            .ToListAsync();

        return new PagedResult<ChannelMemberDto>(members, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
    }

    public async Task AddChannelMemberAsync(Guid orgId, Guid teamId, Guid channelId, AddChannelMemberDto dto)
    {
        await _validationService.ValidateAsync<AddChannelMemberDto>(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var context = await _repositories.ChannelRepository
            .GetByIdInTeam(channelId, teamId, orgId)
            .AsNoTracking()
            .Select(c => new
            {
                c.IsArchived,
                c.IsPrivate,
                TargetIsTeamMember = c.Team.TeamMembers
                    .Any(m => m.UserId == dto.UserId && !m.IsDeleted),
                AlreadyChannelMember = c.ChannelMembers
                    .Any(m => m.UserId == dto.UserId && !m.IsDeleted),
                CallerTeamRole = c.Team.TeamMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (TeamRoleEnum?)m.Role)
                    .FirstOrDefault(),
                CallerOrgRole = c.Team.Organization.OrganizationMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (OrganizationRoleEnum?)m.Role)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (context is null)
            throw new NotFoundAppException("Channel");

        if (context.IsArchived)
            throw new InvalidDataAppException("Cannot add members to an archived channel.");

        if (!context.IsPrivate)
            throw new InvalidDataAppException("Public channels automatically include all team members.");

        if (!context.TargetIsTeamMember)
            throw new InvalidDataAppException("User must be a team member before being added to a channel.");

        if (context.AlreadyChannelMember)
            throw new DuplicateEntryAppException("User is already a member of this channel.");

        // Only admins can assign the ChannelModerator role
        if (dto.Role == ChannelRoleEnum.ChannelModerator)
        {
            var callerIsAdmin =
                context.CallerOrgRole is OrganizationRoleEnum.OrgOwner or OrganizationRoleEnum.OrgAdmin
                || context.CallerTeamRole is TeamRoleEnum.TeamAdmin;

            if (!callerIsAdmin)
                throw new ForbiddenAppException("Only a team admin or org admin can assign the ChannelModerator role.");
        }

        var memberEntity = _mapper.Map<ChannelMember>(dto);
        memberEntity.ChannelId = channelId;
        memberEntity.AddedByUserId = authUserId;
        memberEntity.JoinedAt = DateTime.UtcNow;
        memberEntity.IsMuted = false;

        await _repositories.ChannelMemberRepository.AddAsync(memberEntity);
        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateChannelMemberRoleAsync(Guid orgId, Guid teamId, Guid channelId, Guid membershipId, UpdateChannelMemberRoleDto dto)
    {
        await _validationService.ValidateAsync<UpdateChannelMemberRoleDto>(dto);

        var context = await _repositories.ChannelRepository
            .GetByIdInTeam(channelId, teamId, orgId)
            .AsNoTracking()
            .Select(c => new
            {
                c.IsArchived,
                c.IsPrivate,
                Membership = c.ChannelMembers.Where(m => m.Id == membershipId).Select(m => new { m.UserId, m.Role }).FirstOrDefault(),
                ModeratorCount = c.ChannelMembers.Count(m => m.Role == ChannelRoleEnum.ChannelModerator)
            })
            .FirstOrDefaultAsync();

        if (context is null)
            throw new NotFoundAppException("Channel");

        if (context.IsArchived)
            throw new InvalidDataAppException("Cannot update member roles in an archived channel.");

        if (!context.IsPrivate)
            throw new InvalidDataAppException("Public channels do not support custom member roles.");

        if (context.Membership is null)
            throw new NotFoundAppException("Channel membership");

        if (context.Membership.Role == dto.Role)
            return;

        if (context.Membership.Role == ChannelRoleEnum.ChannelModerator && dto.Role != ChannelRoleEnum.ChannelModerator && context.ModeratorCount <= 1)
            throw new InvalidDataAppException("This user is the only moderator of the channel. Assign another moderator before changing their role.");

        var membership = await _repositories.ChannelMemberRepository
            .GetByIdInChannel(membershipId, channelId)
            .FirstOrDefaultAsync();

        if (membership is null)
            throw new NotFoundAppException("Channel membership");

        membership.Role = dto.Role;

        await _repositories.UnitOfWork.SaveChangesAsync();

        TryInvalidatePermissions(context.Membership.UserId, "Failed to invalidate permissions for user {UserId} after channel role change");
    }

    public async Task RemoveChannelMemberAsync(Guid orgId, Guid teamId, Guid channelId, Guid membershipId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var context = await _repositories.ChannelRepository
            .GetByIdInTeam(channelId, teamId, orgId)
            .AsNoTracking()
            .Select(c => new
            {
                c.IsArchived,
                c.IsPrivate,
                Membership = c.ChannelMembers.Where(m => m.Id == membershipId).Select(m => new { m.UserId, m.Role }).FirstOrDefault(),
                ModeratorCount = c.ChannelMembers.Count(m => m.Role == ChannelRoleEnum.ChannelModerator)
            })
            .FirstOrDefaultAsync();

        if (context is null)
            throw new NotFoundAppException("Channel");

        if (context.IsArchived)
            throw new InvalidDataAppException("Cannot remove members from an archived channel.");

        if (!context.IsPrivate)
            throw new InvalidDataAppException("Public channels do not support removing individual members.");

        if (context.Membership is null)
            throw new NotFoundAppException("Channel membership");

        if (context.Membership.UserId == authUserId)
            throw new InvalidDataAppException("You cannot remove yourself. Use the leave channel action instead.");

        if (context.Membership.Role == ChannelRoleEnum.ChannelModerator && context.ModeratorCount <= 1)
            throw new InvalidDataAppException("Cannot remove the only moderator of the channel. Assign another moderator first.");

        var membership = await _repositories.ChannelMemberRepository
            .GetByIdInChannel(membershipId, channelId)
            .FirstOrDefaultAsync();

        if (membership is null)
            throw new NotFoundAppException("Channel membership");

        membership.IsDeleted = true;

        await _repositories.UnitOfWork.SaveChangesAsync();

        TryInvalidatePermissions(membership.UserId, "Failed to invalidate permissions for user {UserId} after channel member removal");
    }

    public async Task LeaveChannelAsync(Guid orgId, Guid teamId, Guid channelId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var context = await _repositories.ChannelRepository
            .GetByIdInTeam(channelId, teamId, orgId)
            .AsNoTracking()
            .Select(c => new
            {
                c.IsArchived,
                c.IsPrivate,
                Membership = c.ChannelMembers.Where(m => m.UserId == authUserId).Select(m => new { m.Id, m.Role }).FirstOrDefault(),
                ModeratorCount = c.ChannelMembers.Count(m => m.Role == ChannelRoleEnum.ChannelModerator)
            })
            .FirstOrDefaultAsync();

        if (context is null)
            throw new NotFoundAppException("Channel");

        if (context.IsArchived)
            throw new InvalidDataAppException("Cannot leave from an archived channel.");

        if (!context.IsPrivate)
            throw new InvalidDataAppException("Public channels do not support leaving individually.");

        if (context.Membership is null)
            throw new NotFoundAppException("Channel membership");

        if (context.Membership.Role == ChannelRoleEnum.ChannelModerator && context.ModeratorCount <= 1)
            throw new InvalidDataAppException("Cannot leave as only moderator of the channel. Assign another moderator first.");

        var membership = await _repositories.ChannelMemberRepository
            .GetByIdInChannel(context.Membership.Id, channelId)
            .FirstOrDefaultAsync();

        if (membership is null)
            throw new NotFoundAppException("Channel membership");

        membership.IsDeleted = true;

        await _repositories.UnitOfWork.SaveChangesAsync();

        TryInvalidatePermissions(authUserId, "Failed to invalidate permissions for user {UserId} after leaving channel");
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

    #endregion
}
