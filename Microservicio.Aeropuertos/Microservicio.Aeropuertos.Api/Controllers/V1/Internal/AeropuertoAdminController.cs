using Asp.Versioning;
using Microservicio.Aeropuertos.Api.Model.Common;
using Microservicio.Aeropuertos.Api.Models.Common;
using Microservicio.Aeropuertos.Business.DTOs.Aeropuerto;
using Microservicio.Aeropuertos.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Microservicio.Aeropuertos.Api.Controllers.V1.Internal;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/aeropuertos")]
[Produces("application/json")]
[Authorize]
public class AeropuertoController : ControllerBase
{
    private readonly IAeropuertoService _aeropuertoService;

    public AeropuertoController(
        IAeropuertoService aeropuertoService)
    {
        _aeropuertoService =
            aeropuertoService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetPaged(
        [FromQuery] AeropuertoFilterDto filter)
    {
        var result =
            await _aeropuertoService.GetPagedAsync(filter);

        return Ok(
            ApiResponse<object>.Ok(
                result,
                "Consulta de aeropuertos realizada correctamente."));
    }

    [HttpGet("{id_aeropuerto:int}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<AeropuertoResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AeropuertoResponseDto>>> GetById(
        int id_aeropuerto)
    {
        var result =
            await _aeropuertoService.GetByIdAsync(
                id_aeropuerto);

        if (result is null)
        {
            return NotFound(
                ApiResponse<AeropuertoResponseDto>.Fail(
                    "Aeropuerto no encontrado."));
        }

        return Ok(
            ApiResponse<AeropuertoResponseDto>.Ok(
                result,
                "Aeropuerto obtenido correctamente."));
    }

    [HttpPost]
    [Authorize(Roles = "ADMINISTRADOR,AEROLINEA")]
    [ProducesResponseType(
        typeof(ApiResponse<AeropuertoResponseDto>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AeropuertoResponseDto>>> Create(
        [FromBody] AeropuertoRequestDto request)
    {
        var usuario =
            GetUsuario();

        var result =
            await _aeropuertoService.CreateAsync(
                request,
                usuario);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id_aeropuerto = result.IdAeropuerto,
                version = "1"
            },
            ApiResponse<AeropuertoResponseDto>.Ok(
                result,
                "Aeropuerto creado correctamente."));
    }

    [HttpPut("{id_aeropuerto:int}")]
    [Authorize(Roles = "ADMINISTRADOR,AEROLINEA")]
    [ProducesResponseType(
        typeof(ApiResponse<AeropuertoResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AeropuertoResponseDto>>> Update(
        int id_aeropuerto,
        [FromBody] AeropuertoUpdateRequestDto request)
    {
        var usuario =
            GetUsuario();

        var result =
            await _aeropuertoService.UpdateAsync(
                id_aeropuerto,
                request,
                usuario);

        if (result is null)
        {
            return NotFound(
                ApiResponse<AeropuertoResponseDto>.Fail(
                    "Aeropuerto no encontrado."));
        }

        return Ok(
            ApiResponse<AeropuertoResponseDto>.Ok(
                result,
                "Aeropuerto actualizado correctamente."));
    }

    [HttpDelete("{id_aeropuerto:int}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        int id_aeropuerto)
    {
        var usuario =
            GetUsuario();

        var result =
            await _aeropuertoService.DeleteAsync(
                id_aeropuerto,
                usuario);

        return Ok(
            ApiResponse<bool>.Ok(
                result,
                "Aeropuerto eliminado correctamente."));
    }

    private string GetUsuario()
    {
        var name =
            User?.Identity?.Name;

        if (!string.IsNullOrWhiteSpace(name))
        {
            return name.Trim();
        }

        var username =
            User?.FindFirst("username")?.Value;

        if (!string.IsNullOrWhiteSpace(username))
        {
            return username.Trim();
        }

        return "SYSTEM";
    }
}