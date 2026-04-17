using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Team;

namespace ChatarPatar.Application.ServiceContracts;

public interface ITeamService
{
    Task CreateTeamAsync(Guid orgId, CreateTeamDto dto);
    Task UpdateTeamIconAsync(Guid orgId, Guid teamId, ImageUploadDto dto);
    Task UpdateTeamAsync(Guid orgId, Guid teamId, UpdateTeamDto dto);
    Task RemoveTeamIconAsync(Guid orgId, Guid teamId);
}
