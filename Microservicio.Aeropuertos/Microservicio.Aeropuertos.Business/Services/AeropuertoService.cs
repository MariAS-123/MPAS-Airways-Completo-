using Microservicio.Aeropuertos.Business.DTOs.Aeropuerto;
using Microservicio.Aeropuertos.Business.Exceptions;
using Microservicio.Aeropuertos.Business.Integrations.Interfaces;
using Microservicio.Aeropuertos.Business.Interfaces;
using Microservicio.Aeropuertos.Business.Mappers;
using Microservicio.Aeropuertos.Business.Validators;

using Microservicio.Aeropuertos.DataManagement.Common;
using Microservicio.Aeropuertos.DataManagement.Interfaces;
using Microservicio.Aeropuertos.DataManagement.Models;

namespace Microservicio.Aeropuertos.Business.Services;

public class AeropuertoService : IAeropuertoService
{
    private readonly IAeropuertoDataService _aeropuertoDataService;

    private readonly IGeografiaIntegrationService _geografiaIntegrationService;

    private readonly AeropuertoValidator _validator;

    public AeropuertoService(
        IAeropuertoDataService aeropuertoDataService,
        IGeografiaIntegrationService geografiaIntegrationService)
    {
        _aeropuertoDataService = aeropuertoDataService;

        _geografiaIntegrationService = geografiaIntegrationService;

        _validator = new AeropuertoValidator();
    }

    public async Task<DataPagedResult<AeropuertoResponseDto>> GetPagedAsync(
        AeropuertoFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        _validator.ValidateFilter(filter);

        var filtro =
            AeropuertoBusinessMapper.ToFiltroDataModel(filter);

        var result =
            await _aeropuertoDataService.GetPagedAsync(
                filtro,
                cancellationToken);

        return new DataPagedResult<AeropuertoResponseDto>
        {
            Items =
                AeropuertoBusinessMapper
                    .ToResponseDtoList(result.Items),

            PageNumber =
                result.PageNumber,

            PageSize =
                result.PageSize,

            TotalRecords =
                result.TotalRecords
        };
    }

    public async Task<AeropuertoResponseDto?> GetByIdAsync(
        int idAeropuerto,
        CancellationToken cancellationToken = default)
    {
        if (idAeropuerto <= 0)
        {
            throw new ValidationException(
                "El id del aeropuerto debe ser mayor que 0.");
        }

        var data =
            await _aeropuertoDataService.GetByIdAsync(
                idAeropuerto,
                cancellationToken);

        return data is null
            ? null
            : AeropuertoBusinessMapper.ToResponseDto(data);
    }

