using ChatarPatar.Common.AppLogging.Model;
using ChatarPatar.Common.AppLogging.Model.LogRequest;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Sinks.MSSqlServer;

namespace ChatarPatar.Common.AppLogging.Extensions.LoggerExtensions;

public static class CommunicationLogExtension
{
    public static ILogger WriteCommunicationLog(this ILogger logger, CommunicationLogRequest logRequest)
    {
        const string messageTemplate = "Communication sent to {TargetSystem:1}";

        using (LogContext.PushProperty(LoggingProperties.LoggingType, value: LoggingTypes.CommunicationLog))
        using (LogContext.PushProperty(LoggingProperties.DeliveryMethod, value: logRequest.DeliveryMethod))
        using (LogContext.PushProperty(LoggingProperties.DeliveryStatus, value: logRequest.DeliveryStatus))
        using (LogContext.PushProperty(LoggingProperties.DeliveryDetails, value: logRequest.DeliveryDetails, destructureObjects: true))
        using (LogContext.PushProperty(StandardColumn.Exception.ToString(), value: logRequest.ErrorMessage))
            logger.LogInformation(messageTemplate, logRequest.TargetSystem);

        return logger;
    }

    public static ILogger WriteCommunication_Email(this ILogger logger, DeliveryStatus deliveryStatus, string fromAddress, IEnumerable<string> toAddresses, IEnumerable<string>? ccAddresses = null, IEnumerable<string>? bccAddresses = null, string? subject = null, string? body = null, string? errorMessage = null)
    {
        object details = new
        {
            FromAddress = fromAddress,
            ToAddress = toAddresses is not null ? String.Join(", ", toAddresses) : null,
            CCAddress = ccAddresses is not null ? String.Join(", ", ccAddresses) : null,
            BCCAddress = bccAddresses is not null ? String.Join(", ", bccAddresses) : null,
            Subject = subject,
            Body = body
        };

        CommunicationLogRequest logRequest = new CommunicationLogRequest("Outgoing Email", DeliveryMethod.Email, deliveryStatus, details, errorMessage);

        logger.WriteCommunicationLog(logRequest);

        return logger;
    }
}