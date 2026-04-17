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
        CreateMap<CreateTeamDto, Team>(MemberList.Source)
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        CreateMap<UpdateTeamDto, Team>(MemberList.Source)
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        // Team members
        CreateMap<AddTeamMemberDto, TeamMember>(MemberList.Source)
            .ForMember(dest => dest.TeamId, opt => opt.Ignore())
            .ForMember(dest => dest.InvitedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.JoinedAt, opt => opt.Ignore());
    }
}
