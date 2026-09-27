using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.App.E2E;

public sealed class E2EManualOrderScenarioSeeder
{
    private static readonly DateTimeOffset SeededAt = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);
    private const string SourceName = "E2E Manual";
    private const string ProductName = "E2E Produto Manual";
    private readonly AppDbContext dbContext;
    private readonly IWebHostEnvironment environment;

    public E2EManualOrderScenarioSeeder(AppDbContext dbContext, IWebHostEnvironment environment)
    {
        this.dbContext = dbContext;
        this.environment = environment;
    }

    public async Task<E2EManualOrderScenarioResponse> SeedAsync(
        string scenario,
        CancellationToken cancellationToken = default)
    {
        EnsureE2EEnvironment();

        return scenario switch
        {
            "catalog" => await SeedCatalogScenarioAsync(scenario, cancellationToken),
            "editable-order" => await SeedEditableOrderAsync(cancellationToken),
            "unlinked-order" => await SeedUnlinkedOrderAsync(cancellationToken),
            "validation-support" => await SeedCatalogScenarioAsync(scenario, cancellationToken),
            "readonly-support" => await SeedReadonlySupportAsync(cancellationToken),
            _ => throw new InvalidOperationException($"Unknown E2E manual order scenario '{scenario}'.")
        };
    }

    private async Task<E2EManualOrderScenarioResponse> SeedCatalogScenarioAsync(
        string scenario,
        CancellationToken cancellationToken)
    {
        await SeedCatalogAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Response(scenario, null, null);
    }

    private async Task<E2EManualOrderScenarioResponse> SeedEditableOrderAsync(CancellationToken cancellationToken)
    {
        await SeedCatalogAsync(cancellationToken);
        var order = BuildOrder("E2E-MANUAL-EDIT-001", linked: true);
        await dbContext.Orders.AddAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Response("editable-order", order, order.Items.Single());
    }

    private async Task<E2EManualOrderScenarioResponse> SeedUnlinkedOrderAsync(CancellationToken cancellationToken)
    {
        await SeedCatalogAsync(cancellationToken);
        var order = BuildOrder("E2E-MANUAL-LINK-001", linked: false);
        await dbContext.Orders.AddAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Response("unlinked-order", order, order.Items.Single());
    }

    private async Task<E2EManualOrderScenarioResponse> SeedReadonlySupportAsync(CancellationToken cancellationToken)
    {
        await SeedCatalogAsync(cancellationToken);
        var order = BuildOrder("E2E-MANUAL-READONLY-001", linked: false);
        await dbContext.Orders.AddAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Response("readonly-support", order, order.Items.Single());
    }

    private async Task SeedCatalogAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.OrderSources.AnyAsync(source => source.Name == SourceName, cancellationToken))
        {
            await dbContext.OrderSources.AddAsync(new OrderSourceEntity
            {
                Id = Catalog.SourceId,
                Name = SourceName,
                CreatedAt = SeededAt
            }, cancellationToken);
        }

        if (!await dbContext.RawMaterials.AnyAsync(material => material.Id == Catalog.RawMaterialId, cancellationToken))
        {
            await dbContext.RawMaterials.AddAsync(new RawMaterialEntity
            {
                Id = Catalog.RawMaterialId,
                Name = "E2E Materia Prima Manual",
                UnitOfMeasure = "un",
                Status = RawMaterialStatus.Active.ToString()
            }, cancellationToken);
        }

        if (!await dbContext.Products.AnyAsync(product => product.Id == Catalog.ProductId, cancellationToken))
        {
            await dbContext.Products.AddAsync(new ProductEntity
            {
                Id = Catalog.ProductId,
                Name = ProductName,
                Status = ProductStatus.Active.ToString(),
                ProductionDurationHours = 1,
                HourlyRate = 20
            }, cancellationToken);
        }

        if (!await dbContext.BillOfMaterialsItems.AnyAsync(item => item.Id == Catalog.BillOfMaterialsItemId, cancellationToken))
        {
            await dbContext.BillOfMaterialsItems.AddAsync(new BillOfMaterialsItemEntity
            {
                Id = Catalog.BillOfMaterialsItemId,
                ProductId = Catalog.ProductId,
                RawMaterialId = Catalog.RawMaterialId,
                QuantityPerUnit = 1
            }, cancellationToken);
        }
    }

    private static OrderEntity BuildOrder(string externalOrderId, bool linked)
    {
        var orderId = Guid.NewGuid();
        return new OrderEntity
        {
            Id = orderId,
            Source = SourceName,
            ExternalOrderId = externalOrderId,
            Status = OrderStatus.Normalized.ToString(),
            CreatedAt = SeededAt,
            ShippingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10),
            Items =
            [
                new OrderItemEntity
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ExternalItemId = $"{externalOrderId}-ITEM-1",
                    Description = "E2E item manual",
                    Quantity = 1,
                    UnitPriceAmount = 25.50m,
                    UnitPriceCurrency = "BRL",
                    ProductId = linked ? Catalog.ProductId : null
                }
            ]
        };
    }

    private void EnsureE2EEnvironment()
    {
        if (!environment.IsEnvironment("E2E"))
        {
            throw new InvalidOperationException("E2E manual order scenario seed can only run in the E2E environment.");
        }
    }

    private static E2EManualOrderScenarioResponse Response(
        string scenario,
        OrderEntity? order,
        OrderItemEntity? item)
    {
        return new E2EManualOrderScenarioResponse(
            scenario,
            order?.Id,
            item?.Id,
            Catalog.ProductId,
            ProductName,
            SourceName,
            order?.ExternalOrderId,
            new Dictionary<string, string>());
    }

    private static class Catalog
    {
        public static readonly Guid SourceId = Guid.Parse("ccc00000-0000-0000-0000-000000000101");
        public static readonly Guid RawMaterialId = Guid.Parse("ccc00000-0000-0000-0000-000000000201");
        public static readonly Guid ProductId = Guid.Parse("ccc00000-0000-0000-0000-000000000301");
        public static readonly Guid BillOfMaterialsItemId = Guid.Parse("ccc00000-0000-0000-0000-000000000401");
    }
}
