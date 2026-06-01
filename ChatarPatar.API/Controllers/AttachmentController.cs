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
[Route("api/v{version:apiVersion}/attachments")]
[Authorize]
public class AttachmentController : ControllerBase
{
    private readonly IServiceManager _services;

    public AttachmentController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Pre-uploads a channel message attachment to Cloudinary.
    /// Call this when the user selects a file in the message composer — before the message is sent.
    /// </summary>
    [HttpPost("~/api/v{version:apiVersion}/orgs/{orgId:guid}/teams/{teamId:guid}/channels/{channelId:guid}/messages/attachments")]
    [RequirePermission(Common.Enums.PermissionCheckLogicEnum.Any, Permissions.MESSAGE_SEND, Permissions.MESSAGE_THREAD_REPLY)]
    public async Task<ActionResult<UploadAttachmentResponseDto>> UploadChannelAttachment([FromRoute] Guid orgId, [FromRoute] Guid teamId, [FromRoute] Guid channelId, [FromForm] UploadAttachmentDto dto)
    {
        var result = await _services.FileService.UploadChannelAttachmentAsync(orgId, teamId, channelId, dto);
        return Ok(result);
    }

    /// <summary>
    /// Pre-uploads a conversation message attachment to Cloudinary.
    /// Call this when the user selects a file in the message composer — before the message is sent.
    /// </summary>
    [HttpPost("~/api/v{version:apiVersion}/conversations/{conversationId:guid}/messages/attachments")]
    [RequirePermission(Common.Enums.PermissionCheckLogicEnum.Any, Permissions.MESSAGE_SEND, Permissions.MESSAGE_THREAD_REPLY)]
    public async Task<ActionResult<UploadAttachmentResponseDto>> UploadConversationAttachment([FromRoute] Guid conversationId, [FromForm] UploadAttachmentDto dto)
    {
        var result = await _services.FileService.UploadConversationAttachmentAsync(conversationId, dto);
        return Ok(result);
    }

    [HttpDelete("expired")]
    [SkipPermission]
    public async Task<IActionResult> DeleteExpiredPendingAttachments()
    {
        await _services.FileService.DeleteExpiredPendingAttachmentsAsync();
        return Ok("Removed successfully.");
    }
}
