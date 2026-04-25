using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Finance.Repositories;
using Craftsman.Domain.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class FinancialSettlementRepository : IFinancialSettlementRepository
{
    private readonly IApplicationCache? cache;
    private readonly AppDbContext dbContext;

    public FinancialSettlementRepository(AppDbContext dbContext, IApplicationCache? cache = null)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    public async Task<FinancialSettlement?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.FinancialSettlements.FirstOrDefaultAsync(settlement => settlement.OrderId == orderId, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyCollection<FinancialSettlement>> ListAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        var key = $"finance:settlements:{from?.ToString("yyyyMMdd") ?? "all"}:{to?.ToString("yyyyMMdd") ?? "all"}";

        if (cache is not null)
        {
            return await cache.GetOrCreateAsync(key, token => ListCoreAsync(from, to, token), cancellationToken: cancellationToken);
        }

        return await ListCoreAsync(from, to, cancellationToken);
    }

    public async Task AddAsync(FinancialSettlement settlement, CancellationToken cancellationToken = default)
    {
        await dbContext.FinancialSettlements.AddAsync(ToEntity(settlement), cancellationToken);
        cache?.RemoveByPrefix("finance:");
    }

    private async Task<IReadOnlyCollection<FinancialSettlement>> ListCoreAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var query = dbContext.FinancialSettlements.AsQueryable();

        if (from is not null)
        {
            var start = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(settlement => settlement.CalculatedAt >= start);
        }

        if (to is not null)
        {
            var end = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(settlement => settlement.CalculatedAt < end);
        }

        var entities = await query.OrderByDescending(settlement => settlement.CalculatedAt).ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList().AsReadOnly();
    }

    private static FinancialSettlement ToModel(FinancialSettlementEntity entity)
    {
        return new FinancialSettlement(
            entity.Id,
            entity.OrderId,
            entity.RevenueAmount,
            entity.ProductionCostAmount,
            entity.ShippingCostAmount,
            Enum.Parse<FinancialSettlementStatus>(entity.Status),
            entity.CalculatedAt,
            raiseCalculatedEvent: false);
    }

    private static FinancialSettlementEntity ToEntity(FinancialSettlement settlement)
    {
        return new FinancialSettlementEntity
        {
            Id = settlement.Id,
            OrderId = settlement.OrderId,
            RevenueAmount = settlement.RevenueAmount,
            ProductionCostAmount = settlement.ProductionCostAmount,
            ShippingCostAmount = settlement.ShippingCostAmount,
            TotalCostAmount = settlement.TotalCostAmount,
            MarginAmount = settlement.MarginAmount,
            Status = settlement.Status.ToString(),
            CalculatedAt = settlement.CalculatedAt
        };
    }
}
