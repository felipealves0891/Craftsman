using Craftsman.Domain.Events;
using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Finance.Repositories;
using Craftsman.Domain.Finance.Services;
using Craftsman.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace Craftsman.Tests.Finance;

public sealed class DeliveryConfirmedSettlementHandlerTests
{
    [Fact]
    public async Task Handler_does_not_create_duplicate_settlement_for_same_order()
    {
        var orderId = Guid.NewGuid();
        var repository = new FakeFinancialSettlementRepository(new FinancialSettlement(Guid.NewGuid(), orderId, 10, 3, 0));
        var handler = new DeliveryConfirmedSettlementHandler(
            NullLogger<DeliveryConfirmedSettlementHandler>.Instance,
            new FakeSettlementCalculator(),
            repository,
            new FakeUnitOfWork(),
            new FakeDomainEventPublisher());

        await handler.HandleAsync(new DeliveryConfirmedEvent(Guid.NewGuid(), orderId, DateTimeOffset.UtcNow));

        Assert.Single(repository.Settlements);
    }

    private sealed class FakeFinancialSettlementRepository : IFinancialSettlementRepository
    {
        public List<FinancialSettlement> Settlements { get; }

        public FakeFinancialSettlementRepository(params FinancialSettlement[] settlements)
        {
            Settlements = settlements.ToList();
        }

        public Task AddAsync(FinancialSettlement settlement, CancellationToken cancellationToken = default)
        {
            Settlements.Add(settlement);
            return Task.CompletedTask;
        }

        public Task<FinancialSettlement?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Settlements.FirstOrDefault(settlement => settlement.OrderId == orderId));
        }

        public Task<IReadOnlyCollection<FinancialSettlement>> ListAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<FinancialSettlement>>(Settlements.AsReadOnly());
        }
    }

    private sealed class FakeSettlementCalculator : ISettlementCalculator
    {
        public Task<FinancialSettlement> CalculateAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new FinancialSettlement(Guid.NewGuid(), orderId, 10, 3, 0));
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
