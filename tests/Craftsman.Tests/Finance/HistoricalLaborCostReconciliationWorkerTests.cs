using Craftsman.App.Services;
using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Finance.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Craftsman.Tests.Finance;

public sealed class HistoricalLaborCostReconciliationWorkerTests
{
    [Fact]
    public async Task Worker_continues_after_an_individual_reconciliation_failure()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var repository = new FakeSettlementRepository(
            new FinancialSettlement(firstId, Guid.NewGuid(), 0, 0, 0, raiseCalculatedEvent: false, laborCostReconciled: false),
            new FinancialSettlement(secondId, Guid.NewGuid(), 0, 0, 0, raiseCalculatedEvent: false, laborCostReconciled: false));
        var processor = new FakeReconciliationService(firstId);
        using var provider = BuildProvider(repository, processor);
        var worker = new HistoricalLaborCostReconciliationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<HistoricalLaborCostReconciliationWorker>.Instance);

        await worker.RunAsync();

        Assert.Equal([firstId, secondId], processor.AttemptedIds);
        Assert.Contains(secondId, processor.SucceededIds);
    }

    [Fact]
    public async Task Worker_swallows_startup_failure_so_application_can_continue()
    {
        var repository = new FakeSettlementRepository { ThrowOnList = true };
        using var provider = BuildProvider(repository, new FakeReconciliationService());
        var worker = new HistoricalLaborCostReconciliationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<HistoricalLaborCostReconciliationWorker>.Instance);

        var exception = await Record.ExceptionAsync(() => worker.RunAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task Worker_stops_before_processing_when_cancellation_was_requested()
    {
        var settlementId = Guid.NewGuid();
        var repository = new FakeSettlementRepository(
            new FinancialSettlement(settlementId, Guid.NewGuid(), 0, 0, 0, raiseCalculatedEvent: false, laborCostReconciled: false));
        var processor = new FakeReconciliationService();
        using var provider = BuildProvider(repository, processor);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var worker = new HistoricalLaborCostReconciliationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<HistoricalLaborCostReconciliationWorker>.Instance);

        await worker.RunAsync(cancellation.Token);

        Assert.Empty(processor.AttemptedIds);
    }

    [Fact]
    public async Task Worker_handles_cancellation_raised_while_loading_pending_settlements()
    {
        var repository = new FakeSettlementRepository { ThrowOnCancellation = true };
        using var provider = BuildProvider(repository, new FakeReconciliationService());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var worker = new HistoricalLaborCostReconciliationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<HistoricalLaborCostReconciliationWorker>.Instance);

        var exception = await Record.ExceptionAsync(() => worker.RunAsync(cancellation.Token));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Worker_handles_cancellation_raised_by_an_item_processor()
    {
        var settlementId = Guid.NewGuid();
        var repository = new FakeSettlementRepository(
            new FinancialSettlement(settlementId, Guid.NewGuid(), 0, 0, 0, raiseCalculatedEvent: false, laborCostReconciled: false));
        using var cancellation = new CancellationTokenSource();
        var processor = new FakeReconciliationService(onCall: cancellation.Cancel, throwCancellation: true);
        using var provider = BuildProvider(repository, processor);
        var worker = new HistoricalLaborCostReconciliationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<HistoricalLaborCostReconciliationWorker>.Instance);

        var exception = await Record.ExceptionAsync(() => worker.RunAsync(cancellation.Token));

        Assert.Null(exception);
        Assert.Equal([settlementId], processor.AttemptedIds);
    }

    private static ServiceProvider BuildProvider(
        FakeSettlementRepository repository,
        FakeReconciliationService processor)
    {
        var services = new ServiceCollection();
        services.AddScoped<IFinancialSettlementRepository>(_ => repository);
        services.AddScoped<IHistoricalLaborCostReconciliationService>(_ => processor);
        return services.BuildServiceProvider();
    }

    private sealed class FakeReconciliationService : IHistoricalLaborCostReconciliationService
    {
        private readonly Guid? failingId;
        private readonly Action? onCall;
        private readonly bool throwCancellation;

        public FakeReconciliationService(Guid? failingId = null, Action? onCall = null, bool throwCancellation = false)
        {
            this.failingId = failingId;
            this.onCall = onCall;
            this.throwCancellation = throwCancellation;
        }

        public List<Guid> AttemptedIds { get; } = [];

        public List<Guid> SucceededIds { get; } = [];

        public Task ReconcileAsync(Guid settlementId, CancellationToken cancellationToken = default)
        {
            AttemptedIds.Add(settlementId);
            onCall?.Invoke();
            if (throwCancellation)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (settlementId == failingId)
            {
                throw new InvalidOperationException("Synthetic reconciliation failure.");
            }

            SucceededIds.Add(settlementId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSettlementRepository : IFinancialSettlementRepository
    {
        private readonly IReadOnlyCollection<FinancialSettlement> settlements;

        public FakeSettlementRepository(params FinancialSettlement[] settlements)
        {
            this.settlements = settlements;
        }

        public bool ThrowOnList { get; init; }

        public bool ThrowOnCancellation { get; init; }

        public Task<FinancialSettlement?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<FinancialSettlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<FinancialSettlement>> ListAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<FinancialSettlement>> ListPendingLaborCostReconciliationAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowOnCancellation)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (ThrowOnList)
            {
                throw new InvalidOperationException("Synthetic repository failure.");
            }

            return Task.FromResult(settlements);
        }

        public Task AddAsync(FinancialSettlement settlement, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(FinancialSettlement settlement, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ReplaceForOrderAsync(FinancialSettlement settlement, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteByOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
