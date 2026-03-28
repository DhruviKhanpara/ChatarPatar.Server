namespace ChatarPatar.Common.Security.SecurityContracts;

public interface ITokenService
{
    string CreateToken(string email, Guid id, string name);
    string GenerateRefreshToken();
    string GenerateInviteToken();
    DateTime GetInviteExpiresAt();
    string HashToken(string token);
}
