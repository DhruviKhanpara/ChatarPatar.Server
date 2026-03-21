using ChatarPatar.API.Attributes;
using ChatarPatar.API.Models;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace ChatarPatar.API.Middlewares;

public class ResponseWrapperMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseWrapperMiddleware> _logger;

    private readonly List<string> _excludedContentTypes =
        new List<string>()
        {
            "application/octet-stream",
            "text/html",
            "application/xml; charset=utf-8",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/pdf",
            "application/png",
            "image/"
        };

    public ResponseWrapperMiddleware(RequestDelegate next, ILogger<ResponseWrapperMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        //using MemoryStream placeholderStream = new MemoryStream();

        Stream originalStream = httpContext.Response.Body;

        await using var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        HttpStatusCode statusCode = HttpStatusCode.OK;
        string statusMessage = "Success";
        string contentType = "application/json";
        string? responseBody = String.Empty;

        var useResponseWrapper = httpContext.GetEndpoint()?.Metadata?.GetMetadata<ResponseWrapperAttribute>()?.WrapResponse != false;

        try
        {
            await _next(httpContext);

            statusCode = httpContext.Response.StatusCode == (int)HttpStatusCode.NoContent ? HttpStatusCode.OK : (HttpStatusCode)httpContext.Response.StatusCode;
            contentType = httpContext.Response.ContentType ?? "";

            memoryStream.Position = 0;
            responseBody = await GetResponseBodyFromStream(memoryStream);

            useResponseWrapper &= (int)statusCode > 199 && !new[] { HttpStatusCode.NotModified, HttpStatusCode.ResetContent }.Contains(statusCode);

            if (!IsExcluded(contentType) && useResponseWrapper)
            {
                object? objResult = null;
                try { objResult = JsonConvert.DeserializeObject(responseBody); }
                catch { objResult = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(responseBody)); }

                (statusMessage, objResult) = GetMessageByStatusCode(statusCode, objResult, httpContext);
                contentType = "application/json";

                responseBody = JsonConvert.SerializeObject(new ApiResponse(statusCode, httpContext.Items["ExceptionCode"]?.ToString() ?? "None", objResult, statusMessage));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"App exception at {httpContext?.GetEndpoint()?.DisplayName ?? "[Unknown endpoint]"} : {ex.Message} {ex.InnerException?.Message}", httpContext?.GetEndpoint()?.Metadata);

            statusCode = HttpStatusCode.InternalServerError;
            statusMessage = $"Uncaught exception in Response Wrapper Middleware Pipeline";
            contentType = "application/json";

            if (useResponseWrapper)
            {
                responseBody = JsonConvert.SerializeObject(new ApiResponse(statusCode, "None", null, statusMessage));
            }
        }
        finally
        {
            var responseBytes = Encoding.UTF8.GetBytes(responseBody);

            httpContext.Response.StatusCode = (int)statusCode;
            httpContext.Response.ContentLength = responseBytes.Length;
            httpContext.Response.ContentType = contentType;
            //httpContext.Response.Body.SetLength(responseBytes.Length);

            //placeholderStream.Seek(0, SeekOrigin.Begin);
            //await placeholderStream.WriteAsync(responseBytes, 0, responseBytes.Length);

            //placeholderStream.Seek(0, SeekOrigin.Begin);
            //await placeholderStream.CopyToAsync(originalStream);

            //httpContext.Response.Body = originalStream;

            // Restore original stream first, then write directly to it
            httpContext.Response.Body = originalStream;
            await originalStream.WriteAsync(responseBytes, 0, responseBytes.Length);
        }
    }

    private bool IsExcluded(string contentType)
    {
        return _excludedContentTypes.Any(x => contentType.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> GetResponseBodyFromStream(Stream stream)
    {
        string responseBody = String.Empty;
        try
        {
            using (StreamReader responseBodyStream = new StreamReader(stream, leaveOpen: true))
            {
                responseBodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
                responseBody = await responseBodyStream.ReadToEndAsync();
            }
        }
        catch
        {
            responseBody = "Error getting response body in ResponseWrapper Middleware pipeline";
        }

        return responseBody;
    }

    private (string statusMessage, object? objResult) GetMessageByStatusCode(HttpStatusCode statusCode, object? objResult, HttpContext httpContext)
    {
        object? message = httpContext.Items["StatusMessage"];
        switch (statusCode)
        {
            case HttpStatusCode.OK:
                return (message?.ToString() ?? "Success", objResult);
            case HttpStatusCode.Conflict:
                return (message?.ToString() ?? "Conflict", null);
            case HttpStatusCode.NotFound:
                return (message?.ToString() ?? "Not Found", null);
            case HttpStatusCode.BadRequest:
                return (message?.ToString() ?? "Bad Request. Verify the request structure/data and try again", httpContext.Items["ErrorData"]);
            default:
                return (message?.ToString() ?? "Something went wrong", null);
        }
    }
}
