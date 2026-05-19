namespace ChatarPatar.Common.Consts;

public static class DbConstraints
{
    public static class Users
    {
        public const string FKAvatarFile = "FK_Users_AvatarFile";

        public const string UniqueEmail = "UQ_Users_Email";
        public const string UniqueUsername = "UQ_Users_Username";
    }

    public static class UserStatuses
    {
        public const string CKStatus = "CK_UserStatus_Status";
        public const string CKCustomStatus = "CK_UserStatus_CustomStatus";
        public const string CKLogical = "CK_UserStatus_Logical";

        public const string IXStatus = "IX_UserStatus_Status";
    }

    public static class Organizations
    {
        public const string FKLogoFile = "FK_Organizations_Logo";
        public const string FKCreatedByUser = "FK_Organizations_CreatedBy";
        public const string FKUpdatedByUser = "FK_Organizations_UpdatedBy";
        public const string FKDeletedByUser = "FK_Organizations_DeletedBy";

        public const string UniqueSlug = "UQ_Organizations_Slug";
    }

    public static class OrganizationMembers
    {
        public const string FKOrg = "FK_OrgMembers_Org";
        public const string FKUser = "FK_OrgMembers_User";
        public const string FKInviter = "FK_OrgMembers_InvitedBy";
        public const string FKCreatedByUser = "FK_OrgMembers_CreatedBy";
        public const string FKUpdatedByUser = "FK_OrgMembers_UpdatedBy";
        public const string FKDeletedByUser = "FK_OrgMembers_DeletedBy";

        public const string CKRole = "CK_OrgMembers_Role";

        public const string UniqueActiveOrgMembers = "UX_OrgMembers_Active";

        public const string IXUserId = "IX_OrgMembers_UserId";
        public const string IXOrgId = "IX_OrgMembers_OrgId";
    }

    public static class OrganizationInvites
    {
        public const string FKCreatedByUser = "FK_OrgInvites_CreatedBy";
        public const string FKUsedByUser = "FK_OrgInvites_UsedBy";
        public const string FKOrg = "FK_OrgInvites_Org";

        public const string CKRole = "CK_OrgInvites_Role";
        public const string CKUsedConsistency = "CK_OrgInvites_UsedConsistency";
        public const string CKFailedAttempts = "CK_OrgInvites_FailedAttempts";

        public const string UniqueToken = "UQ_OrgInvites_Token";

        public const string IXExpiresAt = "IX_OrgInvites_ExpiresAt";
        public const string IXEmail = "IX_OrgInvites_Email";
        public const string IXOrgId = "IX_OrgInvites_OrgId";
    }

    public static class Teams
    {
        public const string FKOrg = "FK_Teams_Org";
        public const string FKIconFile = "FK_Teams_Icon";
        public const string FKArchiver = "FK_Teams_Archiver";
        public const string FKCreatedByUser = "FK_Teams_CreatedBy";
        public const string FKUpdatedByUser = "FK_Teams_UpdatedBy";
        public const string FKDeletedByUser = "FK_Teams_DeletedBy";

        public const string CKArchiveState = "CK_Teams_ArchiveState";

        public const string UniqueNamePerOrg = "UX_Teams_Name";

        public const string IXTeamArchivedInOrg = "IX_Teams_Archived";
        public const string IXOrgId = "IX_Teams_OrgId";
    }

    public static class TeamMembers
    {
        public const string FKTeam = "FK_TeamMembers_Team";
        public const string FKUser = "FK_TeamMembers_User";
        public const string FKInviter = "FK_TeamMembers_Inviter";
        public const string FKCreatedByUser = "FK_TeamMembers_CreatedBy";
        public const string FKUpdatedByUser = "FK_TeamMembers_UpdatedBy";
        public const string FKDeletedByUser = "FK_TeamMembers_DeletedBy";

        public const string CKRole = "CK_TeamMembers_Role";

        public const string UniqueActiveTeamMembers = "UX_TeamMembers_Active";

        public const string IXUserId = "IX_TeamMembers_UserId";
        public const string IXTeamId = "IX_TeamMembers_TeamId";
    }

    public static class Channels
    {
        public const string FKTeam = "FK_Channels_Team";
        public const string FKOrg = "FK_Channels_Org";
        public const string FKArchiver = "FK_Channels_Archiver";
        public const string FKCreatedByUser = "FK_Channels_CreatedBy";
        public const string FKUpdatedByUser = "FK_Channels_UpdatedBy";
        public const string FKDeletedByUser = "FK_Channels_DeletedBy";

        public const string CKArchiveState = "CK_Channels_ArchiveState";
        public const string CKType = "CK_Channels_Type";

