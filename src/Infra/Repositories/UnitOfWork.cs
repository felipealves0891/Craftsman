using Craftsman.Domain.Repositories;
using Craftsman.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics.CodeAnalysis;

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

    [ExcludeFromCodeCoverage]
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
        => await ExecuteInTransactionAsync(operation, IsolationLevel.ReadCommitted, cancellationToken);

    public async Task<T> ExecuteInSerializableTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
        => await ExecuteInTransactionAsync(operation, IsolationLevel.Serializable, cancellationToken);

    [ExcludeFromCodeCoverage]
    private async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return await operation();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
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
