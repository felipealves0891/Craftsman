using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Sales.Entities;

namespace Craftsman.Domain.Production.Services;

public sealed class ProductionPlanner : IProductionPlanner
{
    private readonly InventoryService inventoryService;
    private readonly ProductionPlanPreparationService preparationService;
    private readonly IProductionTaskRepository? productionTaskRepository;
    private readonly ProductionScheduleOptions scheduleOptions;

    public ProductionPlanner(
        IProductRepository productRepository,
        InventoryService inventoryService,
        IProductionTaskRepository? productionTaskRepository = null,
        ProductionScheduleOptions? scheduleOptions = null,
        ProductionPlanPreparationService? preparationService = null)
    {
        this.preparationService = preparationService ?? new ProductionPlanPreparationService(productRepository);
        this.inventoryService = inventoryService;
        this.productionTaskRepository = productionTaskRepository;
        this.scheduleOptions = scheduleOptions ?? new ProductionScheduleOptions();
    }

    public async Task<IReadOnlyCollection<ProductionTask>> PlanAsync(Order order, CancellationToken cancellationToken = default)
    {
        var productionTasks = new List<ProductionTask>();
        var existingTasks = productionTaskRepository is null
            ? []
            : (await productionTaskRepository.ListAsync(cancellationToken: cancellationToken))
                .Where(task => task.Status != ProductionTaskStatus.Cancelled)
                .ToList();

        var preparation = await preparationService.PrepareAsync(order, cancellationToken);
        if (!preparation.IsValid)
        {
            throw new InvalidOperationException(preparation.FailureReason switch
            {
                ProductionPlanningFailureReason.MissingProductMapping => "Production cannot be planned without an internal product mapping.",
                _ => "Production cannot be planned without an active product and valid bill of materials."
            });
        }

        var detailedAvailability = await inventoryService.GetAvailabilityAsync(preparation.Requirements, cancellationToken);

        if (detailedAvailability.Any(material => !material.IsConfigured))
        {
            throw new InvalidOperationException("Production cannot be planned with missing or inactive raw material configuration.");
        }

        var availability = await inventoryService.CheckAvailabilityAsync(preparation.Requirements, cancellationToken);

        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException($"Production cannot be planned with insufficient material: {string.Join(", ", availability.MissingMaterials)}.");
        }

        foreach (var plannedItem in preparation.PlannedItems)
        {
            var productionDurationHours = plannedItem.Product.ProductionDurationHours * plannedItem.OrderItem.Quantity;
            var plannedWindow = PlanWindow(order, productionDurationHours, existingTasks.Concat(productionTasks));
            var productionTask = new ProductionTask(
                Guid.NewGuid(),
                order.Id,
                plannedItem.OrderItem.Id,
                plannedItem.Product.Id,
                plannedItem.OrderItem.Quantity,
                productionDurationHours,
                plannedStartAt: plannedWindow.StartAt,
                plannedAt: plannedWindow.EndAt);
            await inventoryService.ConsumeForProductionAsync(plannedItem.Requirements, productionTask.Id, cancellationToken);
            productionTasks.Add(productionTask);
        }

        return productionTasks.AsReadOnly();
    }

    private ProductionWindow PlanWindow(Order order, int productionDurationHours, IEnumerable<ProductionTask> reservedTasks)
    {
        var shippingDate = order.ShippingDate ?? DateOnly.FromDateTime(order.CreatedAt.UtcDateTime);
        var latestEnd = WorkdayEnd(shippingDate);
        var candidateEnd = latestEnd;
        var reserved = reservedTasks
            .Where(task => task.Status != ProductionTaskStatus.Cancelled)
            .OrderByDescending(task => task.PlannedStartAt)
            .ToList();

        while (true)
        {
            candidateEnd = ClampToWorkingTime(candidateEnd);
            var candidateStart = SubtractWorkingHours(candidateEnd, productionDurationHours);
            var overlap = reserved.FirstOrDefault(task => Overlaps(candidateStart, candidateEnd, task.PlannedStartAt, task.PlannedAt));

            if (overlap is null)
            {
                return new ProductionWindow(candidateStart, candidateEnd);
            }

            candidateEnd = overlap.PlannedStartAt;
        }
    }

    private DateTimeOffset SubtractWorkingHours(DateTimeOffset endAt, int hours)
    {
        var remaining = TimeSpan.FromHours(hours);
        var cursor = ClampToWorkingTime(endAt);

        while (remaining > TimeSpan.Zero)
        {
            var date = DateOnly.FromDateTime(cursor.UtcDateTime);
            var dayStart = WorkdayStart(date);
            var dayEnd = WorkdayEnd(date);

            if (cursor <= dayStart)
            {
                cursor = WorkdayEnd(date.AddDays(-1));
                continue;
            }

            if (cursor > dayEnd)
            {
                cursor = dayEnd;
            }

            var available = cursor - dayStart;
            if (available >= remaining)
            {
                return cursor - remaining;
            }

            remaining -= available;
            cursor = WorkdayEnd(date.AddDays(-1));
        }

        return cursor;
    }

    private DateTimeOffset ClampToWorkingTime(DateTimeOffset value)
    {
        var date = DateOnly.FromDateTime(value.UtcDateTime);
        var dayStart = WorkdayStart(date);
        var dayEnd = WorkdayEnd(date);

        if (value > dayEnd)
        {
            return dayEnd;
        }

        if (value <= dayStart)
        {
            return WorkdayEnd(date.AddDays(-1));
        }

        return value;
    }

    private DateTimeOffset WorkdayStart(DateOnly date)
    {
        var time = TimeOnly.FromTimeSpan(TimeSpan.FromHours(scheduleOptions.EffectiveWorkdayStartHour));
        return new DateTimeOffset(date.ToDateTime(time, DateTimeKind.Utc), TimeSpan.Zero);
    }

    private DateTimeOffset WorkdayEnd(DateOnly date)
    {
        return WorkdayStart(date).AddHours(scheduleOptions.EffectiveWorkingHoursPerDay);
    }

    private static bool Overlaps(DateTimeOffset startAt, DateTimeOffset endAt, DateTimeOffset reservedStartAt, DateTimeOffset reservedEndAt)
    {
        return startAt < reservedEndAt && reservedStartAt < endAt;
    }

    private sealed record ProductionWindow(DateTimeOffset StartAt, DateTimeOffset EndAt);
}
