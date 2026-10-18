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
        var entity = await QueryOrders()
            .AsNoTracking()
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<Order?> GetByOriginAsync(OrderOrigin origin, CancellationToken cancellationToken = default)
    {
        var entity = await QueryOrders()
            .AsNoTracking()
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

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        var entity = await QueryOrders().FirstOrDefaultAsync(existing => existing.Id == order.Id, cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException($"Order '{order.Id}' was not found.");
        }
        else
        {
            entity.Source = order.Origin.Source;
            entity.ExternalOrderId = order.Origin.ExternalOrderId;
            entity.Status = order.Status.ToString();
            entity.CreatedAt = order.CreatedAt;
            entity.ShippingDate = order.ShippingDate;
            entity.CustomerId = order.CustomerId;
            entity.DiscountDescription = order.Discount?.Description;
            entity.DiscountAmount = order.Discount?.Amount.Amount ?? 0;

            var orderItemsById = order.Items.ToDictionary(item => item.Id);
            var removedItems = entity.Items
                .Where(item => !orderItemsById.ContainsKey(item.Id))
                .ToList();

            dbContext.OrderItems.RemoveRange(removedItems);

            foreach (var removedItem in removedItems)
            {
                entity.Items.Remove(removedItem);
            }

            foreach (var item in order.Items)
            {
                var existingItem = entity.Items.FirstOrDefault(entityItem => entityItem.Id == item.Id);
                if (existingItem is null)
                {
                    entity.Items.Add(new OrderItemEntity
                    {
                        Id = item.Id,
                        OrderId = order.Id,
                        ExternalItemId = item.ExternalItemId,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPriceAmount = item.UnitPrice.Amount,
                        UnitPriceCurrency = item.UnitPrice.Currency,
                        ProductId = item.ProductId
                    });
                    continue;
                }

                existingItem.ExternalItemId = item.ExternalItemId;
                existingItem.Description = item.Description;
                existingItem.Quantity = item.Quantity;
                existingItem.UnitPriceAmount = item.UnitPrice.Amount;
                existingItem.UnitPriceCurrency = item.UnitPrice.Currency;
                existingItem.ProductId = item.ProductId;
            }
        }

        cache?.RemoveByPrefix("sales:orders:");
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await QueryOrders().FirstOrDefaultAsync(existing => existing.Id == id, cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException($"Order '{id}' was not found.");
        }

        dbContext.Orders.Remove(entity);
        cache?.RemoveByPrefix("sales:orders:");
    }

    private async Task<IReadOnlyCollection<Order>> ListCoreAsync(CancellationToken cancellationToken)
    {
        var entities = await QueryOrders()
            .AsNoTracking()
            .OrderByDescending(order => order.ShippingDate)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList().AsReadOnly();
    }

    private IQueryable<OrderEntity> QueryOrders()
    {
        return dbContext.Orders
            .Include(order => order.Customer)
            .Include(order => order.Items);
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

        var customer = entity.Customer is null
            ? null
            : new Customer(entity.Customer.Id, entity.Customer.Name, entity.Customer.Phone, entity.Customer.Email, entity.Customer.CreatedAt);
        var discount = entity.DiscountAmount > 0
            ? new OrderDiscount(entity.DiscountDescription ?? string.Empty, new Money(entity.DiscountAmount, "BRL"))
            : null;

        return new Order(
            entity.Id,
            new OrderOrigin(entity.Source, entity.ExternalOrderId),
            items,
            Enum.Parse<OrderStatus>(entity.Status),
            entity.CreatedAt,
            entity.ShippingDate,
            customer,
            entity.CustomerId,
            discount,
            raiseNormalizedEvent: false);
    }

    private static OrderEntity ToEntity(Order order)
    {
        return new OrderEntity
        {
            Id = order.Id,
            Source = order.Origin.Source,
            ExternalOrderId = order.Origin.ExternalOrderId,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            ShippingDate = order.ShippingDate,
            CustomerId = order.CustomerId,
            DiscountDescription = order.Discount?.Description,
            DiscountAmount = order.Discount?.Amount.Amount ?? 0,
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
