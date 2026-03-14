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

        if (context.ActionArguments.TryGetValue("channelId", out var ch))
            channelId = (Guid)ch;

        if (context.ActionArguments.TryGetValue("teamId", out var t))
            teamId = (Guid)t;

        if (context.ActionArguments.TryGetValue("conversationId", out var c))
            conversationId = (Guid)c;

        var permissionContext = new PermissionContext(
            userId,
            orgId,
            teamId,
            channelId,
            conversationId
        );

        var allowed = await _services.PermissionService.HasPermissionAsync(permissionContext, attribute.Permissions);

        if (!allowed)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
