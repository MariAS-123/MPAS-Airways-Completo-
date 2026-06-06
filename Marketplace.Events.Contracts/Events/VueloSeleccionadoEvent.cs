namespace Marketplace.Events.Contracts.Events;

/// <summary>
/// Gateway → MS Vuelos. Cliente eligió vuelo y asiento.
/// </summary>
public record VueloSeleccionadoEvent : MarketplaceEvent
{
    public int IdCliente { get; init; }
    public int IdVuelo { get; init; }
    public int IdAsiento { get; init; }
}
