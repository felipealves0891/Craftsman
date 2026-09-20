using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Events;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Domain.Shipping.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Craftsman.Infra.Security;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Integration;

public sealed class EfCoreConcurrencyExceptionTests
{
    [Fact]
    public async Task Production_schedule_advances_loaded_task_without_ef_concurrency_error()
    {
        await using var dbContext = CreateDbContext();
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var orderRepository = new OrderRepository(dbContext);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "PROD-CONCURRENCY"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 1, new Money(30, "BRL"))],
            OrderStatus.ReadyForProduction);
        var task = new ProductionTask(Guid.NewGuid(), order.Id, Guid.NewGuid(), Guid.NewGuid(), 1);
        var service = new ProductionScheduleService(
            productionTaskRepository,
            new EmptyProductRepository(),
            orderRepository,
            new UnitOfWork(dbContext));

        await orderRepository.AddAsync(order);
        await productionTaskRepository.AddAsync(task);
        await dbContext.SaveChangesAsync();

        await service.AdvanceAsync(task.Id, "start");

        dbContext.ChangeTracker.Clear();
        var persisted = await productionTaskRepository.GetByIdAsync(task.Id);

        Assert.NotNull(persisted);
        Assert.Equal(ProductionTaskStatus.InProduction, persisted.Status);
        Assert.NotNull(persisted.StartedAt);
        var persistedOrder = await orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.InProduction, persistedOrder?.Status);
    }

    [Fact]
    public async Task Inventory_service_updates_loaded_raw_material_without_ef_concurrency_error()
    {
        await using var dbContext = CreateDbContext();
        var service = new InventoryAppService(
            new RawMaterialRepository(dbContext),
            new StockMovementRepository(dbContext),
            new UnitOfWork(dbContext));
        var materialId = await service.SaveRawMaterialAsync(new RawMaterialInputModel
        {
            Name = "Linha",
            UnitOfMeasure = "un"
        });

        await service.SaveRawMaterialAsync(new RawMaterialInputModel
        {
            Id = materialId,
            Name = "Linha premium",
            UnitOfMeasure = "rolo",
            Status = RawMaterialStatus.Inactive
        });

        dbContext.ChangeTracker.Clear();
        var persisted = await new RawMaterialRepository(dbContext).GetByIdAsync(materialId);

        Assert.NotNull(persisted);
        Assert.Equal("Linha premium", persisted.Name);
        Assert.Equal("rolo", persisted.UnitOfMeasure);
        Assert.Equal(RawMaterialStatus.Inactive, persisted.Status);
    }

    [Fact]
    public async Task Shipping_service_updates_loaded_shipment_without_ef_concurrency_error()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ShipmentRepository(dbContext);
        var orderRepository = new OrderRepository(dbContext);
        var service = new ShippingService(repository, orderRepository, new UnitOfWork(dbContext), new NoOpDomainEventPublisher());
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "SHIP-CONCURRENCY"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 1, new Money(30, "BRL"))],
            OrderStatus.InProduction);
        var shipment = new Shipment(Guid.NewGuid(), order.Id, "TRACK-1");

        await orderRepository.AddAsync(order);
        await repository.AddAsync(shipment);
        await dbContext.SaveChangesAsync();

        await service.UpdateStatusAsync(shipment.Id, ShipmentStatus.InTransit);

        dbContext.ChangeTracker.Clear();
        var persisted = await repository.GetByIdAsync(shipment.Id);

        Assert.NotNull(persisted);
        Assert.Equal(ShipmentStatus.InTransit, persisted.Status);
        Assert.NotNull(persisted.ShippedAt);
        var persistedOrder = await orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Shipped, persistedOrder?.Status);
    }

    [Fact]
    public async Task Updating_missing_raw_material_fails_with_clear_error()
    {
        await using var dbContext = CreateDbContext();
        var repository = new RawMaterialRepository(dbContext);
        var material = new RawMaterial(Guid.NewGuid(), "Linha", "un");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(material));

        Assert.Contains("Raw material", exception.Message);
        Assert.Contains(material.Id.ToString(), exception.Message);
    }

    [Fact]
    public async Task Auditing_still_records_repository_updates()
    {
        await using var dbContext = CreateDbContext(withCurrentUser: true);
        var repository = new RawMaterialRepository(dbContext);
        var material = new RawMaterial(Guid.NewGuid(), "Linha", "un");

        await repository.AddAsync(material);
        await dbContext.SaveChangesAsync();

        var loaded = await repository.GetByIdAsync(material.Id);
        Assert.NotNull(loaded);
        loaded.Deactivate();
        await repository.UpdateAsync(loaded);
        await dbContext.SaveChangesAsync();

        var modifiedLog = await dbContext.AuditLogs
            .Where(log => log.EntityName == "RawMaterialEntity" && log.Action == AuditAction.Modified)
            .SingleAsync();

        Assert.Equal(7, modifiedLog.UserId);
        Assert.Equal("operador@craftsman.local", modifiedLog.UserName);
        Assert.Contains("Inactive", modifiedLog.AfterJson);
    }

    private static AppDbContext CreateDbContext(bool withCurrentUser = false)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        if (!withCurrentUser)
        {
            return new AppDbContext(options);
        }

        return new AppDbContext(
            options,
            new StaticCurrentUserContext(new CurrentUserInfo(7, "operador@craftsman.local", ["Operador"], "corr-test", "/test")));
    }

    private sealed class NoOpDomainEventPublisher : IDomainEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyProductRepository : IProductRepository
    {
        public Task<Craftsman.Domain.ProductCatalog.Entities.Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Craftsman.Domain.ProductCatalog.Entities.Product?>(null);

        public Task<IReadOnlyCollection<Craftsman.Domain.ProductCatalog.Entities.Product>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Craftsman.Domain.ProductCatalog.Entities.Product>>([]);

        public Task AddAsync(Craftsman.Domain.ProductCatalog.Entities.Product product, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(Craftsman.Domain.ProductCatalog.Entities.Product product, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class EmptyOrderRepository : IOrderRepository
    {
        public Task<Craftsman.Domain.Sales.Entities.Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Craftsman.Domain.Sales.Entities.Order?>(null);

        public Task<Craftsman.Domain.Sales.Entities.Order?> GetByOriginAsync(Craftsman.Domain.Sales.ObjectValues.OrderOrigin origin, CancellationToken cancellationToken = default)
            => Task.FromResult<Craftsman.Domain.Sales.Entities.Order?>(null);

        public Task<IReadOnlyCollection<Craftsman.Domain.Sales.Entities.Order>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Craftsman.Domain.Sales.Entities.Order>>([]);

        public Task AddAsync(Craftsman.Domain.Sales.Entities.Order order, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(Craftsman.Domain.Sales.Entities.Order order, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
