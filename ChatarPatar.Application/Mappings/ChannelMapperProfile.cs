using AutoMapper;
using ChatarPatar.Application.DTOs.Channel;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Application.Mappings;

public class ChannelMapperProfile : Profile
{
    public ChannelMapperProfile()
    {
        // Channel
        CreateMap<Channel, ChannelDto>()
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.ChannelMembers.Count(m => !m.IsDeleted)));

        CreateMap<Channel, ChannelWithRoleDto>()
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.ChannelMembers.Count(m => !m.IsDeleted)))
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.JoinedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsMuted, opt => opt.Ignore());

        CreateMap<CreateChannelDto, Channel>(MemberList.Source)
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        CreateMap<UpdateChannelDto, Channel>(MemberList.Source)
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        //Channel members
    }
}
