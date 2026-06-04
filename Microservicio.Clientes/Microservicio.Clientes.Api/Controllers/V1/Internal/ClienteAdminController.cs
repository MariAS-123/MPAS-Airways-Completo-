using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microservicio.Clientes.Api.Models.Common;
using Microservicio.Clientes.Business.DTOs.Cliente;
using Microservicio.Clientes.Business.Interfaces;

namespace Microservicio.Clientes.Api.Controllers.V1.Internal;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/clientes")]
[Produces("application/json")]
[Authorize]
public class ClienteAdminController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClienteAdminController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    [HttpGet]
    [Authorize(Roles = "ADMINISTRADOR,AEROLINEA")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetPaged([FromQuery] ClienteFilterDto filter)
    {
        var result = await _clienteService.GetPagedAsync(filter);
        return Ok(ApiResponse<object>.Ok(result, "Consulta de clientes realizada correctamente."));
    }

    [HttpGet("{id_cliente:int}")]
    [Authorize(Roles = "ADMINISTRADOR,AEROLINEA,CLIENTE")]
    [ProducesResponseType(typeof(ApiResponse<ClienteResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ClienteResponseDto>>> GetById(int id_cliente)
    {
        var result = await _clienteService.GetByIdAsync(id_cliente, GetIdCliente(), GetRol());

        if (result is null)
            return NotFound(ApiResponse<ClienteResponseDto>.Fail("Cliente no encontrado."));

        return Ok(ApiResponse<ClienteResponseDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "ADMINISTRADOR,AEROLINEA")]
    [ProducesResponseType(typeof(ApiResponse<ClienteResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ClienteResponseDto>>> Create([FromBody] ClienteRequestDto request)
    {
        var result = await _clienteService.CreateAsync(request, GetUsuario());

        return CreatedAtAction(
            nameof(GetById),
            new { id_cliente = result.IdCliente, version = "1" },
            ApiResponse<ClienteResponseDto>.Ok(result, "Cliente creado correctamente."));
    }

    [HttpPut("{id_cliente:int}")]
    [Authorize(Roles = "ADMINISTRADOR,CLIENTE")]
    [ProducesResponseType(typeof(ApiResponse<ClienteResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ClienteResponseDto>>> Update(
        int id_cliente, [FromBody] ClienteUpdateRequestDto request)
    {
        var result = await _clienteService.UpdateAsync(
            id_cliente, request, GetUsuario(), GetIdCliente(), GetRol());

        if (result is null)
            return NotFound(ApiResponse<ClienteResponseDto>.Fail("Cliente no encontrado."));

        return Ok(ApiResponse<ClienteResponseDto>.Ok(result, "Cliente actualizado correctamente."));
    }

    [HttpDelete("{id_cliente:int}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id_cliente)
    {
        var result = await _clienteService.DeleteAsync(id_cliente, GetUsuario());
        return Ok(ApiResponse<bool>.Ok(result, "Cliente eliminado correctamente."));
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