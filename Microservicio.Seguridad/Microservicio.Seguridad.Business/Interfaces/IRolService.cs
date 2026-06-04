using Microservicio.Seguridad.Business.DTOs.Rol;
using Microservicio.Seguridad.DataAccess.Common;

namespace Microservicio.Seguridad.Business.Interfaces;

public interface IRolService
{
    Task<PagedResult<RolResponseDto>> GetPagedAsync(RolFilterDto filter);
    Task<RolResponseDto?> GetByIdAsync(int idRol);
    Task<RolResponseDto> CreateAsync(RolRequestDto request, string creadoPorUsuario);
    Task<RolResponseDto?> UpdateAsync(int idRol, RolUpdateRequestDto request, string modificadoPorUsuario);
    Task<bool> DeleteAsync(int idRol, string modificadoPorUsuario);
}