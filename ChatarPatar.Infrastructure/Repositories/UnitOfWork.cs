using ChatarPatar.Common.AppLogging.Extensions.LoggerExtensions;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Infrastructure.Entities.Common;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Infrastructure.Repositories;

internal class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly HttpContext? _httpContext;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(AppDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _httpContext = httpContextAccessor?.HttpContext;
        _logger = logger;
    }

    public int SaveChanges()
    {
        UpdateAuditInEntityBeforeSave();

        int result = _context.SaveChangesWithAudit(_logger);

        return result;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditInEntityBeforeSave();

        int result = await _context.SaveChangesAsyncWithAudit(_logger);

        return result;
    }

    private void UpdateAuditInEntityBeforeSave()
    {
        var isLogin = Guid.TryParse(_httpContext?.GetUserId(), out Guid authUserId);

        var entities = _context.ChangeTracker
                .Entries<AuditableEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entities)
        {
            if (!isLogin)
            {
                _logger.LogWarning(
                    "Audit detected write operation without authenticated user. Entity: {Entity}",
                    entry.Entity.GetType().Name
                );
            }

            var entity = entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedBy = isLogin ? authUserId : null;
                entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                var isDeletedProp = entry.Property(nameof(AuditableEntity.IsDeleted));

                if (isDeletedProp.IsModified && entity.IsDeleted)
                {
                    entity.DeletedBy = isLogin ? authUserId : null;
                    entity.DeletedAt = DateTime.UtcNow;
                }
                else
                {
                    entity.UpdatedBy = isLogin ? authUserId : null;
                    entity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
