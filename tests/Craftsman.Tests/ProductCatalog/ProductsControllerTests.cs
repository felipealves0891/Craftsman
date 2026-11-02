using Craftsman.App.Controllers;
using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Craftsman.Tests.ProductCatalog;

public sealed class ProductsControllerTests
{
    [Fact]
    public async Task Product_actions_cover_list_create_edit_validation_and_save_paths()
    {
        await using var dbContext = CreateDbContext();
        var product = new Product(Guid.NewGuid(), "Caneca");
        await new ProductRepository(dbContext).AddAsync(product);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        Assert.NotNull((await controller.Index(CancellationToken.None) as ViewResult)?.Model);
        Assert.IsType<ViewResult>(controller.Create());
        Assert.IsType<NotFoundResult>(await controller.Edit(Guid.NewGuid(), CancellationToken.None));
        Assert.IsType<ViewResult>(await controller.Edit(product.Id, CancellationToken.None));

        var newInput = new ProductInputModel { Name = "Bolsa", Status = ProductStatus.Active };
        controller.ModelState.AddModelError(nameof(ProductInputModel.Name), "required");
        Assert.Same(newInput, Assert.IsType<ViewResult>(await controller.Edit(newInput, CancellationToken.None)).Model);

        controller.ModelState.Clear();
        Assert.Equal(nameof(ProductsController.Index), Assert.IsType<RedirectToActionResult>(
            await controller.Edit(newInput, CancellationToken.None)).ActionName);

        controller.ModelState.Clear();
        var invalidInput = new ProductInputModel { Name = " ", Status = ProductStatus.Active };
        Assert.Same(invalidInput, Assert.IsType<ViewResult>(await controller.Edit(invalidInput, CancellationToken.None)).Model);
        Assert.NotEmpty(controller.ModelState[string.Empty]!.Errors);
    }

    [Fact]
    public async Task Bill_of_material_actions_populate_options_and_handle_invalid_or_valid_input()
    {
        await using var dbContext = CreateDbContext();
        var product = new Product(Guid.NewGuid(), "Bolsa");
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        await new ProductRepository(dbContext).AddAsync(product);
        await new RawMaterialRepository(dbContext).AddAsync(material);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        Assert.IsType<NotFoundResult>(await controller.BillOfMaterials(Guid.NewGuid(), CancellationToken.None));
        var get = Assert.IsType<ViewResult>(await controller.BillOfMaterials(product.Id, CancellationToken.None));
        Assert.NotNull(get.Model);
        Assert.NotNull(controller.ViewBag.RawMaterials);

        var input = new BillOfMaterialsInputModel { ProductId = product.Id };
        controller.ModelState.AddModelError("Items[0]", "invalid");
        Assert.Same(input, Assert.IsType<ViewResult>(await controller.BillOfMaterials(input, CancellationToken.None)).Model);
        Assert.NotNull(controller.ViewBag.RawMaterials);

        controller.ModelState.Clear();
        input.Items = [new BillOfMaterialsItemInputModel { RawMaterialId = material.Id, QuantityPerUnit = 2 }];
        Assert.Equal(nameof(ProductsController.Index), Assert.IsType<RedirectToActionResult>(
            await controller.BillOfMaterials(input, CancellationToken.None)).ActionName);

        controller.ModelState.Clear();
        input.Items =
        [
            new BillOfMaterialsItemInputModel { RawMaterialId = material.Id, QuantityPerUnit = 1 },
            new BillOfMaterialsItemInputModel { RawMaterialId = material.Id, QuantityPerUnit = 2 }
        ];
        Assert.IsType<ViewResult>(await controller.BillOfMaterials(input, CancellationToken.None));
        Assert.NotEmpty(controller.ModelState[string.Empty]!.Errors);
    }

    [Fact]
    public async Task Mapping_actions_list_create_and_return_view_on_duplicate_or_invalid_input()
    {
        await using var dbContext = CreateDbContext();
        var product = new Product(Guid.NewGuid(), "Caneca");
        await new ProductRepository(dbContext).AddAsync(product);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        Assert.IsType<ViewResult>(await controller.Mappings(CancellationToken.None));
        Assert.NotNull(controller.ViewBag.Products);
        Assert.NotNull(controller.ViewBag.Mappings);

        var input = new ProductMappingInputModel { Source = "Shopee", ExternalItemId = "SKU-1", ProductId = product.Id };
        Assert.Equal(nameof(ProductsController.Mappings), Assert.IsType<RedirectToActionResult>(
            await controller.Mappings(input, CancellationToken.None)).ActionName);

        controller.ModelState.Clear();
        Assert.IsType<ViewResult>(await controller.Mappings(input, CancellationToken.None));
        Assert.NotEmpty(controller.ModelState[string.Empty]!.Errors);
        Assert.NotNull(controller.ViewBag.Products);
        Assert.NotNull(controller.ViewBag.Mappings);

        controller.ModelState.Clear();
        controller.ModelState.AddModelError(nameof(ProductMappingInputModel.Source), "required");
        Assert.Same(input, Assert.IsType<ViewResult>(await controller.Mappings(input, CancellationToken.None)).Model);
    }

    private static ProductsController CreateController(AppDbContext dbContext)
    {
        var products = new ProductRepository(dbContext);
        var mappings = new ProductMappingRepository(dbContext);
        var materials = new RawMaterialRepository(dbContext);
        var service = new ProductCatalogAppService(products, mappings, materials,
            new ProductMappingService(mappings), new UnitOfWork(dbContext),
            NullLogger<ProductCatalogAppService>.Instance);
        return new ProductsController(service);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
