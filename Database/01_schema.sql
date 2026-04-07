-- ============================================================
--  TEAMS-LIKE APP — SQL SERVER SCHEMA
--  Scope: Users, Orgs, Teams, Channels, Conversations,
--         Messages, Reactions, Attachments, Files, RBAC
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE ChatarPatar;
GO

-- ============================================================================
-- SECTION 0: CREATE SCHEMAS
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'logging')
BEGIN
    EXEC('CREATE SCHEMA [logging] AUTHORIZATION [dbo];');
END
GO

-- ══════════════════════════════════════════════════════════════
--  SECTION 1 — USERS
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'Users' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE Users (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        Email             NVARCHAR(254)       NOT NULL,
        Username          NVARCHAR(100)       NOT NULL,
        Name              NVARCHAR(150)       NOT NULL,
        PasswordHash      NVARCHAR(512)       NOT NULL,
        AvatarFileId      UNIQUEIDENTIFIER    NULL,         -- FK → Files (set after Files table)
        Bio               NVARCHAR(500)       NULL,
        IsEmailVerified   BIT                 NOT NULL DEFAULT 0,
    	IsDeleted         BIT                 NOT NULL DEFAULT 0,
        CreatedAt         DATETIME2			  NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt         DATETIME2			  NOT NULL DEFAULT SYSUTCDATETIME(),
        DeletedBy         UNIQUEIDENTIFIER    NULL,
        DeletedAt         DATETIME2           NULL,

        CONSTRAINT PK_Users PRIMARY KEY (Id),
        CONSTRAINT UQ_Users_Email     UNIQUE (Email),
        CONSTRAINT UQ_Users_Username  UNIQUE (Username),
        CONSTRAINT FK_Users_DeletedBy FOREIGN KEY (DeletedBy) REFERENCES Users(Id) ON DELETE NO ACTION
    );
END
GO

-- ══════════════════════════════════════════════════════════════
--  SECTION 2 — FILES  (central file store — Cloudinary metadata)
--  Placed early because many tables FK into it
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'Files' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE Files (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        UploadedByUserId  UNIQUEIDENTIFIER    NOT NULL,
        -- Cloudinary fields
        PublicId  NVARCHAR(512)     NOT NULL,   -- e.g. "avatars/abc123"
        Url       NVARCHAR(1024)    NOT NULL,   -- full delivery URL
        ThumbnailUrl        NVARCHAR(1024)    NULL,        -- auto-generated thumb
        -- Categorisation
        FileType          NVARCHAR(50)        NOT NULL,   
            -- 'image', // png, jpg, gif, webp, svg
            -- 'video', // mp4, mov, webm
            -- 'audio', // mp3, wav, ogg, m4a
            -- 'document', // pdf, doc, docx, ppt, pptx, xls, xlsx
            -- 'code', // js, ts, py, json, html, css etc.
            -- 'archive', // zip, rar, tar, gz
            -- 'other'
        UsageContext      NVARCHAR(50)        NOT NULL,   -- 'avatar','attachment','org_logo','team_icon', 'conversation_logo'
        MimeType          NVARCHAR(100)       NOT NULL,
        SizeInBytes     BIGINT              NOT NULL,
        OriginalName  NVARCHAR(255)       NOT NULL,
        -- Scope — at most one of these is set (which entity owns this file)
        UserId            UNIQUEIDENTIFIER    NULL,
        OrgId             UNIQUEIDENTIFIER    NULL,
        TeamId            UNIQUEIDENTIFIER    NULL,
        ChannelId         UNIQUEIDENTIFIER    NULL, -- attachment in channel
        ConversationId    UNIQUEIDENTIFIER    NULL, -- attachment in conversation
        -- Soft delete
        IsDeleted         BIT                 NOT NULL DEFAULT 0,
    	CreatedBy		  UNIQUEIDENTIFIER	  NULL,
        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    	UpdatedBy		  UNIQUEIDENTIFIER	  NULL,
        UpdatedAt         DATETIME2           NULL,
    	DeletedBy		  UNIQUEIDENTIFIER	  NULL,
        DeletedAt         DATETIME2           NULL,

        CONSTRAINT PK_Files PRIMARY KEY (Id),
        CONSTRAINT FK_Files_UploadedBy FOREIGN KEY (UploadedByUserId) REFERENCES Users(Id),
    	CONSTRAINT FK_Files_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_Files_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_Files_DeletedBy FOREIGN KEY (DeletedBy) REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_Files_FileType  CHECK (FileType IN ('image','video','audio','document', 'code', 'archive', 'other')),
        CONSTRAINT CK_Files_UsageContext CHECK (UsageContext IN ('avatar','attachment','org_logo','team_icon', 'conversation_logo')),
        CONSTRAINT CK_Files_OnlyOneScope CHECK (
            (
                (CASE WHEN UserId IS NOT NULL THEN 1 ELSE 0 END) +
                (CASE WHEN OrgId IS NOT NULL THEN 1 ELSE 0 END) +
                (CASE WHEN TeamId IS NOT NULL THEN 1 ELSE 0 END) +
                (CASE WHEN ChannelId IS NOT NULL THEN 1 ELSE 0 END) +
                (CASE WHEN ConversationId IS NOT NULL THEN 1 ELSE 0 END)
            ) <= 1
        )
    );

    CREATE INDEX IX_Files_UploadedBy   ON Files(UploadedByUserId);
    CREATE INDEX IX_Files_UsageContext ON Files(UsageContext);
END
GO

