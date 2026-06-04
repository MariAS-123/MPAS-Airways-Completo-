using System.Diagnostics;
using Asp.Versioning;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microservicio.ReservasF.Api.Models.Common;
using Microservicio.ReservasF.Business.Integrations.Interfaces;
using Microservicio.Vuelos.Grpc;

namespace Microservicio.ReservasF.Api.Controllers.V1.Internal;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/internal/grpc/vuelos")]
[Produces("application/json")]
[AllowAnonymous]
public class VuelosGrpcTestController : ControllerBase
{
    private readonly IVueloIntegrationService _vueloIntegrationService;
    private readonly VuelosGrpc.VuelosGrpcClient _grpcClient;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public VuelosGrpcTestController(
        IVueloIntegrationService vueloIntegrationService,
        VuelosGrpc.VuelosGrpcClient grpcClient,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _vueloIntegrationService = vueloIntegrationService;
        _grpcClient = grpcClient;
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Verifica conectividad gRPC con MS Vuelos.
    /// GET /api/v1/internal/grpc/vuelos/ping?id_vuelo=1
    /// </summary>
    [HttpGet("ping")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ping(
        [FromQuery(Name = "id_vuelo")] int idVuelo = 1,
        CancellationToken cancellationToken = default)
    {
       // if (!_environment.IsDevelopment())
         //   return NotFound();

        var grpcUrl = _configuration["Integrations:Vuelos:GrpcUrl"] ?? "(no configurada)";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var vuelo = await _vueloIntegrationService.ObtenerVueloAsync(idVuelo, cancellationToken);
            stopwatch.Stop();

            return Ok(ApiResponse<object>.Ok(new
            {
                conectado = true,
                grpcUrl,
                latenciaMs = stopwatch.ElapsedMilliseconds,
                idVueloConsultado = idVuelo,
                vueloEncontrado = vuelo != null,
                vuelo = vuelo
            }, "Conexión gRPC con MS Vuelos exitosa."));
        }
        catch (RpcException ex)
        {
            stopwatch.Stop();

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Fail(
                    "No se pudo conectar con MS Vuelos via gRPC.",
                    new[]
                    {
                        $"GrpcUrl: {grpcUrl}",
                        $"StatusCode: {ex.StatusCode}",
                        $"Detalle: {ex.Status.Detail}",
                        $"LatenciaMs: {stopwatch.ElapsedMilliseconds}"
                    }));
        }
    }

    /// <summary>
    /// Prueba los RPC GetVuelo, ValidarVuelo y GetAsientosByVuelo.
    /// GET /api/v1/internal/grpc/vuelos/test?id_vuelo=1
    /// </summary>
    [HttpGet("test")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Test(
        [FromQuery(Name = "id_vuelo")] int idVuelo = 1,
        CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var grpcUrl = _configuration["Integrations:Vuelos:GrpcUrl"] ?? "(no configurada)";
        var stopwatch = Stopwatch.StartNew();
        var pruebas = new List<object>();

        try
        {
            var getVuelo = await _grpcClient.GetVueloAsync(
                new GetVueloRequest { IdVuelo = idVuelo },
                cancellationToken: cancellationToken);

            pruebas.Add(new
            {
                rpc = "GetVuelo",
                exito = getVuelo.Success,
                mensaje = getVuelo.Message,
                numeroVuelo = getVuelo.NumeroVuelo
            });

            var validarVuelo = await _grpcClient.ValidarVueloAsync(
                new GetVueloRequest { IdVuelo = idVuelo },
                cancellationToken: cancellationToken);

            pruebas.Add(new
            {
                rpc = "ValidarVuelo",
                exito = validarVuelo.Success,
                mensaje = validarVuelo.Message,
                estadoVuelo = validarVuelo.EstadoVuelo
            });

            var asientos = await _grpcClient.GetAsientosByVueloAsync(
                new GetVueloRequest { IdVuelo = idVuelo },
                cancellationToken: cancellationToken);

            pruebas.Add(new
            {
                rpc = "GetAsientosByVuelo",
                exito = asientos.Success,
                mensaje = asientos.Message,
                cantidadAsientos = asientos.Asientos.Count
            });

            stopwatch.Stop();

            var todasExitosas = pruebas.All(p =>
            {
                var prop = p.GetType().GetProperty("exito");
                return prop?.GetValue(p) is true;
            });

            return Ok(ApiResponse<object>.Ok(new
            {
                conectado = true,
                grpcUrl,
                latenciaTotalMs = stopwatch.ElapsedMilliseconds,
                idVueloConsultado = idVuelo,
                todasLasPruebasExitosas = todasExitosas,
                pruebas
            }, "Prueba gRPC completada."));
        }
        catch (RpcException ex)
        {
            stopwatch.Stop();

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Fail(
                    "Falló la prueba gRPC con MS Vuelos.",
                    new[]
                    {
                        $"GrpcUrl: {grpcUrl}",
                        $"StatusCode: {ex.StatusCode}",
                        $"Detalle: {ex.Status.Detail}",
                        $"LatenciaMs: {stopwatch.ElapsedMilliseconds}"
                    }));
        }
    }
}
