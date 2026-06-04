using Microservicio.Seguridad.Business.DTOs.Rol;
using Microservicio.Seguridad.Business.Exceptions;
using Microservicio.Seguridad.Business.Interfaces;
using Microservicio.Seguridad.Business.Mappers;
using Microservicio.Seguridad.Business.Validators;
using Microservicio.Seguridad.DataAccess.Common;
using Microservicio.Seguridad.DataManagement.Interfaces;
using Microservicio.Seguridad.DataManagement.Models;

namespace Microservicio.Seguridad.Business.Services;

public class RolService : IRolService
{
    private readonly IRolDataService _rolDataService;
    private readonly RolValidator _validator;

    public RolService(IRolDataService rolDataService)
    {
        _rolDataService = rolDataService;
        _validator = new RolValidator();
    }

    public async Task<PagedResult<RolResponseDto>> GetPagedAsync(RolFilterDto filter)
    {
        _validator.ValidateFilter(filter);

        var filtro = RolBusinessMapper.ToFiltroDataModel(filter);
        var result = await _rolDataService.GetPagedAsync(filtro);

        return PagedResult<RolResponseDto>.Crear(
            RolBusinessMapper.ToResponseDtoList(result.Items),
            result.TotalRegistros,
            result.PaginaActual,
            result.TamanoPagina);
    }

    public async Task<RolResponseDto?> GetByIdAsync(int idRol)
    {
        if (idRol <= 0)
            throw new ValidationException("El id del rol debe ser mayor que 0.");

        var data = await _rolDataService.GetByIdAsync(idRol);
        return data == null ? null : RolBusinessMapper.ToResponseDto(data);
    }

    public async Task<RolResponseDto> CreateAsync(RolRequestDto request, string creadoPorUsuario)
    {
        if (string.IsNullOrWhiteSpace(creadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario creador.");

        _validator.ValidateRequest(request);

        var existente = await _rolDataService.GetByNombreAsync(request.NombreRol);
        if (existente != null)
            throw new BusinessException("Ya existe un rol con el mismo nombre.");

        var dataModel = RolBusinessMapper.ToDataModel(request, creadoPorUsuario);
        var creado = await _rolDataService.CreateAsync(dataModel);

        return RolBusinessMapper.ToResponseDto(creado);
    }

    public async Task<RolResponseDto?> UpdateAsync(int idRol, RolUpdateRequestDto request, string modificadoPorUsuario)
    {
        if (idRol <= 0)
            throw new ValidationException("El id del rol debe ser mayor que 0.");

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario modificador.");

        _validator.ValidateUpdate(request);

        var actual = await _rolDataService.GetByIdAsync(idRol);
        if (actual == null)
            throw new NotFoundException("Rol no encontrado.");

        var existente = await _rolDataService.GetByNombreAsync(request.NombreRol);
        if (existente != null && existente.IdRol != idRol)
            throw new BusinessException("Ya existe otro rol con el mismo nombre.");

        var dataModel = RolBusinessMapper.ToDataModel(idRol, request, modificadoPorUsuario);
        var actualizado = await _rolDataService.UpdateAsync(dataModel);

        return actualizado == null ? null : RolBusinessMapper.ToResponseDto(actualizado);
    }

    public async Task<bool> DeleteAsync(int idRol, string modificadoPorUsuario)
    {
        if (idRol <= 0)
            throw new ValidationException("El id del rol debe ser mayor que 0.");

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario modificador.");

        var actual = await _rolDataService.GetByIdAsync(idRol);
        if (actual == null)
            throw new NotFoundException("Rol no encontrado.");

        return await _rolDataService.DeleteAsync(idRol, modificadoPorUsuario);
    }
}