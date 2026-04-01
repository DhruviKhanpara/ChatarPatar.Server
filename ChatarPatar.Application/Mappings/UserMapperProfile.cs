using AutoMapper;
using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Application.Mappings;

public class UserMapperProfile : Profile
{
    public UserMapperProfile()
    {
        CreateMap<UserRegisterDto, User>();

        CreateMap<User, AuthUserDto>()
            .ForMember(dest => dest.AvatarUrl, src => src.MapFrom(act => act.AvatarFile != null ? act.AvatarFile.ThumbnailUrl : null));
        
        CreateMap<User, UserProfileDto>()
            .ForMember(dest => dest.AvatarUrl, src => src.MapFrom(act => act.AvatarFile != null ? act.AvatarFile.Url : null))
            .ForMember(dest => dest.AvatarThumbnailUrl, src => src.MapFrom(act => act.AvatarFile != null ? act.AvatarFile.ThumbnailUrl : null));

        CreateMap<User, UserProfileSummaryDto>()
            .ForMember(dest => dest.AvatarUrl, src => src.MapFrom(act => act.AvatarFile != null ? act.AvatarFile.Url : null))
            .ForMember(dest => dest.AvatarThumbnailUrl, src => src.MapFrom(act => act.AvatarFile != null ? act.AvatarFile.ThumbnailUrl : null));
    }
}
