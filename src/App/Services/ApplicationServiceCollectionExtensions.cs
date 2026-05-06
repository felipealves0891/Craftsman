namespace Craftsman.App.Services;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCraftsmanApplication(this IServiceCollection services)
    {
        services.AddScoped<OrderQueryService>();
        services.AddScoped<ProductionScheduleService>();
        services.AddScoped<ShippingAppService>();
        services.AddScoped<FinanceAppService>();
        services.AddScoped<ManualOrderService>();
        services.AddScoped<ProductCatalogAppService>();
        services.AddScoped<InventoryAppService>();
        services.AddScoped<StockAlertAppService>();

        return services;
    }
}
