using ChatarPatar.Common.AppExceptions.CustomExceptions;

namespace ChatarPatar.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (AppException ex)
        {
            if (ex is ValidationAppException validationEx)
            {
                _logger.LogWarning(
                    "Validation failed at {Endpoint} — {ErrorCount} error(s): {ValidationErrors}",
                    httpContext?.GetEndpoint()?.DisplayName ?? "[Unknown]",
                    validationEx.Errors.Count,
                    validationEx.GroupedErrors
                );

                httpContext.Items["ErrorData"] = validationEx.GroupedErrors;
            }
            else
            {
                _logger.LogError(
                    ex,
                    "App exception at {Endpoint}: {Message} {InnerException}",
                    httpContext?.GetEndpoint()?.DisplayName ?? "[Unknown endpoint]",
                    ex.Message,
                    ex.InnerException?.Message
                );
            }

            string exceptionMessage = !string.IsNullOrEmpty(ex.UserFriendlyMessage) ? ex.UserFriendlyMessage : ex.Message;

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = ex switch
            {
                InvalidDataAppException or
                ValidationAppException => StatusCodes.Status400BadRequest,
                NotFoundAppException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            httpContext.Items.Add("StatusMessage", exceptionMessage);
            httpContext.Items["ExceptionCode"] = ex.GetType().Name;

            var errorPayload = new { exceptionCode = ex.GetType().Name, message = ex.UserFriendlyMessage ?? ex.Message };

            await httpContext.Response.WriteAsJsonAsync(errorPayload);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception at {Endpoint}: {Message} {InnerException}",
                httpContext?.GetEndpoint()?.DisplayName ?? "[Unknown endpoint]",
                ex.Message,
                ex.InnerException?.Message
            );

            string exceptionMessage = "Something went wrong. Please try again later.";

            httpContext.Response.HttpContext.Items.Add("StatusMessage", exceptionMessage);

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var errorPayload = new { exceptionCode = "UnhandledException", message = "Something went wrong. Please try again later." };

            await httpContext.Response.WriteAsJsonAsync(errorPayload);
        }
    }
}
