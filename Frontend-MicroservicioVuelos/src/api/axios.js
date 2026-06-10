import axios from 'axios'

const apiBaseUrl = (
  import.meta.env.VITE_API_URL ||
  'https://middlewaremicroserviciovuelos-bpf7bsgqh8g2cea5.eastus-01.azurewebsites.net/api/v1'
).trim()
const apiClient = axios.create({
  baseURL: apiBaseUrl.replace(/\/$/, ''),
  headers: {
    'Content-Type': 'application/json',
  },
})

const esDev = import.meta.env.DEV

function resumirPayload(payload) {
  if (Array.isArray(payload)) return { tipo: 'array', longitud: payload.length }
  if (!payload || typeof payload !== 'object') return payload

  const data = payload.data
  const items = Array.isArray(data?.items)
    ? data.items.length
    : Array.isArray(payload.items)
      ? payload.items.length
      : Array.isArray(data)
        ? data.length
        : Array.isArray(payload.records)
          ? payload.records.length
          : undefined

  return {
    claves: Object.keys(payload),
    dataClaves: data && typeof data === 'object' && !Array.isArray(data) ? Object.keys(data) : undefined,
    items,
  }
}

function logApi(etiqueta, payload) {
  if (!esDev) return
  console.info(etiqueta, JSON.stringify(payload))
}

// Lee token de localStorage (sesión persistente) o sessionStorage (sesión temporal)
function getToken() {
  return localStorage.getItem('token') || sessionStorage.getItem('token') || null
}

function limpiarSesion() {
  ;['token', 'usuario', 'rol'].forEach((k) => {
    localStorage.removeItem(k)
    sessionStorage.removeItem(k)
  })
}

apiClient.interceptors.request.use((config) => {
  const token = getToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  logApi('[api:req]', {
    method: config.method?.toUpperCase(),
    baseURL: config.baseURL,
    url: config.url,
    params: config.params ?? null,
    hasAuth: Boolean(token),
  })
  return config
})

apiClient.interceptors.response.use(
  (response) => {
    logApi('[api:res]', {
      method: response.config?.method?.toUpperCase(),
      url: response.config?.url,
      status: response.status,
      params: response.config?.params ?? null,
      payload: resumirPayload(response.data),
    })
    return response
  },
  (error) => {
    const status = error.response?.status

    if (esDev) {
      console.error('[api:err]', JSON.stringify({
        method: error.config?.method?.toUpperCase(),
        url: error.config?.url,
        status: status ?? null,
        params: error.config?.params ?? null,
        data: error.response?.data ?? null,
        message: error.message,
      }))
    }

    if (status === 401 && !error.config?.skipAuthRedirect) {
      limpiarSesion()
      window.location.href = '/login'
    } else if (status === 403 && !error.config?.skipAuthRedirect) {
      window.location.href = '/no-autorizado'
    }

    return Promise.reject(error)
  },
)

export default apiClient
