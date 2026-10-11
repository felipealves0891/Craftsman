using Craftsman.Domain.Sales.Entities;

namespace Craftsman.Domain.Sales.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Customer?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Customer>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
}
