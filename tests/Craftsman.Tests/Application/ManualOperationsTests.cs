using Craftsman.App.Models;
using Craftsman.App.Controllers;
using Craftsman.App.Services;
using Craftsman.Domain.Events;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.Integration.Services;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Services;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Craftsman.Infra.Repositories;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Craftsman.Tests.Application;

public sealed class ManualOperationsTests
{
    [Fact]
    public async Task Manual_order_service_creates_manual_order_with_traceable_origin()
    {
        await using var dbContext = CreateDbContext();
        var orderRepository = new OrderRepository(dbContext);
        var orderSourceRepository = new OrderSourceCatalogRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        await SeedOrderSourceAsync(dbContext, "WhatsApp");
        var service = new ManualOrderService(
            orderRepository,
            orderSourceRepository,
            productRepository,
            new OrderProductionPlanningService(
                orderRepository,
                new ProductionPlanner(productRepository, new InventoryService(rawMaterialRepository, stockMovementRepository)),
                productionTaskRepository,
                unitOfWork,
                new RecordingDomainEventPublisher(),
                NullLogger<OrderProductionPlanningService>.Instance),
            unitOfWork,
            new RecordingDomainEventPublisher());

        var orderId = await service.CreateAsync(new ManualOrderInputModel
        {
            Source = "WhatsApp",
            Reference = "MAN-001",
            ShippingDate = new DateOnly(2026, 5, 3),
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
        Assert.Equal("WhatsApp", loaded.Origin.Source);
        Assert.Equal("MAN-001", loaded.Origin.ExternalOrderId);
        Assert.Equal(new DateOnly(2026, 5, 3), loaded.ShippingDate);
        Assert.Single(loaded.Items);
    }

    [Fact]
    public async Task Manual_order_service_creates_new_order_source_and_rejects_duplicate()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateManualOrderService(dbContext);

        await service.CreateOrderSourceAsync(new OrderSourceInputModel { Name = "Elo7" });

        var sources = await service.ListOrderSourcesAsync();
        Assert.Contains("Elo7", sources);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateOrderSourceAsync(new OrderSourceInputModel { Name = "Elo7" }));
    }

