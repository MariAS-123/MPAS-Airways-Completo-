using Marketplace.Events.Contracts.Events;
using Marketplace.Events.Contracts.Routing;

namespace Marketplace.Events.Contracts;

/// <summary>
/// Mapa tipo de evento → routing key RabbitMQ.
/// </summary>
public static class MarketplaceEventTypes
{
    public static string GetRoutingKey<TEvent>() where TEvent : MarketplaceEvent =>
        typeof(TEvent) switch
        {
            var t when t == typeof(VueloSeleccionadoEvent) => MarketplaceRoutingKeys.VueloSeleccionado,
            var t when t == typeof(AsientoPreReservadoEvent) => MarketplaceRoutingKeys.AsientoPreReservado,
            var t when t == typeof(PasajerosRegistradosEvent) => MarketplaceRoutingKeys.PasajerosRegistrados,
            var t when t == typeof(PasajerosValidadosEvent) => MarketplaceRoutingKeys.PasajerosValidados,
            var t when t == typeof(EquipajeAgregadoEvent) => MarketplaceRoutingKeys.EquipajeAgregado,
            var t when t == typeof(ReservaSolicitadaEvent) => MarketplaceRoutingKeys.ReservaSolicitada,
            var t when t == typeof(ReservaCreadaEvent) => MarketplaceRoutingKeys.ReservaCreada,
            var t when t == typeof(ReservaRechazadaEvent) => MarketplaceRoutingKeys.ReservaRechazada,
            var t when t == typeof(FacturaGeneradaEvent) => MarketplaceRoutingKeys.FacturaGenerada,
            var t when t == typeof(BoletoEmitidoEvent) => MarketplaceRoutingKeys.BoletoEmitido,
            _ => throw new ArgumentOutOfRangeException(nameof(TEvent), typeof(TEvent).Name, "Tipo de evento no registrado.")
        };

    public static string GetEventTypeName<TEvent>() where TEvent : MarketplaceEvent =>
        typeof(TEvent).Name;
}