-- Now that Files exists, add the FK from Users.AvatarFileId
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Users_AvatarFile' AND parent_object_id = OBJECT_ID('dbo.Users')
)
    ALTER TABLE dbo.Users
        ADD CONSTRAINT FK_Users_AvatarFile
        FOREIGN KEY (AvatarFileId) REFERENCES dbo.Files(Id) ON DELETE SET NULL;
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 3 — ORGANIZATIONS
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'Organizations' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE Organizations (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        Name              NVARCHAR(200)       NOT NULL,
        Slug              NVARCHAR(100)       NOT NULL,   -- URL-safe unique identifier
        LogoFileId        UNIQUEIDENTIFIER    NULL,
    	-- Soft delete
        IsDeleted         BIT                 NOT NULL DEFAULT 0,
    	CreatedBy		  UNIQUEIDENTIFIER	  NULL,
        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedBy         UNIQUEIDENTIFIER    NULL,
        UpdatedAt         DATETIME2           NULL,
    	DeletedBy		  UNIQUEIDENTIFIER	  NULL,
        DeletedAt         DATETIME2           NULL,
        RowVersion        ROWVERSION          NOT NULL,

        CONSTRAINT PK_Organizations       PRIMARY KEY (Id),
        CONSTRAINT UQ_Organizations_Slug  UNIQUE (Slug),
        CONSTRAINT FK_Organizations_Logo  FOREIGN KEY (LogoFileId)       REFERENCES Files(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Organizations_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_Organizations_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_Organizations_DeletedBy FOREIGN KEY (DeletedBy) REFERENCES Users(Id) ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'OrganizationMembers' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE OrganizationMembers (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        OrgId             UNIQUEIDENTIFIER    NOT NULL,
        UserId            UNIQUEIDENTIFIER    NOT NULL,
        -- Role stored as string; resolved to permissions in application layer
        Role              NVARCHAR(50)        NOT NULL DEFAULT 'OrgMember',
        -- 'OrgOwner' | 'OrgAdmin' | 'OrgMember' | 'OrgGuest'
        InvitedByUserId   UNIQUEIDENTIFIER    NULL,
        JoinedAt          DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    	-- Soft delete
    	IsDeleted         BIT                 NOT NULL DEFAULT 0,
    	CreatedBy		  UNIQUEIDENTIFIER	  NULL,
        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    	UpdatedBy		  UNIQUEIDENTIFIER	  NULL,
        UpdatedAt         DATETIME2           NULL,
    	DeletedBy		  UNIQUEIDENTIFIER	  NULL,
        DeletedAt         DATETIME2           NULL,

        CONSTRAINT PK_OrganizationMembers   PRIMARY KEY (Id),
        CONSTRAINT UQ_OrgMembers_OrgUser     UNIQUE (OrgId, UserId, IsDeleted),
        CONSTRAINT FK_OrgMembers_Org         FOREIGN KEY (OrgId)            REFERENCES Organizations(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_OrgMembers_User        FOREIGN KEY (UserId)           REFERENCES Users(Id)         ON DELETE NO ACTION,
        CONSTRAINT FK_OrgMembers_InvitedBy   FOREIGN KEY (InvitedByUserId)  REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_OrgMembers_CreatedBy	 FOREIGN KEY (CreatedBy)		REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_OrgMembers_UpdatedBy	 FOREIGN KEY (UpdatedBy)		REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_OrgMembers_DeletedBy	 FOREIGN KEY (DeletedBy)		REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_OrgMembers_Role        CHECK (Role IN ('OrgOwner','OrgAdmin','OrgMember','OrgGuest'))
    );

    CREATE INDEX IX_OrgMembers_OrgId  ON OrganizationMembers(OrgId);
    CREATE INDEX IX_OrgMembers_UserId ON OrganizationMembers(UserId);
    CREATE UNIQUE INDEX UX_OrgMembers_Active ON OrganizationMembers (OrgId, UserId) WHERE IsDeleted = 0;
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 4 — TEAMS
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'Teams' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE Teams (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        OrgId             UNIQUEIDENTIFIER    NOT NULL,
        Name              NVARCHAR(200)       NOT NULL,
        Description       NVARCHAR(500)       NULL,
        IconFileId        UNIQUEIDENTIFIER    NULL,
        IsPrivate         BIT                 NOT NULL DEFAULT 0,  -- private teams hidden from non-members
        IsArchived        BIT                 NOT NULL DEFAULT 0,
    	ArchivedAt		  DATETIME2			  NULL,
    	ArchivedBy  UNIQUEIDENTIFIER    NULL,
        -- Soft delete
    	IsDeleted         BIT                 NOT NULL DEFAULT 0,
    	CreatedBy		  UNIQUEIDENTIFIER	  NULL,
        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    	UpdatedBy		  UNIQUEIDENTIFIER	  NULL,
        UpdatedAt         DATETIME2           NULL,
    	DeletedBy		  UNIQUEIDENTIFIER	  NULL,
        DeletedAt         DATETIME2           NULL,
        RowVersion        ROWVERSION          NOT NULL,

        CONSTRAINT PK_Teams            PRIMARY KEY (Id),
        CONSTRAINT FK_Teams_Org        FOREIGN KEY (OrgId)            REFERENCES Organizations(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Teams_Icon       FOREIGN KEY (IconFileId)       REFERENCES Files(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Teams_Archiver   FOREIGN KEY (ArchivedBy)       REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_Teams_CreatedBy  FOREIGN KEY (CreatedBy)		  REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_Teams_UpdatedBy  FOREIGN KEY (UpdatedBy)		  REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_Teams_DeletedBy  FOREIGN KEY (DeletedBy)		  REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_Teams_ArchiveState CHECK (
            (IsArchived = 0 AND ArchivedAt IS NULL AND ArchivedBy IS NULL)
            OR
            (IsArchived = 1 AND ArchivedAt IS NOT NULL)
        )
    );

    CREATE INDEX IX_Teams_OrgId ON Teams(OrgId);
    CREATE INDEX IX_Teams_Archived ON Teams(OrgId, IsArchived);
    CREATE UNIQUE INDEX UX_Teams_Name ON Teams (OrgId, Name) WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'TeamMembers' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE TeamMembers (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        TeamId            UNIQUEIDENTIFIER    NOT NULL,
        UserId            UNIQUEIDENTIFIER    NOT NULL,
        Role              NVARCHAR(50)        NOT NULL DEFAULT 'TeamMember',
        -- 'TeamAdmin' | 'TeamMember' | 'TeamGuest'
        InvitedByUserId   UNIQUEIDENTIFIER    NULL,
        JoinedAt          DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    	IsMuted			  BIT                 NOT NULL DEFAULT 0,
        -- Soft delete
    	IsDeleted         BIT                 NOT NULL DEFAULT 0,
    	CreatedBy		  UNIQUEIDENTIFIER	  NULL,
        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    	UpdatedBy		  UNIQUEIDENTIFIER	  NULL,
        UpdatedAt         DATETIME2           NULL,
    	DeletedBy		  UNIQUEIDENTIFIER	  NULL,
        DeletedAt         DATETIME2           NULL,

        CONSTRAINT PK_TeamMembers			PRIMARY KEY (Id),
        CONSTRAINT FK_TeamMembers_Team		FOREIGN KEY (TeamId)            REFERENCES Teams(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_TeamMembers_User		FOREIGN KEY (UserId)            REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_TeamMembers_Inviter   FOREIGN KEY (InvitedByUserId)   REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_TeamMembers_CreatedBy	FOREIGN KEY (CreatedBy)			REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_TeamMembers_UpdatedBy	FOREIGN KEY (UpdatedBy)			REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_TeamMembers_DeletedBy	FOREIGN KEY (DeletedBy)			REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_TeamMembers_Role  CHECK (Role IN ('TeamAdmin','TeamMember','TeamGuest'))
    );

    CREATE INDEX IX_TeamMembers_TeamId ON TeamMembers(TeamId);
    CREATE INDEX IX_TeamMembers_UserId ON TeamMembers(UserId);
    CREATE UNIQUE INDEX UX_TeamMembers_Active ON TeamMembers (TeamId, UserId) WHERE IsDeleted = 0;
END
GO

-- ══════════════════════════════════════════════════════════════
--  SECTION 5 — CHANNELS
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'Channels' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE Channels (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        TeamId            UNIQUEIDENTIFIER    NOT NULL,
        OrgId             UNIQUEIDENTIFIER    NOT NULL,   -- denormalised for fast permission checks
        Name              NVARCHAR(100)       NOT NULL,
        Description       NVARCHAR(500)       NULL,
        Type              NVARCHAR(20)        NOT NULL DEFAULT 'Text', -- 'Text' | 'Announcement'
        IsPrivate         BIT                 NOT NULL DEFAULT 0,
        IsArchived        BIT                 NOT NULL DEFAULT 0,
    	ArchivedAt		  DATETIME2			  NULL,
    	ArchivedBy        UNIQUEIDENTIFIER    NULL,
        -- Soft delete
    	IsDeleted         BIT                 NOT NULL DEFAULT 0,
    	CreatedBy		  UNIQUEIDENTIFIER	  NULL,
        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    	UpdatedBy		  UNIQUEIDENTIFIER	  NULL,
        UpdatedAt         DATETIME2           NULL,
    	DeletedBy		  UNIQUEIDENTIFIER	  NULL,
        DeletedAt         DATETIME2           NULL,
        RowVersion        ROWVERSION          NOT NULL,

        CONSTRAINT PK_Channels          PRIMARY KEY (Id),
        CONSTRAINT FK_Channels_Team     FOREIGN KEY (TeamId)          REFERENCES Teams(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Channels_Org      FOREIGN KEY (OrgId)           REFERENCES Organizations(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Channels_Archiver  FOREIGN KEY (ArchivedBy)	  REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Channels_CreatedBy  FOREIGN KEY (CreatedBy)	  REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Channels_UpdatedBy  FOREIGN KEY (UpdatedBy)	  REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Channels_DeletedBy  FOREIGN KEY (DeletedBy)	  REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_Channels_Type     CHECK (Type IN ('Text','Announcement')),
        CONSTRAINT CK_Channels_ArchiveState CHECK (
            (IsArchived = 0 AND ArchivedAt IS NULL AND ArchivedBy IS NULL)
            OR
            (IsArchived = 1 AND ArchivedAt IS NOT NULL)
        )
    );

    CREATE INDEX IX_Channels_TeamId ON Channels(TeamId);
    CREATE INDEX IX_Channels_Archived ON Channels(TeamId, IsArchived);
    CREATE UNIQUE INDEX UX_Channels_Name ON Channels (TeamId, Name) WHERE IsDeleted = 0;
END
GO

-- Only needed for Private channels
-- Public channels → all TeamMembers have implicit access

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'ChannelMembers' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE ChannelMembers (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        ChannelId         UNIQUEIDENTIFIER    NOT NULL,
        UserId            UNIQUEIDENTIFIER    NOT NULL,
        Role              NVARCHAR(50)        NOT NULL DEFAULT 'ChannelMember', -- 'ChannelModerator' | 'ChannelMember' | 'ChannelReadOnly'
        AddedByUserId     UNIQUEIDENTIFIER    NULL,
        JoinedAt          DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    	IsMuted			  BIT                 NOT NULL DEFAULT 0,
    	-- Soft delete
    	IsDeleted         BIT                 NOT NULL DEFAULT 0,
    	CreatedBy		  UNIQUEIDENTIFIER	  NULL,
        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    	UpdatedBy		  UNIQUEIDENTIFIER	  NULL,
        UpdatedAt         DATETIME2           NULL,
    	DeletedBy		  UNIQUEIDENTIFIER	  NULL,
        DeletedAt         DATETIME2           NULL,

        CONSTRAINT PK_ChannelMembers			PRIMARY KEY (Id),
        CONSTRAINT FK_ChannelMembers_Channel	FOREIGN KEY (ChannelId)		REFERENCES Channels(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ChannelMembers_User		FOREIGN KEY (UserId)		REFERENCES Users(Id)    ON DELETE NO ACTION,
        CONSTRAINT FK_ChannelMembers_AddedBy	FOREIGN KEY (AddedByUserId) REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_ChannelMembers_CreatedBy  FOREIGN KEY (CreatedBy)		REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ChannelMembers_UpdatedBy  FOREIGN KEY (UpdatedBy)		REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ChannelMembers_DeletedBy  FOREIGN KEY (DeletedBy)		REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_ChannelMembers_Role   CHECK (Role IN ('ChannelModerator','ChannelMember','ChannelReadOnly'))
    );

    CREATE INDEX IX_ChannelMembers_ChannelId ON ChannelMembers(ChannelId);
    CREATE INDEX IX_ChannelMembers_UserId    ON ChannelMembers(UserId);
    CREATE UNIQUE INDEX UX_ChannelMembers_Active ON ChannelMembers(ChannelId, UserId) WHERE IsDeleted = 0;
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 6 — CONVERSATIONS  (Direct Messages & Group DMs)
--  Separate from Channels — no team/org ownership
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'Conversations' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE Conversations (
        Id                 UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        Type               NVARCHAR(20)        NOT NULL,   -- 'Direct' (1-to-1) | 'Group' (multi-person DM)
        Name               NVARCHAR(150)       NULL,       -- only for Group DMs
        LogoFileId         UNIQUEIDENTIFIER    NULL,
        -- Soft delete
    	IsDeleted         BIT                 NOT NULL DEFAULT 0,
    	CreatedBy		  UNIQUEIDENTIFIER	  NULL,
        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    	UpdatedBy		  UNIQUEIDENTIFIER	  NULL,
        UpdatedAt         DATETIME2           NULL,
    	DeletedBy		  UNIQUEIDENTIFIER	  NULL,
        DeletedAt         DATETIME2           NULL,

        CONSTRAINT PK_Conversations				PRIMARY KEY (Id),
        CONSTRAINT FK_Conversations_Logo        FOREIGN KEY (LogoFileId)    REFERENCES Files(Id) ON DELETE NO ACTION,
    	CONSTRAINT FK_Conversations_CreatedBy	FOREIGN KEY (CreatedBy)		REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Conversations_UpdatedBy	FOREIGN KEY (UpdatedBy)		REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Conversations_DeletedBy	FOREIGN KEY (DeletedBy)		REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_Conversations_Type		CHECK (Type IN ('Direct','Group')),
        CONSTRAINT CK_Conversations_NameRule CHECK (
            (Type = 'Direct' AND Name IS NULL)
            OR
            (Type = 'Group' AND Name IS NOT NULL)
        )
    );
END
GO

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'ConversationParticipants' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE ConversationParticipants (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        ConversationId    UNIQUEIDENTIFIER    NOT NULL,
        UserId            UNIQUEIDENTIFIER    NOT NULL,
        Role              NVARCHAR(50)        NOT NULL DEFAULT 'GroupMember',
        -- 'GroupAdmin' | 'GroupMember'
        -- Soft-left: user left group DM but history preserved
        AddedBy            UNIQUEIDENTIFIER    NOT NULL,
        HasLeft           BIT                 NOT NULL DEFAULT 0,
        LeftAt            DATETIME2           NULL,
        JoinedAt          DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_ConversationParticipants       PRIMARY KEY (Id),
        CONSTRAINT FK_ConvParticipants_Conversation  FOREIGN KEY (ConversationId) REFERENCES Conversations(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ConvParticipants_User          FOREIGN KEY (UserId)         REFERENCES Users(Id)         ON DELETE NO ACTION,
        CONSTRAINT FK_ConvParticipants_AddedBy       FOREIGN KEY (AddedBy)         REFERENCES Users(Id)         ON DELETE NO ACTION,
        CONSTRAINT CK_ConvParticipants_Role   CHECK (Role IN ('GroupAdmin','GroupMember'))
    );

    CREATE INDEX IX_ConvParticipants_ConvId ON ConversationParticipants(ConversationId);
    CREATE INDEX IX_ConvParticipants_UserId ON ConversationParticipants(UserId);
    CREATE UNIQUE INDEX UX_ConvParticipants_Name ON ConversationParticipants(ConversationId, UserId) WHERE HasLeft = 0;
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 7 — MESSAGES
--  Single table handles both Channel messages and DM messages
--  via discriminator columns (ChannelId XOR ConversationId)
-- ══════════════════════════════════════════════════════════════


IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'Messages' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE Messages (
        Id                  UNIQUEIDENTIFIER     NOT NULL DEFAULT NEWSEQUENTIALID(),
        SequenceNumber      BIGINT IDENTITY(1,1) NOT NULL,
        ClientMessageId     UNIQUEIDENTIFIER     NOT NULL,
        -- Source: exactly one of these is set
        ConversationId      UNIQUEIDENTIFIER    NULL,
        ChannelId           UNIQUEIDENTIFIER    NULL,
        -- Sender
        SenderId            UNIQUEIDENTIFIER    NOT NULL,
        -- Threading
        ThreadRootMessageId UNIQUEIDENTIFIER    NULL,       -- set if this is a thread reply
        -- Content
        Content             NVARCHAR(4000)       NULL,       -- NULL if message is attachment-only
        MessageType         TINYINT              NOT NULL,
        -- 1=text
        -- 2=system
        -- 3=file
        -- 4=image
        -- Edit tracking
        IsEdited            BIT                 NOT NULL DEFAULT 0,
        EditedAt            DATETIME2           NULL,
    	-- replies needs to show "5 replies, last reply 2h ago."
    	ReplyCount		    INT				  NOT NULL DEFAULT 0,  -- only meaningful when ThreadId IS NULL
        -- 1-on-1 DM delivery state (NULL for channel messages):
        DmStatus            NVARCHAR(20)        NULL,       -- 'Sending' | 'Sent' | 'Delivered' | 'Seen'
        DmDeliveredAt       DATETIME2           NULL,
        DmSeenAt            DATETIME2           NULL,
    	LastReplyAt         DATETIME2			  NULL,
        -- Soft delete
        IsDeleted           BIT                 NOT NULL DEFAULT 0,
        DeletedBy           UNIQUEIDENTIFIER    NULL,
        DeletedAt           DATETIME2           NULL,
        -- Timestamps
        CreatedAt           DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
        RowVersion          ROWVERSION          NOT NULL,

        CONSTRAINT PK_Messages              PRIMARY KEY (Id),
        CONSTRAINT FK_Messages_Channel      FOREIGN KEY (ChannelId)       REFERENCES Channels(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Messages_Conversation FOREIGN KEY (ConversationId)  REFERENCES Conversations(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Messages_Sender       FOREIGN KEY (SenderId)        REFERENCES Users(Id),
        CONSTRAINT FK_Messages_Thread       FOREIGN KEY (ThreadRootMessageId)        REFERENCES Messages(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Messages_DeletedBy    FOREIGN KEY (DeletedBy)       REFERENCES Users(Id) ON DELETE NO ACTION,
        -- Enforce exactly one source
        CONSTRAINT CK_Messages_Source CHECK (
            (ChannelId IS NOT NULL AND ConversationId IS NULL) OR
            (ChannelId IS NULL AND ConversationId IS NOT NULL)
        ),
        CONSTRAINT CK_Messages_DmStatus CHECK (DmStatus IS NULL OR DmStatus IN ('Sending','Sent','Delivered','Seen')),
        CONSTRAINT CK_Messages_ThreadReplyRule CHECK (
            (ThreadRootMessageId IS NULL) OR (ReplyCount = 0)
        ),  -- ReplyCount only allowed on root messages
        CONSTRAINT CK_Message_MessageType CHECK (
            MessageType BETWEEN 1 AND 4
        )
    );

    CREATE INDEX IX_Messages_ThreadRootMessageId 
    ON Messages(ThreadRootMessageId, CreatedAt)
    WHERE IsDeleted = 0 AND ThreadRootMessageId IS NOT NULL;

    CREATE INDEX IX_Messages_Channel_Active
    ON Messages(ChannelId, SequenceNumber)
    WHERE IsDeleted = 0 AND ChannelId IS NOT NULL;

    CREATE INDEX IX_Messages_Conversation_Active
    ON Messages(ConversationId, SequenceNumber)
    WHERE IsDeleted = 0 AND ConversationId IS NOT NULL;

    CREATE UNIQUE INDEX UX_Messages_Channel_ClientMessage
    ON Messages(ChannelId, SenderId, ClientMessageId)
    WHERE ChannelId IS NOT NULL;

    CREATE UNIQUE INDEX UX_Messages_Conversation_ClientMessage
    ON Messages(ConversationId, SenderId, ClientMessageId)
    WHERE ConversationId IS NOT NULL;

    CREATE INDEX IX_Messages_SenderId_CreatedAt
    ON Messages (SenderId, CreatedAt)
    WHERE IsDeleted = 0;
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 8 — MESSAGE REACTIONS
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'MessageReactions' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE MessageReactions (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        MessageId         UNIQUEIDENTIFIER    NOT NULL,
        UserId            UNIQUEIDENTIFIER    NOT NULL,
        Emoji             NVARCHAR(50)        NOT NULL,   -- e.g. "👍" or ":thumbsup:"
        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_MessageReactions          PRIMARY KEY (Id),
        CONSTRAINT UQ_MessageReactions          UNIQUE (MessageId, UserId, Emoji),  -- one reaction per emoji per user
        CONSTRAINT FK_MessageReactions_Message  FOREIGN KEY (MessageId) REFERENCES Messages(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_MessageReactions_User     FOREIGN KEY (UserId)    REFERENCES Users(Id)    ON DELETE NO ACTION
    );

    CREATE INDEX IX_MessageReactions_MessageId ON MessageReactions(MessageId);
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 9 — MESSAGE ATTACHMENTS
--  Join table between Messages and Files
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'MessageAttachments' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE MessageAttachments (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        MessageId         UNIQUEIDENTIFIER    NOT NULL,
        FileId            UNIQUEIDENTIFIER    NOT NULL,
        DisplayOrder      INT                 NOT NULL DEFAULT 0,   -- ordering within message
        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_MessageAttachments            PRIMARY KEY (Id),
        CONSTRAINT UQ_MessageAttachments_File       UNIQUE (MessageId, FileId),
        CONSTRAINT UQ_MessageAttachments_Order      UNIQUE (MessageId, DisplayOrder),
        CONSTRAINT FK_MessageAttachments_Message    FOREIGN KEY (MessageId) REFERENCES Messages(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_MessageAttachments_File       FOREIGN KEY (FileId)    REFERENCES Files(Id)
    );

    CREATE INDEX IX_MessageAttachments_MessageId ON MessageAttachments(MessageId);
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 10 — MESSAGE RECEIPTS  (group chat delivery state)
--  Per-user delivered/seen state for group conversations (≤20 participants).
--  DMs use DmStatus on Messages instead.
--  Channels do not track per-message delivery — ReadStates handles that.
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'MessageReceipts' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE MessageReceipts (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        MessageId         UNIQUEIDENTIFIER    NOT NULL,
        UserId            UNIQUEIDENTIFIER    NOT NULL,

        DeliveredAt       DATETIME2           NULL,
        SeenAt            DATETIME2           NULL,

        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_MessageReceipts PRIMARY KEY (Id),

        -- One receipt per user per message
        CONSTRAINT UQ_MessageReceipts UNIQUE (MessageId, UserId),

        CONSTRAINT FK_MessageReceipts_Message
            FOREIGN KEY (MessageId)
            REFERENCES Messages(Id)
            ON DELETE NO ACTION,

        CONSTRAINT FK_MessageReceipts_User
            FOREIGN KEY (UserId)
            REFERENCES Users(Id)
            ON DELETE NO ACTION,

        -- Seen must not be earlier than Delivered
        CONSTRAINT CK_MessageReceipts_SeenAfterDelivered CHECK (
            SeenAt IS NULL OR
            DeliveredAt IS NULL OR
            SeenAt >= DeliveredAt
        )
    );

    CREATE INDEX IX_MessageReceipts_Message ON MessageReceipts(UserId, MessageId);
    CREATE INDEX IX_MessageReceipts_User_Seen ON MessageReceipts(UserId, SeenAt);
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 11 — PINNED MESSAGES
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'PinnedMessages' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE PinnedMessages (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        MessageId         UNIQUEIDENTIFIER    NOT NULL,

        -- Scope: pinned in a channel OR a conversation
        ChannelId         UNIQUEIDENTIFIER    NULL,
        ConversationId    UNIQUEIDENTIFIER    NULL,

        PinnedByUserId    UNIQUEIDENTIFIER    NOT NULL,
        PinnedAt          DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

    	UnPinnedByUserId  UNIQUEIDENTIFIER    NULL,
        UnPinnedAt        DATETIME2           NULL,
        RowVersion        ROWVERSION          NOT NULL,

        -- Snapshot so the pin list renders without re-joining Messages
        -- (original may be edited or deleted after pinning)
        ContentSnapshot   NVARCHAR(500)       NULL,

        CONSTRAINT PK_PinnedMessages            PRIMARY KEY (Id),
        CONSTRAINT UQ_PinnedConversationMessages            UNIQUE (MessageId, ConversationId),   -- a message can only be pinned once in conversation
        CONSTRAINT FK_PinnedMessages_Message    FOREIGN KEY (MessageId)          REFERENCES Messages(Id)      ON DELETE NO ACTION,
        CONSTRAINT FK_PinnedMessages_Channel    FOREIGN KEY (ChannelId)          REFERENCES Channels(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_PinnedMessages_Conv       FOREIGN KEY (ConversationId)     REFERENCES Conversations(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_PinnedMessages_PinnedBy   FOREIGN KEY (PinnedByUserId)     REFERENCES Users(Id),
    	CONSTRAINT FK_PinnedMessages_UnPinnedBy FOREIGN KEY (UnPinnedByUserId)   REFERENCES Users(Id),
        CONSTRAINT CK_PinnedMessages_Source CHECK (
            (ChannelId IS NOT NULL AND ConversationId IS NULL) OR
            (ChannelId IS NULL AND ConversationId IS NOT NULL)
        ),
        CONSTRAINT CK_PinnedMessages_UnpinConsistency CHECK (
            (UnPinnedAt IS NULL AND UnPinnedByUserId IS NULL) OR
            (UnPinnedAt IS NOT NULL AND UnPinnedByUserId IS NOT NULL)
        )
    );

    CREATE UNIQUE INDEX UX_Pinned_Channel_Active
    ON PinnedMessages (MessageId, ChannelId)
    WHERE ChannelId IS NOT NULL
    AND UnPinnedAt IS NULL;

    CREATE UNIQUE INDEX UX_Pinned_Conversation_Active
    ON PinnedMessages (MessageId, ConversationId)
    WHERE ConversationId IS NOT NULL
    AND UnPinnedAt IS NULL;

    CREATE INDEX IX_Pinned_Channel_Active
    ON PinnedMessages (ChannelId, PinnedAt)
    WHERE UnPinnedAt IS NULL;

    CREATE INDEX IX_Pinned_Conversation_Active
    ON PinnedMessages (ConversationId, PinnedAt)
    WHERE UnPinnedAt IS NULL;
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 12 — MESSAGE MENTIONS
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'MessageMentions' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE MessageMentions (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        MessageId         UNIQUEIDENTIFIER    NOT NULL,
        MentionedUserId   UNIQUEIDENTIFIER    NOT NULL,

        ChannelId         UNIQUEIDENTIFIER    NULL,         -- denormalized for fast "all my mentions in channel" query
        ConversationId    UNIQUEIDENTIFIER    NULL,

        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_MessageMentions             PRIMARY KEY (Id),
        CONSTRAINT UQ_MessageMentions             UNIQUE (MessageId, MentionedUserId),
        CONSTRAINT FK_MessageMentions_Message     FOREIGN KEY (MessageId)        REFERENCES Messages(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_MessageMentions_User        FOREIGN KEY (MentionedUserId)  REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_MessageMentions_Channel     FOREIGN KEY (ChannelId)        REFERENCES Channels(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_MessageMentions_Conv        FOREIGN KEY (ConversationId)   REFERENCES Conversations(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_MessageMentions_Source CHECK (
            (ChannelId IS NOT NULL AND ConversationId IS NULL) OR
            (ChannelId IS NULL     AND ConversationId IS NOT NULL)
        )
    );

    -- Most common query: "show all messages that mention me in this channel"
    CREATE INDEX IX_MessageMentions_UserChannel  ON MessageMentions(MentionedUserId, ChannelId, CreatedAt);
    CREATE INDEX IX_MessageMentions_UserConv     ON MessageMentions(MentionedUserId, ConversationId, CreatedAt);
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 13 — READ STATE
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'ReadStates' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE ReadStates (
        Id              UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWSEQUENTIALID(),
        UserId          UNIQUEIDENTIFIER  NOT NULL,

        -- Exactly one set:
        ChannelId       UNIQUEIDENTIFIER  NULL,
        ConversationId  UNIQUEIDENTIFIER  NULL,

        -- Counters (never COUNT rows — always maintain these)
        UnreadCount     INT               NOT NULL DEFAULT 0,
        MentionCount    INT               NOT NULL DEFAULT 0,  -- feeds the red badge

        LastReadSequenceNumber  BIGINT            NOT NULL DEFAULT 0,
        LastReadMessageId       UNIQUEIDENTIFIER  NULL,
        LastReadAt              DATETIME2         NULL,

        CreatedAt       DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME(),
        RowVersion      ROWVERSION        NOT NULL,

        CONSTRAINT PK_ReadStates PRIMARY KEY (Id),
        CONSTRAINT FK_ReadStates_User     FOREIGN KEY (UserId)        REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ReadStates_Channel  FOREIGN KEY (ChannelId)     REFERENCES Channels(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ReadStates_Conv     FOREIGN KEY (ConversationId) REFERENCES Conversations(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ReadStates_Message  FOREIGN KEY (LastReadMessageId) REFERENCES Messages(Id),
        CONSTRAINT CK_ReadStates_Source CHECK (
            (ChannelId IS NOT NULL AND ConversationId IS NULL) OR
            (ChannelId IS NULL     AND ConversationId IS NOT NULL)
        ),
        CONSTRAINT CK_ReadStates_Unread_NonNegative CHECK (UnreadCount >= 0),
        CONSTRAINT CK_ReadStates_Mention_NonNegative CHECK (MentionCount >= 0)
    );

    CREATE INDEX IX_ReadStates_User ON ReadStates(UserId);  -- load full sidebar state

    CREATE UNIQUE INDEX UX_ReadStates_User_Channel
    ON ReadStates (UserId, ChannelId)
    WHERE ChannelId IS NOT NULL;

    CREATE UNIQUE INDEX UX_ReadStates_User_Conversation
    ON ReadStates (UserId, ConversationId)
    WHERE ConversationId IS NOT NULL;
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 14 — NOTIFICATIONS
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'Notifications' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE Notifications (
        Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        RecipientId       UNIQUEIDENTIFIER    NOT NULL,

        Type              TINYINT             NOT NULL, 
        -- 1=mention
        -- 2=thread_reply
        -- 3=reaction
        -- 4=dm
        -- 5=group_message
        -- 6=added_to_team
        -- 7=added_to_channel
        -- 8=added_to_group

        MessageId         UNIQUEIDENTIFIER    NULL,
        ChannelId         UNIQUEIDENTIFIER    NULL,
        TeamId            UNIQUEIDENTIFIER    NULL,
        ConversationId    UNIQUEIDENTIFIER    NULL,
        ActorId           UNIQUEIDENTIFIER    NULL,

        -- State
        Preview           NVARCHAR(256)       NULL,       -- truncated message text for toast/push display

        IsRead            BIT                 NOT NULL DEFAULT 0,
        ReadAt            DATETIME2           NULL,

        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Notifications         PRIMARY KEY (Id),
        CONSTRAINT FK_Notifications_User    FOREIGN KEY (RecipientId)            REFERENCES Users(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Notifications_Message FOREIGN KEY (MessageId)         REFERENCES Messages(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Notifications_Channel FOREIGN KEY (ChannelId)         REFERENCES Channels(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Notifications_Team    FOREIGN KEY (TeamId)            REFERENCES Teams(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Notifications_Conv    FOREIGN KEY (ConversationId)    REFERENCES Conversations(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Notifications_Trigger FOREIGN KEY (ActorId) REFERENCES Users(Id),
        CONSTRAINT CK_Notifications_ReadConsistency CHECK (
            (IsRead = 0 AND ReadAt IS NULL) OR
            (IsRead = 1 AND ReadAt IS NOT NULL)
        ),
        CONSTRAINT CK_Notifications_Type CHECK (
            Type BETWEEN 1 AND 8
        )
    );

    CREATE INDEX IX_Notifications_UserId ON Notifications(RecipientId, IsRead, CreatedAt);
END
GO

-- ── TTL CLEANUP (no native TTL in SQL Server) ─────────────────────────────────
-- Schedule the following as a SQL Agent job or Azure Logic App (runs nightly):
--   DELETE FROM Notifications
--   WHERE IsRead = 1 AND ReadAt < DATEADD(DAY, -30, SYSUTCDATETIME());
-- ─────────────────────────────────────────────────────────────────────────────


-- ══════════════════════════════════════════════════════════════
--  SECTION 15 — USER PRESENCE  (online/away/busy/offline)
--  Updated by SignalR hub on connect/disconnect
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'UserStatus' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE UserStatus (
        UserId         UNIQUEIDENTIFIER NOT NULL,

        Status         TINYINT          NOT NULL DEFAULT 0,
        -- 0 = Offline
        -- 1 = Online
        -- 2 = Away

        CustomStatus   TINYINT          NULL,
        -- 1 = active
        -- 2 = busy
        -- 3 = do_not_disturb
        -- 4 = be_right_back
        -- 5 = appear_away
        -- 6 = appear_offline

        LastSeenAt     DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_UserStatus
            PRIMARY KEY (UserId),

        CONSTRAINT FK_UserStatus_User
            FOREIGN KEY (UserId)
            REFERENCES Users(Id)
            ON DELETE NO ACTION,

        CONSTRAINT CK_UserStatus_Status
            CHECK (Status BETWEEN 0 AND 2),

        CONSTRAINT CK_UserStatus_CustomStatus
            CHECK (CustomStatus IS NULL OR CustomStatus BETWEEN 1 AND 6),

        -- Prevent impossible combinations
        CONSTRAINT CK_UserStatus_Logical
            CHECK (
                -- If offline → no custom status
                (Status = 0 AND CustomStatus IS NULL)
                OR
                -- If online/away → custom allowed
                (Status IN (1,2))
            )
    );

    CREATE INDEX IX_UserStatus_Status ON UserStatus (Status);
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 16 — REFERESH TOKEN
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'RefreshTokens' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE RefreshTokens (
    	Id			   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        UserId         UNIQUEIDENTIFIER NOT NULL,
    	Token		   NVARCHAR(512)	NOT NULL,
    	Device         NVARCHAR(255)    NULL,
    	Browser        NVARCHAR(255)    NULL,
    	OperatingSystem NVARCHAR(255)    NULL,
    	IPAddress      NVARCHAR(64)     NULL,
    	ExpiresAt	   DATETIME2		NOT NULL,
    	IsRevoked	   BIT				NOT NULL DEFAULT (0),
        RevokedAt      DATETIME2        NULL,
        CreatedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    	UpdatedAt      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_RefreshTokens         PRIMARY KEY (Id),
        CONSTRAINT FK_RefreshTokens_User    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE NO ACTION,
    	CONSTRAINT CK_RefreshTokens_RevokeConsistency
        CHECK (
            (IsRevoked = 0 AND RevokedAt IS NULL) OR
            (IsRevoked = 1 AND RevokedAt IS NOT NULL)
        )
    );

    CREATE UNIQUE INDEX UX_RefreshTokens_Token
    ON RefreshTokens (Token)
    WHERE IsRevoked = 0;

    CREATE INDEX IX_RefreshToken_ActiveToken ON RefreshTokens (UserId, IsRevoked, ExpiresAt);
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 17 — ORGANIZATION INVITE
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'OrganizationInvites' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE OrganizationInvites (
    	Id              UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID(),
        OrganizationId  UNIQUEIDENTIFIER  NOT NULL,
        CreatedBy       UNIQUEIDENTIFIER  NOT NULL,

        Email           NVARCHAR(254)     NOT NULL,
        Role            NVARCHAR(50)      NOT NULL  DEFAULT 'OrgMember',
        Token           NVARCHAR(512)     NOT NULL,

        IsUsed          BIT               NOT NULL  DEFAULT 0,
        UsedAt          DATETIME2                   DEFAULT NULL,
        UsedBy          UNIQUEIDENTIFIER            DEFAULT NULL,

        ExpiresAt       DATETIME2         NOT NULL,
        CreatedAt       DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_OrganizationInvites
            PRIMARY KEY (Id),

        CONSTRAINT UQ_OrganizationInvites_Token
            UNIQUE (Token),

        CONSTRAINT FK_OrgInvites_Org
            FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id) ON DELETE CASCADE,

        CONSTRAINT FK_OrgInvites_CreatedBy
            FOREIGN KEY (CreatedBy)      REFERENCES Users(Id),

        CONSTRAINT FK_OrgInvites_UsedBy
            FOREIGN KEY (UsedBy)         REFERENCES Users(Id) ON DELETE SET NULL,

        CONSTRAINT CK_OrgInvites_Role
             CHECK ([Role]='OrgGuest' OR [Role]='OrgMember' OR [Role]='OrgAdmin' OR [Role]='OrgOwner'),

        CONSTRAINT CK_OrgInvites_UsedConsistency
            CHECK (
                (IsUsed = 0 AND UsedAt IS NULL  AND UsedBy IS NULL) OR
                (IsUsed = 1 AND UsedAt IS NOT NULL AND UsedBy IS NOT NULL)
            )
    );

    CREATE INDEX IX_OrgInvites_OrgId
        ON OrganizationInvites (OrganizationId)
        WHERE IsUsed = 0;

    CREATE INDEX IX_OrgInvites_Email
        ON OrganizationInvites (Email)
        WHERE IsUsed = 0;

    -- TTL cleanup job (SQL Server has no native TTL unlike MongoDB)
    -- Schedule a nightly SQL Agent job:
    -- DELETE FROM OrganizationInvites
    -- WHERE ExpiresAt < SYSUTCDATETIME() AND IsUsed = 0;
    CREATE INDEX IX_OrgInvites_ExpiresAt
        ON OrganizationInvites (ExpiresAt)
        WHERE IsUsed = 0;
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 18 — NOW BACK-FILL Files FK columns
--  Files.OrgId / TeamId / ChannelId / MessageId FKs
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Files_Org' AND parent_object_id = OBJECT_ID('dbo.Files'))
    ALTER TABLE dbo.Files ADD CONSTRAINT FK_Files_Org FOREIGN KEY (OrgId) REFERENCES dbo.Organizations(Id) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Files_Team' AND parent_object_id = OBJECT_ID('dbo.Files'))
    ALTER TABLE dbo.Files ADD CONSTRAINT FK_Files_Team FOREIGN KEY (TeamId) REFERENCES dbo.Teams(Id) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Files_Channel' AND parent_object_id = OBJECT_ID('dbo.Files'))
    ALTER TABLE dbo.Files ADD CONSTRAINT FK_Files_Channel FOREIGN KEY (ChannelId) REFERENCES dbo.Channels(Id) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Files_Conversation' AND parent_object_id = OBJECT_ID('dbo.Files'))
    ALTER TABLE dbo.Files ADD CONSTRAINT FK_Files_Conversation FOREIGN KEY (ConversationId) REFERENCES dbo.Conversations(Id) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Files_UserId' AND parent_object_id = OBJECT_ID('dbo.Files'))
    ALTER TABLE dbo.Files ADD CONSTRAINT FK_Files_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE SET NULL;
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 19 — Notification Templates
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'NotificationTemplates' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE NotificationTemplates (
        Id          UNIQUEIDENTIFIER    DEFAULT (newsequentialid())     NOT NULL,

        Name         NVARCHAR(100)       NOT NULL,
        TemplateType NVARCHAR(20)     NOT NULL,
        SubjectText  NVARCHAR(500)       NOT NULL,
        BodyText     NVARCHAR(MAX)       NOT NULL,

        IsActive     BIT                 DEFAULT ((1)),

        RowVersion   ROWVERSION          NOT NULL
    
        CONSTRAINT [PK_NotificationTemplates] PRIMARY KEY CLUSTERED ([Id]),
    
        CONSTRAINT [CK_NotificationTemplates_TemplateType] CHECK (
            [TemplateType] IN ('Email', 'Sms')
        )
    );

    -- Unique per (Name, TemplateType)
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_NotificationTemplates_Name_Type]
        ON [dbo].[NotificationTemplates] ([Name], [TemplateType]);

    -- Fast lookup by transport type
    CREATE NONCLUSTERED INDEX [IX_NotificationTemplates_TemplateType]
        ON [dbo].[NotificationTemplates] ([TemplateType]);
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 20 — Outbox Messages
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'OutboxMessages' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE OutboxMessages (
    	Id             UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    	Type		   NVARCHAR (200)								NOT NULL,
    	Payload        NVARCHAR (MAX)								NOT NULL,
    	RetryCount	   INT				DEFAULT ((0))				NOT NULL,
        CreatedBy      UNIQUEIDENTIFIER								NULL,
        CreatedAt      DATETIME2 (7)    DEFAULT (sysutcdatetime())	NOT NULL,
    	IsProcessed	   BIT				DEFAULT ((0))				NOT NULL,
    	ProcessedAt	   DATETIME2 (7)								NULL,
    	NextAttemptAt  DATETIME2 (7)								NULL,
    	IsDeleted      BIT              DEFAULT ((0))				NOT NULL,
    	RowVersion	   ROWVERSION									NOT NULL,

    	CONSTRAINT [PK_OutboxMessages] PRIMARY KEY CLUSTERED ([Id] ASC),
    	CONSTRAINT [FK_OutboxMessages_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users] ([Id]),
    );

    CREATE NONCLUSTERED INDEX [IX_OutboxMessages_Processing]
    ON [dbo].[OutboxMessages] (IsProcessed, NextAttemptAt)
    WHERE IsDeleted = 0;
END
GO









-- ══════════════════════════════════════════════════════════════
--  Logging schema
--  System, Audit, Communication... logs are store in the following tables
-- ══════════════════════════════════════════════════════════════


-- ══════════════════════════════════════════════════════════════
--  SECTION 1 — System Log
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'SystemLog' AND schema_id = SCHEMA_ID('logging')
)
BEGIN
    CREATE TABLE [logging].[SystemLog] (
        [Id]              INT            IDENTITY (1, 1) NOT NULL,
        [Message]         NVARCHAR (MAX) NULL,
        [MessageTemplate] NVARCHAR (MAX) NULL,
        [Level]           NVARCHAR (MAX) NULL,
        [TimeStamp]       DATETIME       NULL,
        [Exception]       NVARCHAR (MAX) NULL,
        [UserName]        NVARCHAR (MAX) NULL,
        [ServerName]      NVARCHAR (MAX) NULL,
        [MethodType]      NVARCHAR (MAX) NULL,
        [Origin]          NVARCHAR (MAX) NULL,
        [Platform]        NVARCHAR (MAX) NULL,
        [Path]            NVARCHAR (MAX) NULL,
        [UserAgent]       NVARCHAR (MAX) NULL,
        CONSTRAINT [PK_SystemLog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 2 — Audit Log
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'AuditLog' AND schema_id = SCHEMA_ID('logging')
)
BEGIN
    CREATE TABLE [logging].[AuditLog] (
        [Id]              INT              IDENTITY (1, 1) NOT NULL,
        [Message]         NVARCHAR (MAX)   NULL,
        [MessageTemplate] NVARCHAR (MAX)   NULL,
        [Level]           NVARCHAR (MAX)   NULL,
        [TimeStamp]       DATETIME         NULL,
        [LogEvent]        NVARCHAR (MAX)   NULL,
        [UserName]        NVARCHAR (MAX)   NULL,
        [TableName]       NVARCHAR (MAX)   NULL,
        [RecordId]        UNIQUEIDENTIFIER NULL,
        CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO


-- ══════════════════════════════════════════════════════════════
--  SECTION 3 — Communication Log
-- ══════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'CommunicationLog' AND schema_id = SCHEMA_ID('logging')
)
BEGIN
    CREATE TABLE [logging].[CommunicationLog] (
        [Id]              INT            IDENTITY (1, 1) NOT NULL,
        [Message]         NVARCHAR (MAX) NULL,
        [MessageTemplate] NVARCHAR (MAX) NULL,
        [Level]           NVARCHAR (MAX) NULL,
        [TimeStamp]       DATETIME       NULL,
        [LogEvent]        NVARCHAR (MAX) NULL,
        [UserName]        NVARCHAR (MAX) NULL,
        [DeliveryMethod]  NVARCHAR (MAX) NULL,
        [DeliveryStatus]  NVARCHAR (MAX) NULL,
        CONSTRAINT [PK_CommunicationLog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO
