using Microservicio.Geografia.DataAccess.Entities;
using Microservicio.Geografia.DataManagement.Models;

namespace Microservicio.Geografia.DataManagement.Mappers;

public static class PaisDataMapper
{
    public static PaisDataModel ToDataModel(PaisEntity entity)
    {
        return new PaisDataModel
        {
            IdPais = entity.IdPais,
            CodigoIso2 = entity.CodigoIso2,
            CodigoIso3 = entity.CodigoIso3,
            Nombre = entity.Nombre,
            Continente = entity.Continente,
            Estado = entity.Estado,
            Eliminado = entity.Eliminado
        };
    }

    public static PaisEntity ToEntity(PaisDataModel model)
    {
        return new PaisEntity
        {
            IdPais = model.IdPais,
            CodigoIso2 = model.CodigoIso2,
            CodigoIso3 = model.CodigoIso3,
            Nombre = model.Nombre,
            Continente = model.Continente,
            Estado = model.Estado,
            Eliminado = model.Eliminado
        };
    }

    public static void UpdateEntity(
    PaisEntity entity,
    PaisDataModel model)
    {
        entity.CodigoIso2 = model.CodigoIso2.Trim().ToUpperInvariant();

        entity.CodigoIso3 = string.IsNullOrWhiteSpace(model.CodigoIso3)
            ? null
            : model.CodigoIso3.Trim().ToUpperInvariant();

        entity.Nombre = model.Nombre.Trim();

        entity.Continente = string.IsNullOrWhiteSpace(model.Continente)
            ? null
            : model.Continente.Trim();

        entity.Estado = string.IsNullOrWhiteSpace(model.Estado)
            ? entity.Estado
            : model.Estado.Trim().ToUpperInvariant();

        entity.Eliminado = model.Eliminado;
    }

    public static IEnumerable<PaisDataModel> ToDataModelList(
        IEnumerable<PaisEntity> entities)
    {
        return entities.Select(ToDataModel);
    }
}