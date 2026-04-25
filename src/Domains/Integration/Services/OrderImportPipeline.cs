using Craftsman.Domain.Events;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Sales.Repositories;

namespace Craftsman.Domain.Integration.Services;

public sealed class OrderImportPipeline : IOrderImportPipeline
{
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly IOrderNormalizer orderNormalizer;
    private readonly IOrderRepository orderRepository;
    private readonly IReadOnlyCollection<IOrderSource> orderSources;
    private readonly IUnitOfWork unitOfWork;

    public OrderImportPipeline(
        IEnumerable<IOrderSource> orderSources,
        IOrderNormalizer orderNormalizer,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher domainEventPublisher)
    {
        this.orderSources = orderSources.ToList().AsReadOnly();
        this.orderNormalizer = orderNormalizer;
        this.orderRepository = orderRepository;
        this.unitOfWork = unitOfWork;
        this.domainEventPublisher = domainEventPublisher;
    }

    public async Task<OrderImportResult> ImportAsync(CancellationToken cancellationToken = default)
    {
        var importedCount = 0;
        var skippedCount = 0;
        var failures = new List<ImportFailure>();

        foreach (var orderSource in orderSources)
        {
            IReadOnlyCollection<Models.RawOrder> rawOrders;

            try
            {
                rawOrders = await orderSource.FetchOrdersAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                failures.Add(new ImportFailure(orderSource.SourceName, null, exception.Message));
                continue;
            }

            foreach (var rawOrder in rawOrders)
            {
                try
                {
                    var origin = new OrderOrigin(rawOrder.Source, rawOrder.ExternalOrderId);
                    var existingOrder = await orderRepository.GetByOriginAsync(origin, cancellationToken);

                    if (existingOrder is not null)
                    {
                        skippedCount++;
                        continue;
                    }

                    var order = orderNormalizer.Normalize(rawOrder);
                    await orderRepository.AddAsync(order, cancellationToken);

                    foreach (var domainEvent in order.DomainEvents)
                    {
                        await domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
                    }

                    order.ClearDomainEvents();
                    importedCount++;
                }
                catch (Exception exception)
                {
                    failures.Add(new ImportFailure(rawOrder.Source, rawOrder.ExternalOrderId, exception.Message));
                }
            }
        }

        if (importedCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new OrderImportResult(importedCount, skippedCount, failures.AsReadOnly());
    }
}
