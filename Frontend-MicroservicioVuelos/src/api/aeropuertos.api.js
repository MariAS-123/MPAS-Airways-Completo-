import apiClient from './axios'

function limpiarParams(params = {}) {
  const pares = {
    codigoIata: 'codigo_iata',
    codigoIcao: 'codigo_icao',
    idCiudad: 'id_ciudad',
    idPais: 'id_pais',
    zonaHoraria: 'zona_horaria',
    estado: 'Estado',
  }

  return Object.entries(params).reduce((acc, [clave, valor]) => {
    if (valor === undefined || valor === null || valor === '') return acc
    const claveApi = pares[clave] || clave
    acc[claveApi === 'Estado' ? 'estado' : claveApi] = valor
    return acc
  }, {})
}

export const getAeropuertosAdminApi = (params = {}) =>
  apiClient.get('/aeropuertos', { params: limpiarParams(params) })

export const getAeropuertoAdminApi = (idAeropuerto) =>
  apiClient.get(`/aeropuertos/${idAeropuerto}`)

export const createAeropuertoAdminApi = (payload) =>
  apiClient.post('/aeropuertos', payload)

export const updateAeropuertoAdminApi = (idAeropuerto, payload) =>
  apiClient.put(`/aeropuertos/${idAeropuerto}`, payload)

export const deleteAeropuertoAdminApi = (idAeropuerto) =>
  apiClient.delete(`/aeropuertos/${idAeropuerto}`)
