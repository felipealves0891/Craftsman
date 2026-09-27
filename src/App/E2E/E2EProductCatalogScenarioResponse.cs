namespace Craftsman.App.E2E;

public sealed record E2EProductCatalogScenarioResponse(
    string Scenario,
    Guid ProductId,
    string ProductName,
    IReadOnlyCollection<E2EProductCatalogRawMaterialResponse> RawMaterials,
    IReadOnlyDictionary<string, string> Metadata);
