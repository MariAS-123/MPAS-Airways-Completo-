using Marketplace.Events.Contracts.Saga;
using System.Collections.Concurrent;

namespace Marketplace.EventGateway.Api.Services;

public sealed class SagaStateStore
{
    private readonly ConcurrentDictionary<Guid, SagaStateEntry> _states = new();

    public SagaStateEntry GetOrCreate(Guid correlationId) =>
        _states.GetOrAdd(correlationId, id => new SagaStateEntry { CorrelationId = id });

    public SagaStateEntry? TryGet(Guid correlationId) =>
        _states.TryGetValue(correlationId, out var entry) ? entry : null;

    public void MarkAccepted(Guid correlationId, string paso)
    {
        var entry = GetOrCreate(correlationId);
        entry.UltimoPaso = paso;
        entry.ActualizadoEnUtc = DateTime.UtcNow;
        if (entry.Estado is MarketplaceSagaStatus.Aceptada or MarketplaceSagaStatus.Rechazada)
            return;

        entry.Estado = paso switch
        {
            "VUELO_SELECCIONADO" => MarketplaceSagaStatus.VueloSeleccionado,
            "PASAJEROS_REGISTRADOS" => MarketplaceSagaStatus.PasajerosValidados,
            "RESERVA_SOLICITADA" => MarketplaceSagaStatus.ReservaEnProceso,
            _ => entry.Estado
        };
    }
}

public sealed class SagaStateEntry
{
    public Guid CorrelationId { get; init; }
    public MarketplaceSagaStatus Estado { get; set; } = MarketplaceSagaStatus.Aceptada;
    public string? UltimoPaso { get; set; }
    public int? IdReserva { get; set; }
    public string? CodigoReserva { get; set; }
    public string? TokenPreReserva { get; set; }
    public IReadOnlyList<int> IdsPasajerosValidados { get; set; } = [];
    public string? MotivoRechazo { get; set; }
    public string? CodigoError { get; set; }
    public DateTime ActualizadoEnUtc { get; set; } = DateTime.UtcNow;
}
