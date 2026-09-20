using Craftsman.App.Models;
using Craftsman.Domain;
using Craftsman.Domain.Events;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Repositories;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Shipping.Repositories;

namespace Craftsman.App.Services;

public sealed class ManualOrderService
{
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly IOrderRepository orderRepository;
    private readonly IOrderSourceCatalogRepository orderSourceCatalogRepository;
    private readonly OrderProductionPlanningService productionPlanningService;
    private readonly IProductionTaskRepository productionTaskRepository;
    private readonly IProductRepository productRepository;
    private readonly IShipmentRepository shipmentRepository;
    private readonly IStockMovementRepository stockMovementRepository;
    private readonly IUnitOfWork unitOfWork;

    public ManualOrderService(
        IOrderRepository orderRepository,
        IOrderSourceCatalogRepository orderSourceCatalogRepository,
        IProductRepository productRepository,
        IProductionTaskRepository productionTaskRepository,
        IStockMovementRepository stockMovementRepository,
        IShipmentRepository shipmentRepository,
        OrderProductionPlanningService productionPlanningService,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher domainEventPublisher)
    {
        this.orderRepository = orderRepository;
        this.orderSourceCatalogRepository = orderSourceCatalogRepository;
        this.productRepository = productRepository;
        this.productionTaskRepository = productionTaskRepository;
        this.stockMovementRepository = stockMovementRepository;
        this.shipmentRepository = shipmentRepository;
        this.productionPlanningService = productionPlanningService;
        this.unitOfWork = unitOfWork;
        this.domainEventPublisher = domainEventPublisher;
    }

