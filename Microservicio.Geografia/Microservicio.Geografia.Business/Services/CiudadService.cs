using Microservicio.Geografia.Business.DTOs.Ciudad;
using Microservicio.Geografia.Business.Exceptions;
using Microservicio.Geografia.Business.Interfaces;
using Microservicio.Geografia.Business.Mappers;
using Microservicio.Geografia.Business.Validators;
using Microservicio.Geografia.DataManagement.Common;
using Microservicio.Geografia.DataManagement.Interfaces;
using Microservicio.Geografia.DataManagement.Models;

namespace Microservicio.Geografia.Business.Services;

public class CiudadService : ICiudadService
{
    private readonly ICiudadDataService _ciudadDataService;
    private readonly IPaisDataService _paisDataService;
    private readonly CiudadValidator _validator;

    public CiudadService(
        ICiudadDataService ciudadDataService,
        IPaisDataService paisDataService)
    {
        _ciudadDataService = ciudadDataService;
        _paisDataService = paisDataService;
        _validator = new CiudadValidator();
    }

    public async Task<DataPagedResult<CiudadResponseDto>> GetPagedAsync(
        CiudadFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        _validator.ValidateFilter(filter);

        var filtro =
            CiudadBusinessMapper.ToFiltroDataModel(filter);

        var result =
            await _ciudadDataService.GetPagedAsync(
                filtro,
                cancellationToken);

        return new DataPagedResult<CiudadResponseDto>
        {
            Items =
                CiudadBusinessMapper.ToResponseDtoList(result.Items),

            PaginaActual = result.PaginaActual,

            TamanoPagina = result.TamanoPagina,

            TotalRegistros = result.TotalRegistros
        };
    }

    public async Task<CiudadResponseDto?> GetByIdAsync(
        int idCiudad,
        CancellationToken cancellationToken = default)
    {
        if (idCiudad <= 0)
        {
            throw new ValidationException(
                "El id de la ciudad debe ser mayor que 0.");
        }

        var data =
            await _ciudadDataService.GetByIdAsync(
                idCiudad,
                cancellationToken);

        return data is null
            ? null
            : CiudadBusinessMapper.ToResponseDto(data);
    }

    public async Task<CiudadResponseDto> CreateAsync(
        CiudadRequestDto request,
        string creadoPorUsuario,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(creadoPorUsuario))
        {
            throw new UnauthorizedBusinessException(
                "No se pudo identificar el usuario creador.");
        }

        _validator.ValidateRequest(request);

        var pais =
            await _paisDataService.GetByIdAsync(
                request.IdPais,
                cancellationToken);

        if (pais is null)
        {
            throw new NotFoundException(
                "El país indicado no existe.");
        }

        if (pais.Eliminado || pais.Estado != "ACTIVO")
        {
            throw new BusinessException(
                "El país indicado está inactivo o eliminado.");
        }

        var existentes =
            await _ciudadDataService.GetPagedAsync(
                new CiudadFiltroDataModel
                {
                    IdPais = request.IdPais,
                    PaginaActual = 1,
                    TamanoPagina = 10000
                },
                cancellationToken);

        var nombre =
            request.Nombre
                .Trim()
                .ToUpperInvariant();

        if (existentes.Items.Any(x =>
            x.Nombre.Trim().ToUpperInvariant() == nombre))
        {
            throw new BusinessException(
                "Ya existe una ciudad con el mismo nombre en el país indicado.");
        }

        var dataModel =
            CiudadBusinessMapper.ToDataModel(
                request,
                creadoPorUsuario);

        var creada =
            await _ciudadDataService.CreateAsync(
                dataModel,
                cancellationToken);

        return CiudadBusinessMapper.ToResponseDto(creada);
    }

    public async Task<CiudadResponseDto?> UpdateAsync(
        int idCiudad,
        CiudadUpdateRequestDto request,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default)
    {
        if (idCiudad <= 0)
        {
            throw new ValidationException(
                "El id de la ciudad debe ser mayor que 0.");
        }

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
        {
            throw new UnauthorizedBusinessException(
                "No se pudo identificar el usuario modificador.");
        }

        _validator.ValidateUpdate(request);

        var actual =
            await _ciudadDataService.GetByIdAsync(
                idCiudad,
                cancellationToken);

        if (actual is null)
        {
            throw new NotFoundException(
                "Ciudad no encontrada.");
        }

        var pais =
            await _paisDataService.GetByIdAsync(
                request.IdPais,
                cancellationToken);

        if (pais is null)
        {
            throw new NotFoundException(
                "El país indicado no existe.");
        }

        if (pais.Eliminado || pais.Estado != "ACTIVO")
        {
            throw new BusinessException(
                "El país indicado está inactivo o eliminado.");
        }

        var existentes =
            await _ciudadDataService.GetPagedAsync(
                new CiudadFiltroDataModel
                {
                    IdPais = request.IdPais,
                    PaginaActual = 1,
                    TamanoPagina = 10000
                },
                cancellationToken);

        var nombre =
            request.Nombre
                .Trim()
                .ToUpperInvariant();

        if (existentes.Items.Any(x =>
            x.IdCiudad != idCiudad &&
            x.Nombre.Trim().ToUpperInvariant() == nombre))
        {
            throw new BusinessException(
                "Ya existe otra ciudad con el mismo nombre en el país indicado.");
        }

        var dataModel =
            CiudadBusinessMapper.ToDataModel(
                idCiudad,
                request);

        dataModel.ModificadoPorUsuario =
            modificadoPorUsuario.Trim();

        dataModel.FechaModificacionUtc =
            DateTime.UtcNow;

        var actualizada =
            await _ciudadDataService.UpdateAsync(
                dataModel,
                cancellationToken);

        return actualizada is null
            ? null
            : CiudadBusinessMapper.ToResponseDto(actualizada);
    }

    public async Task<bool> DeleteAsync(
        int idCiudad,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default)
    {
        if (idCiudad <= 0)
        {
            throw new ValidationException(
                "El id de la ciudad debe ser mayor que 0.");
        }

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
        {
            throw new UnauthorizedBusinessException(
                "No se pudo identificar el usuario modificador.");
        }

        var actual =
            await _ciudadDataService.GetByIdAsync(
                idCiudad,
                cancellationToken);

        if (actual is null)
        {
            throw new NotFoundException(
                "Ciudad no encontrada.");
        }

        return await _ciudadDataService.DeleteAsync(
            idCiudad,
            modificadoPorUsuario,
            cancellationToken);
    }
}