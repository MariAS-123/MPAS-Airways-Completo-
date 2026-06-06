namespace Marketplace.Events.Contracts.Events;

/// <summary>
/// MS Vuelos → MS Reservas / Gateway. Pre-reserva temporal del asiento (15 min según PDF).
/// </summary>
public record AsientoPreReservadoEvent : MarketplaceEvent
{
    public int IdVuelo { get; init; }
    public int IdAsiento { get; init; }
    public int IdCliente { get; init; }
    public DateTime ExpiraEnUtc { get; init; }
    public string? TokenPreReserva { get; init; }
}
