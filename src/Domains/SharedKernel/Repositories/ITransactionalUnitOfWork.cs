namespace Craftsman.Domain.Repositories;

public interface ITransactionalUnitOfWork : IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default);
}
