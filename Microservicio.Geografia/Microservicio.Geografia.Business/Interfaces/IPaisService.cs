using Microservicio.Geografia.Business.DTOs.Pais;
using Microservicio.Geografia.DataManagement.Common;

namespace Microservicio.Geografia.Business.Interfaces;

public interface IPaisService
{
    Task<DataPagedResult<PaisResponseDto>> GetPagedAsync(
        PaisFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<PaisResponseDto?> GetByIdAsync(
        int idPais,
        CancellationToken cancellationToken = default);

    Task<PaisResponseDto> CreateAsync(
        PaisRequestDto request,
        string creadoPorUsuario,
        CancellationToken cancellationToken = default);

    Task<PaisResponseDto?> UpdateAsync(
        int idPais,
        PaisUpdateRequestDto request,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int idPais,
        string modificadoPorUsuario,
        CancellationToken cancellationToken = default);
}