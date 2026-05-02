namespace Craftsman.Domain.Sales.Entities;

public sealed class OrderSource
{
    public Guid Id { get; }

    public string Name { get; }

    public DateTimeOffset CreatedAt { get; }

    public OrderSource(Guid id, string name, DateTimeOffset? createdAt = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Order source id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Order source name is required.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }
}
