using ChatarPatar.Common.AppExceptions.CustomExceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UAParser;

namespace ChatarPatar.Common.HttpUserDetails;

public static class UserInfoSetup
{
    public static string GetUserEmail(this HttpContext _httpContext)
    {
        return _httpContext?.User?.GetUserEmail() ?? string.Empty;
    }

    public static string GetUserName(this HttpContext _httpContext)
    {
        return _httpContext?.User?.GetUserName() ?? string.Empty;
    }

    public static string GetProfilePhoto(this HttpContext _httpContext)
    {
        return _httpContext?.User?.GetProfilePhoto() ?? string.Empty;
    }

    public static string GetUserId(this HttpContext _httpContext)
    {
        return _httpContext?.User?.GetUserId() ?? string.Empty;
    }

    public static string GetUserRole(this HttpContext _httpContext)
    {
        return _httpContext?.User?.GetUserRole() ?? string.Empty;
    }

    public static string GetOriginBaseURL(this HttpContext _httpContext)
    {
        string? originUrl = _httpContext?.Request.Headers["Origin"];

        if (string.IsNullOrWhiteSpace(originUrl))
            throw new AppException("Error receiving while Return URL from 'Origin' HTTP Header");

        return originUrl;
    }

    public static string GetBaseURL(this HttpContext _httpContext)
    {
        return $"{_httpContext?.Request.Scheme}://{_httpContext?.Request.Host.ToString()}";
    }

    public static string GetUserAgent(this HttpContext _httpContext)
    {
        return _httpContext.Request.Headers["User-Agent"].ToString() ?? "Unknown";
    }

    public static (string Browser, string Device, string OS) GetDeviceInfo(this HttpContext _httpContext)
    {
        var userAgent = _httpContext.Request.Headers["User-Agent"].ToString();

        if (string.IsNullOrWhiteSpace(userAgent))
            return ("Unknown", "Unknown", "Unknown");

        var parser = Parser.GetDefault();
        var clientInfo = parser.Parse(userAgent);

        var browser = clientInfo.UA.Family;
        var device = clientInfo.Device.Family;
        var os = clientInfo.OS.Family;

        return (browser, device, os);
    }

    public static string GetClientIp(this HttpContext _httpContext)
    {
        var ip = _httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrEmpty(ip))
            return ip.Split(',').First();

        return _httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
