namespace ChatarPatar.Common.Security.SecurityContracts;

public interface ITokenService
{
    string CreateToken(string email, Guid id, string name);
    string GenerateRefreshToken();
    string HashToken(string token);
}
