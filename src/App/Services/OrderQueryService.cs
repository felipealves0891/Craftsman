using Craftsman.App.Models;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Shipping.Repositories;

namespace Craftsman.App.Services;

public sealed class OrderQueryService
{
    private readonly IOrderRepository orderRepository;
    private readonly IProductRepository productRepository;
    private readonly IProductionTaskRepository productionTaskRepository;
    private readonly IShipmentRepository shipmentRepository;
    private readonly ProductionPreflightService? productionPreflightService;

    public OrderQueryService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IProductionTaskRepository productionTaskRepository,
        IShipmentRepository shipmentRepository,
        ProductionPreflightService? productionPreflightService = null)
    {
        this.orderRepository = orderRepository;
        this.productRepository = productRepository;
        this.productionTaskRepository = productionTaskRepository;
        this.shipmentRepository = shipmentRepository;
        this.productionPreflightService = productionPreflightService;
    }

    public async Task<IReadOnlyCollection<OrderListItemViewModel>> ListAsync(CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.ListAsync(cancellationToken);

        return orders.Select(order => new OrderListItemViewModel(
            order.Id,
            order.Origin.Source,
            order.Origin.ExternalOrderId,
            order.Customer?.Name,
            order.Discount?.Amount.Amount ?? 0,
            order.Status.ToString(),
            order.Items.Count,
            order.CreatedAt,
            order.ShippingDate)).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyCollection<ShipmentOrderSelectionViewModel>> ListShipmentOrderSelectionsAsync(CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.ListAsync(cancellationToken);

        return orders
            .Where(IsAvailableForShipment)
            .Select(order => new ShipmentOrderSelectionViewModel(
                order.Id,
                order.Origin.Source,
                order.Origin.ExternalOrderId,
                order.Status.ToString(),
                order.ShippingDate,
                order.Items.Select(ToOrderItemViewModel).ToList().AsReadOnly()))
            .ToList()
            .AsReadOnly();
    }

    public async Task<ShipmentOrderSelectionViewModel?> GetShipmentOrderSelectionAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);

        if (order is null || !IsAvailableForShipment(order))
        {
            return null;
        }

        return new ShipmentOrderSelectionViewModel(
            order.Id,
            order.Origin.Source,
            order.Origin.ExternalOrderId,
            order.Status.ToString(),
            order.ShippingDate,
            order.Items.Select(ToOrderItemViewModel).ToList().AsReadOnly());
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

        var productionPreflight = productionPreflightService is null
            ? null
            : await BuildProductionPreflightAsync(orderId, cancellationToken);

        return new OrderDetailViewModel(
            order.Id,
            order.Origin.Source,
            order.Origin.ExternalOrderId,
            order.Customer?.Name,
            order.Customer?.Phone,
            order.Customer?.Email,
            order.Discount?.Description,
            order.Discount?.Amount.Amount ?? 0,
            order.Status.ToString(),
            order.CreatedAt,
            order.ShippingDate,
            order.Items.Select(ToOrderItemViewModel).ToList().AsReadOnly(),
            productionTasks
                .Where(task => task.OrderId == order.Id)
                .Select(task => ProductionScheduleService.ToViewModel(task))
                .ToList()
                .AsReadOnly(),
            shipments.Where(shipment => shipment.OrderId == order.Id).Select(ShippingAppService.ToViewModel).ToList().AsReadOnly(),
            productionPreflight);
    }

    private async Task<ProductionPreflightViewModel> BuildProductionPreflightAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var result = await productionPreflightService!.GetAsync(orderId, cancellationToken);
        var materials = result.Materials
            .Select(material => new ProductionPreflightMaterialViewModel(
                material.RawMaterialId,
                material.Name,
                material.UnitOfMeasure,
                material.RequiredQuantity,
                material.AvailableQuantity,
                material.MissingQuantity,
                material.IsConfigured,
                material.IsAvailable))
            .ToList()
            .AsReadOnly();
        var products = result.Products
            .Select(product => new ProductionPreflightProductViewModel(
                product.ProductId,
                product.Name,
                product.Quantity,
                product.Materials
                    .Select(material => new ProductionPreflightProductMaterialViewModel(
                        material.RawMaterialId,
                        material.Name,
                        material.UnitOfMeasure,
                        material.RequiredQuantity,
                        material.AvailableQuantity,
                        material.MissingQuantity,
                        material.IsConfigured,
                        material.IsAvailable))
                    .ToList()
                    .AsReadOnly()))
            .ToList()
            .AsReadOnly();

        var failureMessage = result.FailureReason switch
        {
            ProductionPlanningFailureReason.AlreadyPlanned => "Este pedido ja possui planejamento de producao.",
            ProductionPlanningFailureReason.MissingProductMapping => "Existe item do pedido sem vinculo com produto interno.",
            ProductionPlanningFailureReason.InvalidProductConfiguration => "Existe produto sem cadastro ativo ou ficha tecnica valida.",
            _ => null
        };

        return new ProductionPreflightViewModel(result.CanConfirm, failureMessage, products, materials);
    }

    private static bool IsAvailableForShipment(Order order)
    {
        return order.Status is not OrderStatus.Cancelled and not OrderStatus.Delivered;
    }

    private static OrderItemViewModel ToOrderItemViewModel(OrderItem item)
    {
        return new OrderItemViewModel(
            item.Id,
            item.ExternalItemId,
            item.Description,
            item.Quantity,
            item.UnitPrice.Amount,
            item.UnitPrice.Currency,
            item.ProductId);
    }
}
