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
            productionTaskRepository,
            stockMovementRepository,
            new ShipmentRepository(dbContext),
            new OrderProductionPlanningService(
                orderRepository,
                new ProductionPlanner(productRepository, new InventoryService(rawMaterialRepository, stockMovementRepository)),
                productionTaskRepository,
                unitOfWork,
                new RecordingDomainEventPublisher(),
                NullLogger<OrderProductionPlanningService>.Instance),
            unitOfWork,
            new RecordingDomainEventPublisher());

        var shippingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);
        var orderId = await service.CreateAsync(new ManualOrderInputModel
        {
            Source = "WhatsApp",
            Reference = "MAN-001",
            ShippingDate = shippingDate,
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
        Assert.Equal(shippingDate, loaded.ShippingDate);
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
    public async Task Manual_order_service_requires_future_shipping_date_in_utc()
    {
        await using var dbContext = CreateDbContext();
        await SeedOrderSourceAsync(dbContext, "Manual");
        var service = CreateManualOrderService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new ManualOrderInputModel
        {
            Source = "Manual",
            Reference = "MAN-PAST-DATE",
            ShippingDate = DateOnly.FromDateTime(DateTime.UtcNow),
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

        Assert.Contains("futura", exception.Message);
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
            productionTaskRepository,
            stockMovementRepository,
            new ShipmentRepository(dbContext),
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
            ShippingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10),
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
    public async Task Manual_order_service_update_recalculates_planned_stock_for_changed_quantity()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateManualOrderService(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 2)]);

        await SeedOrderSourceAsync(dbContext, "Manual");
        await rawMaterialRepository.AddAsync(material);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), material.Id, StockMovementType.Inbound, 20, "Compra", null));
        await productRepository.AddAsync(product);
        await dbContext.SaveChangesAsync();

        var shippingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);
        var orderId = await service.CreateAsync(new ManualOrderInputModel
        {
            Source = "Manual",
            Reference = "MAN-EDIT-001",
            ShippingDate = shippingDate,
            Items =
            [
                new ManualOrderItemInputModel
                {
                    Description = "Bolsa",
                    Quantity = 2,
                    UnitPriceAmount = 10,
                    ProductId = product.Id
                }
            ]
        });

        await service.UpdateAsync(orderId, new ManualOrderInputModel
        {
            Source = "Manual",
            Reference = "MAN-EDIT-001",
            ShippingDate = shippingDate,
            Items =
            [
                new ManualOrderItemInputModel
                {
                    Description = "Bolsa atualizada",
                    Quantity = 3,
                    UnitPriceAmount = 10,
                    ProductId = product.Id
                }
            ]
        });

        var loaded = await new OrderRepository(dbContext).GetByIdAsync(orderId);
        var productionTask = Assert.Single(await productionTaskRepository.ListByOrderAsync(orderId));

        Assert.Equal(3, loaded?.Items.Single().Quantity);
        Assert.Equal(3, productionTask.Quantity);
        Assert.Equal(14, await stockMovementRepository.GetBalanceAsync(material.Id));
    }

    [Fact]
    public async Task Manual_order_service_update_to_unlinked_item_reverses_old_planning()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateManualOrderService(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var orderRepository = new OrderRepository(dbContext);
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 2)]);

        await SeedOrderSourceAsync(dbContext, "Manual");
        await rawMaterialRepository.AddAsync(material);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), material.Id, StockMovementType.Inbound, 20, "Compra", null));
        await productRepository.AddAsync(product);
        await dbContext.SaveChangesAsync();

        var shippingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);
        var orderId = await service.CreateAsync(new ManualOrderInputModel
        {
            Source = "Manual",
            Reference = "MAN-UNLINK-001",
            ShippingDate = shippingDate,
            Items =
            [
                new ManualOrderItemInputModel
                {
                    Description = "Bolsa",
                    Quantity = 2,
                    UnitPriceAmount = 10,
                    ProductId = product.Id
                }
            ]
        });

        await service.UpdateAsync(orderId, new ManualOrderInputModel
        {
            Source = "Manual",
            Reference = "MAN-UNLINK-001",
            ShippingDate = shippingDate,
            Items =
            [
                new ManualOrderItemInputModel
                {
                    Description = "Bolsa sem vinculo",
                    Quantity = 2,
                    UnitPriceAmount = 10
                }
            ]
        });

        var loaded = await orderRepository.GetByIdAsync(orderId);

        Assert.Equal(20, await stockMovementRepository.GetBalanceAsync(material.Id));
        Assert.Empty(await productionTaskRepository.ListByOrderAsync(orderId));
        Assert.Equal(OrderStatus.Normalized, loaded?.Status);
    }

    [Fact]
    public async Task Manual_order_service_delete_reverses_planned_stock_and_removes_order()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateManualOrderService(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var orderRepository = new OrderRepository(dbContext);
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 2)]);

        await SeedOrderSourceAsync(dbContext, "Manual");
        await rawMaterialRepository.AddAsync(material);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), material.Id, StockMovementType.Inbound, 20, "Compra", null));
        await productRepository.AddAsync(product);
        await dbContext.SaveChangesAsync();

        var orderId = await service.CreateAsync(new ManualOrderInputModel
        {
            Source = "Manual",
            Reference = "MAN-DELETE-001",
            ShippingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10),
            Items =
            [
                new ManualOrderItemInputModel
                {
                    Description = "Bolsa",
                    Quantity = 2,
                    UnitPriceAmount = 10,
                    ProductId = product.Id
                }
            ]
        });

        await service.DeleteAsync(orderId);

        Assert.Null(await orderRepository.GetByIdAsync(orderId));
        Assert.Empty(await productionTaskRepository.ListByOrderAsync(orderId));
        Assert.Equal(20, await stockMovementRepository.GetBalanceAsync(material.Id));
    }

    [Fact]
    public async Task Manual_order_service_blocks_update_when_production_started()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateManualOrderService(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 2)]);

        await SeedOrderSourceAsync(dbContext, "Manual");
        await rawMaterialRepository.AddAsync(material);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), material.Id, StockMovementType.Inbound, 20, "Compra", null));
        await productRepository.AddAsync(product);
        await dbContext.SaveChangesAsync();

        var shippingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);
        var orderId = await service.CreateAsync(new ManualOrderInputModel
        {
            Source = "Manual",
            Reference = "MAN-STARTED-001",
            ShippingDate = shippingDate,
            Items =
            [
                new ManualOrderItemInputModel
                {
                    Description = "Bolsa",
                    Quantity = 2,
                    UnitPriceAmount = 10,
                    ProductId = product.Id
                }
            ]
        });
        var task = Assert.Single(await productionTaskRepository.ListByOrderAsync(orderId));
        task.Start();
        await productionTaskRepository.UpdateAsync(task);
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(orderId, new ManualOrderInputModel
        {
            Source = "Manual",
            Reference = "MAN-STARTED-001",
            ShippingDate = shippingDate,
            Items =
            [
                new ManualOrderItemInputModel
                {
                    Description = "Bolsa",
                    Quantity = 1,
                    UnitPriceAmount = 10,
                    ProductId = product.Id
                }
            ]
        }));

        Assert.Contains("producao ja foi iniciada", exception.Message);
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
            CreateManualOrderService(dbContext),
            new EmptyOrderImportPipeline(),
            planningService,
            new StockAlertAppService(rawMaterialRepository, stockMovementRepository, orderRepository, productRepository),
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
            new UnitOfWork(dbContext),
            NullLogger<ProductCatalogAppService>.Instance);

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
    public async Task Product_catalog_service_saves_more_than_five_bill_of_materials_items()
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
            new UnitOfWork(dbContext),
            NullLogger<ProductCatalogAppService>.Instance);

        var materials = Enumerable.Range(1, 6)
            .Select(index => new RawMaterial(Guid.NewGuid(), $"Material {index}", "un"))
            .ToList();

        foreach (var material in materials)
        {
            await rawMaterialRepository.AddAsync(material);
        }

        var productId = await service.SaveProductAsync(new ProductInputModel { Name = "Kit" });

        await service.SaveBillOfMaterialsAsync(new BillOfMaterialsInputModel
        {
            ProductId = productId,
            Items = materials
                .Select((material, index) => new BillOfMaterialsItemInputModel
                {
                    RawMaterialId = material.Id,
                    QuantityPerUnit = index + 1
                })
                .ToList()
        });

        var product = await productRepository.GetByIdAsync(productId);

        Assert.NotNull(product);
        Assert.Equal(6, product!.BillOfMaterials.Count);
        Assert.Equal([1, 2, 3, 4, 5, 6], product.BillOfMaterials.Select(item => (int)item.QuantityPerUnit));
    }

    [Fact]
    public async Task Product_catalog_service_saves_updates_production_hours_and_hourly_rate()
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
            new UnitOfWork(dbContext),
            NullLogger<ProductCatalogAppService>.Instance);

        var productId = await service.SaveProductAsync(new ProductInputModel
        {
            Name = "Bolsa",
            ProductionDurationHours = 6,
            HourlyRate = 25.50m
        });

        var created = await service.GetProductInputAsync(productId);
        Assert.Equal(6, created?.ProductionDurationHours);
        Assert.Equal(25.50m, created?.HourlyRate);

        await service.SaveProductAsync(new ProductInputModel
        {
            Id = productId,
            Name = "Bolsa",
            Status = ProductStatus.Active,
            ProductionDurationHours = 8,
            HourlyRate = 30m
        });

        var updated = await service.GetProductInputAsync(productId);
        Assert.Equal(8, updated?.ProductionDurationHours);
        Assert.Equal(30m, updated?.HourlyRate);
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

    [Fact]
    public async Task Inventory_service_saves_updates_and_lists_low_stock_levels()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var service = new InventoryAppService(rawMaterialRepository, stockMovementRepository, new UnitOfWork(dbContext));

        var materialId = await service.SaveRawMaterialAsync(new RawMaterialInputModel
        {
            Name = "Linha",
            UnitOfMeasure = "un",
            MinimumStockLevel = 10,
            CriticalStockLevel = 3
        });

        await service.SaveRawMaterialAsync(new RawMaterialInputModel
        {
            Id = materialId,
            Name = "Linha",
            UnitOfMeasure = "un",
            Status = RawMaterialStatus.Active,
            MinimumStockLevel = 8,
            CriticalStockLevel = 2
        });

        var input = await service.GetRawMaterialInputAsync(materialId);
        var materials = await service.ListRawMaterialsAsync();

        Assert.Equal(8, input?.MinimumStockLevel);
        Assert.Equal(2, input?.CriticalStockLevel);
        var listed = Assert.Single(materials);
        Assert.Equal(8, listed.MinimumStockLevel);
        Assert.Equal(2, listed.CriticalStockLevel);
    }

    [Fact]
    public async Task Stock_alert_service_returns_critical_alerts_before_warnings_and_only_critical_for_movements()
    {
        await using var dbContext = CreateDbContext();
        var orderRepository = new OrderRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var warningMaterial = new RawMaterial(Guid.NewGuid(), "Linha", "un", minimumStockLevel: 10, criticalStockLevel: 3);
        var criticalMaterial = new RawMaterial(Guid.NewGuid(), "Couro", "m2", minimumStockLevel: 10, criticalStockLevel: 2);
        var normalMaterial = new RawMaterial(Guid.NewGuid(), "Ziper", "un", minimumStockLevel: 2);
        var service = new StockAlertAppService(rawMaterialRepository, stockMovementRepository, orderRepository, productRepository);

        await rawMaterialRepository.AddAsync(warningMaterial);
        await rawMaterialRepository.AddAsync(criticalMaterial);
        await rawMaterialRepository.AddAsync(normalMaterial);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), warningMaterial.Id, StockMovementType.Inbound, 5, "Compra", null));
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), criticalMaterial.Id, StockMovementType.Inbound, 2, "Compra", null));
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), normalMaterial.Id, StockMovementType.Inbound, 5, "Compra", null));
        await dbContext.SaveChangesAsync();

        var alerts = await service.ListAlertsAsync();
        var criticalAlerts = await service.ListCriticalAlertsAsync();

        Assert.Collection(
            alerts,
            alert => Assert.Equal(StockAlertLevel.Critical, alert.Level),
            alert => Assert.Equal(StockAlertLevel.Warning, alert.Level));
        Assert.Single(criticalAlerts);
        Assert.Equal(criticalMaterial.Id, criticalAlerts.Single().RawMaterialId);
    }

    [Fact]
    public async Task Orders_controller_sending_to_production_includes_low_stock_alert_without_blocking_when_available()
    {
        await using var dbContext = CreateDbContext();
        var orderRepository = new OrderRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var shipmentRepository = new ShipmentRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m", minimumStockLevel: 5, criticalStockLevel: 2);
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 1)]);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "LOW-STOCK-001"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 1, new Money(10, "BRL"), product.Id)]);

        await rawMaterialRepository.AddAsync(material);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), material.Id, StockMovementType.Inbound, 5, "Compra", null));
        await productRepository.AddAsync(product);
        await orderRepository.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var planningService = new OrderProductionPlanningService(
            orderRepository,
            new ProductionPlanner(productRepository, new InventoryService(rawMaterialRepository, stockMovementRepository)),
            productionTaskRepository,
            unitOfWork,
            new RecordingDomainEventPublisher(),
            NullLogger<OrderProductionPlanningService>.Instance);
        var controller = new OrdersController(
            new OrderQueryService(orderRepository, productRepository, productionTaskRepository, shipmentRepository),
            CreateManualOrderService(dbContext),
            new EmptyOrderImportPipeline(),
            planningService,
            new StockAlertAppService(rawMaterialRepository, stockMovementRepository, orderRepository, productRepository),
            new NoOpAuditService())
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider())
        };

        await controller.SendToProduction(order.Id, CancellationToken.None);

        Assert.Equal("Pedido enviado para producao.", controller.TempData["Success"]);
        Assert.Contains("Aviso: Tecido", Assert.IsType<string>(controller.TempData["StockAlerts"]));
        Assert.Single(await productionTaskRepository.ListAsync());
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
            productionTaskRepository,
            stockMovementRepository,
            new ShipmentRepository(dbContext),
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
