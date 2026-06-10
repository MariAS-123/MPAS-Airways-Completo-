import apiClient from './axios'

export const getBoletosApi = (params = {}, config = {}) =>
  apiClient.get('/boletos', {
    ...config,
    params: {
      ...params,
      idReserva: params.idReserva ?? params.id_reserva,
    },
  })

export const getClienteBoletosApi = () =>
  apiClient.get('/portal/cliente/boletos', { skipAuthRedirect: true })

export const getClienteReservaBoletosApi = (idReserva) =>
  apiClient.get(`/portal/cliente/reservas/${idReserva}/boleto`, { skipAuthRedirect: true })

export const getBoletoApi = (idBoleto) =>
  apiClient.get(`/boletos/${idBoleto}`)
