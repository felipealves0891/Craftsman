namespace Craftsman.App.Services;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCraftsmanApplication(this IServiceCollection services)
    {
        services.AddScoped<OrderQueryService>();
        services.AddScoped<ProductionScheduleService>();
        services.AddScoped<ShippingAppService>();
        services.AddScoped<FinanceAppService>();

        return services;
    }
}
