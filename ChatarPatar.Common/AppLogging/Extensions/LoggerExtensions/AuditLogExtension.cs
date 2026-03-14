using ChatarPatar.Common.AppLogging.Model;
using ChatarPatar.Common.AppLogging.Model.LogRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace ChatarPatar.Common.AppLogging.Extensions.LoggerExtensions;

public static class AuditLogExtension
{
    public static ILogger WriteAuditLog(this ILogger logger, AuditLogRequest logRequest)
    {
        const string messageTemplate = "{Entity:1} was {state:1}";

        using (LogContext.PushProperty(LoggingProperties.LoggingType, value: LoggingTypes.AuditLog))
        using (LogContext.PushProperty(LoggingProperties.TableName, value: logRequest.TableName))
        using (LogContext.PushProperty(LoggingProperties.RecordId, value: logRequest.RecordId))
        using (LogContext.PushProperty(LoggingProperties.Record, value: logRequest.ChangeRecord, destructureObjects: true))
            logger.LogInformation(messageTemplate, logRequest.TableName, logRequest.ChangeRecord.ToString());

        return logger;
    }

    public static ILogger WriteAuditLog(this ILogger logger, IEnumerable<AuditLogRequest> logRequests)
    {
        logRequests.ToList().ForEach(logRequest =>
        {
            logger.WriteAuditLog(logRequest);
        });

        return logger;
    }

    public static int SaveChangesWithAudit(this DbContext context, ILogger logger)
    {
        var auditLogRequests = RetrieveChangesUsingChangeTracker(context);

        var result = context.SaveChanges();

        SaveAuditLogRequests(auditLogRequests, logger, context);

        return result;
    }

    public static async Task<int> SaveChangesAsyncWithAudit(this DbContext context, ILogger logger)
    {
        var auditLogRequests = RetrieveChangesUsingChangeTracker(context);

        var result = await context.SaveChangesAsync();

        SaveAuditLogRequests(auditLogRequests, logger, context);

        return result;
    }

    private static List<AuditLogRequest> RetrieveChangesUsingChangeTracker(DbContext context)
    {
        var changes = context.ChangeTracker.Entries()
            .Where(entity =>
                entity.State == EntityState.Added
             || entity.State == EntityState.Modified
             || entity.State == EntityState.Deleted)
            .ToList();

        List<AuditLogRequest> auditLogRequests = new List<AuditLogRequest>();
        changes.ForEach(change => auditLogRequests.Add(new AuditLogRequest(change)));

        return auditLogRequests;
    }

    private static void SaveAuditLogRequests(IEnumerable<AuditLogRequest> logRequests, ILogger logger, DbContext context)
    {
        foreach (var logRequest in logRequests.Where(x => x.ChangeState == EntityState.Added))
        {
            if (logRequest.SourceEntity != null)
            {
                var entry = context.Entry(logRequest.SourceEntity.Entity);

                AuditLogRequest auditRequestPostSave = new AuditLogRequest(entry);
                logRequest.RecordId = auditRequestPostSave.RecordId;
                logRequest.ChangeRecord.After = auditRequestPostSave.ChangeRecord.After;
            }
        }

        logger.WriteAuditLog(logRequests);
    }
}
