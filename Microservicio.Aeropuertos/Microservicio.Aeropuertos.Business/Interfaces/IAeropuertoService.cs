using Microservicio.Aeropuertos.Business.DTOs.Aeropuerto;

using Microservicio.Aeropuertos.DataManagement.Common;

namespace Microservicio.Aeropuertos.Business.Interfaces;

public interface IAeropuertoService
{
    Task<DataPagedResult<AeropuertoResponseDto>> GetPagedAsync(
        AeropuertoFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<AeropuertoResponseDto?> GetByIdAsync(
        int idAeropuerto,
        CancellationToken cancellationToken = default);

    Task<AeropuertoResponseDto?> GetByCodigoIataAsync(
        string codigoIata,
        CancellationToken cancellationToken = default);

    Task<AeropuertoResponseDto> CreateAsync(
        AeropuertoRequestDto request,
        string creadoPorUsuario,
        CancellationToken cancellationToken = default);

    Task<AeropuertoResponseDto?> UpdateAsync(
        int idAeropuerto,
        AeropuertoUpdateRequestDto request,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int idAeropuerto,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default);
}