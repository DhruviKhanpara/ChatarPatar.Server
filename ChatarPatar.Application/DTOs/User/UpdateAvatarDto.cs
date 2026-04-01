using Microsoft.AspNetCore.Http;

namespace ChatarPatar.Application.DTOs.User;

public class UpdateAvatarDto
{
    public IFormFile AvatarFile { get; set; } = null!;
}
