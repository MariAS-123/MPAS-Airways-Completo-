using Microservicio.Clientes.Business.DTOs.Pasajero;
using Microservicio.Clientes.Business.Exceptions;
using Microservicio.Clientes.Business.Interfaces;
using Microservicio.Clientes.Business.Mappers;
using Microservicio.Clientes.Business.Validators;
using Microservicio.Clientes.DataManagement.Common;
using Microservicio.Clientes.DataManagement.Interfaces;
using Microservicio.Clientes.DataManagement.Models;

namespace Microservicio.Clientes.Business.Services;

public class PasajeroService : IPasajeroService
{
    private readonly IPasajeroDataService _pasajeroDataService;
    private readonly IClienteDataService _clienteDataService;
    private readonly PasajeroValidator _validator;

    // ✅ IPaisDataService e IReservaDataService eliminados — están en otros MS
    public PasajeroService(
        IPasajeroDataService pasajeroDataService,
        IClienteDataService clienteDataService)
    {
        _pasajeroDataService = pasajeroDataService;
        _clienteDataService = clienteDataService;
        _validator = new PasajeroValidator();
    }

    public async Task<DataPagedResult<PasajeroResponseDto>> GetPagedAsync(PasajeroFilterDto filter)
    {
        _validator.ValidateFilter(filter);

        var filtro = PasajeroBusinessMapper.ToFiltroDataModel(filter);
        var result = await _pasajeroDataService.GetPagedAsync(filtro);

        return new DataPagedResult<PasajeroResponseDto>
        {
            Items = PasajeroBusinessMapper.ToResponseDtoList(result.Items),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalRecords = result.TotalRecords
        };
    }

    public async Task<PasajeroResponseDto?> GetByIdAsync(int idPasajero, int? idClienteDelToken, string rolDelToken)
    {
        if (idPasajero <= 0)
            throw new ValidationException("El id del pasajero debe ser mayor que 0.");

        var data = await _pasajeroDataService.GetByIdAsync(idPasajero);
        if (data == null) return null;

        // ✅ CLIENTE solo puede ver pasajeros propios
        // La verificación de "vinculado a reserva mía" ya no es posible desde este MS
        // — las reservas están en MS Ventas
        if (rolDelToken == "CLIENTE")
        {
            if (idClienteDelToken is null)
                throw new UnauthorizedBusinessException("No se pudo identificar el cliente del token.");

            if (data.IdCliente != idClienteDelToken)
                throw new UnauthorizedBusinessException("No tienes permiso para ver este pasajero.");
        }

        return PasajeroBusinessMapper.ToResponseDto(data);
    }

    public async Task<PasajeroResponseDto> CreateAsync(PasajeroRequestDto request, string creadoPorUsuario)
    {
        if (string.IsNullOrWhiteSpace(creadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario creador.");

        _validator.ValidateRequest(request);

        // Validar que el cliente existe en BDD_Clientes — sí está en este MS
        if (request.IdCliente.HasValue)
        {
            var cliente = await _clienteDataService.GetByIdAsync(request.IdCliente.Value);
            if (cliente == null)
                throw new NotFoundException("El cliente indicado no existe.");
            if (cliente.EsEliminado || cliente.Estado != "ACT")
                throw new BusinessException("El cliente indicado está inactivo o eliminado.");
        }

        // ✅ País ya no se valida aquí — está en MS Geografía

        // Verificar duplicado por tipo y número de documento
        var existentes = await _pasajeroDataService.GetPagedAsync(new PasajeroFiltroDataModel
        {
            PageNumber = 1,
            PageSize = 10000
        });

        var numeroDocumento = request.NumeroDocumentoPasajero.Trim().ToUpperInvariant();
        var tipoDocumento = request.TipoDocumentoPasajero.Trim().ToUpperInvariant();

        if (existentes.Items.Any(x =>
            x.TipoDocumentoPasajero.Trim().ToUpperInvariant() == tipoDocumento &&
            x.NumeroDocumentoPasajero.Trim().ToUpperInvariant() == numeroDocumento))
            throw new BusinessException("Ya existe un pasajero con el mismo tipo y número de documento.");

        var dataModel = PasajeroBusinessMapper.ToDataModel(request, creadoPorUsuario);
        var creado = await _pasajeroDataService.CreateAsync(dataModel);

        return PasajeroBusinessMapper.ToResponseDto(creado);
    }

    public async Task<PasajeroResponseDto?> UpdateAsync(int idPasajero, PasajeroUpdateRequestDto request, string modificadoPorUsuario)
    {
        if (idPasajero <= 0)
            throw new ValidationException("El id del pasajero debe ser mayor que 0.");

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario modificador.");

        _validator.ValidateUpdate(request);

        var actual = await _pasajeroDataService.GetByIdAsync(idPasajero);
        if (actual == null)
            throw new NotFoundException("Pasajero no encontrado.");

        if (request.IdCliente.HasValue)
        {
            var cliente = await _clienteDataService.GetByIdAsync(request.IdCliente.Value);
            if (cliente == null)
                throw new NotFoundException("El cliente indicado no existe.");
            if (cliente.EsEliminado || cliente.Estado != "ACT")
                throw new BusinessException("El cliente indicado está inactivo o eliminado.");
        }

        // ✅ País ya no se valida aquí — está en MS Geografía

        var existentes = await _pasajeroDataService.GetPagedAsync(new PasajeroFiltroDataModel
        {
            PageNumber = 1,
            PageSize = 10000
        });

        var numeroDocumento = request.NumeroDocumentoPasajero.Trim().ToUpperInvariant();
        var tipoDocumento = request.TipoDocumentoPasajero.Trim().ToUpperInvariant();

        if (existentes.Items.Any(x =>
            x.IdPasajero != idPasajero &&
            x.TipoDocumentoPasajero.Trim().ToUpperInvariant() == tipoDocumento &&
            x.NumeroDocumentoPasajero.Trim().ToUpperInvariant() == numeroDocumento))
            throw new BusinessException("Ya existe otro pasajero con el mismo tipo y número de documento.");

        var dataModel = PasajeroBusinessMapper.ToDataModel(idPasajero, request, modificadoPorUsuario);
        var actualizado = await _pasajeroDataService.UpdateAsync(dataModel);

        return actualizado == null ? null : PasajeroBusinessMapper.ToResponseDto(actualizado);
    }

    public async Task<bool> DeleteAsync(int idPasajero, string modificadoPorUsuario)
    {
        if (idPasajero <= 0)
            throw new ValidationException("El id del pasajero debe ser mayor que 0.");

        if (string.IsNullOrWhiteSpace(modificadoPorUsuario))
            throw new UnauthorizedBusinessException("No se pudo identificar el usuario modificador.");

        var actual = await _pasajeroDataService.GetByIdAsync(idPasajero);
        if (actual == null)
            throw new NotFoundException("Pasajero no encontrado.");

        return await _pasajeroDataService.DeleteAsync(idPasajero, modificadoPorUsuario);
    }
}