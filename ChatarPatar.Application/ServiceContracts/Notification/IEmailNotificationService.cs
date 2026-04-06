namespace ChatarPatar.Application.ServiceContracts.Notification;

public interface IEmailNotificationService
{
    /// <summary>
    /// Sends an org invite email using the OrgInvite notification template.
    /// </summary>
    Task SendOrgInviteAsync(string toEmail, string orgName, string inviterName, string roleName, string inviteToken, int expiryDays);
}
