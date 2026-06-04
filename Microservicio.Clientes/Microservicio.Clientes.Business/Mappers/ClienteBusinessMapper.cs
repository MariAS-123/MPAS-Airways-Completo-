// ClienteBusinessMapper.cs
using Microservicio.Clientes.Business.DTOs.Cliente;
using Microservicio.Clientes.DataManagement.Models;

namespace Microservicio.Clientes.Business.Mappers;

public static class ClienteBusinessMapper
{
    public static ClienteFiltroDataModel ToFiltroDataModel(ClienteFilterDto dto) => new()
    {
        NumeroIdentificacion = dto.NumeroIdentificacion,
        Correo = dto.Correo,
        Estado = dto.Estado,
        PageNumber = dto.Page,
        PageSize = dto.PageSize
    };

    public static ClienteDataModel ToDataModel(ClienteRequestDto dto, string creadoPorUsuario) => new()
    {
        TipoIdentificacion = dto.TipoIdentificacion,
        NumeroIdentificacion = dto.NumeroIdentificacion,
        Nombres = dto.Nombres,
        Apellidos = dto.Apellidos,
        RazonSocial = dto.RazonSocial,
        Correo = dto.Correo,
        Telefono = dto.Telefono,
        Direccion = dto.Direccion,
        IdCiudadResidencia = dto.IdCiudadResidencia,
        IdPaisNacionalidad = dto.IdPaisNacionalidad,
        FechaNacimiento = dto.FechaNacimiento,
        Genero = dto.Genero,
        Estado = "ACT",
        EsEliminado = false,
        CreadoPorUsuario = creadoPorUsuario,
        // ✅ Cambiado de "VUELOS" a "CLIENTES"
        ServicioOrigen = "CLIENTES"
    };

    public static ClienteDataModel ToDataModel(int idCliente, ClienteUpdateRequestDto dto, string modificadoPorUsuario) => new()
    {
        IdCliente = idCliente,
        TipoIdentificacion = dto.TipoIdentificacion,
        NumeroIdentificacion = dto.NumeroIdentificacion,
        Nombres = dto.Nombres,
        Apellidos = dto.Apellidos,
        RazonSocial = dto.RazonSocial,
        Correo = dto.Correo,
        Telefono = dto.Telefono,
        Direccion = dto.Direccion,
        IdCiudadResidencia = dto.IdCiudadResidencia,
        IdPaisNacionalidad = dto.IdPaisNacionalidad,
        FechaNacimiento = dto.FechaNacimiento,
        Genero = dto.Genero,
        Estado = "ACT",
        ModificadoPorUsuario = modificadoPorUsuario
    };

    public static ClienteResponseDto ToResponseDto(ClienteDataModel model) => new()
    {
        IdCliente = model.IdCliente,
        ClienteGuid = model.ClienteGuid,
        TipoIdentificacion = model.TipoIdentificacion,
        NumeroIdentificacion = model.NumeroIdentificacion,
        Nombres = model.Nombres,
        Apellidos = model.Apellidos,
        RazonSocial = model.RazonSocial,
        Correo = model.Correo,
        Telefono = model.Telefono,
        Direccion = model.Direccion,
        IdCiudadResidencia = model.IdCiudadResidencia,
        IdPaisNacionalidad = model.IdPaisNacionalidad,
        FechaNacimiento = model.FechaNacimiento,
        Genero = model.Genero,
        Estado = model.Estado
    };

    public static List<ClienteResponseDto> ToResponseDtoList(IEnumerable<ClienteDataModel> items)
        => items.Select(ToResponseDto).ToList();
}