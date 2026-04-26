using AutoMapper;
using ChatarPatar.Application.DTOs.Team;
using ChatarPatar.Application.DTOs.TeamMember;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Application.Mappings;

public class TeamMapperProfile : Profile
{
    public TeamMapperProfile()
    {
        // Team
        CreateMap<Team, TeamDto>()
            .ForMember(dest => dest.IconUrl, opt => opt.MapFrom(src => src.IconFile != null ? src.IconFile.Url : null))
            .ForMember(dest => dest.IconThumbnailUrl, opt => opt.MapFrom(src => src.IconFile != null ? src.IconFile.ThumbnailUrl : null))
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.TeamMembers.Count(m => !m.IsDeleted)));

        CreateMap<CreateTeamDto, Team>(MemberList.Source)
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        CreateMap<UpdateTeamDto, Team>(MemberList.Source)
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        CreateMap<Team, TeamWithRoleDto>()
            .ForMember(dest => dest.IconUrl, opt => opt.MapFrom(src => src.IconFile != null ? src.IconFile.Url : null))
            .ForMember(dest => dest.IconThumbnailUrl, opt => opt.MapFrom(src => src.IconFile != null ? src.IconFile.ThumbnailUrl : null))
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.TeamMembers.Count()))
            .ForMember(dest => dest.JoinedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsMuted, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore());

        CreateMap<Team, TeamDto>()
            .ForMember(dest => dest.IconUrl, opt => opt.MapFrom(src => src.IconFile != null ? src.IconFile.Url : null))
            .ForMember(dest => dest.IconThumbnailUrl, opt => opt.MapFrom(src => src.IconFile != null ? src.IconFile.ThumbnailUrl : null))
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.TeamMembers.Count()));

        // Team members
        CreateMap<AddTeamMemberDto, TeamMember>(MemberList.Source)
            .ForMember(dest => dest.TeamId, opt => opt.Ignore())
            .ForMember(dest => dest.InvitedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.JoinedAt, opt => opt.Ignore());
    }
}
