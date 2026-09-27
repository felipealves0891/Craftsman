using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.App.E2E;

public sealed class E2EProductCatalogScenarioSeeder
{
    private const string ProductName = "E2E Produto Catalogo";
    private const string SecondaryProductName = "E2E Produto Secundario";
    private static readonly Guid ProductId = Guid.Parse("ddd00000-0000-0000-0000-000000000301");
    private static readonly Guid SecondaryProductId = Guid.Parse("ddd00000-0000-0000-0000-000000000302");
    private static readonly Guid FirstRawMaterialId = Guid.Parse("ddd00000-0000-0000-0000-000000000401");
    private static readonly Guid SecondRawMaterialId = Guid.Parse("ddd00000-0000-0000-0000-000000000402");
    private static readonly Guid BillOfMaterialsItemId = Guid.Parse("ddd00000-0000-0000-0000-000000000501");

    private readonly AppDbContext dbContext;
    private readonly IWebHostEnvironment environment;

    public E2EProductCatalogScenarioSeeder(AppDbContext dbContext, IWebHostEnvironment environment)
    {
        this.dbContext = dbContext;
        this.environment = environment;
    }

    public async Task<E2EProductCatalogScenarioResponse> SeedAsync(
        string scenario,
        CancellationToken cancellationToken = default)
    {
        EnsureE2EEnvironment();

        return scenario switch
        {
            "listing" => await SeedListingAsync(cancellationToken),
            "editable-product" => await SeedProductOnlyAsync(scenario, cancellationToken),
            "bom-support" => await SeedProductAndRawMaterialsAsync(scenario, cancellationToken),
            "mapping-support" => await SeedProductOnlyAsync(scenario, cancellationToken),
            "validation-support" => await SeedProductOnlyAsync(scenario, cancellationToken),
            "readonly-support" => await SeedProductAndRawMaterialsAsync(scenario, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown E2E product catalog scenario '{scenario}'.")
        };
    }

    private async Task<E2EProductCatalogScenarioResponse> SeedListingAsync(CancellationToken cancellationToken)
    {
        await SeedProductAndRawMaterialsAsync("listing", cancellationToken);

        if (!await dbContext.BillOfMaterialsItems.AnyAsync(item => item.Id == BillOfMaterialsItemId, cancellationToken))
        {
            await dbContext.BillOfMaterialsItems.AddAsync(new BillOfMaterialsItemEntity
            {
                Id = BillOfMaterialsItemId,
                ProductId = ProductId,
                RawMaterialId = FirstRawMaterialId,
                QuantityPerUnit = 2
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Response("listing");
    }

    private async Task<E2EProductCatalogScenarioResponse> SeedProductOnlyAsync(
        string scenario,
        CancellationToken cancellationToken)
    {
        await SeedProductsAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Response(scenario);
    }

    private async Task<E2EProductCatalogScenarioResponse> SeedProductAndRawMaterialsAsync(
        string scenario,
        CancellationToken cancellationToken)
    {
        await SeedProductsAsync(cancellationToken);
        await SeedRawMaterialsAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Response(scenario);
    }

    private async Task SeedProductsAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.Products.AnyAsync(product => product.Id == ProductId, cancellationToken))
        {
            await dbContext.Products.AddAsync(new ProductEntity
            {
                Id = ProductId,
                Name = ProductName,
                Status = ProductStatus.Active.ToString(),
                ProductionDurationHours = 2,
                HourlyRate = 25
            }, cancellationToken);
        }

        if (!await dbContext.Products.AnyAsync(product => product.Id == SecondaryProductId, cancellationToken))
        {
            await dbContext.Products.AddAsync(new ProductEntity
            {
                Id = SecondaryProductId,
                Name = SecondaryProductName,
                Status = ProductStatus.Inactive.ToString(),
                ProductionDurationHours = 3,
                HourlyRate = 30
            }, cancellationToken);
        }
    }

    private async Task SeedRawMaterialsAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.RawMaterials.AnyAsync(material => material.Id == FirstRawMaterialId, cancellationToken))
        {
            await dbContext.RawMaterials.AddAsync(new RawMaterialEntity
            {
                Id = FirstRawMaterialId,
                Name = "E2E Argila Catalogo",
                UnitOfMeasure = "kg",
                Status = RawMaterialStatus.Active.ToString()
            }, cancellationToken);
        }

        if (!await dbContext.RawMaterials.AnyAsync(material => material.Id == SecondRawMaterialId, cancellationToken))
        {
            await dbContext.RawMaterials.AddAsync(new RawMaterialEntity
            {
                Id = SecondRawMaterialId,
                Name = "E2E Tinta Catalogo",
                UnitOfMeasure = "un",
                Status = RawMaterialStatus.Active.ToString()
            }, cancellationToken);
        }
    }

    private void EnsureE2EEnvironment()
    {
        if (!environment.IsEnvironment("E2E"))
        {
            throw new InvalidOperationException("E2E product catalog scenario seed can only run in the E2E environment.");
        }
    }

    private static E2EProductCatalogScenarioResponse Response(string scenario)
    {
        return new E2EProductCatalogScenarioResponse(
            scenario,
            ProductId,
            ProductName,
            [
                new E2EProductCatalogRawMaterialResponse(FirstRawMaterialId, "E2E Argila Catalogo", "kg"),
                new E2EProductCatalogRawMaterialResponse(SecondRawMaterialId, "E2E Tinta Catalogo", "un")
            ],
            new Dictionary<string, string>
            {
                ["secondaryProductId"] = SecondaryProductId.ToString(),
                ["secondaryProductName"] = SecondaryProductName
            });
    }
}
