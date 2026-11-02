using Craftsman.UI.Components.Production;
using Craftsman.UI.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Craftsman.Tests.Production;

public sealed class ProductionIndexPageTests
{
    [Fact]
    public async Task Renders_empty_calendar_date_filter_and_error_message()
    {
        var html = await RenderAsync(new Dictionary<string, object?>
        {
            [nameof(ProductionIndexPage.StatusOptions)] = new[] { new SelectOption("Planned", "Planejada", true) },
            [nameof(ProductionIndexPage.PlannedDate)] = "2031-06-15",
            [nameof(ProductionIndexPage.MonthTitle)] = "Junho 2031",
            [nameof(ProductionIndexPage.VisibleTaskCount)] = 0,
            [nameof(ProductionIndexPage.HasTasks)] = false,
            [nameof(ProductionIndexPage.LeadingBlankDays)] = 0,
            [nameof(ProductionIndexPage.RequestVerificationToken)] = "csrf-token",
            [nameof(ProductionIndexPage.ErrorMessage)] = "Falha ao avançar tarefa",
            [nameof(ProductionIndexPage.Days)] = Array.Empty<ProductionIndexPage.CalendarDay>()
        });

        Assert.Contains("Agenda de producao", html);
        Assert.Contains("Junho 2031", html);
        Assert.Contains("value=\"2031-06-15\"", html);
        Assert.Contains("selected", html);
        Assert.Contains("role=\"alert\"", html);
        Assert.Contains("Falha ao avan", html);
        Assert.Contains("Nenhuma tarefa encontrada", html);
    }

    [Fact]
    public async Task Renders_task_details_and_enables_only_allowed_actions()
    {
        var task = new ProductionIndexPage.ProductionTaskRow(
            Guid.NewGuid(),
            "Planejada",
            "planned",
            "10:00 - 11:00",
            "Manual PROD-1",
            "Bolsa",
            3,
            2,
            CanStart: true,
            CanComplete: false,
            CanCancel: false);
        var days = new[]
        {
            new ProductionIndexPage.CalendarDay(12, "calendar-day today", [task])
        };

        var html = await RenderAsync(new Dictionary<string, object?>
        {
            [nameof(ProductionIndexPage.StatusOptions)] = Array.Empty<SelectOption>(),
            [nameof(ProductionIndexPage.MonthTitle)] = "Junho 2031",
            [nameof(ProductionIndexPage.VisibleTaskCount)] = 1,
            [nameof(ProductionIndexPage.HasTasks)] = true,
            [nameof(ProductionIndexPage.LeadingBlankDays)] = 1,
            [nameof(ProductionIndexPage.RequestVerificationToken)] = "csrf-token",
            [nameof(ProductionIndexPage.Days)] = days
        });

        Assert.Contains("calendar-day-muted", html);
        Assert.Contains("calendar-day today", html);
        Assert.Contains("Planejada", html);
        Assert.Contains("10:00 - 11:00", html);
        Assert.Contains("Manual PROD-1", html);
        Assert.Contains("3 unidade(s) - 2 hora(s)", html);
        Assert.Contains("value=\"start\"", html);
        Assert.Contains("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"csrf-token\"", html);
        Assert.Contains("value=\"complete\"", html);
        Assert.Contains("value=\"cancel\"", html);
        Assert.Contains("disabled", html);
        Assert.DoesNotContain("Nenhuma tarefa encontrada", html);
    }

    private static async Task<string> RenderAsync(IDictionary<string, object?> parameters)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(serviceProvider, loggerFactory);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<ProductionIndexPage>(ParameterView.FromDictionary(parameters));
            return root.ToHtmlString();
        });
    }
}
