using Craftsman.App.Models;
using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Finance.Repositories;
using Craftsman.Domain.Finance.Services;
using Craftsman.Domain.Events;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Domain.Shipping.Repositories;

namespace Craftsman.App.Services;

public sealed class FinanceAppService
{
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
        IDomainEventPublisher domainEventPublisher)
    {
        this.settlementRepository = settlementRepository;
        this.settlementCalculator = settlementCalculator;
        this.shipmentRepository = shipmentRepository;
        this.orderRepository = orderRepository;
        this.unitOfWork = unitOfWork;
        this.domainEventPublisher = domainEventPublisher;
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
