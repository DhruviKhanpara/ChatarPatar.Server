using AutoMapper;
using ChatarPatar.Application.DTOs.OrganizationMember;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.HttpUserDetails;
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

    public OrganizationMemberService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
    }
    private HttpContext? _httpContext => _httpContextAccessor.HttpContext;

    public async Task AddOrganizationMemberAsync(Guid orgId, AddOrganizationMemberDto dto)
    {
        await _validationService.ValidateAsync<AddOrganizationMemberDto>(dto);

        var user = await _repositories.UserRepository.GetById(id: dto.UserId).AsNoTracking().FirstOrDefaultAsync();

        if (user is null)
            throw new NotFoundAppException("User");

        var hasMembership = await _repositories.OrganizationMemberRepository.GetOrgMemberAsync(orgId: orgId, userId: dto.UserId).AsNoTracking().AnyAsync();

        if (hasMembership)
            throw new DuplicateEntryAppException("User is already a member of this organization");

        var authUserId = Guid.Parse(_httpContext!.GetUserId());

        var memberEntity = _mapper.Map<OrganizationMember>(dto);

        memberEntity.OrgId = orgId;
        memberEntity.InvitedByUserId = authUserId;
        memberEntity.JoinedAt = DateTime.UtcNow;

        await _repositories.OrganizationMemberRepository.AddAsync(memberEntity);
        await _repositories.UnitOfWork.SaveChangesAsync();
    }
}
