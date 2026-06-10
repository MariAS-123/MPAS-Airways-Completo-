namespace Marketplace.Events.Contracts.Events;

/// <summary>
/// MS Reservas → Gateway. Reserva persistida y pagada (EMI) con equipaje opcional.
/// </summary>
public record ReservaCreadaEvent : MarketplaceEvent
{
    public int IdReserva { get; init; }
    public string CodigoReserva { get; init; } = null!;
    public string EstadoReserva { get; init; } = "EMI";
    public int IdCliente { get; init; }
    public int IdVuelo { get; init; }
    public int EquipajesRegistrados { get; init; }
}
