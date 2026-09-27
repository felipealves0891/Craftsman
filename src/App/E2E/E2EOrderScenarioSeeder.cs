using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.App.E2E;

public sealed class E2EOrderScenarioSeeder
{
    private static readonly DateTimeOffset SeededAt = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);
    private readonly AppDbContext dbContext;
    private readonly IWebHostEnvironment environment;

    public E2EOrderScenarioSeeder(AppDbContext dbContext, IWebHostEnvironment environment)
    {
        this.dbContext = dbContext;
        this.environment = environment;
    }

    public async Task<E2EOrderScenarioResponse> SeedAsync(string scenario, CancellationToken cancellationToken = default)
    {
        EnsureE2EEnvironment();

        return scenario switch
        {
            "listing-details" => await SeedListingDetailsAsync(cancellationToken),
            "plannable-order" => await SeedPlannableOrderAsync(cancellationToken),
            "unplannable-order" => await SeedUnplannableOrderAsync(cancellationToken),
            "deletable-order" => await SeedDeletableOrderAsync(cancellationToken),
            "referenced-delete" => await SeedReferencedDeleteAsync(cancellationToken),
            "import-support" => await SeedImportSupportAsync(cancellationToken),
            _ => throw new InvalidOperationException($"Unknown E2E order scenario '{scenario}'.")
        };
    }

    private async Task<E2EOrderScenarioResponse> SeedListingDetailsAsync(CancellationToken cancellationToken)
    {
        await SeedCatalogAsync(includeStock: false, cancellationToken);
        var order = await AddOrderAsync("Manual", "E2E-LIST-001", linked: false, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Response("listing-details", order);
    }

    private async Task<E2EOrderScenarioResponse> SeedPlannableOrderAsync(CancellationToken cancellationToken)
    {
        await SeedCatalogAsync(includeStock: true, cancellationToken);
        var order = await AddOrderAsync("Manual", "E2E-PROD-001", linked: true, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Response("plannable-order", order);
    }

    private async Task<E2EOrderScenarioResponse> SeedUnplannableOrderAsync(CancellationToken cancellationToken)
    {
        await SeedCatalogAsync(includeStock: true, cancellationToken);
        var order = await AddOrderAsync("Manual", "E2E-UNPLAN-001", linked: false, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Response("unplannable-order", order);
    }

    private async Task<E2EOrderScenarioResponse> SeedDeletableOrderAsync(CancellationToken cancellationToken)
    {
        await SeedCatalogAsync(includeStock: false, cancellationToken);
        var order = await AddOrderAsync("Manual", "E2E-DELETE-001", linked: false, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Response("deletable-order", order);
    }

    private async Task<E2EOrderScenarioResponse> SeedReferencedDeleteAsync(CancellationToken cancellationToken)
    {
        await SeedCatalogAsync(includeStock: true, cancellationToken);
        var order = await AddOrderAsync("Manual", "E2E-REF-DELETE-001", linked: true, cancellationToken);
        var taskId = Guid.Parse("bbb00000-0000-0000-0000-000000000301");

        await dbContext.ProductionTasks.AddAsync(new ProductionTaskEntity
        {
            Id = taskId,
            OrderId = order.Id,
            OrderItemId = order.Items.Single().Id,
            ProductId = Catalog.ProductId,
            Quantity = 1,
            ProductionDurationHours = 2,
            Status = ProductionTaskStatus.Completed.ToString(),
            PlannedStartAt = SeededAt.AddDays(1),
            PlannedAt = SeededAt.AddDays(1).AddHours(2),
            StartedAt = SeededAt.AddDays(1),
            CompletedAt = SeededAt.AddDays(1).AddHours(2)
        }, cancellationToken);

        await dbContext.StockMovements.AddAsync(new StockMovementEntity
        {
            Id = Guid.Parse("bbb00000-0000-0000-0000-000000000302"),
            RawMaterialId = Catalog.RawMaterialId,
            Type = StockMovementType.Outbound.ToString(),
            Quantity = 1,
            UnitCostAmount = 3,
            Reason = "E2E production consumption",
            BusinessReference = taskId.ToString(),
            OccurredAt = SeededAt.AddMinutes(5)
        }, cancellationToken);

        await dbContext.Shipments.AddAsync(new ShipmentEntity
        {
            Id = Guid.Parse("bbb00000-0000-0000-0000-000000000303"),
            OrderId = order.Id,
            TrackingCode = "E2E-TRACK-001",
            Status = ShipmentStatus.Delivered.ToString(),
            CreatedAt = SeededAt,
            ShippedAt = SeededAt.AddDays(2),
            DeliveredAt = SeededAt.AddDays(3)
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Response("referenced-delete", order);
    }

    private async Task<E2EOrderScenarioResponse> SeedImportSupportAsync(CancellationToken cancellationToken)
    {
        await SeedCatalogAsync(includeStock: true, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new E2EOrderScenarioResponse(
            "import-support",
            null,
            null,
            new Dictionary<string, string>
            {
                ["expectedImportedExternalOrderId"] = "SIM-1001",
                ["expectedFailedExternalOrderId"] = "SIM-INVALID"
            });
    }

    private async Task SeedCatalogAsync(bool includeStock, CancellationToken cancellationToken)
    {
        if (!await dbContext.OrderSources.AnyAsync(source => source.Name == "Manual", cancellationToken))
        {
            await dbContext.OrderSources.AddAsync(new OrderSourceEntity
            {
                Id = Catalog.ManualSourceId,
                Name = "Manual",
                CreatedAt = SeededAt
            }, cancellationToken);
        }

        if (!await dbContext.OrderSources.AnyAsync(source => source.Name == "Simulated", cancellationToken))
        {
            await dbContext.OrderSources.AddAsync(new OrderSourceEntity
            {
                Id = Catalog.SimulatedSourceId,
                Name = "Simulated",
                CreatedAt = SeededAt
            }, cancellationToken);
        }

        if (!await dbContext.RawMaterials.AnyAsync(material => material.Id == Catalog.RawMaterialId, cancellationToken))
        {
            await dbContext.RawMaterials.AddAsync(new RawMaterialEntity
            {
                Id = Catalog.RawMaterialId,
                Name = "E2E Argila",
                UnitOfMeasure = "kg",
                Status = RawMaterialStatus.Active.ToString()
            }, cancellationToken);
        }

        if (!await dbContext.Products.AnyAsync(product => product.Id == Catalog.ProductId, cancellationToken))
        {
            await dbContext.Products.AddAsync(new ProductEntity
            {
                Id = Catalog.ProductId,
                Name = "E2E Caneca",
                Status = ProductStatus.Active.ToString(),
                ProductionDurationHours = 2,
                HourlyRate = 25
            }, cancellationToken);
        }

        if (!await dbContext.BillOfMaterialsItems.AnyAsync(item => item.ProductId == Catalog.ProductId && item.RawMaterialId == Catalog.RawMaterialId, cancellationToken))
        {
            await dbContext.BillOfMaterialsItems.AddAsync(new BillOfMaterialsItemEntity
            {
                Id = Catalog.BillOfMaterialsItemId,
                ProductId = Catalog.ProductId,
                RawMaterialId = Catalog.RawMaterialId,
                QuantityPerUnit = 1
            }, cancellationToken);
        }

        if (!await dbContext.ProductMappings.AnyAsync(mapping => mapping.Source == "Simulated" && mapping.ExternalItemId == "SIM-SKU-1", cancellationToken))
        {
            await dbContext.ProductMappings.AddAsync(new ProductMappingEntity
            {
                Id = Catalog.ProductMappingId,
                Source = "Simulated",
                ExternalItemId = "SIM-SKU-1",
                ProductId = Catalog.ProductId
            }, cancellationToken);
        }

        if (includeStock && !await dbContext.StockMovements.AnyAsync(movement => movement.BusinessReference == "e2e:initial-stock", cancellationToken))
        {
            await dbContext.StockMovements.AddAsync(new StockMovementEntity
            {
                Id = Catalog.StockMovementId,
                RawMaterialId = Catalog.RawMaterialId,
                Type = StockMovementType.Inbound.ToString(),
                Quantity = 50,
                UnitCostAmount = 3,
                Reason = "E2E initial stock",
                BusinessReference = "e2e:initial-stock",
                OccurredAt = SeededAt
            }, cancellationToken);
        }
    }

    private async Task<OrderEntity> AddOrderAsync(
        string source,
        string externalOrderId,
        bool linked,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        var order = new OrderEntity
        {
            Id = orderId,
            Source = source,
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
                    Description = "E2E item de pedido",
                    Quantity = 1,
                    UnitPriceAmount = 49.90m,
                    UnitPriceCurrency = "BRL",
                    ProductId = linked ? Catalog.ProductId : null
                }
            ]
        };

        await dbContext.Orders.AddAsync(order, cancellationToken);
        return order;
    }

    private void EnsureE2EEnvironment()
    {
        if (!environment.IsEnvironment("E2E"))
        {
            throw new InvalidOperationException("E2E order scenario seed can only run in the E2E environment.");
        }
    }

    private static E2EOrderScenarioResponse Response(string scenario, OrderEntity order)
    {
        return new E2EOrderScenarioResponse(
            scenario,
            order.Id,
            order.ExternalOrderId,
            new Dictionary<string, string>());
    }

    private static class Catalog
    {
        public static readonly Guid ManualSourceId = Guid.Parse("aaa00000-0000-0000-0000-000000000101");
        public static readonly Guid SimulatedSourceId = Guid.Parse("aaa00000-0000-0000-0000-000000000102");
        public static readonly Guid RawMaterialId = Guid.Parse("aaa00000-0000-0000-0000-000000000201");
        public static readonly Guid ProductId = Guid.Parse("aaa00000-0000-0000-0000-000000000301");
        public static readonly Guid BillOfMaterialsItemId = Guid.Parse("aaa00000-0000-0000-0000-000000000401");
        public static readonly Guid ProductMappingId = Guid.Parse("aaa00000-0000-0000-0000-000000000501");
        public static readonly Guid StockMovementId = Guid.Parse("aaa00000-0000-0000-0000-000000000601");
    }
}
