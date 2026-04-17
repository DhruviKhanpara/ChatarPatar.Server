using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Organization;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.ExternalServiceContracts;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Application.Services;

internal class OrganizationService : IOrganizationService
{
    private readonly IRepositoryManager _repositories;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IExternalServiceManager _externalServiceManager;

    public OrganizationService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor, IExternalServiceManager externalServiceManager)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _externalServiceManager = externalServiceManager;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<OrganizationDto> GetOrganizationAsync(Guid orgId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        // Verify the caller is an active member of this org
        var isMember = await _repositories.OrganizationMemberRepository
            .GetOrgMemberAsync(userId: authUserId, orgId: orgId)
            .AnyAsync();

        if (!isMember)
            throw new NotFoundAppException("Organization");

        var org = await _repositories.OrganizationRepository
            .GetByIdWithLogo(orgId)
            .AsNoTracking()
            .ProjectTo<OrganizationDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        if (org == null)
            throw new NotFoundAppException("Organization");

        return org;
    }

    public async Task<List<OrganizationWithRoleDto>> GetMyOrganizationsAsync()
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        return await _repositories.OrganizationMemberRepository
            .GetMembershipsByUserId(authUserId)
            .AsNoTracking()
            .ProjectTo<OrganizationWithRoleDto>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task CreateOrganizationAsync(CreateOrganizationDto dto)
    {
        await _validationService.ValidateAsync<CreateOrganizationDto>(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());
        var slug = dto.Slug.Trim();

        var slugExists = await _repositories.OrganizationRepository.SlugExistsAsync(slug: slug);

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

    public async Task UpdateOrganizationLogoAsync(Guid orgId, ImageUploadDto dto)
    {
        await _validationService.ValidateAsync<ImageUploadDto>(dto);

        var userId = Guid.Parse(_httpContext.GetUserId());

        var fileType = dto.File.ValidateFile(FileUsageContextEnum.Org_Logo);

        var org = await _repositories.OrganizationRepository.GetById(orgId).FirstOrDefaultAsync();

        if (org == null)
            throw new NotFoundAppException("Organization");

        if (org.LogoFileId != null)
        {
            var orgLogoFile = await _repositories.FileRepository.GetByIdAsync((Guid)org.LogoFileId).FirstOrDefaultAsync();

            if (orgLogoFile != null)
                orgLogoFile.IsDeleted = true;
        }

        var publicId = CloudinaryPublicId.OrgLogo(org.Id);

        var uploadResult = await _externalServiceManager.CloudinaryService.UploadProfileAssetAsync(dto.File, CloudinaryPath.Organization(orgId).Profile(), publicId);

        org.LogoFile = new FileEntity()
        {
            UploadedByUserId = userId,
            OrgId = org.Id,
            UsageContext = FileUsageContextEnum.Org_Logo,

            PublicId = uploadResult.PublicId,
            Url = uploadResult.Url,
            ThumbnailUrl = uploadResult.ThumbnailUrl,

            SizeInBytes = dto.File.Length,
            OriginalName = dto.File.FileName,
            MimeType = dto.File.ContentType,
            FileType = fileType,
        };

        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateOrganizationAsync(Guid orgId, UpdateOrganizationDto dto)
    {
        await _validationService.ValidateAsync<UpdateOrganizationDto>(dto);

        var org = await _repositories.OrganizationRepository.GetById(orgId).FirstOrDefaultAsync();

        if (org == null)
            throw new NotFoundAppException("Organization");

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var isMember = await _repositories.OrganizationMemberRepository
            .GetOrgMemberAsync(userId: authUserId, orgId: orgId)
            .AnyAsync();

        if (!isMember)
            throw new NotFoundAppException("Organization membership");

        _mapper.Map<UpdateOrganizationDto, Organization>(dto, org);

        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task RemoveOrganizationLogoAsync(Guid orgId)
    {
        var org = await _repositories.OrganizationRepository.GetById(orgId).FirstOrDefaultAsync();

        if (org == null)
            throw new NotFoundAppException("Organization");

        if (org.LogoFileId == null)
            return;

        var existingLogo = await _repositories.FileRepository.GetByIdAsync((Guid)org.LogoFileId).FirstOrDefaultAsync();

        if (existingLogo != null)
        {
            existingLogo.IsDeleted = true;
            await _externalServiceManager.CloudinaryService.DeleteFileAsync(existingLogo.PublicId);
        }

        org.LogoFileId = null;

        await _repositories.UnitOfWork.SaveChangesAsync();
    }
}
