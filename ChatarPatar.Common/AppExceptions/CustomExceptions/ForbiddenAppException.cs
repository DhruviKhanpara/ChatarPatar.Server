using ChatarPatar.Common.Consts;

namespace ChatarPatar.Common.AppExceptions.CustomExceptions;

public class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string? message = "You do not have permission to perform this action.", string? exceptionCode = null)
        : base(message, exceptionCode: exceptionCode ?? ExceptionCodes.FORBIDDEN) { }
}
