using ChatarPatar.Common.Security.SecurityContracts;
using Microsoft.AspNetCore.Http;

namespace ChatarPatar.Common.Security;

public class AuthTokenStrategyFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CookieAuthTokenStrategy _cookieStrategy;
    private readonly BearerAuthTokenStrategy _bearerStrategy;

    public AuthTokenStrategyFactory(
        IHttpContextAccessor httpContextAccessor,
        CookieAuthTokenStrategy cookieStrategy,
        BearerAuthTokenStrategy bearerStrategy)
    {
        _httpContextAccessor = httpContextAccessor;
        _cookieStrategy = cookieStrategy;
        _bearerStrategy = bearerStrategy;
    }

    public IAuthTokenStrategy Resolve()
    {
        var request = _httpContextAccessor.HttpContext?.Request;

        // mobile sends Authorization header or X-Client-Type header
        var isMobileOrApiClient =
            request?.Headers["X-Client-Type"].ToString().Equals("mobile", StringComparison.OrdinalIgnoreCase) == true;

        return isMobileOrApiClient ? _bearerStrategy : _cookieStrategy;
    }
}
