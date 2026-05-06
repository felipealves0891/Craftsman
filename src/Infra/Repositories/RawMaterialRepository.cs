using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Repositories;
using Craftsman.Domain.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class RawMaterialRepository : IRawMaterialRepository
{
    private readonly IApplicationCache? cache;
    private readonly AppDbContext dbContext;

    public RawMaterialRepository(AppDbContext dbContext, IApplicationCache? cache = null)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    public async Task<RawMaterial?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.RawMaterials.FirstOrDefaultAsync(material => material.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyCollection<RawMaterial>> ListAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = "inventory:raw-materials";
        if (cache is not null)
        {
            return await cache.GetOrCreateAsync(cacheKey, LoadMaterialsAsync, cancellationToken: cancellationToken);
        }

        return await LoadMaterialsAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<RawMaterial>> LoadMaterialsAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.RawMaterials
            .OrderBy(material => material.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList().AsReadOnly();
    }

    public async Task AddAsync(RawMaterial rawMaterial, CancellationToken cancellationToken = default)
    {
        await dbContext.RawMaterials.AddAsync(ToEntity(rawMaterial), cancellationToken);
        cache?.RemoveByPrefix("inventory:");
    }

    public async Task UpdateAsync(RawMaterial rawMaterial, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.RawMaterials.FirstOrDefaultAsync(
            existing => existing.Id == rawMaterial.Id,
            cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException($"Raw material '{rawMaterial.Id}' was not found.");
        }

        MapToEntity(rawMaterial, entity);
        cache?.RemoveByPrefix("inventory:");
    }

    private static RawMaterial ToModel(RawMaterialEntity entity)
    {
        return new RawMaterial(
            entity.Id,
            entity.Name,
            entity.UnitOfMeasure,
            Enum.Parse<RawMaterialStatus>(entity.Status),
            entity.MinimumStockLevel,
            entity.CriticalStockLevel);
    }

    private static RawMaterialEntity ToEntity(RawMaterial rawMaterial)
    {
        var entity = new RawMaterialEntity();
        MapToEntity(rawMaterial, entity);
        return entity;
    }

    private static void MapToEntity(RawMaterial rawMaterial, RawMaterialEntity entity)
    {
        entity.Id = rawMaterial.Id;
        entity.Name = rawMaterial.Name;
        entity.UnitOfMeasure = rawMaterial.UnitOfMeasure;
        entity.Status = rawMaterial.Status.ToString();
        entity.MinimumStockLevel = rawMaterial.MinimumStockLevel;
        entity.CriticalStockLevel = rawMaterial.CriticalStockLevel;
    }
}
