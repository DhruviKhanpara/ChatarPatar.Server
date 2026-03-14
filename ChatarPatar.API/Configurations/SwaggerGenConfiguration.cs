using ChatarPatar.API.ActionFilters;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace ChatarPatar.API.Configuration;

public static class SwaggerGenConfiguration
{
    public static void AddSwaggerGenConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(option =>
        {
            option.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "My ChatarPatar API",
                Version = "v1",
                Description = "ChatarPatar - Teams-like Web API"
            });

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
                Name = "AccessToken",
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