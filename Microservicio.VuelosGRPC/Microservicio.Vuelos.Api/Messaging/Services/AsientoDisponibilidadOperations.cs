using Microservicio.Vuelos.Business.DTOs.Asiento;
using Microservicio.Vuelos.Business.Interfaces;

namespace Microservicio.Vuelos.Api.Messaging.Services;

/// <summary>
/// Envuelve IAsientoService con la misma operación que usa VuelosGrpcService.BloquearAsiento.
/// </summary>
public sealed class AsientoDisponibilidadOperations
{
    private readonly IAsientoService _asientoService;

    public AsientoDisponibilidadOperations(IAsientoService asientoService)
    {
        _asientoService = asientoService;
    }

    public async Task<(bool Success, string Message)> BloquearAsync(
        int idVuelo,
        int idAsiento,
        string modificadoPor,
        CancellationToken cancellationToken = default)
    {
        var actual = await _asientoService.GetByIdAsync(idAsiento);

        if (actual is null || actual.IdVuelo != idVuelo)
            return (false, "Asiento no encontrado.");

        if (!actual.Disponible)
            return (true, "Asiento ya estaba bloqueado.");

        var modificadoPorUsuario = string.IsNullOrWhiteSpace(modificadoPor)
            ? "MARKETPLACE_EVENT_BUS"
            : modificadoPor;

        var result = await _asientoService.UpdateAsync(
            idAsiento,
            new AsientoUpdateRequestDto
            {
                IdVuelo = idVuelo,
                NumeroAsiento = actual.NumeroAsiento,
                Clase = actual.Clase,
                Disponible = false,
                PrecioExtra = actual.PrecioExtra,
                Posicion = actual.Posicion
            },
            modificadoPorUsuario);

        return result is null
            ? (false, "No se pudo bloquear el asiento.")
            : (true, "Asiento bloqueado correctamente.");
    }
}
