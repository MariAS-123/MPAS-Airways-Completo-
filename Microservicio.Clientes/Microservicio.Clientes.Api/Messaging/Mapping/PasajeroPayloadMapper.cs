using Marketplace.Events.Contracts.Payloads;
using Microservicio.Clientes.Business.DTOs.Pasajero;

namespace Microservicio.Clientes.Api.Messaging.Mapping;

public static class PasajeroPayloadMapper
{
    public static PasajeroRequestDto ToRequestDto(PasajeroPayload source, int idCliente) =>
        new()
        {
            IdCliente = idCliente,
            NombrePasajero = source.NombrePasajero,
            ApellidoPasajero = source.ApellidoPasajero,
            TipoDocumentoPasajero = source.TipoDocumentoPasajero,
            NumeroDocumentoPasajero = source.NumeroDocumentoPasajero,
            FechaNacimientoPasajero = source.FechaNacimientoPasajero,
            IdPaisNacionalidad = source.IdPaisNacionalidad,
            EmailContactoPasajero = source.EmailContactoPasajero,
            TelefonoContactoPasajero = source.TelefonoContactoPasajero,
            GeneroPasajero = source.GeneroPasajero,
            RequiereAsistencia = source.RequiereAsistencia,
            ObservacionesPasajero = source.ObservacionesPasajero
        };
}
