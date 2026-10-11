namespace Craftsman.Infra.Persistence.Entities;

public sealed class CustomerEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<OrderEntity> Orders { get; set; } = [];
}
