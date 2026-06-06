using Microsoft.Extensions.Caching.Memory;

namespace Microservicio.Vuelos.Api.Messaging.Services;

public sealed record PreReservaAsientoEntry(
    Guid CorrelationId,
    int IdCliente,
    string TokenPreReserva,
    DateTime ExpiraEnUtc);

public interface IPreReservaAsientoStore
{
    bool TryRegistrar(
        int idVuelo,
        int idAsiento,
        int idCliente,
        Guid correlationId,
        TimeSpan duracion,
        out PreReservaAsientoEntry entry);
}

public sealed class PreReservaAsientoStore : IPreReservaAsientoStore
{
    private readonly IMemoryCache _cache;

    public PreReservaAsientoStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool TryRegistrar(
        int idVuelo,
        int idAsiento,
        int idCliente,
        Guid correlationId,
        TimeSpan duracion,
        out PreReservaAsientoEntry entry)
    {
        var key = BuildKey(idVuelo, idAsiento);

        if (_cache.TryGetValue<PreReservaAsientoEntry>(key, out var existente)
            && existente is not null
            && existente.ExpiraEnUtc > DateTime.UtcNow
            && existente.CorrelationId != correlationId)
        {
            entry = default!;
            return false;
        }

        entry = new PreReservaAsientoEntry(
            correlationId,
            idCliente,
            Guid.NewGuid().ToString("N"),
            DateTime.UtcNow.Add(duracion));

        _cache.Set(key, entry, entry.ExpiraEnUtc);
        return true;
    }

    private static string BuildKey(int idVuelo, int idAsiento) =>
        $"marketplace:pre-reserva:{idVuelo}:{idAsiento}";
}
