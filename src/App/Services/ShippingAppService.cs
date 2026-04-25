using Craftsman.App.Models;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Domain.Shipping.Repositories;
using Craftsman.Domain.Shipping.Services;

namespace Craftsman.App.Services;

public sealed class ShippingAppService
{
    private readonly IShipmentRepository shipmentRepository;
    private readonly ShippingService shippingService;

    public ShippingAppService(IShipmentRepository shipmentRepository, ShippingService shippingService)
    {
        this.shipmentRepository = shipmentRepository;
        this.shippingService = shippingService;
    }

    public async Task<IReadOnlyCollection<ShipmentListItemViewModel>> ListAsync(ShipmentStatus? status = null, CancellationToken cancellationToken = default)
    {
        var shipments = await shipmentRepository.ListAsync(status, cancellationToken);

        return shipments.Select(ToViewModel).ToList().AsReadOnly();
    }

    public async Task CreateAsync(CreateShipmentInputModel input, CancellationToken cancellationToken = default)
    {
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
