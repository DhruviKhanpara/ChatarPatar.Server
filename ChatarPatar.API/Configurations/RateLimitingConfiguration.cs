using ChatarPatar.Common.Consts;
using ChatarPatar.Common.HttpUserDetails;
using System.Threading.RateLimiting;

namespace ChatarPatar.API.Configurations;

public static class RateLimitingConfiguration
{
    /// <summary>
    /// For endpoints where the risk is someone guessing a secret —
    /// a password, an OTP, a reset token. Tight limit, since a legitimate
    /// caller rarely needs more than a couple of attempts per minute.
    /// </summary>
    public const string AuthStrictPolicy = "auth-strict";

    /// <summary>
    /// For endpoints where the risk is spam/volume rather than guessing —
    /// account creation, triggering an email. Looser window, since a
    /// legitimate caller might genuinely retry a few times.
    /// </summary>
    public const string AuthModeratePolicy = "auth-moderate";

    public static void AddRateLimitingConfiguration(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AuthStrictPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.AddPolicy(AuthModeratePolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
                    }));

            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Items["ExceptionCode"] = ExceptionCodes.RATE_LIMIT_EXCEEDED;
                context.HttpContext.Items["StatusMessage"] = "Too many attempts. Please wait and try again.";

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                return ValueTask.CompletedTask;
            };
        });
    }

    /// <summary>
    /// Partitions by user id once authenticated (so one noisy shared-IP
    /// neighbor can't lock out someone else's account), otherwise by IP 
    /// (no identity to key on yet).
    /// </summary>
    private static string GetPartitionKey(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.GetUserId();
            if (!string.IsNullOrEmpty(userId))
                return $"user:{userId}";
        }

        return $"ip:{httpContext.Connection.RemoteIpAddress}";
    }
}
