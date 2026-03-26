namespace ChatarPatar.Application.DTOs.User;

public class LoginResponseDto
{
    // null for web — token is in HttpOnly cookie, JS never touches it
    // populated for mobile — client stores it in secure storage
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public double ExpiresIn { get; set; }
    public string TokenType { get; set; } = null!;
}
