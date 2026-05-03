using ChatarPatar.Common.Consts;

namespace ChatarPatar.Common.AppExceptions.CustomExceptions;

public class NotFoundAppException : AppException
{
    public NotFoundAppException(string message, string? exceptionCode = null)
        : base($"Not found: {message}", exceptionCode: exceptionCode ?? ExceptionCodes.RESOURCE_NOT_FOUND) { }
}
