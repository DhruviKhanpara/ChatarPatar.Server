using ChatarPatar.Application.RepositoryContracts;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.ExternalServiceContracts;
using ChatarPatar.Infrastructure.ExternalServices;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatarPatar.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(option => option.UseSqlServer(configuration.GetConnectionString("AppDbConnection")));

        services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));

        services.AddScoped<ICloudinaryService, CloudinaryService>();

        services.AddScoped<IRepositoryManager, RepositoryManager>();
        services.AddScoped<IExternalServiceManager, ExternalServiceManager>();

        return services;
    }
}
