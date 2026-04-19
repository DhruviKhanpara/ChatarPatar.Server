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

        var isOrgMember = await _repositories.OrganizationMemberRepository
            .GetOrgMemberAsync(userId: dto.UserId, orgId: orgId)
            .AnyAsync();

        if (!isOrgMember)
            throw new InvalidDataAppException("User must be a member of the organization before being added to a team.");

        var teamExists = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .Include(x => x.TeamMembers.Where(x => x.UserId == dto.UserId))
            .FirstOrDefaultAsync();

        if (teamExists == null)
            throw new NotFoundAppException("Team");

        if (teamExists.TeamMembers.Any())
            throw new DuplicateEntryAppException("User is already a member of this team.");

        var authUserId = Guid.Parse(_httpContext.GetUserId());

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

        var membership = await _repositories.TeamMemberRepository
            .GetByIdInTeam(membershipId, teamId)
            .FirstOrDefaultAsync();

        if (membership is null)
            throw new NotFoundAppException("Team membership");

        var teamBelongsToOrg = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .Include(x => x.TeamMembers.Where(x => x.Role == TeamRoleEnum.TeamAdmin))
            .FirstOrDefaultAsync();

        if (teamBelongsToOrg == null)
            throw new NotFoundAppException("Team");

        if (membership.Role == TeamRoleEnum.TeamAdmin && dto.Role != TeamRoleEnum.TeamAdmin && !teamBelongsToOrg.TeamMembers.Any(x => x.Id != membershipId))
            throw new InvalidDataAppException("User is the only admin of this team. Assign another admin before change.");

        membership.Role = dto.Role;

        await _repositories.UnitOfWork.SaveChangesAsync();

        try
        {
            _permissionService.InvalidateUserPermissions(membership.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to invalidate permissions for user {UserId} after team role change",
                membership.UserId);
        }
    }
}
