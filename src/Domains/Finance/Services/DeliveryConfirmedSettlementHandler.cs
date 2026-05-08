using Craftsman.Domain.Events;
using Craftsman.Domain.Finance.Repositories;
using Craftsman.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Craftsman.Domain.Finance.Services;

public sealed class DeliveryConfirmedSettlementHandler : IDomainEventHandler<DeliveryConfirmedEvent>
{
    private readonly ILogger<DeliveryConfirmedSettlementHandler> logger;
    private readonly ISettlementCalculator settlementCalculator;
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly IFinancialSettlementRepository settlementRepository;
    private readonly IUnitOfWork unitOfWork;

    public DeliveryConfirmedSettlementHandler(
        ILogger<DeliveryConfirmedSettlementHandler> logger,
        ISettlementCalculator settlementCalculator,
        IFinancialSettlementRepository settlementRepository,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher domainEventPublisher)
    {
        this.logger = logger;
        this.settlementCalculator = settlementCalculator;
        this.settlementRepository = settlementRepository;
        this.unitOfWork = unitOfWork;
        this.domainEventPublisher = domainEventPublisher;
    }

    public async Task HandleAsync(DeliveryConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Handling delivery confirmed event for order {OrderId}", domainEvent.OrderId);
        var existingSettlement = await settlementRepository.GetByOrderIdAsync(domainEvent.OrderId, cancellationToken);

        if (existingSettlement is not null)
        {
            logger.LogWarning("Settlement already exists for order {OrderId}, skipping calculation", domainEvent.OrderId);
            return;
        }

        logger.LogInformation("Calculating settlement for order {OrderId}", domainEvent.OrderId);
        var settlement = await settlementCalculator.CalculateAsync(domainEvent.OrderId, cancellationToken);
        logger.LogInformation("Settlement calculated for order {OrderId}", domainEvent.OrderId);
        
        await settlementRepository.AddAsync(settlement, cancellationToken);

        foreach (var settlementEvent in settlement.DomainEvents)
        {
            logger.LogInformation("Publishing domain event for order {OrderId}", domainEvent.OrderId);
            await domainEventPublisher.PublishAsync(settlementEvent, cancellationToken);
        }

        settlement.ClearDomainEvents();
        logger.LogInformation("Saving changes for order {OrderId}", domainEvent.OrderId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
