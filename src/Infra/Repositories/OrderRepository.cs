using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly IApplicationCache? cache;
    private readonly AppDbContext dbContext;

    public OrderRepository(AppDbContext dbContext, IApplicationCache? cache = null)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await QueryOrders().FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<Order?> GetByOriginAsync(OrderOrigin origin, CancellationToken cancellationToken = default)
    {
        var entity = await QueryOrders()
            .FirstOrDefaultAsync(
                order => order.Source == origin.Source && order.ExternalOrderId == origin.ExternalOrderId,
                cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyCollection<Order>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (cache is not null)
        {
            return await cache.GetOrCreateAsync("sales:orders:list", ListCoreAsync, cancellationToken: cancellationToken);
        }

        return await ListCoreAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await dbContext.Orders.AddAsync(ToEntity(order), cancellationToken);
        cache?.RemoveByPrefix("sales:orders:");
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        dbContext.Orders.Update(ToEntity(order));
        cache?.RemoveByPrefix("sales:orders:");
        return Task.CompletedTask;
    }

    private async Task<IReadOnlyCollection<Order>> ListCoreAsync(CancellationToken cancellationToken)
    {
        var entities = await QueryOrders()
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList().AsReadOnly();
    }

    private IQueryable<OrderEntity> QueryOrders()
    {
        return dbContext.Orders.Include(order => order.Items);
    }

    private static Order ToModel(OrderEntity entity)
    {
        var items = entity.Items.Select(item => new OrderItem(
            item.Id,
            item.ExternalItemId,
            item.Description,
            item.Quantity,
            new Money(item.UnitPriceAmount, item.UnitPriceCurrency),
            item.ProductId));

        return new Order(
            entity.Id,
            new OrderOrigin(entity.Source, entity.ExternalOrderId),
            new CustomerInfo(entity.CustomerName, entity.CustomerEmail),
            items,
            Enum.Parse<OrderStatus>(entity.Status),
            entity.CreatedAt,
            raiseNormalizedEvent: false);
    }

    private static OrderEntity ToEntity(Order order)
    {
        return new OrderEntity
        {
            Id = order.Id,
            Source = order.Origin.Source,
            ExternalOrderId = order.Origin.ExternalOrderId,
            CustomerName = order.Customer.Name,
            CustomerEmail = order.Customer.Email,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(item => new OrderItemEntity
            {
                Id = item.Id,
                OrderId = order.Id,
                ExternalItemId = item.ExternalItemId,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPriceAmount = item.UnitPrice.Amount,
                UnitPriceCurrency = item.UnitPrice.Currency,
                ProductId = item.ProductId
            }).ToList()
        };
    }
}
