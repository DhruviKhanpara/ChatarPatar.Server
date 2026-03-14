using ChatarPatar.Infrastructure.Persistence;
using ChatarPatar.Infrastructure.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatarPatar.Infrastructure.Repositories;

internal class BaseSoftDeleteRepository<T> : BaseRepository<T>, IBaseSoftDeleteRepository<T> where T : class
{
    private readonly DbSet<T> _dbSet;

    public BaseSoftDeleteRepository(AppDbContext context) : base(context) 
    {
        _dbSet = _context.Set<T>();
    }

    public IQueryable<T> GetAllWithInactive() => _dbSet.IgnoreQueryFilters().AsQueryable();
}
