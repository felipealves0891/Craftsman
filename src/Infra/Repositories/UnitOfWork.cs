using Craftsman.Domain.Repositories;
using Craftsman.Infra.Persistence;

namespace Craftsman.Infra.Repositories;

public sealed class UnitOfWork : ITransactionalUnitOfWork
{
    private readonly AppDbContext dbContext;

    public UnitOfWork(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
