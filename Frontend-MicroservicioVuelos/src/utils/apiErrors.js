export function extraerMensajeApi(error) {
  const data = error?.response?.data

  if (data && typeof data === 'object') {
    if (typeof data.message === 'string' && data.message.trim()) {
      if (Array.isArray(data.errors) && data.errors.length) {
        return `${data.message} ${data.errors.join(' ')}`.trim()
      }
      return data.message.trim()
    }

    if (typeof data.detail === 'string' && data.detail.trim()) return data.detail.trim()
    if (typeof data.title === 'string' && data.title.trim() && !data.title.includes('error occurred')) {
      return data.title.trim()
    }
  }

  if (error?.message && error.message !== 'Network Error') return error.message
  if (error?.message === 'Network Error') {
    return 'No se pudo contactar al servidor. Revisa tu conexión e intenta de nuevo.'
  }

  return 'No se pudo completar el pago.'
}

export function mensajePagoReservaAmigable(texto) {
  const raw = String(texto || '').trim()
  if (!raw) return 'No se pudo completar el pago.'

  if (/asiento\s+\d+\s+no est[aá] disponible/i.test(raw) || /ya no est[aá] disponible para completar el pago/i.test(raw)) {
    return 'Ese asiento ya no está disponible. Alguien lo reservó antes que tú. Vuelve a elegir otro asiento e intenta de nuevo.'
  }

  if (/ya fue reservado en este vuelo/i.test(raw)) {
    return 'Ese asiento ya fue reservado en este vuelo. Elige otro asiento e intenta de nuevo.'
  }

  if (/ya tiene una reserva activa en este vuelo/i.test(raw)) {
    return 'Este pasajero ya tiene una reserva activa en este vuelo.'
  }

  return raw
}
