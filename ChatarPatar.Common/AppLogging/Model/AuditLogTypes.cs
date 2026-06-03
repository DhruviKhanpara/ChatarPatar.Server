namespace ChatarPatar.Common.AppLogging.Model;

public enum AuditLogTypes
{
    /// <summary>
    /// Auto-collected change-tracker entry. Has Before/After snapshot.
    /// </summary>
    RowChange,

    /// <summary>
    /// Manually queued business event (bulk operations, cascade actions).
    /// Has EventName + Payload. Before is always null.
    /// </summary>
    BulkEvent
}
