namespace Marketplace.Events.Contracts.Events;

/// <summary>
/// MS Clientes → Gateway. Resultado de validación/registro de pasajeros.
/// </summary>
public record PasajerosValidadosEvent : MarketplaceEvent
{
    public int IdCliente { get; init; }
    public bool EsValido { get; init; }
    public IReadOnlyList<int> IdsPasajerosValidados { get; init; } = [];
    public string? MotivoRechazo { get; init; }
}
