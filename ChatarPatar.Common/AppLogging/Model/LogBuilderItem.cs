using Serilog.Events;

namespace ChatarPatar.Common.AppLogging.Model;

public class LogBuilderItem
{
    public string ConnectionString;
    public LogEventLevel MinimumLogLevel;
    public bool Enabled;

    public LogBuilderItem(string connectionString, string? logEventLevel, bool enabled)
        : this(connectionString, GetLogEventLevelFromString(logEventLevel), enabled)
    { }

    public LogBuilderItem(string connectionString, LogEventLevel logEventLevel, bool enabled)
    {
        ConnectionString = connectionString;
        MinimumLogLevel = logEventLevel;
        Enabled = enabled;
    }

    public static LogEventLevel GetLogEventLevelFromString(string? logLevelString)
    {
        LogEventLevel logEventLevel = LogEventLevel.Information;
        Enum.TryParse<LogEventLevel>(logLevelString, out logEventLevel);
        return logEventLevel;
    }
}
