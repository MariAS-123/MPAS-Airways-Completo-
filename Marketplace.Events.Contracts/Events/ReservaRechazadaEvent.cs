namespace Marketplace.Events.Contracts.Events;

/// <summary>
/// MS Reservas → Gateway. Fallo en cualquier paso de la saga.
/// </summary>
public record ReservaRechazadaEvent : MarketplaceEvent
{
    public int? IdReserva { get; init; }
    public int IdCliente { get; init; }
    public int? IdVuelo { get; init; }
    public string PasoFallido { get; init; } = null!;
    public string CodigoError { get; init; } = null!;
    public string Mensaje { get; init; } = null!;
}
