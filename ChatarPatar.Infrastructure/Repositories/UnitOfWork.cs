using ChatarPatar.Common.AppLogging.Extensions.LoggerExtensions;
using ChatarPatar.Common.AppLogging.Model.LogRequest;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Infrastructure.Entities.Common;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Infrastructure.Repositories;

internal class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(AppDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    // Holds audit log entries collected during SaveChangesWithoutAuditAsync calls.
    // Flushed only after CommitAsync() succeeds via FlushPendingAuditLogs().
    private readonly List<AuditLogRequest> _pendingAuditLogs = new();

    // ── Normal path (no explicit transaction) ────────────────────────────────
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

    // ── Transactional path ────────────────────────────────────────────────────
    // Use SaveChangesWithoutAuditAsync for each intermediate SaveChanges inside
    // an explicit transaction, then call FlushPendingAuditLogs() after CommitAsync.
    // This guarantees audit logs are never written for rolled-back transactions.

    public async Task<int> SaveChangesWithoutAuditAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditInEntityBeforeSave();

        // Collect audit entries BEFORE SaveChanges (while change tracker still has states)
        var entries = CollectAuditEntries();

        var result = await _context.SaveChangesAsync(cancellationToken);

        // Resolve the DB-generated Ids into the collected entries (same as SaveAuditLogRequests does)
        foreach (var logRequest in entries.Where(x => x.ChangeState == EntityState.Added))
        {
            if (logRequest.SourceEntity != null)
            {
                var entry = _context.Entry(logRequest.SourceEntity.Entity);
                var postSave = new AuditLogRequest(entry);
                logRequest.RecordId = postSave.RecordId;
                logRequest.ChangeRecord.After = postSave.ChangeRecord.After;
            }
        }

        // Hold them — do NOT write to Serilog yet
        _pendingAuditLogs.AddRange(entries);

        return result;
    }

    public void FlushPendingAuditLogs()
    {
        if (_pendingAuditLogs.Count == 0) return;

        _logger.WriteAuditLog(_pendingAuditLogs);
        _pendingAuditLogs.Clear();
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _context.Database.BeginTransactionAsync(cancellationToken);

    #region Private Section

    private void UpdateAuditInEntityBeforeSave()
    {
        var isLogin = Guid.TryParse(_httpContextAccessor.HttpContext?.GetUserId(), out Guid authUserId);

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
                // Only set CreatedBy if not already assigned.
                if (entity.CreatedBy is null)
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

    private List<AuditLogRequest> CollectAuditEntries()
    {
        return _context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added
                     || e.State == EntityState.Modified
                     || e.State == EntityState.Deleted)
            .Select(e => new AuditLogRequest(e))
            .ToList();
    }

    #endregion
}
