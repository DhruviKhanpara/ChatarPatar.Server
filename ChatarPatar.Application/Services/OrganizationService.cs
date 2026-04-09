using AutoMapper;
using ChatarPatar.Application.DTOs.Organization;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;

namespace ChatarPatar.Application.Services;

internal class OrganizationService : IOrganizationService
{
    private readonly IRepositoryManager _repositories;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrganizationService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
    }
    private HttpContext? _httpContext => _httpContextAccessor.HttpContext;

    public async Task CreateOrganizationAsync(CreateOrganizationDto dto)
    {
        await _validationService.ValidateAsync<CreateOrganizationDto>(dto);

        var authUserId = Guid.Parse(_httpContext!.GetUserId());
        var slug = dto.Slug.Trim();

        var slugExists = await _repositories.OrganizationRepository
            .AnyAsync(o => o.Slug == slug);

        if (slugExists)
            throw new DuplicateEntryAppException("Organization slug is already taken");

        var orgEntity = _mapper.Map<Organization>(dto);

        orgEntity.OrganizationMembers.Add(new OrganizationMember
        {
            UserId = authUserId,
            Role = OrganizationRoleEnum.OrgOwner,
            JoinedAt = DateTime.UtcNow
        });

        await _repositories.OrganizationRepository.AddAsync(orgEntity);
        await _repositories.UnitOfWork.SaveChangesAsync();
    }
}
