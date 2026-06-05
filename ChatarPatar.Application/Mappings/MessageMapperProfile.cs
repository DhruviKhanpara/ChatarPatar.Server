using AutoMapper;
using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Application.DTOs.Message.Attachment;
using ChatarPatar.Application.DTOs.Message.Mention;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Application.Mappings;

public class MessageMapperProfile : Profile
{
    public MessageMapperProfile()
    {
        // Message
        CreateMap<Message, MessageDto>()
            .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender.Name))
            .ForMember(dest => dest.SenderAvatarThumbnailUrl, opt => opt.MapFrom(src => src.Sender.AvatarFile != null ? src.Sender.AvatarFile.ThumbnailUrl : null))
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.MessageAttachments.OrderBy(a => a.DisplayOrder)))
            .ForMember(dest => dest.Mentions, opt => opt.MapFrom(src => src.MessageMentions));

        // Message Attachment
        CreateMap<MessageAttachment, MessageAttachmentDto>()
           .ForMember(dest => dest.Url, opt => opt.MapFrom(src => src.File.Url))
           .ForMember(dest => dest.ThumbnailUrl, opt => opt.MapFrom(src => src.File.ThumbnailUrl))
           .ForMember(dest => dest.OriginalName, opt => opt.MapFrom(src => src.File.OriginalName))
           .ForMember(dest => dest.MimeType, opt => opt.MapFrom(src => src.File.MimeType))
           .ForMember(dest => dest.SizeInBytes, opt => opt.MapFrom(src => src.File.SizeInBytes))
           .ForMember(dest => dest.FileType, opt => opt.MapFrom(src => src.File.FileType));

        // Message Mentions
        CreateMap<MessageMention, MessageMentionDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.MentionedUserId))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.MentionedUser.Name))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.MentionedUser.Username));
    }
}
