using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Repositories;
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

    [Fact]
    public async Task Reversing_unreferenced_production_consumption_deletes_the_movement()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var repository = new StockMovementRepository(dbContext);
        var movementId = Guid.NewGuid();
        await repository.AddAsync(new StockMovement(
            movementId,
            rawMaterial.Id,
            StockMovementType.Outbound,
            2,
            "Production consumption",
            null));
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext).ReverseMovementAsync(movementId);

        Assert.True(result.WasDeleted);
        Assert.Null(result.ReversalMovementId);
        Assert.DoesNotContain(await repository.ListAsync(), movement => movement.Id == movementId);
        Assert.Equal(5, await repository.GetBalanceAsync(rawMaterial.Id));
    }

    [Fact]
    public async Task Reversing_referenced_outbound_creates_an_inbound_compensation_and_is_idempotent()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var repository = new StockMovementRepository(dbContext);
        var movementId = Guid.NewGuid();
        await repository.AddAsync(new StockMovement(
            movementId,
            rawMaterial.Id,
            StockMovementType.Outbound,
            2,
            "Manual consumption",
            "external-reference",
            unitCostAmount: 3));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var first = await service.ReverseMovementAsync(movementId);
        var second = await service.ReverseMovementAsync(movementId);

        Assert.False(first.WasDeleted);
        Assert.NotNull(first.ReversalMovementId);
        Assert.Null(second.ReversalMovementId);
        var movements = await repository.ListAsync();
        Assert.Single(movements, movement => movement.BusinessReference == $"stock-movement-reversal:{movementId}");
        Assert.Equal(5, await repository.GetBalanceAsync(rawMaterial.Id));
    }

    [Fact]
    public async Task Reversing_inbound_creates_an_outbound_compensation_without_deleting_the_original()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var repository = new StockMovementRepository(dbContext);
        var movementId = Guid.NewGuid();
        await repository.AddAsync(new StockMovement(
            movementId,
            rawMaterial.Id,
            StockMovementType.Inbound,
            2,
            "Purchase",
            "purchase-reference",
            unitCostAmount: 4));
        await dbContext.SaveChangesAsync();

        await CreateService(dbContext).ReverseMovementAsync(movementId);

        var movements = await repository.ListAsync();
        Assert.Contains(movements, movement => movement.Id == movementId);
        Assert.Contains(movements, movement =>
            movement.Type == StockMovementType.Outbound
            && movement.Reason == "Stock movement reversal"
            && movement.BusinessReference == $"stock-movement-reversal:{movementId}"
            && movement.Quantity == 2
            && movement.UnitCostAmount == 4);
        Assert.Equal(5, await repository.GetBalanceAsync(rawMaterial.Id));
    }

    [Fact]
    public async Task Reversing_adjustment_creates_an_adjustment_with_opposite_quantity()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var repository = new StockMovementRepository(dbContext);
        var movementId = Guid.NewGuid();
        await repository.AddAsync(new StockMovement(
            movementId,
            rawMaterial.Id,
            StockMovementType.Adjustment,
            -2,
            "Inventory correction",
            "adjustment-reference"));
        await dbContext.SaveChangesAsync();

        await CreateService(dbContext).ReverseMovementAsync(movementId);

        var movements = await repository.ListAsync();
        Assert.Contains(movements, movement =>
            movement.Type == StockMovementType.Adjustment
            && movement.Quantity == 2
            && movement.Reason == "Stock movement reversal"
            && movement.BusinessReference == $"stock-movement-reversal:{movementId}");
        Assert.Equal(5, await repository.GetBalanceAsync(rawMaterial.Id));
    }

    [Fact]
    public async Task Reversing_movement_linked_to_delivered_order_is_rejected()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "DELIVERED-1"),
            [new OrderItem(Guid.NewGuid(), "ITEM-DELIVERED", "Produto", 1, new Money(10, "BRL"))],
            OrderStatus.Delivered);
        var orderRepository = new OrderRepository(dbContext);
        await orderRepository.AddAsync(order);
        var movementRepository = new StockMovementRepository(dbContext);
        var movementId = Guid.NewGuid();
        await movementRepository.AddAsync(new StockMovement(
            movementId,
            rawMaterial.Id,
            StockMovementType.Outbound,
            1,
            "Manual consumption",
            "delivered-reference",
            orderId: order.Id));
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(dbContext).ReverseMovementAsync(movementId));

        Assert.Equal("Movimento nao pode ser revertido porque o pedido foi entregue.", exception.Message);
        Assert.Single(await movementRepository.ListAsync(), movement => movement.Id == movementId);
    }

    [Fact]
    public async Task Reversing_movement_for_order_with_completed_task_is_rejected()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext, stockQuantity: 5);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "COMPLETED-1"),
            [new OrderItem(Guid.NewGuid(), "ITEM-COMPLETED", "Produto", 1, new Money(10, "BRL"))],
            OrderStatus.InProduction);
        await new OrderRepository(dbContext).AddAsync(order);
        var movementRepository = new StockMovementRepository(dbContext);
        var movementId = Guid.NewGuid();
        await movementRepository.AddAsync(new StockMovement(
            movementId,
            rawMaterial.Id,
            StockMovementType.Outbound,
            1,
            "Manual consumption",
            "completed-reference",
            orderId: order.Id));
        await dbContext.SaveChangesAsync();

        var completedTask = new ProductionTask(
            Guid.NewGuid(),
            order.Id,
            order.Items.Single().Id,
            Guid.NewGuid(),
            1,
            status: ProductionTaskStatus.Completed,
            raisePlannedEvent: false);
        var service = CreateService(dbContext, new StubProductionTaskRepository([completedTask]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReverseMovementAsync(movementId));

        Assert.Equal("Movimento nao pode ser revertido porque o pedido foi concluido.", exception.Message);
        Assert.Single(await movementRepository.ListAsync(), movement => movement.Id == movementId);
    }

    [Fact]
    public async Task Reversing_inbound_after_its_quantity_was_consumed_is_rejected_without_compensation()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = await SeedRawMaterialAsync(dbContext);
        var movementRepository = new StockMovementRepository(dbContext);
        var movementId = Guid.NewGuid();
        await movementRepository.AddAsync(new StockMovement(
            movementId,
            rawMaterial.Id,
            StockMovementType.Inbound,
            5,
            "Purchase",
            "consumed-inbound"));
        await movementRepository.AddAsync(new StockMovement(
            Guid.NewGuid(),
            rawMaterial.Id,
            StockMovementType.Outbound,
            5,
            "Consumption",
            "consumption-reference"));
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(dbContext).ReverseMovementAsync(movementId));

        Assert.Equal("Reversao nao pode gerar saldo negativo.", exception.Message);
        Assert.DoesNotContain(await movementRepository.ListAsync(), movement =>
            movement.BusinessReference == $"stock-movement-reversal:{movementId}");
    }

    private static InventoryAppService CreateService(
        AppDbContext dbContext,
        IProductionTaskRepository? productionTaskRepository = null)
    {
        return new InventoryAppService(
            new RawMaterialRepository(dbContext),
            new StockMovementRepository(dbContext),
            new UnitOfWork(dbContext),
            new OrderRepository(dbContext),
            new FinancialSettlementRepository(dbContext),
            productionTaskRepository: productionTaskRepository);
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

    private sealed class StubProductionTaskRepository(IReadOnlyCollection<ProductionTask> tasks) : IProductionTaskRepository
    {
        public Task<ProductionTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(tasks.FirstOrDefault(task => task.Id == id));

        public Task<IReadOnlyCollection<ProductionTask>> ListAsync(
            ProductionTaskStatus? status = null,
            DateOnly? plannedDate = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(tasks);

        public Task<IReadOnlyCollection<ProductionTask>> ListRangeAsync(
            DateOnly plannedStartDate,
            DateOnly plannedEndDate,
            ProductionTaskStatus? status = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(tasks);

        public Task<IReadOnlyCollection<ProductionTask>> ListByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ProductionTask>>(tasks.Where(task => task.OrderId == orderId).ToList());

        public Task AddAsync(ProductionTask productionTask, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(ProductionTask productionTask, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
