using ChatarPatar.Infrastructure.Entities;
using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;

namespace ChatarPatar.Infrastructure.Repositories;

internal class FileRepository : BaseSoftDeleteRepository<Files>, IFileRepository
{
    public FileRepository(AppDbContext context) : base(context) { }
}
