using ChatarPatar.API.ActionFilters;
using ChatarPatar.API.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ChatarPatar.API.Configuration;

public static class SwaggerGenConfiguration
{
    public static void AddSwaggerGenConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var accessTokenName = configuration.GetSection("TokenSettings:AccessTokenName").Value!;

        // ConfigureSwaggerOptions creates one SwaggerDoc per API version automatically.
        // Registered here as IConfigureOptions so it resolves post-DI-build,
        // when IApiVersionDescriptionProvider is available.
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

        services.AddSwaggerGen(option =>
        {
            // Security definitions are shared across all versions

            // Bearer Token (for Postman / Mobile / Swagger manual use)
            option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter: Bearer {your JWT token}"
            });

            // Cookie Authentication (for browser / Swagger after login)
            option.AddSecurityDefinition("CookieAuth", new OpenApiSecurityScheme
            {
                Name = accessTokenName,
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Cookie,
                Description = "JWT stored in cookie"
            });

            option.OperationFilter<AuthorizeCheckOperationFilter>();

            //option.AddSecurityRequirement(new OpenApiSecurityRequirement
            //{
            //    {
            //        new OpenApiSecurityScheme
            //        {
            //            Reference = new OpenApiReference
            //            {
            //                Type = ReferenceType.SecurityScheme,
            //                Id = "Bearer"
            //            }
            //        },
            //        Array.Empty<string>()
            //    }
            //});
        });
    }
}