namespace ChatarPatar.Application.DTOs.User;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = null!;
    public double ExpiredIn { get; set; }
    public string TokenType { get; set; } = null!;
}
