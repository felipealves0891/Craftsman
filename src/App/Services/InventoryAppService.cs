using Craftsman.App.Models;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Repositories;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Finance.Repositories;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Services;

namespace Craftsman.App.Services;

public sealed class InventoryAppService
{
    private readonly IRawMaterialRepository rawMaterialRepository;
    private readonly IStockMovementRepository stockMovementRepository;
    private readonly IOrderRepository orderRepository;
    private readonly IFinancialSettlementRepository financialSettlementRepository;
    private readonly IProductionTaskRepository? productionTaskRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IApplicationCache? cache;

    public InventoryAppService(
        IRawMaterialRepository rawMaterialRepository,
        IStockMovementRepository stockMovementRepository,
        IUnitOfWork unitOfWork,
        IOrderRepository orderRepository,
        IFinancialSettlementRepository financialSettlementRepository,
        IApplicationCache? cache = null,
        IProductionTaskRepository? productionTaskRepository = null)
    {
        this.rawMaterialRepository = rawMaterialRepository;
        this.stockMovementRepository = stockMovementRepository;
        this.orderRepository = orderRepository;
        this.financialSettlementRepository = financialSettlementRepository;
        this.unitOfWork = unitOfWork;
        this.cache = cache;
        this.productionTaskRepository = productionTaskRepository;
    }

    public async Task<IReadOnlyCollection<RawMaterialListItemViewModel>> ListRawMaterialsAsync(CancellationToken cancellationToken = default)
    {
        var materials = await rawMaterialRepository.ListAsync(cancellationToken);
        var viewModels = new List<RawMaterialListItemViewModel>();

        foreach (var material in materials)
        {
            var balance = await stockMovementRepository.GetBalanceAsync(material.Id, cancellationToken);
            viewModels.Add(new RawMaterialListItemViewModel(
                material.Id,
                material.Name,
                material.UnitOfMeasure,
                material.Status.ToString(),
                material.MinimumStockLevel,
                material.CriticalStockLevel,
                material.GetStockAlertLevel(balance)));
        }

        return viewModels.AsReadOnly();
    }

    public async Task<RawMaterialInputModel?> GetRawMaterialInputAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var material = await rawMaterialRepository.GetByIdAsync(id, cancellationToken);

