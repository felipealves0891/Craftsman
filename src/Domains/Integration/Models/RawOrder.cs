namespace Craftsman.Domain.Integration.Models;

public sealed record RawOrder(
    string Source,
    string ExternalOrderId,
    string CustomerName,
    string? CustomerEmail,
    IReadOnlyCollection<RawOrderItem> Items);
