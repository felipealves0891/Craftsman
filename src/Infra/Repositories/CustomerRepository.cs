using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly IApplicationCache? cache;
    private readonly AppDbContext dbContext;

    public CustomerRepository(AppDbContext dbContext, IApplicationCache? cache = null)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<Customer?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(customer => customer.NormalizedName == normalizedName, cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyCollection<Customer>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (cache is not null)
        {
            return await cache.GetOrCreateAsync("sales:customers:list", ListCoreAsync, cancellationToken: cancellationToken);
        }

        return await ListCoreAsync(cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await dbContext.Customers.AddAsync(ToEntity(customer), cancellationToken);
        cache?.RemoveByPrefix("sales:customers:");
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Customers.FirstOrDefaultAsync(existing => existing.Id == customer.Id, cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException($"Customer '{customer.Id}' was not found.");
        }

        entity.Name = customer.Name;
        entity.Phone = customer.Phone;
        entity.Email = customer.Email;
        cache?.RemoveByPrefix("sales:customers:");
    }

    private async Task<IReadOnlyCollection<Customer>> ListCoreAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList().AsReadOnly();
    }

    private static Customer ToModel(CustomerEntity entity)
    {
        return new Customer(entity.Id, entity.Name, entity.Phone, entity.Email, entity.CreatedAt);
    }

    private static CustomerEntity ToEntity(Customer customer)
    {
        return new CustomerEntity
        {
            Id = customer.Id,
            Name = customer.Name,
            NormalizedName = customer.NormalizedName,
            Phone = customer.Phone,
            Email = customer.Email,
            CreatedAt = customer.CreatedAt
        };
    }
}
