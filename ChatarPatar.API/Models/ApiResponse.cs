using System.Net;

namespace ChatarPatar.API.Models;

public class ApiResponse
{
    public int StatusCode { get; set; }
    public string? ExceptionCode { get; set; }
    public string StatusMessage { get; set; }
    public object? Result { get; set; }

    public ApiResponse(HttpStatusCode statusCode, string? exceptionCode, object? result = null, string statusMessage = "")
    {
        StatusCode = (int)statusCode;
        Result = result;
        StatusMessage = statusMessage;
        ExceptionCode = exceptionCode;
    }
}
