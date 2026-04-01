SELECT * FROM [dbo].[Files];

SELECT * FROM [dbo].[Users];
SELECT * FROM [dbo].[RefreshTokens];
SELECT * FROM [dbo].[UserStatus];

SELECT * FROM [dbo].[Organizations];
SELECT * FROM [dbo].[OrganizationMembers];
SELECT * FROM [dbo].[OrganizationInvites];

SELECT * FROM [dbo].[Teams];
SELECT * FROM [dbo].[TeamMembers];

SELECT * FROM [dbo].[Channels];
SELECT * FROM [dbo].[ChannelMembers];

SELECT * FROM [dbo].[Conversations];
SELECT * FROM [dbo].[ConversationParticipants];

SELECT * FROM [dbo].[Messages];
SELECT * FROM [dbo].[MessageAttachments];
SELECT * FROM [dbo].[MessageReactions];
SELECT * FROM [dbo].[MessageMentions];
SELECT * FROM [dbo].[PinnedMessages];
SELECT * FROM [dbo].[MessageReceipts];

SELECT * FROM [dbo].[ReadStates];

SELECT * FROM [dbo].[Notifications];
SELECT * FROM [DBO].[NotificationTemplates];

SELECT * FROM [DBO].[OutboxMessages];

SELECT * FROM [logging].[SystemLog] ORDER BY Timestamp DESC;
SELECT * FROM [logging].[AuditLog] ORDER BY Timestamp DESC;
SELECT * FROM [logging].[CommunicationLog] ORDER BY Timestamp DESC;