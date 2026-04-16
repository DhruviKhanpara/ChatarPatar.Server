using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.OrganizationInvite;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Common.Security.SecurityContracts;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Application.Services;

internal class OrganizationInviteService : IOrganizationInviteService
{
    private readonly IRepositoryManager _repositories;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenService _tokenService;
    private readonly IEmailNotificationService _emailNotificationService;

    public OrganizationInviteService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor, ITokenService tokenService, IEmailNotificationService emailNotificationService)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _tokenService = tokenService;
        _emailNotificationService = emailNotificationService;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<PagedResult<OrganizationInviteListItemDto>> GetPendingInvitesAsync(Guid orgId, InviteQueryParams queryParams)
    {
        var baseQuery = _repositories.OrganizationInviteRepository
            .GetPendingInvitesQuery(orgId, queryParams.Search, queryParams.Role);

        var totalCount = await baseQuery.CountAsync();

        var items = await baseQuery
            .PaginateOffset(queryParams.PageSize, queryParams.PageNumber)
            .AsNoTracking()
            .ProjectTo<OrganizationInviteListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<OrganizationInviteListItemDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
    }

    public async Task SendInviteAsync(Guid orgId, SendInviteDto dto)
    {
        await _validationService.ValidateAsync<SendInviteDto>(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());
        var authUser = _httpContext.GetUserName();
        var email = dto.Email.Trim().ToLower();

        var user = await _repositories.UserRepository.GetUserByEmailAsync(email: email);

        if (user is not null)
            throw new InvalidDataAppException("This email is already registered, please add them directly from the app");

        // --- No pending invite for the same email already exists ---
        var hasPendingInvite = await _repositories.OrganizationInviteRepository
            .GetPendingByEmail(email)
            .AnyAsync(x => x.OrganizationId == orgId);

        if (hasPendingInvite)
            throw new DuplicateEntryAppException("An active invite has already been sent to this email");

        // --- Fetch org for display name in email ---
        var org = await _repositories.OrganizationRepository
            .FindByCondition(o => o.Id == orgId)
            .FirstOrDefaultAsync();

        if (org is null)
            throw new NotFoundAppException("Organization not found");

        // --- Generate token — store hash in DB, return raw token to caller ---
        var rawToken = _tokenService.GenerateInviteToken();
        var hashedToken = _tokenService.HashToken(rawToken);
        var expiresAt = _tokenService.GetInviteExpiresAt();

        var invite = new OrganizationInvite
        {
            OrganizationId = orgId,
            CreatedBy = authUserId,
            Email = email,
            Role = dto.Role,
            Token = hashedToken,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _repositories.OrganizationInviteRepository.AddAsync(invite);
        await _repositories.UnitOfWork.SaveChangesAsync();

        // --- Dispatch invite email via outbox ---
        await _emailNotificationService.SendOrgInviteAsync(
            toEmail: email,
            orgName: org.Name,
            inviterName: authUser,
            roleName: dto.Role.ToString(),
            inviteToken: rawToken,
            expiryDays: (expiresAt.Date - DateTime.UtcNow.Date).Days
        );
    }

    public async Task CancelInviteAsync(Guid orgId, Guid inviteId)
    {
        var invite = await _repositories.OrganizationInviteRepository
            .GetByIdInOrg(id: inviteId, orgId: orgId)
            .FirstOrDefaultAsync();

        if (invite is null)
            throw new NotFoundAppException("Invite");

        if (invite.IsUsed)
            throw new InvalidDataAppException("Invite already used or invalid");

        invite.IsUsed = true;
        invite.UsedAt = DateTime.UtcNow;
        invite.UpdatedAt = DateTime.UtcNow;

        await _repositories.UnitOfWork.SaveChangesAsync();
    }
}
