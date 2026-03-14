namespace ChatarPatar.Common.AppLogging.Model;

public static class LoggingTypes
{
    public const string SystemLog = "SystemLog";
    public const string ApplicationLog = "ApplicationLog";
    public const string AuditLog = "AuditLog";

    public static string[] AllNonSystemLoggingTypes = new[] { ApplicationLog, AuditLog };
}
