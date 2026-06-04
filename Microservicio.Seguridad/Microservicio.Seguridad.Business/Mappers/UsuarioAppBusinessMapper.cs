using Microservicio.Seguridad.Business.DTOs.UsuarioApp;
using Microservicio.Seguridad.DataManagement.Models;

namespace Microservicio.Seguridad.Business.Mappers;

public static class UsuarioAppBusinessMapper
{
    public static UsuarioAppFiltroDataModel ToFiltroDataModel(UsuarioAppFilterDto dto) => new()
    {
        Username = dto.Username,
        Correo = dto.Correo,
        Activo = dto.Activo,
        PageNumber = dto.Page,
        PageSize = dto.PageSize
    };

    public static UsuarioAppDataModel ToDataModel(
        UsuarioAppRequestDto dto,
        string creadoPorUsuario,
        byte[] passwordHash,
        byte[] passwordSalt) => new()
        {
            IdCliente = dto.IdCliente,
            // Referencia lógica al MS Clientes — solo se almacena el ID
            Username = dto.Username,
            Correo = dto.Correo,
            PasswordHash = Convert.ToBase64String(passwordHash),
            PasswordSalt = Convert.ToBase64String(passwordSalt),
            EstadoUsuario = "ACT",
            Activo = true,
            EsEliminado = false,
            CreadoPorUsuario = creadoPorUsuario
        };

    public static UsuarioAppDataModel ToDataModel(
        int idUsuario,
        UsuarioAppUpdateRequestDto dto,
        UsuarioAppDataModel actual,
        string modificadoPorUsuario,
        byte[]? passwordHash = null,
        byte[]? passwordSalt = null) => new()
        {
            IdUsuario = idUsuario,
            IdCliente = actual.IdCliente,
            Username = actual.Username,
            Correo = dto.Correo,
            PasswordHash = passwordHash != null ? Convert.ToBase64String(passwordHash) : actual.PasswordHash,
            PasswordSalt = passwordSalt != null ? Convert.ToBase64String(passwordSalt) : actual.PasswordSalt,
            EstadoUsuario = actual.EstadoUsuario,
            Activo = actual.Activo,
            EsEliminado = actual.EsEliminado,
            FechaUltimoLogin = actual.FechaUltimoLogin,
            CreadoPorUsuario = actual.CreadoPorUsuario,
            FechaRegistroUtc = actual.FechaRegistroUtc,
            ModificadoPorUsuario = modificadoPorUsuario
        };

    public static UsuarioAppResponseDto ToResponseDto(UsuarioAppDataModel model) => new()
    {
        IdUsuario = model.IdUsuario,
        UsuarioGuid = model.UsuarioGuid,
        IdCliente = model.IdCliente,
        Username = model.Username,
        Correo = model.Correo,
        FechaUltimoLogin = model.FechaUltimoLogin,
        EstadoUsuario = model.EstadoUsuario,
        Activo = model.Activo
    };

    public static List<UsuarioAppResponseDto> ToResponseDtoList(IEnumerable<UsuarioAppDataModel> items)
        => items.Select(ToResponseDto).ToList();
}