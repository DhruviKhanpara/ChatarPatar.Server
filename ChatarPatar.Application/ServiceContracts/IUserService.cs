using ChatarPatar.Application.DTOs.User;

namespace ChatarPatar.Application.ServiceContracts;

public interface IUserService
{
    Task<LoginResponseDto> LoginUserAsync(UserLoginDto user);
    Task<LoginResponseDto> RegisterUserAsync(UserRegisterDto user);
    Task<LoginResponseDto> RefreshAuthToken();
    Task LogoutUser();
    Task RevokeAllUserSessions(Guid? userId = null);

    Task<AuthUserDto> GetCurrentUserAsync();
    Task<T> GetUserProfileAsync<T>(Guid? userId = null) where T : class;
    Task UpdateUserAsync(UserUpdateDto model);
    Task UpdateAvatarAsync(UpdateAvatarDto dto);
}
