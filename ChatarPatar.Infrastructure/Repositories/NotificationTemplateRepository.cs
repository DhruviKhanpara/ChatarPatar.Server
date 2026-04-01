using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly AppDbContext _context;

    public NotificationTemplateRepository(AppDbContext context) => _context = context;

    public async Task<NotificationTemplate> GetByNameAndTypeAsync(string name, NotificationTemplateTypeEnum type)
        => await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Name == name && t.TemplateType == type) ?? throw new NotFoundAppException($"Email template '{name}' not found or inactive.");
}
