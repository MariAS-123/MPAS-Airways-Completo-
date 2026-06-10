<script setup>
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const KEY_PROGRESO = 'aerolinea_progreso'
const errorGeneral = ref('')

function obtenerPrimero(...valores) {
  return valores.flat().find((valor) => valor !== undefined && valor !== null && String(valor).trim() !== '')
}

function parseJwtPayload(token) {
  try {
    const base64 = String(token || '').split('.')[1]
    if (!base64) return null
    const normalized = base64.replace(/-/g, '+').replace(/_/g, '/')
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=')
    return JSON.parse(atob(padded))
  } catch {
    return null
  }
}

function obtenerIdVueloDesdeToken(token) {
  const payload = parseJwtPayload(token)
  const posibleCampoVuelo = buscarCampoVuelo(payload)

  return Number(
    obtenerPrimero(
      payload?.idVuelo,
      payload?.id_vuelo,
      payload?.IdVuelo,
      payload?.ID_VUELO,
      payload?.vueloId,
      payload?.vuelo_id,
      payload?.idVueloSeleccionado,
      payload?.id_vuelo_seleccionado,
      payload?.flightId,
      payload?.idFlight,
      payload?.id_flight,
      payload?.id_vuelo_reserva,
      payload?.vuelo?.idVuelo,
      payload?.vuelo?.id_vuelo,
      posibleCampoVuelo,
    ) || 0,
  )
}

function buscarCampoVuelo(valor) {
  if (!valor || typeof valor !== 'object') return null

  for (const [clave, contenido] of Object.entries(valor)) {
    if (/vuelo|flight/i.test(clave) && Number(contenido) > 0) {
      return contenido
    }

    if (contenido && typeof contenido === 'object') {
      const encontrado = buscarCampoVuelo(contenido)
      if (Number(encontrado) > 0) return encontrado
    }
  }

  return null
}

function leerPayload() {
  const query = route.query
  let guardado = null
  try {
    guardado = JSON.parse(localStorage.getItem(KEY_PROGRESO) || 'null')
  } catch {
    guardado = null
  }
  const token = String(obtenerPrimero(query.token, query.tokenJwt, query.jwt, guardado?.token) || '')

  return {
    idVuelo: Number(
      obtenerPrimero(
        query.id_vuelo,
        query.idVuelo,
        query.flightId,
        query.id_flight,
        query.vuelo,
        query.id,
        guardado?.idVuelo,
        obtenerIdVueloDesdeToken(token),
      ) || 0,
    ),
    idAsiento: Number(obtenerPrimero(query.id_asiento, query.idAsiento, guardado?.idAsiento) || 0) || null,
    precioAsiento: Number(obtenerPrimero(query.precio_asiento, query.precioAsiento, guardado?.precioAsiento) || 0),
    urlRetorno: String(obtenerPrimero(query.retorno, query.url_retorno, query.urlRetorno, query.returnUrl, query.redirect_uri, guardado?.urlRetorno) || ''),
    token,
  }
}

function guardarProgreso(payload) {
  localStorage.setItem(KEY_PROGRESO, JSON.stringify({
    idVuelo: payload.idVuelo,
    idAsiento: payload.idAsiento,
    precioAsiento: payload.precioAsiento,
    urlRetorno: payload.urlRetorno,
    token: payload.token,
  }))
}

onMounted(() => {
  const payload = leerPayload()

  if (!payload.idVuelo) {
    console.warn('[aerolinea-redirect:sin-id-vuelo]', {
      query: route.query,
      tokenPayload: parseJwtPayload(payload.token),
    })
    errorGeneral.value = 'No se recibio el id del vuelo seleccionado desde la aerolinea.'
    return
  }

  guardarProgreso(payload)

  router.replace({
    name: 'detalle-vuelo',
    params: { id: payload.idVuelo },
    query: {
      origen: 'aerolinea',
      url_retorno: payload.urlRetorno || undefined,
      tokenJwt: payload.token || undefined,
    },
  })
})
</script>

<template>
  <section class="min-h-[calc(100vh-64px)] bg-background px-4 py-10">
    <div class="mx-auto max-w-3xl rounded-[28px] bg-white p-8 shadow-sm">
      <p class="text-sm font-semibold uppercase tracking-[0.28em] text-gold-dark">Aerolinea</p>
      <h1 class="mt-3 text-3xl font-bold text-navy">Preparando tu reserva</h1>

      <div v-if="errorGeneral" class="mt-6 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
        {{ errorGeneral }}
      </div>

      <p v-else class="mt-6 text-text-muted">
        Redirigiendo al vuelo seleccionado para continuar con pasajeros, asientos, equipaje, pago y confirmacion...
      </p>
    </div>
  </section>
</template>
