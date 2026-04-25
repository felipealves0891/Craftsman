namespace Craftsman.Domain.Sales.ObjectValues;

public sealed record OrderOrigin
{
    public string Source { get; }

    public string ExternalOrderId { get; }

    public OrderOrigin(string source, string externalOrderId)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source is required.", nameof(source));
        }

        if (string.IsNullOrWhiteSpace(externalOrderId))
        {
            throw new ArgumentException("External order id is required.", nameof(externalOrderId));
        }

        Source = source.Trim();
        ExternalOrderId = externalOrderId.Trim();
    }
}
