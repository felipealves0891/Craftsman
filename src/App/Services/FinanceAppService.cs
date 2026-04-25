using Craftsman.App.Models;
using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Finance.Repositories;

namespace Craftsman.App.Services;

public sealed class FinanceAppService
{
    private readonly IFinancialSettlementRepository settlementRepository;

    public FinanceAppService(IFinancialSettlementRepository settlementRepository)
    {
        this.settlementRepository = settlementRepository;
    }

    public async Task<IReadOnlyCollection<FinancialSettlementListItemViewModel>> ListAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var settlements = await settlementRepository.ListAsync(from, to, cancellationToken);

        return settlements.Select(ToListItem).ToList().AsReadOnly();
    }

    public async Task<FinancialSettlementDetailViewModel?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var settlement = await settlementRepository.GetByOrderIdAsync(orderId, cancellationToken);

        return settlement is null ? null : ToDetail(settlement);
    }

    private static FinancialSettlementListItemViewModel ToListItem(FinancialSettlement settlement)
    {
        return new FinancialSettlementListItemViewModel(
            settlement.Id,
            settlement.OrderId,
            settlement.RevenueAmount,
            settlement.ProductionCostAmount,
            settlement.ShippingCostAmount,
            settlement.TotalCostAmount,
            settlement.MarginAmount,
            settlement.Status.ToString(),
            settlement.CalculatedAt);
    }

    private static FinancialSettlementDetailViewModel ToDetail(FinancialSettlement settlement)
    {
        return new FinancialSettlementDetailViewModel(
            settlement.Id,
            settlement.OrderId,
            settlement.RevenueAmount,
            settlement.ProductionCostAmount,
            settlement.ShippingCostAmount,
            settlement.TotalCostAmount,
            settlement.MarginAmount,
            settlement.Status.ToString(),
            settlement.CalculatedAt);
    }
}
