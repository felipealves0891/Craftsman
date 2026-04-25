namespace Craftsman.Domain.Events;

public sealed record OrderNormalizedEvent(Guid OrderId, string Source, string ExternalOrderId) : DomainEvent;
