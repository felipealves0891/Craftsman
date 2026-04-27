using Craftsman.App.Models;
using Craftsman.Domain.Production.Entities;

namespace Craftsman.Tests.Application;

public sealed class ProductionTaskListItemViewModelTests
{
    [Theory]
    [InlineData(nameof(ProductionTaskStatus.Planned), true, false, false)]
    [InlineData(nameof(ProductionTaskStatus.InProduction), false, true, true)]
    [InlineData(nameof(ProductionTaskStatus.Completed), false, false, false)]
    [InlineData(nameof(ProductionTaskStatus.Cancelled), false, false, false)]
    public void Production_task_actions_are_enabled_only_for_valid_statuses(
        string status,
        bool canStart,
        bool canComplete,
        bool canCancel)
    {
        var viewModel = new ProductionTaskListItemViewModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            1,
            status,
            DateTimeOffset.UtcNow,
            null,
            null);

        Assert.Equal(canStart, viewModel.CanStart);
        Assert.Equal(canComplete, viewModel.CanComplete);
        Assert.Equal(canCancel, viewModel.CanCancel);
    }
}
