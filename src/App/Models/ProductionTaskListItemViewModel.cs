using Craftsman.Domain.Production.Entities;

namespace Craftsman.App.Models;

public sealed record ProductionTaskListItemViewModel(
    Guid Id,
    Guid OrderId,
    Guid OrderItemId,
    Guid ProductId,
    int Quantity,
    int ProductionDurationDays,
    string Status,
    DateTimeOffset PlannedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string OrderReference = "",
    string ItemDescription = "",
    string ExternalOrderId = "")
{
    public bool CanStart => Status == nameof(ProductionTaskStatus.Planned);

    public bool CanComplete => Status == nameof(ProductionTaskStatus.InProduction);

    public bool CanCancel => Status == nameof(ProductionTaskStatus.InProduction);
}
