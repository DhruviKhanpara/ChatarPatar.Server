using AutoMapper;
using ChatarPatar.Application.DTOs.Conversation;
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
    }
}
