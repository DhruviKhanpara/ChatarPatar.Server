using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.SignalR.Model;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Application.Services;

internal class PresenceService : IPresenceService
{
    private readonly IRepositoryManager _repositories;
    private readonly ILogger<PresenceService> _logger;

    public PresenceService(IRepositoryManager repositories, ILogger<PresenceService> logger)
    {
        _repositories = repositories;
        _logger = logger;
    }

    public async Task OnUserConnectedAsync(Guid userId)
    {
        var status = await _repositories.UserStatusRepository
            .FindByCondition(s => s.UserId == userId)
            .FirstOrDefaultAsync();

        if (status is null)
        {
            _logger.LogWarning("[Presence] UserStatus row missing for UserId={UserId}. Skipping.", userId);
            return;
        }

        status.Status = PresenceStatusEnum.Online;
        status.CustomStatus ??= CustomPresenceStatusEnum.Active;
        status.LastSeenAt = DateTime.UtcNow;
        status.UpdatedAt = DateTime.UtcNow;

        await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();

        _logger.LogInformation("[Presence] UserId={UserId} is now Online.", userId);
    }

    public async Task OnUserDisconnectedAsync(Guid userId)
    {
        var status = await _repositories.UserStatusRepository
            .FindByCondition(s => s.UserId == userId)
            .FirstOrDefaultAsync();

        if (status is null)
        {
            _logger.LogWarning("[Presence] UserStatus row missing for UserId={UserId}. Skipping.", userId);
            return;
        }

        status.Status = PresenceStatusEnum.Offline;
        status.LastSeenAt = DateTime.UtcNow;
        status.UpdatedAt = DateTime.UtcNow;

        await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();

        _logger.LogInformation("[Presence] UserId={UserId} is now Offline.", userId);
    }

    public async Task SetCustomStatusAsync(Guid userId, PresenceStatusEnum status, CustomPresenceStatusEnum? customStatus)
    {
        var userStatus = await _repositories.UserStatusRepository
            .FindByCondition(s => s.UserId == userId)
            .FirstOrDefaultAsync();

        if (userStatus is null)
        {
            _logger.LogWarning("[Presence] UserStatus row missing for UserId={UserId}. Skipping.", userId);
            return;
        }

        userStatus.Status = status;
        userStatus.CustomStatus = customStatus;
        userStatus.UpdatedAt = DateTime.UtcNow;

        await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();
    }

    public async Task<PresencePush> GetStatusAsync(Guid userId)
    {
        var status = await _repositories.UserStatusRepository
            .FindByCondition(s => s.UserId == userId)
            .AsNoTracking()
            .Select(s => new PresencePush
            {
                UserId = s.UserId,
                Status = s.Status,
                CustomStatus = s.CustomStatus,
                LastSeenAt = s.LastSeenAt
            })
            .FirstOrDefaultAsync();

        // Default to Offline if row doesn't exist yet
        return status ?? new PresencePush { UserId = userId, Status = PresenceStatusEnum.Offline, LastSeenAt = DateTime.UtcNow };
    }
}
