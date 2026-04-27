using Craftsman.App.Models;
using Craftsman.Domain.Events;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Sales.Repositories;

namespace Craftsman.App.Services;

public sealed class ManualOrderService
{
    private const string ManualSource = "Manual";
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly IOrderRepository orderRepository;
    private readonly OrderProductionPlanningService productionPlanningService;
    private readonly IProductRepository productRepository;
    private readonly IUnitOfWork unitOfWork;

    public ManualOrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        OrderProductionPlanningService productionPlanningService,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher domainEventPublisher)
    {
        this.orderRepository = orderRepository;
        this.productRepository = productRepository;
        this.productionPlanningService = productionPlanningService;
        this.unitOfWork = unitOfWork;
        this.domainEventPublisher = domainEventPublisher;
    }

    public async Task<Guid> CreateAsync(ManualOrderInputModel input, CancellationToken cancellationToken = default)
    {
        var populatedItems = input.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Description) || item.Quantity > 0 || item.UnitPriceAmount > 0 || item.ProductId.HasValue)
            .ToList();

        if (populatedItems.Count == 0)
        {
            throw new InvalidOperationException("Pedido manual deve conter ao menos um item.");
        }

        var externalOrderId = string.IsNullOrWhiteSpace(input.Reference)
            ? $"MAN-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..30]
            : input.Reference.Trim();

        var existingOrder = await orderRepository.GetByOriginAsync(new OrderOrigin(ManualSource, externalOrderId), cancellationToken);
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
            new OrderOrigin(ManualSource, externalOrderId),
            new CustomerInfo(input.CustomerName, input.CustomerEmail),
            items);

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

    public async Task<IReadOnlyCollection<ProductOptionViewModel>> ListProductOptionsAsync(CancellationToken cancellationToken = default)
    {
        var products = await productRepository.ListAsync(cancellationToken);

        return products
            .Select(product => new ProductOptionViewModel(product.Id, product.Name))
            .ToList()
            .AsReadOnly();
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
}
