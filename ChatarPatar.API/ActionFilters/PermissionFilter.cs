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

        if (!context.ActionArguments.TryGetValue("orgId", out var orgIdArg) || orgIdArg is not Guid orgId)
        {
            context.Result = new BadRequestObjectResult("Missing orgId.");
            return;
        }

        Guid? teamId = null;
        Guid? channelId = null;
        Guid? conversationId = null;

        var data = GetValue(context, "channelId");
        if (data is Guid chId)
            channelId = chId;

        data = GetValue(context, "teamId");
        if (data is Guid tId)
            teamId = tId;

        data = GetValue(context, "conversationId");
        if (data is Guid covId)
            conversationId = covId;

        var permissionContext = new PermissionContext(
            userId,
            orgId,
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

    private object? GetValue(ActionExecutingContext context, string key)
    {
        if (context.ActionArguments.TryGetValue(key, out var val))
            return val;

        if (context.RouteData.Values.TryGetValue(key, out var routeVal))
            return routeVal;

        return null;
    }

    #endregion
}
