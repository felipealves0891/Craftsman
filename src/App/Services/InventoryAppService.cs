using Craftsman.App.Models;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Repositories;
using Craftsman.Domain.Repositories;

namespace Craftsman.App.Services;

public sealed class InventoryAppService
{
    private readonly IRawMaterialRepository rawMaterialRepository;
    private readonly IStockMovementRepository stockMovementRepository;
    private readonly IUnitOfWork unitOfWork;

    public InventoryAppService(
        IRawMaterialRepository rawMaterialRepository,
        IStockMovementRepository stockMovementRepository,
        IUnitOfWork unitOfWork)
    {
        this.rawMaterialRepository = rawMaterialRepository;
        this.stockMovementRepository = stockMovementRepository;
        this.unitOfWork = unitOfWork;
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

        var unitCost = input.Type == StockMovementType.Inbound ? input.UnitCostAmount : 0;
        var movement = new StockMovement(
            Guid.NewGuid(),
            input.RawMaterialId,
            input.Type,
            input.Quantity,
            ResolveManualReason(input),
            null,
            unitCostAmount: unitCost);

        await stockMovementRepository.AddAsync(movement, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
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
                movement.OccurredAt))
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
}
