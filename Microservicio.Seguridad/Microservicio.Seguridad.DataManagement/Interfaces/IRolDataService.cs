using Microservicio.Seguridad.DataAccess.Common;
using Microservicio.Seguridad.DataManagement.Models;

namespace Microservicio.Seguridad.DataManagement.Interfaces;

public interface IRolDataService
{
    Task<PagedResult<RolDataModel>> GetPagedAsync(RolFiltroDataModel filtro);
    Task<RolDataModel?> GetByIdAsync(int id);
    Task<RolDataModel?> GetByNombreAsync(string nombre);
    Task<RolDataModel> CreateAsync(RolDataModel model);
    Task<RolDataModel?> UpdateAsync(RolDataModel model);
    Task<bool> DeleteAsync(int id, string modificadoPorUsuario);
}