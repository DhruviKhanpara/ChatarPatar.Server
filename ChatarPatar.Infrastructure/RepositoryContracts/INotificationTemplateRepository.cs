using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface INotificationTemplateRepository
{
    Task<NotificationTemplate> GetByNameAndTypeAsync(string name, NotificationTemplateTypeEnum type);
}
