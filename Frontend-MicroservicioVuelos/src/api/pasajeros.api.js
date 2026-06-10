import apiClient from './axios'

function limpiarParams(params = {}) {
  const pares = {
    id_cliente: 'IdCliente',
    idCliente: 'IdCliente',
    nombre_pasajero: 'NombrePasajero',
    nombrePasajero: 'NombrePasajero',
    numero_documento: 'NumeroDocumento',
    numeroDocumento: 'NumeroDocumento',
    estado: 'Estado',
    page: 'Page',
    page_size: 'PageSize',
  }

  return Object.entries(params).reduce((acc, [clave, valor]) => {
    if (valor === undefined || valor === null || valor === '') return acc
    acc[pares[clave] || clave] = valor
    return acc
  }, {})
}

export const getPasajerosApi = (params = {}) =>
  apiClient.get('/pasajeros', { params: limpiarParams(params) })

export const getPasajeroApi = (idPasajero, config = {}) =>
  apiClient.get(`/pasajeros/${idPasajero}`, config)

export const createPasajeroApi = (payload) =>
  apiClient.post('/pasajeros', payload)

export const updatePasajeroApi = (idPasajero, payload) =>
  apiClient.put(`/pasajeros/${idPasajero}`, payload)

export const deletePasajeroApi = (idPasajero) =>
  apiClient.delete(`/pasajeros/${idPasajero}`)
