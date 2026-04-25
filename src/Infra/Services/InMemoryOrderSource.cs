using Craftsman.Domain.Integration.Models;
using Craftsman.Domain.Integration.Services;

namespace Craftsman.Infra.Services;

public sealed class InMemoryOrderSource : IOrderSource
{
    private static readonly IReadOnlyCollection<RawOrder> Orders =
    [
        new RawOrder(
            "Simulated",
            "SIM-1001",
            "Cliente Simulado",
            "cliente.simulado@example.com",
            [new RawOrderItem("SIM-SKU-1", "Produto simulado", 1, 49.9m)]),
        new RawOrder(
            "Simulated",
            "SIM-INVALID",
            "Cliente Sem Item",
            null,
            [])
    ];

    public string SourceName => "Simulated";

    public Task<IReadOnlyCollection<RawOrder>> FetchOrdersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Orders);
    }
}
