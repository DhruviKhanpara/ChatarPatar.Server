using AutoMapper;
using ChatarPatar.Application.DTOs.Notification;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Application.Mappings;

public class NotificationMapperProfile : Profile
{
    public NotificationMapperProfile()
    {
        CreateMap<NotificationEntity, NotificationDto>()
            .ForMember(dest => dest.ActorName, opt => opt.MapFrom(src => src.Actor != null ? src.Actor.Name : null))
            .ForMember(dest => dest.ActorAvatarThumbnailUrl, opt => opt.MapFrom(src => src.Actor != null && src.Actor.AvatarFile != null ? src.Actor.AvatarFile.ThumbnailUrl : null));
    }
}
