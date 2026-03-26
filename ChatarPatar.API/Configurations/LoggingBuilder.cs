using ChatarPatar.Common.AppLogging.Infrastructure;
using ChatarPatar.Common.AppLogging.Model;

namespace ChatarPatar.API.Configuration;

public static class Logger
{
    public static WebApplicationBuilder BuildLogging(this WebApplicationBuilder builder)
    {
        string connectionString = builder.Configuration.GetConnectionString("AppDbConnection") ?? "";
        string logEventLevel = builder.Configuration["Serilog:LevelSwitches:$systemLogSwitch"] ?? "Information";

        LogBuilderRequest logBuilderRequest = new LogBuilderRequest();

        if (!string.IsNullOrEmpty(connectionString))
        {
            logBuilderRequest.LogToBuild.Add(LoggingTypes.SystemLog, new LogBuilderItem(connectionString, logEventLevel, true));
            logBuilderRequest.LogToBuild.Add(LoggingTypes.AuditLog, new LogBuilderItem(connectionString, "Information", true));
        }
         
        builder.ApplyAppLogs(logBuilderRequest);

        return builder;
    }
}
