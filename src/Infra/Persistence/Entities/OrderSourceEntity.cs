namespace Craftsman.Infra.Persistence.Entities;

public sealed class OrderSourceEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
