namespace Marketplace.Events.Contracts.Events;

/// <summary>
/// MS Reservas → Gateway. Reserva persistida en estado PEN.
/// </summary>
public record ReservaCreadaEvent : MarketplaceEvent
{
    public int IdReserva { get; init; }
    public string CodigoReserva { get; init; } = null!;
    public string EstadoReserva { get; init; } = "PEN";
    public int IdCliente { get; init; }
    public int IdVuelo { get; init; }
}
