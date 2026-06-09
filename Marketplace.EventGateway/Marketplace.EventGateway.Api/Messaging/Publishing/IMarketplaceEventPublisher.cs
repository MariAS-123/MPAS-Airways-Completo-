using Marketplace.Events.Contracts;

namespace Marketplace.EventGateway.Api.Messaging.Publishing;

public interface IMarketplaceEventPublisher
{
    Task PublishAsync<TEvent>(TEvent marketplaceEvent, CancellationToken cancellationToken = default)
        where TEvent : MarketplaceEvent;
}
