
using ChatarPatar.API.ActionFilters;
using ChatarPatar.API.Configuration;
using ChatarPatar.API.Middlewares;
using ChatarPatar.Application.Services.DependencyInjection;
using ChatarPatar.Infrastructure.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace ChatarPatar.API;

public class Program
{
    public static void Main(string[] args)
    {
        //Serilog.Debugging.SelfLog.Enable(msg =>
        //{
        //    var formatted = $"[{DateTime.UtcNow:u}] {msg}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}";
        //    File.AppendAllText("serilog-errors.txt", formatted);
        //});

        //Log.Logger = new LoggerConfiguration()
        //        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        //        .WriteTo.Console()
        //        .CreateBootstrapLogger();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        //config
        //builder.BuildConfiguration();

        builder.Services.AddAuthenticationConfiguration(builder.Configuration);
        builder.Services.AddSwaggerGenConfiguration(builder.Configuration);

        builder.Services.AddControllers(options =>
        {
            options.Filters.Add<PermissionFilter>();
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();

        //serilog
        //builder.BuildLogging();

        //Automapper setup
        builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // Add Dependency configurations
        builder.Services.AddService(builder.Configuration);
        builder.Services.AddInfrastructure(builder.Configuration);

        // CORS configuration to allow requests from Angular app
        builder.Services.AddCorsConfiguration(builder.Configuration);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseCors("AllowSpecificOrigin");

        app.UseAuthentication();
        app.UseAuthorization();

        //app.UseMiddleware<LoggingMiddleware>();

        app.UseMiddleware<ResponseWrapperMiddleware>();            
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.MapControllers();

        app.Run();
    }
}
