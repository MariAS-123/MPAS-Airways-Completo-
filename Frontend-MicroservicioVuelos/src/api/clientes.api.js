import apiClient from './axios'

function limpiarParams(params = {}) {
  const pares = {
    tipo_identificacion: 'TipoIdentificacion',
    tipoIdentificacion: 'TipoIdentificacion',
    numero_identificacion: 'NumeroIdentificacion',
    numeroIdentificacion: 'NumeroIdentificacion',
    nombres: 'Nombres',
    correo: 'Correo',
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

export const getClientesApi = (params = {}) =>
  apiClient.get('/clientes', { params: limpiarParams(params) })

export const getMiPerfilClienteApi = () =>
  apiClient.get('/clientes/portal/mi-perfil')

export const updateMiPerfilClienteApi = (datos) =>
  apiClient.put('/clientes/portal/mi-perfil', datos)

// GET /clientes/{id} — ADMINISTRADOR, AEROLINEA, CLIENTE (solo propio)
export const getClienteApi = (id) =>
  apiClient.get(`/clientes/${id}`)

export const createClienteApi = (datos) =>
  apiClient.post('/clientes', datos)

// PUT /clientes/{id} — ADMINISTRADOR, CLIENTE (solo propio)
export const updateClienteApi = (id, datos) =>
  apiClient.put(`/clientes/${id}`, datos)

export const deleteClienteApi = (id) =>
  apiClient.delete(`/clientes/${id}`)
