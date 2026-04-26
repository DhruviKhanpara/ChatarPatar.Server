using AutoMapper;
using ChatarPatar.Application.DTOs.TeamMember;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.HttpUserDetails;
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
                    .Any(m => m.UserId == dto.UserId),
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
                throw new AppException("Only a team admin or org admin can add a member with the TeamAdmin role.");
        }

        var memberEntity = _mapper.Map<TeamMember>(dto);

        memberEntity.TeamId = teamId;
        memberEntity.InvitedByUserId = authUserId;
        memberEntity.JoinedAt = DateTime.UtcNow;

        await _repositories.TeamMemberRepository.AddAsync(memberEntity);
        await _repositories.UnitOfWork.SaveChangesAsync();
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
                AdminCount = t.TeamMembers.Count(m => m.Role == TeamRoleEnum.TeamAdmin)
            })
            .FirstOrDefaultAsync();

        if (context is null)
            throw new NotFoundAppException("Team");

        if (context.IsArchived)
            throw new InvalidDataAppException("Cannot update members of an archived team.");

        if (context.Membership is null)
            throw new NotFoundAppException("Team membership");
        
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
        // TODO: Remove Channel membership too
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var context = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .AsNoTracking()
            .Select(t => new
            {
                t.IsArchived,
                Membership = t.TeamMembers.Where(m => m.Id == membershipId).Select(m => new { m.UserId, m.Role }).FirstOrDefault(),
                AdminCount = t.TeamMembers.Count(m => m.Role == TeamRoleEnum.TeamAdmin)
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
            .FirstOrDefaultAsync();

        if (membership is null)
            throw new NotFoundAppException("Team membership");

        membership.IsDeleted = true;

        await _repositories.UnitOfWork.SaveChangesAsync();

        TryInvalidatePermissions(membership.UserId, "Failed to invalidate permissions for user {UserId} after team member removal");
    }

    public async Task LeaveTeamAsync(Guid orgId, Guid teamId)
    {
        // TODO: Channel membership too
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var context = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .AsNoTracking()
            .Select(t => new
            {
                t.IsArchived,
                Membership = t.TeamMembers.Where(m => m.UserId == authUserId).Select(m => new { m.Id, m.Role }).FirstOrDefault(),
                AdminCount = t.TeamMembers.Count(m => m.Role == TeamRoleEnum.TeamAdmin)
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
            .FirstOrDefaultAsync();

        if (membership is null)
            throw new NotFoundAppException("Team membership");

        membership.IsDeleted = true;

        await _repositories.UnitOfWork.SaveChangesAsync();

        TryInvalidatePermissions(authUserId,"Failed to invalidate permissions for user {UserId} after leaving team");
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
