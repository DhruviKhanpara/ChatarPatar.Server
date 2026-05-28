using ChatarPatar.Infrastructure.Entities;

namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IFileRepository : IBaseSoftDeleteRepository<FileEntity>
{
    IQueryable<FileEntity> GetByIdAsync(Guid id) => FindByCondition(x => x.Id == id).AsQueryable();
    Task<List<FileEntity>> GetPendingAttachmentsByIdsAsync(List<Guid> fileIds, Guid uploadedByUserId);
    IQueryable<FileEntity> GetExpiredPendingAttachmentsQuery();
}
