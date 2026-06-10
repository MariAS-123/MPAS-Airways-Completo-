import apiClient from './axios'

export const getFacturasApi = (params = {}, config = {}) =>
  apiClient.get('/facturas', {
    ...config,
    params: {
      page: 1,
      page_size: 100,
      ...params,
    },
  })

export const getClienteFacturasApi = () =>
  apiClient.get('/portal/cliente/facturas', { skipAuthRedirect: true })

export const getClienteReservaFacturaApi = (idReserva) =>
  apiClient.get(`/portal/cliente/reservas/${idReserva}/factura`, { skipAuthRedirect: true })

export const getFacturaApi = (idFactura) =>
  apiClient.get(`/facturas/${idFactura}`)

export const getFacturaReservaApi = (idReserva) =>
  apiClient.get(`/facturas/reserva/${idReserva}`, { skipAuthRedirect: true })

export const anularFacturaApi = (idFactura, payload = {}) =>
  apiClient.patch(`/facturas/${idFactura}/anular`, payload)
