using AutoMapper;
using ChatarPatar.Application.DTOs.Channel;
using ChatarPatar.Application.DTOs.ChannelMember;
using ChatarPatar.Common.Enums;
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
        CreateMap<ChannelMember, ChannelMemberDto>()
            .ForMember(dest => dest.MembershipId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.User.Name))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Username))
            .ForMember(dest => dest.AvatarThumbnailUrl, opt => opt.MapFrom(src => src.User.AvatarFile != null ? src.User.AvatarFile.ThumbnailUrl : null));

        CreateMap<TeamMember, ChannelMemberDto>()
            .ForMember(dest => dest.MembershipId, opt => opt.MapFrom(_ => (Guid?)null)) 
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.User.Name))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Username))
            .ForMember(dest => dest.AvatarThumbnailUrl, opt => opt.MapFrom(src => src.User.AvatarFile != null ? src.User.AvatarFile.ThumbnailUrl : null))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(_ => ChannelRoleEnum.ChannelMember));

        CreateMap<AddChannelMemberDto, ChannelMember>(MemberList.Source)
            .ForMember(dest => dest.ChannelId, opt => opt.Ignore())
            .ForMember(dest => dest.AddedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.JoinedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsMuted, opt => opt.Ignore());
    }
}
