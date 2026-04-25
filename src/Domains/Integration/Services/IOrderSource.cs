using Craftsman.Domain.Integration.Models;

namespace Craftsman.Domain.Integration.Services;

public interface IOrderSource
{
    string SourceName { get; }

    Task<IReadOnlyCollection<RawOrder>> FetchOrdersAsync(CancellationToken cancellationToken = default);
}
