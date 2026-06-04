// PasajeroBusinessMapper.cs
using Microservicio.Clientes.Business.DTOs.Pasajero;
using Microservicio.Clientes.DataManagement.Models;

namespace Microservicio.Clientes.Business.Mappers;

public static class PasajeroBusinessMapper
{
    public static PasajeroFiltroDataModel ToFiltroDataModel(PasajeroFilterDto dto) => new()
    {
        IdCliente = dto.IdCliente,
        NombrePasajero = dto.NombrePasajero,
        ApellidoPasajero = dto.ApellidoPasajero,
        TipoDocumentoPasajero = dto.TipoDocumentoPasajero,
        NumeroDocumentoPasajero = dto.NumeroDocumentoPasajero,
        Estado = dto.Estado,
        RequiereAsistencia = dto.RequiereAsistencia,
        PageNumber = dto.Page,
        PageSize = dto.PageSize
    };

    public static PasajeroDataModel ToDataModel(PasajeroRequestDto dto, string creadoPorUsuario) => new()
    {
        IdCliente = dto.IdCliente,
        NombrePasajero = dto.NombrePasajero,
        ApellidoPasajero = dto.ApellidoPasajero,
        TipoDocumentoPasajero = dto.TipoDocumentoPasajero,
        NumeroDocumentoPasajero = dto.NumeroDocumentoPasajero,
        FechaNacimientoPasajero = dto.FechaNacimientoPasajero,
        IdPaisNacionalidad = dto.IdPaisNacionalidad,
        EmailContactoPasajero = dto.EmailContactoPasajero,
        TelefonoContactoPasajero = dto.TelefonoContactoPasajero,
        GeneroPasajero = dto.GeneroPasajero,
        RequiereAsistencia = dto.RequiereAsistencia,
        ObservacionesPasajero = dto.ObservacionesPasajero,
        Estado = "ACTIVO",
        EsEliminado = false,
        CreadoPorUsuario = creadoPorUsuario
    };

    public static PasajeroDataModel ToDataModel(int idPasajero, PasajeroUpdateRequestDto dto, string modificadoPorUsuario) => new()
    {
        IdPasajero = idPasajero,
        IdCliente = dto.IdCliente,
        NombrePasajero = dto.NombrePasajero,
        ApellidoPasajero = dto.ApellidoPasajero,
        TipoDocumentoPasajero = dto.TipoDocumentoPasajero,
        NumeroDocumentoPasajero = dto.NumeroDocumentoPasajero,
        FechaNacimientoPasajero = dto.FechaNacimientoPasajero,
        IdPaisNacionalidad = dto.IdPaisNacionalidad,
        EmailContactoPasajero = dto.EmailContactoPasajero,
        TelefonoContactoPasajero = dto.TelefonoContactoPasajero,
        GeneroPasajero = dto.GeneroPasajero,
        RequiereAsistencia = dto.RequiereAsistencia,
        ObservacionesPasajero = dto.ObservacionesPasajero,
        Estado = "ACTIVO",
        ModificadoPorUsuario = modificadoPorUsuario
    };

    public static PasajeroResponseDto ToResponseDto(PasajeroDataModel model) => new()
    {
        IdPasajero = model.IdPasajero,
        IdCliente = model.IdCliente,
        NombrePasajero = model.NombrePasajero,
        ApellidoPasajero = model.ApellidoPasajero,
        TipoDocumentoPasajero = model.TipoDocumentoPasajero,
        NumeroDocumentoPasajero = model.NumeroDocumentoPasajero,
        FechaNacimientoPasajero = model.FechaNacimientoPasajero,
        IdPaisNacionalidad = model.IdPaisNacionalidad,
        EmailContactoPasajero = model.EmailContactoPasajero,
        TelefonoContactoPasajero = model.TelefonoContactoPasajero,
        GeneroPasajero = model.GeneroPasajero,
        RequiereAsistencia = model.RequiereAsistencia,
        ObservacionesPasajero = model.ObservacionesPasajero,
        Estado = model.Estado
    };

    public static List<PasajeroResponseDto> ToResponseDtoList(IEnumerable<PasajeroDataModel> items)
        => items.Select(ToResponseDto).ToList();
}