        public const string UniqueNamePerTeam = "UX_Channels_Name";

        public const string IXArchivedInTeam = "IX_Channels_Archived";
        public const string IXTeamId = "IX_Channels_TeamId";
    }

    public static class ChannelMembers
    {
        public const string FKChannel = "FK_ChannelMembers_Channel";
        public const string FKUser = "FK_ChannelMembers_User";
        public const string FKAddedByUser = "FK_ChannelMembers_AddedBy";
        public const string FKCreatedByUser = "FK_ChannelMembers_CreatedBy";
        public const string FKUpdatedByUser = "FK_ChannelMembers_UpdatedBy";
        public const string FKDeletedByUser = "FK_ChannelMembers_DeletedBy";

        public const string CKRole = "CK_ChannelMembers_Role";
        
        public const string UniqueActiveChannelMembers = "UX_ChannelMembers_Active";
        
        public const string IXChannelId = "IX_ChannelMembers_ChannelId";
        public const string IXUserId = "IX_ChannelMembers_UserId";
    }

    public static class Conversations
    {
        public const string FKLogoFile = "FK_Conversations_Logo";
        public const string FKDirectParticipantA = "FK_Conversations_DirectParticipantAId";
        public const string FKDirectParticipantB = "FK_Conversations_DirectParticipantBId";
        public const string FKCreatedByUser = "FK_Conversations_CreatedBy";
        public const string FKUpdatedByUser = "FK_Conversations_UpdatedBy";
        public const string FKDeletedByUser = "FK_Conversations_DeletedBy";
        
        public const string CKType = "CK_Conversations_Type";
        public const string CKDirectRule = "CK_Conversations_DirectRule";
        
        public const string UniqueDirectConversationParticipants = "UX_Conversations_Direct";
    }

    public static class ConversationParticipants
    {
        public const string FKConversation = "FK_ConvParticipants_Conversation";
        public const string FKUser = "FK_ConvParticipants_User";
        public const string FKAddedByUser = "FK_ConvParticipants_AddedBy";
        public const string FKRejoinedByUser = "FK_ConvParticipants_RejoinedBy";

        public const string CKRole = "CK_ConvParticipants_Role";

        public const string UniqueConversationUser = "UQ_ConvParticipants";

        public const string IXConversationId = "IX_ConvParticipants_ConvId";
        public const string IXUserId = "IX_ConvParticipants_UserId";
        public const string IXActiveConversation = "IX_ConvParticipants_ActiveConversation";
    }

    public static class Files
    {
        public const string FKUploadedByUser = "FK_Files_UploadedBy";
        public const string FKCreatedByUser = "FK_Files_CreatedBy";
        public const string FKUpdatedByUser = "FK_Files_UpdatedBy";
        public const string FKDeletedByUser = "FK_Files_DeletedBy";

        public const string CKType = "CK_Files_FileType";
        public const string CKUsageContext = "CK_Files_UsageContext";
        public const string CKScope = "CK_Files_OnlyOneScope";

        public const string IXUploadedByUserId = "IX_Files_UploadedBy";
        public const string IXUsageContext = "IX_Files_UsageContext";
    }

    public static class Messages
    {
        public const string FKChannel = "FK_Messages_Channel";
        public const string FKConversation = "FK_Messages_Conversation";
        public const string FKSender = "FK_Messages_Sender";
        public const string FKThreadMessage = "FK_Messages_Thread";
        public const string FKDeletedByUser = "FK_Messages_DeletedBy";

        public const string CKMessageSource = "CK_Messages_Source";
        public const string CKDmStatus = "CK_Messages_DmStatus";
        public const string CKThreadReplyRule = "CK_Messages_ThreadReplyRule";
        public const string CKType = "CK_Messages_MessageType";

        public const string UniqueChannelClientMessage = "UX_Messages_Channel_ClientMessage";
        public const string UniqueConversationClientMessage = "UX_Messages_Conversation_ClientMessage";

        public const string IXThreadRootMessageId = "IX_Messages_ThreadRootMessageId";
        public const string IXActiveChannelMessage = "IX_Messages_Channel_Active";
        public const string IXActiveConversationMessage = "IX_Messages_Conversation_Active";
        public const string IXSenderId = "IX_Messages_SenderId_CreatedAt";
    }

    public static class MessageAttachments
    {
        public const string FKMessage = "FK_MessageAttachments_Message";
        public const string FKFile = "FK_MessageAttachments_File";

        public const string UniqueFilePerMessage = "UQ_MessageAttachments_File";
        public const string UniqueDisplayOrderInMessage = "UQ_MessageAttachments_Order";

        public const string IXMessageId = "IX_MessageAttachments_MessageId";
    }

