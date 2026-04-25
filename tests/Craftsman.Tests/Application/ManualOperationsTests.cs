using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Events;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Application;

public sealed class ManualOperationsTests
{
    [Fact]
    public async Task Manual_order_service_creates_manual_order_with_traceable_origin()
    {
        await using var dbContext = CreateDbContext();
        var service = new ManualOrderService(
            new OrderRepository(dbContext),
            new ProductRepository(dbContext),
            new UnitOfWork(dbContext),
            new RecordingDomainEventPublisher());

        var orderId = await service.CreateAsync(new ManualOrderInputModel
        {
            Reference = "MAN-001",
            CustomerName = "Cliente manual",
            Items =
            [
                new ManualOrderItemInputModel
                {
                    ExternalItemId = "ITEM-1",
                    Description = "Produto informado manualmente",
                    Quantity = 2,
                    UnitPriceAmount = 10
                }
            ]
        });

        var loaded = await new OrderRepository(dbContext).GetByIdAsync(orderId);

        Assert.NotNull(loaded);
        Assert.Equal("Manual", loaded.Origin.Source);
        Assert.Equal("MAN-001", loaded.Origin.ExternalOrderId);
        Assert.Single(loaded.Items);
    }

    [Fact]
    public async Task Product_catalog_service_rejects_duplicate_bill_of_materials_items()
    {
        await using var dbContext = CreateDbContext();
        var productRepository = new ProductRepository(dbContext);
        var mappingRepository = new ProductMappingRepository(dbContext);
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var service = new ProductCatalogAppService(
            productRepository,
            mappingRepository,
            rawMaterialRepository,
            new ProductMappingService(mappingRepository),
            new UnitOfWork(dbContext));

        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        await rawMaterialRepository.AddAsync(material);
        var productId = await service.SaveProductAsync(new ProductInputModel { Name = "Bolsa" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveBillOfMaterialsAsync(new BillOfMaterialsInputModel
        {
            ProductId = productId,
            Items =
            [
                new BillOfMaterialsItemInputModel { RawMaterialId = material.Id, QuantityPerUnit = 1 },
                new BillOfMaterialsItemInputModel { RawMaterialId = material.Id, QuantityPerUnit = 2 }
            ]
        }));
    }

    [Fact]
    public async Task Inventory_service_blocks_manual_outbound_that_would_make_balance_negative()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var service = new InventoryAppService(rawMaterialRepository, stockMovementRepository, new UnitOfWork(dbContext));
        var materialId = await service.SaveRawMaterialAsync(new RawMaterialInputModel
        {
            Name = "Linha",
            UnitOfMeasure = "un"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterMovementAsync(new StockMovementInputModel
        {
            RawMaterialId = materialId,
            Type = StockMovementType.Outbound,
            Quantity = 1,
            Reason = "Ajuste manual"
        }));
    }

    [Fact]
    public async Task Inventory_service_records_inbound_unit_cost_for_financial_costing()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var service = new InventoryAppService(rawMaterialRepository, stockMovementRepository, new UnitOfWork(dbContext));
        var materialId = await service.SaveRawMaterialAsync(new RawMaterialInputModel
        {
            Name = "Couro",
            UnitOfMeasure = "m2"
        });

        await service.RegisterMovementAsync(new StockMovementInputModel
        {
            RawMaterialId = materialId,
            Type = StockMovementType.Inbound,
            Quantity = 3,
            UnitCostAmount = 12,
            Reason = "Compra manual",
            BusinessReference = "NF-1"
        });

        var movements = await stockMovementRepository.GetByRawMaterialAsync(materialId);
        var movement = Assert.Single(movements);
        Assert.Equal(12, movement.UnitCostAmount);
        Assert.Equal(36, movement.TotalCostAmount);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed class RecordingDomainEventPublisher : IDomainEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            return Task.CompletedTask;
        }
    }
}
