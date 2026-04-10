using Microsoft.AspNetCore.Http;

namespace ChatarPatar.Application.DTOs.Common;

public class ImageUploadDto
{
    public IFormFile File { get; set; } = null!;
}
