using Asp.Versioning;
using Microservicio.Seguridad.Api.Models.Common;
using Microservicio.Seguridad.Business.DTOs.Internal;
using Microservicio.Seguridad.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Microservicio.Seguridad.Api.Controllers.V1.Internal;

/// <summary>
/// Endpoints internos de Seguridad exclusivos para el Bus de Integración.
/// NO exponer en Swagger público ni en documentación externa.
/// Solo accesible desde la red interna / Bus.
///
/// Ruta base: /api/v1/internal/seguridad
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/internal/seguridad")]
[ApiExplorerSettings(IgnoreApi = true)] // Oculto en Swagger público
public class InternalSeguridadController : ControllerBase
{
    private readonly IInternalSeguridadService _internalService;
    private readonly ILogger<InternalSeguridadController> _logger;

    public InternalSeguridadController(
        IInternalSeguridadService internalService,
        ILogger<InternalSeguridadController> logger)
    {
        _internalService = internalService;
        _logger = logger;
    }

    /// <summary>
    /// Crea un usuario de aplicación en MS Seguridad con el IdCliente real vinculado.
    ///
    /// Invocado exclusivamente por el Bus de Integración durante la saga de
    /// registro de cliente. Reemplaza el endpoint público register-cliente
    /// que devolvía IdCliente = 0.
    ///
    /// Comportamiento idempotente: si el usuario ya existe con el mismo IdCliente,
    /// devuelve los datos existentes sin duplicar registros.
    ///
    /// POST /api/v1/internal/seguridad/users/create-for-client
    /// </summary>
    [HttpPost("users/create-for-client")]
    [AllowAnonymous] // El Bus llama internamente; en producción usar mTLS o API Key de red interna
    [ProducesResponseType(typeof(ApiResponse<CreateUserForClientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateUserForClient(
        [FromBody] CreateUserForClientRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiErrorResponse
            {
                Success = false,
                Message = "Datos de entrada inválidos.",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        _logger.LogInformation(
            "[Internal] CreateUserForClient request recibido. " +
            "IdCliente={IdCliente} Username={Username} CorrelationId={CorrelationId}",
            request.IdCliente, request.Username, request.CorrelationId);

        var result = await _internalService.CreateUserForClientAsync(request);

        return Ok(new ApiResponse<CreateUserForClientResponse>
        {
            Success = true,
            Message = "Usuario de cliente creado correctamente.",
            Data = result,
            Errors = []
        });
    }
}