using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Sales.Entities;

namespace Craftsman.Domain.Production.Services;

public sealed class ProductionPlanner : IProductionPlanner
{
    private readonly InventoryService inventoryService;
    private readonly IProductRepository productRepository;
    private readonly IProductionTaskRepository? productionTaskRepository;
    private readonly ProductionScheduleOptions scheduleOptions;

    public ProductionPlanner(
        IProductRepository productRepository,
        InventoryService inventoryService,
        IProductionTaskRepository? productionTaskRepository = null,
        ProductionScheduleOptions? scheduleOptions = null)
    {
        this.productRepository = productRepository;
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

        foreach (var item in order.Items)
        {
            if (item.ProductId is null)
            {
                throw new InvalidOperationException("Production cannot be planned without an internal product mapping.");
            }

            var product = await productRepository.GetByIdAsync(item.ProductId.Value, cancellationToken);

            if (product is null || !product.CanBePlannedForProduction)
            {
                throw new InvalidOperationException("Production cannot be planned without an active product and valid bill of materials.");
            }

            var requirements = BuildRequirements(product, item.Quantity);
            var availability = await inventoryService.CheckAvailabilityAsync(requirements, cancellationToken);

            if (!availability.IsAvailable)
            {
                throw new InvalidOperationException($"Production cannot be planned with insufficient material: {string.Join(", ", availability.MissingMaterials)}.");
            }

            var productionDurationHours = product.ProductionDurationHours * item.Quantity;
            var plannedWindow = PlanWindow(order, productionDurationHours, existingTasks.Concat(productionTasks));
            var productionTask = new ProductionTask(
                Guid.NewGuid(),
                order.Id,
                item.Id,
                product.Id,
                item.Quantity,
                productionDurationHours,
                plannedStartAt: plannedWindow.StartAt,
                plannedAt: plannedWindow.EndAt);
            await inventoryService.ConsumeForProductionAsync(requirements, productionTask.Id, cancellationToken);
            productionTasks.Add(productionTask);
        }

        return productionTasks.AsReadOnly();
    }

    private static IReadOnlyCollection<MaterialRequirement> BuildRequirements(Product product, int quantity)
    {
        return product.BillOfMaterials
            .Select(item => new MaterialRequirement(item.RawMaterialId, item.QuantityPerUnit * quantity))
            .ToList()
            .AsReadOnly();
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
