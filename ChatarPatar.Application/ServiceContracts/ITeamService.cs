using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Team;
using ChatarPatar.Common.Models;

namespace ChatarPatar.Application.ServiceContracts;

public interface ITeamService
{
    Task<PagedResult<TeamWithRoleDto>> GetTeamsAsync(Guid orgId, TeamQueryParams queryParams);
    Task<TeamDto> GetTeamAsync(Guid orgId, Guid teamId);
    Task CreateTeamAsync(Guid orgId, CreateTeamDto dto);
    Task UpdateTeamIconAsync(Guid orgId, Guid teamId, ImageUploadDto dto);
    Task UpdateTeamAsync(Guid orgId, Guid teamId, UpdateTeamDto dto);
    Task RemoveTeamIconAsync(Guid orgId, Guid teamId);
    Task ArchiveTeamAsync(Guid orgId, Guid teamId);
    Task UnarchiveTeamAsync(Guid orgId, Guid teamId);
}
