using Craftsman.Domain.Finance.Repositories;
using Craftsman.Domain.Finance.Services;
using Craftsman.Domain.Inventory.Repositories;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Repositories;

namespace Craftsman.App.Services;

public interface IHistoricalLaborCostReconciliationService
{
    Task ReconcileAsync(Guid settlementId, CancellationToken cancellationToken = default);
}

public sealed class HistoricalLaborCostReconciliationService : IHistoricalLaborCostReconciliationService
{
    private readonly IFinancialSettlementRepository settlementRepository;
    private readonly IOrderRepository orderRepository;
    private readonly IProductionTaskRepository productionTaskRepository;
    private readonly IStockMovementRepository stockMovementRepository;
    private readonly IProductRepository productRepository;
    private readonly ITransactionalUnitOfWork transactionalUnitOfWork;
    private readonly IUnitOfWork unitOfWork;

    public HistoricalLaborCostReconciliationService(
        IFinancialSettlementRepository settlementRepository,
        IOrderRepository orderRepository,
        IProductionTaskRepository productionTaskRepository,
        IStockMovementRepository stockMovementRepository,
        IProductRepository productRepository,
        ITransactionalUnitOfWork transactionalUnitOfWork,
        IUnitOfWork unitOfWork)
    {
        this.settlementRepository = settlementRepository;
        this.orderRepository = orderRepository;
        this.productionTaskRepository = productionTaskRepository;
        this.stockMovementRepository = stockMovementRepository;
        this.productRepository = productRepository;
        this.transactionalUnitOfWork = transactionalUnitOfWork;
        this.unitOfWork = unitOfWork;
    }

    public async Task ReconcileAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        var settlement = await settlementRepository.GetByIdAsync(settlementId, cancellationToken);
        if (settlement is null || settlement.LaborCostReconciled)
        {
            return;
        }

        var order = await orderRepository.GetByIdAsync(settlement.OrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order '{settlement.OrderId}' for financial settlement '{settlement.Id}' was not found.");
        var productionTasks = await productionTaskRepository.ListByOrderAsync(order.Id, cancellationToken);
        var stockMovements = await stockMovementRepository.ListAsync(cancellationToken);
        var products = productionTasks.Count == 0
            ? new Dictionary<Guid, Craftsman.Domain.ProductCatalog.Entities.Product>()
            : (await productRepository.ListAsync(cancellationToken)).ToDictionary(product => product.Id);
        var materialCost = SettlementCostCalculator.CalculateMaterialCost(productionTasks, stockMovements);
        var laborCost = SettlementCostCalculator.CalculateLaborCost(productionTasks, products);

        settlement.ReconcileLaborCost(laborCost, materialCost + laborCost);

        await transactionalUnitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await settlementRepository.UpdateAsync(settlement, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }
}
