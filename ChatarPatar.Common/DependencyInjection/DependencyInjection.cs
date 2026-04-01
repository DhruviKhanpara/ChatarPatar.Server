using ChatarPatar.Common.EmailNotification.Interfaces;
using ChatarPatar.Common.EmailNotification.Services;
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
        services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<OutboxRetrySettings>(configuration.GetSection("OutboxRetrySettings"));

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailTemplateManagerService, EmailTemplateManagerService>();
        services.AddScoped<IEmailHandlerService, EmailHandlerService>();

        services.AddScoped<CookieAuthTokenStrategy>();
        services.AddScoped<BearerAuthTokenStrategy>();
        services.AddScoped<AuthTokenStrategyFactory>();

        return services;
    }
}
