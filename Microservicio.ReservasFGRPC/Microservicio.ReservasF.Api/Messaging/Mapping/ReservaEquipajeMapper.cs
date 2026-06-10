using Marketplace.Events.Contracts.Payloads;
using Microservicio.ReservasF.Business.DTOs.Reserva;
using Microservicio.ReservasF.Business.DTOs.ReservaDetalle;
using Microservicio.ReservasF.Business.Exceptions;

namespace Microservicio.ReservasF.Api.Messaging.Mapping;

public static class ReservaEquipajeMapper
{
    public static List<ReservaPagarEquipajeRequestDto> ToPagarEquipaje(
        IReadOnlyList<EquipajePayload> equipaje,
        IReadOnlyList<ReservaDetalleResponseDto> detalles)
    {
        if (equipaje.Count == 0)
            return [];

        var activos = detalles.Where(d => !d.EsEliminado).ToList();
        var result = new List<ReservaPagarEquipajeRequestDto>();

        foreach (var item in equipaje)
        {
            ReservaDetalleResponseDto? detalle = null;

            if (item.IdDetalle is > 0)
                detalle = activos.FirstOrDefault(d => d.IdDetalle == item.IdDetalle);
            else if (item.IdPasajero is > 0)
                detalle = activos.FirstOrDefault(d => d.IdPasajero == item.IdPasajero);
            else if (activos.Count == 1)
                detalle = activos[0];

            if (detalle is null)
            {
                throw new ValidationException(
                    $"No se encontró detalle de reserva para equipaje del pasajero {item.IdPasajero}.");
            }

            result.Add(new ReservaPagarEquipajeRequestDto
            {
                IdDetalle = detalle.IdDetalle,
                Tipo = item.Tipo,
                PesoKg = item.PesoKg,
                DescripcionEquipaje = item.DescripcionEquipaje
            });
        }

        return result;
    }
}
