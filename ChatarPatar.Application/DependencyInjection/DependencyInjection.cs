using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.ServiceContracts.Notification;
using ChatarPatar.Application.Services.Notification;
using ChatarPatar.Application.Services.Notification.BackgroundServices;
using ChatarPatar.Application.Services.Notification.Dispatcher;
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

        // --- Notification queue (singleton — lives for app lifetime) ---
        services.AddSingleton<IOutboxBackgroundQueue, OutboxBackgroundQueue>();

        // --- Background service (hosted, picks up queue signals) ---
        services.AddHostedService<OutboxBackgroundService>();

        // --- Validators ---
        services.AddValidatorsFromAssemblyContaining<UserLoginDtoValidator>();

        // --- Mapper Profiles ---
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // --- Validator Service ---
        services.AddScoped<IValidationService, ValidationService>();

        // --- Application services ---
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationInviteService, OrganizationInviteService>();
        services.AddScoped<IOrganizationMemberService, OrganizationMemberService>();

        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<ITeamMemberService, TeamMemberService>();

        services.AddScoped<IPermissionService, PermissionService>();

        // --- Notification ---
        services.AddScoped<INotificationDispatcher, OutboxEmailDispatcher>();
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();

        services.AddScoped<IServiceManager, ServiceManager>();

        return services;
    }
}
