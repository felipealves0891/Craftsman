using Craftsman.Domain.Finance.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Craftsman.App.Services;

public sealed class HistoricalLaborCostReconciliationWorker : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<HistoricalLaborCostReconciliationWorker> logger;

    public HistoricalLaborCostReconciliationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<HistoricalLaborCostReconciliationWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return RunAsync(stoppingToken);
    }

    public async Task RunAsync(CancellationToken stoppingToken = default)
    {
        try
        {
            IReadOnlyCollection<Guid> settlementIds;
            using (var scope = scopeFactory.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IFinancialSettlementRepository>();
                var pending = await repository.ListPendingLaborCostReconciliationAsync(stoppingToken);
                settlementIds = pending.Select(settlement => settlement.Id).ToList().AsReadOnly();
            }

            logger.LogInformation("Starting historical labor cost reconciliation for {SettlementCount} settlements.", settlementIds.Count);

            foreach (var settlementId in settlementIds)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IHistoricalLaborCostReconciliationService>();
                    await service.ReconcileAsync(settlementId, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Historical labor cost reconciliation failed for settlement {SettlementId}.", settlementId);
                }
            }

            logger.LogInformation("Historical labor cost reconciliation finished.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Historical labor cost reconciliation was cancelled.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Historical labor cost reconciliation could not start; the application will continue running.");
        }
    }
}
