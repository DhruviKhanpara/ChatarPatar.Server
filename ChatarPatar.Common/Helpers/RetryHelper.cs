namespace ChatarPatar.Common.Helpers;

public static class RetryHelper
{
    public static TimeSpan GetExponentialBackoff(int retryCount, int maxDelayMinutes = 60)
    {
        var delay = TimeSpan.FromMinutes(Math.Pow(2, retryCount));
        return delay > TimeSpan.FromMinutes(maxDelayMinutes)
            ? TimeSpan.FromMinutes(maxDelayMinutes)
            : delay;
    }
}