    public async Task<ManualOrderInputModel?> GetInputAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        return new ManualOrderInputModel
        {
            Id = order.Id,
            Source = order.Origin.Source,
            Reference = order.Origin.ExternalOrderId,
            ShippingDate = order.ShippingDate,
            Items = order.Items.Select(item => new ManualOrderItemInputModel
            {
                ExternalItemId = item.ExternalItemId,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPriceAmount = item.UnitPrice.Amount,
                ProductId = item.ProductId
            }).ToList()
        };
    }

    public async Task<Guid> CreateAsync(ManualOrderInputModel input, LogData log, CancellationToken cancellationToken = default)
    {
        var data = log.AddStep($"{nameof(ManualOrderService)}.{nameof(CreateAsync)}");

        var populatedItems = input.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Description) || item.Quantity > 0 || item.UnitPriceAmount > 0 || item.ProductId.HasValue)
            .ToList();
        
        data["populatedItems.Count"] = populatedItems.Count;
        if (populatedItems.Count == 0)
        {
            throw new InvalidOperationException("Novo pedido deve conter ao menos um item.");
        }

        var externalOrderId = string.IsNullOrWhiteSpace(input.Reference)
            ? $"MAN-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..30]
            : input.Reference.Trim();

        data["externalOrderId"] = externalOrderId;

        if (!input.ShippingDate.HasValue)
        {
            throw new InvalidOperationException("Data de envio e obrigatoria.");
        }

        if (input.ShippingDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Data de envio deve ser futura em UTC.");
        }

        var source = await orderSourceCatalogRepository.GetByNameAsync(input.Source, cancellationToken)
            ?? throw new InvalidOperationException("Origem nao encontrada.");
        
        var existingOrder = await orderRepository.GetByOriginAsync(new OrderOrigin(source.Name, externalOrderId), cancellationToken);
        if (existingOrder is not null)
        {
            throw new InvalidOperationException("Referencia manual ja existe.");
        }

        var line = 0;
        var items = new List<OrderItem>();
        foreach (var inputItem in populatedItems)
        {
            line++;
            var item = new OrderItem(
                Guid.NewGuid(),
                string.IsNullOrWhiteSpace(inputItem.ExternalItemId) ? $"{externalOrderId}-{line}" : inputItem.ExternalItemId,
                inputItem.Description,
                inputItem.Quantity,
                new Money(inputItem.UnitPriceAmount, "BRL"),
                inputItem.ProductId);

            items.Add(item);
        }

        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin(source.Name, externalOrderId),
            items,
            shippingDate: input.ShippingDate);

        await orderRepository.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in order.DomainEvents)
        {
            await domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
        }

        order.ClearDomainEvents();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await productionPlanningService.TryPlanAsync(order.Id, cancellationToken);

        return order.Id;
    }

    public async Task UpdateAsync(Guid orderId, ManualOrderInputModel input, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException("Pedido nao encontrado.");

        await EnsureOrderCanChangeAsync(orderId, cancellationToken);

        var source = await ResolveSourceAsync(input.Source, cancellationToken);
        var externalOrderId = ResolveExternalOrderId(input.Reference);

        var existingOrder = await orderRepository.GetByOriginAsync(new OrderOrigin(source.Name, externalOrderId), cancellationToken);
        if (existingOrder is not null && existingOrder.Id != orderId)
        {
            throw new InvalidOperationException("Referencia manual ja existe.");
        }

        var items = await BuildOrderItemsAsync(input, externalOrderId, cancellationToken, order.Items);
        var shippingDate = ResolveShippingDate(input.ShippingDate);

        await ReversePlannedProductionAsync(orderId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        order.Replace(new OrderOrigin(source.Name, externalOrderId), items, shippingDate);
        if (order.Status == OrderStatus.ReadyForProduction)
        {
            order.MarkNormalized();
        }

        await orderRepository.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await productionPlanningService.TryPlanAsync(order.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        _ = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException("Pedido nao encontrado.");

        await EnsureOrderCanChangeAsync(orderId, cancellationToken);
        await ReversePlannedProductionAsync(orderId, cancellationToken);
        await orderRepository.DeleteAsync(orderId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductOptionViewModel>> ListProductOptionsAsync(CancellationToken cancellationToken = default)
    {
        var products = await productRepository.ListAsync(cancellationToken);

        return products
            .Select(product => new ProductOptionViewModel(product.Id, product.Name))
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyCollection<string>> ListOrderSourcesAsync(CancellationToken cancellationToken = default)
    {
        var sources = await orderSourceCatalogRepository.ListAsync(cancellationToken);
        return sources.Select(source => source.Name).ToList().AsReadOnly();
    }

    public async Task CreateOrderSourceAsync(OrderSourceInputModel input, CancellationToken cancellationToken = default)
    {
        var existingSource = await orderSourceCatalogRepository.GetByNameAsync(input.Name, cancellationToken);
        if (existingSource is not null)
        {
            throw new InvalidOperationException("Origem ja existe.");
        }

        await orderSourceCatalogRepository.AddAsync(new OrderSource(Guid.NewGuid(), input.Name), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task LinkProductAsync(Guid orderId, Guid itemId, Guid productId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException("Pedido nao encontrado.");

        _ = await productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new InvalidOperationException("Produto nao encontrado.");

        var item = order.Items.FirstOrDefault(orderItem => orderItem.Id == itemId)
            ?? throw new InvalidOperationException("Item do pedido nao encontrado.");

        item.AssignProduct(productId);
        await orderRepository.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await productionPlanningService.TryPlanAsync(orderId, cancellationToken);
    }

    private async Task<OrderSource> ResolveSourceAsync(string sourceName, CancellationToken cancellationToken)
    {
        return await orderSourceCatalogRepository.GetByNameAsync(sourceName, cancellationToken)
            ?? throw new InvalidOperationException("Origem nao encontrada.");
    }

    private static string ResolveExternalOrderId(string? reference)
    {
        return string.IsNullOrWhiteSpace(reference)
            ? $"MAN-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..30]
            : reference.Trim();
    }

    private static DateOnly ResolveShippingDate(DateOnly? shippingDate)
    {
        if (!shippingDate.HasValue)
        {
            throw new InvalidOperationException("Data de envio e obrigatoria.");
        }

        if (shippingDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Data de envio deve ser futura em UTC.");
        }

        return shippingDate.Value;
    }

    private async Task<List<OrderItem>> BuildOrderItemsAsync(
        ManualOrderInputModel input,
        string externalOrderId,
        CancellationToken cancellationToken,
        IReadOnlyCollection<OrderItem>? existingItems = null)
    {
        var populatedItems = input.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Description) || item.Quantity > 0 || item.UnitPriceAmount > 0 || item.ProductId.HasValue)
            .ToList();

        if (populatedItems.Count == 0)
        {
            throw new InvalidOperationException("Novo pedido deve conter ao menos um item.");
        }

        var line = 0;
        var items = new List<OrderItem>();
        var existingItemsByPosition = existingItems?.ToList() ?? [];
        foreach (var inputItem in populatedItems)
        {
            line++;
            if (inputItem.ProductId.HasValue)
            {
                _ = await productRepository.GetByIdAsync(inputItem.ProductId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Produto nao encontrado.");
            }

            items.Add(new OrderItem(
                line <= existingItemsByPosition.Count ? existingItemsByPosition[line - 1].Id : Guid.NewGuid(),
                string.IsNullOrWhiteSpace(inputItem.ExternalItemId) ? $"{externalOrderId}-{line}" : inputItem.ExternalItemId,
                inputItem.Description,
                inputItem.Quantity,
                new Money(inputItem.UnitPriceAmount, "BRL"),
                inputItem.ProductId));
        }

        return items;
    }

    private async Task EnsureOrderCanChangeAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var productionTasks = await productionTaskRepository.ListByOrderAsync(orderId, cancellationToken);
        if (productionTasks.Any(task => task.Status is ProductionTaskStatus.InProduction or ProductionTaskStatus.Completed))
        {
            throw new InvalidOperationException("Pedido nao pode ser alterado pois a producao ja foi iniciada.");
        }

        var shipments = await shipmentRepository.ListAsync(cancellationToken: cancellationToken);
        if (shipments.Any(shipment => shipment.OrderId == orderId))
        {
            throw new InvalidOperationException("Pedido nao pode ser alterado pois possui envio vinculado.");
        }
    }

    private async Task ReversePlannedProductionAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var productionTasks = await productionTaskRepository.ListByOrderAsync(orderId, cancellationToken);
        foreach (var productionTask in productionTasks.Where(task => task.Status != ProductionTaskStatus.Cancelled))
        {
            if (productionTask.Status != ProductionTaskStatus.Planned)
            {
                throw new InvalidOperationException("Pedido nao pode ser alterado pois a producao ja foi iniciada.");
            }

            var movements = await stockMovementRepository.ListByBusinessReferenceAsync(productionTask.Id.ToString(), cancellationToken);
            foreach (var movement in movements.Where(movement => movement.Type == StockMovementType.Outbound))
            {
                await stockMovementRepository.AddAsync(
                    new StockMovement(
                        Guid.NewGuid(),
                        movement.RawMaterialId,
                        StockMovementType.Inbound,
                        movement.Quantity,
                        "Production consumption reversal",
                        $"reversal:{movement.Id}",
                        unitCostAmount: movement.UnitCostAmount),
                    cancellationToken);
            }

            await productionTaskRepository.DeleteAsync(productionTask.Id, cancellationToken);
        }
    }
}