    public static class MessageMentions
    {
        public const string FKMessage = "FK_MessageMentions_Message";
        public const string FKMentionedUser = "FK_MessageMentions_User";
        public const string FKChannel = "FK_MessageMentions_Channel";
        public const string FKConversation = "FK_MessageMentions_Conv";
        
        public const string CKMessageSource = "CK_MessageMentions_Source";
        
        public const string UniqueMentionUserPerMessage = "UQ_MessageMentions";

        public const string IXMentionUserInChannel = "IX_MessageMentions_UserChannel";
        public const string IXMentionUserInConversation = "IX_MessageMentions_UserConv";
    }

    public static class MessageReactions
    {
        public const string FKMessage = "FK_MessageReactions_Message";
        public const string FKUser = "FK_MessageReactions_User";

        public const string UniqueMessageReactionPerMessage = "UQ_MessageReactions";

        public const string IXMessageId = "IX_MessageReactions_MessageId";
    }

    public static class MessageReceipts
    {
        public const string FKMessage = "FK_MessageReceipts_Message";
        public const string FKUser = "FK_MessageReceipts_User";

        public const string CKSeenAfterDelivered = "CK_MessageReceipts_SeenAfterDelivered";

        public const string UniqueReceiptPerMessage = "UQ_MessageReceipts";

        public const string IXUserMessage = "IX_MessageReceipts_Message";
        public const string IXUserSeenAt = "IX_MessageReceipts_User_Seen";
    }

    public static class PinnedMessages
    {
        public const string FKMessage = "FK_PinnedMessages_Message";
        public const string FKPinnedByUser = "FK_PinnedMessages_PinnedBy";
        public const string FKUnPinnedByUser = "FK_PinnedMessages_UnPinnedBy";
        public const string FKChannel = "FK_PinnedMessages_Channel";
        public const string FKConversation = "FK_PinnedMessages_Conv";

        public const string CKMessageSource = "CK_PinnedMessages_Source";
        public const string CKUnpinConsistency = "CK_PinnedMessages_UnpinConsistency";

        public const string UniquePinnedMessagePerChannel = "UX_Pinned_Channel_Active";
        public const string UniquePinnedMessagePerConversation = "UX_Pinned_Conversation_Active";

        public const string IXChannelMessagePinnedAt = "IX_Pinned_Channel_Active";
        public const string IXConversationMessagePinnedAt = "IX_Pinned_Conversation_Active";
    }

    public static class ReadStates
    {
        public const string CKMessageSource = "CK_ReadStates_Source";
        public const string CKNonNegativeUnreadCount = "CK_ReadStates_Unread_NonNegative";
        public const string CKNonNegativeMentionCount = "CK_ReadStates_Mention_NonNegative";

        public const string UniqueReadStatePerChannel = "UX_ReadStates_User_Channel";
        public const string UniqueReadStatePerConversation = "UX_ReadStates_User_Conversation";

        public const string IXUserId = "IX_ReadStates_User";
    }

    public static class Notifications
    {
        public const string CKReadConsistency = "CK_Notifications_ReadConsistency";
        public const string CKType = "CK_Notifications_Type";

        public const string IXRecipientReadCreated = "IX_Notifications_UserId";
    }

    public static class NotificationTemplates
    {
        public const string CKType = "CK_NotificationTemplates_TemplateType";

        public const string UniqueNamePerTemplateType = "UQ_NotificationTemplates_Name_Type";

        public const string IXType = "IX_NotificationTemplates_TemplateType";
    }

    public static class OtpVerifications
    {
        public const string FKUser = "FK_OtpVerifications_User";

        public const string CKPurpose = "CK_OtpVerifications_Purpose";
        public const string CKUsedConsistency = "CK_OtpVerifications_UsedConsistency";
        public const string CKFailedAttempts = "CK_OtpVerifications_FailedAttempts";

        public const string IXUserPurposeUnused = "IX_OtpVerifications_UserId_Purpose";
        public const string IXUserPurposeCreatedAt = "IX_OtpVerifications_UserId_Purpose_CreatedAt";
        public const string IXUnusedExpiredAt = "IX_OtpVerifications_ExpiresAt";
    }

    public static class OutboxMessages
    {
        public const string FKCreatedByUser = "FK_OutboxMessages_CreatedBy";

        public const string IXProcessedNextAttempt = "IX_OutboxMessages_Processing";
    }

    public static class RefreshTokens
    {
        public const string CKRevokeConsistency = "CK_RefreshTokens_RevokeConsistency";

        public const string UniqueActiveToken = "UX_RefreshTokens_Token";

        public const string IXUserActiveExpiration = "IX_RefreshToken_ActiveToken";
    }
}
