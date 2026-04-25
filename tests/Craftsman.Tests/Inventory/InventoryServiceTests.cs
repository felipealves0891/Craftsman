using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Inventory;

public sealed class InventoryServiceTests
{
    [Fact]
    public async Task Availability_reports_missing_material_when_stock_is_insufficient()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Tecido", "metro");
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var inventoryService = new InventoryService(rawMaterialRepository, stockMovementRepository);

        await rawMaterialRepository.AddAsync(rawMaterial);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), rawMaterial.Id, StockMovementType.Inbound, 2, "Initial stock", null));
        await dbContext.SaveChangesAsync();

        var result = await inventoryService.CheckAvailabilityAsync([new MaterialRequirement(rawMaterial.Id, 3)]);

        Assert.False(result.IsAvailable);
        Assert.Equal(1, result.MissingMaterials.Single().MissingQuantity);
    }

    [Fact]
    public async Task Consumption_creates_auditable_outbound_movement()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Tecido", "metro");
        var productionTaskId = Guid.NewGuid();
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var inventoryService = new InventoryService(rawMaterialRepository, stockMovementRepository);

        await rawMaterialRepository.AddAsync(rawMaterial);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), rawMaterial.Id, StockMovementType.Inbound, 5, "Initial stock", null));
        await dbContext.SaveChangesAsync();

        await inventoryService.ConsumeForProductionAsync([new MaterialRequirement(rawMaterial.Id, 2)], productionTaskId);
        await dbContext.SaveChangesAsync();

        var movements = await stockMovementRepository.GetByRawMaterialAsync(rawMaterial.Id);

        Assert.Contains(movements, movement =>
            movement.Type == StockMovementType.Outbound &&
            movement.BusinessReference == productionTaskId.ToString());
        Assert.Equal(3, await stockMovementRepository.GetBalanceAsync(rawMaterial.Id));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
