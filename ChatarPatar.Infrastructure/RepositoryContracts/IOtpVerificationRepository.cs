using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IOtpVerificationRepository
{
    Task AddAsync(OtpVerification entity);

    /// <summary>
    /// Returns the latest active (unused, non-expired) OTP for a user + purpose.
    /// </summary>
    IQueryable<OtpVerification> GetActiveOtp(Guid userId, OtpPurposeEnum purpose);

    /// <summary>
    /// Returns all unused, non-expired OTPs for a user + purpose (for invalidation before issuing a new one).
    /// </summary>
    IQueryable<OtpVerification> GetAllActiveOtps(Guid userId, OtpPurposeEnum purpose);

    /// <summary>
    /// Returns the most recently created OTP for a user + purpose regardless of IsUsed/expiry.
    /// Used to enforce resend cooldown — check CreatedAt of the last issued OTP.
    /// </summary>
    IQueryable<OtpVerification> GetLatestOtp(Guid userId, OtpPurposeEnum purpose);
}
