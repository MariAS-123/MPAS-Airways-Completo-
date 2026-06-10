<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { getClienteReservasApi } from '@/api/reservas.api'
import { getClienteBoletosApi } from '@/api/boletos.api'
import { useAutenticacionStore } from '@/stores/autenticacion.store'
import { useClienteStore } from '@/stores/cliente.store'
import { deepValue, extractItems, leerPortalReservas, longDate, shortTime } from '@/utils/portalCliente'

const router = useRouter()
const auth = useAutenticacionStore()
const cliente = useClienteStore()

const cargando = ref(true)
const error = ref('')
const reservas = ref([])
const KEY_CONFIRMACION = 'mpas_confirmacion'
const KEY_RESERVA_DETALLE = 'mpas_reserva_detalle'

const nombreVisible = computed(() =>
  cliente.nombreMostrado || auth.usuario?.username || 'Cliente',
)

const reservasProximas = computed(() => {
  const activas = reservas.value.filter((item) => !['CAN', 'FIN'].includes(item.estado))
  return activas.slice(0, 6)
})

function normalizarReserva(item) {
  const vuelo = deepValue(item, ['vuelo']) || {}
  const origen =
    deepValue(item, ['codigoOrigen', 'codigo_origen', 'iataOrigen', 'origenCodigo']) ||
    deepValue(vuelo, ['codigoOrigen', 'codigo_origen', 'iataOrigen']) ||
    ''
  const destino =
    deepValue(item, ['codigoDestino', 'codigo_destino', 'iataDestino', 'destinoCodigo']) ||
    deepValue(vuelo, ['codigoDestino', 'codigo_destino', 'iataDestino']) ||
    ''
  const ciudadOrigen =
    deepValue(item, ['ciudadOrigen', 'ciudad_origen', 'nombreCiudadOrigen']) ||
    deepValue(vuelo, ['ciudadOrigen', 'ciudad_origen']) ||
    origen
  const ciudadDestino =
    deepValue(item, ['ciudadDestino', 'ciudad_destino', 'nombreCiudadDestino']) ||
    deepValue(vuelo, ['ciudadDestino', 'ciudad_destino']) ||
    destino

  return {
    idReserva: deepValue(item, ['idReserva', 'id_reserva', 'id']) || null,
    codigoReserva: deepValue(item, ['codigoReserva', 'codigo_reserva']) || '',
    estado: deepValue(item, ['estadoReserva', 'estado_reserva']) || 'EMI',
    fechaInicio:
      deepValue(item, ['fechaInicio', 'fecha_inicio']) ||
      deepValue(vuelo, ['fechaInicio', 'fechaHoraSalida', 'fecha_hora_salida']) ||
      '',
    numeroVuelo:
      deepValue(item, ['numeroVuelo', 'numero_vuelo']) ||
      deepValue(vuelo, ['numeroVuelo', 'numero_vuelo']) ||
      '',
    origen,
    destino,
    ciudadOrigen,
    ciudadDestino,
  }
}

function fallbackDesdeConfirmacion() {
  const locales = leerPortalReservas()
  if (locales.length) {
    return locales.map((item) => {
      const [origen = '', destino = ''] = String(item.ruta || '')
        .split('-')
        .map((parte) => parte.trim())

      return {
        idReserva: item.idReserva || null,
        codigoReserva: item.codigoReserva || '',
        estado: 'EMI',
        fechaInicio: item.fecha || '',
        numeroVuelo: item.numeroVuelo || '',
        origen,
        destino,
        ciudadOrigen: origen,
        ciudadDestino: destino,
      }
    })
  }

  try {
    const data = JSON.parse(sessionStorage.getItem(KEY_CONFIRMACION) || 'null')
    if (!data) return []

    const [origen = '', destino = ''] = String(data.ruta || '')
      .split('-')
      .map((parte) => parte.trim())

    return [
      {
        idReserva: data.idReserva || 'local-confirmacion',
        codigoReserva: data.codigoReserva || '',
        estado: 'EMI',
        fechaInicio: data.fecha || '',
        numeroVuelo: data.numeroVuelo || '',
        origen,
        destino,
        ciudadOrigen: origen,
        ciudadDestino: destino,
      },
    ]
  } catch {
    return []
  }
}

