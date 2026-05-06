using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Events;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Domain.Shipping.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Shipping;

public sealed class ShippingAppServiceTests
{
    [Fact]
    public async Task Order_selection_returns_available_orders_with_items()
    {
        await using var dbContext = CreateDbContext();
        var orderRepository = new OrderRepository(dbContext);
        var availableOrder = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "PED-001"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 2, new Money(25, "BRL"))],
            shippingDate: new DateOnly(2026, 5, 10));
        var deliveredOrder = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "PED-002"),
            [new OrderItem(Guid.NewGuid(), "ITEM-2", "Carteira", 1, new Money(15, "BRL"))],
            OrderStatus.Delivered);

        await orderRepository.AddAsync(availableOrder);
        await orderRepository.AddAsync(deliveredOrder);
        await dbContext.SaveChangesAsync();
        var service = CreateOrderQueryService(dbContext);

        var orders = await service.ListShipmentOrderSelectionsAsync();

        var order = Assert.Single(orders);
        Assert.Equal(availableOrder.Id, order.Id);
        Assert.Equal("Manual", order.Source);
        Assert.Equal("PED-001", order.ExternalOrderId);
        Assert.Equal(new DateOnly(2026, 5, 10), order.ShippingDate);
        var item = Assert.Single(order.Items);
        Assert.Equal("Bolsa", item.Description);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public async Task Create_shipment_uses_selected_order_id()
    {
        await using var dbContext = CreateDbContext();
        var orderRepository = new OrderRepository(dbContext);
        var shipmentRepository = new ShipmentRepository(dbContext);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "PED-003"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 1, new Money(30, "BRL"))]);
        await orderRepository.AddAsync(order);
        await dbContext.SaveChangesAsync();
        var service = CreateShippingAppService(dbContext);

        await service.CreateAsync(new CreateShipmentInputModel
        {
            OrderId = order.Id,
            TrackingCode = "TRACK-1"
        });

        var shipment = Assert.Single(await shipmentRepository.ListAsync(ShipmentStatus.Created));
        Assert.Equal(order.Id, shipment.OrderId);
        Assert.Equal("TRACK-1", shipment.TrackingCode);
    }

    [Fact]
    public async Task Create_shipment_rejects_missing_or_unavailable_order()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateShippingAppService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateShipmentInputModel { OrderId = Guid.NewGuid() }));

        Assert.Contains("Pedido selecionado", exception.Message);
    }

    private static ShippingAppService CreateShippingAppService(AppDbContext dbContext)
    {
        var shipmentRepository = new ShipmentRepository(dbContext);
        return new ShippingAppService(
            shipmentRepository,
            CreateOrderQueryService(dbContext),
            new ShippingService(shipmentRepository, new UnitOfWork(dbContext), new NoOpDomainEventPublisher()));
    }

    private static OrderQueryService CreateOrderQueryService(AppDbContext dbContext)
    {
        return new OrderQueryService(
            new OrderRepository(dbContext),
            new ProductRepository(dbContext),
            new ProductionTaskRepository(dbContext),
            new ShipmentRepository(dbContext));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed class NoOpDomainEventPublisher : IDomainEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            return Task.CompletedTask;
        }
    }
}
