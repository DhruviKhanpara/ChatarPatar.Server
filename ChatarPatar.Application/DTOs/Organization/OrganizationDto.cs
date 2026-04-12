using ChatarPatar.Common.Enums;

namespace ChatarPatar.Application.DTOs.Organization;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public string? LogoThumbnailUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrganizationWithRoleDto : OrganizationDto
{
    public OrganizationRoleEnum MyRole { get; set; }
    public DateTime JoinedAt { get; set; }
}
