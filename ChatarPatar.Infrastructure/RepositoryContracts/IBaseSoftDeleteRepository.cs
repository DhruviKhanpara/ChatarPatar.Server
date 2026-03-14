namespace ChatarPatar.Infrastructure.RepositoryContracts;

public interface IBaseSoftDeleteRepository<T> : IBaseRepository<T> where T : class
{
    IQueryable<T> GetAllWithInactive();
}
