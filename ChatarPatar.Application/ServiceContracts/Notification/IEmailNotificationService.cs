namespace ChatarPatar.Application.ServiceContracts.Notification;

public interface IEmailNotificationService
{
    Task SendOrgInviteAsync(string toEmail, string orgName, string inviteToken);
}
