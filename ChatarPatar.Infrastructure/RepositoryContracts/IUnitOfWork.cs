using ChatarPatar.Common.AppLogging.Model.LogRequest;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IUnitOfWork
{
    int SaveChanges();
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to DB WITHOUT immediately writing audit logs.
    /// Use for every SaveChanges inside an explicit transaction.
    ///
    /// suppressRowAudit = false (default):
    ///   Auto-collects row-level RowChange entries from the change tracker and queues them.
    ///   Flush after CommitAsync.
    ///
    /// suppressRowAudit = true:
    ///   Skips row-level collection entirely.
    ///   Use when the caller will queue a single BulkEvent entry via QueueManualAuditLog instead
    /// </summary>
    Task<int> SaveChangesWithoutAuditAsync(bool suppressRowAudit = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually queues a BulkEvent audit entry for operations that bypass the
    /// change tracker (ExecuteUpdate, ExecuteDelete) or that fan out to N rows
    /// from a single user action.
    /// Call before CommitAsync; the entry is flushed with FlushPendingAuditLogs.
    /// </summary>
    void QueueManualAuditLog(AuditLogRequest logRequest);

    /// <summary>
    /// Manually queues an audit log entry that was produced outside of the
    /// change tracker — e.g. from an ExecuteUpdateAsync / ExecuteDeleteAsync
    /// bulk operation. These entries are flushed together with the rest of
    /// the pending audit logs when FlushPendingAuditLogs() is called.
    /// </summary>
    void QueueManualAuditLog(AuditLogRequest logRequest);

    /// <summary>
    /// Writes all audit log entries collected during SaveChangesWithoutAuditAsync.
    /// Call this after CommitAsync() succeeds — never in the catch block.
    /// </summary>
    void FlushPendingAuditLogs();

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detaches an entity from the EF change tracker.
    /// Use after an explicit transaction commit when the entity has a RowVersion
    /// concurrency token, to prevent a subsequent SaveChanges from seeing a stale token.
    /// </summary>
    void DetachEntity<T>(T entity) where T : class;
}
