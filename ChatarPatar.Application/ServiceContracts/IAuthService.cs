using ChatarPatar.Application.DTOs.User;

namespace ChatarPatar.Application.ServiceContracts;

public interface IAuthService
{
    Task<LoginResponseDto> LoginUserAsync(UserLoginDto user);
    Task<LoginResponseDto> RegisterUserAsync(UserRegisterDto user);
    Task<LoginResponseDto> RefreshAuthToken();
    Task LogoutUser();
    Task LogoutAllUserSessions(Guid? userId = null);

    Task ForgotPasswordAsync(ForgotPasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);

    Task VerifyEmailAsync(VerifyEmailDto dto);
    Task ResendVerificationOtpAsync();
}
