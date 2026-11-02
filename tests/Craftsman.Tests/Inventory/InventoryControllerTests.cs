using Craftsman.App.Controllers;
using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Repositories;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Shipping.Repositories;
using Craftsman.Domain.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Inventory;

public sealed class InventoryControllerTests
{
    [Fact]
    public async Task Inventory_read_actions_return_views_for_balances_history_and_materials()
    {
        await using var dbContext = CreateDbContext();
        var material = await SeedMaterialAsync(dbContext, 4);
        var controller = CreateController(dbContext);

        Assert.NotNull((await controller.Index(CancellationToken.None) as ViewResult)?.Model);
        Assert.IsType<ViewResult>(controller.CreateMaterial());
        Assert.IsType<NotFoundResult>(await controller.EditMaterial(Guid.NewGuid(), CancellationToken.None));
        Assert.IsType<ViewResult>(await controller.EditMaterial(material.Id, CancellationToken.None));
        Assert.NotNull((await controller.Balances(CancellationToken.None) as ViewResult)?.Model);
        Assert.NotNull((await controller.History(material.Id, CancellationToken.None) as ViewResult)?.Model);
    }

    [Fact]
    public async Task Edit_material_post_returns_validation_view_redirects_or_shows_domain_error()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);
        var input = new RawMaterialInputModel { Name = "Tecido", UnitOfMeasure = "m", Status = RawMaterialStatus.Active };
        controller.ModelState.AddModelError(nameof(RawMaterialInputModel.Name), "required");

        Assert.Same(input, Assert.IsType<ViewResult>(await controller.EditMaterial(input, CancellationToken.None)).Model);

        controller.ModelState.Clear();
        var redirect = Assert.IsType<RedirectToActionResult>(await controller.EditMaterial(input, CancellationToken.None));
        Assert.Equal(nameof(InventoryController.Index), redirect.ActionName);

        var invalidDomainInput = new RawMaterialInputModel { Name = " ", UnitOfMeasure = "m", Status = RawMaterialStatus.Active };
        var errorView = Assert.IsType<ViewResult>(await controller.EditMaterial(invalidDomainInput, CancellationToken.None));
        Assert.Contains(controller.ModelState.Values.SelectMany(value => value.Errors), error => error.ErrorMessage.Contains("name", StringComparison.OrdinalIgnoreCase));
        Assert.Same(invalidDomainInput, errorView.Model);
    }

    [Fact]
    public async Task Movements_get_and_post_populate_lists_and_report_success_or_validation_error()
    {
        await using var dbContext = CreateDbContext();
        var material = await SeedMaterialAsync(dbContext, 5);
        var controller = CreateController(dbContext);

        var getView = Assert.IsType<ViewResult>(await controller.Movements(CancellationToken.None));
        Assert.IsType<StockMovementInputModel>(getView.Model);
        Assert.NotNull(controller.ViewBag.RawMaterials);
        Assert.NotNull(controller.ViewBag.CriticalStockAlerts);
        Assert.NotNull(controller.ViewBag.Movements);

        var successInput = new StockMovementInputModel
        {
            RawMaterialId = material.Id,
            Type = StockMovementType.Inbound,
            Quantity = 2,
            UnitCostAmount = 3
        };
        var success = Assert.IsType<RedirectToActionResult>(await controller.Movements(successInput, CancellationToken.None));
        Assert.Equal(nameof(InventoryController.Movements), success.ActionName);
        Assert.Equal("Movimento de estoque registrado.", controller.TempData["Success"]);

        var invalidInput = new StockMovementInputModel
        {
            RawMaterialId = material.Id,
            Type = StockMovementType.Outbound,
            Quantity = 0,
            Reason = "Saída manual"
        };
        var error = await controller.Movements(invalidInput, CancellationToken.None);
        Assert.IsType<ViewResult>(error);
        Assert.Contains(controller.ModelState.Values.SelectMany(value => value.Errors), item => item.ErrorMessage.Contains("Quantidade", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(controller.ViewBag.RawMaterials);
    }

    private static InventoryController CreateController(AppDbContext dbContext)
    {
        var rawMaterials = new RawMaterialRepository(dbContext);
        var movements = new StockMovementRepository(dbContext);
        var orders = new OrderRepository(dbContext);
        var products = new ProductRepository(dbContext);
        var service = new InventoryAppService(rawMaterials, movements, new UnitOfWork(dbContext));
        var alerts = new StockAlertAppService(rawMaterials, movements, orders, products);
        return new InventoryController(service, alerts)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider())
        };
    }

    private static async Task<RawMaterial> SeedMaterialAsync(AppDbContext dbContext, decimal openingStock)
    {
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m", minimumStockLevel: 8, criticalStockLevel: 3);
        var materials = new RawMaterialRepository(dbContext);
        var movements = new StockMovementRepository(dbContext);
        await materials.AddAsync(material);
        await movements.AddAsync(new StockMovement(Guid.NewGuid(), material.Id, StockMovementType.Inbound, openingStock, "Saldo inicial", null));
        await dbContext.SaveChangesAsync();
        return material;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
