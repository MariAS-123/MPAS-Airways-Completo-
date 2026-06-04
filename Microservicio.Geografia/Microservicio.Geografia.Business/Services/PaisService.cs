using Microservicio.Geografia.Business.DTOs.Pais;
using Microservicio.Geografia.Business.Exceptions;
using Microservicio.Geografia.Business.Interfaces;
using Microservicio.Geografia.Business.Mappers;
using Microservicio.Geografia.Business.Validators;
using Microservicio.Geografia.DataManagement.Common;
using Microservicio.Geografia.DataManagement.Interfaces;
using Microservicio.Geografia.DataManagement.Models;

namespace Microservicio.Geografia.Business.Services;

public class PaisService : IPaisService
{
    private readonly IPaisDataService _paisDataService;
    private readonly PaisValidator _validator;

    public PaisService(IPaisDataService paisDataService)
    {
        _paisDataService = paisDataService;
        _validator = new PaisValidator();
    }

    public async Task<DataPagedResult<PaisResponseDto>> GetPagedAsync(
        PaisFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        _validator.ValidateFilter(filter);

        var filtro =
            PaisBusinessMapper.ToFiltroDataModel(filter);

        var result =
            await _paisDataService.GetPagedAsync(
                filtro,
                cancellationToken);

        return new DataPagedResult<PaisResponseDto>
        {
            Items =
                PaisBusinessMapper.ToResponseDtoList(result.Items),

            PaginaActual = result.PaginaActual,

            TamanoPagina = result.TamanoPagina,

            TotalRegistros = result.TotalRegistros
        };
    }

    public async Task<PaisResponseDto?> GetByIdAsync(
        int idPais,
        CancellationToken cancellationToken = default)
    {
        if (idPais <= 0)
        {
            throw new ValidationException(
                "El id del país debe ser mayor que 0.");
        }

        var data =
            await _paisDataService.GetByIdAsync(
                idPais,
                cancellationToken);

        return data is null
            ? null
            : PaisBusinessMapper.ToResponseDto(data);
    }

    public async Task<PaisResponseDto> CreateAsync(
        PaisRequestDto request,
        string creadoPorUsuario,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(creadoPorUsuario))
        {
            throw new UnauthorizedBusinessException(
                "No se pudo identificar el usuario creador.");
        }

        _validator.ValidateRequest(request);

        var duplicados =
            await _paisDataService.GetPagedAsync(
                new PaisFiltroDataModel
                {
                    PaginaActual = 1,
                    TamanoPagina = 10000
                },
                cancellationToken);

        var codigoIso2 =
            request.CodigoIso2
                .Trim()
                .ToUpperInvariant();

        var codigoIso3 =
            string.IsNullOrWhiteSpace(request.CodigoIso3)
                ? null
                : request.CodigoIso3
                    .Trim()
                    .ToUpperInvariant();

        var nombre =
            request.Nombre
                .Trim()
                .ToUpperInvariant();

        if (duplicados.Items.Any(x =>
            x.CodigoIso2.Trim().ToUpperInvariant() == codigoIso2))
        {
            throw new BusinessException(
                "Ya existe un país con el mismo código ISO2.");
        }

        if (!string.IsNullOrWhiteSpace(codigoIso3))
        {
            if (duplicados.Items.Any(x =>
                    !string.IsNullOrWhiteSpace(x.CodigoIso3) &&
                    x.CodigoIso3!.Trim().ToUpperInvariant() == codigoIso3))
            {
                throw new BusinessException(
                    "Ya existe un país con el mismo código ISO3.");
            }
        }

        if (duplicados.Items.Any(x =>
            x.Nombre.Trim().ToUpperInvariant() == nombre))
        {
            throw new BusinessException(
                "Ya existe un país con el mismo nombre.");
        }

        var dataModel =
            PaisBusinessMapper.ToDataModel(request);

        var creado =
            await _paisDataService.CreateAsync(
                dataModel,
                cancellationToken);

        return PaisBusinessMapper.ToResponseDto(creado);
    }

    public async Task<PaisResponseDto?> UpdateAsync(
        int idPais,
        PaisUpdateRequestDto request,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default)
    {
        if (idPais <= 0)
        {
            throw new ValidationException(
                "El id del país debe ser mayor que 0.");
        }

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
        {
            throw new UnauthorizedBusinessException(
                "No se pudo identificar el usuario modificador.");
        }

        _validator.ValidateUpdate(request);

        var actual =
            await _paisDataService.GetByIdAsync(
                idPais,
                cancellationToken);

        if (actual is null)
        {
            throw new NotFoundException(
                "País no encontrado.");
        }

        var duplicados =
            await _paisDataService.GetPagedAsync(
                new PaisFiltroDataModel
                {
                    PaginaActual = 1,
                    TamanoPagina = 10000
                },
                cancellationToken);

        var codigoIso2 =
            request.CodigoIso2
                .Trim()
                .ToUpperInvariant();

        var codigoIso3 =
            string.IsNullOrWhiteSpace(request.CodigoIso3)
                ? null
                : request.CodigoIso3
                    .Trim()
                    .ToUpperInvariant();

        var nombre =
            request.Nombre
                .Trim()
                .ToUpperInvariant();

        if (duplicados.Items.Any(x =>
            x.IdPais != idPais &&
            x.CodigoIso2.Trim().ToUpperInvariant() == codigoIso2))
        {
            throw new BusinessException(
                "Ya existe otro país con el mismo código ISO2.");
        }

        if (!string.IsNullOrWhiteSpace(codigoIso3))
        {
            if (duplicados.Items.Any(x =>
                    x.IdPais != idPais &&
                    !string.IsNullOrWhiteSpace(x.CodigoIso3) &&
                    x.CodigoIso3!.Trim().ToUpperInvariant() == codigoIso3))
            {
                throw new BusinessException(
                    "Ya existe otro país con el mismo código ISO3.");
            }
        }

        if (duplicados.Items.Any(x =>
            x.IdPais != idPais &&
            x.Nombre.Trim().ToUpperInvariant() == nombre))
        {
            throw new BusinessException(
                "Ya existe otro país con el mismo nombre.");
        }

        var dataModel =
            PaisBusinessMapper.ToDataModel(
                idPais,
                request);

        var actualizado =
            await _paisDataService.UpdateAsync(
                dataModel,
                cancellationToken);

        return actualizado is null
            ? null
            : PaisBusinessMapper.ToResponseDto(actualizado);
    }

    public async Task<bool> DeleteAsync(
        int idPais,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default)
    {
        if (idPais <= 0)
        {
            throw new ValidationException(
                "El id del país debe ser mayor que 0.");
        }

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
        {
            throw new UnauthorizedBusinessException(
                "No se pudo identificar el usuario modificador.");
        }

        var actual =
            await _paisDataService.GetByIdAsync(
                idPais,
                cancellationToken);

        if (actual is null)
        {
            throw new NotFoundException(
                "País no encontrado.");
        }

        return await _paisDataService.DeleteAsync(
            idPais,
            modificadoPorUsuario,
            cancellationToken);
    }
}