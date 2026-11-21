namespace Craftsman.Domain.Production.Services;

public enum ProductionPlanningFailureReason
{
    None = 0,
    OrderNotFound = 1,
    MissingProductMapping = 2,
    InvalidProductConfiguration = 3,
    InsufficientStock = 4,
    AlreadyPlanned = 5,
    ConcurrencyConflict = 6,
    Unexpected = 7
}
