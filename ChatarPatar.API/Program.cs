
using Asp.Versioning.ApiExplorer;
using ChatarPatar.API.ActionFilters;
using ChatarPatar.API.Configuration;
using ChatarPatar.API.Configurations;
using ChatarPatar.API.Middlewares;
using ChatarPatar.Application.Services.DependencyInjection;
using ChatarPatar.Common.DependencyInjection;
using ChatarPatar.Infrastructure.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using Serilog.Events;
using System.Text.Json.Serialization;

namespace ChatarPatar.API;

public class Program
{
    public static void Main(string[] args)
    {
        Serilog.Debugging.SelfLog.Enable(msg =>
        {
            var formatted = $"[{DateTime.UtcNow:u}] {msg}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}";
            File.AppendAllText("serilog-errors.txt", formatted);
        });

        Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.Console()
                .CreateBootstrapLogger();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        //config
        builder.BuildConfiguration();

        // Global Newtonsoft default — applies to every JsonConvert.SerializeObject call
        // in the app (middleware, filters, etc.) without needing per-call settings.
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        builder.Services.AddAuthenticationConfiguration(builder.Configuration);
        builder.Services.AddSwaggerGenConfiguration(builder.Configuration);

        // API Versioning
        builder.Services.AddApiVersioningConfiguration();

        builder.Services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
        });

        builder.Services.AddControllers(options =>
            {
                options.Filters.Add<PermissionFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();

        //serilog
        builder.BuildLogging();

        // Add Dependency configurations
        builder.Services.AddService(builder.Configuration);
        builder.Services.AddCommonService(builder.Configuration);
        builder.Services.AddInfrastructure(builder.Configuration);

        // CORS configuration to allow requests from Angular app
        builder.Services.AddCorsConfiguration(builder.Configuration);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();

            // ── Swagger UI: dropdown per version ───────────────────────────────
            // ResolveConflictingActions is NOT needed — each version has its own doc.
            app.UseSwaggerUI(options =>
            {
                // IApiVersionDescriptionProvider is resolved from DI
                var descriptions = app.Services
                    .GetRequiredService<IApiVersionDescriptionProvider>()
                    .ApiVersionDescriptions;

                // Adds one entry per version — newest first in the dropdown
                foreach (var description in descriptions.Reverse())
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        $"ChatarPatar API {description.GroupName.ToUpperInvariant()}");
                }
            });
        }

        app.UseHttpsRedirection();

        app.UseCors("AllowSpecificOrigin");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<LoggingMiddleware>();

        app.UseMiddleware<ResponseWrapperMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.MapControllers();

        app.Run();
    }
}
