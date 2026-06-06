namespace Marketplace.Events.Contracts.Routing;

/// <summary>
/// Topología RabbitMQ acordada (Etapa 2). Un solo exchange topic por vhost dedicado.
/// </summary>
public static class MarketplaceExchanges
{
    public const string Vhost = "vuelos-marketplace";

    public const string Events = "vuelos.marketplace.events";

    public const string DeadLetter = "vuelos.marketplace.dlx";
}
