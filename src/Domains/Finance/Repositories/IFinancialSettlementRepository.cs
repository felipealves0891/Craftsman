using Craftsman.Domain.Finance.Entities;

namespace Craftsman.Domain.Finance.Repositories;

public interface IFinancialSettlementRepository
{
    Task<FinancialSettlement?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FinancialSettlement>> ListAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default);

    Task AddAsync(FinancialSettlement settlement, CancellationToken cancellationToken = default);
}
