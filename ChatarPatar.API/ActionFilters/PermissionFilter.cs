using ChatarPatar.API.Attributes;
using ChatarPatar.API.Models;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Consts;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Net;

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
        var metadata = context.ActionDescriptor.EndpointMetadata;

        // ── Branch 1: [SkipPermission] — explicitly public endpoint ──────────
        // Developer has consciously marked this endpoint as open.
        // Pass straight through with no further checks.
        var skipAttr = metadata.OfType<SkipPermissionAttribute>().FirstOrDefault();
        if (skipAttr != null)
        {
            await next();
            return;
        }

        // ── Branch 2: [RequirePermission(...)] — specific permission required ─
        // Validate the caller holds the declared permission(s).
        var requireAttr = metadata.OfType<RequirePermissionAttribute>().FirstOrDefault();
        if (requireAttr != null)
        {
            await ExecutePermissionCheckAsync(context, next, requireAttr);
            return;
        }

        // ── Branch 3: No annotation at all ───────────────────────────────────
        // The developer forgot to annotate this endpoint.
        // Fail closed — return 403 so this is caught during development/review,
        // not silently left open in production.
        await WriteResponseAsync(
            context.HttpContext,
            HttpStatusCode.Forbidden,
            ExceptionCodes.MISSING_PERMISSION_ANNOTATION,
            "This endpoint has no permission annotation. Add [RequirePermission(...)] or [SkipPermission].");

        context.Result = new EmptyResult();
    }

    #region Private section

    private async Task ExecutePermissionCheckAsync(ActionExecutingContext context, ActionExecutionDelegate next, RequirePermissionAttribute attribute)
    {
        var httpContext = context.HttpContext;

        // Must be authenticated — no JWT/cookie means no userId in claims
        if (!Guid.TryParse(httpContext.GetUserId(), out var userId))
        {
            await WriteResponseAsync(
                httpContext,
                HttpStatusCode.Unauthorized,
                ExceptionCodes.AUTH_REQUIRED,
                "Authentication is required to access this resource.");
            context.Result = new EmptyResult();
            return;
        }

        var orgId = GetGuid(context, "orgId");
        var conversationId = GetGuid(context, "conversationId");

        // At least one scope is required — org-scoped or conversation-scoped
        if (orgId == null && conversationId == null)
        {
            await WriteResponseAsync(
                httpContext,
                HttpStatusCode.BadRequest,
                ExceptionCodes.INVALID_DATA,
                "Missing or invalid orgId.");
            context.Result = new EmptyResult();
            return;
        }

        var teamId = GetGuid(context, "teamId");
        var channelId = GetGuid(context, "channelId");

        var permissionContext = new PermissionContext(
            userId,
            orgId,
            teamId,
            channelId,
            conversationId
        );

        var allowed = await _services.PermissionService
            .HasPermissionAsync(permissionContext, attribute.Permissions, attribute.Logic);

        if (!allowed)
        {
            await WriteResponseAsync(
                httpContext,
                HttpStatusCode.Forbidden,
                ExceptionCodes.FORBIDDEN,
                "You do not have permission to perform this action.");
            context.Result = new EmptyResult();
            return;
        }

        await next();
    }

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

    // Mirrors ExceptionHandlingMiddleware.WriteApiResponseAsync
    private static async Task WriteResponseAsync(HttpContext httpContext, HttpStatusCode statusCode, string exceptionCode, string message)
    {
        httpContext.Items["ExceptionCode"] = exceptionCode;
        httpContext.Items["StatusMessage"] = message;

        var response = new ApiResponse(statusCode, exceptionCode, result: null, statusMessage: message);
        var json = JsonConvert.SerializeObject(response);

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(json);
    }

    #endregion
}
