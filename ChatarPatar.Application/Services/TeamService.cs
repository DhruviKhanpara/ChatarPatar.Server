using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.Common.Extensions;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Team;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.ExternalServiceContracts;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Application.Services;

internal class TeamService : ITeamService
{
    private readonly IRepositoryManager _repositories;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IExternalServiceManager _externalServiceManager;
    private readonly ILogger<TeamService> _logger;

    public TeamService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor, IExternalServiceManager externalServiceManager, ILogger<TeamService> logger)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _externalServiceManager = externalServiceManager;
        _logger = logger;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<PagedResult<TeamWithRoleDto>> GetTeamsAsync(Guid orgId, TeamQueryParams queryParams)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var orgRole = await _repositories.OrganizationMemberRepository
            .GetOrgMemberAsync(userId: authUserId, orgId: orgId)
            .AsNoTracking()
            .Select(m => (OrganizationRoleEnum?)m.Role)
            .FirstOrDefaultAsync();

        if (orgRole is null)
            throw new NotFoundAppException("Organization");

        var callerIsOrgAdmin = orgRole is OrganizationRoleEnum.OrgOwner or OrganizationRoleEnum.OrgAdmin;

        var baseQuery = _repositories.TeamRepository.GetTeamsQuery(
            orgId,
            callerId: authUserId,
            callerIsOrgAdmin: callerIsOrgAdmin,
            search: queryParams.Search,
            isArchived: queryParams.IsArchived,
            includePrivate: queryParams.IncludePrivate);

        var totalCount = await baseQuery.CountAsync();
        var items = await baseQuery
            .PaginateOffset(queryParams.PageSize, queryParams.PageNumber)
            .AsNoTracking()
            .ProjectTo<TeamWithRoleDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        if (items.Count > 0)
        {
            var pageTeamIds = items.Select(t => t.Id).ToList();

            var myTeams = await _repositories.TeamMemberRepository
                .FindByCondition(m => pageTeamIds.Contains(m.TeamId) && m.UserId == authUserId)
                .AsNoTracking()
                .Select(m => new { m.TeamId, m.Role, m.IsMuted, m.JoinedAt })
                .ToListAsync();

            var myTeamsDict = myTeams.ToDictionary(x => x.TeamId);

            foreach (var item in items)
            {
                myTeamsDict.TryGetValue(item.Id, out var myMembership);
                item.Role = myMembership?.Role ?? null;
                item.IsMuted = myMembership?.IsMuted ?? null;
                item.JoinedAt = myMembership?.JoinedAt ?? null;
            }
        }

        return new PagedResult<TeamWithRoleDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
    }

    public async Task<TeamDto> GetTeamAsync(Guid orgId, Guid teamId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var orgRole = await _repositories.OrganizationMemberRepository
            .GetOrgMemberAsync(userId: authUserId, orgId: orgId)
            .AsNoTracking()
            .Select(m => (OrganizationRoleEnum?)m.Role)
            .FirstOrDefaultAsync();

        if (orgRole is null)
            throw new NotFoundAppException("Organization");

        var callerIsOrgAdmin = orgRole is OrganizationRoleEnum.OrgOwner or OrganizationRoleEnum.OrgAdmin;

        var query = _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .AsNoTracking();

        if (!callerIsOrgAdmin)
            query = query.Where(t =>
                !t.IsPrivate ||
                t.TeamMembers.Any(m => m.UserId == authUserId && !m.IsDeleted));

        var result = await query
            .ProjectTo<TeamDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        if (result is null)
            throw new NotFoundAppException("Team");

        return result;
    }

    public async Task CreateTeamAsync(Guid orgId, CreateTeamDto dto)
    {
        await _validationService.ValidateAsync<CreateTeamDto>(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        // Verify org exists and caller is a member
        var orgMemberExists = await _repositories.OrganizationMemberRepository
            .GetOrgMemberAsync(userId: authUserId, orgId: orgId)
            .AnyAsync();

        if (!orgMemberExists)
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

        team.EnsureEditable();

        FileUploadResult? uploadResult = null;
        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            if (team.IconFileId != null)
            {
                var existingIcon = await _repositories.FileRepository.GetByIdAsync((Guid)team.IconFileId).FirstOrDefaultAsync();

                if (existingIcon != null)
                    existingIcon.IsDeleted = true;
            }

            var publicId = CloudinaryPublicId.TeamIcon(team.Id);
            uploadResult = await _externalServiceManager.CloudinaryService.UploadProfileAssetAsync(dto.File, CloudinaryPath.Organization(orgId).Team(teamId).Profile(), publicId);

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

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();

            if (uploadResult != null)
            {
                try { await _externalServiceManager.CloudinaryService.DeleteFileAsync(uploadResult.PublicId); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete team icon from Cloudinary. PublicId: {PublicId}", uploadResult.PublicId);
                }
            }

            throw;
        }
    }

    public async Task UpdateTeamAsync(Guid orgId, Guid teamId, UpdateTeamDto dto)
    {
        await _validationService.ValidateAsync<UpdateTeamDto>(dto);

        var team = await _repositories.TeamRepository.GetByIdInOrg(teamId, orgId).FirstOrDefaultAsync();

        if (team is null)
            throw new NotFoundAppException("Team");

        team.EnsureEditable();

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
        var team = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .Include(x => x.IconFile)
            .FirstOrDefaultAsync();

        if (team is null)
            throw new NotFoundAppException("Team");

        team.EnsureEditable();

        if (team.IconFileId == null)
            return;

        string? oldPublicId = null;
        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            if (team.IconFile != null)
            {
                oldPublicId = team.IconFile.PublicId;
                team.IconFile.IsDeleted = true;
            }

            team.IconFileId = null;

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        if (oldPublicId != null)
        {
            try { await _externalServiceManager.CloudinaryService.DeleteFileAsync(oldPublicId); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete team icon from Cloudinary. PublicId: {PublicId}", oldPublicId);
            }
        }
    }
}
