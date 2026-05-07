using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ChatarPatar.API.Configurations;

/// <summary>
/// Dynamically creates one Swagger document per discovered API version.
/// Today → one "v1" entry in the Swagger UI dropdown.
/// When v2 is added → a "v2" entry appears automatically, with zero extra config.
///
/// Registered as IConfigureOptions so it runs after DI is built and
/// IApiVersionDescriptionProvider is available.
/// </summary>
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, BuildInfo(description));
        }
    }

    private static OpenApiInfo BuildInfo(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = "ChatarPatar API",
            Version = description.ApiVersion.ToString(),
            Description = "ChatarPatar — Teams-like Web API"
        };

        if (description.IsDeprecated)
            info.Description += " — ⚠️ This version is deprecated. Please migrate to the latest.";

        return info;
    }
}
