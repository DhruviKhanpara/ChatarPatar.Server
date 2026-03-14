using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.Validators.User;
using ChatarPatar.Common.Security;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatarPatar.Application.Services.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddService(this IServiceCollection services, IConfiguration configuration)
    {
        //services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
        services.AddHttpContextAccessor();

        services.AddValidatorsFromAssemblyContaining<UserLoginDtoValidator>();
        services.AddScoped<IValidationService, ValidationService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPermissionService, PermissionService>();

        services.AddScoped<IServiceManager, ServiceManager>();
        services.AddScoped<TokenService>();

        return services;
    }
}
