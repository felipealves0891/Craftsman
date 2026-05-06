using Craftsman.App.Models;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Domain.Shipping.Repositories;
using Craftsman.Domain.Shipping.Services;

namespace Craftsman.App.Services;

public sealed class ShippingAppService
{
    private readonly IShipmentRepository shipmentRepository;
    private readonly OrderQueryService orderQueryService;
    private readonly ShippingService shippingService;

    public ShippingAppService(IShipmentRepository shipmentRepository, OrderQueryService orderQueryService, ShippingService shippingService)
    {
        this.shipmentRepository = shipmentRepository;
        this.orderQueryService = orderQueryService;
        this.shippingService = shippingService;
    }

    public async Task<IReadOnlyCollection<ShipmentListItemViewModel>> ListAsync(ShipmentStatus? status = null, CancellationToken cancellationToken = default)
    {
        var shipments = await shipmentRepository.ListAsync(status, cancellationToken);

        return shipments.Select(ToViewModel).ToList().AsReadOnly();
    }

    public async Task<CreateShipmentInputModel> BuildCreateModelAsync(Guid? orderId = null, CreateShipmentInputModel? input = null, CancellationToken cancellationToken = default)
    {
        var model = input ?? new CreateShipmentInputModel();

        if (orderId.HasValue)
        {
            model.OrderId = orderId.Value;
        }

        model.AvailableOrders = await orderQueryService.ListShipmentOrderSelectionsAsync(cancellationToken);
        return model;
    }

    public async Task CreateAsync(CreateShipmentInputModel input, CancellationToken cancellationToken = default)
    {
        if (input.OrderId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione um pedido para criar o envio.");
        }

        var order = await orderQueryService.GetShipmentOrderSelectionAsync(input.OrderId, cancellationToken);
        if (order is null)
        {
            throw new InvalidOperationException("Pedido selecionado nao foi encontrado ou nao esta disponivel para envio.");
        }

        await shippingService.CreateAsync(input.OrderId, input.TrackingCode, cancellationToken);
    }

    public Task UpdateStatusAsync(Guid id, ShipmentStatus status, CancellationToken cancellationToken = default)
    {
        return shippingService.UpdateStatusAsync(id, status, cancellationToken);
    }

    public static ShipmentListItemViewModel ToViewModel(Shipment shipment)
    {
        return new ShipmentListItemViewModel(
            shipment.Id,
            shipment.OrderId,
            shipment.TrackingCode,
            shipment.Status.ToString(),
            shipment.CreatedAt,
            shipment.ShippedAt,
            shipment.DeliveredAt);
    }
}
