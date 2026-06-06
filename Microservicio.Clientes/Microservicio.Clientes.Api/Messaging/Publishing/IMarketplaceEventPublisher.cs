using Marketplace.Events.Contracts;

namespace Microservicio.Clientes.Api.Messaging.Publishing;

public interface IMarketplaceEventPublisher
{
    Task PublishAsync<TEvent>(TEvent marketplaceEvent, CancellationToken cancellationToken = default)
        where TEvent : MarketplaceEvent;
}
