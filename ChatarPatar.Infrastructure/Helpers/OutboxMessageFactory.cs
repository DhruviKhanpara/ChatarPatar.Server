using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using Newtonsoft.Json;

namespace ChatarPatar.Infrastructure.Helpers;

public static class OutboxMessageFactory
{
    public static OutboxMessage BuildCloudinaryDeleteMessage(string publicId, FileTypeEnum fileType, Guid initiatedBy, string initiatedByName)
    {
        return new OutboxMessage
        {
            Type = CloudinaryDeletePayload.OutboxType,
            Payload = JsonConvert.SerializeObject(new CloudinaryDeletePayload
            {
                PublicId = publicId,
                FileType = fileType,
                InitiatedBy = initiatedByName
            }),
            IsProcessed = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = initiatedBy,
            IsDeleted = false
        };
    }

    public static OutboxMessage BuildChannelSendMessage(List<Guid> mentionedUserIds, Guid channelId, Guid messageId, long messageSequenceNumber, Guid initiatedBy, string initiatedByName)
    {
        return new OutboxMessage
        {
            Type = MessageSentChannelPayload.OutboxType,
            Payload = JsonConvert.SerializeObject(new MessageSentChannelPayload
            {
                MessageId = messageId,
                SequenceNumber = messageSequenceNumber,
                ChannelId = channelId,
                SenderId = initiatedBy,
                MentionedUserIds = mentionedUserIds,
                InitiatedBy = initiatedByName,
            }),
            IsProcessed = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = initiatedBy,
            IsDeleted = false
        };
    }
}
