using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.OrganizationMember;
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

namespace ChatarPatar.Application.Services;

internal class OrganizationMemberService : IOrganizationMemberService
{
    private readonly IRepositoryManager _repositories;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;

    public OrganizationMemberService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor, IPermissionService permissionService)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
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
            .GetMembersQuery(orgId, queryParams.Search, queryParams.Role);

        var totalCount = await baseQuery.CountAsync();

        var items = await baseQuery
            .PaginateOffset(queryParams.PageSize, queryParams.PageNumber)
            .AsNoTracking()
            .ProjectTo<OrganizationMemberDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<OrganizationMemberDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
    }

    public async Task<OrganizationMemberDto> GetMemberAsync(Guid orgId, Guid membershipId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        // Caller must be an active member of the org
        var isMember = await _repositories.OrganizationMemberRepository
            .GetOrgMemberAsync(userId: authUserId, orgId: orgId)
            .AnyAsync();

        if (!isMember)
            throw new NotFoundAppException("Organization");

        var membership = await _repositories.OrganizationMemberRepository
            .GetMemberById(membershipId)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (membership is null || membership.OrgId != orgId)
            throw new NotFoundAppException("Organization membership");

        return _mapper.Map<OrganizationMemberDto>(membership);
    }

    public async Task AddOrganizationMemberAsync(Guid orgId, AddOrganizationMemberDto dto)
    {
        await _validationService.ValidateAsync<AddOrganizationMemberDto>(dto);

        var user = await _repositories.UserRepository.GetById(id: dto.UserId).AsNoTracking().FirstOrDefaultAsync();

        if (user is null)
            throw new NotFoundAppException("User");

        var hasMembership = await _repositories.OrganizationMemberRepository.GetOrgMemberAsync(orgId: orgId, userId: dto.UserId).AsNoTracking().AnyAsync();

        if (hasMembership)
            throw new DuplicateEntryAppException("User is already a member of this organization");

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var memberEntity = _mapper.Map<OrganizationMember>(dto);

        memberEntity.OrgId = orgId;
        memberEntity.UserId = dto.UserId;
        memberEntity.InvitedByUserId = authUserId;
        memberEntity.JoinedAt = DateTime.UtcNow;

        await _repositories.OrganizationMemberRepository.AddAsync(memberEntity);
        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateOrganizationMemberRole(Guid orgId, Guid membershipId, UpdateOrganizationMemberRoleDto dto)
    {
        await _validationService.ValidateAsync<UpdateOrganizationMemberRoleDto>(dto);

        var membership = await _repositories.OrganizationMemberRepository.GetById(id: membershipId).FirstOrDefaultAsync();

        if (membership is null || membership.OrgId != orgId)
            throw new NotFoundAppException("Organization membership");

        if (membership.Role == OrganizationRoleEnum.OrgOwner)
            throw new InvalidDataAppException("The organization owner role can't change from here");

        membership.Role = dto.Role;

        await _repositories.UnitOfWork.SaveChangesAsync();
        _permissionService.InvalidateUserPermissions(membership.UserId);
    }

    public async Task RemoveMemberAsync(Guid orgId, Guid membershipId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var membership = await _repositories.OrganizationMemberRepository
            .GetById(membershipId)
            .FirstOrDefaultAsync();

        if (membership is null || membership.OrgId != orgId)
            throw new NotFoundAppException("Organization membership");

        // Owners cannot be removed — they must transfer ownership first
        if (membership.Role == OrganizationRoleEnum.OrgOwner)
            throw new InvalidDataAppException("The organization owner cannot be removed. Transfer ownership first.");

        if (membership.UserId == authUserId)
            throw new InvalidDataAppException("You cannot remove yourself. Use the leave organization action instead.");

        membership.IsDeleted = true;

        await _repositories.UnitOfWork.SaveChangesAsync();
        _permissionService.InvalidateUserPermissions(membership.UserId);
    }
}
