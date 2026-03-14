namespace ChatarPatar.Common.AppExceptions.CustomExceptions;

public class NotFoundAppException : AppException
{
    public NotFoundAppException(string message): base($"Not found: {message}") { }
}
