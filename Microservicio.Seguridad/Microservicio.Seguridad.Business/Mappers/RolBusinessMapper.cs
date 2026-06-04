using Microservicio.Seguridad.Business.DTOs.Rol;
using Microservicio.Seguridad.DataManagement.Models;

namespace Microservicio.Seguridad.Business.Mappers;

public static class RolBusinessMapper
{
    public static RolFiltroDataModel ToFiltroDataModel(RolFilterDto dto) => new()
    {
        NombreRol = dto.NombreRol,
        EstadoRol = dto.EstadoRol,
        PageNumber = dto.Page,
        PageSize = dto.PageSize
    };

    public static RolDataModel ToDataModel(RolRequestDto dto, string creadoPorUsuario) => new()
    {
        NombreRol = dto.NombreRol,
        DescripcionRol = dto.DescripcionRol,
        EstadoRol = "ACT",
        Activo = true,
        EsEliminado = false,
        CreadoPorUsuario = creadoPorUsuario
    };

    public static RolDataModel ToDataModel(int idRol, RolUpdateRequestDto dto, string modificadoPorUsuario) => new()
    {
        IdRol = idRol,
        NombreRol = dto.NombreRol,
        DescripcionRol = dto.DescripcionRol,
        EstadoRol = "ACT",
        Activo = true,
        ModificadoPorUsuario = modificadoPorUsuario
    };

    public static RolResponseDto ToResponseDto(RolDataModel model) => new()
    {
        IdRol = model.IdRol,
        RolGuid = model.RolGuid,
        NombreRol = model.NombreRol,
        DescripcionRol = model.DescripcionRol,
        EstadoRol = model.EstadoRol,
        Activo = model.Activo
    };

    public static List<RolResponseDto> ToResponseDtoList(IEnumerable<RolDataModel> items)
        => items.Select(ToResponseDto).ToList();
}