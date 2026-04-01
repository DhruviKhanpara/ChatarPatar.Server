using ChatarPatar.Common.AppLogging.Model;
using Serilog;
using Serilog.Filters;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;

namespace ChatarPatar.Common.AppLogging.Infrastructure.LogBuilders;

public static class CommunicationLogBuilder
{
    public static LoggerConfiguration ApplyCommunicationLog(this LoggerConfiguration logger, LogBuilderItem logBuilderItem)
    {
        if (logBuilderItem != null)
        {
            var sinkOptions = new MSSqlServerSinkOptions
            {
                TableName = LoggingTypes.CommunicationLog,
                AutoCreateSqlDatabase = true,
                AutoCreateSqlTable = true,
                SchemaName = "logging"
            };

            var columnOptions = new ColumnOptions
            {
                AdditionalColumns = new Collection<SqlColumn>
                {
                    new SqlColumn(LoggingProperties.UserName, SqlDbType.NVarChar),
                    new SqlColumn(LoggingProperties.DeliveryMethod, SqlDbType.NVarChar),
                    new SqlColumn(LoggingProperties.DeliveryStatus, SqlDbType.NVarChar),
                }
            };

            columnOptions.Store.Remove(StandardColumn.Properties);
            columnOptions.Store.Add(StandardColumn.LogEvent);
            columnOptions.Store.Remove(StandardColumn.Exception);
            columnOptions.LogEvent.ExcludeStandardColumns = true;

            logger
                .Filter.ByIncludingOnly(Matching.WithProperty(LoggingProperties.LoggingType, LoggingTypes.CommunicationLog))
                .WriteTo.MSSqlServer(logBuilderItem.ConnectionString, sinkOptions: sinkOptions, columnOptions: columnOptions);
        }
        return logger;
    }
}

