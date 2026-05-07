namespace ChatarPatar.Common.Models;

public class TokenSettings
{
    /// <summary>
    /// The cookie/header name for access token
    /// </summary>
    public string AccessTokenName { get; set; } = null!;

    /// <summary>
    /// The cookie/header name for refresh token.
    /// </summary>
    public string RefreshTokenName { get; set; } = null!;

    /// <summary>
    /// The secret key for create the access token.
    /// </summary>
    public string SecretKey { get; set; } = null!;

    /// <summary>
    /// The issuer of the token.
    /// </summary>
    public string Issuer { get; set; } = null!;

    /// <summary>
    /// List of Audience for access token
    /// </summary>
    public List<string> Audience { get; set; } = [];

    /// <summary>
    /// How long Access token can be active.
    /// Recommended: 15 minutes
    /// </summary>
    public double TokenExpirationMinutes { get; set; }

    /// <summary>
    /// How long Refresh token can be active.
    /// Recommended: 6 days
    /// </summary>
    public double RefreshTokenExpirationDays { get; set; }

    /// <summary>
    /// How long Otp can be active
    /// Recommended: 10 minutes.
    /// </summary>
    public double OtpExpirationMinutes { get; set; }

    /// <summary>
    /// How long need to wait before sending the new OTP.
    /// Recommended: 60 seconds
    /// </summary>
    public double OtpResendCooldownSeconds { get; set; }

    /// <summary>
    /// How many active session are allow per valid user.
    /// Recommended: 5
    /// </summary>
    public int MaxSessions { get; set; }

    /// <summary>
    /// How many wrong OTP guesses are allowed before the OTP is invalidated.
    /// Recommended: 5
    /// </summary>
    public int OtpMaxFailedAttempts { get; set; }

    /// <summary>
    /// How many wrong invite token attempts are allowed before the invite is invalidated.
    /// Primarily protects against enumeration rather than brute-force
    /// (tokens are 32-byte random hex — brute-force is not feasible).
    /// Recommended: 5
    /// </summary>
    public int InviteMaxFailedAttempts { get; set; }
}
