using AutoMapper;
using ChatarPatar.Application.DTOs.Organization;
using ChatarPatar.Application.DTOs.OrganizationMember;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Application.Mappings;

public class OrganizationMapperProfile : Profile
{
    public OrganizationMapperProfile()
    {
        // Organization
        CreateMap<CreateOrganizationDto, Organization>(MemberList.Source)
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        CreateMap<UpdateOrganizationDto, Organization>(MemberList.Source)
            .ForMember(dest => dest.Slug, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        // Organization members
        CreateMap<AddOrganizationMemberDto, OrganizationMember>(MemberList.Source)
            .ForMember(dest => dest.OrgId, opt => opt.Ignore())
            .ForMember(dest => dest.InvitedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.JoinedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore());
    }
}