        return material is null
            ? null
            : new RawMaterialInputModel
            {
                Id = material.Id,
                Name = material.Name,
                UnitOfMeasure = material.UnitOfMeasure,
                Status = material.Status,
                MinimumStockLevel = material.MinimumStockLevel,
                CriticalStockLevel = material.CriticalStockLevel
            };
    }

    public async Task<Guid> SaveRawMaterialAsync(RawMaterialInputModel input, CancellationToken cancellationToken = default)
    {
        var material = new RawMaterial(
            input.Id.GetValueOrDefault(Guid.NewGuid()),
            input.Name,
            input.UnitOfMeasure,
            input.Status,
            input.MinimumStockLevel,
            input.CriticalStockLevel);

        if (input.Id is null || input.Id == Guid.Empty)
        {
            await rawMaterialRepository.AddAsync(material, cancellationToken);
        }
        else
        {
            await rawMaterialRepository.UpdateAsync(material, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        cache?.RemoveByPrefix("inventory:");

        return material.Id;
    }

    public async Task RegisterMovementAsync(StockMovementInputModel input, CancellationToken cancellationToken = default)
    {
        _ = await rawMaterialRepository.GetByIdAsync(input.RawMaterialId, cancellationToken)
            ?? throw new InvalidOperationException("Materia-prima nao encontrada.");

        if (input.Type == StockMovementType.Outbound && string.IsNullOrWhiteSpace(input.Reason))
        {
            throw new InvalidOperationException("Informe o motivo da saida manual.");
        }

        if (input.Type != StockMovementType.Adjustment && input.Quantity <= 0)
        {
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");
        }

        if (input.Type == StockMovementType.Adjustment && input.Quantity == 0)
        {
            throw new InvalidOperationException("Ajuste de estoque nao pode ter quantidade zero.");
        }

        if (input.Type == StockMovementType.Outbound)
        {
            await ValidateOrderAsync(input.OrderId, cancellationToken);

            var currentBalance = await stockMovementRepository.GetBalanceAsync(input.RawMaterialId, cancellationToken);
            if (currentBalance - input.Quantity < 0)
            {
                throw new InvalidOperationException("Saida manual nao pode gerar saldo negativo.");
            }
        }
        else if (input.Type == StockMovementType.Adjustment)
        {
            var currentBalance = await stockMovementRepository.GetBalanceAsync(input.RawMaterialId, cancellationToken);
            if (currentBalance + input.Quantity < 0)
            {
                throw new InvalidOperationException("Ajuste manual nao pode gerar saldo negativo.");
            }
        }

        var unitCost = input.Type is StockMovementType.Inbound or StockMovementType.Outbound
            ? input.UnitCostAmount
            : 0;
        var movement = new StockMovement(
            Guid.NewGuid(),
            input.RawMaterialId,
            input.Type,
            input.Quantity,
            ResolveManualReason(input),
            null,
            unitCostAmount: unitCost,
            orderId: input.Type == StockMovementType.Outbound ? input.OrderId : null);

        await stockMovementRepository.AddAsync(movement, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<StockMovementReversalResult> ReverseMovementAsync(
        Guid movementId,
        CancellationToken cancellationToken = default)
    {
        if (unitOfWork is ITransactionalUnitOfWork transactionalUnitOfWork)
        {
            return await transactionalUnitOfWork.ExecuteInSerializableTransactionAsync(
                () => ReverseMovementCoreAsync(movementId, cancellationToken),
                cancellationToken);
        }

        return await ReverseMovementCoreAsync(movementId, cancellationToken);
    }

    private async Task<StockMovementReversalResult> ReverseMovementCoreAsync(
        Guid movementId,
        CancellationToken cancellationToken)
    {
        var movement = await stockMovementRepository.GetByIdAsync(movementId, cancellationToken)
            ?? throw new InvalidOperationException("Movimento de estoque nao encontrado.");

        await EnsureMovementCanBeReversedAsync(movement, cancellationToken);

        if (IsUnreferencedProductionConsumption(movement))
        {
            await stockMovementRepository.DeleteAsync(movement.Id, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new StockMovementReversalResult(movement.Id, WasDeleted: true, ReversalMovementId: null);
        }

        var reversalReference = BuildReversalReference(movement.Id);
        if (await stockMovementRepository.ExistsByBusinessReferenceAsync(reversalReference, cancellationToken))
        {
            return new StockMovementReversalResult(movement.Id, WasDeleted: false, ReversalMovementId: null);
        }

        var currentBalance = await stockMovementRepository.GetBalanceAsync(movement.RawMaterialId, cancellationToken);
        var reversal = CreateReversalMovement(movement, reversalReference);
        ValidateReversalBalance(reversal, currentBalance);
        await stockMovementRepository.AddAsync(reversal, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new StockMovementReversalResult(movement.Id, WasDeleted: false, ReversalMovementId: reversal.Id);
    }

    public async Task<IReadOnlyCollection<InventoryBalanceViewModel>> ListBalancesAsync(CancellationToken cancellationToken = default)
    {
        var materials = await rawMaterialRepository.ListAsync(cancellationToken);
        var balances = new List<InventoryBalanceViewModel>();

        foreach (var material in materials)
        {
            var balance = await stockMovementRepository.GetBalanceAsync(material.Id, cancellationToken);
            var alertLevel = material.GetStockAlertLevel(balance);
            var reachedLimit = alertLevel switch
            {
                StockAlertLevel.Critical => material.CriticalStockLevel,
                StockAlertLevel.Warning => material.MinimumStockLevel,
                _ => null
            };
            balances.Add(new InventoryBalanceViewModel(
                material.Id,
                material.Name,
                material.UnitOfMeasure,
                balance,
                alertLevel,
                reachedLimit));
        }

        return balances.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<StockMovementViewModel>> ListMovementsAsync(Guid? rawMaterialId = null, CancellationToken cancellationToken = default)
    {
        var materials = await rawMaterialRepository.ListAsync(cancellationToken);
        var materialNames = materials.ToDictionary(material => material.Id, material => material.Name);
        var orders = await orderRepository.ListAsync(cancellationToken);
        var orderNames = orders.ToDictionary(
            order => order.Id,
            order => $"{order.Origin.Source} - {order.Origin.ExternalOrderId}");
        var movements = rawMaterialId.HasValue
            ? await stockMovementRepository.GetByRawMaterialAsync(rawMaterialId.Value, cancellationToken)
            : await stockMovementRepository.ListAsync(cancellationToken);

        return movements
            .OrderByDescending(movement => movement.OccurredAt)
            .Select(movement => new StockMovementViewModel(
                movement.Id,
                movement.RawMaterialId,
                materialNames.GetValueOrDefault(movement.RawMaterialId, "(materia-prima nao encontrada)"),
                movement.Type.ToString(),
                movement.SignedQuantity,
                movement.UnitCostAmount,
                movement.TotalCostAmount,
                movement.Reason,
                movement.BusinessReference,
                movement.OccurredAt,
                movement.OrderId,
                movement.OrderId.HasValue ? orderNames.GetValueOrDefault(movement.OrderId.Value) : null))
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyCollection<RawMaterialOptionViewModel>> ListRawMaterialOptionsAsync(CancellationToken cancellationToken = default)
    {
        var materials = await rawMaterialRepository.ListAsync(cancellationToken);

        return materials
            .Select(material => new RawMaterialOptionViewModel(material.Id, material.Name, material.UnitOfMeasure))
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyCollection<StockMovementOrderOptionViewModel>> ListOutboundOrderOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.ListAsync(cancellationToken);
        var options = new List<StockMovementOrderOptionViewModel>();

        foreach (var order in orders.Where(order => order.Status == OrderStatus.InProduction))
        {
            if (await financialSettlementRepository.GetByOrderIdAsync(order.Id, cancellationToken) is not null)
            {
                continue;
            }

            options.Add(new StockMovementOrderOptionViewModel(
                order.Id,
                order.Origin.Source,
                order.Origin.ExternalOrderId));
        }

        return options
            .OrderBy(option => option.DisplayName)
            .ToList()
            .AsReadOnly();
    }

    private async Task ValidateOrderAsync(Guid? orderId, CancellationToken cancellationToken)
    {
        if (!orderId.HasValue)
        {
            return;
        }

        var order = await orderRepository.GetByIdAsync(orderId.Value, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("Pedido nao encontrado.");
        }

        if (order.Status != OrderStatus.InProduction)
        {
            throw new InvalidOperationException("Pedido deve estar em producao para ser vinculado.");
        }

        if (await financialSettlementRepository.GetByOrderIdAsync(order.Id, cancellationToken) is not null)
        {
            throw new InvalidOperationException("Pedido ja possui apuracao financeira.");
        }
    }

    private static string ResolveManualReason(StockMovementInputModel input)
    {
        if (!string.IsNullOrWhiteSpace(input.Reason))
        {
            return input.Reason.Trim();
        }

        return input.Type switch
        {
            StockMovementType.Inbound => "Entrada manual",
            StockMovementType.Adjustment => "Ajuste manual",
            _ => throw new InvalidOperationException("Informe o motivo da saida manual.")
        };
    }

    private async Task EnsureMovementCanBeReversedAsync(
        StockMovement movement,
        CancellationToken cancellationToken)
    {
        if (!movement.OrderId.HasValue)
        {
            return;
        }

        var order = await orderRepository.GetByIdAsync(movement.OrderId.Value, cancellationToken)
            ?? throw new InvalidOperationException("Pedido associado nao encontrado.");

        if (order.Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Movimento nao pode ser revertido porque o pedido foi entregue.");
        }

        if (productionTaskRepository is null)
        {
            throw new InvalidOperationException("Repositorio de tarefas de producao nao configurado.");
        }

        var productionTasks = await productionTaskRepository.ListByOrderAsync(order.Id, cancellationToken);
        if (productionTasks.Any(task => task.Status == ProductionTaskStatus.Completed))
        {
            throw new InvalidOperationException("Movimento nao pode ser revertido porque o pedido foi concluido.");
        }
    }

    private static bool IsUnreferencedProductionConsumption(StockMovement movement)
    {
        return movement.Type == StockMovementType.Outbound
            && string.IsNullOrWhiteSpace(movement.BusinessReference)
            && movement.Reason == "Production consumption";
    }

    private static string BuildReversalReference(Guid movementId) => $"stock-movement-reversal:{movementId}";

    private static StockMovement CreateReversalMovement(StockMovement movement, string reversalReference)
    {
        var (type, quantity) = movement.Type switch
        {
            StockMovementType.Inbound => (StockMovementType.Outbound, movement.Quantity),
            StockMovementType.Outbound => (StockMovementType.Inbound, movement.Quantity),
            StockMovementType.Adjustment => (StockMovementType.Adjustment, -movement.Quantity),
            _ => throw new InvalidOperationException($"Unsupported stock movement type {movement.Type}.")
        };

        return new StockMovement(
            Guid.NewGuid(),
            movement.RawMaterialId,
            type,
            quantity,
            "Stock movement reversal",
            reversalReference,
            unitCostAmount: movement.UnitCostAmount,
            orderId: movement.OrderId);
    }

    private static void ValidateReversalBalance(StockMovement reversal, decimal currentBalance)
    {
        if (currentBalance + reversal.SignedQuantity < 0)
        {
            throw new InvalidOperationException("Reversao nao pode gerar saldo negativo.");
        }
    }
}
