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

/// <summary>
/// Returned when listing orgs for the calling user — includes their role in the org.
/// </summary>
public class OrganizationWithRoleDto : OrganizationDto
{
    public OrganizationRoleEnum Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
