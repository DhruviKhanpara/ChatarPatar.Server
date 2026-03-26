using ChatarPatar.Common.Security.SecurityContracts;
using Microsoft.AspNetCore.Http;

namespace ChatarPatar.Common.Security;

public class BearerAuthTokenStrategy : IAuthTokenStrategy
{
    private const string RefreshTokenHeader = "X-Refresh-Token";

    private readonly IHttpContextAccessor _httpContextAccessor;

    private HttpContext HttpContext => _httpContextAccessor.HttpContext!;

    public BearerAuthTokenStrategy(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? SetAccessToken(string token)
    {
        HttpContext.Items["AccessToken"] = token; // returned in response body via LoginResponseDto
        return token;
    }

    public string? SetRefreshToken(string token)
    {
        HttpContext.Items["RefreshToken"] = token; // returned in response body
        return token;
    }

    public void ClearTokens() { } // mobile clears from secure storage — server has nothing to clear

    public string? GetRefreshToken()
        => HttpContext.Request.Headers[RefreshTokenHeader].FirstOrDefault();
}
