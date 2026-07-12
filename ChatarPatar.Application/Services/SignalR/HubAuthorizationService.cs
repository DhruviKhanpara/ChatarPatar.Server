using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.ServiceContracts.SignalR;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Application.Services.SignalR;

internal class HubAuthorizationService : IHubAuthorizationService
{
    private readonly IRepositoryManager _repositories;
    private readonly IPermissionService _permissionService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HubAuthorizationService> _logger;

    public HubAuthorizationService(IRepositoryManager repositories, IPermissionService permissionService, IMemoryCache cache, ILogger<HubAuthorizationService> logger)
    {
        _repositories = repositories;
        _permissionService = permissionService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> CanAccessChannelAsync(Guid userId, Guid channelId)
    {
        var version = _permissionService.GetUserPermissionVersion(userId);
        var cacheKey = $"hubChanAccess:v{version}:{userId}:{channelId}";

        if (_cache.TryGetValue(cacheKey, out bool cached))
            return cached;

        _logger.LogDebug(
            "[HUB_AUTH] Cache miss — computing channel access. UserId={UserId} ChannelId={ChannelId}",
            userId, channelId);

        var result = await _repositories.ChannelRepository.IsActiveMembershipAsync(userId, channelId);

        if (!result)
        {
            _logger.LogWarning(
                "[HUB_AUTH] Channel access denied. UserId={UserId} ChannelId={ChannelId}",
                userId, channelId);
        }

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
            SlidingExpiration = TimeSpan.FromSeconds(10)
        });

        return result;
    }

    public async Task<bool> CanAccessConversationAsync(Guid userId, Guid conversationId)
    {
        var version = _permissionService.GetUserPermissionVersion(userId);
        var cacheKey = $"hubConvAccess:v{version}:{userId}:{conversationId}";

        if (_cache.TryGetValue(cacheKey, out bool cached))
            return cached;

        _logger.LogDebug(
            "[HUB_AUTH] Cache miss — computing conversation access. UserId={UserId} ConversationId={ConversationId}",
            userId, conversationId);

        var result = await _repositories.ConversationRepository.IsActiveParticipantAsync(userId, conversationId);

        if (!result)
        {
            _logger.LogWarning(
                "[HUB_AUTH] Conversation access denied. UserId={UserId} ConversationId={ConversationId}",
                userId, conversationId);
        }

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
            SlidingExpiration = TimeSpan.FromSeconds(10)
        });

        return result;
    }
}
