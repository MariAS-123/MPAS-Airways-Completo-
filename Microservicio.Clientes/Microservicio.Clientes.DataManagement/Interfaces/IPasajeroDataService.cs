// IPasajeroDataService.cs
using Microservicio.Clientes.DataManagement.Common;
using Microservicio.Clientes.DataManagement.Models;

namespace Microservicio.Clientes.DataManagement.Interfaces;

public interface IPasajeroDataService
{
    Task<DataPagedResult<PasajeroDataModel>> GetPagedAsync(PasajeroFiltroDataModel filtro);
    Task<PasajeroDataModel?> GetByIdAsync(int id);
    Task<PasajeroDataModel> CreateAsync(PasajeroDataModel model);
    Task<PasajeroDataModel?> UpdateAsync(PasajeroDataModel model);
    Task<bool> DeleteAsync(int id, string modificadoPorUsuario);
}