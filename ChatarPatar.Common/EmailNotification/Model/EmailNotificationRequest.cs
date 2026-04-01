namespace ChatarPatar.Common.EmailNotification.Model;

public class EmailNotificationRequest
{
    public EmailNotificationRequest() { }

    public EmailNotificationRequest(string emailBody, string subject, List<string> toAddresses, List<string>? cCAddresses, List<string>? bCCAddresses, string? fromAddress = null)
    {
        EmailBody = emailBody;
        Subject = subject;
        ToAddresses = toAddresses;
        CCAddresses = cCAddresses;
        BCCAddresses = bCCAddresses;

        if(!string.IsNullOrWhiteSpace(fromAddress))
            FromAddress = fromAddress;
    }

    public List<string> ToAddresses { get; set; } = new List<string>();
    public List<string>? CCAddresses { get; set; }
    public List<string>? BCCAddresses { get; set; }
    public string Subject { get; set; } = null!;
    public string EmailBody { get; set; } = null!;
    public string? FromAddress { get; set; } = null!;
    public string? DisplayName { get; set; } = null!;
}
