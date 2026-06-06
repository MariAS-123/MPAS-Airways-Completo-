namespace Marketplace.Events.Contracts.Payloads;

/// <summary>
/// Línea de reserva (pasajero + asiento + montos). Alineado a ReservaDetalleRequestDto.
/// </summary>
public record ReservaDetallePayload
{
    public int IdPasajero { get; init; }
    public int IdAsiento { get; init; }
    public decimal SubtotalLinea { get; init; }
    public decimal ValorIvaLinea { get; init; }
    public decimal TotalLinea { get; init; }
}
