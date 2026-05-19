using AutoMapper;
using ChatarPatar.Application.DTOs.Conversation;
using ChatarPatar.Application.DTOs.ConversationParticipant;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Application.Mappings;

public class ConversationMapperProfile : Profile
{
    public ConversationMapperProfile()
    {
        // Conversation
        CreateMap<Conversation, ConversationDto>()
            .ForMember(dest => dest.LogoThumbnailUrl, opt => opt.MapFrom(src => src.LogoFile != null ? src.LogoFile.ThumbnailUrl : null))
            .ForMember(dest => dest.ParticipantCount, opt => opt.Ignore())
            .ForMember(dest => dest.Peer, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore());

        // ConversationParticipant
        CreateMap<ConversationParticipant, ConversationParticipantDto>()
            .ForMember(dest => dest.ParticipantId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.User.Name))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Username))
            .ForMember(dest => dest.AvatarThumbnailUrl, opt => opt.MapFrom(src => src.User.AvatarFile != null ? src.User.AvatarFile.ThumbnailUrl : null));
    }
}
