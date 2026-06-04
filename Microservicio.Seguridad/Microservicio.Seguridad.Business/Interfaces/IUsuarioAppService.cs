using Microservicio.Seguridad.Business.DTOs.UsuarioApp;
using Microservicio.Seguridad.DataAccess.Common;

namespace Microservicio.Seguridad.Business.Interfaces;

public interface IUsuarioAppService
{
    Task<PagedResult<UsuarioAppResponseDto>> GetPagedAsync(UsuarioAppFilterDto filter);
    Task<UsuarioAppResponseDto?> GetByIdAsync(int idUsuario);
    Task<UsuarioAppResponseDto> CreateAsync(UsuarioAppRequestDto request, string creadoPorUsuario);
    Task<UsuarioAppResponseDto?> UpdateAsync(int idUsuario, UsuarioAppUpdateRequestDto request, string modificadoPorUsuario);
    Task<bool> DeleteAsync(int idUsuario, string modificadoPorUsuario);
}