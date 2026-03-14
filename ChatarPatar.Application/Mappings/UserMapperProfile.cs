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
            .ForMember(dest => dest.ProfilePhotoUrl, src => src.MapFrom(act => act.AvatarFile != null ? act.AvatarFile.ThumbnailUrl : null));
    }
}
