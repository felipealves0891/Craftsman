using Craftsman.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Craftsman.Tests.Application;

public sealed class ApplicationServiceCollectionExtensionsTests
{
    [Fact]
    public void Add_craftsman_application_registers_application_services_and_system_clock()
    {
        var services = new ServiceCollection();

        var result = services.AddCraftsmanApplication();

        Assert.Same(services, result);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(TimeProvider)
            && ReferenceEquals(descriptor.ImplementationInstance, TimeProvider.System));
        Assert.All(
            services.Where(descriptor => descriptor.ServiceType != typeof(TimeProvider) && descriptor.ServiceType != typeof(IHostedService)),
            descriptor => Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ProductionScheduleService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(OrderQueryService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ShippingAppService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(FinanceAppService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ManualOrderService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ProductCatalogAppService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(InventoryAppService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(StockAlertAppService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(HistoricalLaborCostReconciliationService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(HistoricalLaborCostReconciliationWorker));
    }
}
