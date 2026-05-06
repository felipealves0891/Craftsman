using Craftsman.App.Models;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Repositories;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Services;

namespace Craftsman.App.Services;

public sealed class StockAlertAppService
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(2);

    private readonly IApplicationCache? cache;
    private readonly IOrderRepository orderRepository;
    private readonly IProductRepository productRepository;
    private readonly IRawMaterialRepository rawMaterialRepository;
    private readonly IStockMovementRepository stockMovementRepository;

    public StockAlertAppService(
        IRawMaterialRepository rawMaterialRepository,
        IStockMovementRepository stockMovementRepository,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IApplicationCache? cache = null)
    {
        this.rawMaterialRepository = rawMaterialRepository;
        this.stockMovementRepository = stockMovementRepository;
        this.orderRepository = orderRepository;
        this.productRepository = productRepository;
        this.cache = cache;
    }

    public async Task<IReadOnlyCollection<StockAlertViewModel>> ListAlertsAsync(CancellationToken cancellationToken = default)
    {
        if (cache is not null)
        {
            return await cache.GetOrCreateAsync(
                "inventory:alerts:all",
                LoadAlertsAsync,
                CacheExpiration,
                cancellationToken);
        }

        return await LoadAlertsAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<StockAlertViewModel>> ListCriticalAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = await ListAlertsAsync(cancellationToken);

        return alerts
            .Where(alert => alert.Level == StockAlertLevel.Critical)
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyCollection<StockAlertViewModel>> ListAlertsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.Items.Any(item => item.ProductId is null))
        {
            return Array.Empty<StockAlertViewModel>();
        }

        var requirements = new Dictionary<Guid, decimal>();
        foreach (var item in order.Items)
        {
            var product = await productRepository.GetByIdAsync(item.ProductId!.Value, cancellationToken);
            if (product is null)
            {
                continue;
            }

            foreach (var billOfMaterialsItem in product.BillOfMaterials)
            {
                requirements[billOfMaterialsItem.RawMaterialId] =
                    requirements.GetValueOrDefault(billOfMaterialsItem.RawMaterialId) +
                    billOfMaterialsItem.QuantityPerUnit * item.Quantity;
            }
        }

        if (requirements.Count == 0)
        {
            return Array.Empty<StockAlertViewModel>();
        }

        var materials = await rawMaterialRepository.ListAsync(cancellationToken);
        var relatedMaterials = materials
            .Where(material => requirements.ContainsKey(material.Id))
            .ToList();

        return await BuildAlertsAsync(relatedMaterials, cancellationToken);
    }

    private async Task<IReadOnlyCollection<StockAlertViewModel>> LoadAlertsAsync(CancellationToken cancellationToken)
    {
        var materials = await rawMaterialRepository.ListAsync(cancellationToken);
        var configuredMaterials = materials
            .Where(material => material.MinimumStockLevel.HasValue || material.CriticalStockLevel.HasValue)
            .ToList();

        return await BuildAlertsAsync(configuredMaterials, cancellationToken);
    }

    private async Task<IReadOnlyCollection<StockAlertViewModel>> BuildAlertsAsync(
        IReadOnlyCollection<RawMaterial> materials,
        CancellationToken cancellationToken)
    {
        var alerts = new List<StockAlertViewModel>();

        foreach (var material in materials)
        {
            var balance = await stockMovementRepository.GetBalanceAsync(material.Id, cancellationToken);
            var level = material.GetStockAlertLevel(balance);

            if (level == StockAlertLevel.Normal)
            {
                continue;
            }

            var reachedLimit = level == StockAlertLevel.Critical
                ? material.CriticalStockLevel!.Value
                : material.MinimumStockLevel!.Value;

            alerts.Add(new StockAlertViewModel(
                material.Id,
                material.Name,
                material.UnitOfMeasure,
                balance,
                level,
                reachedLimit));
        }

        return alerts
            .OrderBy(alert => alert.Level == StockAlertLevel.Critical ? 0 : 1)
            .ThenBy(alert => alert.RawMaterialName)
            .ToList()
            .AsReadOnly();
    }
}
