using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class FileRepository : BaseSoftDeleteRepository<FileEntity>, IFileRepository
{
    public FileRepository(AppDbContext context) : base(context) { }

    public IQueryable<FileEntity> GetByIdAsync(Guid id) => 
        FindByCondition(x => x.Id == id);
}
