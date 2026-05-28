using Asp.Versioning;
using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.File;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/files")]
[Authorize]
public class FileController : ControllerBase
{
    private readonly IServiceManager _services;

    public FileController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Pre-uploads a message attachment to Cloudinary.
    /// Call this when the user selects a file in the message composer — before the message is sent.
    /// </summary>
    [HttpPost("attachments")]
    [RequirePermission(Common.Enums.PermissionCheckLogicEnum.Any, Permissions.MESSAGE_SEND, Permissions.MESSAGE_THREAD_REPLY)]
    public async Task<ActionResult<UploadAttachmentResponseDto>> UploadAttachment([FromForm] UploadAttachmentDto dto)
    {
        var result = await _services.FileService.UploadAttachmentAsync(dto);
        return Ok(result);
    }
}
