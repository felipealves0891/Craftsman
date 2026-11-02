using Craftsman.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Integration;

public sealed class DevelopmentDataSeederTests
{
    [Fact]
    public async Task Development_seed_populates_reference_data_and_is_idempotent()
    {
        await using var dbContext = CreateDbContext();

        await DevelopmentDataSeeder.SeedAsync(dbContext);

        Assert.Equal(3, await dbContext.OrderSources.CountAsync());
        Assert.Equal(6, await dbContext.RawMaterials.CountAsync());
        Assert.Equal(3, await dbContext.Products.CountAsync());
        Assert.Equal(11, await dbContext.BillOfMaterialsItems.CountAsync());
        Assert.Equal(4, await dbContext.ProductMappings.CountAsync());
        Assert.Equal(6, await dbContext.StockMovements.CountAsync());

        await DevelopmentDataSeeder.SeedAsync(dbContext);

        Assert.Equal(3, await dbContext.OrderSources.CountAsync());
        Assert.Equal(6, await dbContext.RawMaterials.CountAsync());
        Assert.Equal(3, await dbContext.Products.CountAsync());
        Assert.Equal(11, await dbContext.BillOfMaterialsItems.CountAsync());
        Assert.Equal(4, await dbContext.ProductMappings.CountAsync());
        Assert.Equal(6, await dbContext.StockMovements.CountAsync());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
