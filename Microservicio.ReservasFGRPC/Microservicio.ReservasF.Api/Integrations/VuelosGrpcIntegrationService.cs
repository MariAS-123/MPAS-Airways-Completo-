using Microservicio.ReservasF.Business.Integrations;
using Microservicio.ReservasF.Business.Integrations.Interfaces;
using Microservicio.Vuelos.Grpc;

namespace Microservicio.ReservasF.Api.Integrations;

public class VuelosGrpcIntegrationService : IVueloIntegrationService
{
    private readonly VuelosGrpc.VuelosGrpcClient _client;

    public VuelosGrpcIntegrationService(VuelosGrpc.VuelosGrpcClient client)
    {
        _client = client;
    }

    public async Task<VueloIntegrationDto?> ObtenerVueloAsync(
        int idVuelo,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.GetVueloAsync(
            new GetVueloRequest { IdVuelo = idVuelo },
            cancellationToken: cancellationToken);

        if (!response.Success || response.IdVuelo <= 0)
            return null;

        return MapVuelo(response);
    }

    public async Task<AsientoIntegrationDto?> ObtenerAsientoAsync(
        int idVuelo,
        int idAsiento,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.ValidarAsientoAsync(
            new ValidarAsientoRequest { IdVuelo = idVuelo, IdAsiento = idAsiento },
            cancellationToken: cancellationToken);

        if (response.IdAsiento <= 0 || response.IdVuelo != idVuelo)
            return null;

        return MapAsiento(response);
    }

    public async Task<bool> ExisteVueloAsync(
        int idVuelo,
        CancellationToken cancellationToken = default)
    {
        var vuelo = await ObtenerVueloAsync(idVuelo, cancellationToken);
        return vuelo != null;
    }

    public async Task<bool> VueloDisponibleParaReservaAsync(
        int idVuelo,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.ValidarVueloAsync(
            new GetVueloRequest { IdVuelo = idVuelo },
            cancellationToken: cancellationToken);

        return response.Success;
    }

    public async Task<bool> VueloPermiteEmisionAsync(
        int idVuelo,
        CancellationToken cancellationToken = default)
    {
        var vuelo = await ObtenerVueloAsync(idVuelo, cancellationToken);

        if (vuelo == null)
            return false;

        var estado = vuelo.Estado.Trim().ToUpperInvariant();
        var estadoVuelo = vuelo.EstadoVuelo.Trim().ToUpperInvariant();

        return estado == "ACTIVO"
            && !vuelo.Eliminado
            && estadoVuelo != "CANCELADO";
    }

    public async Task<bool> ExisteAsientoAsync(
        int idVuelo,
        int idAsiento,
        CancellationToken cancellationToken = default)
    {
        var asiento = await ObtenerAsientoAsync(idVuelo, idAsiento, cancellationToken);
        return asiento != null;
    }

    public async Task<bool> AsientoPerteneceAVueloAsync(
        int idAsiento,
        int idVuelo,
        CancellationToken cancellationToken = default)
    {
        var asiento = await ObtenerAsientoAsync(idVuelo, idAsiento, cancellationToken);
        return asiento != null && asiento.IdVuelo == idVuelo;
    }

    public async Task MarcarAsientoNoDisponibleAsync(
        int idVuelo,
        int idAsiento,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.BloquearAsientoAsync(
            new BloquearAsientoRequest
            {
                IdVuelo = idVuelo,
                IdAsiento = idAsiento,
                ModificadoPor = modificadoPorUsuario
            },
            cancellationToken: cancellationToken);

        if (!response.Success)
            throw new InvalidOperationException(response.Message);
    }

    private static VueloIntegrationDto MapVuelo(VueloGrpcResponse response)
    {
        return new VueloIntegrationDto
        {
            IdVuelo = response.IdVuelo,
            IdAeropuertoOrigen = response.IdAeropuertoOrigen,
            IdAeropuertoDestino = response.IdAeropuertoDestino,
            NumeroVuelo = response.NumeroVuelo,
            FechaHoraSalida = DateTime.Parse(response.FechaHoraSalida),
            FechaHoraLlegada = DateTime.Parse(response.FechaHoraLlegada),
            DuracionMin = response.DuracionMin,
            PrecioBase = (decimal)response.PrecioBase,
            CapacidadTotal = response.CapacidadTotal,
            EstadoVuelo = response.EstadoVuelo,
            Estado = response.Estado,
            Eliminado = response.Eliminado
        };
    }

    private static AsientoIntegrationDto MapAsiento(AsientoGrpcResponse response)
    {
        return new AsientoIntegrationDto
        {
            IdAsiento = response.IdAsiento,
            IdVuelo = response.IdVuelo,
            NumeroAsiento = response.NumeroAsiento,
            Clase = response.Clase,
            Disponible = response.Disponible,
            PrecioExtra = (decimal)response.PrecioExtra,
            Posicion = string.IsNullOrWhiteSpace(response.Posicion) ? null : response.Posicion,
            Estado = response.Estado,
            Eliminado = response.Eliminado
        };
    }
}
