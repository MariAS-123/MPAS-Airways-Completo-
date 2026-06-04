using Microservicio.Seguridad.Business.DTOs.UsuarioRol;
using Microservicio.Seguridad.DataAccess.Common;

namespace Microservicio.Seguridad.Business.Interfaces;

public interface IUsuarioRolService
{
    Task<PagedResult<UsuarioRolResponseDto>> GetPagedAsync(UsuarioRolFilterDto filter);
    Task<UsuarioRolResponseDto?> GetByIdAsync(int idUsuarioRol);
    Task<UsuarioRolResponseDto> CreateAsync(UsuarioRolRequestDto request, string creadoPorUsuario);
    Task<bool> DeleteAsync(int idUsuarioRol, string modificadoPorUsuario);
}