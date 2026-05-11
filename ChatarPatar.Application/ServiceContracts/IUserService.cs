using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.User;

namespace ChatarPatar.Application.ServiceContracts;

public interface IUserService
{
    Task<AuthUserDto> GetCurrentUserAsync();
    Task<T> GetUserProfileAsync<T>(Guid? userId = null) where T : class;
    Task UpdateUserAsync(UserUpdateDto model);
    Task UpdateUserAvatarAsync(ImageUploadDto dto);
    Task ChangePasswordAsync(ChangePasswordDto dto);
    Task RemoveUserAvatarAsync();
}
