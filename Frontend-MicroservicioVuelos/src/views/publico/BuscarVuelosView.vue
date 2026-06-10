<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { buscarVuelosBookingApi, getVuelosApi } from '@/api/vuelos.api'
import { useCatalogosStore } from '@/stores/catalogos.store'
import { useReservaStore } from '@/stores/reserva.store'
import { extractItems } from '@/utils/portalCliente'

const route = useRoute()
const router = useRouter()
const catalogos = useCatalogosStore()
const reserva = useReservaStore()

const cargando = ref(false)
const cargandoDestacados = ref(false)
const errorGeneral = ref('')
const resultados = ref([])
const destacados = ref([])
const vueloExpandido = ref(null)
const tipoViaje = ref(route.query.tipo || 'IDA_VUELTA')
const form = ref({
  origen: route.query.origen || '',
  destino: route.query.destino || '',
  salida: route.query.salida || '',
  regreso: route.query.regreso || '',
  pasajeros: route.query.pasajeros || '1',
  clase: route.query.clase || 'ECONOMICA',
})

const opcionesAeropuertos = computed(() => catalogos.opcionesAeropuertos)
const filtrosActivos = computed(() => !!form.value.origen || !!form.value.destino || !!form.value.salida)
const listaVisible = computed(() => (filtrosActivos.value ? resultados.value : destacados.value))
const ESTADOS_VISIBLES_PUBLICO = new Set(['PROGRAMADO'])

function normalizarLista(data) {
  if (Array.isArray(data)) return data
  if (Array.isArray(data?.items)) return data.items
  if (Array.isArray(data?.resultados)) return data.resultados
  if (Array.isArray(data?.records)) return data.records
  return []
}

function obtenerCampo(obj, claves, fallback = null) {
  for (const clave of claves) {
    if (obj?.[clave] !== undefined && obj?.[clave] !== null) return obj[clave]
  }
  return fallback
}

function limpiarNombreCatalogo(nombre = '') {
  if (!nombre || typeof nombre === 'object') return ''
  return String(nombre).replace(/\d{6,}$/u, '').trim()
}

function textoPlano(valor, claves = []) {
  if (!valor) return ''
  if (typeof valor !== 'object') return limpiarNombreCatalogo(valor)

  for (const clave of claves) {
    const encontrado = valor?.[clave]
    if (encontrado !== undefined && encontrado !== null && typeof encontrado !== 'object') {
      return limpiarNombreCatalogo(encontrado)
    }
  }

  return ''
}

