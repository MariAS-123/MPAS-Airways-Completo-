using Microservicio.Seguridad.Business.DTOs.UsuarioRol;
using Microservicio.Seguridad.Business.Exceptions;
using Microservicio.Seguridad.Business.Interfaces;
using Microservicio.Seguridad.Business.Mappers;
using Microservicio.Seguridad.Business.Validators;
using Microservicio.Seguridad.DataAccess.Common;
using Microservicio.Seguridad.DataManagement.Interfaces;
using Microservicio.Seguridad.DataManagement.Models;

namespace Microservicio.Seguridad.Business.Services;

public class UsuarioRolService : IUsuarioRolService
{
    private readonly IUsuarioRolDataService _usuarioRolDataService;
    private readonly IUsuarioAppDataService _usuarioAppDataService;
    private readonly IRolDataService _rolDataService;
    private readonly UsuarioRolValidator _validator;

    public UsuarioRolService(
        IUsuarioRolDataService usuarioRolDataService,
        IUsuarioAppDataService usuarioAppDataService,
        IRolDataService rolDataService)
    {
        _usuarioRolDataService = usuarioRolDataService;
        _usuarioAppDataService = usuarioAppDataService;
        _rolDataService = rolDataService;
        _validator = new UsuarioRolValidator();
    }

    public async Task<PagedResult<UsuarioRolResponseDto>> GetPagedAsync(UsuarioRolFilterDto filter)
    {
        _validator.ValidateFilter(filter);

        var filtro = UsuarioRolBusinessMapper.ToFiltroDataModel(filter);
        var result = await _usuarioRolDataService.GetPagedAsync(filtro);

        return PagedResult<UsuarioRolResponseDto>.Crear(
            UsuarioRolBusinessMapper.ToResponseDtoList(result.Items),
            result.TotalRegistros,
            result.PaginaActual,
            result.TamanoPagina);
    }

    public async Task<UsuarioRolResponseDto?> GetByIdAsync(int idUsuarioRol)
    {
        if (idUsuarioRol <= 0)
            throw new ValidationException("El id usuario-rol debe ser mayor que 0.");

        var data = await _usuarioRolDataService.GetByIdAsync(idUsuarioRol);
        return data == null ? null : UsuarioRolBusinessMapper.ToResponseDto(data);
    }

    public async Task<UsuarioRolResponseDto> CreateAsync(UsuarioRolRequestDto request, string creadoPorUsuario)
    {
        if (string.IsNullOrWhiteSpace(creadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario creador.");

        _validator.ValidateRequest(request);

        var usuario = await _usuarioAppDataService.GetByIdAsync(request.IdUsuario);
        if (usuario == null)
            throw new NotFoundException("El usuario indicado no existe.");
        if (!usuario.Activo || usuario.EstadoUsuario != "ACT")
            throw new BusinessException("El usuario indicado está inactivo o eliminado.");

        var rol = await _rolDataService.GetByIdAsync(request.IdRol);
        if (rol == null)
            throw new NotFoundException("El rol indicado no existe.");
        if (!rol.Activo || rol.EstadoRol != "ACT")
            throw new BusinessException("El rol indicado está inactivo o eliminado.");

        var existente = await _usuarioRolDataService.GetByUsuarioRolAsync(request.IdUsuario, request.IdRol);
        if (existente != null)
            throw new BusinessException("El usuario ya tiene asignado ese rol.");

        var dataModel = UsuarioRolBusinessMapper.ToDataModel(request, creadoPorUsuario);
        var creado = await _usuarioRolDataService.CreateAsync(dataModel);

        return UsuarioRolBusinessMapper.ToResponseDto(creado);
    }

    public async Task<bool> DeleteAsync(int idUsuarioRol, string modificadoPorUsuario)
    {
        if (idUsuarioRol <= 0)
            throw new ValidationException("El id usuario-rol debe ser mayor que 0.");

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario modificador.");

        var actual = await _usuarioRolDataService.GetByIdAsync(idUsuarioRol);
        if (actual == null)
            throw new NotFoundException("Asignación usuario-rol no encontrada.");

        return await _usuarioRolDataService.DeleteAsync(idUsuarioRol, modificadoPorUsuario);
    }
}