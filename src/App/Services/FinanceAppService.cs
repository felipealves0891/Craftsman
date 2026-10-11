using Craftsman.App.Models;
using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Finance.Repositories;
using Craftsman.Domain.Finance.Services;
using Craftsman.Domain.Events;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Services;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Domain.Shipping.Repositories;

namespace Craftsman.App.Services;

public sealed class FinanceAppService
{
    private static readonly TimeSpan SummaryCacheExpiration = TimeSpan.FromHours(1);

    private readonly IApplicationCache? cache;
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly IOrderRepository orderRepository;
    private readonly ISettlementCalculator settlementCalculator;
    private readonly IFinancialSettlementRepository settlementRepository;
    private readonly IShipmentRepository shipmentRepository;
    private readonly IUnitOfWork unitOfWork;

    public FinanceAppService(
        IFinancialSettlementRepository settlementRepository,
        ISettlementCalculator settlementCalculator,
        IShipmentRepository shipmentRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher domainEventPublisher,
        IApplicationCache? cache = null)
    {
        this.settlementRepository = settlementRepository;
        this.settlementCalculator = settlementCalculator;
        this.shipmentRepository = shipmentRepository;
        this.orderRepository = orderRepository;
        this.unitOfWork = unitOfWork;
        this.domainEventPublisher = domainEventPublisher;
        this.cache = cache;
    }

    public async Task<FinancialSettlementIndexViewModel> GetIndexAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var settlements = await ListAsync(from, to, cancellationToken);
        var summary = await GetSummaryAsync(from, to, cancellationToken);

        return new FinancialSettlementIndexViewModel(settlements, summary);
    }

    public async Task<IReadOnlyCollection<FinancialSettlementListItemViewModel>> ListAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var settlements = await settlementRepository.ListAsync(from, to, cancellationToken);

        return settlements.Select(ToListItem).ToList().AsReadOnly();
    }

    public async Task<FinancialSettlementSummaryViewModel> GetSummaryAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        if (cache is not null)
        {
            return await cache.GetOrCreateAsync(
                BuildSummaryCacheKey(from, to),
                token => CalculateSummaryAsync(from, to, token),
                SummaryCacheExpiration,
                cancellationToken);
        }

        return await CalculateSummaryAsync(from, to, cancellationToken);
    }

    public async Task<FinancialSettlementDetailViewModel?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var settlement = await settlementRepository.GetByOrderIdAsync(orderId, cancellationToken);

        return settlement is null ? null : ToDetail(settlement);
    }

    public async Task<IReadOnlyCollection<FinancialSettlementPendingViewModel>> ListPendingAsync(CancellationToken cancellationToken = default)
    {
        var deliveredShipments = await shipmentRepository.ListAsync(ShipmentStatus.Delivered, cancellationToken);
        var pending = new List<FinancialSettlementPendingViewModel>();

        foreach (var shipmentGroup in deliveredShipments.GroupBy(shipment => shipment.OrderId))
        {
            if (await settlementRepository.GetByOrderIdAsync(shipmentGroup.Key, cancellationToken) is not null)
            {
                continue;
            }

            var order = await orderRepository.GetByIdAsync(shipmentGroup.Key, cancellationToken);
            if (order is null)
            {
                continue;
            }

            pending.Add(new FinancialSettlementPendingViewModel(
                order.Id,
                order.Origin.Source,
                order.Origin.ExternalOrderId,
                shipmentGroup.Max(shipment => shipment.DeliveredAt)));
        }

        return pending
            .OrderBy(item => item.DeliveredAt ?? DateTimeOffset.MinValue)
            .ToList()
            .AsReadOnly();
    }

    public async Task<FinancialSettlementDetailViewModel> GenerateAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var existingSettlement = await settlementRepository.GetByOrderIdAsync(orderId, cancellationToken);
        if (existingSettlement is not null)
        {
            return ToDetail(existingSettlement);
        }

        var settlement = await settlementCalculator.CalculateAsync(orderId, cancellationToken);
        await settlementRepository.AddAsync(settlement, cancellationToken);

        foreach (var domainEvent in settlement.DomainEvents)
        {
            await domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
        }

        settlement.ClearDomainEvents();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDetail(settlement);
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

    private async Task<FinancialSettlementSummaryViewModel> CalculateSummaryAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var settlements = await settlementRepository.ListAsync(from, to, cancellationToken);
        var orderCount = settlements.Count;
        var totalMargin = settlements.Sum(settlement => settlement.MarginAmount);

        return new FinancialSettlementSummaryViewModel(
            settlements.Sum(settlement => settlement.RevenueAmount),
            settlements.Sum(settlement => settlement.TotalCostAmount),
            totalMargin,
            orderCount == 0 ? 0 : totalMargin / orderCount,
            orderCount);
    }

    private static string BuildSummaryCacheKey(DateOnly? from, DateOnly? to)
    {
        return $"finance:settlement-summary:{from?.ToString("yyyyMMdd") ?? "all"}:{to?.ToString("yyyyMMdd") ?? "all"}";
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
