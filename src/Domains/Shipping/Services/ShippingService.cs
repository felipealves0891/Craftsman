using Craftsman.Domain.Events;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Domain.Shipping.Repositories;

namespace Craftsman.Domain.Shipping.Services;

public sealed class ShippingService
{
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly IOrderRepository orderRepository;
    private readonly IShipmentRepository shipmentRepository;
    private readonly IUnitOfWork unitOfWork;

    public ShippingService(
        IShipmentRepository shipmentRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher domainEventPublisher)
    {
        this.shipmentRepository = shipmentRepository;
        this.orderRepository = orderRepository;
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
                await MarkOrderShippedAsync(shipment.OrderId, cancellationToken);
                break;
            case ShipmentStatus.DeliveryAttempted:
                shipment.MarkDeliveryAttempted();
                break;
            case ShipmentStatus.Delivered:
                shipment.MarkDelivered();
                await MarkOrderDeliveredAsync(shipment.OrderId, cancellationToken);
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

        foreach (var domainEvent in shipment.DomainEvents)
        {
            await domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
        }

        shipment.ClearDomainEvents();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkOrderShippedAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order related to shipment was not found.");

        if (order.Status == OrderStatus.Shipped)
        {
            return;
        }

        if (order.Status != OrderStatus.InProduction)
        {
            throw new InvalidOperationException($"Order cannot be shipped from status {order.Status}.");
        }

        order.MarkShipped();
        await orderRepository.UpdateAsync(order, cancellationToken);
    }

    private async Task MarkOrderDeliveredAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order related to shipment was not found.");

        if (order.Status == OrderStatus.Delivered)
        {
            return;
        }

        if (order.Status != OrderStatus.Shipped)
        {
            throw new InvalidOperationException($"Order cannot be delivered from status {order.Status}.");
        }

        order.MarkDelivered();
        await orderRepository.UpdateAsync(order, cancellationToken);
    }
}
