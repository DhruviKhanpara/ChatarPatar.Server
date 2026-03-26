using ChatarPatar.Common.AppLogging.Model;
using ChatarPatar.Common.HttpUserDetails;
using Serilog.Context;

namespace ChatarPatar.API.Middlewares;

public class LoggingMiddleware
{
    readonly RequestDelegate _next;

    public LoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var userName = httpContext.GetUserName()
            ?? httpContext.GetUserEmail()
            ?? httpContext.GetUserId()
            ?? "Anonymous";

        using (LogContext.PushProperty(LoggingProperties.ServerName, Environment.MachineName))
        using (LogContext.PushProperty(LoggingProperties.UserName, userName))
        using (LogContext.PushProperty(LoggingProperties.MethodType, httpContext.Request.Method))
        using (LogContext.PushProperty(LoggingProperties.Origin, httpContext.Request.Headers.Referer))
        using (LogContext.PushProperty(LoggingProperties.Path, httpContext.Request.Path + httpContext.Request.QueryString))
        using (LogContext.PushProperty(LoggingProperties.Platform, httpContext.Request.Headers["sec-ch-ua-platform"].ToString()))
        using (LogContext.PushProperty(LoggingProperties.UserAgent, httpContext.Request.Headers["User-Agent"].ToString()))
        {
            await _next(httpContext);
        }
    }
}
