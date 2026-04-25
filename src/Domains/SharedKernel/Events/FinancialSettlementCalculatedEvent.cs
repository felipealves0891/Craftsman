namespace Craftsman.Domain.Events;

public sealed record FinancialSettlementCalculatedEvent(Guid SettlementId, Guid OrderId, decimal Margin) : DomainEvent;
