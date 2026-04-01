using ChatarPatar.Infrastructure.ExternalServiceContracts;
using ChatarPatar.Infrastructure.ExternalServiceContracts.Notification;
using ChatarPatar.Infrastructure.ExternalServices;
using ChatarPatar.Infrastructure.ExternalServices.Notification.Handlers;
using ChatarPatar.Infrastructure.ExternalServices.Notification.Processor;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.Repositories;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatarPatar.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(option => option.UseSqlServer(configuration.GetConnectionString("AppDbConnection")));

        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped<IOutboxMessageHandler, EmailOutboxMessageHandler>();
        services.AddScoped<IOutboxProcessor, GenericOutboxProcessor>();

        services.AddScoped<IRepositoryManager, RepositoryManager>();
        services.AddScoped<IExternalServiceManager, ExternalServiceManager>();

        return services;
    }
}
