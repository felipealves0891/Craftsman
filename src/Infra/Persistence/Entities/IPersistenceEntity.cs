namespace Craftsman.Infra.Persistence.Entities;

public interface IPersistenceEntity<TId>
{
    TId Id { get; set; }
}
