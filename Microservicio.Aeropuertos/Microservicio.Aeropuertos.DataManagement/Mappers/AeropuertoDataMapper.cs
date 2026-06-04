using Microservicio.Aeropuertos.DataAccess.Entities;
using Microservicio.Aeropuertos.DataManagement.Models;

namespace Microservicio.Aeropuertos.DataManagement.Mappers;

public static class AeropuertoDataMapper
{
    public static AeropuertoDataModel ToDataModel(
        AeropuertoEntity entity)
    {
        return new AeropuertoDataModel
        {
            IdAeropuerto = entity.IdAeropuerto,
            CodigoIata = entity.CodigoIata,
            CodigoIcao = entity.CodigoIcao,
            Nombre = entity.Nombre,
            IdCiudad = entity.IdCiudad,
            IdPais = entity.IdPais,
            ZonaHoraria = entity.ZonaHoraria,
            Latitud = entity.Latitud,
            Longitud = entity.Longitud,
            Estado = entity.Estado,
            Eliminado = entity.Eliminado,
            FechaRegistroUtc = entity.FechaRegistroUtc,
            CreadoPorUsuario = entity.CreadoPorUsuario,
            ModificadoPorUsuario = entity.ModificadoPorUsuario,
            FechaModificacionUtc = entity.FechaModificacionUtc,
            ModificacionIp = entity.ModificacionIp
        };
    }

    public static AeropuertoEntity ToEntity(
        AeropuertoDataModel model)
    {
        return new AeropuertoEntity
        {
            IdAeropuerto = model.IdAeropuerto,

            CodigoIata =
                model.CodigoIata
                    .Trim()
                    .ToUpperInvariant(),

            CodigoIcao =
                string.IsNullOrWhiteSpace(model.CodigoIcao)
                    ? null
                    : model.CodigoIcao
                        .Trim()
                        .ToUpperInvariant(),

            Nombre =
                model.Nombre.Trim(),

            IdCiudad = model.IdCiudad,
            IdPais = model.IdPais,

            ZonaHoraria =
                string.IsNullOrWhiteSpace(model.ZonaHoraria)
                    ? null
                    : model.ZonaHoraria.Trim(),

            Latitud = model.Latitud,
            Longitud = model.Longitud,
            Estado = model.Estado,
            Eliminado = model.Eliminado,
            FechaRegistroUtc = model.FechaRegistroUtc,

            CreadoPorUsuario =
                model.CreadoPorUsuario.Trim(),

            ModificadoPorUsuario =
                string.IsNullOrWhiteSpace(model.ModificadoPorUsuario)
                    ? null
                    : model.ModificadoPorUsuario.Trim(),

            FechaModificacionUtc = model.FechaModificacionUtc,

            ModificacionIp =
                string.IsNullOrWhiteSpace(model.ModificacionIp)
                    ? null
                    : model.ModificacionIp.Trim()
        };
    }

    public static void UpdateEntity(
        AeropuertoEntity entity,
        AeropuertoDataModel model)
    {
        entity.CodigoIata =
            model.CodigoIata
                .Trim()
                .ToUpperInvariant();

        entity.CodigoIcao =
            string.IsNullOrWhiteSpace(model.CodigoIcao)
                ? null
                : model.CodigoIcao
                    .Trim()
                    .ToUpperInvariant();

        entity.Nombre = model.Nombre.Trim();
        entity.IdCiudad = model.IdCiudad;
        entity.IdPais = model.IdPais;

        entity.ZonaHoraria =
            string.IsNullOrWhiteSpace(model.ZonaHoraria)
                ? null
                : model.ZonaHoraria.Trim();

        entity.Latitud = model.Latitud;
        entity.Longitud = model.Longitud;
        entity.Estado = model.Estado;

        entity.ModificadoPorUsuario =
            string.IsNullOrWhiteSpace(model.ModificadoPorUsuario)
                ? null
                : model.ModificadoPorUsuario.Trim();

        entity.FechaModificacionUtc = model.FechaModificacionUtc;

        entity.ModificacionIp =
            string.IsNullOrWhiteSpace(model.ModificacionIp)
                ? null
                : model.ModificacionIp.Trim();
    }
}