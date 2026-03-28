namespace ChatarPatar.Common.Consts;

public static class ValidationConstants
{
    public static class User
    {
        public static class Lengths
        {
            public const int Email = 320;
            public const int Username = 100;
            public const int UsernameMin = 5;
            public const int Name = 150;
            public const int PasswordHash = 512;
            public const int Bio = 500;
            public const int PasswordMin = 8;
            public const int PasswordMax = 200;
        }

        public static class Patterns
        {
            public const string UserName = "^[a-z0-9_]+$";
            public const string HasUppercase = @"[A-Z]";
            public const string HasLowercase = @"[a-z]";
            public const string HasNumber = @"[0-9]";
            public const string HasSpecialChar = @"[@$!%*?&]";
        }
    }

    public static class File
    {
        public static class Lengths
        {
            public const int PublicId = 512;
            public const int Url = 1024;
            public const int ThumbnailUrl = 1024;
            public const int FileType = 50;
            public const int UsageContext = 50;
            public const int MimeType = 100;
            public const int OriginalName = 255;
        }
    }

    public static class Organization
    {
        public static class Lengths
        {
            public const int Name = 200;
            public const int Slug = 100;
            public const int Role = 50;
            public const int Email = 320;
            public const int Token = 512;
        }

        public static class Patterns
        {
            public const string Name = "^[a-zA-Z0-9 ]+$";
            public const string Slug = "^[a-z0-9]+(-[a-z0-9]+)*$";
        }
    }

    public static class Team
    {
        public static class Lengths
        {
            public const int Name = 200;
            public const int Description = 500;
            public const int Role = 50;
        }
    }

    public static class Channel
    {
        public static class Lengths
        {
            public const int Name = 100;
            public const int Description = 500;
            public const int Type = 20;
            public const int Role = 50;
        }
    }

    public static class Conversation
    {
        public static class Lengths
        {
            public const int Name = 150;
            public const int Type = 20;
            public const int Role = 50;
        }
    }

    public static class Message
    {
        public static class Lengths
        {
            public const int Content = 4000;
            public const int DmStatus = 20;
            public const int Emoji = 50;
            public const int ContentSnapshot = 500;
        }        
    }

    public static class Notification
    {
        public static class Lengths
        {
            public const int Preview = 256;
        }
    }

    public static class RefreshToken
    {
        public static class Lengths
        {
            public const int TokenLength = 512;
            public const int DeviceLength = 255;
            public const int BrowserLength = 255;
            public const int OperatingSystemLength = 255;
            public const int IPAddressLength = 64;
        }
    }
}
