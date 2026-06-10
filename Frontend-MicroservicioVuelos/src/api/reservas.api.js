import apiClient from './axios'

export const getReservasApi = (params = {}, config = {}) =>
  apiClient.get('/reservas', { ...config, params })

export const getReservaApi = (idReserva) =>
  apiClient.get(`/reservas/${idReserva}`)

export const createReservaApi = (payload) =>
  apiClient.post('/reservas', payload)

export const getClienteReservasApi = () =>
  apiClient.get('/portal/cliente/reservas', { skipAuthRedirect: true })

export const getClienteReservaApi = (idReserva) =>
  apiClient.get(`/portal/cliente/reservas/${idReserva}/detalle`, { skipAuthRedirect: true })

export const getClienteReservaDetalleApi = (idReserva) =>
  apiClient.get(`/portal/cliente/reservas/${idReserva}/detalle`, { skipAuthRedirect: true })

export const getClienteReservaPorCodigoApi = (codigo) =>
  apiClient.get(`/portal/cliente/reservas/by-codigo/${codigo}`, { skipAuthRedirect: true })

export const pagarReservaApi = (idReserva, payload) =>
  apiClient.patch(`/reservas/${idReserva}/pagar`, payload)

export const cambiarEstadoReservaApi = (idReserva, payload) =>
  apiClient.patch(`/reservas/${idReserva}/cancelar`, {
    motivo: payload?.motivo ?? payload?.motivo_cancelacion ?? payload?.motivoCancelacion ?? undefined,
  })

export const deleteReservaApi = (idReserva) =>
  apiClient.patch(`/reservas/${idReserva}/cancelar`, {})

export const getReservaBoletosApi = (idReserva) =>
  apiClient.get(`/reservas/${idReserva}/boletos`, { skipAuthRedirect: true })
