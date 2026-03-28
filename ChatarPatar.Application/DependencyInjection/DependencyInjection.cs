using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.Validators.User;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatarPatar.Application.Services.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddService(this IServiceCollection services, IConfiguration configuration)
    {
        // Add http context support
        //services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
        services.AddHttpContextAccessor();
        // Add memory cache support
        services.AddMemoryCache();

        services.AddValidatorsFromAssemblyContaining<UserLoginDtoValidator>();
        services.AddScoped<IValidationService, ValidationService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationInviteService, OrganizationInviteService>();
        services.AddScoped<IPermissionService, PermissionService>();

        services.AddScoped<IServiceManager, ServiceManager>();

        return services;
    }
}
