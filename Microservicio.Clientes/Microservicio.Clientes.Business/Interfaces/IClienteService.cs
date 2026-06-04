// IClienteService.cs
using Microservicio.Clientes.Business.DTOs.Cliente;
using Microservicio.Clientes.DataManagement.Common;

namespace Microservicio.Clientes.Business.Interfaces;

public interface IClienteService
{
    Task<DataPagedResult<ClienteResponseDto>> GetPagedAsync(ClienteFilterDto filter);
    Task<ClienteResponseDto?> GetByIdAsync(int idCliente, int? idClienteDelToken, string rolDelToken);
    Task<ClienteResponseDto> CreateAsync(ClienteRequestDto request, string creadoPorUsuario);
    Task<ClienteResponseDto?> UpdateAsync(int idCliente, ClienteUpdateRequestDto request, string modificadoPorUsuario, int? idClienteDelToken, string rolDelToken);
    Task<bool> DeleteAsync(int idCliente, string modificadoPorUsuario);
}