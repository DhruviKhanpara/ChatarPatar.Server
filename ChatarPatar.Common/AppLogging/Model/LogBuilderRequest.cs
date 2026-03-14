namespace ChatarPatar.Common.AppLogging.Model;

public class LogBuilderRequest
{
    public Dictionary<string, LogBuilderItem> LogToBuild;

    public LogBuilderRequest()
    {
        LogToBuild = new Dictionary<string, LogBuilderItem>();
    }
}
