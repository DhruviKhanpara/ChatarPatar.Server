namespace ChatarPatar.Common.Consts;

public static class FieldLengths
{
    public static class UserFields
    {
        public const int Email = 320;
        public const int Username = 100;
        public const int Name = 150;
        public const int PasswordHash = 512;
        public const int Bio = 500;
        public const int PasswordMin = 8;
        public const int PasswordMax = 200;
    }

    public static class FileFields
    {
        public const int PublicId = 512;
        public const int Url = 1024;
        public const int ThumbnailUrl = 1024;
        public const int FileType = 50;
        public const int UsageContext = 50;
        public const int MimeType = 100;
        public const int OriginalName = 255;
    }

    public static class OrganizationFields
    {
        public const int Name = 200;
        public const int Slug = 100;
        public const int Role = 50;
    }

    public static class TeamFields
    {
        public const int Name = 200;
        public const int Description = 500;
        public const int Role = 50;
    }

    public static class ChannelFields
    {
        public const int Name = 100;
        public const int Description = 500;
        public const int Type = 20;
        public const int Role = 50;
    }

    public static class ConversationFields
    {
        public const int Name = 150;
        public const int Type = 20;
        public const int Role = 50;
    }

    public static class MessageFields
    {
        public const int Content = 4000;
        public const int DmStatus = 20;
        public const int Emoji = 50;
        public const int ContentSnapshot = 500;
    }

    public static class NotificationFields
    {
        public const int Preview = 256;
    }

    public static class RefreshTokenFields
    {
        public const int TokenLength = 512;
        public const int DeviceLength = 255;
        public const int BrowserLength = 255;
        public const int OperatingSystemLength = 255;
        public const int IPAddressLength = 64;
    }
}
