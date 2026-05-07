using Asp.Versioning;

namespace ChatarPatar.API.Configurations;

public static class ApiVersioningConfiguration
{
    public static void AddApiVersioningConfiguration(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                // Default to v1 when the client sends no version — avoids 400 on un-versioned calls.
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;

                // Response headers tell clients which versions exist:
                //   api-supported-versions: 1.0
                //   api-deprecated-versions: (populated once you mark something [Deprecated])
                options.ReportApiVersions = true;

                // URL-segment strategy: /api/v1/auth/login
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                // Formats the version as "v1", "v2" etc. in Swagger group names.
                options.GroupNameFormat = "'v'VVV";

                // Replaces {version:apiVersion} in route templates automatically
                // so Swagger generates the correct URL (e.g. /api/v1/auth/login).
                options.SubstituteApiVersionInUrl = true;
            });
    }
}
