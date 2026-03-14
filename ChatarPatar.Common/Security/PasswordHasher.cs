using Microsoft.AspNetCore.Identity;

namespace ChatarPatar.Common.Security;

public static class PasswordHasher
{
    private static readonly PasswordHasher<string> _passwordHasher = new PasswordHasher<string>();

    #region Create Password Hash
    public static string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(string.Empty, password);
    }
    #endregion

    #region Verify Password
    public static bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(string.Empty, hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
    #endregion
}