function fallbackDesdeBoletos(payload) {
  const agrupadas = new Map()

  extractItems(payload).forEach((item) => {
    const vuelo = deepValue(item, ['vuelo']) || {}
    const idReserva =
      deepValue(item, ['idReserva', 'id_reserva']) ||
      deepValue(vuelo, ['idReserva', 'id_reserva']) ||
      null
    const codigoReserva =
      deepValue(item, ['codigoReserva', 'codigo_reserva']) ||
      deepValue(vuelo, ['codigoReserva', 'codigo_reserva']) ||
      ''
    const clave = idReserva || codigoReserva || deepValue(item, ['numeroBoleto', 'numero_boleto']) || crypto.randomUUID()

    if (!agrupadas.has(clave)) {
      agrupadas.set(clave, {
        idReserva,
        codigoReserva,
        estado: 'EMI',
        fechaInicio:
          deepValue(item, ['fechaVuelo', 'fecha_vuelo', 'fechaInicio', 'fecha_inicio']) ||
          deepValue(vuelo, ['fechaHoraSalida', 'fecha_hora_salida']) ||
          '',
        numeroVuelo:
          deepValue(item, ['numeroVuelo', 'numero_vuelo']) ||
          deepValue(vuelo, ['numeroVuelo', 'numero_vuelo']) ||
          '',
        origen:
          deepValue(item, ['codigoOrigen', 'codigo_origen']) ||
          deepValue(vuelo, ['codigoOrigen', 'codigo_origen']) ||
          '',
        destino:
          deepValue(item, ['codigoDestino', 'codigo_destino']) ||
          deepValue(vuelo, ['codigoDestino', 'codigo_destino']) ||
          '',
        ciudadOrigen:
          deepValue(item, ['ciudadOrigen', 'ciudad_origen', 'nombreCiudadOrigen']) ||
          deepValue(vuelo, ['ciudadOrigen', 'ciudad_origen']) ||
          deepValue(item, ['codigoOrigen', 'codigo_origen']) ||
          '',
        ciudadDestino:
          deepValue(item, ['ciudadDestino', 'ciudad_destino', 'nombreCiudadDestino']) ||
          deepValue(vuelo, ['ciudadDestino', 'ciudad_destino']) ||
          deepValue(item, ['codigoDestino', 'codigo_destino']) ||
          '',
      })
    }
  })

  return Array.from(agrupadas.values())
}

function verDetalleReserva(reserva) {
  sessionStorage.setItem(KEY_RESERVA_DETALLE, JSON.stringify(reserva))
  router.push({ name: 'cliente-reserva-detalle', params: { id: reserva.idReserva } })
}

async function cargarReservasCliente() {
  const respuesta = await getClienteReservasApi()
  return extractItems(respuesta)
}

async function cargarBoletosCliente() {
  const respuesta = await getClienteBoletosApi()
  return extractItems(respuesta)
}

async function cargarReservas() {
  cargando.value = true
  error.value = ''

  try {
    const items = await cargarReservasCliente()
    reservas.value = items.map(normalizarReserva)
  } catch (err) {
    try {
      const boletos = await cargarBoletosCliente()
      reservas.value = fallbackDesdeBoletos(boletos)
    } catch {
      reservas.value = []
    }

    if (!reservas.value.length) {
      reservas.value = fallbackDesdeConfirmacion()
    }

    if (!reservas.value.length) {
      error.value = err.response?.data?.message || 'No se pudieron cargar los viajes del cliente.'
    } else {
      error.value = ''
    }
  } finally {
    cargando.value = false
  }
}

onMounted(cargarReservas)
</script>

