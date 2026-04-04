using ChatarPatar.Common.AppLogging.Model;
using Serilog;
using Serilog.Filters;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;

namespace ChatarPatar.Common.AppLogging.Infrastructure.LogBuilders;

internal static class AuditLogBuilder
{
    internal static LoggerConfiguration ApplyAuditLog(this LoggerConfiguration logger, LogBuilderItem logBuilderItem)
    {
        if (logBuilderItem != null)
        {
            var sinkOptions = new MSSqlServerSinkOptions
            {
                TableName = LoggingTypes.AuditLog,
                AutoCreateSqlDatabase = false,
                AutoCreateSqlTable = false,
                SchemaName = "logging"
            };

            var columnOptions = new ColumnOptions
            {
                AdditionalColumns = new Collection<SqlColumn>
                {
                    new SqlColumn(LoggingProperties.UserName, SqlDbType.NVarChar),
                    new SqlColumn(LoggingProperties.TableName, SqlDbType.NVarChar),
                    new SqlColumn(LoggingProperties.RecordId, SqlDbType.UniqueIdentifier),
                }
            };

            columnOptions.Store.Remove(StandardColumn.Properties);
            columnOptions.Store.Remove(StandardColumn.Exception);
            columnOptions.Store.Add(StandardColumn.LogEvent);
            columnOptions.LogEvent.ExcludeStandardColumns = true;

            logger
                .Filter.ByIncludingOnly(Matching.WithProperty(LoggingProperties.LoggingType, LoggingTypes.AuditLog))
                .WriteTo.MSSqlServer(logBuilderItem.ConnectionString, sinkOptions: sinkOptions, restrictedToMinimumLevel: logBuilderItem.MinimumLogLevel, columnOptions: columnOptions);
        }
        return logger;
    }
}
