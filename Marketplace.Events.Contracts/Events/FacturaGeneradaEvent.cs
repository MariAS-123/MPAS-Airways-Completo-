namespace Marketplace.Events.Contracts.Events;

/// <summary>
/// MS Reservas → Gateway / cadena interna. Factura generada tras crear o pagar reserva.
/// </summary>
public record FacturaGeneradaEvent : MarketplaceEvent
{
    public int IdFactura { get; init; }
    public string NumeroFactura { get; init; } = null!;
    public int IdReserva { get; init; }
    public string CodigoReserva { get; init; } = null!;
    public int IdCliente { get; init; }
    public decimal Total { get; init; }
    public string EstadoFactura { get; init; } = "APR";
}
