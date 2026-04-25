namespace Craftsman.Domain.Entities;

public abstract class Entity<TId>
{
    public TId Id { get; protected init; }

    protected Entity(TId id)
    {
        Id = id;
    }
}