<template>
  <section class="space-y-6">
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-3xl font-bold text-navy">Bienvenido, {{ nombreVisible }}</h1>
        <p class="mt-2 text-text-muted">Gestiona tus reservas y vuelos desde aquí.</p>
      </div>

      <button
        type="button"
        class="rounded-2xl border border-slate-300 px-5 py-3 font-semibold text-navy transition-colors hover:bg-slate-50"
        @click="router.push({ name: 'cliente-perfil' })"
      >
        Mi Perfil
      </button>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <div class="grid gap-6 lg:grid-cols-[1.15fr_0.95fr]">
      <section class="rounded-[30px] bg-white p-8 shadow-sm">
        <div class="flex items-center justify-between gap-4">
          <h2 class="text-2xl font-semibold text-navy">Próximos Vuelos</h2>
          <button
            type="button"
            class="text-sm font-semibold text-blue-accent transition-colors hover:text-navy"
            @click="router.push({ name: 'buscar-vuelos' })"
          >
            Nuevo vuelo
          </button>
        </div>

        <div v-if="cargando" class="mt-6 text-text-muted">Cargando reservas...</div>

        <div v-else-if="!reservasProximas.length" class="mt-6 rounded-[24px] bg-slate-50 px-6 py-10 text-center text-text-muted">
          Aún no tienes vuelos próximos registrados.
        </div>

        <div v-else class="mt-6 space-y-5">
          <article
            v-for="reserva in reservasProximas"
            :key="reserva.idReserva || `${reserva.numeroVuelo}-${reserva.fechaInicio}`"
            class="flex flex-col gap-5 rounded-[24px] bg-slate-50 px-5 py-5 shadow-sm sm:flex-row sm:items-center sm:justify-between"
          >
            <div class="flex items-center gap-4">
              <div class="flex h-16 w-16 items-center justify-center rounded-[20px] bg-slate-200 text-navy">
                <svg class="h-8 w-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M10.18 9 6 11l-2 4 5.5-1.5L12 18l2-1-1.5-4.5L18 10l-1-2-5 1.5L10 5 8 6l2.18 3Z" />
                </svg>
              </div>

              <div>
                <p class="text-2xl font-semibold text-navy">{{ reserva.ciudadOrigen }} → {{ reserva.ciudadDestino }}</p>
                <p class="mt-1 text-text-muted">{{ longDate(reserva.fechaInicio) }} · {{ shortTime(reserva.fechaInicio) }}</p>
              </div>
            </div>

            <p class="text-sm font-semibold text-blue-accent sm:self-end">
              Reserva {{ reserva.codigoReserva || reserva.idReserva || 'pendiente' }}
            </p>

            <button
              v-if="reserva.idReserva"
              type="button"
              class="rounded-2xl border border-blue-accent px-5 py-3 font-semibold text-blue-accent transition-colors hover:bg-blue-accent hover:text-white"
              @click="verDetalleReserva(reserva)"
            >
              Ver detalles
            </button>
          </article>
        </div>
      </section>

      <section class="rounded-[30px] bg-white p-8 shadow-sm">
        <h2 class="text-2xl font-semibold text-navy">Accesos Rápidos</h2>

        <div class="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-1 xl:grid-cols-2">
          <button
            type="button"
            class="rounded-[24px] bg-slate-50 px-6 py-8 text-left transition-colors hover:bg-slate-100"
            @click="router.push({ name: 'cliente-facturas' })"
          >
            <div class="flex h-14 w-14 items-center justify-center rounded-[18px] bg-violet-100 text-violet-500">
              <svg class="h-7 w-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 12h6m-6 4h6M7 4h8l4 4v12a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1Z" />
              </svg>
            </div>
            <p class="mt-5 text-2xl font-semibold text-navy">Mis Facturas</p>
            <p class="mt-2 text-text-muted">Consulta tus documentos emitidos.</p>
          </button>

          <button
            type="button"
            class="rounded-[24px] bg-slate-50 px-6 py-8 text-left transition-colors hover:bg-slate-100"
            @click="router.push({ name: 'cliente-boletos' })"
          >
            <div class="flex h-14 w-14 items-center justify-center rounded-[18px] bg-emerald-100 text-emerald-500">
              <svg class="h-7 w-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M5 9a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v2a2 2 0 0 0 0 4v2a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2v-2a2 2 0 0 0 0-4V9Z" />
              </svg>
            </div>
            <p class="mt-5 text-2xl font-semibold text-navy">Mis Boletos</p>
            <p class="mt-2 text-text-muted">Revisa los boletos asociados a tus reservas.</p>
          </button>

          <button
            type="button"
            class="rounded-[24px] bg-navy px-6 py-8 text-left text-white transition-colors hover:bg-navy/90 sm:col-span-2 lg:col-span-1 xl:col-span-2"
            @click="router.push({ name: 'buscar-vuelos' })"
          >
            <div class="flex h-14 w-14 items-center justify-center rounded-[18px] bg-white/10 text-white">
              <svg class="h-7 w-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M10.18 9 6 11l-2 4 5.5-1.5L12 18l2-1-1.5-4.5L18 10l-1-2-5 1.5L10 5 8 6l2.18 3Z" />
              </svg>
            </div>
            <p class="mt-5 text-2xl font-semibold">Nuevo Vuelo</p>
            <p class="mt-2 text-white/75">Explora otra vez el buscador público.</p>
          </button>
        </div>
      </section>
    </div>
  </section>
</template>
