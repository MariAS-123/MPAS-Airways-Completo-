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
            "PASAJEROS_REGISTRADOS" => entry.Estado,
            "RESERVA_SOLICITADA" => MarketplaceSagaStatus.ReservaEnProceso,
            _ => entry.Estado
        };
    }

    public void MarkAsientoPreReservado(Guid correlationId, string? tokenPreReserva)
    {
        var entry = GetOrCreate(correlationId);
        entry.TokenPreReserva = tokenPreReserva;
        entry.UltimoPaso = "ASIENTO_PRE_RESERVADO";
        entry.Estado = MarketplaceSagaStatus.AsientoPreReservado;
        entry.ActualizadoEnUtc = DateTime.UtcNow;
    }

    public void MarkPasajerosValidados(Guid correlationId, IReadOnlyList<int> idsPasajeros)
    {
        var entry = GetOrCreate(correlationId);
        entry.IdsPasajerosValidados = idsPasajeros;
        entry.UltimoPaso = "PASAJEROS_VALIDADOS";
        entry.Estado = MarketplaceSagaStatus.PasajerosValidados;
        entry.ActualizadoEnUtc = DateTime.UtcNow;
    }

    public void MarkReservaCreada(Guid correlationId, int idReserva, string codigoReserva)
    {
        var entry = GetOrCreate(correlationId);
        entry.IdReserva = idReserva;
        entry.CodigoReserva = codigoReserva;
        entry.UltimoPaso = "RESERVA_CREADA";
        entry.Estado = MarketplaceSagaStatus.ReservaCreada;
        entry.ActualizadoEnUtc = DateTime.UtcNow;
    }

    public void MarkRechazada(Guid correlationId, string motivo, string? codigoError = null, string? pasoFallido = null)
    {
        var entry = GetOrCreate(correlationId);
        entry.Estado = MarketplaceSagaStatus.Rechazada;
        entry.MotivoRechazo = motivo;
        entry.CodigoError = codigoError;
        entry.UltimoPaso = pasoFallido ?? entry.UltimoPaso;
        entry.ActualizadoEnUtc = DateTime.UtcNow;
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
