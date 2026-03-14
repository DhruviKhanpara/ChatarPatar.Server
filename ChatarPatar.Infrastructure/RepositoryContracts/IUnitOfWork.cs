namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IUnitOfWork
{
    int SaveChanges();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
