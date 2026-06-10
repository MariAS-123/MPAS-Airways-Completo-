export function parseJwtPayload(token) {
  try {
    const base64 = token.split('.')[1]
    if (!base64) return null
    const normalized = base64.replace(/-/g, '+').replace(/_/g, '/')
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=')
    return JSON.parse(atob(padded))
  } catch {
    return null
  }
}

export function deepValue(obj, keys) {
  for (const key of keys) {
    if (obj?.[key] !== undefined && obj?.[key] !== null) return obj[key]
  }

  for (const value of Object.values(obj || {})) {
    if (value && typeof value === 'object') {
      const found = deepValue(value, keys)
      if (found !== undefined && found !== null) return found
    }
  }

  return null
}

function collectArrays(value, arrays = []) {
  if (Array.isArray(value)) {
    arrays.push(value)
    value.forEach((item) => collectArrays(item, arrays))
    return arrays
  }

  if (value && typeof value === 'object') {
    Object.values(value).forEach((item) => collectArrays(item, arrays))
  }

  return arrays
}

export function extractItems(responseLike) {
  const raw = responseLike?.data?.data ?? responseLike?.data ?? responseLike ?? null
  if (Array.isArray(raw)) return raw
  if (Array.isArray(raw?.items)) return raw.items
  const arrays = collectArrays(raw)
  return arrays[0] ?? []
}

export function resolveClienteId(auth, cliente = null) {
  const payload = parseJwtPayload(auth?.token || '')
  const keys = ['idCliente', 'id_cliente', 'clienteId', 'cliente_id', 'id']
  const candidate =
    deepValue(cliente?.perfil, keys) ||
    deepValue(cliente, keys) ||
    deepValue(auth?.usuario, keys)

  return Number(
    candidate ??
      payload?.idCliente ??
      payload?.id_cliente ??
      payload?.clienteId ??
      payload?.cliente_id ??
      0,
  ) || 0
}

export function cleanLabel(value) {
  return String(value || '').replace(/\d{6,}$/u, '').trim()
}

export function money(value) {
  return new Intl.NumberFormat('es-EC', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(Number(value || 0))
}

export function longDate(value) {
  if (!value) return 'Fecha por confirmar'
  return new Intl.DateTimeFormat('es-EC', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(new Date(value))
}

export function shortTime(value) {
  if (!value) return '--:--'
  return new Intl.DateTimeFormat('es-EC', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(new Date(value))
}

export const KEY_PORTAL_RESERVAS = 'mpas_portal_reservas'

export function leerPortalReservas() {
  try {
    const raw = JSON.parse(localStorage.getItem(KEY_PORTAL_RESERVAS) || '[]')
    return Array.isArray(raw) ? raw : []
  } catch {
    return []
  }
}

export function guardarPortalReserva(reserva) {
  if (!reserva) return

  const actuales = leerPortalReservas()
  const clave = reserva.idReserva || `${reserva.codigoReserva || ''}-${reserva.numeroVuelo || ''}-${reserva.fecha || ''}`
  const filtradas = actuales.filter((item) => {
    const itemClave = item.idReserva || `${item.codigoReserva || ''}-${item.numeroVuelo || ''}-${item.fecha || ''}`
    return itemClave !== clave
  })

  localStorage.setItem(KEY_PORTAL_RESERVAS, JSON.stringify([reserva, ...filtradas].slice(0, 20)))
}
