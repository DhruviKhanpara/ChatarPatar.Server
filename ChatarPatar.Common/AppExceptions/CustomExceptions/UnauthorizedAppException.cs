using ChatarPatar.Common.Consts;

namespace ChatarPatar.Common.AppExceptions.CustomExceptions;

public class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string? message = "Authentication is required to access this resource.", string? exceptionCode = null)
        : base(message, exceptionCode: exceptionCode ?? ExceptionCodes.AUTH_REQUIRED) { }
}
