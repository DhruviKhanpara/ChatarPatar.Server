namespace ChatarPatar.Common.EmailNotification.Model;

public class EmailNotificationTemplate
{
    public EmailNotificationTemplate() { }

    public EmailNotificationTemplate(string templateString, Dictionary<string, string> replacements)
    {
        TemplateString = templateString;
        TemplateStringReplacement = replacements;
    }

    public string TemplateString { get; set; } = null!;
    public Dictionary<string, string> TemplateStringReplacement { get; set; } = null!;
}
