import apiClient from './axios'

function limpiarParams(params = {}, tipo = 'default') {
  const paresDefault = {
    id_pais: 'IdPais',
    idPais: 'IdPais',
    id_ciudad: 'IdCiudad',
    idCiudad: 'IdCiudad',
    codigo_iata: 'CodigoIata',
    codigoIata: 'CodigoIata',
    codigo_icao: 'CodigoIcao',
    codigoIcao: 'CodigoIcao',
    nombre: 'Nombre',
    estado: 'Estado',
    zona_horaria: 'ZonaHoraria',
    zonaHoraria: 'ZonaHoraria',
    page: 'Page',
    page_size: 'PageSize',
  }
  const paresAeropuertos = {
    codigoIata: 'codigo_iata',
    codigoIcao: 'codigo_icao',
    idCiudad: 'id_ciudad',
    idPais: 'id_pais',
    zonaHoraria: 'zona_horaria',
  }
  const paresPaises = {
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
  const pares = tipo === 'paises' ? paresPaises : tipo === 'aeropuertos' ? paresAeropuertos : paresDefault
  const limpios = {}

  Object.entries(params).forEach(([clave, valor]) => {
    if (valor === undefined || valor === null || valor === '') return

    if ((clave === 'id_pais' || clave === 'id_ciudad') && Number(valor) <= 0) return

    const claveApi = pares[clave] || clave
    limpios[claveApi === 'Estado' ? 'estado' : claveApi] = valor
  })

  const pageKey = tipo === 'paises' ? 'PaginaActual' : tipo === 'aeropuertos' ? 'page' : 'Page'
  const pageSizeKey = tipo === 'paises' ? 'TamanoPagina' : tipo === 'aeropuertos' ? 'page_size' : 'PageSize'
  const page = Number(limpios[pageKey] ?? 1)
  const pageSize = Number(limpios[pageSizeKey] ?? 200)

  limpios[pageKey] = Number.isFinite(page) && page > 0 ? page : 1
  limpios[pageSizeKey] = Number.isFinite(pageSize)
    ? Math.min(Math.max(pageSize, 1), 200)
    : 200

  return limpios
}

// GET /paises — AllowAnonymous
export const getPaisesApi = (params = {}) =>
  apiClient.get('/paises', { params: limpiarParams(params, 'paises') })

// GET /ciudades — AllowAnonymous. Filtrar por id_pais para cargar por país.
export const getCiudadesApi = (params = {}) =>
  apiClient.get('/ciudades', { params: limpiarParams(params) })

// GET /aeropuertos — AllowAnonymous
export const getAeropuertosApi = (params = {}) =>
  apiClient.get('/aeropuertos', { params: limpiarParams(params, 'aeropuertos') })
