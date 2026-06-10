import apiClient from './axios'

function validarIdPositivo(nombre, valor) {
  const id = Number(valor)
  if (!Number.isInteger(id) || id <= 0) {
    throw new Error(`${nombre} debe ser mayor que 0.`)
  }
  return id
}

function limpiarParams(params = {}) {
  const pares = {
    id_aeropuerto_origen: 'IdAeropuertoOrigen',
    idAeropuertoOrigen: 'IdAeropuertoOrigen',
    id_aeropuerto_destino: 'IdAeropuertoDestino',
    idAeropuertoDestino: 'IdAeropuertoDestino',
    numero_vuelo: 'NumeroVuelo',
    numeroVuelo: 'NumeroVuelo',
    estado_vuelo: 'EstadoVuelo',
    estadoVuelo: 'EstadoVuelo',
    fecha_salida_desde: 'FechaSalidaDesde',
    fechaSalidaDesde: 'FechaSalidaDesde',
    fecha_salida_hasta: 'FechaSalidaHasta',
    fechaSalidaHasta: 'FechaSalidaHasta',
    fecha_salida: 'FechaSalida',
    fechaSalida: 'FechaSalida',
    codigo_iata_origen: 'CodigoIataOrigen',
    codigoIataOrigen: 'CodigoIataOrigen',
    codigo_iata_destino: 'CodigoIataDestino',
    codigoIataDestino: 'CodigoIataDestino',
    cantidad_pasajeros: 'CantidadPasajeros',
    cantidadPasajeros: 'CantidadPasajeros',
    clase: 'Clase',
    page: 'Page',
    page_size: 'PageSize',
  }

  return Object.entries(params).reduce((acc, [clave, valor]) => {
    if (valor === undefined || valor === null || valor === '') return acc
    acc[pares[clave] || clave] = valor
    return acc
  }, {})
}

export const getVuelosApi = (params = {}) =>
  apiClient.get('/vuelos', {
    params: limpiarParams({
      page_size: 50,
      ...params,
    }),
  })

export const getVueloDetalleApi = (idVuelo) =>
  apiClient.get(`/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}`)

export const buscarVuelosBookingApi = (params = {}) =>
  apiClient.get('/booking/vuelos/buscar', {
    params: limpiarParams({
      PageSize: 50,
      ...params,
    }),
  })

export const getVueloBookingDetalleApi = (idVuelo) =>
  apiClient.get(`/booking/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}`)

export const getEscalasVueloBookingApi = (idVuelo) =>
  apiClient.get(`/booking/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}/escalas`)

export const getAsientosVueloBookingApi = (idVuelo) =>
  apiClient.get(`/booking/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}/asientos`)

export const crearSesionRedirectVueloApi = (payload) =>
  apiClient.post('/booking/vuelos/sesion-redirect', payload)

export const createVueloApi = (payload) => apiClient.post('/vuelos', payload)

export const updateVueloApi = (idVuelo, payload) =>
  apiClient.put(`/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}`, payload)

export const cambiarEstadoVueloApi = (idVuelo, payload) =>
  apiClient.patch(`/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}/estado`, payload)

export const deleteVueloApi = (idVuelo) =>
  apiClient.delete(`/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}`)

export const getEscalasVueloApi = (idVuelo) =>
  apiClient.get(`/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}/escalas`)

export const getEscalaVueloDetalleApi = (idVuelo, idEscala) =>
  apiClient.get(
    `/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}/escalas/${validarIdPositivo('El id de la escala', idEscala)}`,
  )

export const createEscalaVueloApi = (idVuelo, payload) =>
  apiClient.post(`/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}/escalas`, payload)

export const deleteEscalaVueloApi = (idVuelo, idEscala) =>
  apiClient.delete(
    `/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}/escalas/${validarIdPositivo('El id de la escala', idEscala)}`,
  )

export const getAsientosVueloApi = (idVuelo, params = {}) =>
  apiClient.get(`/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}/asientos`, {
    params: limpiarParams(params),
  })

export const getAsientoVueloDetalleApi = (idVuelo, idAsiento, config = {}) =>
  apiClient.get(
    `/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}/asientos/${validarIdPositivo('El id del asiento', idAsiento)}`,
    config,
  )

export const createAsientoVueloApi = (idVuelo, payload) =>
  apiClient.post(`/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}/asientos`, payload)

export const updateAsientoVueloApi = (idVuelo, idAsiento, payload) =>
  apiClient.patch(
    `/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}/asientos/${validarIdPositivo('El id del asiento', idAsiento)}`,
    payload,
  )

export const deleteAsientoVueloApi = (idVuelo, idAsiento) =>
  apiClient.delete(
    `/vuelos/${validarIdPositivo('El id del vuelo', idVuelo)}/asientos/${validarIdPositivo('El id del asiento', idAsiento)}`,
  )
