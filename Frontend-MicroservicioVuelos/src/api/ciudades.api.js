import apiClient from './axios'

function limpiarParams(params = {}) {
  const pares = {
    id_pais: 'IdPais',
    idPais: 'IdPais',
    nombre: 'Nombre',
    codigo_postal: 'CodigoPostal',
    codigoPostal: 'CodigoPostal',
    zona_horaria: 'ZonaHoraria',
    zonaHoraria: 'ZonaHoraria',
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

export const getCiudadesApi = (params = {}) =>
  apiClient.get('/ciudades', { params: limpiarParams(params) })

export const getCiudadApi = (idCiudad) =>
  apiClient.get(`/ciudades/${idCiudad}`)

export const createCiudadApi = (payload) =>
  apiClient.post('/ciudades', payload)

export const updateCiudadApi = (idCiudad, payload) =>
  apiClient.put(`/ciudades/${idCiudad}`, payload)

export const deleteCiudadApi = (idCiudad) =>
  apiClient.delete(`/ciudades/${idCiudad}`)
