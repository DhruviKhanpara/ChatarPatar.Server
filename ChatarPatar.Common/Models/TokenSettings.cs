namespace ChatarPatar.Common.Models;

public class TokenSettings
{
    public string SecretKey { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public List<string> Audience { get; set; } = [];
    public double TokenExpirationMinutes { get; set; }
    public double RefreshTokenExpirationDays { get; set; }
    public int MaxSessions { get; set; }
}
