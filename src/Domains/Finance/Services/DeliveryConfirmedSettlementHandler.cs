using Craftsman.Domain.Events;
using Craftsman.Domain.Finance.Repositories;
using Craftsman.Domain.Repositories;

namespace Craftsman.Domain.Finance.Services;

public sealed class DeliveryConfirmedSettlementHandler : IDomainEventHandler<DeliveryConfirmedEvent>
{
    private readonly ISettlementCalculator settlementCalculator;
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly IFinancialSettlementRepository settlementRepository;
    private readonly IUnitOfWork unitOfWork;

    public DeliveryConfirmedSettlementHandler(
        ISettlementCalculator settlementCalculator,
        IFinancialSettlementRepository settlementRepository,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher domainEventPublisher)
    {
        this.settlementCalculator = settlementCalculator;
        this.settlementRepository = settlementRepository;
        this.unitOfWork = unitOfWork;
        this.domainEventPublisher = domainEventPublisher;
    }

    public async Task HandleAsync(DeliveryConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var existingSettlement = await settlementRepository.GetByOrderIdAsync(domainEvent.OrderId, cancellationToken);

        if (existingSettlement is not null)
        {
            return;
        }

        var settlement = await settlementCalculator.CalculateAsync(domainEvent.OrderId, cancellationToken);
        await settlementRepository.AddAsync(settlement, cancellationToken);

        foreach (var settlementEvent in settlement.DomainEvents)
        {
            await domainEventPublisher.PublishAsync(settlementEvent, cancellationToken);
        }

        settlement.ClearDomainEvents();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
