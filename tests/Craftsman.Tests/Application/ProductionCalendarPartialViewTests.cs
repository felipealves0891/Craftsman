using System.Globalization;
using System.Diagnostics;
using Craftsman.App.Controllers;
using Craftsman.App.Models;
using Craftsman.Domain.Production.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Craftsman.Tests.Application;

public sealed class ProductionCalendarPartialViewTests
{
    [Fact]
    public async Task Partial_uses_current_month_and_empty_state_when_no_date_or_tasks_exist()
    {
        var rendered = await RenderPartialAsync([], selectedDate: null, new DateTime(2031, 6, 15));

        Assert.Contains("data-current-month=\"2031-06\"", rendered);
        Assert.Contains("Junho 2031", rendered);
        Assert.Contains("Nenhuma tarefa encontrada", rendered);
        Assert.Contains("data-auto-update=\"true\"", rendered);
    }

    [Fact]
    public async Task Partial_preserves_selected_date_and_renders_matching_task()
    {
        var taskId = Guid.NewGuid();
        var tasks = new[]
        {
            new ProductionTaskListItemViewModel(
                taskId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                2,
                3,
                nameof(ProductionTaskStatus.Planned),
                new DateTimeOffset(2031, 5, 12, 10, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2031, 5, 12, 13, 0, 0, TimeSpan.Zero),
                null,
                null,
                "Manual PROD-12",
                "Bolsa",
                "PROD-12")
        };

        var rendered = await RenderPartialAsync(tasks, new DateOnly(2031, 5, 12), new DateTime(2031, 6, 15));

        Assert.Contains("data-current-month=\"2031-05\"", rendered);
        Assert.Contains("data-auto-update=\"false\"", rendered);
        Assert.Contains("value=\"2031-05-12\"", rendered);
        Assert.Contains("Manual PROD-12", rendered);
        Assert.Contains("Bolsa", rendered);
        Assert.Contains("2 unidade(s) - 3 hora(s)", rendered);
        Assert.Contains("value=\"start\"", rendered);
        Assert.Contains("value=\"cancel\"", rendered);
        Assert.Contains(taskId.ToString(), rendered);
        Assert.DoesNotContain("Nenhuma tarefa encontrada", rendered);
    }

    [Fact]
    public async Task Partial_uses_selected_date_when_reference_date_is_not_provided()
    {
        var rendered = await RenderPartialAsync([], new DateOnly(2032, 2, 9), new DateTime(2031, 6, 15),
            includeReferenceDate: false, includeToday: false, status: null);

        Assert.Contains("data-current-month=\"2032-02\"", rendered);
        Assert.Contains("value=\"2032-02-09\"", rendered);
        Assert.Contains("data-auto-update=\"false\"", rendered);
    }

    [Fact]
    public async Task Partial_labels_start_middle_and_end_days_for_a_multiday_task()
    {
        var plannedStart = new DateTimeOffset(2031, 6, 1, 8, 0, 0, TimeSpan.Zero);
        var plannedEnd = new DateTimeOffset(2031, 6, 3, 17, 0, 0, TimeSpan.Zero);
        var task = new ProductionTaskListItemViewModel(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            1, 3, nameof(ProductionTaskStatus.Planned),
            plannedStart,
            plannedEnd,
            null, null, "Manual PROD-13", "Bolsa", "PROD-13");

        var rendered = await RenderPartialAsync([task], null, new DateTime(2031, 6, 15));

        Assert.Contains($"Inicio {plannedStart.ToLocalTime():HH\\:mm}", rendered);
        Assert.Contains("Em producao", rendered);
        Assert.Contains($"Fim {plannedEnd.ToLocalTime():HH\\:mm}", rendered);
        Assert.Contains("data-auto-update=\"true\"", rendered);
    }

    private static async Task<string> RenderPartialAsync(
        IReadOnlyCollection<ProductionTaskListItemViewModel> tasks,
        DateOnly? selectedDate,
        DateTime referenceDate,
        bool includeReferenceDate = true,
        bool includeToday = true,
        ProductionTaskStatus? status = ProductionTaskStatus.Planned)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        var diagnosticListener = new DiagnosticListener("Craftsman.Tests");
        services.AddSingleton(diagnosticListener);
        services.AddSingleton<DiagnosticSource>(diagnosticListener);
        services.AddRouting();
        services.AddAntiforgery();
        services.AddRazorComponents();
        services.AddControllersWithViews().AddApplicationPart(typeof(ProductionController).Assembly);
        await using var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        var routeData = new RouteData();
        routeData.Routers.Add(new TestRouter());
        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
        var viewEngine = provider.GetRequiredService<IRazorViewEngine>();
        var viewResult = viewEngine.GetView(
            executingFilePath: null,
            viewPath: "/Views/Production/_Calendar.cshtml",
            isMainPage: false);

        Assert.True(viewResult.Success, string.Join(Environment.NewLine, viewResult.SearchedLocations ?? []));

        var viewData = new ViewDataDictionary<IReadOnlyCollection<ProductionTaskListItemViewModel>>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary())
        {
            Model = tasks
        };
        viewData["Status"] = status;
        viewData["PlannedDate"] = selectedDate;
        if (includeReferenceDate)
        {
            viewData["ReferenceDate"] = selectedDate?.ToDateTime(TimeOnly.MinValue) ?? referenceDate;
        }
        if (includeToday)
        {
            viewData["Today"] = DateOnly.FromDateTime(referenceDate);
        }

        var tempData = new TempDataDictionary(httpContext, new EmptyTempDataProvider());
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewData,
            tempData,
            writer,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
    }

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class TestRouter : IRouter
    {
        public VirtualPathData? GetVirtualPath(VirtualPathContext context)
        {
            var controller = context.Values.TryGetValue("controller", out var controllerValue) ? controllerValue : "";
            var action = context.Values.TryGetValue("action", out var actionValue) ? actionValue : "";
            return new VirtualPathData(this, $"/{controller}/{action}");
        }

        public Task RouteAsync(RouteContext context) => Task.CompletedTask;
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Craftsman.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } = "Development";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
