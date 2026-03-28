namespace ChatarPatar.Common.AppExceptions.CustomExceptions;

public class DuplicateEntryAppException : AppException
{
    public DuplicateEntryAppException(string message) : base($"Duplicate entry found: {message}") { }
}
