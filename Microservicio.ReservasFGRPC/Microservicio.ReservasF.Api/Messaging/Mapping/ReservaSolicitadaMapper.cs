using Marketplace.Events.Contracts.Events;
using Microservicio.ReservasF.Business.DTOs.Reserva;
using Microservicio.ReservasF.Business.DTOs.ReservaDetalle;

namespace Microservicio.ReservasF.Api.Messaging.Mapping;

public static class ReservaSolicitadaMapper
{
    public static ReservaRequestDto ToRequestDto(ReservaSolicitadaEvent source)
    {
        var detalles = source.Detalles
            .Select(d => new ReservaDetalleRequestDto
            {
                IdPasajero = d.IdPasajero,
                IdAsiento = d.IdAsiento,
                SubtotalLinea = d.SubtotalLinea,
                ValorIvaLinea = d.ValorIvaLinea,
                TotalLinea = d.TotalLinea
            })
            .ToList();

        var request = new ReservaRequestDto
        {
            IdCliente = source.IdCliente,
            IdVuelo = source.IdVuelo,
            SubtotalReserva = source.SubtotalReserva,
            ValorIva = source.ValorIva,
            TotalReserva = source.TotalReserva,
            OrigenCanalReserva = source.OrigenCanalReserva,
            ContactoEmail = source.ContactoEmail,
            ContactoTelefono = source.ContactoTelefono,
            Observaciones = source.Observaciones,
            Detalles = detalles
        };

        if (detalles.Count > 0)
        {
            request.IdPasajero = detalles[0].IdPasajero;
            request.IdAsiento = detalles[0].IdAsiento;
        }

        return request;
    }
}
