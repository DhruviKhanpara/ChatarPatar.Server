using ChatarPatar.Common.Models;
using ChatarPatar.Common.Security;
using ChatarPatar.Common.Security.SecurityContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatarPatar.Common.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddCommonService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TokenSettings>(configuration.GetSection("TokenSettings"));
        services.Configure<InviteTokenSettings>(configuration.GetSection("InviteTokenSettings"));

        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<CookieAuthTokenStrategy>();
        services.AddScoped<BearerAuthTokenStrategy>();
        services.AddScoped<AuthTokenStrategyFactory>();

        return services;
    }
}
