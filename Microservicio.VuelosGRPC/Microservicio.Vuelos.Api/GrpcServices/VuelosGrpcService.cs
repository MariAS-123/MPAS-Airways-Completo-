using Grpc.Core;
using Microservicio.Vuelos.Business.DTOs.Asiento;
using Microservicio.Vuelos.Business.Interfaces;
using Microservicio.Vuelos.Grpc;

namespace Microservicio.Vuelos.Api.GrpcServices;

public class VuelosGrpcService : VuelosGrpc.VuelosGrpcBase
{
    private readonly IVueloService _vueloService;
    private readonly IAsientoService _asientoService;

    public VuelosGrpcService(IVueloService vueloService, IAsientoService asientoService)
    {
        _vueloService = vueloService;
        _asientoService = asientoService;
    }

    public override async Task<VueloGrpcResponse> GetVuelo(
        GetVueloRequest request,
        ServerCallContext context)
    {
        var vuelo = await _vueloService.GetByIdAsync(request.IdVuelo);

        if (vuelo is null)
        {
            return new VueloGrpcResponse
            {
                Success = false,
                Message = "Vuelo no encontrado."
            };
        }

        return MapVuelo(vuelo, success: true, message: "Vuelo obtenido correctamente.");
    }

    public override async Task<VueloGrpcResponse> ValidarVuelo(
        GetVueloRequest request,
        ServerCallContext context)
    {
        var vuelo = await _vueloService.GetByIdAsync(request.IdVuelo);

        if (vuelo is null)
        {
            return new VueloGrpcResponse
            {
                Success = false,
                Message = "Vuelo no encontrado."
            };
        }

        var estado = vuelo.Estado.Trim().ToUpperInvariant();
        var estadoVuelo = vuelo.EstadoVuelo.Trim().ToUpperInvariant();
        var valido = estado == "ACTIVO" && estadoVuelo is "PROGRAMADO" or "DEMORADO";

        return MapVuelo(
            vuelo,
            success: valido,
            message: valido
                ? "Vuelo válido para reserva."
                : "El vuelo no está disponible para reserva.");
    }

    public override async Task<AsientosGrpcResponse> GetAsientosByVuelo(
        GetVueloRequest request,
        ServerCallContext context)
    {
        var vuelo = await _vueloService.GetByIdAsync(request.IdVuelo);
        if (vuelo is null)
        {
            return new AsientosGrpcResponse
            {
                Success = false,
                Message = "Vuelo no encontrado."
            };
        }

        var asientos = new List<AsientoResponseDto>();
        const int pageSize = 200;
        var page = 1;

        while (true)
        {
            var result = await _asientoService.GetPagedAsync(new AsientoFilterDto
            {
                IdVuelo = request.IdVuelo,
                Page = page,
                PageSize = pageSize
            });

            asientos.AddRange(result.Items);

            if (!result.TienePaginaSiguiente)
                break;

            page++;
        }

        var response = new AsientosGrpcResponse
        {
            Success = true,
            Message = "Asientos obtenidos correctamente."
        };

        response.Asientos.AddRange(asientos.Select(a => MapAsiento(a, success: true, message: string.Empty)));

        return response;
    }

    public override async Task<AsientoGrpcResponse> ValidarAsiento(
        ValidarAsientoRequest request,
        ServerCallContext context)
    {
        var asiento = await _asientoService.GetByIdAsync(request.IdAsiento);

        if (asiento is null || asiento.IdVuelo != request.IdVuelo)
        {
            return new AsientoGrpcResponse
            {
                Success = false,
                Message = "Asiento no encontrado."
            };
        }

        var valido = asiento.Disponible
            && asiento.Estado.Trim().ToUpperInvariant() == "ACTIVO";

        return MapAsiento(
            asiento,
            success: valido,
            message: valido
                ? "Asiento válido."
                : "El asiento no está disponible.");
    }

    public override async Task<AsientoGrpcResponse> BloquearAsiento(
        BloquearAsientoRequest request,
        ServerCallContext context)
    {
        return await UpdateDisponibilidadAsync(
            request.IdVuelo,
            request.IdAsiento,
            disponible: false,
            request.ModificadoPor,
            successMessage: "Asiento bloqueado correctamente.",
            failureMessage: "No se pudo bloquear el asiento.");
    }

    public override async Task<AsientoGrpcResponse> LiberarAsiento(
        LiberarAsientoRequest request,
        ServerCallContext context)
    {
        return await UpdateDisponibilidadAsync(
            request.IdVuelo,
            request.IdAsiento,
            disponible: true,
            request.ModificadoPor,
            successMessage: "Asiento liberado correctamente.",
            failureMessage: "No se pudo liberar el asiento.");
    }

    private async Task<AsientoGrpcResponse> UpdateDisponibilidadAsync(
        int idVuelo,
        int idAsiento,
        bool disponible,
        string modificadoPor,
        string successMessage,
        string failureMessage)
    {
        var actual = await _asientoService.GetByIdAsync(idAsiento);

        if (actual is null || actual.IdVuelo != idVuelo)
        {
            return new AsientoGrpcResponse
            {
                Success = false,
                Message = "Asiento no encontrado."
            };
        }

        var modificadoPorUsuario = string.IsNullOrWhiteSpace(modificadoPor)
            ? "SYSTEM_RESERVAS"
            : modificadoPor;

        var result = await _asientoService.UpdateAsync(
            idAsiento,
            new AsientoUpdateRequestDto
            {
                IdVuelo = idVuelo,
                NumeroAsiento = actual.NumeroAsiento,
                Clase = actual.Clase,
                Disponible = disponible,
                PrecioExtra = actual.PrecioExtra,
                Posicion = actual.Posicion
            },
            modificadoPorUsuario);

        if (result is null)
        {
            return new AsientoGrpcResponse
            {
                Success = false,
                Message = failureMessage
            };
        }

        return MapAsiento(result, success: true, message: successMessage);
    }

    private static VueloGrpcResponse MapVuelo(
        Business.DTOs.Vuelo.VueloResponseDto vuelo,
        bool success,
        string message)
    {
        return new VueloGrpcResponse
        {
            Success = success,
            Message = message,
            IdVuelo = vuelo.IdVuelo,
            NumeroVuelo = vuelo.NumeroVuelo,
            IdAeropuertoOrigen = vuelo.IdAeropuertoOrigen,
            IdAeropuertoDestino = vuelo.IdAeropuertoDestino,
            FechaHoraSalida = vuelo.FechaHoraSalida.ToString("O"),
            FechaHoraLlegada = vuelo.FechaHoraLlegada.ToString("O"),
            DuracionMin = vuelo.DuracionMin,
            PrecioBase = (double)vuelo.PrecioBase,
            CapacidadTotal = vuelo.CapacidadTotal,
            EstadoVuelo = vuelo.EstadoVuelo,
            Estado = vuelo.Estado,
            Eliminado = false
        };
    }

    private static AsientoGrpcResponse MapAsiento(
        AsientoResponseDto asiento,
        bool success,
        string message)
    {
        return new AsientoGrpcResponse
        {
            Success = success,
            Message = message,
            IdAsiento = asiento.IdAsiento,
            IdVuelo = asiento.IdVuelo,
            NumeroAsiento = asiento.NumeroAsiento,
            Clase = asiento.Clase,
            Disponible = asiento.Disponible,
            PrecioExtra = (double)asiento.PrecioExtra,
            Posicion = asiento.Posicion ?? string.Empty,
            Estado = asiento.Estado,
            Eliminado = false
        };
    }
}
