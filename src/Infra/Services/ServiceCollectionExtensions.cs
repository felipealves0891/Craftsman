using Craftsman.Domain.Events;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddMemoryCache();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventPublisher, InMemoryDomainEventPublisher>();
        services.AddScoped<IDomainEventHandler<OrderNormalizedEvent>, DomainEventPersistenceHandler<OrderNormalizedEvent>>();
        services.AddScoped<IDomainEventHandler<ProductionPlannedEvent>, DomainEventPersistenceHandler<ProductionPlannedEvent>>();
        services.AddScoped<IDomainEventHandler<ShipmentCreatedEvent>, DomainEventPersistenceHandler<ShipmentCreatedEvent>>();
        services.AddScoped<IDomainEventHandler<DeliveryConfirmedEvent>, DomainEventPersistenceHandler<DeliveryConfirmedEvent>>();
        services.AddScoped<IDomainEventHandler<FinancialSettlementCalculatedEvent>, DomainEventPersistenceHandler<FinancialSettlementCalculatedEvent>>();
        services.AddSingleton<IApplicationCache, MemoryApplicationCache>();

        return services;
    }
}