function moneda(valor) {
  return new Intl.NumberFormat('es-EC', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(Number(valor || 0))
}

function horaLegible(valor) {
  if (!valor) return '--:--'
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return valor
  return new Intl.DateTimeFormat('es-EC', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(fecha)
}

function fechaLegible(valor) {
  if (!valor) return 'Fecha por confirmar'
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return valor
  return new Intl.DateTimeFormat('es-EC', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(fecha)
}

function duracionLegible(minutos) {
  const total = Number(minutos || 0)
  const horas = Math.floor(total / 60)
  const mins = total % 60
  if (!horas) return `${mins}m`
  if (!mins) return `${horas}h`
  return `${horas}h ${mins}m`
}

function aeropuertoPorId(id) {
  return catalogos.aeropuertos.find((a) => String(a.idAeropuerto ?? a.id_aeropuerto ?? a.id) === String(id))
}

function codigoAeropuerto(id) {
  const aeropuerto = aeropuertoPorId(id)
  return obtenerCampo(aeropuerto, ['codigoIata', 'codigo_iata', 'iata'], '---')
}

function nombreCiudadAeropuerto(id) {
  const aeropuerto = aeropuertoPorId(id)
  const ciudad = textoPlano(
    obtenerCampo(aeropuerto, ['ciudad', 'nombreCiudad', 'ciudad_nombre']),
    ['nombre', 'nombreCiudad', 'nombre_ciudad'],
  )
  if (ciudad) return ciudad

  const nombre = obtenerCampo(aeropuerto, ['nombre', 'nombreAeropuerto'], 'Aeropuerto')
  return limpiarNombreCatalogo(nombre).replace(/^Aeropuerto\s+/i, '').trim() || 'Ciudad'
}

function nombreAeropuerto(id) {
  const aeropuerto = aeropuertoPorId(id)
  if (!aeropuerto) return ''
  const codigo = textoPlano(obtenerCampo(aeropuerto, ['codigoIata', 'codigo_iata', 'iata']))
  const nombre = textoPlano(obtenerCampo(aeropuerto, ['nombre', 'nombreAeropuerto']))
  return [codigo, nombre].filter(Boolean).join(' - ')
}

function claseLegible(clase) {
  if (clase === 'EJECUTIVA') return 'Ejecutiva'
  if (clase === 'PRIMERA') return 'Primera'
  return 'Economica'
}

function normalizarVuelo(vuelo) {
  const idVuelo = obtenerCampo(vuelo, ['idVuelo', 'id_vuelo', 'id'])
  const origenId = obtenerCampo(vuelo, ['idAeropuertoOrigen', 'id_aeropuerto_origen'])
  const destinoId = obtenerCampo(vuelo, ['idAeropuertoDestino', 'id_aeropuerto_destino'])
  const precioBase = Number(obtenerCampo(vuelo, ['precioBase', 'precio_base'], 0))
  const escalas = obtenerCampo(vuelo, ['numeroEscalas', 'cantidadEscalas', 'escalas_count'], 0)
  const numeroVuelo = obtenerCampo(vuelo, ['numeroVuelo', 'numero_vuelo'], `AV${String(idVuelo).padStart(4, '0')}`)

  return {
    idVuelo,
    numeroVuelo,
    codigoDisplay: numeroVuelo,
    origenId,
    destinoId,
    origen: obtenerCampo(vuelo, ['origenNombre', 'origen', 'nombreOrigen'], nombreAeropuerto(origenId)),
    destino: obtenerCampo(vuelo, ['destinoNombre', 'destino', 'nombreDestino'], nombreAeropuerto(destinoId)),
    fechaHoraSalida: obtenerCampo(vuelo, ['fechaHoraSalida', 'fecha_hora_salida']),
    fechaHoraLlegada: obtenerCampo(vuelo, ['fechaHoraLlegada', 'fecha_hora_llegada']),
    duracionMin: Number(obtenerCampo(vuelo, ['duracionMin', 'duracion_min'], 0)),
    precioBase,
    escalas: Number(escalas || 0),
    estado: obtenerCampo(vuelo, ['estadoVuelo', 'estado_vuelo'], 'PROGRAMADO'),
    codigoOrigen: codigoAeropuerto(origenId),
    codigoDestino: codigoAeropuerto(destinoId),
    ciudadOrigen: nombreCiudadAeropuerto(origenId),
    ciudadDestino: nombreCiudadAeropuerto(destinoId),
    aeronave: 'Boeing 787',
  }
}

function esVueloVisiblePublico(vuelo) {
  const estado = String(vuelo?.estado || '').trim().toUpperCase()
  return ESTADOS_VISIBLES_PUBLICO.has(estado)
}

function ordenarVuelos(a, b) {
  return new Date(a.fechaHoraSalida).getTime() - new Date(b.fechaHoraSalida).getTime()
}

function mapearVuelosPublicos(respuesta) {
  return extractItems(respuesta).map(normalizarVuelo).filter(esVueloVisiblePublico).sort(ordenarVuelos)
}

function extraerMetaPaginacion(respuesta) {
  const raw = respuesta?.data?.data ?? respuesta?.data ?? respuesta ?? {}
  const bloques = [raw, raw?.pagination, raw?.paginacion, raw?.meta, raw?.data].filter(Boolean)

  for (const bloque of bloques) {
    const totalPages = Number(
      obtenerCampo(bloque, ['totalPages', 'total_pages', 'paginasTotales', 'lastPage', 'last_page'], 0),
    )
    const currentPage = Number(
      obtenerCampo(bloque, ['page', 'currentPage', 'current_page', 'paginaActual', 'pageNumber'], 0),
    )

    if (totalPages > 0 || currentPage > 0) {
      return { totalPages, currentPage }
    }
  }

  return { totalPages: 0, currentPage: 0 }
}

async function cargarVuelosPaginados(params = {}) {
  const acumulados = []
  const vistos = new Set()
  const pageSize = 100
  const maxPaginas = 40
  let paginasSinNuevos = 0

  for (let page = 1; page <= maxPaginas; page += 1) {
    const respuesta = await buscarVuelosBookingApi({
      estado_vuelo: 'PROGRAMADO',
      page,
      page_size: pageSize,
      ...params,
    })

    const items = extractItems(respuesta)
    if (!items.length) break

    let nuevosEnPagina = 0
    for (const item of items) {
      const idVuelo = obtenerCampo(item, ['idVuelo', 'id_vuelo', 'id'])
      const clave = String(idVuelo ?? `${page}-${acumulados.length}`)
      if (vistos.has(clave)) continue
      vistos.add(clave)
      acumulados.push(item)
      nuevosEnPagina += 1
    }

    const { totalPages, currentPage } = extraerMetaPaginacion(respuesta)
    if (totalPages > 0 && (currentPage || page) >= totalPages) break

    if (nuevosEnPagina === 0) {
      paginasSinNuevos += 1
      if (paginasSinNuevos >= 2) break
    } else {
      paginasSinNuevos = 0
    }
  }

  return acumulados
}

async function cargarVuelosGeneralesPaginados() {
  const acumulados = []
  const vistos = new Set()
  const pageSize = 100
  const maxPaginas = 40

  for (let page = 1; page <= maxPaginas; page += 1) {
    const respuesta = await getVuelosApi({
      estado_vuelo: 'PROGRAMADO',
      page,
      page_size: pageSize,
    })

    const items = extractItems(respuesta)
    if (!items.length) break

    for (const item of items) {
      const idVuelo = obtenerCampo(item, ['idVuelo', 'id_vuelo', 'id'])
      const clave = String(idVuelo ?? `${page}-${acumulados.length}`)
      if (vistos.has(clave)) continue
      vistos.add(clave)
      acumulados.push(item)
    }

    const { totalPages, currentPage } = extraerMetaPaginacion(respuesta)
    if (totalPages > 0 && (currentPage || page) >= totalPages) break
    if (items.length < pageSize) break
  }

  return acumulados
}

async function buscar() {
  cargando.value = true
  errorGeneral.value = ''

  try {
    const params = {
      id_aeropuerto_origen: form.value.origen || undefined,
      id_aeropuerto_destino: form.value.destino || undefined,
      fecha_salida: form.value.salida || undefined,
    }
    const items = filtrosActivos.value
      ? await cargarVuelosPaginados(params)
      : await cargarVuelosGeneralesPaginados()

    resultados.value = items.map(normalizarVuelo).filter(esVueloVisiblePublico).sort(ordenarVuelos)
    if (!filtrosActivos.value) destacados.value = resultados.value

    vueloExpandido.value = null
  } catch (error) {
    resultados.value = []
    errorGeneral.value =
      error.response?.data?.message || 'No se pudieron cargar los vuelos disponibles en este momento.'
  } finally {
    cargando.value = false
  }
}

async function cargarDestacados() {
  cargandoDestacados.value = true
  try {
    const items = await cargarVuelosGeneralesPaginados()

    destacados.value = items.map(normalizarVuelo).filter(esVueloVisiblePublico).sort(ordenarVuelos)
  } catch {
    destacados.value = []
  } finally {
    cargandoDestacados.value = false
  }
}

function sincronizarQuery() {
  router.replace({
    path: '/search',
    query: {
      origen: form.value.origen || undefined,
      destino: form.value.destino || undefined,
      salida: form.value.salida || undefined,
      regreso: tipoViaje.value === 'IDA_VUELTA' ? form.value.regreso || undefined : undefined,
      pasajeros: form.value.pasajeros || undefined,
      clase: form.value.clase || undefined,
      tipo: tipoViaje.value,
    },
  })
}

function guardarSeleccion(vuelo) {
  reserva.setVuelo({
    ...vuelo,
    pasajeros: Number(form.value.pasajeros),
    clase: form.value.clase,
    tipoViaje: tipoViaje.value,
    fechaRegreso: tipoViaje.value === 'IDA_VUELTA' ? form.value.regreso : null,
  })
}

function toggleDetalles(vueloId) {
  vueloExpandido.value = vueloExpandido.value === vueloId ? null : vueloId
}

function reservar(vuelo) {
  guardarSeleccion(vuelo)
  router.push({ name: 'detalle-vuelo', params: { id: vuelo.idVuelo }, query: route.query })
}

const resumenBusqueda = computed(() => ({
  origen: nombreAeropuerto(form.value.origen) || 'Origen',
  destino: nombreAeropuerto(form.value.destino) || 'Destino',
  fecha: form.value.salida ? fechaLegible(form.value.salida) : 'Fecha',
}))

watch(
  () => route.query,
  (query) => {
    form.value.origen = query.origen || ''
    form.value.destino = query.destino || ''
    form.value.salida = query.salida || ''
    form.value.regreso = query.regreso || ''
    form.value.pasajeros = query.pasajeros || '1'
    form.value.clase = query.clase || 'ECONOMICA'
    tipoViaje.value = query.tipo || 'IDA_VUELTA'
  },
)

onMounted(async () => {
  await catalogos.cargarAeropuertos(true).catch(() => {})
  if (filtrosActivos.value) await buscar()
  else await cargarDestacados()
})
</script>

<template>
  <section class="min-h-[calc(100vh-64px)] bg-background py-10">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
      <div class="rounded-[28px] bg-white p-6 shadow-sm sm:p-8">
        <p class="text-sm font-semibold uppercase tracking-[0.28em] text-gold-dark">Vuelos</p>
        <h1 class="mt-2 text-3xl font-bold text-navy">Vuelos Disponibles</h1>
        <div class="mt-4 flex flex-wrap gap-x-3 gap-y-2 text-sm text-text-muted">
          <span>{{ resumenBusqueda.origen }}</span>
          <span>-</span>
          <span>{{ resumenBusqueda.destino }}</span>
          <span>-</span>
          <span>{{ resumenBusqueda.fecha }}</span>
        </div>

        <form
          class="mt-8 grid gap-4 xl:grid-cols-[minmax(0,1.2fr)_minmax(0,1.2fr)_220px_160px]"
          @submit.prevent="sincronizarQuery(); buscar()"
        >
          <select
            v-model="form.origen"
            class="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-text-main outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
          >
            <option value="">Origen</option>
            <option v-for="opcion in opcionesAeropuertos" :key="opcion.valor" :value="opcion.valor">
              {{ opcion.etiqueta }}
            </option>
          </select>

          <select
            v-model="form.destino"
            class="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-text-main outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
          >
            <option value="">Destino</option>
            <option v-for="opcion in opcionesAeropuertos" :key="opcion.valor" :value="opcion.valor">
              {{ opcion.etiqueta }}
            </option>
          </select>

          <input
            v-model="form.salida"
            type="date"
            class="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-text-main outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
          />

          <button
            type="submit"
            class="w-full rounded-2xl bg-gold px-6 py-3 font-semibold text-navy transition-colors hover:bg-gold-light"
          >
            Buscar
          </button>
        </form>
      </div>

      <div class="mt-10">
        <div v-if="cargando" class="rounded-[28px] bg-white p-10 text-center shadow-sm">
          <div class="mx-auto h-9 w-9 animate-spin rounded-full border-4 border-blue-accent/20 border-t-blue-accent" />
          <p class="mt-4 text-text-muted">Consultando vuelos disponibles...</p>
        </div>

        <div v-else-if="errorGeneral" class="rounded-[28px] border border-red-200 bg-red-50 p-6 text-red-700 shadow-sm">
          {{ errorGeneral }}
        </div>

        <div v-else-if="!filtrosActivos && cargandoDestacados" class="rounded-[28px] bg-white p-10 text-center shadow-sm">
          <div class="mx-auto h-9 w-9 animate-spin rounded-full border-4 border-blue-accent/20 border-t-blue-accent" />
          <p class="mt-4 text-text-muted">Cargando vuelos programados...</p>
        </div>

        <div v-else-if="!listaVisible.length" class="rounded-[28px] bg-white p-10 text-center shadow-sm">
          <p class="text-lg font-semibold text-navy">
            {{ filtrosActivos ? 'No encontramos vuelos programados para esa busqueda.' : 'Todavia no hay vuelos programados para mostrar.' }}
          </p>
          <p class="mt-2 text-text-muted">
            {{ filtrosActivos ? 'Prueba con otra fecha o una combinacion distinta de aeropuertos.' : 'Cuando existan vuelos en estado PROGRAMADO apareceran aqui.' }}
          </p>
        </div>

        <div v-else class="space-y-5">
          <article
            v-for="vuelo in listaVisible"
            :key="vuelo.idVuelo"
            class="overflow-hidden rounded-[28px] bg-white shadow-sm transition-shadow hover:shadow-md"
          >
            <div class="grid gap-5 px-6 py-5 lg:grid-cols-[240px_1fr_210px] lg:items-center">
              <div class="space-y-6">
                <div class="flex items-start gap-3">
                  <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-navy text-white">
                    <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2 16l20-5-8 8-2.5-5.5L6 11l-4-1 20-5-5 20-3.5-7L2 16z" />
                    </svg>
                  </div>
                  <div class="min-w-0">
                    <p class="text-lg font-semibold leading-tight text-navy">MPAS Airways</p>
                    <p class="mt-1 text-sm text-text-muted">{{ vuelo.numeroVuelo }} - {{ vuelo.aeronave }}</p>
                  </div>
                </div>

                <div>
                  <p class="text-[2.15rem] font-light leading-none text-navy">{{ horaLegible(vuelo.fechaHoraSalida) }}</p>
                  <p class="mt-3 text-[1.3rem] font-semibold leading-none text-navy">{{ vuelo.codigoOrigen }}</p>
                  <p class="mt-2 text-sm text-text-muted">{{ vuelo.ciudadOrigen }}</p>
                </div>
              </div>

              <div class="grid items-center gap-4 md:grid-cols-[1fr_120px]">
                <div class="text-center">
                  <p class="text-sm text-text-muted">{{ duracionLegible(vuelo.duracionMin) }}</p>
                  <div class="mx-auto mt-4 flex max-w-[360px] items-center gap-3">
                    <span class="h-px flex-1 bg-slate-300" />
                    <svg class="h-5 w-5 text-navy" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2 16l20-5-8 8-2.5-5.5L6 11l-4-1 20-5-5 20-3.5-7L2 16z" />
                    </svg>
                    <span class="h-px flex-1 bg-slate-300" />
                  </div>
                  <p class="mt-4 text-sm font-medium text-emerald-600">
                    {{ vuelo.escalas === 0 ? 'Directo' : `${vuelo.escalas} escalas` }}
                  </p>
                </div>

                <div class="text-left md:text-right">
                  <p class="text-[2.15rem] font-light leading-none text-navy">{{ horaLegible(vuelo.fechaHoraLlegada) }}</p>
                  <p class="mt-3 text-[1.3rem] font-semibold leading-none text-navy">{{ vuelo.codigoDestino }}</p>
                  <p class="mt-2 text-sm text-text-muted">{{ vuelo.ciudadDestino }}</p>
                </div>
              </div>

              <div class="border-l border-slate-200 pl-6">
                <p class="text-right text-sm text-text-muted">Desde</p>
                <p class="text-right text-[2.85rem] font-light leading-none text-navy">{{ moneda(vuelo.precioBase) }}</p>
                <p class="mt-2 text-right text-sm text-text-muted">por persona</p>
                <button
                  type="button"
                  class="mt-5 w-full rounded-2xl bg-gold px-5 py-3 text-base font-semibold text-navy transition-colors hover:bg-gold-light"
                  @click="reservar(vuelo)"
                >
                  Reservar
                </button>
                <button
                  type="button"
                  class="mt-4 flex w-full items-center justify-center gap-2 text-sm font-semibold text-blue-accent transition-colors hover:text-navy"
                  @click="toggleDetalles(vuelo.idVuelo)"
                >
                  <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <span>Ver detalles</span>
                  <svg
                    class="h-4 w-4 transition-transform"
                    :class="{ 'rotate-180': vueloExpandido === vuelo.idVuelo }"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                  </svg>
                </button>
              </div>
            </div>

            <Transition name="expand">
              <div v-if="vueloExpandido === vuelo.idVuelo" class="border-t border-slate-200 bg-slate-50/90 px-6 py-8 lg:px-8">
                <div class="grid gap-8 md:grid-cols-3">
                  <div>
                    <h3 class="text-2xl font-semibold text-navy">Servicios Incluidos</h3>
                    <ul class="mt-4 space-y-3 text-text-muted">
                      <li class="text-emerald-600">- Equipaje de mano (10kg)</li>
                      <li class="text-amber-600">- Equipaje de bodega (opcional)</li>
                      <li class="text-emerald-600">- Snack y bebida</li>
                    </ul>
                  </div>

                  <div>
                    <h3 class="text-2xl font-semibold text-navy">Informacion Adicional</h3>
                    <div class="mt-4 space-y-3 text-text-muted">
                      <p>Aeronave: {{ vuelo.aeronave }}</p>
                    </div>
                  </div>

                  <div>
                    <h3 class="text-2xl font-semibold text-navy">Politicas</h3>
                    <div class="mt-4 space-y-3 text-text-muted">
                      <p>Check-in: Online o aeropuerto</p>
                      <p>3 horas antes en el aeropuerto</p>
                    </div>
                  </div>
                </div>
              </div>
            </Transition>
          </article>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.expand-enter-active,
.expand-leave-active {
  transition: all 0.22s ease;
}

.expand-enter-from,
.expand-leave-to {
  max-height: 0;
  opacity: 0;
}

.expand-enter-to,
.expand-leave-from {
  max-height: 360px;
  opacity: 1;
}
</style>
