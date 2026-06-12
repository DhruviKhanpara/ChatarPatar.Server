using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.Notification;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Application.Services;

internal class NotificationService : INotificationService
{
    private readonly IRepositoryManager _repositories;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<NotificationService> _logger;
    private readonly IMapper _mapper;

    public NotificationService(IRepositoryManager repositories, IValidationService validationService, IHttpContextAccessor httpContextAccessor, ILogger<NotificationService> logger, IMapper mapper)
    {
        _repositories = repositories;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _mapper = mapper;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(PaginationParams paginationParams)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var query = _repositories.NotificationRepository
            .GetForUserQuery(authUserId);

        var totalCount = await query.CountAsync();

        var items = await query
            .AsNoTracking()
            .PaginateOffset(paginationParams.PageSize, paginationParams.PageNumber)
            .ProjectTo<NotificationDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<NotificationDto>(items, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync()
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var count = await _repositories.NotificationRepository
            .GetUnreadCountAsync(authUserId);

        return new UnreadCountDto { Count = count };
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var notification = await _repositories.NotificationRepository
            .GetByIdForUserAsync(notificationId, authUserId);

        if (notification is null)
            throw new NotFoundAppException("Notification");

        if (notification.IsRead)
            return;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _repositories.UnitOfWork.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync()
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        await _repositories.NotificationRepository.MarkAllAsReadAsync(authUserId, DateTime.UtcNow);
    }
}
