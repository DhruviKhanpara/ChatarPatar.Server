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
            .GetMembersQuery(orgId, queryParams.Search, queryParams.Role);

        var totalCount = await baseQuery.CountAsync();

        var items = await baseQuery
            .PaginateOffset(queryParams.PageSize, queryParams.PageNumber)
            .AsNoTracking()
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

        if (membership.Role == OrganizationRoleEnum.OrgOwner)
            throw new InvalidDataAppException("The organization owner role can't change from here");

        membership.Role = dto.Role;

        await _repositories.UnitOfWork.SaveChangesAsync();

        TryInvalidatePermissions(membership.UserId, "Failed to invalidate permissions for user {UserId} after organization role change");
    }

    public async Task RemoveMemberAsync(Guid orgId, Guid membershipId)
    {
        // TODO: Remove Team, Channel membership too
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

        membership.IsDeleted = true;

        await _repositories.UnitOfWork.SaveChangesAsync();

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

        membership.IsDeleted = true;

        await _repositories.UnitOfWork.SaveChangesAsync();

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

    #endregion
}
