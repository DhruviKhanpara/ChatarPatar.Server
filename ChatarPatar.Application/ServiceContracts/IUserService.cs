using ChatarPatar.Application.DTOs.User;
using Microsoft.AspNetCore.Http;

namespace ChatarPatar.Application.ServiceContracts;

public interface IUserService
{
    Task<LoginResponseDto> LoginUserAsync(UserLoginDto user);
    Task<LoginResponseDto> RegisterUserAsync(UserRegisterDto user);
    Task<LoginResponseDto> RefreshAuthToken();
    Task LogoutUser();
    Task RevokeAllUserSessions(Guid? userId = null);

    Task<AuthUserDto> GetCurrentUserAsync();
    Task UpdateAvatarAsync(Guid userId, IFormFile file);
}
