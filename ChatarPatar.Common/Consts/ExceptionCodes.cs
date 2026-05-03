namespace ChatarPatar.Common.Consts;

public static class ExceptionCodes
{
    // ── Auth ───────────────────────────────────────────────────
    /// <summary>
    /// No token provided or token is missing entirely.
    /// </summary>
    public const string AUTH_REQUIRED = "AUTH_REQUIRED";

    /// <summary>
    /// Token provided but it has expired.
    /// </summary>
    public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";

    /// <summary>
    /// Token signature is invalid / tampered.
    /// </summary>
    public const string TOKEN_INVALID = "TOKEN_INVALID";

    /// <summary>
    /// Authenticated but missing required permission.
    /// </summary>
    public const string FORBIDDEN = "FORBIDDEN";

    /// <summary>
    /// Endpoint has no permission annotation (developer error).
    /// </summary>
    public const string MISSING_PERMISSION_ANNOTATION = "MISSING_PERMISSION_ANNOTATION";

    // ── Generic domain defaults ────────────────────────────────────────────
    public const string RESOURCE_NOT_FOUND = "RESOURCE_NOT_FOUND";
    public const string DUPLICATE_RESOURCE = "DUPLICATE_RESOURCE";
    public const string INVALID_DATA = "INVALID_DATA";
    public const string VALIDATION_FAILED = "VALIDATION_FAILED";

    // ── User ───────────────────────────────────────────────────────────────
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";

    // ── OTP / Password ─────────────────────────────────────────────────────
    public const string PASSWORD_SAME_AS_CURRENT = "PASSWORD_SAME_AS_CURRENT";

    // ── Refresh token ──────────────────────────────────────────────────────
    public const string REFRESH_TOKEN_INVALID = "REFRESH_TOKEN_INVALID";
    public const string REFRESH_TOKEN_REVOKED = "REFRESH_TOKEN_REVOKED";

    // ── File ───────────────────────────────────────────────────────────────
    public const string FILE_NOT_FOUND = "FILE_NOT_FOUND";
    public const string FILE_TYPE_NOT_ALLOWED = "FILE_TYPE_NOT_ALLOWED";
    public const string FILE_TOO_LARGE = "FILE_TOO_LARGE";

    // ── Fallback ───────────────────────────────────────────────────────────
    public const string UNHANDLED_EXCEPTION = "UNHANDLED_EXCEPTION";
    public const string TIMEOUT = "TIMEOUT";
}
