using Marketplace.Events.Contracts.Payloads;

namespace Marketplace.Events.Contracts.Events;

/// <summary>
/// Gateway → MS Clientes. Registro/validación de pasajeros del flujo marketplace.
/// </summary>
public record PasajerosRegistradosEvent : MarketplaceEvent
{
    public int IdCliente { get; init; }
    public IReadOnlyList<PasajeroPayload> Pasajeros { get; init; } = [];
}
