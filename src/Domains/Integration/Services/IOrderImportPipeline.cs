namespace Craftsman.Domain.Integration.Services;

public interface IOrderImportPipeline
{
    Task<OrderImportResult> ImportAsync(CancellationToken cancellationToken = default);
}
