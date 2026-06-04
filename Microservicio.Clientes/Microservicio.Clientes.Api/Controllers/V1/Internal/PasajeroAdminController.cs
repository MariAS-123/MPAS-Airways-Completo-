using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microservicio.Clientes.Api.Models.Common;
using Microservicio.Clientes.Business.DTOs.Pasajero;
using Microservicio.Clientes.Business.Interfaces;

namespace Microservicio.Clientes.Api.Controllers.V1.Internal;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/pasajeros")]
[Produces("application/json")]
[Authorize]
public class PasajeroAdminController : ControllerBase
{
    private readonly IPasajeroService _pasajeroService;

    public PasajeroAdminController(IPasajeroService pasajeroService)
    {
        _pasajeroService = pasajeroService;
    }

    [HttpGet]
    [Authorize(Roles = "ADMINISTRADOR,AEROLINEA")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetPaged([FromQuery] PasajeroFilterDto filter)
    {
        var result = await _pasajeroService.GetPagedAsync(filter);
        return Ok(ApiResponse<object>.Ok(result, "Consulta de pasajeros realizada correctamente."));
    }

    [HttpGet("{id_pasajero:int}")]
    [Authorize(Roles = "ADMINISTRADOR,AEROLINEA,CLIENTE")]
    [ProducesResponseType(typeof(ApiResponse<PasajeroResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PasajeroResponseDto>>> GetById(int id_pasajero)
    {
        var result = await _pasajeroService.GetByIdAsync(id_pasajero, GetIdCliente(), GetRol());

        if (result is null)
            return NotFound(ApiResponse<PasajeroResponseDto>.Fail("Pasajero no encontrado."));

        return Ok(ApiResponse<PasajeroResponseDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "ADMINISTRADOR,AEROLINEA,CLIENTE")]
    [ProducesResponseType(typeof(ApiResponse<PasajeroResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PasajeroResponseDto>>> Create([FromBody] PasajeroRequestDto request)
    {
        if (GetRol() == "CLIENTE")
        {
            var idCliente = GetIdCliente();
            if (idCliente is null)
                return Unauthorized(ApiResponse<PasajeroResponseDto>.Fail(
                    "No se pudo identificar el cliente del token."));

            request.IdCliente = idCliente.Value;
        }

        var result = await _pasajeroService.CreateAsync(request, GetUsuario());

        return CreatedAtAction(
            nameof(GetById),
            new { id_pasajero = result.IdPasajero, version = "1" },
            ApiResponse<PasajeroResponseDto>.Ok(result, "Pasajero creado correctamente."));
    }

    [HttpPut("{id_pasajero:int}")]
    [Authorize(Roles = "ADMINISTRADOR,AEROLINEA")]
    [ProducesResponseType(typeof(ApiResponse<PasajeroResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PasajeroResponseDto>>> Update(
        int id_pasajero, [FromBody] PasajeroUpdateRequestDto request)
    {
        var result = await _pasajeroService.UpdateAsync(id_pasajero, request, GetUsuario());

        if (result is null)
            return NotFound(ApiResponse<PasajeroResponseDto>.Fail("Pasajero no encontrado."));

        return Ok(ApiResponse<PasajeroResponseDto>.Ok(result, "Pasajero actualizado correctamente."));
    }

    private string GetUsuario() =>
        User?.Identity?.Name ?? "SYSTEM";

    private int? GetIdCliente()
    {
        var claim = User.FindFirst("id_cliente")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string GetRol() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
}