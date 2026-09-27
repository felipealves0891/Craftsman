namespace Craftsman.App.E2E;

public sealed record E2EManualOrderScenarioResponse(
    string Scenario,
    Guid? OrderId,
    Guid? OrderItemId,
    Guid ProductId,
    string ProductName,
    string Source,
    string? ExternalOrderId,
    IReadOnlyDictionary<string, string> Metadata);
