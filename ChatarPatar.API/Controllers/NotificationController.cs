using Asp.Versioning;
using ChatarPatar.API.Attributes;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Notification;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly IServiceManager _services;

    public NotificationController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Returns a paged list of the calling user's notifications.
    /// Unread notifications appear first, then ordered by newest.
    /// </summary>
    [HttpGet]
    [SkipPermission]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetNotifications([FromQuery] PaginationParams paginationParams)
    {
        var result = await _services.NotificationService.GetNotificationsAsync(paginationParams);
        return Ok(result);
    }

    /// <summary>
    /// Returns the count of unread notifications for the calling user.
    /// Use this to drive the badge on the notification bell.
    /// </summary>
    [HttpGet("unread-count")]
    [SkipPermission]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount()
    {
        var result = await _services.NotificationService.GetUnreadCountAsync();
        return Ok(result);
    }

    /// <summary>
    /// Marks a single notification as read.
    /// </summary>
    [HttpPost("{notificationId:guid}/read")]
    [SkipPermission]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid notificationId)
    {
        await _services.NotificationService.MarkAsReadAsync(notificationId);
        return Ok("Notification marked as read.");
    }

    /// <summary>
    /// Marks all notifications of the calling user as read in a single operation.
    /// </summary>
    [HttpPost("read-all")]
    [SkipPermission]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _services.NotificationService.MarkAllAsReadAsync();
        return Ok("All notifications marked as read.");
    }
}
