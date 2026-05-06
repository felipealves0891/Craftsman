using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Inventory.Entities;
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

    private static InventoryAppService CreateService(AppDbContext dbContext)
    {
        return new InventoryAppService(
            new RawMaterialRepository(dbContext),
            new StockMovementRepository(dbContext),
            new UnitOfWork(dbContext));
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
