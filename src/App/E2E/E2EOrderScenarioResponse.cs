namespace Craftsman.App.E2E;

public sealed record E2EOrderScenarioResponse(
    string Scenario,
    Guid? OrderId,
    string? ExternalOrderId,
    IReadOnlyDictionary<string, string> Metadata);
