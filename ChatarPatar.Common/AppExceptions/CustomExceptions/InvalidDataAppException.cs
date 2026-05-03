using ChatarPatar.Common.Consts;

namespace ChatarPatar.Common.AppExceptions.CustomExceptions;

public class InvalidDataAppException : AppException
{
    public InvalidDataAppException(string message, string? exceptionCode = null)
        : base($"Invalid data: {message}", exceptionCode: exceptionCode ?? ExceptionCodes.INVALID_DATA) { }
}
