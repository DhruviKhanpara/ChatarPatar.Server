using ChatarPatar.Common.AppLogging.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ChatarPatar.Common.AppLogging.Model.LogRequest;

public class AuditLogRequest
{
    public EntityEntry? SourceEntity { get; set; } = null;
    public string TableName { get; set; }
    public Guid? RecordId { get; set; }
    public ChangeRecord ChangeRecord { get; set; }
    public EntityState ChangeState { get; set; }
    public AuditLogTypes LogType { get; set; } = AuditLogTypes.RowChange;

    // Only set for BulkEvent entries
    public string? EventName { get; set; }

    public AuditLogRequest(string tableName, Guid? recordId, object? before, object? after, EntityState changeState)
    {
        TableName = tableName;
        RecordId = recordId;
        ChangeState = changeState;
        LogType = AuditLogTypes.RowChange;

        if (ChangeState == EntityState.Added) before = null;
        if (ChangeState == EntityState.Deleted) after = null;

        ChangeRecord = new ChangeRecord(before, after);
    }

    public AuditLogRequest(EntityEntry entity) : this
    (
        entity.Metadata.GetTableName() ?? "Unknown Table",
        entity.GetNullableGuidFromProperty("Id"),
        entity.OriginalValues.ToObject(),
        entity.CurrentValues.ToObject(),
        entity.State
    )
    {
        SourceEntity = entity;
    }

    /// <summary>
    /// Use for bulk/cascade operations where one human action affects N rows.
    /// Produces a single audit entry instead of N row-level entries.
    /// </summary>
    public AuditLogRequest(string tableName, string eventName, object payload)
    {
        TableName = tableName;
        EventName = eventName;
        LogType = AuditLogTypes.BulkEvent;
        ChangeState = EntityState.Modified; // neutral — not used for BulkEvent
        RecordId = null;
        ChangeRecord = new ChangeRecord(before: null, after: payload);
    }
}
