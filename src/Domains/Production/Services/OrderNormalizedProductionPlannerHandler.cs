using Craftsman.Domain.Events;

namespace Craftsman.Domain.Production.Services;

public sealed class OrderNormalizedProductionPlannerHandler : IDomainEventHandler<OrderNormalizedEvent>
{
    private readonly OrderProductionPlanningService productionPlanningService;

    public OrderNormalizedProductionPlannerHandler(OrderProductionPlanningService productionPlanningService)
    {
        this.productionPlanningService = productionPlanningService;
    }

    public Task HandleAsync(OrderNormalizedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        return productionPlanningService.TryPlanAsync(domainEvent.OrderId, cancellationToken);
    }
}
