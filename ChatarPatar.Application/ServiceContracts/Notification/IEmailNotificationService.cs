namespace ChatarPatar.Application.ServiceContracts.Notification;

public interface IEmailNotificationService
{
    /// <summary>
    /// Sends an org invite email using the OrgInvite notification template.
    /// </summary>
    Task SendOrgInviteAsync(string toEmail, string orgName, string inviterName, string roleName, string inviteToken, int expiryDays);

    /// <summary>
    /// Sends a forgot-password OTP email using the ForgotPassword notification template.
    /// </summary>
    Task SendForgotPasswordOtpAsync(string toEmail, string userName, string otp, double expiryMinutes);

    /// <summary>
    /// Sends a security alert of password change using the PasswordChangedAlert notification template.
    /// </summary>
    Task SendPasswordChangedAlertAsync(string toEmail, string userName, string device, string location);
}
