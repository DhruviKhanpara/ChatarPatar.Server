using System.Security.Claims;

namespace ChatarPatar.Common.HttpUserDetails;

internal static class ClaimPrincipalExtensions
{
    public static string? GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
    }

    public static string? GetUserName(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Name) ?? principal.FindFirstValue("name");
    }

    public static string? GetProfilePhoto(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("profilePhoto")?.Value;
    }

    public static string? GetUserRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Role) ?? principal.FindFirstValue("role");
    }

    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
