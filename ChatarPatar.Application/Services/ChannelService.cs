using AutoMapper;
using AutoMapper.QueryableExtensions;
using ChatarPatar.Application.Common.Extensions;
using ChatarPatar.Application.DTOs.Channel;
using ChatarPatar.Application.DTOs.Message;
using ChatarPatar.Application.ServiceContracts;
using ChatarPatar.Application.ServiceContracts.SignalR;
using ChatarPatar.Common.AppExceptions.CustomExceptions;
using ChatarPatar.Common.AppLogging.Model.LogRequest;
using ChatarPatar.Common.Enums;
using ChatarPatar.Common.Helpers;
using ChatarPatar.Common.HttpUserDetails;
using ChatarPatar.Common.Models;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatarPatar.Application.Services;

internal class ChannelService : IChannelService
{
    private readonly IRepositoryManager _repositories;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISignalRService _signalR;
    private readonly ILogger<ChannelService> _logger;

    public ChannelService(IRepositoryManager repositories, IMapper mapper, IValidationService validationService, IHttpContextAccessor httpContextAccessor, ISignalRService signalR, ILogger<ChannelService> logger)
    {
        _repositories = repositories;
        _mapper = mapper;
        _validationService = validationService;
        _httpContextAccessor = httpContextAccessor;
        _signalR = signalR;
        _logger = logger;
    }
    private HttpContext _httpContext => _httpContextAccessor.HttpContext ?? throw new AppException("No HTTP context available");

    public async Task<PagedResult<ChannelWithRoleDto>> GetChannelsAsync(Guid orgId, Guid teamId, ChannelQueryParams queryParams)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        // Verify caller is in the org and resolve their team role in one query
        var callerContext = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .AsNoTracking()
            .Select(t => new
            {
                OrgRole = t.Organization.OrganizationMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (OrganizationRoleEnum?)m.Role)
                    .FirstOrDefault(),
                TeamRole = t.TeamMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (TeamRoleEnum?)m.Role)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (callerContext == null || callerContext.OrgRole == null)
            throw new NotFoundAppException("Team");

        var callerIsAdmin =
            callerContext.OrgRole is OrganizationRoleEnum.OrgOwner or OrganizationRoleEnum.OrgAdmin
            || callerContext.TeamRole is TeamRoleEnum.TeamAdmin;

        var baseQuery = _repositories.ChannelRepository.GetChannelsQuery(
            teamId,
            orgId,
            callerId: authUserId,
            callerHasElevatedAccess: callerIsAdmin,
            search: queryParams.Search,
            isArchived: queryParams.IsArchived,
            includePrivate: queryParams.IncludePrivate);

        var totalCount = await baseQuery.CountAsync();
        var items = await baseQuery
            .PaginateOffset(queryParams.PageSize, queryParams.PageNumber)
            .AsNoTracking()
            .ProjectTo<ChannelWithRoleDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        // Back-fill caller's own membership data (role, joinedAt, isMuted)
        if (items.Count > 0)
        {
            var pageChannelIds = items.Select(c => c.Id).ToList();

            var readStates = await _repositories.ReadStateRepository
                .FindByCondition(rs => pageChannelIds.Contains(rs.ChannelId!.Value) && rs.UserId == authUserId)
                .AsNoTracking()
                .Select(rs => new { rs.ChannelId, rs.UnreadCount, rs.MentionCount })
                .ToListAsync();

            var readStateDict = readStates.Where(rs => rs.ChannelId.HasValue).ToDictionary(rs => rs.ChannelId!.Value);

            var myMemberships = await _repositories.ChannelMemberRepository
                .FindByCondition(m => pageChannelIds.Contains(m.ChannelId) && m.UserId == authUserId)
                .AsNoTracking()
                .Select(m => new { m.ChannelId, m.Role, m.IsMuted, m.JoinedAt })
                .ToListAsync();

            var myMembershipsDict = myMemberships.ToDictionary(x => x.ChannelId);

            foreach (var item in items)
            {
                if (readStateDict.TryGetValue(item.Id, out var rs))
                {
                    item.UnreadCount = rs.UnreadCount;
                    item.MentionCount = rs.MentionCount;
                }

                myMembershipsDict.TryGetValue(item.Id, out var membership);
                item.Role = membership?.Role;
                item.IsMuted = membership?.IsMuted;
                item.JoinedAt = membership?.JoinedAt;
            }
        }

