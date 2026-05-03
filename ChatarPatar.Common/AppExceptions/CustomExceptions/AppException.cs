namespace ChatarPatar.Common.AppExceptions.CustomExceptions;

public class AppException : Exception
{
    public string? UserFriendlyMessage { get; }

    /// <summary>
    /// Machine-readable code sent to the client as "exceptionCode".
    /// Subclasses set a sensible default; callers can override (e.g. "INVITE_TOKEN_EXPIRED" instead of "INVALID_DATA").
    /// </summary>
    public string? ExceptionCode { get; }

    public AppException(string? userFriendlyMessage = null, string? technicalMessage = null, string? exceptionCode = null)
        : base(technicalMessage ?? userFriendlyMessage)
    {
        UserFriendlyMessage = userFriendlyMessage;
        ExceptionCode = exceptionCode;
    }
}