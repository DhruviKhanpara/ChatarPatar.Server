namespace ChatarPatar.Common.Enums;

/// <summary>
/// IMPORTANT:
/// These enum values are persisted in the database using `.ToString()`.
/// 
/// Do NOT rename these values unless you also update the corresponding
/// database records and constraints.
///
/// Changing names without DB sync will break data consistency.
/// </summary>
public enum OtpPurposeEnum
{
    PasswordReset = 1,
    EmailVerification = 2,
}
