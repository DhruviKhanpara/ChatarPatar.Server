namespace ChatarPatar.Common.Security.SecurityContracts;

public interface IAuthTokenStrategy
{
    string? SetAccessToken(string token);
    string? SetRefreshToken(string token);
    void ClearTokens();
    string? GetRefreshToken();
}
