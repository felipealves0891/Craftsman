using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Services;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Integration;

public sealed class PersistenceRepositoryTests
{
    [Fact]
    public async Task Order_repository_saves_and_loads_normalized_order_with_items_and_origin()
    {
        await using var dbContext = CreateDbContext();
        var repository = new OrderRepository(dbContext);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Shopee", "SO-200"),
            new CustomerInfo("Cliente", null),
            [new OrderItem(Guid.NewGuid(), "SKU-1", "Item", 1, new Money(99, "BRL"))]);

        await repository.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var loaded = await repository.GetByOriginAsync(new OrderOrigin("Shopee", "SO-200"));

        Assert.NotNull(loaded);
        Assert.Equal(order.Id, loaded.Id);
        Assert.Single(loaded.Items);
        Assert.Empty(loaded.DomainEvents);
    }

    [Fact]
    public async Task Product_mapping_service_rejects_conflicting_external_mapping()
    {
        await using var dbContext = CreateDbContext();
        var product = new Product(Guid.NewGuid(), "Produto", billOfMaterials: [new BillOfMaterialsItem(Guid.NewGuid(), 2)]);
        var productRepository = new ProductRepository(dbContext);
        var mappingRepository = new ProductMappingRepository(dbContext);
        var mappingService = new ProductMappingService(mappingRepository);

        await productRepository.AddAsync(product);
        await dbContext.SaveChangesAsync();

        await mappingService.CreateMappingAsync("Elo7", "EXT-1", product.Id);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => mappingService.CreateMappingAsync("Elo7", "EXT-1", product.Id));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
