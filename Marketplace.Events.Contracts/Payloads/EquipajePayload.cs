namespace Marketplace.Events.Contracts.Payloads;

/// <summary>
/// Equipaje asociado a una línea de reserva. Alineado a ReservaPagarEquipajeRequestDto.
/// IdDetalle puede resolverse en Reservas tras crear la reserva (por índice o id pasajero).
/// </summary>
public record EquipajePayload
{
    public int? IdDetalle { get; init; }
    public int? IdPasajero { get; init; }
    public string Tipo { get; init; } = null!;
    public decimal PesoKg { get; init; }
    public string? DescripcionEquipaje { get; init; }
}
