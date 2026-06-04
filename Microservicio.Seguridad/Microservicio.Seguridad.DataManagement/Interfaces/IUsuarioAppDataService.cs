using Microservicio.Seguridad.DataAccess.Common;
using Microservicio.Seguridad.DataManagement.Models;

namespace Microservicio.Seguridad.DataManagement.Interfaces;

public interface IUsuarioAppDataService
{
    Task<PagedResult<UsuarioAppDataModel>> GetPagedAsync(UsuarioAppFiltroDataModel filtro);
    Task<UsuarioAppDataModel?> GetByIdAsync(int id);
    Task<UsuarioAppDataModel?> GetByUsernameAsync(string username);
    Task<UsuarioAppDataModel> CreateAsync(UsuarioAppDataModel model);
    Task<UsuarioAppDataModel?> UpdateAsync(UsuarioAppDataModel model);
    Task<bool> DeleteAsync(int id, string modificadoPorUsuario);
}