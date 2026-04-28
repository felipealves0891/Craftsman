using Craftsman.Domain.Events;
using Craftsman.Domain.Finance.Repositories;
using Craftsman.Domain.Finance.Services;
using Craftsman.Domain.Integration.Services;
using Craftsman.Domain.Inventory.Repositories;
using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.ProductCatalog.Services;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Domain.Services;
using Craftsman.Domain.Shipping.Repositories;
using Craftsman.Domain.Shipping.Services;
using Craftsman.Infra.Integrations.Shopee;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Craftsman.Infra.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCraftsmanInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CraftsmanDb")
            ?? throw new InvalidOperationException("Connection string 'CraftsmanDb' was not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddDataProtection();
        services.AddMemoryCache();
        services.AddOptions<ShopeeOptions>()
            .Bind(configuration.GetSection(ShopeeOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ShopeeOptions>, ShopeeOptionsValidator>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductMappingRepository, ProductMappingRepository>();
        services.AddScoped<IRawMaterialRepository, RawMaterialRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IProductionTaskRepository, ProductionTaskRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IFinancialSettlementRepository, FinancialSettlementRepository>();
        services.AddScoped<InventoryService>();
        services.AddScoped<IProductionPlanner, ProductionPlanner>();
        services.AddScoped<OrderProductionPlanningService>();
        services.AddScoped<ISettlementCalculator, SettlementCalculator>();
        services.AddScoped<IOrderNormalizer, OrderNormalizer>();
        services.AddScoped<IOrderImportPipeline, OrderImportPipeline>();
        services.AddScoped<IOrderSource, InMemoryOrderSource>();
        services.AddScoped<IShopeeSigner, ShopeeSigner>();
        services.AddScoped<IShopeeShopTokenRepository, ShopeeShopTokenRepository>();
        services.AddScoped<IShopeeTokenService, ShopeeTokenService>();
        services.AddHttpClient<IShopeeClient, ShopeeClient>((serviceProvider, client) =>
        {
            var shopeeOptions = serviceProvider.GetRequiredService<IOptions<ShopeeOptions>>().Value;
            client.BaseAddress = new Uri(shopeeOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(shopeeOptions.RequestTimeoutSeconds);
        });

        var shopeeOptions = configuration.GetSection(ShopeeOptions.SectionName).Get<ShopeeOptions>() ?? new ShopeeOptions();
        if (shopeeOptions.Enabled)
        {
            services.AddScoped<IOrderSource, ShopeeOrderSource>();
        }

        services.AddScoped<IShippingTracker, StaticShippingTracker>();
        services.AddScoped<ShippingService>();
        services.AddScoped<ProductMappingService>();
        services.AddScoped<IDomainEventPublisher, InMemoryDomainEventPublisher>();
        services.AddScoped<IDomainEventHandler<OrderNormalizedEvent>, DomainEventPersistenceHandler<OrderNormalizedEvent>>();
        services.AddScoped<IDomainEventHandler<OrderNormalizedEvent>, OrderNormalizedProductionPlannerHandler>();
        services.AddScoped<IDomainEventHandler<ProductionPlannedEvent>, DomainEventPersistenceHandler<ProductionPlannedEvent>>();
        services.AddScoped<IDomainEventHandler<ShipmentCreatedEvent>, DomainEventPersistenceHandler<ShipmentCreatedEvent>>();
        services.AddScoped<IDomainEventHandler<DeliveryConfirmedEvent>, DomainEventPersistenceHandler<DeliveryConfirmedEvent>>();
        services.AddScoped<IDomainEventHandler<DeliveryConfirmedEvent>, DeliveryConfirmedSettlementHandler>();
        services.AddScoped<IDomainEventHandler<FinancialSettlementCalculatedEvent>, DomainEventPersistenceHandler<FinancialSettlementCalculatedEvent>>();
        services.AddSingleton<IApplicationCache, MemoryApplicationCache>();

        return services;
    }
}
