using Craftsman.Domain.Events;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Domain.Shipping.Repositories;

namespace Craftsman.Domain.Shipping.Services;

public sealed class ShippingService
{
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly IShipmentRepository shipmentRepository;
    private readonly IUnitOfWork unitOfWork;

    public ShippingService(IShipmentRepository shipmentRepository, IUnitOfWork unitOfWork, IDomainEventPublisher domainEventPublisher)
    {
        this.shipmentRepository = shipmentRepository;
        this.unitOfWork = unitOfWork;
        this.domainEventPublisher = domainEventPublisher;
    }

    public async Task<Shipment> CreateAsync(Guid orderId, string? trackingCode, CancellationToken cancellationToken = default)
    {
        var shipment = new Shipment(Guid.NewGuid(), orderId, trackingCode);
        await shipmentRepository.AddAsync(shipment, cancellationToken);

        foreach (var domainEvent in shipment.DomainEvents)
        {
            await domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
        }

        shipment.ClearDomainEvents();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return shipment;
    }

    public async Task UpdateStatusAsync(Guid shipmentId, ShipmentStatus status, CancellationToken cancellationToken = default)
    {
        var shipment = await shipmentRepository.GetByIdAsync(shipmentId, cancellationToken)
            ?? throw new InvalidOperationException("Shipment was not found.");

        switch (status)
        {
            case ShipmentStatus.InTransit:
                shipment.MarkInTransit();
                break;
            case ShipmentStatus.DeliveryAttempted:
                shipment.MarkDeliveryAttempted();
                break;
            case ShipmentStatus.Delivered:
                shipment.MarkDelivered();
                break;
            case ShipmentStatus.Cancelled:
                shipment.Cancel();
                break;
            case ShipmentStatus.Created:
                throw new InvalidOperationException("Shipment cannot be moved back to Created.");
            default:
                throw new InvalidOperationException($"Unsupported shipment status {status}.");
        }

        await shipmentRepository.UpdateAsync(shipment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
