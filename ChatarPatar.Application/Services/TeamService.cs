using AutoMapper;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Team;
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

internal class TeamService : ITeamService
{
    private readonly IRepositoryManager _repositories;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IExternalServiceManager _externalServiceManager;

    public TeamService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor, IExternalServiceManager externalServiceManager)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _externalServiceManager = externalServiceManager;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task CreateTeamAsync(Guid orgId, CreateTeamDto dto)
    {
        await _validationService.ValidateAsync<CreateTeamDto>(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        // Verify org exists and caller is a member
        var orgExists = await _repositories.OrganizationRepository
            .GetById(id: orgId)
            .AnyAsync();

        if (!orgExists)
            throw new NotFoundAppException("Organization");

        var nameExists = await _repositories.TeamRepository
            .NameExistsInOrgAsync(orgId, dto.Name);

        if (nameExists)
            throw new DuplicateEntryAppException("A team with this name already exists in the organization");

        var teamEntity = _mapper.Map<Team>(dto);
        teamEntity.OrgId = orgId;

        // Creator is automatically added as TeamAdmin
        teamEntity.TeamMembers.Add(new TeamMember
        {
            UserId = authUserId,
            Role = TeamRoleEnum.TeamAdmin,
            JoinedAt = DateTime.UtcNow,
            InvitedByUserId = null
        });

        await _repositories.TeamRepository.AddAsync(teamEntity);
        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task UpdateTeamIconAsync(Guid orgId, Guid teamId, ImageUploadDto dto)
    {
        await _validationService.ValidateAsync<ImageUploadDto>(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var fileType = dto.File.ValidateFile(FileUsageContextEnum.Team_Icon);

        var team = await _repositories.TeamRepository.GetByIdInOrg(teamId, orgId).FirstOrDefaultAsync();

        if (team is null)
            throw new NotFoundAppException("Team");

        if (team.IconFileId != null)
        {
            var existingIcon = await _repositories.FileRepository.GetByIdAsync((Guid)team.IconFileId).FirstOrDefaultAsync();

            if (existingIcon != null)
                existingIcon.IsDeleted = true;
        }

        var publicId = CloudinaryPublicId.TeamIcon(team.Id);

        var uploadResult = await _externalServiceManager.CloudinaryService.UploadProfileAssetAsync(dto.File, CloudinaryPath.Organization(orgId).Team(teamId).Profile(), publicId);

        team.IconFile = new FileEntity
        {
            UploadedByUserId = authUserId,
            TeamId = teamId,
            UsageContext = FileUsageContextEnum.Team_Icon,

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

    public async Task UpdateTeamAsync(Guid orgId, Guid teamId, UpdateTeamDto dto)
    {
        await _validationService.ValidateAsync<UpdateTeamDto>(dto);

        var team = await _repositories.TeamRepository.GetByIdInOrg(teamId, orgId).FirstOrDefaultAsync();

        if (team is null)
            throw new NotFoundAppException("Team");

        if (team.IsArchived)
            throw new InvalidDataAppException("Archived teams cannot be modified. Unarchive the team first.");

        if (!string.Equals(team.Name, dto.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var nameExists = await _repositories.TeamRepository
                .NameExistsInOrgAsync(orgId, dto.Name, excludeTeamId: teamId);

            if (nameExists)
                throw new DuplicateEntryAppException("A team with this name already exists in the organization");
        }

        _mapper.Map<UpdateTeamDto, Team>(dto, team);

        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task RemoveTeamIconAsync(Guid orgId, Guid teamId)
    {
        var team = await _repositories.TeamRepository.GetByIdInOrg(teamId, orgId).FirstOrDefaultAsync();

        if (team is null)
            throw new NotFoundAppException("Team");

        if (team.IconFileId == null)
            return;

        var existingIcon = await _repositories.FileRepository.GetByIdAsync((Guid)team.IconFileId).FirstOrDefaultAsync();

        if (existingIcon != null)
        {
            existingIcon.IsDeleted = true;

            await _externalServiceManager.CloudinaryService.DeleteFileAsync(existingIcon.PublicId);
        }

        team.IconFileId = null;

        await _repositories.UnitOfWork.SaveChangesAsync();
    }

}
