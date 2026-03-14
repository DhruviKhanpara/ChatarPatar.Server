namespace ChatarPatar.Common.AppExceptions.CustomExceptions;

public class InvalidDataAppException : AppException
{
    public InvalidDataAppException(string message) : base($"Invalid data: {message}") { }
}
