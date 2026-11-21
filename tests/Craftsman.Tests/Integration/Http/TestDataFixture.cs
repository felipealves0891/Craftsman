using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Integration.Http;

internal static class TestDataFixture
{
    public static readonly Guid OrderId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OrderItemId = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ProductId = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid RawMaterialId = new("66666666-6666-6666-6666-666666666666");
    public static readonly Guid TaskId = new("44444444-4444-4444-4444-444444444444");
    public static readonly Guid CompletedTaskId = new("55555555-5555-5555-5555-555555555555");

    public static async Task SeedOrderAsync(AppDbContext db, OrderStatus status = OrderStatus.Normalized)
    {
        db.Orders.Add(new OrderEntity
        {
            Id = OrderId,
            Source = "test-source",
            ExternalOrderId = "TEST-ORDER-001",
            Status = status.ToString(),
            CreatedAt = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero),
            ShippingDate = new DateOnly(2026, 11, 28),
            Items =
            [
                new OrderItemEntity
                {
                    Id = OrderItemId,
                    ExternalItemId = "TEST-ITEM-001",
                    Description = "Produto de teste",
                    Quantity = 2,
                    UnitPriceAmount = 12.50m,
                    UnitPriceCurrency = "BRL",
                    ProductId = ProductId
                }
            ]
        });

        await db.SaveChangesAsync();
    }

    public static async Task SeedProductionTaskAsync(
        AppDbContext db,
        ProductionTaskStatus status = ProductionTaskStatus.Planned,
        DateTimeOffset? plannedStartAt = null,
        Guid? taskId = null)
    {
        if (!await db.Orders.AnyAsync(order => order.Id == OrderId))
        {
            await SeedOrderAsync(db, OrderStatus.ReadyForProduction);
        }

        db.ProductionTasks.Add(new ProductionTaskEntity
        {
            Id = taskId ?? TaskId,
            OrderId = OrderId,
            OrderItemId = OrderItemId,
            ProductId = ProductId,
            Quantity = 2,
            ProductionDurationHours = 2,
            Status = status.ToString(),
            PlannedStartAt = plannedStartAt ?? new DateTimeOffset(2026, 11, 21, 8, 0, 0, TimeSpan.Zero),
            PlannedAt = new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero)
        });

        await db.SaveChangesAsync();
    }

    public static async Task SeedProductionPlanningScenarioAsync(
        AppDbContext db,
        ProductStatus productStatus = ProductStatus.Active,
        bool includeBillOfMaterials = true,
        RawMaterialStatus rawMaterialStatus = RawMaterialStatus.Active,
        decimal stockQuantity = 10m)
    {
        await SeedOrderAsync(db, OrderStatus.Normalized);

        var product = new ProductEntity
        {
            Id = ProductId,
            Name = "Produto planejavel",
            Status = productStatus.ToString(),
            ProductionDurationHours = 2,
            HourlyRate = 10m
        };

        var rawMaterial = new RawMaterialEntity
        {
            Id = RawMaterialId,
            Name = "Material de teste",
            UnitOfMeasure = "un",
            Status = rawMaterialStatus.ToString()
        };

        db.Products.Add(product);
        db.RawMaterials.Add(rawMaterial);

        if (includeBillOfMaterials)
        {
            db.BillOfMaterialsItems.Add(new BillOfMaterialsItemEntity
            {
                Id = Guid.NewGuid(),
                ProductId = ProductId,
                RawMaterialId = RawMaterialId,
                QuantityPerUnit = 2m
            });
        }

        if (stockQuantity > 0)
        {
            db.StockMovements.Add(new StockMovementEntity
            {
                Id = Guid.NewGuid(),
                RawMaterialId = RawMaterialId,
                Type = StockMovementType.Inbound.ToString(),
                Quantity = stockQuantity,
                UnitCostAmount = 3m,
                Reason = "Test fixture inbound stock",
                OccurredAt = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero)
            });
        }

        await db.SaveChangesAsync();
    }
}
