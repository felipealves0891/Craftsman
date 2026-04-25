using Craftsman.Domain.Events;
using Craftsman.Domain.Integration.Models;
using Craftsman.Domain.Integration.Services;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Infra.Services;

namespace Craftsman.Tests.Integration;

public sealed class OrderImportPipelineTests
{
    [Fact]
    public async Task Pipeline_imports_valid_orders_and_reports_invalid_orders()
    {
        var orderRepository = new FakeOrderRepository();
        var pipeline = new OrderImportPipeline(
            [new InMemoryOrderSource()],
            new OrderNormalizer(),
            orderRepository,
            new FakeUnitOfWork(),
            new FakeDomainEventPublisher());

        var result = await pipeline.ImportAsync();

        Assert.Equal(1, result.ImportedCount);
        Assert.Single(result.Failures);
        Assert.Single(orderRepository.Orders);
    }

    [Fact]
    public async Task Pipeline_skips_duplicate_external_order()
    {
        var orderRepository = new FakeOrderRepository();
        var existingOrder = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Simulated", "SIM-1001"),
            new CustomerInfo("Cliente", null),
            [new OrderItem(Guid.NewGuid(), "SIM-SKU-1", "Produto", 1, new Money(10, "BRL"))]);
        await orderRepository.AddAsync(existingOrder);

        var pipeline = new OrderImportPipeline(
            [new InMemoryOrderSource()],
            new OrderNormalizer(),
            orderRepository,
            new FakeUnitOfWork(),
            new FakeDomainEventPublisher());

        var result = await pipeline.ImportAsync();

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        public List<Order> Orders { get; } = [];

        public Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            Orders.Add(order);
            return Task.CompletedTask;
        }

        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Orders.FirstOrDefault(order => order.Id == id));
        }

        public Task<Order?> GetByOriginAsync(OrderOrigin origin, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Orders.FirstOrDefault(order =>
                order.Origin.Source == origin.Source &&
                order.Origin.ExternalOrderId == origin.ExternalOrderId));
        }

        public Task<IReadOnlyCollection<Order>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Order>>(Orders.AsReadOnly());
        }

        public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakeDomainEventPublisher : IDomainEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            return Task.CompletedTask;
        }
    }
}
