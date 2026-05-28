using ChatarPatar.Common.Enums;
using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class FileRepository : BaseSoftDeleteRepository<FileEntity>, IFileRepository
{
    public FileRepository(AppDbContext context) : base(context) { }

    public IQueryable<FileEntity> GetByIdAsync(Guid id) => 
        FindByCondition(x => x.Id == id);

    public Task<List<FileEntity>> GetPendingAttachmentsByIdsAsync(List<Guid> fileIds, Guid uploadedByUserId)
    {
        return FindByCondition(f =>
                fileIds.Contains(f.Id) &&
                f.UploadedByUserId == uploadedByUserId &&
                f.UsageContext == FileUsageContextEnum.Attachment &&
                f.Status == FileStatusEnum.Pending &&
                !f.IsDeleted)
            .ToListAsync();
    }

    public IQueryable<FileEntity> GetExpiredPendingAttachmentsQuery()
    {
        return GetAllWithInactive()
            .Where(f =>
                f.Status == FileStatusEnum.Pending &&
                !f.IsDeleted &&
                f.ExpiresAt != null &&
                f.ExpiresAt < DateTime.UtcNow);
    }
}
