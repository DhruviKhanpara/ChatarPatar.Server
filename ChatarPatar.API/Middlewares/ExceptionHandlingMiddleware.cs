using ChatarPatar.API.Models;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Consts;
using Newtonsoft.Json;
using System.Net;

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
                    "Validation failed was thrown — {ErrorCount} error(s): {ValidationErrors}",
                    validationEx.Errors.Count,
                    validationEx.GroupedErrors
                );

                httpContext.Items["ErrorData"] = validationEx.GroupedErrors;
            }
            else
            {
                _logger.LogError(ex, "An exception was thrown: {ExceptionMessage}", ex.Message);
            }

            var (statusCode, exceptionCode) = MapException(ex);

            string exceptionMessage = !string.IsNullOrEmpty(ex.UserFriendlyMessage)
                ? ex.UserFriendlyMessage
                : ex.Message;

            await WriteApiResponseAsync(httpContext, statusCode, exceptionCode, exceptionMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                    ex,
                    "An exception was thrown: {ExceptionMessage}",
                    ex.Message
            );

            string exceptionMessage = "Something went wrong. Please try again later.";

            await WriteApiResponseAsync(httpContext, HttpStatusCode.InternalServerError, ExceptionCodes.UNHANDLED_EXCEPTION, exceptionMessage);
        }
    }

    #region Private Section

    private static async Task WriteApiResponseAsync(HttpContext httpContext, HttpStatusCode statusCode, string exceptionCode, string message)
    {
        httpContext.Items["ExceptionCode"] = exceptionCode;
        httpContext.Items["StatusMessage"] = message;

        var response = new ApiResponse(statusCode, exceptionCode, result: null, statusMessage: message);
        var json = JsonConvert.SerializeObject(response);

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(json);
    }

    private static (HttpStatusCode statusCode, string exceptionCode) MapException(AppException ex)
    {
        var (statusCode, defaultCode) = ex switch
        {
            ValidationAppException => (HttpStatusCode.BadRequest, ExceptionCodes.VALIDATION_FAILED),
            InvalidDataAppException => (HttpStatusCode.BadRequest, ExceptionCodes.INVALID_DATA),
            NotFoundAppException => (HttpStatusCode.NotFound, ExceptionCodes.RESOURCE_NOT_FOUND),
            DuplicateEntryAppException => (HttpStatusCode.Conflict, ExceptionCodes.DUPLICATE_RESOURCE),
            UnauthorizedAppException => (HttpStatusCode.Unauthorized, ExceptionCodes.AUTH_REQUIRED),
            ForbiddenAppException => (HttpStatusCode.Forbidden, ExceptionCodes.FORBIDDEN),
            _ => (HttpStatusCode.InternalServerError, ex.GetType().Name),
        };

        // Caller-supplied ExceptionCode always wins over the type-level default.
        // e.g. throw new InvalidDataAppException("...", ExceptionCodes.INVITE_TOKEN_EXPIRED)
        var resolvedCode = !string.IsNullOrEmpty(ex.ExceptionCode) ? ex.ExceptionCode : defaultCode;

        return (statusCode, resolvedCode);
    }

    #endregion
}
