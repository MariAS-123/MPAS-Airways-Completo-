using Microservicio.Geografia.Business.DTOs.Ciudad;
using Microservicio.Geografia.DataManagement.Common;

namespace Microservicio.Geografia.Business.Interfaces;

public interface ICiudadService
{
    Task<DataPagedResult<CiudadResponseDto>> GetPagedAsync(
        CiudadFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<CiudadResponseDto?> GetByIdAsync(
        int idCiudad,
        CancellationToken cancellationToken = default);

    Task<CiudadResponseDto> CreateAsync(
        CiudadRequestDto request,
        string creadoPorUsuario,
        CancellationToken cancellationToken = default);

    Task<CiudadResponseDto?> UpdateAsync(
        int idCiudad,
        CiudadUpdateRequestDto request,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int idCiudad,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default);
}