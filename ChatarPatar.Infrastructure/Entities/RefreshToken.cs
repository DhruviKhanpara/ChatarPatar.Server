using ChatarPatar.Infrastructure.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatarPatar.Infrastructure.Entities;

public class RefreshToken : BaseEntity
{
    #region Table References
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    #endregion

    public Guid UserId { get; set; }
    public string Token { get; set; } = default!;
    public string? Device { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? IPAddress { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
