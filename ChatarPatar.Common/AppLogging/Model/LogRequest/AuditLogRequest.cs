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

    public AuditLogRequest(string tableName, Guid? recordId, object? before, object? after, EntityState changeState)
    {
        TableName = tableName;
        RecordId = recordId;
        ChangeState = changeState;

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
}
