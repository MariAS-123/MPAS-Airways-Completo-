import apiClient from './axios'

function limpiarParams(params = {}) {
  const pares = {
    username: 'username',
    correo: 'correo',
    activo: 'activo',
    page: 'page',
    page_size: 'page_size',
  }

  return Object.entries(params).reduce((acc, [clave, valor]) => {
    if (valor === undefined || valor === null || valor === '') return acc
    acc[pares[clave] || clave] = valor
    return acc
  }, {})
}

export const getUsuariosApi = (params = {}) =>
  apiClient.get('/usuarios', { params: limpiarParams(params) })

export const getUsuarioApi = (idUsuario) =>
  apiClient.get(`/usuarios/${idUsuario}`)

export const createUsuarioApi = (payload) =>
  apiClient.post('/usuarios', payload)

export const updateUsuarioApi = (idUsuario, payload) =>
  apiClient.put(`/usuarios/${idUsuario}`, payload)

export const deleteUsuarioApi = (idUsuario) =>
  apiClient.delete(`/usuarios/${idUsuario}`)

export const asignarRolUsuarioApi = (idUsuario, payload) =>
  apiClient.post(`/usuarios/${idUsuario}/roles`, payload)

export const quitarRolUsuarioApi = (idUsuario, idRol) =>
  apiClient.delete(`/usuarios/${idUsuario}/roles/${idRol}`)
