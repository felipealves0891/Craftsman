using Craftsman.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Craftsman.Infra.Services;

public sealed class InMemoryDomainEventPublisher : IDomainEventPublisher
{
    private readonly IServiceProvider serviceProvider;

    public InMemoryDomainEventPublisher(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var handlers = serviceProvider.GetServices<IDomainEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }
}
