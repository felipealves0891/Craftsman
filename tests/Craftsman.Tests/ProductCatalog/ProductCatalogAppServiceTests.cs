using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Craftsman.Tests.ProductCatalog;

public sealed class ProductCatalogAppServiceTests
{
    [Fact]
    public async Task Product_inputs_and_list_cover_missing_and_existing_products()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var product = new Product(Guid.NewGuid(), "Caneca");
        await new ProductRepository(dbContext).AddAsync(product);
        await dbContext.SaveChangesAsync();

        Assert.Null(await service.GetProductInputAsync(Guid.NewGuid()));
        var input = await service.GetProductInputAsync(product.Id);
        Assert.NotNull(input);
        Assert.Equal(product.Id, input!.Id);
        Assert.Equal("Caneca", input.Name);
        Assert.Single(await service.ListProductsAsync());
    }

    [Fact]
    public async Task Save_product_creates_and_updates_with_active_or_inactive_status()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var createdId = await service.SaveProductAsync(new ProductInputModel
        {
            Name = "Caneca",
            Status = ProductStatus.Active,
            ProductionDurationHours = 2,
            HourlyRate = 15
        });
        var updatedId = await service.SaveProductAsync(new ProductInputModel
        {
            Id = createdId,
            Name = "Caneca personalizada",
            Status = ProductStatus.Inactive,
            ProductionDurationHours = 3,
            HourlyRate = 20
        });
        var updated = await service.GetProductInputAsync(updatedId);

        Assert.Equal(createdId, updatedId);
        Assert.Equal("Caneca personalizada", updated?.Name);
        Assert.Equal(ProductStatus.Inactive, updated?.Status);
        Assert.Equal(3, updated?.ProductionDurationHours);
        Assert.Equal(20, updated?.HourlyRate);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveProductAsync(new ProductInputModel
        {
            Id = Guid.NewGuid(), Name = "Inexistente", Status = ProductStatus.Active
        }));
    }

    [Fact]
    public async Task Bill_of_material_input_pads_to_five_rows_and_save_replaces_items()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var materialId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(materialId, 2)]);
        await new ProductRepository(dbContext).AddAsync(product);
        await dbContext.SaveChangesAsync();

        Assert.Null(await service.GetBillOfMaterialsInputAsync(Guid.NewGuid()));
        var input = await service.GetBillOfMaterialsInputAsync(product.Id);
        Assert.NotNull(input);
        Assert.Equal(5, input!.Items.Count);
        Assert.Equal(materialId, input.Items[0].RawMaterialId);
        Assert.Equal(2, input.Items[0].QuantityPerUnit);

        await service.SaveBillOfMaterialsAsync(new BillOfMaterialsInputModel
        {
            ProductId = product.Id,
            Items = [new BillOfMaterialsItemInputModel { RawMaterialId = materialId, QuantityPerUnit = 4 }]
        });

        var updated = await new ProductRepository(dbContext).GetByIdAsync(product.Id);
        Assert.Equal(4, Assert.Single(updated!.BillOfMaterials).QuantityPerUnit);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveBillOfMaterialsAsync(
            new BillOfMaterialsInputModel { ProductId = product.Id, Items = [] }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveBillOfMaterialsAsync(new BillOfMaterialsInputModel
        {
            ProductId = product.Id,
            Items = [new BillOfMaterialsItemInputModel { QuantityPerUnit = 1 }]
        }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveBillOfMaterialsAsync(new BillOfMaterialsInputModel
        {
            ProductId = Guid.NewGuid(), Items = []
        }));
    }

    [Fact]
    public async Task Mapping_creation_rejects_missing_products_and_duplicate_external_items()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var product = new Product(Guid.NewGuid(), "Bolsa");
        await new ProductRepository(dbContext).AddAsync(product);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateMappingAsync(new ProductMappingInputModel
        {
            Source = "Shopee", ExternalItemId = "SKU-1", ProductId = Guid.NewGuid()
        }));

        var input = new ProductMappingInputModel { Source = "Shopee", ExternalItemId = "SKU-1", ProductId = product.Id };
        await service.CreateMappingAsync(input);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateMappingAsync(input));

        var mapping = Assert.Single(await service.ListMappingsAsync());
        Assert.Equal("Bolsa", mapping.ProductName);
        Assert.Equal("Shopee", mapping.Source);
        Assert.Single(await service.ListProductOptionsAsync());
    }

    private static ProductCatalogAppService CreateService(AppDbContext dbContext)
    {
        var products = new ProductRepository(dbContext);
        var mappings = new ProductMappingRepository(dbContext);
        var materials = new RawMaterialRepository(dbContext);
        return new ProductCatalogAppService(
            products,
            mappings,
            materials,
            new ProductMappingService(mappings),
            new UnitOfWork(dbContext),
            NullLogger<ProductCatalogAppService>.Instance);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