    [Fact]
    public async Task Manual_order_service_requires_shipping_date()
    {
        await using var dbContext = CreateDbContext();
        await SeedOrderSourceAsync(dbContext, "Manual");
        var service = CreateManualOrderService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new ManualOrderInputModel
        {
            Source = "Manual",
            Reference = "MAN-NO-DATE",
            Items =
            [
                new ManualOrderItemInputModel
                {
                    Description = "Produto informado manualmente",
                    Quantity = 1,
                    UnitPriceAmount = 10
                }
            ]
        }));

        Assert.Contains("Data de envio", exception.Message);
    }

    [Fact]
    public async Task Manual_order_service_plans_production_when_items_have_internal_products()
    {
        await using var dbContext = CreateDbContext();
        var orderRepository = new OrderRepository(dbContext);
        var orderSourceRepository = new OrderSourceCatalogRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var publisher = new RecordingDomainEventPublisher();
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 2)]);

        await SeedOrderSourceAsync(dbContext, "Manual");
        await rawMaterialRepository.AddAsync(material);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), material.Id, StockMovementType.Inbound, 10, "Compra", null));
        await productRepository.AddAsync(product);
        await dbContext.SaveChangesAsync();

        var service = new ManualOrderService(
            orderRepository,
            orderSourceRepository,
            productRepository,
            new OrderProductionPlanningService(
                orderRepository,
                new ProductionPlanner(productRepository, new InventoryService(rawMaterialRepository, stockMovementRepository)),
                productionTaskRepository,
                unitOfWork,
                publisher,
                NullLogger<OrderProductionPlanningService>.Instance),
            unitOfWork,
            publisher);

        var orderId = await service.CreateAsync(new ManualOrderInputModel
        {
            Source = "Manual",
            Reference = "MAN-PROD-001",
            ShippingDate = new DateOnly(2026, 5, 4),
            Items =
            [
                new ManualOrderItemInputModel
                {
                    ExternalItemId = "ITEM-1",
                    Description = "Bolsa",
                    Quantity = 2,
                    UnitPriceAmount = 10,
                    ProductId = product.Id
                }
            ]
        });

        var productionTasks = await productionTaskRepository.ListAsync();
        var productionTask = Assert.Single(productionTasks);
        var loaded = await orderRepository.GetByIdAsync(orderId);

        Assert.Equal(orderId, productionTask.OrderId);
        Assert.Equal(OrderStatus.ReadyForProduction, loaded?.Status);
        Assert.Equal(6, await stockMovementRepository.GetBalanceAsync(material.Id));
    }

    [Fact]
    public async Task Orders_controller_sends_order_to_production_manually()
    {
        await using var dbContext = CreateDbContext();
        var orderRepository = new OrderRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var shipmentRepository = new ShipmentRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var publisher = new RecordingDomainEventPublisher();
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 2)]);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Shopee", "SO-MANUAL-PROD-001"),
            [
                new OrderItem(
                    Guid.NewGuid(),
                    "ITEM-1",
                    "Bolsa",
                    2,
                    new Money(10, "BRL"),
                    product.Id)
            ]);

        await rawMaterialRepository.AddAsync(material);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), material.Id, StockMovementType.Inbound, 10, "Compra", null));
        await productRepository.AddAsync(product);
        await orderRepository.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var planningService = new OrderProductionPlanningService(
            orderRepository,
            new ProductionPlanner(productRepository, new InventoryService(rawMaterialRepository, stockMovementRepository)),
            productionTaskRepository,
            unitOfWork,
            publisher,
            NullLogger<OrderProductionPlanningService>.Instance);
        var controller = new OrdersController(
            new OrderQueryService(orderRepository, productRepository, productionTaskRepository, shipmentRepository),
            new EmptyOrderImportPipeline(),
            planningService,
            new NoOpAuditService())
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider())
        };

        var result = await controller.SendToProduction(order.Id, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(order.Id, redirect.RouteValues?["id"]);
        Assert.Equal("Pedido enviado para producao.", controller.TempData["Success"]);
        var productionTask = Assert.Single(await productionTaskRepository.ListAsync());
        var loaded = await orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(order.Id, productionTask.OrderId);
        Assert.Equal(OrderStatus.ReadyForProduction, loaded?.Status);
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
    public async Task Product_catalog_service_saves_updates_and_clears_barcode()
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

        var productId = await service.SaveProductAsync(new ProductInputModel
        {
            Name = "Bolsa",
            Barcode = " 7891000315507 "
        });

        var created = await service.GetProductInputAsync(productId);
        Assert.Equal("7891000315507", created?.Barcode);

        await service.SaveProductAsync(new ProductInputModel
        {
            Id = productId,
            Name = "Bolsa",
            Status = ProductStatus.Active,
            ProductionDurationDays = 1,
            Barcode = " "
        });

        var updated = await service.GetProductInputAsync(productId);
        Assert.Null(updated?.Barcode);
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

    private static ManualOrderService CreateManualOrderService(AppDbContext dbContext)
    {
        var orderRepository = new OrderRepository(dbContext);
        var orderSourceRepository = new OrderSourceCatalogRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var publisher = new RecordingDomainEventPublisher();

        return new ManualOrderService(
            orderRepository,
            orderSourceRepository,
            productRepository,
            new OrderProductionPlanningService(
                orderRepository,
                new ProductionPlanner(productRepository, new InventoryService(rawMaterialRepository, stockMovementRepository)),
                productionTaskRepository,
                unitOfWork,
                publisher,
                NullLogger<OrderProductionPlanningService>.Instance),
            unitOfWork,
            publisher);
    }

    private static async Task SeedOrderSourceAsync(AppDbContext dbContext, string name)
    {
        await dbContext.OrderSources.AddAsync(new OrderSourceEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    private sealed class RecordingDomainEventPublisher : IDomainEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyOrderImportPipeline : IOrderImportPipeline
    {
        public Task<OrderImportResult> ImportAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OrderImportResult(0, 0, []));
        }
    }

    private sealed class NoOpAuditService : IAuditService
    {
        public Task RecordAsync(
            string action,
            string entityName,
            string? entityId = null,
            object? before = null,
            object? after = null,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
