using Microservicio.Seguridad.DataAccess.Common;
using Microservicio.Seguridad.DataManagement.Models;

namespace Microservicio.Seguridad.DataManagement.Interfaces;

public interface IUsuarioRolDataService
{
    Task<PagedResult<UsuarioRolDataModel>> GetPagedAsync(UsuarioRolFiltroDataModel filtro);
    Task<UsuarioRolDataModel?> GetByIdAsync(int id);
    Task<UsuarioRolDataModel?> GetByUsuarioRolAsync(int idUsuario, int idRol);
    Task<UsuarioRolDataModel> CreateAsync(UsuarioRolDataModel model);
    Task<UsuarioRolDataModel?> UpdateAsync(UsuarioRolDataModel model);
    Task<bool> DeleteAsync(int id, string modificadoPorUsuario);
}