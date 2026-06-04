// IClienteDataService.cs
using Microservicio.Clientes.DataManagement.Common;
using Microservicio.Clientes.DataManagement.Models;

namespace Microservicio.Clientes.DataManagement.Interfaces;

public interface IClienteDataService
{
    Task<DataPagedResult<ClienteDataModel>> GetPagedAsync(ClienteFiltroDataModel filtro);
    Task<ClienteDataModel?> GetByIdAsync(int id);
    Task<ClienteDataModel> CreateAsync(ClienteDataModel model);
    Task<ClienteDataModel?> UpdateAsync(ClienteDataModel model);
    Task<bool> DeleteAsync(int id, string modificadoPorUsuario);
}