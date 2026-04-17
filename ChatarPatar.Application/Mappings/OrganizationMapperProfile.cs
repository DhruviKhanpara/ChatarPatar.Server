using AutoMapper;
using ChatarPatar.Application.DTOs.Organization;
using ChatarPatar.Application.DTOs.OrganizationInvite;
using ChatarPatar.Application.DTOs.OrganizationMember;
using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Application.Mappings;

public class OrganizationMapperProfile : Profile
{
    public OrganizationMapperProfile()
    {
        // Organization
        CreateMap<Organization, OrganizationDto>()
            .ForMember(dest => dest.LogoUrl, opt => opt.MapFrom(src => src.LogoFile != null ? src.LogoFile.Url : null))
            .ForMember(dest => dest.LogoThumbnailUrl, opt => opt.MapFrom(src => src.LogoFile != null ? src.LogoFile.ThumbnailUrl : null));

        CreateMap<CreateOrganizationDto, Organization>(MemberList.Source)
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        CreateMap<UpdateOrganizationDto, Organization>(MemberList.Source)
            .ForMember(dest => dest.Slug, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        // Organization members
        CreateMap<OrganizationMember, OrganizationWithRoleDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Organization.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Organization.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Organization.Slug))
            .ForMember(dest => dest.LogoUrl, opt => opt.MapFrom(src => src.Organization.LogoFile != null ? src.Organization.LogoFile.Url : null))
            .ForMember(dest => dest.LogoThumbnailUrl, opt => opt.MapFrom(src => src.Organization.LogoFile != null ? src.Organization.LogoFile.ThumbnailUrl : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.Organization.CreatedAt))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

        CreateMap<OrganizationMember, OrganizationMemberDto>()
            .ForMember(dest => dest.MembershipId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.User.Name))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Username))
            .ForMember(dest => dest.AvatarThumbnailUrl, opt => opt.MapFrom(src => src.User.AvatarFile != null ? src.User.AvatarFile.ThumbnailUrl : null));

        CreateMap<AddOrganizationMemberDto, OrganizationMember>(MemberList.Source)
            .ForMember(dest => dest.OrgId, opt => opt.Ignore())
            .ForMember(dest => dest.InvitedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.JoinedAt, opt => opt.Ignore());

        // Organization Invite
        CreateMap<OrganizationInvite, OrganizationInviteListItemDto>()
            .ForMember(dest => dest.InvitedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.InvitedByName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.Name : string.Empty));
    }
}
