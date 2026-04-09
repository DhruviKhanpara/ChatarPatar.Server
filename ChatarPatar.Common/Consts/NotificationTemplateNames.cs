namespace ChatarPatar.Common.Consts;

/// <summary>
/// Template name constants used to look up rows in the NotificationTemplates table.
/// Each value maps to NotificationTemplate.Name.
/// The same name can exist for multiple TemplateTypes (e.g. OrgInvite for Email + Sms).
/// </summary>
public static class NotificationTemplateNames
{
    public const string OrganizationInvite = "Organization Invite";
    public const string ForgotPassword = "Forgot Password";
    public const string PasswordChangedAlert = "Password Changed Alert";
}