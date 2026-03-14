using ChatarPatar.Common.AppLogging.Infrastructure.LogBuilders;
using ChatarPatar.Common.AppLogging.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ChatarPatar.Common.AppLogging.Infrastructure;

public static class LoggingBuilder
{
    public static WebApplicationBuilder ApplyAppLogs(this WebApplicationBuilder builder, LogBuilderRequest logBuilderRequest)
    {
        static void InitializeLogger(IConfiguration configuration, LogBuilderRequest logBuilderRequest)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(configuration)
                .WriteTo.Console()
                .WriteTo.Logger(log => log.ApplySystemLog(logBuilderRequest.LogToBuild.FirstOrDefault(x => x.Key == LoggingTypes.SystemLog).Value))
                .WriteTo.Logger(log => log.ApplyAuditLog(logBuilderRequest.LogToBuild.FirstOrDefault(x => x.Key == LoggingTypes.AuditLog).Value))
                .CreateLogger();
        }

        InitializeLogger(builder.Configuration, logBuilderRequest);

        builder.Logging.ClearProviders(); // Optional: Avoid duplicate logs
        builder.Logging.AddSerilog();

        builder.Host.UseSerilog();

        return builder;

        //builder.Logging.ClearProviders(); // Optional: Avoid duplicate logs
        //builder.Logging.AddSerilog();

        //builder.Host.UseSerilog((hostContext, service, loggerConfiguration) =>
        //{
        //    loggerConfiguration
        //        .WriteTo.Console()
        //        .Enrich.FromLogContext()
        //        .ReadFrom.Configuration(hostContext.Configuration)
        //        .WriteTo.Logger(log => log.ApplySystemLog(logBuilderRequest.LogToBuild.FirstOrDefault(x => x.Key == LoggingTypes.SystemLog).Value));
        //});

        //return builder;
    }
}
