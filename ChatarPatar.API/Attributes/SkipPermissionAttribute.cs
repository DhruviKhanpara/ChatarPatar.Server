namespace ChatarPatar.API.Attributes;

/// <summary>
/// Marks an endpoint as intentionally exempt from the global permission gate.
///
/// Use this ONLY on endpoints that are genuinely public or handle their own
/// auth internally (login, register, refresh-token, forgot/reset-password).
///
/// Every other endpoint must carry either:
///   [RequirePermission(...)]  — for endpoints that need a specific permission, or
///   [SkipPermission]          — for endpoints that are open by design.
///
/// If neither attribute is present the global PermissionFilter will return 403,
/// so a forgotten annotation is caught at test/review time, not in production.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class SkipPermissionAttribute : Attribute { }