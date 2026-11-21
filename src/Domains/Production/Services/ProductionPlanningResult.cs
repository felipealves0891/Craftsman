namespace Craftsman.Domain.Production.Services;

public sealed record ProductionPlanningResult(
    bool Succeeded,
    ProductionPlanningFailureReason FailureReason = ProductionPlanningFailureReason.None)
{
    public static ProductionPlanningResult Success() => new(true);

    public static ProductionPlanningResult Failed(ProductionPlanningFailureReason reason) => new(false, reason);

}
