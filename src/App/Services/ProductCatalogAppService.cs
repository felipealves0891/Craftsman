using Craftsman.App.Models;
using Craftsman.Domain.Inventory.Repositories;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.ProductCatalog.Services;
using Craftsman.Domain.Repositories;

namespace Craftsman.App.Services;

public sealed class ProductCatalogAppService
{
    private readonly ILogger<ProductCatalogAppService> logger;
    private readonly IProductMappingRepository productMappingRepository;
    private readonly ProductMappingService productMappingService;
    private readonly IProductRepository productRepository;
    private readonly IRawMaterialRepository rawMaterialRepository;
    private readonly IUnitOfWork unitOfWork;

    public ProductCatalogAppService(
        IProductRepository productRepository,
        IProductMappingRepository productMappingRepository,
        IRawMaterialRepository rawMaterialRepository,
        ProductMappingService productMappingService,
        IUnitOfWork unitOfWork,
        ILogger<ProductCatalogAppService> logger)
    {
        this.productRepository = productRepository;
        this.productMappingRepository = productMappingRepository;
        this.rawMaterialRepository = rawMaterialRepository;
        this.productMappingService = productMappingService;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<IReadOnlyCollection<ProductListItemViewModel>> ListProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await productRepository.ListAsync(cancellationToken);

        return products
            .Select(product => new ProductListItemViewModel(
                product.Id,
                product.Name,
                product.Status.ToString(),
                product.ProductionDurationHours,
                product.HourlyRate,
                product.BillOfMaterials.Count))
            .ToList()
            .AsReadOnly();
    }

    public async Task<ProductInputModel?> GetProductInputAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);

        return product is null
            ? null
            : new ProductInputModel
            {
                Id = product.Id,
                Name = product.Name,
                Status = product.Status,
                ProductionDurationHours = product.ProductionDurationHours,
                HourlyRate = product.HourlyRate
            };
    }

    public async Task<Guid> SaveProductAsync(ProductInputModel input, CancellationToken cancellationToken = default)
    {
        if (input.Id is null || input.Id == Guid.Empty)
        {
            var product = new Product(Guid.NewGuid(), input.Name, input.Status, input.ProductionDurationHours, input.HourlyRate);
            await productRepository.AddAsync(product, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return product.Id;
        }

        var existing = await productRepository.GetByIdAsync(input.Id.Value, cancellationToken)
            ?? throw new InvalidOperationException("Produto nao encontrado.");

        existing.Rename(input.Name);
        existing.SetProductionDurationHours(input.ProductionDurationHours);
        existing.SetHourlyRate(input.HourlyRate);
        if (input.Status == ProductStatus.Active)
        {
            existing.Activate();
        }
        else
        {
            existing.Deactivate();
        }

        await productRepository.UpdateAsync(existing, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return existing.Id;
    }

    public async Task<BillOfMaterialsInputModel?> GetBillOfMaterialsInputAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var model = new BillOfMaterialsInputModel
        {
            ProductId = product.Id,
            Items = product.BillOfMaterials
                .Select(item => new BillOfMaterialsItemInputModel
                {
                    RawMaterialId = item.RawMaterialId,
                    QuantityPerUnit = (int)item.QuantityPerUnit
                })
                .ToList()
        };

        while (model.Items.Count < 5)
        {
            model.Items.Add(new BillOfMaterialsItemInputModel());
        }

        return model;
    }

    public async Task SaveBillOfMaterialsAsync(BillOfMaterialsInputModel input, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Saving bill of materials for product: {ProductId}", input.ProductId);

        var product = await productRepository.GetByIdAsync(input.ProductId, cancellationToken)
            ?? throw new InvalidOperationException("Produto nao encontrado.");

        logger.LogInformation("Found product: {ProductId}", product.Id);
        
        var items = input.Items
            .Where(item => item.RawMaterialId.HasValue || item.QuantityPerUnit > 0)
            .Select(item => new BillOfMaterialsItem(
                item.RawMaterialId ?? throw new InvalidOperationException("Materia-prima e obrigatoria."),
                item.QuantityPerUnit))
            .ToList();

        logger.LogInformation("Found bill of materials for product {ProductId}: {ItemCount} items", product.Id, items.Count);

        product.ReplaceBillOfMaterials(items);

        logger.LogInformation("Replacing bill of materials for product {ProductId}", product.Id);
        
        await productRepository.UpdateAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductMappingListItemViewModel>> ListMappingsAsync(CancellationToken cancellationToken = default)
    {
        var mappings = await productMappingRepository.ListAsync(cancellationToken);
        var products = await productRepository.ListAsync(cancellationToken);
        var productNames = products.ToDictionary(product => product.Id, product => product.Name);

        return mappings
            .Select(mapping => new ProductMappingListItemViewModel(
                mapping.Id,
                mapping.Source,
                mapping.ExternalItemId,
                mapping.ProductId,
                productNames.GetValueOrDefault(mapping.ProductId, "(produto nao encontrado)")))
            .ToList()
            .AsReadOnly();
    }

    public async Task CreateMappingAsync(ProductMappingInputModel input, CancellationToken cancellationToken = default)
    {
        _ = await productRepository.GetByIdAsync(input.ProductId, cancellationToken)
            ?? throw new InvalidOperationException("Produto nao encontrado.");

        await productMappingService.CreateMappingAsync(input.Source, input.ExternalItemId, input.ProductId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductOptionViewModel>> ListProductOptionsAsync(CancellationToken cancellationToken = default)
    {
        var products = await productRepository.ListAsync(cancellationToken);

        return products.Select(product => new ProductOptionViewModel(product.Id, product.Name)).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyCollection<RawMaterialOptionViewModel>> ListRawMaterialOptionsAsync(CancellationToken cancellationToken = default)
    {
        var materials = await rawMaterialRepository.ListAsync(cancellationToken);

        return materials
            .Select(material => new RawMaterialOptionViewModel(material.Id, material.Name, material.UnitOfMeasure))
            .ToList()
            .AsReadOnly();
    }
}
