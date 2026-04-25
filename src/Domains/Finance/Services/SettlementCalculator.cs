using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Inventory.Repositories;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Domain.Shipping.Repositories;

namespace Craftsman.Domain.Finance.Services;

public sealed class SettlementCalculator : ISettlementCalculator
{
    private readonly IOrderRepository orderRepository;
    private readonly IProductionTaskRepository productionTaskRepository;
    private readonly IShipmentRepository shipmentRepository;
    private readonly IStockMovementRepository stockMovementRepository;

    public SettlementCalculator(
        IOrderRepository orderRepository,
        IProductionTaskRepository productionTaskRepository,
        IStockMovementRepository stockMovementRepository,
        IShipmentRepository shipmentRepository)
    {
        this.orderRepository = orderRepository;
        this.productionTaskRepository = productionTaskRepository;
        this.stockMovementRepository = stockMovementRepository;
        this.shipmentRepository = shipmentRepository;
    }

    public async Task<FinancialSettlement> CalculateAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order was not found.");
        var shipments = await shipmentRepository.ListAsync(ShipmentStatus.Delivered, cancellationToken);

        if (order.Status != OrderStatus.Delivered && shipments.All(shipment => shipment.OrderId != orderId))
        {
            throw new InvalidOperationException("Financial settlement can only be calculated for delivered orders.");
        }

        var productionTasks = (await productionTaskRepository.ListAsync(cancellationToken: cancellationToken))
            .Where(task => task.OrderId == orderId)
            .ToList();
        var stockMovements = await stockMovementRepository.ListAsync(cancellationToken);
        var productionCost = productionTasks.Sum(task =>
            stockMovements
                .Where(movement => movement.BusinessReference == task.Id.ToString())
                .Sum(movement => movement.TotalCostAmount));
        var revenue = order.Items.Sum(item => item.Quantity * item.UnitPrice.Amount);

        return new FinancialSettlement(
            Guid.NewGuid(),
            order.Id,
            revenue,
            productionCost,
            shippingCostAmount: 0);
    }
}
