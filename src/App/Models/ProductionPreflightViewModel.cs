using System.Diagnostics.CodeAnalysis;

namespace Craftsman.App.Models;

[ExcludeFromCodeCoverage]
public sealed record ProductionPreflightViewModel(
    bool CanConfirm,
    string? FailureMessage,
    IReadOnlyCollection<ProductionPreflightMaterialViewModel> Materials);

[ExcludeFromCodeCoverage]
public sealed record ProductionPreflightMaterialViewModel(
    Guid RawMaterialId,
    string Name,
    string UnitOfMeasure,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    decimal MissingQuantity,
    bool IsConfigured,
    bool IsAvailable);
