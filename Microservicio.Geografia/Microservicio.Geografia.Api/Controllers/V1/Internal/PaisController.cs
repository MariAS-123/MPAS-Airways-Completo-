using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microservicio.Geografia.Api.Models.Common;
using Microservicio.Geografia.Business.DTOs.Pais;
using Microservicio.Geografia.Business.Interfaces;

namespace Microservicio.Geografia.Api.Controllers.V1.Internal;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/paises")]
[Produces("application/json")]
[Authorize]
public class PaisController : ControllerBase
{
    private readonly IPaisService _paisService;

    public PaisController(
        IPaisService paisService)
    {
        _paisService = paisService;
    }

    // ============================================================
    // GET: api/v1/paises
    // ============================================================
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetPaged(
        [FromQuery] PaisFilterDto filter)
    {
        var result =
            await _paisService.GetPagedAsync(filter);

        return Ok(
            ApiResponse<object>.Ok(result));
    }

    // ============================================================
    // GET: api/v1/paises/{id_pais}
    // ============================================================
    [HttpGet("{id_pais:int}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PaisResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PaisResponseDto>>> GetById(
        int id_pais)
    {
        var result =
            await _paisService.GetByIdAsync(id_pais);

        if (result is null)
        {
            return NotFound(
                ApiResponse<PaisResponseDto>.Fail(
                    "País no encontrado."));
        }

        return Ok(
            ApiResponse<PaisResponseDto>.Ok(result));
    }

    // ============================================================
    // POST: api/v1/paises
    // ============================================================
    [HttpPost]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ProducesResponseType(
        typeof(ApiResponse<PaisResponseDto>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<PaisResponseDto>>> Create(
        [FromBody] PaisRequestDto request)
    {
        var usuario =
            GetUsuario();

        var result =
            await _paisService.CreateAsync(
                request,
                usuario);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id_pais = result.IdPais,
                version = "1"
            },
            ApiResponse<PaisResponseDto>.Ok(
                result,
                "País creado correctamente."));
    }

    // ============================================================
    // PUT: api/v1/paises/{id_pais}
    // ============================================================
    [HttpPut("{id_pais:int}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ProducesResponseType(
        typeof(ApiResponse<PaisResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<PaisResponseDto>>> Update(
        int id_pais,
        [FromBody] PaisUpdateRequestDto request)
    {
        var usuario =
            GetUsuario();

        var result =
            await _paisService.UpdateAsync(
                id_pais,
                request,
                usuario);

        if (result is null)
        {
            return NotFound(
                ApiResponse<PaisResponseDto>.Fail(
                    "País no encontrado."));
        }

        return Ok(
            ApiResponse<PaisResponseDto>.Ok(
                result,
                "País actualizado correctamente."));
    }

    // ============================================================
    // DELETE: api/v1/paises/{id_pais}
    // ============================================================
    [HttpDelete("{id_pais:int}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        int id_pais)
    {
        var usuario =
            GetUsuario();

        var result =
            await _paisService.DeleteAsync(
                id_pais,
                usuario);

        return Ok(
            ApiResponse<bool>.Ok(
                result,
                "País eliminado correctamente."));
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