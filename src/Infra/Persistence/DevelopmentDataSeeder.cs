using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Persistence;

public static class DevelopmentDataSeeder
{
    private static readonly DateTimeOffset SeedOccurredAt = new(2026, 4, 26, 12, 0, 0, TimeSpan.Zero);

    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Start SeedOrderSourcesAsync");
        await SeedOrderSourcesAsync(dbContext, cancellationToken);
        Console.WriteLine("End SeedOrderSourcesAsync");
        Console.WriteLine("Start SeedRawMaterialsAsync");
        await SeedRawMaterialsAsync(dbContext, cancellationToken);
        Console.WriteLine("End SeedRawMaterialsAsync");
        Console.WriteLine("Start SeedProductsAsync");
        await SeedProductsAsync(dbContext, cancellationToken);
        Console.WriteLine("End SeedProductsAsync");
        Console.WriteLine("Start SeedBillOfMaterialsAsync");
        await SeedBillOfMaterialsAsync(dbContext, cancellationToken);
        Console.WriteLine("End SeedBillOfMaterialsAsync");
        Console.WriteLine("Start SeedProductMappingsAsync");
        await SeedProductMappingsAsync(dbContext, cancellationToken);
        Console.WriteLine("End SeedProductMappingsAsync");
        Console.WriteLine("Start SeedInitialStockAsync");
        await SeedInitialStockAsync(dbContext, cancellationToken);
        Console.WriteLine("End SeedInitialStockAsync");
    }

    private static async Task SeedOrderSourcesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        
        var existingNames = await dbContext.OrderSources
            .Select(source => source.Name)
            .ToListAsync(cancellationToken);

        var existingNameSet = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sources = new[]
        {
            new OrderSourceEntity
            {
                Id = Guid.Parse("b1c93b88-fd77-4d32-a2be-95b9aa7b0101"),
                Name = "Manual",
                CreatedAt = SeedOccurredAt
            },
            new OrderSourceEntity
            {
                Id = Guid.Parse("b1c93b88-fd77-4d32-a2be-95b9aa7b0102"),
                Name = "Simulated",
                CreatedAt = SeedOccurredAt
            },
            new OrderSourceEntity
            {
                Id = Guid.Parse("b1c93b88-fd77-4d32-a2be-95b9aa7b0103"),
                Name = "Shopee",
                CreatedAt = SeedOccurredAt
            }
        };

        var missingSources = sources
            .Where(source => !existingNameSet.Contains(source.Name))
            .ToList();

        if (missingSources.Count == 0)
        {
            return;
        }

        await dbContext.OrderSources.AddRangeAsync(missingSources, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRawMaterialsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var existingNames = await dbContext.RawMaterials
            .Select(material => material.Name)
            .ToListAsync(cancellationToken);

        var existingNameSet = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rawMaterials = new[]
        {
            new RawMaterialEntity
            {
                Id = Guid.Parse("f1b7c2a6-3df4-4f7f-a6d5-8a34c0a9a101"),
                Name = "Argila branca",
                UnitOfMeasure = "kg",
                Status = RawMaterialStatus.Active.ToString()
            },
            new RawMaterialEntity
            {
                Id = Guid.Parse("f1b7c2a6-3df4-4f7f-a6d5-8a34c0a9a102"),
                Name = "Esmalte azul",
                UnitOfMeasure = "l",
                Status = RawMaterialStatus.Active.ToString()
            },
            new RawMaterialEntity
            {
                Id = Guid.Parse("f1b7c2a6-3df4-4f7f-a6d5-8a34c0a9a103"),
                Name = "Caixa kraft",
                UnitOfMeasure = "un",
                Status = RawMaterialStatus.Active.ToString()
            },
            new RawMaterialEntity
            {
                Id = Guid.Parse("f1b7c2a6-3df4-4f7f-a6d5-8a34c0a9a104"),
                Name = "Tecido algodao cru",
                UnitOfMeasure = "m",
                Status = RawMaterialStatus.Active.ToString()
            },
            new RawMaterialEntity
            {
                Id = Guid.Parse("f1b7c2a6-3df4-4f7f-a6d5-8a34c0a9a105"),
                Name = "Linha encerada",
                UnitOfMeasure = "m",
                Status = RawMaterialStatus.Active.ToString()
            },
            new RawMaterialEntity
            {
                Id = Guid.Parse("f1b7c2a6-3df4-4f7f-a6d5-8a34c0a9a106"),
                Name = "Cartao de agradecimento",
                UnitOfMeasure = "un",
                Status = RawMaterialStatus.Active.ToString()
            }
        };

        var missingRawMaterials = rawMaterials
            .Where(material => !existingNameSet.Contains(material.Name))
            .ToList();

        if (missingRawMaterials.Count == 0)
        {
            return;
        }

        await dbContext.RawMaterials.AddRangeAsync(missingRawMaterials, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedProductsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var existingNames = await dbContext.Products
            .Select(product => product.Name)
            .ToListAsync(cancellationToken);

        var existingNameSet = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var products = new[]
        {
            new ProductEntity
            {
                Id = Guid.Parse("a8845f12-c9f4-46d6-8d8d-15f0714f7101"),
                Name = "Caneca artesanal azul",
                Status = ProductStatus.Active.ToString(),
                ProductionDurationHours = 72,
                HourlyRate = 25m
            },
            new ProductEntity
            {
                Id = Guid.Parse("a8845f12-c9f4-46d6-8d8d-15f0714f7102"),
                Name = "Ecobag algodao cru",
                Status = ProductStatus.Active.ToString(),
                ProductionDurationHours = 48,
                HourlyRate = 20m
            },
            new ProductEntity
            {
                Id = Guid.Parse("a8845f12-c9f4-46d6-8d8d-15f0714f7103"),
                Name = "Kit presente atelie",
                Status = ProductStatus.Active.ToString(),
                ProductionDurationHours = 96,
                HourlyRate = 30m
            }
        };

        var missingProducts = products
            .Where(product => !existingNameSet.Contains(product.Name))
            .ToList();

        if (missingProducts.Count == 0)
        {
            return;
        }

        await dbContext.Products.AddRangeAsync(missingProducts, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedBillOfMaterialsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var productsByName = await dbContext.Products
            .ToDictionaryAsync(product => product.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var materialsByName = await dbContext.RawMaterials
            .ToDictionaryAsync(material => material.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var billOfMaterials = new[]
        {
            CreateBillOfMaterialsItem(productsByName, materialsByName, "Caneca artesanal azul", "Argila branca", 0.450m),
            CreateBillOfMaterialsItem(productsByName, materialsByName, "Caneca artesanal azul", "Esmalte azul", 0.080m),
            CreateBillOfMaterialsItem(productsByName, materialsByName, "Caneca artesanal azul", "Caixa kraft", 1m),
            CreateBillOfMaterialsItem(productsByName, materialsByName, "Ecobag algodao cru", "Tecido algodao cru", 0.700m),
            CreateBillOfMaterialsItem(productsByName, materialsByName, "Ecobag algodao cru", "Linha encerada", 2.500m),
            CreateBillOfMaterialsItem(productsByName, materialsByName, "Kit presente atelie", "Argila branca", 0.450m),
            CreateBillOfMaterialsItem(productsByName, materialsByName, "Kit presente atelie", "Esmalte azul", 0.080m),
            CreateBillOfMaterialsItem(productsByName, materialsByName, "Kit presente atelie", "Tecido algodao cru", 0.700m),
            CreateBillOfMaterialsItem(productsByName, materialsByName, "Kit presente atelie", "Linha encerada", 2.500m),
            CreateBillOfMaterialsItem(productsByName, materialsByName, "Kit presente atelie", "Caixa kraft", 1m),
            CreateBillOfMaterialsItem(productsByName, materialsByName, "Kit presente atelie", "Cartao de agradecimento", 1m)
        };

        var validBillOfMaterials = billOfMaterials
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();

        var existingPairs = await dbContext.BillOfMaterialsItems
            .Select(item => new { item.ProductId, item.RawMaterialId })
            .ToListAsync(cancellationToken);

        var missingItems = validBillOfMaterials
            .Where(item => !existingPairs.Any(existing => existing.ProductId == item.ProductId && existing.RawMaterialId == item.RawMaterialId))
            .ToList();

        if (missingItems.Count == 0)
        {
            return;
        }

        await dbContext.BillOfMaterialsItems.AddRangeAsync(missingItems, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedProductMappingsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var productsByName = await dbContext.Products
            .ToDictionaryAsync(product => product.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var productMappings = new[]
        {
            CreateProductMapping(productsByName, "Caneca artesanal azul", "Simulated", "SIM-SKU-1"),
            CreateProductMapping(productsByName, "Caneca artesanal azul", "DevSeed", "CAN-AZUL"),
            CreateProductMapping(productsByName, "Ecobag algodao cru", "DevSeed", "ECO-CRU"),
            CreateProductMapping(productsByName, "Kit presente atelie", "DevSeed", "KIT-ATELIE")
        };

        var existingMappings = await dbContext.ProductMappings
            .Select(mapping => new { mapping.Source, mapping.ExternalItemId })
            .ToListAsync(cancellationToken);

        var missingMappings = productMappings
            .Where(mapping => mapping is not null)
            .Select(mapping => mapping!)
            .Where(mapping => !existingMappings.Any(existing =>
                existing.Source == mapping.Source && existing.ExternalItemId == mapping.ExternalItemId))
            .ToList();

        if (missingMappings.Count == 0)
        {
            return;
        }

        await dbContext.ProductMappings.AddRangeAsync(missingMappings, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedInitialStockAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var materialsByName = await dbContext.RawMaterials
            .ToDictionaryAsync(material => material.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var stockMovements = new[]
        {
            CreateStockMovement(materialsByName, "Argila branca", 25m, 12.50m),
            CreateStockMovement(materialsByName, "Esmalte azul", 8m, 38.90m),
            CreateStockMovement(materialsByName, "Caixa kraft", 120m, 1.75m),
            CreateStockMovement(materialsByName, "Tecido algodao cru", 40m, 16.00m),
            CreateStockMovement(materialsByName, "Linha encerada", 300m, 0.22m),
            CreateStockMovement(materialsByName, "Cartao de agradecimento", 80m, 0.65m)
        };

        var existingReferences = await dbContext.StockMovements
            .Where(movement => movement.BusinessReference != null && movement.BusinessReference.StartsWith("dev-seed:initial-stock:"))
            .Select(movement => movement.BusinessReference)
            .ToListAsync(cancellationToken);

        var existingReferenceSet = existingReferences.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingMovements = stockMovements
            .Where(movement => movement is not null)
            .Select(movement => movement!)
            .Where(movement => movement.BusinessReference is not null && !existingReferenceSet.Contains(movement.BusinessReference))
            .ToList();

        if (missingMovements.Count == 0)
        {
            return;
        }

        await dbContext.StockMovements.AddRangeAsync(missingMovements, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static BillOfMaterialsItemEntity? CreateBillOfMaterialsItem(
        IReadOnlyDictionary<string, ProductEntity> productsByName,
        IReadOnlyDictionary<string, RawMaterialEntity> materialsByName,
        string productName,
        string rawMaterialName,
        decimal quantityPerUnit)
    {
        if (!productsByName.TryGetValue(productName, out var product) ||
            !materialsByName.TryGetValue(rawMaterialName, out var material))
        {
            return null;
        }

        return new BillOfMaterialsItemEntity
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            RawMaterialId = material.Id,
            QuantityPerUnit = quantityPerUnit
        };
    }

    private static ProductMappingEntity? CreateProductMapping(
        IReadOnlyDictionary<string, ProductEntity> productsByName,
        string productName,
        string source,
        string externalItemId)
    {
        if (!productsByName.TryGetValue(productName, out var product))
        {
            return null;
        }

        return new ProductMappingEntity
        {
            Id = Guid.NewGuid(),
            Source = source,
            ExternalItemId = externalItemId,
            ProductId = product.Id
        };
    }

    private static StockMovementEntity? CreateStockMovement(
        IReadOnlyDictionary<string, RawMaterialEntity> materialsByName,
        string rawMaterialName,
        decimal quantity,
        decimal unitCostAmount)
    {
        if (!materialsByName.TryGetValue(rawMaterialName, out var material))
        {
            return null;
        }

        return new StockMovementEntity
        {
            Id = Guid.NewGuid(),
            RawMaterialId = material.Id,
            Type = StockMovementType.Inbound.ToString(),
            Quantity = quantity,
            UnitCostAmount = unitCostAmount,
            Reason = "Estoque inicial de desenvolvimento",
            BusinessReference = $"dev-seed:initial-stock:{rawMaterialName}",
            OccurredAt = SeedOccurredAt
        };
    }
}
