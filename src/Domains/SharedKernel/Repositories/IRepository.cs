namespace Craftsman.Domain.Repositories;

public interface IRepository<TModel, TId>
    where TModel : class
{
    Task<TModel?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task AddAsync(TModel model, CancellationToken cancellationToken = default);

    Task UpdateAsync(TModel model, CancellationToken cancellationToken = default);
}
