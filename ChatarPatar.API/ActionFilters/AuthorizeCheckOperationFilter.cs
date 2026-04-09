using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ChatarPatar.API.ActionFilters;

public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var customAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true) ?? Array.Empty<object>();

        var hasAuthorize = customAttributes.OfType<AuthorizeAttribute>().Any()
            || context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

        if (!hasAuthorize)
            return;

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            },
            new()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "CookieAuth"
                        }
                    },
                    Array.Empty<string>()
                }
            }
        };

        operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
    }
}
