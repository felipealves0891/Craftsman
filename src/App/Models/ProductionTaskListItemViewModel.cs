namespace Craftsman.App.Models;

public sealed record ProductionTaskListItemViewModel(
    Guid Id,
    Guid OrderId,
    Guid OrderItemId,
    Guid ProductId,
    int Quantity,
    string Status,
    DateTimeOffset PlannedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);
