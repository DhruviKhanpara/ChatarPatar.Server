namespace ChatarPatar.Common.AppLogging.Model.LogRequest;

public class ChangeRecord
{
    public object? Before { get; set; } = null;
    public object? After { get; set; } = null;

    public ChangeRecord() { }

    public ChangeRecord(object? before, object? after)
    {
        Before = before;
        After = after;
    }
}
