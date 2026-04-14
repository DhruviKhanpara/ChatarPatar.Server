using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.User;

namespace ChatarPatar.Application.ServiceContracts;

public interface IUserService
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

    Task<AuthUserDto> GetCurrentUserAsync();
    Task<T> GetUserProfileAsync<T>(Guid? userId = null) where T : class;
    Task UpdateUserAsync(UserUpdateDto model);
    Task UpdateAvatarAsync(ImageUploadDto dto);
}
