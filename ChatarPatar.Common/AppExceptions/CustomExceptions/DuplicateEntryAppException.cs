using ChatarPatar.Common.Consts;

namespace ChatarPatar.Common.AppExceptions.CustomExceptions;

public class DuplicateEntryAppException : AppException
{
    public DuplicateEntryAppException(string message, string? exceptionCode = null)
        : base($"Duplicate entry found: {message}", exceptionCode: exceptionCode ?? ExceptionCodes.DUPLICATE_RESOURCE) { }
}
