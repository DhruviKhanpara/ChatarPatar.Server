namespace ChatarPatar.Application.DTOs.Team;

public class CreateTeamDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; } = false;
}
