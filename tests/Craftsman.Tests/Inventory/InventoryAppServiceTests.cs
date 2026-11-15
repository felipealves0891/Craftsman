using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Inventory;

public sealed class InventoryAppServiceTests
{
    [Fact]
    public async Task Manual_outbound_without_reason_returns_clear_validation_error()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterMovementAsync(new StockMovementInputModel
            {
                RawMaterialId = rawMaterial.Id,
                Type = StockMovementType.Outbound,
                Quantity = 1,
                Reason = " "
            }));

        Assert.Equal("Informe o motivo da saida manual.", exception.Message);
    }

    [Fact]
    public async Task Manual_outbound_preserves_unit_cost_and_reports_positive_total_cost()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var service = CreateService(dbContext);

        await service.RegisterMovementAsync(new StockMovementInputModel
        {
            RawMaterialId = rawMaterial.Id,
            Type = StockMovementType.Outbound,
            Quantity = 3,
            Reason = "Consumo manual",
            UnitCostAmount = 2.5m
        });

        var repository = new StockMovementRepository(dbContext);
        var movement = Assert.Single(
            await repository.GetByRawMaterialAsync(rawMaterial.Id),
            item => item.Type == StockMovementType.Outbound);
        var listedMovement = Assert.Single(
            await service.ListMovementsAsync(rawMaterial.Id),
            item => item.Type == StockMovementType.Outbound.ToString());

        Assert.Equal(3, movement.Quantity);
        Assert.Equal(-3, movement.SignedQuantity);
        Assert.Equal(2.5m, movement.UnitCostAmount);
        Assert.Equal(7.5m, movement.TotalCostAmount);
        Assert.Equal(-3, listedMovement.Quantity);
        Assert.Equal(2.5m, listedMovement.UnitCostAmount);
        Assert.Equal(7.5m, listedMovement.TotalCostAmount);
        Assert.Equal(2, await repository.GetBalanceAsync(rawMaterial.Id));
    }

    [Fact]
    public async Task Manual_outbound_can_be_linked_to_an_in_production_order()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "PED-ESTOQUE-1"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Produto", 1, new Money(10, "BRL"))],
            OrderStatus.InProduction);
        await new OrderRepository(dbContext).AddAsync(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var options = await service.ListOutboundOrderOptionsAsync();
        Assert.Contains(options, option => option.Id == order.Id && option.DisplayName == "Manual - PED-ESTOQUE-1");

        await service.RegisterMovementAsync(new StockMovementInputModel
        {
            RawMaterialId = rawMaterial.Id,
            Type = StockMovementType.Outbound,
            Quantity = 2,
            Reason = "Consumo do pedido",
            UnitCostAmount = 4,
            OrderId = order.Id
        });

        var movement = Assert.Single(await new StockMovementRepository(dbContext).GetByRawMaterialAsync(rawMaterial.Id), item => item.Type == StockMovementType.Outbound);
        Assert.Equal(order.Id, movement.OrderId);
        Assert.Null(movement.BusinessReference);

        var listedMovement = Assert.Single(await service.ListMovementsAsync(rawMaterial.Id), item => item.Type == StockMovementType.Outbound.ToString());
        Assert.Equal(order.Id, listedMovement.OrderId);
        Assert.Equal("Manual - PED-ESTOQUE-1", listedMovement.OrderDisplayName);
    }

    [Fact]
    public async Task Manual_outbound_rejects_order_that_is_not_in_production()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "PED-ESTOQUE-2"),
            [new OrderItem(Guid.NewGuid(), "ITEM-2", "Produto", 1, new Money(10, "BRL"))]);
        await new OrderRepository(dbContext).AddAsync(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterMovementAsync(new StockMovementInputModel
        {
            RawMaterialId = rawMaterial.Id,
            Type = StockMovementType.Outbound,
            Quantity = 1,
            Reason = "Consumo do pedido",
            OrderId = order.Id
        }));

        Assert.Equal("Pedido deve estar em producao para ser vinculado.", exception.Message);
        Assert.DoesNotContain(
            await new StockMovementRepository(dbContext).ListAsync(),
            movement => movement.Type == StockMovementType.Outbound && movement.OrderId == order.Id);
    }

    [Fact]
    public async Task Outbound_order_options_exclude_orders_with_existing_financial_settlement()
    {
        await using var dbContext = CreateDbContext();
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "PED-ESTOQUE-3"),
            [new OrderItem(Guid.NewGuid(), "ITEM-3", "Produto", 1, new Money(10, "BRL"))],
            OrderStatus.InProduction);
        await new OrderRepository(dbContext).AddAsync(order);
        await new FinancialSettlementRepository(dbContext).AddAsync(new FinancialSettlement(
            Guid.NewGuid(),
            order.Id,
            revenueAmount: 10,
            productionCostAmount: 2,
            shippingCostAmount: 0,
            raiseCalculatedEvent: false));
        await dbContext.SaveChangesAsync();

        var options = await CreateService(dbContext).ListOutboundOrderOptionsAsync();

        Assert.DoesNotContain(options, option => option.Id == order.Id);
    }

    [Fact]
    public async Task Manual_outbound_rejects_missing_order_without_persisting_movement()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterMovementAsync(new StockMovementInputModel
        {
            RawMaterialId = rawMaterial.Id,
            Type = StockMovementType.Outbound,
            Quantity = 1,
            Reason = "Consumo do pedido",
            OrderId = Guid.NewGuid()
        }));

        Assert.Equal("Pedido nao encontrado.", exception.Message);
        Assert.DoesNotContain(await new StockMovementRepository(dbContext).ListAsync(), movement => movement.Type == StockMovementType.Outbound);
    }

    [Fact]
    public async Task Manual_outbound_rejects_order_with_existing_financial_settlement()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "PED-ESTOQUE-4"),
            [new OrderItem(Guid.NewGuid(), "ITEM-4", "Produto", 1, new Money(10, "BRL"))],
            OrderStatus.InProduction);
        await new OrderRepository(dbContext).AddAsync(order);
        await new FinancialSettlementRepository(dbContext).AddAsync(new FinancialSettlement(
            Guid.NewGuid(),
            order.Id,
            revenueAmount: 10,
            productionCostAmount: 2,
            shippingCostAmount: 0,
            raiseCalculatedEvent: false));
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(dbContext).RegisterMovementAsync(new StockMovementInputModel
        {
            RawMaterialId = rawMaterial.Id,
            Type = StockMovementType.Outbound,
            Quantity = 1,
            Reason = "Consumo do pedido",
            OrderId = order.Id
        }));

        Assert.Equal("Pedido ja possui apuracao financeira.", exception.Message);
        Assert.DoesNotContain(await new StockMovementRepository(dbContext).ListAsync(), movement => movement.Type == StockMovementType.Outbound);
    }

    [Fact]
    public async Task Manual_inbound_without_reason_is_registered_without_business_reference()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext);
        var service = CreateService(dbContext);

        await service.RegisterMovementAsync(new StockMovementInputModel
        {
            RawMaterialId = rawMaterial.Id,
            Type = StockMovementType.Inbound,
            Quantity = 3,
            Reason = string.Empty,
            BusinessReference = "informada-pelo-post",
            UnitCostAmount = 2
        });

        var movements = await new StockMovementRepository(dbContext).GetByRawMaterialAsync(rawMaterial.Id);
        var movement = Assert.Single(movements);

        Assert.Equal(StockMovementType.Inbound, movement.Type);
        Assert.Equal("Entrada manual", movement.Reason);
        Assert.Null(movement.BusinessReference);
        Assert.Equal(3, await new StockMovementRepository(dbContext).GetBalanceAsync(rawMaterial.Id));
    }

    [Fact]
    public async Task Manual_adjustment_without_reason_is_registered_when_balance_remains_valid()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var service = CreateService(dbContext);

        await service.RegisterMovementAsync(new StockMovementInputModel
        {
            RawMaterialId = rawMaterial.Id,
            Type = StockMovementType.Adjustment,
            Quantity = -2,
            Reason = string.Empty
        });

        var repository = new StockMovementRepository(dbContext);
        var movements = await repository.GetByRawMaterialAsync(rawMaterial.Id);

        Assert.Contains(movements, movement =>
            movement.Type == StockMovementType.Adjustment &&
            movement.Quantity == -2 &&
            movement.Reason == "Ajuste manual" &&
            movement.BusinessReference is null);
        Assert.Equal(3, await repository.GetBalanceAsync(rawMaterial.Id));
    }

    [Fact]
    public async Task Manual_adjustment_cannot_generate_negative_balance()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 1);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterMovementAsync(new StockMovementInputModel
            {
                RawMaterialId = rawMaterial.Id,
                Type = StockMovementType.Adjustment,
                Quantity = -2
            }));

        Assert.Equal("Ajuste manual nao pode gerar saldo negativo.", exception.Message);
    }

    [Fact]
    public async Task Historical_outbound_with_zero_cost_is_not_rewritten_when_listed()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var repository = new StockMovementRepository(dbContext);
        await repository.AddAsync(new StockMovement(
            Guid.NewGuid(),
            rawMaterial.Id,
            StockMovementType.Outbound,
            2,
            "Saída histórica",
            null,
            unitCostAmount: 0));
        await dbContext.SaveChangesAsync();

        var listedMovement = Assert.Single(
            await CreateService(dbContext).ListMovementsAsync(rawMaterial.Id),
            item => item.Type == StockMovementType.Outbound.ToString());

        Assert.Equal(0, listedMovement.UnitCostAmount);
        Assert.Equal(0, listedMovement.TotalCostAmount);
    }

    [Fact]
    public async Task Inactivating_material_preserves_limits_balance_and_movement_history()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterials = new RawMaterialRepository(dbContext);
        var stockMovements = new StockMovementRepository(dbContext);
        var material = new RawMaterial(
            Guid.NewGuid(),
            "Linha",
            "un",
            minimumStockLevel: 10,
            criticalStockLevel: 2);
        await rawMaterials.AddAsync(material);
        await stockMovements.AddAsync(new StockMovement(
            Guid.NewGuid(),
            material.Id,
            StockMovementType.Inbound,
            4,
            "Compra inicial",
            null));
        await dbContext.SaveChangesAsync();
        var service = new InventoryAppService(rawMaterials, stockMovements, new UnitOfWork(dbContext), new OrderRepository(dbContext), new FinancialSettlementRepository(dbContext));

        await service.SaveRawMaterialAsync(new RawMaterialInputModel
        {
            Id = material.Id,
            Name = material.Name,
            UnitOfMeasure = material.UnitOfMeasure,
            Status = RawMaterialStatus.Inactive,
            MinimumStockLevel = material.MinimumStockLevel,
            CriticalStockLevel = material.CriticalStockLevel
        });

        var input = await service.GetRawMaterialInputAsync(material.Id);
        var listed = Assert.Single(await service.ListRawMaterialsAsync());
        var balance = Assert.Single(await service.ListBalancesAsync());
        var movement = Assert.Single(await service.ListMovementsAsync(material.Id));
        var alerts = await new StockAlertAppService(
            rawMaterials,
            stockMovements,
            new OrderRepository(dbContext),
            new ProductRepository(dbContext)).ListAlertsAsync();

        Assert.Equal(RawMaterialStatus.Inactive, input?.Status);
        Assert.Equal(10, input?.MinimumStockLevel);
        Assert.Equal(2, input?.CriticalStockLevel);
        Assert.Equal(RawMaterialStatus.Inactive.ToString(), listed.Status);
        Assert.Equal(10, listed.MinimumStockLevel);
        Assert.Equal(2, listed.CriticalStockLevel);
        Assert.Equal(4, balance.Balance);
        Assert.Equal(StockAlertLevel.Warning, balance.AlertLevel);
        Assert.Equal(4, movement.Quantity);
        Assert.Equal("Compra inicial", movement.Reason);
        Assert.DoesNotContain(alerts, alert => alert.RawMaterialId == material.Id);
    }

    private static InventoryAppService CreateService(AppDbContext dbContext)
    {
        return new InventoryAppService(
            new RawMaterialRepository(dbContext),
            new StockMovementRepository(dbContext),
            new UnitOfWork(dbContext),
            new OrderRepository(dbContext),
            new FinancialSettlementRepository(dbContext));
    }

    private static async Task<RawMaterial> SeedRawMaterialAsync(AppDbContext dbContext, decimal stockQuantity = 0)
    {
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Tecido", "metro");
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);

        await rawMaterialRepository.AddAsync(rawMaterial);

        if (stockQuantity > 0)
        {
            await stockMovementRepository.AddAsync(new StockMovement(
                Guid.NewGuid(),
                rawMaterial.Id,
                StockMovementType.Inbound,
                stockQuantity,
                "Initial stock",
                null));
        }

        await dbContext.SaveChangesAsync();

        return rawMaterial;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
