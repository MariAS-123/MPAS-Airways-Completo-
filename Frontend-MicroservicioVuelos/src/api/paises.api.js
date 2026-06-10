import apiClient from './axios'

function limpiarParams(params = {}) {
  const pares = {
    nombre: 'Nombre',
    codigo_iso2: 'CodigoIso2',
    codigoIso2: 'CodigoIso2',
    codigo_iso3: 'CodigoIso3',
    codigoIso3: 'CodigoIso3',
    continente: 'Continente',
    estado: 'Estado',
    page: 'PaginaActual',
    page_size: 'TamanoPagina',
  }

  return Object.entries(params).reduce((acc, [clave, valor]) => {
    if (valor === undefined || valor === null || valor === '') return acc
    acc[pares[clave] || clave] = valor
    return acc
  }, {})
}

export const getPaisesApi = (params = {}) =>
  apiClient.get('/paises', { params: limpiarParams(params) })

export const getPaisApi = (idPais) =>
  apiClient.get(`/paises/${idPais}`)

export const createPaisApi = (payload) =>
  apiClient.post('/paises', payload)

export const updatePaisApi = (idPais, payload) =>
  apiClient.put(`/paises/${idPais}`, payload)

export const deletePaisApi = (idPais) =>
  apiClient.delete(`/paises/${idPais}`)
