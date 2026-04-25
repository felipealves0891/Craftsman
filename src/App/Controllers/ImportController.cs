using Craftsman.App.Models;
using Craftsman.Domain.Integration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.Controllers;

public sealed class ImportController : Controller
{
    private readonly IOrderImportPipeline orderImportPipeline;

    public ImportController(IOrderImportPipeline orderImportPipeline)
    {
        this.orderImportPipeline = orderImportPipeline;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ImportResultViewModel(0, 0, []));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        var result = await orderImportPipeline.ImportAsync(cancellationToken);

        return View("Index", new ImportResultViewModel(
            result.ImportedCount,
            result.SkippedCount,
            result.Failures.Select(failure => $"{failure.Source}/{failure.ExternalOrderId}: {failure.Reason}").ToList().AsReadOnly()));
    }
}
