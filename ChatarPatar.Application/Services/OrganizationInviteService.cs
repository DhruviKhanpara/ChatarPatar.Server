using AutoMapper;
using ChatarPatar.Application.DTOs.OrganizationInvite;
using ChatarPatar.Application.RepositoryContracts;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Security.SecurityContracts;
using ChatarPatar.Infrastructure.Entities;
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

    public OrganizationInviteService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor, ITokenService tokenService)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _tokenService = tokenService;
    }
    private HttpContext? _httpContext => _httpContextAccessor.HttpContext;

    public async Task<OrganizationInviteResponseDto> SendInviteAsync(Guid orgId, SendInviteDto dto)
    {
        _validationService.Validate<SendInviteDto>(dto);

        var callerId = Guid.Parse(_httpContext!.GetUserId());
        var email = dto.Email.Trim().ToLower();

        // --- Org must exist and caller must be a member with invite rights ---
        var user = await _repositories.UserRepository.GetUserByEmailAsync(email: email);

        if (user is not null)
            throw new InvalidDataAppException("This email is already registered, please add them directly from the app");

        // --- No pending invite for the same email already exists ---
        var hasPendingInvite = await _repositories.OrganizationInviteRepository
            .GetPendingByEmail(email)
            .AnyAsync(x => x.OrganizationId == orgId);

        if (hasPendingInvite)
            throw new DuplicateEntryAppException("An active invite has already been sent to this email");

        // --- Generate token — store hash in DB, return raw token to caller ---
        var rawToken = _tokenService.GenerateInviteToken();
        var hashedToken = _tokenService.HashToken(rawToken);
        var expiresAt = _tokenService.GetInviteExpiresAt();

        var invite = new OrganizationInvite
        {
            OrganizationId = orgId,
            CreatedBy = callerId,
            Email = email,
            Role = dto.Role,
            Token = hashedToken,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _repositories.OrganizationInviteRepository.AddAsync(invite);
        await _repositories.UnitOfWork.SaveChangesAsync();

        return new OrganizationInviteResponseDto
        {
            RawToken = rawToken,
            ExpiresAt = expiresAt,
        };
    }
}
