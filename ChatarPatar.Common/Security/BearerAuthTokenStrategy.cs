using ChatarPatar.Common.Models;
using ChatarPatar.Common.Security.SecurityContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ChatarPatar.Common.Security;

public class BearerAuthTokenStrategy : IAuthTokenStrategy
{
    private const string RefreshTokenHeader = "X-Refresh-Token";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenSettings _tokenSettings;

    private HttpContext HttpContext => _httpContextAccessor.HttpContext!;

    public BearerAuthTokenStrategy(IHttpContextAccessor httpContextAccessor, IOptions<TokenSettings> tokenSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenSettings = tokenSettings.Value;
    }

    public string? SetAccessToken(string token)
    {
        HttpContext.Items[_tokenSettings.AccessTokenName] = token; // returned in response body via LoginResponseDto
        return token;
    }

    public string? SetRefreshToken(string token)
    {
        HttpContext.Items[_tokenSettings.RefreshTokenName] = token; // returned in response body
        return token;
    }

    public void ClearTokens() { } // mobile clears from secure storage — server has nothing to clear

    public string? GetRefreshToken()
        => HttpContext.Request.Headers[RefreshTokenHeader].FirstOrDefault();
}
