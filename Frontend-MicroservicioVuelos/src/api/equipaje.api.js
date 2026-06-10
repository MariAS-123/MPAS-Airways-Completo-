import apiClient from './axios'

export const getEquipajesBoletoApi = (idBoleto) =>
  apiClient.get(`/boletos/${idBoleto}/equipaje`)

export const createEquipajeBoletoApi = (idBoleto, payload) =>
  apiClient.post(`/boletos/${idBoleto}/equipaje`, payload)

export const cambiarEstadoEquipajeApi = (idBoleto, idEquipaje, payload) =>
  apiClient.patch(`/boletos/${idBoleto}/equipaje/${idEquipaje}/estado`, payload)
