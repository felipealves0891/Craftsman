using Craftsman.Domain.Finance.Entities;

namespace Craftsman.Domain.Finance.Services;

public interface ISettlementCalculator
{
    Task<FinancialSettlement> CalculateAsync(Guid orderId, CancellationToken cancellationToken = default);
}
