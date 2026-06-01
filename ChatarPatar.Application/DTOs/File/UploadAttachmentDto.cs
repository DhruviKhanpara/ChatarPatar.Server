using Microsoft.AspNetCore.Http;

namespace ChatarPatar.Application.DTOs.File;

/// <summary>
/// Multipart/form-data payload for the attachment pre-upload endpoint.
/// </summary>
public sealed class UploadAttachmentDto
{
    public IFormFile File { get; set; } = null!;
}