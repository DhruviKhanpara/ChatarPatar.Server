namespace ChatarPatar.Common.AppExceptions.CustomExceptions;

public class AppException : Exception
{
    public string? UserFriendlyMessage { get; }

    public AppException(string? userFriendlyMessage = null, string? technicalMessage = null)
        : base(technicalMessage ?? userFriendlyMessage)
    {
        UserFriendlyMessage = userFriendlyMessage;
    }
}