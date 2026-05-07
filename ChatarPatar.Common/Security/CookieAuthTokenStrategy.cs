using ChatarPatar.Common.Models;
using ChatarPatar.Common.Security.SecurityContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ChatarPatar.Common.Security;

public class CookieAuthTokenStrategy : IAuthTokenStrategy
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenSettings _tokenSettings;

    private HttpContext HttpContext => _httpContextAccessor.HttpContext!;

    public CookieAuthTokenStrategy(IHttpContextAccessor httpContextAccessor, IOptions<TokenSettings> tokenSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenSettings = tokenSettings.Value;
    }

    public string? SetAccessToken(string token)
    {
        HttpContext.Response.Cookies.Append(_tokenSettings.AccessTokenName, token, BuildAccessCookieOptions());
        return null;
    }

    public string? SetRefreshToken(string token)
    {
        HttpContext.Response.Cookies.Append(_tokenSettings.RefreshTokenName, token, BuildRefreshCookieOptions());
        return null;
    }

    public void ClearTokens()
    {
        HttpContext.Response.Cookies.Delete(_tokenSettings.AccessTokenName, BuildAccessCookieOptions());
        HttpContext.Response.Cookies.Delete(_tokenSettings.RefreshTokenName, BuildRefreshCookieOptions());
    }

    public string? GetRefreshToken()
    {
        HttpContext.Request.Cookies.TryGetValue(_tokenSettings.RefreshTokenName, out var token);
        return token;
    }

    private CookieOptions BuildAccessCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = "/",
        IsEssential = true,
        MaxAge = TimeSpan.FromMinutes(_tokenSettings.TokenExpirationMinutes)
    };

    private CookieOptions BuildRefreshCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = "/api/auth",
        IsEssential = true,
        MaxAge = TimeSpan.FromDays(_tokenSettings.RefreshTokenExpirationDays)
    };
}
