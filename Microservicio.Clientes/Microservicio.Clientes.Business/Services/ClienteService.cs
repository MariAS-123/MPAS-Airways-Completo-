using Microservicio.Clientes.Business.DTOs.Cliente;
using Microservicio.Clientes.Business.Exceptions;
using Microservicio.Clientes.Business.Integrations;
using Microservicio.Clientes.Business.Integrations.Interfaces;
using Microservicio.Clientes.Business.Interfaces;
using Microservicio.Clientes.Business.Mappers;
using Microservicio.Clientes.Business.Validators;
using Microservicio.Clientes.DataManagement.Common;
using Microservicio.Clientes.DataManagement.Interfaces;
using Microservicio.Clientes.DataManagement.Models;

namespace Microservicio.Clientes.Business.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteDataService _clienteDataService;
    private readonly ISeguridadIntegrationService _seguridadService;
    private readonly ClienteValidator _validator;

    // ✅ ICiudadDataService e IPaisDataService eliminados — están en otros MS
    public ClienteService(
        IClienteDataService clienteDataService,
        ISeguridadIntegrationService seguridadService)
    {
        _clienteDataService = clienteDataService;
        _seguridadService = seguridadService;
        _validator = new ClienteValidator();
    }

    public async Task<DataPagedResult<ClienteResponseDto>> GetPagedAsync(ClienteFilterDto filter)
    {
        _validator.ValidateFilter(filter);

        var filtro = ClienteBusinessMapper.ToFiltroDataModel(filter);
        var result = await _clienteDataService.GetPagedAsync(filtro);

        return new DataPagedResult<ClienteResponseDto>
        {
            Items = ClienteBusinessMapper.ToResponseDtoList(result.Items),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalRecords = result.TotalRecords
        };
    }

    public async Task<ClienteResponseDto?> GetByIdAsync(int idCliente, int? idClienteDelToken, string rolDelToken)
    {
        if (idCliente <= 0)
            throw new ValidationException("El id del cliente debe ser mayor que 0.");

        // CLIENTE solo puede ver su propio perfil
        if (rolDelToken == "CLIENTE")
        {
            if (idClienteDelToken == null || idClienteDelToken != idCliente)
                throw new UnauthorizedBusinessException("No tienes permiso para ver este cliente.");
        }

        var data = await _clienteDataService.GetByIdAsync(idCliente);
        return data == null ? null : ClienteBusinessMapper.ToResponseDto(data);
    }

    public async Task<ClienteResponseDto> CreateAsync(ClienteRequestDto request, string creadoPorUsuario)
    {
        if (string.IsNullOrWhiteSpace(creadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario creador.");

        _validator.ValidateRequest(request);

        // Verificar duplicados en BDD_Clientes
        var existentes = await _clienteDataService.GetPagedAsync(new ClienteFiltroDataModel
        {
            PageNumber = 1,
            PageSize = 10000
        });

        var numeroIdentificacion = request.NumeroIdentificacion.Trim().ToUpperInvariant();
        var correo = request.Correo.Trim().ToUpperInvariant();

        if (existentes.Items.Any(x => x.NumeroIdentificacion.Trim().ToUpperInvariant() == numeroIdentificacion))
            throw new BusinessException("Ya existe un cliente con el mismo número de identificación.");

        if (existentes.Items.Any(x => x.Correo.Trim().ToUpperInvariant() == correo))
            throw new BusinessException("Ya existe un cliente con el mismo correo.");

        // Paso 1 — Crear cliente en BDD_Clientes
        var dataModel = ClienteBusinessMapper.ToDataModel(request, creadoPorUsuario);
        var creado = await _clienteDataService.CreateAsync(dataModel);

        // Paso 2 — Crear USUARIO_APP en MS Seguridad
        try
        {
            var usuarioRequest = new UsuarioAppIntegrationRequestDto
            {
                IdCliente = creado.IdCliente,  // ← agregar esta línea
                TipoIdentificacion = request.TipoIdentificacion,
                NumeroIdentificacion = request.NumeroIdentificacion,
                Nombres = request.Nombres,
                Apellidos = request.Apellidos,
                RazonSocial = request.RazonSocial,
                Correo = request.Correo,
                Telefono = request.Telefono,
                Direccion = request.Direccion,
                IdCiudadResidencia = request.IdCiudadResidencia,
                IdPaisNacionalidad = request.IdPaisNacionalidad,
                FechaNacimiento = request.FechaNacimiento,
                Genero = request.Genero,
                Username = request.Username,
                Password = request.Password
            };

            await _seguridadService.CrearUsuarioAppAsync(usuarioRequest);
        }
        catch (Exception ex)
        {
            // ✅ Rollback — eliminar el cliente si MS Seguridad falla
            await _clienteDataService.DeleteAsync(creado.IdCliente, "SYSTEM_ROLLBACK");

            throw new BusinessException(
                $"El cliente fue creado pero no se pudo crear el usuario. " +
                $"Se revirtió la operación. Detalle: {ex.Message}");
        }

        return ClienteBusinessMapper.ToResponseDto(creado);
    }

    public async Task<ClienteResponseDto?> UpdateAsync(
        int idCliente, ClienteUpdateRequestDto request,
        string modificadoPorUsuario, int? idClienteDelToken, string rolDelToken)
    {
        if (idCliente <= 0)
            throw new ValidationException("El id del cliente debe ser mayor que 0.");

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario modificador.");

        // CLIENTE solo puede modificar su propio perfil
        if (rolDelToken == "CLIENTE")
        {
            if (idClienteDelToken == null || idClienteDelToken != idCliente)
                throw new UnauthorizedBusinessException("No tienes permiso para modificar este cliente.");
        }

        _validator.ValidateUpdate(request);

        var actual = await _clienteDataService.GetByIdAsync(idCliente);
        if (actual == null)
            throw new NotFoundException("Cliente no encontrado.");

        // ✅ Ciudad y País ya no se validan aquí — están en MS Geografía
        // Si se necesita validación, el frontend ya la hace contra MS Geografía directamente

        var existentes = await _clienteDataService.GetPagedAsync(new ClienteFiltroDataModel
        {
            PageNumber = 1,
            PageSize = 10000
        });

        var numeroIdentificacion = request.NumeroIdentificacion.Trim().ToUpperInvariant();
        var correo = request.Correo.Trim().ToUpperInvariant();

        if (existentes.Items.Any(x => x.IdCliente != idCliente &&
                                      x.NumeroIdentificacion.Trim().ToUpperInvariant() == numeroIdentificacion))
            throw new BusinessException("Ya existe otro cliente con el mismo número de identificación.");

        if (existentes.Items.Any(x => x.IdCliente != idCliente &&
                                      x.Correo.Trim().ToUpperInvariant() == correo))
            throw new BusinessException("Ya existe otro cliente con el mismo correo.");

        var dataModel = ClienteBusinessMapper.ToDataModel(idCliente, request, modificadoPorUsuario);
        var actualizado = await _clienteDataService.UpdateAsync(dataModel);

        return actualizado == null ? null : ClienteBusinessMapper.ToResponseDto(actualizado);
    }

    public async Task<bool> DeleteAsync(int idCliente, string modificadoPorUsuario)
    {
        if (idCliente <= 0)
            throw new ValidationException("El id del cliente debe ser mayor que 0.");

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario modificador.");

        var actual = await _clienteDataService.GetByIdAsync(idCliente);
        if (actual == null)
            throw new NotFoundException("Cliente no encontrado.");

        return await _clienteDataService.DeleteAsync(idCliente, modificadoPorUsuario);
    }
}