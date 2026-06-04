// IPasajeroService.cs
using Microservicio.Clientes.Business.DTOs.Pasajero;
using Microservicio.Clientes.DataManagement.Common;

namespace Microservicio.Clientes.Business.Interfaces;

public interface IPasajeroService
{
    Task<DataPagedResult<PasajeroResponseDto>> GetPagedAsync(PasajeroFilterDto filter);
    Task<PasajeroResponseDto?> GetByIdAsync(int idPasajero, int? idClienteDelToken, string rolDelToken);
    Task<PasajeroResponseDto> CreateAsync(PasajeroRequestDto request, string creadoPorUsuario);
    Task<PasajeroResponseDto?> UpdateAsync(int idPasajero, PasajeroUpdateRequestDto request, string modificadoPorUsuario);
    Task<bool> DeleteAsync(int idPasajero, string modificadoPorUsuario);
}