        return new PagedResult<ChannelWithRoleDto>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
    }

    public async Task<ChannelDto> GetChannelAsync(Guid orgId, Guid teamId, Guid channelId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        // Resolve caller's org and team role
        var callerContext = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .AsNoTracking()
            .Select(t => new
            {
                OrgRole = t.Organization.OrganizationMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (OrganizationRoleEnum?)m.Role)
                    .FirstOrDefault(),
                TeamRole = t.TeamMembers
                    .Where(m => m.UserId == authUserId && !m.IsDeleted)
                    .Select(m => (TeamRoleEnum?)m.Role)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (callerContext == null || callerContext.OrgRole == null)
            throw new NotFoundAppException("Team");

        var callerIsAdmin =
            callerContext.OrgRole is OrganizationRoleEnum.OrgOwner or OrganizationRoleEnum.OrgAdmin
            || callerContext.TeamRole is TeamRoleEnum.TeamAdmin;

        var query = _repositories.ChannelRepository
            .GetByIdInTeam(channelId, teamId, orgId)
            .AsNoTracking();

        // Non-admins cannot see a private channel they are not a member of
        if (!callerIsAdmin)
            query = query.Where(c =>
                !c.IsPrivate ||
                c.ChannelMembers.Any(m => m.UserId == authUserId && !m.IsDeleted));

        var result = await query
            .ProjectTo<ChannelDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        if (result is null)
            throw new NotFoundAppException("Channel");

        return result;
    }

    public async Task CreateChannelAsync(Guid orgId, Guid teamId, CreateChannelDto dto)
    {
        await _validationService.ValidateAsync<CreateChannelDto>(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        // Verify team exists and caller is an org member
        var teamExists = await _repositories.TeamRepository
            .GetByIdInOrg(teamId, orgId)
            .AsNoTracking()
            .Select(t => new
            {
                t.IsArchived,
                IsOrgMember = t.Organization.OrganizationMembers
                    .Any(m => m.UserId == authUserId && !m.IsDeleted),
                MemberIds = t.TeamMembers
                    .Where(tm => !tm.IsDeleted)
                    .Select(tm => tm.UserId)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (teamExists == null || !teamExists.IsOrgMember)
            throw new NotFoundAppException("Team");

        if (teamExists.IsArchived)
            throw new InvalidDataAppException("Cannot create channels in an archived team.");

        var nameExists = await _repositories.ChannelRepository.NameExistsInTeamAsync(teamId, dto.Name);

        if (nameExists)
            throw new DuplicateEntryAppException("A channel with this name already exists in the team.");

        var channelEntity = _mapper.Map<Channel>(dto);
        channelEntity.TeamId = teamId;
        channelEntity.OrgId = orgId;

        // Only have explicit membership for private channels
        // Public channel have visibility to team so team membership is use as channel membership and channel's audit field can track the creator of the channel.
        if (channelEntity.IsPrivate)
        {
            // Creator is automatically added as ChannelModerator
            channelEntity.ChannelMembers.Add(new ChannelMember
            {
                UserId = authUserId,
                Role = ChannelRoleEnum.ChannelModerator,
                AddedByUserId = authUserId,
                JoinedAt = DateTime.UtcNow
            });
        }

        await using var tx = await _repositories.UnitOfWork.BeginTransactionAsync();
        try
        {
            await _repositories.ChannelRepository.AddAsync(channelEntity);
            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync();

            // Seed read states
            // ReadState is UI state, not an auditable business decision — suppress.
            if (channelEntity.IsPrivate)
                await _repositories.ReadStateRepository.SeedForChannelAsync(authUserId, channelEntity.Id, true);
            else
                await _repositories.ReadStateRepository.SeedForChannelsAsync(teamExists.MemberIds, [channelEntity.Id], true);

            await _repositories.UnitOfWork.SaveChangesWithoutAuditAsync(suppressRowAudit: true);

            // For a public channel, queue one BulkEvent to capture the fan-out count.
            // Private channel creation is already covered by the RowChange entries above
            // (1 Channel + 1 ChannelMember row — no fan-out problem).
            if (!channelEntity.IsPrivate)
            {
                _repositories.UnitOfWork.QueueManualAuditLog(new AuditLogRequest(
                    tableName: "ReadStates",
                    eventName: "ChannelReadStateSeeded",
                    payload: new
                    {
                        ChannelId = channelEntity.Id,
                        TeamId = teamId,
                        SeededForCount = teamExists.MemberIds.Count
                    }));
            }

            await tx.CommitAsync();
            _repositories.UnitOfWork.FlushPendingAuditLogs();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateChannelAsync(Guid orgId, Guid teamId, Guid channelId, UpdateChannelDto dto)
    {
        await _validationService.ValidateAsync<UpdateChannelDto>(dto);

        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var channel = await _repositories.ChannelRepository
            .GetByIdInTeam(channelId, teamId, orgId)
            .Include(c => c.Team)
            .FirstOrDefaultAsync();

        if (channel is null)
            throw new NotFoundAppException("Channel");

        channel.EnsureEditable();

        if (string.Equals(channel.Name, dto.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            _mapper.Map<UpdateChannelDto, Channel>(dto, channel);
            await _repositories.UnitOfWork.SaveChangesAsync();
            return;
        }

        var nameExists = await _repositories.ChannelRepository
                .NameExistsInTeamAsync(teamId, dto.Name, excludeChannelId: channelId);

        if (nameExists)
            throw new DuplicateEntryAppException("A channel with this name already exists in the team.");

        var systemMessage = new Message
        {
            ClientMessageId = Guid.NewGuid(),
            ChannelId = channelId,
            SenderId = authUserId,

            Content = $"Channel has been renamed from {channel.Name} to {dto.Name}.",
            MessageType = MessageTypeEnum.System,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repositories.MessageRepository.AddAsync(systemMessage);

        _mapper.Map<UpdateChannelDto, Channel>(dto, channel);

        await _repositories.UnitOfWork.SaveChangesAsync();

        var messageDto = await GetSystemMessageDto(systemMessage.Id);

        try { await _signalR.BroadcastChannelMessageAsync(channelId, messageDto); }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastChannelMessage (rename) failed. ChannelId={Id}", channelId); }
    }

    public async Task ArchiveChannelAsync(Guid orgId, Guid teamId, Guid channelId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var channel = await _repositories.ChannelRepository
            .GetByIdInTeam(channelId, teamId, orgId)
            .Include(c => c.Team)
            .FirstOrDefaultAsync();

        if (channel is null)
            throw new NotFoundAppException("Channel");

        channel.EnsureEditable();

        channel.IsArchived = true;
        channel.ArchivedAt = DateTime.UtcNow;
        channel.ArchivedBy = authUserId;

        var systemMessage = new Message
        {
            ClientMessageId = Guid.NewGuid(),
            ChannelId = channelId,
            SenderId = authUserId,

            Content = "Channel has been archived.",
            MessageType = MessageTypeEnum.System,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repositories.MessageRepository.AddAsync(systemMessage);

        await _repositories.UnitOfWork.SaveChangesAsync();

        var messageDto = await GetSystemMessageDto(systemMessage.Id);

        try { await _signalR.BroadcastChannelMessageAsync(channelId, messageDto); }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastChannelMessage (archive) failed. ChannelId={Id}", channelId); }
    }

    public async Task UnarchiveChannelAsync(Guid orgId, Guid teamId, Guid channelId)
    {
        var authUserId = Guid.Parse(_httpContext.GetUserId());

        var channel = await _repositories.ChannelRepository
            .GetByIdInTeam(channelId, teamId, orgId)
            .Include(c => c.Team)
            .FirstOrDefaultAsync();

        if (channel is null)
            throw new NotFoundAppException("Channel");

        channel.Team.EnsureEditable();

        if (!channel.IsArchived)
            throw new InvalidDataAppException("Channel is not archived.");

        channel.IsArchived = false;
        channel.ArchivedAt = null;
        channel.ArchivedBy = null;

        var systemMessage = new Message
        {
            ClientMessageId = Guid.NewGuid(),
            ChannelId = channelId,
            SenderId = authUserId,

            Content = "Channel has been un-archived.",
            MessageType = MessageTypeEnum.System,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repositories.MessageRepository.AddAsync(systemMessage);

        await _repositories.UnitOfWork.SaveChangesAsync();

        var messageDto = await GetSystemMessageDto(systemMessage.Id);

        try { await _signalR.BroadcastChannelMessageAsync(channelId, messageDto); }
        catch (Exception ex) { _logger.LogWarning(ex, "[SignalR] BroadcastChannelMessage (unarchive) failed. ChannelId={Id}", channelId); }
    }

    #region Private Section

    private async Task<MessageDto> GetSystemMessageDto(Guid messageId)
    {
        return await _repositories.MessageRepository
            .FindByCondition(m => m.Id == messageId)
            .AsNoTracking()
            .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
            .FirstAsync();
    }

    #endregion
}
