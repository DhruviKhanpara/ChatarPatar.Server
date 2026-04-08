using AutoMapper;
using ChatarPatar.Application.DTOs.OrganizationInvite;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.HttpUserDetails;
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
    private HttpContext? _httpContext => _httpContextAccessor.HttpContext;

    public async Task<OrganizationInviteResponseDto> SendInviteAsync(Guid orgId, SendInviteDto dto)
    {
        _validationService.Validate<SendInviteDto>(dto);

        var authUserId = Guid.Parse(_httpContext!.GetUserId());
        var autUser = _httpContext!.GetUserName();
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
            inviterName: autUser,
            roleName: dto.Role.ToString(),
            inviteToken: rawToken,
            expiryDays: (expiresAt.Date - DateTime.UtcNow.Date).Days
        );

        return new OrganizationInviteResponseDto
        {
            RawToken = rawToken,
            ExpiresAt = expiresAt,
        };
    }
}