    public async Task<AeropuertoResponseDto?> GetByCodigoIataAsync(
        string codigoIata,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoIata))
        {
            throw new ValidationException(
                "El código IATA es obligatorio.");
        }

        var data =
            await _aeropuertoDataService.GetByCodigoIataAsync(
                codigoIata.Trim().ToUpperInvariant(),
                cancellationToken);

        return data is null
            ? null
            : AeropuertoBusinessMapper.ToResponseDto(data);
    }

    public async Task<AeropuertoResponseDto> CreateAsync(
        AeropuertoRequestDto request,
        string creadoPorUsuario,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(creadoPorUsuario))
        {
            throw new UnauthorizedBusinessException(
                "No se pudo identificar el usuario creador.");
        }

        _validator.ValidateRequest(request);

        // ============================================
        // Validar País en MS Geografía
        // ============================================

        var pais =
            await _geografiaIntegrationService
                .GetPaisAsync(
                    request.IdPais,
                    cancellationToken);

        if (pais is null)
        {
            throw new NotFoundException(
                "El país indicado no existe.");
        }

        if (!string.Equals(
                pais.Estado,
                "ACTIVO",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "El país indicado está inactivo.");
        }

        // ============================================
        // Validar Ciudad en MS Geografía
        // ============================================

        if (request.IdCiudad.HasValue)
        {
            var ciudad =
                await _geografiaIntegrationService
                    .GetCiudadAsync(
                        request.IdCiudad.Value,
                        cancellationToken);

            if (ciudad is null)
            {
                throw new NotFoundException(
                    "La ciudad indicada no existe.");
            }

            if (!string.Equals(
                    ciudad.Estado,
                    "ACTIVO",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(
                    "La ciudad indicada está inactiva.");
            }

            if (ciudad.IdPais != request.IdPais)
            {
                throw new BusinessException(
                    "La ciudad indicada no pertenece al país indicado.");
            }
        }

        // ============================================
        // Validar duplicado IATA
        // ============================================

        var existente =
            await _aeropuertoDataService
                .GetByCodigoIataAsync(
                    request.CodigoIata,
                    cancellationToken);

        if (existente is not null)
        {
            throw new BusinessException(
                "AEROPUERTO_DUPLICADO",
                "Ya existe un aeropuerto con el mismo código IATA.",
                409);
        }

        // ============================================
        // Crear
        // ============================================

        var dataModel =
            AeropuertoBusinessMapper.ToDataModel(
                request,
                creadoPorUsuario);

        var creado =
            await _aeropuertoDataService.CreateAsync(
                dataModel,
                cancellationToken);

        return AeropuertoBusinessMapper
            .ToResponseDto(creado);
    }

    public async Task<AeropuertoResponseDto?> UpdateAsync(
        int idAeropuerto,
        AeropuertoUpdateRequestDto request,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default)
    {
        if (idAeropuerto <= 0)
        {
            throw new ValidationException(
                "El id del aeropuerto debe ser mayor que 0.");
        }

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
        {
            throw new UnauthorizedBusinessException(
                "No se pudo identificar el usuario modificador.");
        }

        _validator.ValidateUpdate(request);

        var actual =
            await _aeropuertoDataService.GetByIdAsync(
                idAeropuerto,
                cancellationToken);

        if (actual is null)
        {
            throw new NotFoundException(
                "Aeropuerto no encontrado.");
        }

        // ============================================
        // Validar País
        // ============================================

        var pais =
            await _geografiaIntegrationService
                .GetPaisAsync(
                    request.IdPais,
                    cancellationToken);

        if (pais is null)
        {
            throw new NotFoundException(
                "El país indicado no existe.");
        }

        if (!string.Equals(
                pais.Estado,
                "ACTIVO",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "El país indicado está inactivo.");
        }

        // ============================================
        // Validar Ciudad
        // ============================================

        if (request.IdCiudad.HasValue)
        {
            var ciudad =
                await _geografiaIntegrationService
                    .GetCiudadAsync(
                        request.IdCiudad.Value,
                        cancellationToken);

            if (ciudad is null)
            {
                throw new NotFoundException(
                    "La ciudad indicada no existe.");
            }

            if (!string.Equals(
                    ciudad.Estado,
                    "ACTIVO",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(
                    "La ciudad indicada está inactiva.");
            }

            if (ciudad.IdPais != request.IdPais)
            {
                throw new BusinessException(
                    "La ciudad indicada no pertenece al país indicado.");
            }
        }

        // ============================================
        // Validar duplicado IATA
        // ============================================

        var existente =
            await _aeropuertoDataService
                .GetByCodigoIataAsync(
                    request.CodigoIata,
                    cancellationToken);

        if (existente is not null &&
            existente.IdAeropuerto != idAeropuerto)
        {
            throw new BusinessException(
                "AEROPUERTO_DUPLICADO",
                "Ya existe otro aeropuerto con el mismo código IATA.",
                409);
        }

        // ============================================
        // Actualizar
        // ============================================

        var dataModel =
            AeropuertoBusinessMapper.ToDataModel(
                idAeropuerto,
                request);

        dataModel.ModificadoPorUsuario =
            modificadoPorUsuario.Trim();

        dataModel.FechaModificacionUtc =
            DateTime.UtcNow;

        var actualizado =
            await _aeropuertoDataService.UpdateAsync(
                dataModel,
                cancellationToken);

        return actualizado is null
            ? null
            : AeropuertoBusinessMapper.ToResponseDto(actualizado);
    }

    public async Task<bool> DeleteAsync(
        int idAeropuerto,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default)
    {
        if (idAeropuerto <= 0)
        {
            throw new ValidationException(
                "El id del aeropuerto debe ser mayor que 0.");
        }

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
        {
            throw new UnauthorizedBusinessException(
                "No se pudo identificar el usuario modificador.");
        }

        var actual =
            await _aeropuertoDataService.GetByIdAsync(
                idAeropuerto,
                cancellationToken);

        if (actual is null)
        {
            throw new NotFoundException(
                "Aeropuerto no encontrado.");
        }

        return await _aeropuertoDataService.DeleteAsync(
            idAeropuerto,
            modificadoPorUsuario,
            cancellationToken);
    }
}