using Microservicio.Seguridad.Business.DTOs.UsuarioRol;
using Microservicio.Seguridad.DataManagement.Models;

namespace Microservicio.Seguridad.Business.Mappers;

public static class UsuarioRolBusinessMapper
{
    public static UsuarioRolFiltroDataModel ToFiltroDataModel(UsuarioRolFilterDto dto) => new()
    {
        IdUsuario = dto.IdUsuario,
        IdRol = dto.IdRol,
        PageNumber = dto.Page,
        PageSize = dto.PageSize
    };

    public static UsuarioRolDataModel ToDataModel(UsuarioRolRequestDto dto, string creadoPorUsuario) => new()
    {
        IdUsuario = dto.IdUsuario,
        IdRol = dto.IdRol,
        EstadoUsuarioRol = "ACT",
        Activo = true,
        EsEliminado = false,
        CreadoPorUsuario = creadoPorUsuario
    };

    public static UsuarioRolResponseDto ToResponseDto(UsuarioRolDataModel model) => new()
    {
        IdUsuarioRol = model.IdUsuarioRol,
        IdUsuario = model.IdUsuario,
        IdRol = model.IdRol,
        EstadoUsuarioRol = model.EstadoUsuarioRol,
        Activo = model.Activo
    };

    public static List<UsuarioRolResponseDto> ToResponseDtoList(IEnumerable<UsuarioRolDataModel> items)
        => items.Select(ToResponseDto).ToList();
}