using Marketplace.Events.Contracts.Payloads;

namespace Marketplace.Events.Contracts.Events;

/// <summary>
/// Gateway → MS Reservas. Equipaje declarado antes de confirmar la reserva.
/// Reservas lo acumula por CorrelationId hasta ReservaSolicitada.
/// </summary>
public record EquipajeAgregadoEvent : MarketplaceEvent
{
    public int IdCliente { get; init; }
    public int IdVuelo { get; init; }
    public IReadOnlyList<EquipajePayload> Equipaje { get; init; } = [];
}
