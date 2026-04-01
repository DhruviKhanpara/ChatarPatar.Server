namespace ChatarPatar.Common.Models;

public class OutboxRetrySettings
{
    public int RetryCount { get; set; }
    public int RetryDelayMinutes { get; set; }
}
