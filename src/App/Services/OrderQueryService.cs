using Craftsman.App.Models;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Shipping.Repositories;

namespace Craftsman.App.Services;

public sealed class OrderQueryService
{
    private readonly IOrderRepository orderRepository;
    private readonly IProductionTaskRepository productionTaskRepository;
    private readonly IShipmentRepository shipmentRepository;

    public OrderQueryService(
        IOrderRepository orderRepository,
        IProductionTaskRepository productionTaskRepository,
        IShipmentRepository shipmentRepository)
    {
        this.orderRepository = orderRepository;
        this.productionTaskRepository = productionTaskRepository;
        this.shipmentRepository = shipmentRepository;
    }

    public async Task<IReadOnlyCollection<OrderListItemViewModel>> ListAsync(CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.ListAsync(cancellationToken);

        return orders.Select(order => new OrderListItemViewModel(
            order.Id,
            order.Origin.Source,
            order.Origin.ExternalOrderId,
            order.Customer.Name,
            order.Status.ToString(),
            order.Items.Count,
            order.CreatedAt)).ToList().AsReadOnly();
    }

    public async Task<OrderDetailViewModel?> GetDetailAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        var productionTasks = await productionTaskRepository.ListAsync(cancellationToken: cancellationToken);
        var shipments = await shipmentRepository.ListAsync(cancellationToken: cancellationToken);

        return new OrderDetailViewModel(
            order.Id,
            order.Origin.Source,
            order.Origin.ExternalOrderId,
            order.Customer.Name,
            order.Customer.Email,
            order.Status.ToString(),
            order.CreatedAt,
            order.Items.Select(item => new OrderItemViewModel(
                item.Id,
                item.ExternalItemId,
                item.Description,
                item.Quantity,
                item.UnitPrice.Amount,
                item.UnitPrice.Currency,
                item.ProductId)).ToList().AsReadOnly(),
            productionTasks.Where(task => task.OrderId == order.Id).Select(ProductionScheduleService.ToViewModel).ToList().AsReadOnly(),
            shipments.Where(shipment => shipment.OrderId == order.Id).Select(ShippingAppService.ToViewModel).ToList().AsReadOnly());
    }
}
