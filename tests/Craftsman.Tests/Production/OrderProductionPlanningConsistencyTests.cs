using Craftsman.Domain.Events;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Craftsman.Tests.Production;

public sealed class OrderProductionPlanningConsistencyTests
{
    [Fact]
    public async Task Production_events_are_published_after_planning_changes_are_saved()
    {
        await using var dbContext = CreateDbContext();
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 2)]);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "EVENT-ORDER-001"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 1, new Money(30, "BRL"), product.Id)]);
        var orderRepository = new OrderRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var materialRepository = new RawMaterialRepository(dbContext);
        var movementRepository = new StockMovementRepository(dbContext);
        var taskRepository = new ProductionTaskRepository(dbContext);
        var unitOfWork = new RecordingUnitOfWork(dbContext);
        var publisher = new RecordingPublisher(() => unitOfWork.SaveCount > 0);

        await materialRepository.AddAsync(material);
        await productRepository.AddAsync(product);
        await movementRepository.AddAsync(new StockMovement(Guid.NewGuid(), material.Id, StockMovementType.Inbound, 2, "Initial stock", null));
        await orderRepository.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var service = new OrderProductionPlanningService(
            orderRepository,
            new ProductionPlanner(productRepository, new InventoryService(materialRepository, movementRepository), taskRepository),
            taskRepository,
            unitOfWork,
            publisher,
            NullLogger<OrderProductionPlanningService>.Instance,
            movementRepository);

        var planned = await service.TryPlanAsync(order.Id, CancellationToken.None);

        Assert.True(planned);
        Assert.Single(publisher.PublishedEvents);
        Assert.All(publisher.PublishedEvents, publishedAfterSave => Assert.True(publishedAfterSave));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class RecordingUnitOfWork(AppDbContext dbContext) : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class RecordingPublisher(Func<bool> wasSaved) : IDomainEventPublisher
    {
        public List<bool> PublishedEvents { get; } = [];

        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            PublishedEvents.Add(wasSaved());
            return Task.CompletedTask;
        }
    }
}
