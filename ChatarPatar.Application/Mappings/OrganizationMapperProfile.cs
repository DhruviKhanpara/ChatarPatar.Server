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
        CreateMap<CreateOrganizationDto, Organization>()
            .ForMember(dest => dest.OrganizationMembers, opt => opt.Ignore());

        // Organization members
        CreateMap<AddOrganizationMemberDto, OrganizationMember>()
            .ForMember(dest => dest.OrgId, opt => opt.Ignore())
            .ForMember(dest => dest.JoinedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Organization, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());
    }
}
