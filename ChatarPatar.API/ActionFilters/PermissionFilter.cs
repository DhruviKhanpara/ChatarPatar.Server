using ChatarPatar.API.Attributes;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChatarPatar.API.ActionFilters;

public class PermissionFilter : IAsyncActionFilter
{
    private readonly IServiceManager _services;

    public PermissionFilter(IServiceManager services)
    {
        _services = services;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var attribute = context.ActionDescriptor.EndpointMetadata
            .OfType<RequirePermissionAttribute>()
            .FirstOrDefault();

        if (attribute == null)
        {
            await next();
            return;
        }

        var httpContext = context.HttpContext;

        if (!Guid.TryParse(httpContext.GetUserId(), out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var orgId = GetGuid(context, "orgId");
        if (orgId == null)
        {
            context.Result = new BadRequestObjectResult("Missing or invalid orgId.");
            return;
        }

        var teamId = GetGuid(context, "teamId");
        var channelId = GetGuid(context, "channelId");
        var conversationId = GetGuid(context, "conversationId");

        var permissionContext = new PermissionContext(
            userId,
            (Guid)orgId,
            teamId,
            channelId,
            conversationId
        );

        var allowed = await _services.PermissionService.HasPermissionAsync(permissionContext, attribute.Permissions, attribute.Logic);

        if (!allowed)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }

    #region Private section

    private Guid? GetGuid(ActionExecutingContext context, string key)
    {
        if (context.ActionArguments.TryGetValue(key, out var val))
        {
            if (val is Guid g) return g;
            if (val is string s && Guid.TryParse(s, out var parsed)) return parsed;
        }

        if (context.RouteData.Values.TryGetValue(key, out var routeVal))
        {
            if (routeVal is string s && Guid.TryParse(s, out var parsed)) return parsed;
        }

        return null;
    }

    #endregion
}
