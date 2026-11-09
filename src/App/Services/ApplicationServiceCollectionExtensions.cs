using System.Diagnostics.CodeAnalysis;
namespace Craftsman.App.Services;

[ExcludeFromCodeCoverage]
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCraftsmanApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<OrderQueryService>();
        services.AddScoped<ProductionScheduleService>();
        services.AddScoped<ShippingAppService>();
        services.AddScoped<FinanceAppService>();
        services.AddScoped<ManualOrderService>();
        services.AddScoped<ProductCatalogAppService>();
        services.AddScoped<InventoryAppService>();
        services.AddScoped<StockAlertAppService>();
        services.AddScoped<HistoricalLaborCostReconciliationService>();
        services.AddScoped<IHistoricalLaborCostReconciliationService>(serviceProvider =>
            serviceProvider.GetRequiredService<HistoricalLaborCostReconciliationService>());
        services.AddHostedService<HistoricalLaborCostReconciliationWorker>();

        return services;
    }
}
