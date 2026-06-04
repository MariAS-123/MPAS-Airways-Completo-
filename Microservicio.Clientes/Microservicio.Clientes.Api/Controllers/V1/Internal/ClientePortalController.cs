using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microservicio.Clientes.Api.Models.Common;
using Microservicio.Clientes.Business.DTOs.Cliente;
using Microservicio.Clientes.Business.Interfaces;

namespace Microservicio.Clientes.Api.Controllers.V1.Internal;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/clientes/portal")]
[Produces("application/json")]
public class ClientePortalController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClientePortalController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    // POST — registro público de cliente (no requiere autenticación)
    [HttpPost("registro")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ClienteResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ClienteResponseDto>>> Registrar(
        [FromBody] ClienteRequestDto request)
    {
        // ✅ "SELF_REGISTER" como auditoría — no hay usuario autenticado
        var result = await _clienteService.CreateAsync(request, "SELF_REGISTER");

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<ClienteResponseDto>.Ok(result, "Cliente registrado correctamente."));
    }

    // GET — cliente ve su propio perfil
    [HttpGet("mi-perfil")]
    [Authorize(Roles = "CLIENTE")]
    [ProducesResponseType(typeof(ApiResponse<ClienteResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ClienteResponseDto>>> MiPerfil()
    {
        var idCliente = GetIdCliente();
        if (idCliente is null)
            return Unauthorized(ApiResponse<ClienteResponseDto>.Fail(
                "No se pudo identificar el cliente del token."));

        var result = await _clienteService.GetByIdAsync(idCliente.Value, idCliente, "CLIENTE");

        if (result is null)
            return NotFound(ApiResponse<ClienteResponseDto>.Fail("Cliente no encontrado."));

        return Ok(ApiResponse<ClienteResponseDto>.Ok(result));
    }

    // PUT — cliente actualiza su propio perfil
    [HttpPut("mi-perfil")]
    [Authorize(Roles = "CLIENTE")]
    [ProducesResponseType(typeof(ApiResponse<ClienteResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ClienteResponseDto>>> ActualizarMiPerfil(
        [FromBody] ClienteUpdateRequestDto request)
    {
        var idCliente = GetIdCliente();
        if (idCliente is null)
            return Unauthorized(ApiResponse<ClienteResponseDto>.Fail(
                "No se pudo identificar el cliente del token."));

        var result = await _clienteService.UpdateAsync(
            idCliente.Value, request, GetUsuario(), idCliente, "CLIENTE");

        if (result is null)
            return NotFound(ApiResponse<ClienteResponseDto>.Fail("Cliente no encontrado."));

        return Ok(ApiResponse<ClienteResponseDto>.Ok(result, "Perfil actualizado correctamente."));
    }

    private string GetUsuario() =>
        User?.Identity?.Name ?? "SELF_REGISTER";

    private int? GetIdCliente()
    {
        var claim = User.FindFirst("id_cliente")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
