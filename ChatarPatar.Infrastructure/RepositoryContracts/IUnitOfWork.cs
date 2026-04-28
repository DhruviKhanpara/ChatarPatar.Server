using ChatarPatar.Common.AppLogging.Model.LogRequest;
using Microsoft.EntityFrameworkCore.Storage;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IUnitOfWork
{
    int SaveChanges();
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves changes to DB WITHOUT writing audit logs.
    /// Use this for intermediate SaveChanges calls inside an explicit transaction.
    /// Call FlushPendingAuditLogs() after CommitAsync() so logs are only written
    /// when the full transaction has successfully committed.
    /// </summary>
    Task<int> SaveChangesWithoutAuditAsync(CancellationToken cancellationToken = default);

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
}
