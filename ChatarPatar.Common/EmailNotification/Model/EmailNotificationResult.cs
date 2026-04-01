namespace ChatarPatar.Common.EmailNotification.Model;

public class EmailNotificationResult
{
    public EmailNotificationResult(EmailNotificationRequest request, DateTime attemptedSendDate, DateTime? sentDate, string? errorMessage)
    {
        EmailNotificationRequest = request;
        AttemptedSendDate = attemptedSendDate;
        SentDate = sentDate;
        ErrorMessage = errorMessage;
    }

    public EmailNotificationRequest EmailNotificationRequest { get; private set; }
    public DateTime AttemptedSendDate { get; private set; }
    public DateTime? SentDate { get; private set; }
    public string? ErrorMessage { get; private set; }
}
