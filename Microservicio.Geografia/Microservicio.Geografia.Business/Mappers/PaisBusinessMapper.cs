using Microservicio.Geografia.Business.DTOs.Pais;
using Microservicio.Geografia.DataManagement.Models;

namespace Microservicio.Geografia.Business.Mappers;

public static class PaisBusinessMapper
{
    public static PaisFiltroDataModel ToFiltroDataModel(
        PaisFilterDto dto)
    {
        return new PaisFiltroDataModel
        {
            Nombre = dto.Nombre,
            CodigoIso2 = dto.CodigoIso2,
            CodigoIso3 = dto.CodigoIso3,
            Continente = dto.Continente,
            Estado = dto.Estado,
            PaginaActual = dto.PaginaActual,
            TamanoPagina = dto.TamanoPagina
        };
    }

    public static PaisDataModel ToDataModel(
        PaisRequestDto dto)
    {
        return new PaisDataModel
        {
            CodigoIso2 = dto.CodigoIso2
                .Trim()
                .ToUpperInvariant(),

            CodigoIso3 = string.IsNullOrWhiteSpace(dto.CodigoIso3)
                ? null
                : dto.CodigoIso3
                    .Trim()
                    .ToUpperInvariant(),

            Nombre = dto.Nombre.Trim(),

            Continente = string.IsNullOrWhiteSpace(dto.Continente)
                ? null
                : dto.Continente.Trim(),

            Estado = "ACTIVO",

            Eliminado = false
        };
    }

    public static PaisDataModel ToDataModel(
        int idPais,
        PaisUpdateRequestDto dto)
    {
        return new PaisDataModel
        {
            IdPais = idPais,

            CodigoIso2 = dto.CodigoIso2
                .Trim()
                .ToUpperInvariant(),

            CodigoIso3 = string.IsNullOrWhiteSpace(dto.CodigoIso3)
                ? null
                : dto.CodigoIso3
                    .Trim()
                    .ToUpperInvariant(),

            Nombre = dto.Nombre.Trim(),

            Continente = string.IsNullOrWhiteSpace(dto.Continente)
                ? null
                : dto.Continente.Trim()
        };
    }

    public static PaisResponseDto ToResponseDto(
        PaisDataModel model)
    {
        return new PaisResponseDto
        {
            IdPais = model.IdPais,
            CodigoIso2 = model.CodigoIso2,
            CodigoIso3 = model.CodigoIso3,
            Nombre = model.Nombre,
            Continente = model.Continente,
            Estado = model.Estado
        };
    }

    public static List<PaisResponseDto> ToResponseDtoList(
        IEnumerable<PaisDataModel> items)
    {
        return items
            .Select(ToResponseDto)
            .ToList();
    }
}