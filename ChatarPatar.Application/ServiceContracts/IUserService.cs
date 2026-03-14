using ChatarPatar.Application.DTOs.User;
using Microsoft.AspNetCore.Http;

namespace ChatarPatar.Application.ServiceContracts;

public interface IUserService
{
    Task<LoginResponseDto> LoginUserAsync(UserLoginDto user);
    Task RefreshAuthToken();
    Task LogoutUser();
    Task RevokeAllUserSessions(Guid? userId);
    Task<LoginResponseDto> RegisterUserAsync(UserRegisterDto user);
    Task UpdateAvatarAsync(Guid userId, IFormFile file);
}
