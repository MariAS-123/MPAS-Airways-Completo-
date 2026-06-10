<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import {
  cambiarEstadoVueloApi,
  createVueloApi,
  deleteVueloApi,
  getVueloDetalleApi,
  getVuelosApi,
  updateVueloApi,
} from '@/api/vuelos.api'
import { getAeropuertosAdminApi } from '@/api/aeropuertos.api'
import { deepValue, extractItems } from '@/utils/portalCliente'
import { useAutenticacionStore } from '@/stores/autenticacion.store'

const router = useRouter()
const auth = useAutenticacionStore()

const ESTADOS_VUELO = ['PROGRAMADO', 'ABORDANDO', 'EN_VUELO', 'ATERRIZADO', 'CANCELADO', 'DEMORADO']

const cargando = ref(true)
const guardando = ref(false)
const eliminando = ref(false)
const cargandoDetalle = ref(false)
const error = ref('')
const errorModal = ref('')
const busqueda = ref('')
const estadoFiltro = ref('')
const vuelos = ref([])
const aeropuertos = ref([])
const modalAbierto = ref(false)
const modalEliminar = ref(false)
const modalDetalle = ref(false)
const modo = ref('crear')
const vueloActivo = ref(null)
const vueloDetalle = ref(null)
const errores = ref({})
const yaValidado = ref(false)

const form = ref({
  numero_vuelo: '',
  id_aeropuerto_origen: '',
  id_aeropuerto_destino: '',
  fecha_hora_salida: '',
  fecha_hora_llegada: '',
  duracion_min: '',
  precio_base: '',
  estado_vuelo: 'PROGRAMADO',
})

const puedeEliminar = computed(() => auth.rol === 'ADMINISTRADOR')

const opcionesEstado = computed(() => [
  { valor: '', etiqueta: 'Todos los estados' },
  ...ESTADOS_VUELO.map((estado) => ({
    valor: estado,
    etiqueta: capitalizarEstado(estado),
  })),
])

const opcionesEstadoFormulario = computed(() =>
  ESTADOS_VUELO.map((estado) => ({
    valor: estado,
    etiqueta: capitalizarEstado(estado),
  })),
)

const opcionesAeropuertos = computed(() =>
  aeropuertos.value
    .slice()
    .sort((a, b) => String(a.etiquetaCompleta).localeCompare(String(b.etiquetaCompleta), 'es'))
    .map((aeropuerto) => ({
      valor: String(aeropuerto.idAeropuerto),
      etiqueta: aeropuerto.etiquetaCompleta,
    })),
)

const vuelosFiltrados = computed(() => {
  const termino = busqueda.value.trim().toLowerCase()
  const estado = estadoFiltro.value.trim().toUpperCase()

  return vuelos.value
    .slice()
    .sort((a, b) => new Date(a.fechaHoraSalida || 0).getTime() - new Date(b.fechaHoraSalida || 0).getTime())
    .filter((vuelo) => {
      const matchBusqueda =
        !termino ||
        [
          vuelo.numeroVuelo,
          vuelo.origenCodigo,
          vuelo.destinoCodigo,
          vuelo.origenNombre,
          vuelo.destinoNombre,
          vuelo.estadoVuelo,
        ]
          .filter(Boolean)
          .some((valor) => String(valor).toLowerCase().includes(termino))

      const matchEstado = !estado || String(vuelo.estadoVuelo || '').toUpperCase() === estado
      return matchBusqueda && matchEstado
    })
})

const duracionFormulario = computed(() => Number(form.value.duracion_min || 0))

function normalizarNumeroVueloInput(valor) {
  const limpio = String(valor || '').toUpperCase().replace(/[^A-Z0-9]/g, '')
  const sinPrefijo = limpio.startsWith('AV') ? limpio.slice(2) : limpio.replace(/^A/, '')
  const digitos = sinPrefijo.replace(/\D/g, '').slice(0, 5)
  return `AV${digitos}`
}

function normalizarNumeroVueloFinal(valor) {
  const numero = normalizarNumeroVueloInput(valor).replace(/^AV/, '')
  if (!numero) return ''
  return `AV${numero.padStart(5, '0')}`
}

function obtenerSiguienteNumeroVuelo() {
  const maximo = vuelos.value.reduce((actual, vuelo) => {
    const match = String(vuelo.numeroVuelo || '')
      .trim()
      .toUpperCase()
      .match(/^AV(\d{1,5})$/)
    if (!match) return actual
    return Math.max(actual, Number(match[1]))
  }, 0)

  return `AV${String(maximo + 1).padStart(5, '0')}`
}

function capitalizarEstado(valor) {
  return String(valor || '')
    .trim()
    .toLowerCase()
    .replace(/_/g, ' ')
    .replace(/\b\p{L}/gu, (match) => match.toUpperCase())
}

function money(valor) {
  return new Intl.NumberFormat('es-EC', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(Number(valor || 0))
}

function formatoFecha(valor) {
  if (!valor) return '-'
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return '-'
  return new Intl.DateTimeFormat('es-EC', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(fecha)
}

function formatoFechaInput(valor) {
  if (!valor) return ''
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return ''
  const iso = new Date(fecha.getTime() - fecha.getTimezoneOffset() * 60000).toISOString()
  return iso.slice(0, 16)
}

function minutosEntreFechas(inicio, fin) {
  if (!inicio || !fin) return 0
  const fechaInicio = new Date(inicio)
  const fechaFin = new Date(fin)
  if (Number.isNaN(fechaInicio.getTime()) || Number.isNaN(fechaFin.getTime())) return 0
  return Math.round((fechaFin.getTime() - fechaInicio.getTime()) / 60000)
}

function sumarMinutosAFechaInput(inicio, minutos) {
  if (!inicio || !minutos) return ''
  const fechaInicio = new Date(inicio)
  const totalMinutos = Number(minutos)
  if (Number.isNaN(fechaInicio.getTime()) || Number.isNaN(totalMinutos) || totalMinutos <= 0) return ''
  const fechaFinal = new Date(fechaInicio.getTime() + totalMinutos * 60000)
  return formatoFechaInput(fechaFinal.toISOString())
}

function duracionTexto(minutos) {
  const total = Number(minutos || 0)
  if (!total) return '-'
  if (total < 60) return `${total} min`
  const horas = Math.floor(total / 60)
  const resto = total % 60
  return resto ? `${horas}h ${resto}m` : `${horas}h`
}

function normalizarAeropuerto(item) {
  const codigoIata = deepValue(item, ['codigoIata', 'codigo_iata']) || ''
  const nombre = deepValue(item, ['nombre']) || ''
  const nombreCiudad = deepValue(item, ['nombreCiudad', 'nombre_ciudad', 'ciudadNombre']) || ''

  return {
    idAeropuerto: deepValue(item, ['idAeropuerto', 'id_aeropuerto', 'id']) || null,
    codigoIata,
    nombre,
    nombreCiudad,
    etiquetaCompleta: [codigoIata, nombre, nombreCiudad].filter(Boolean).join(' - '),
  }
}

function resolverAeropuerto(idAeropuerto) {
  return aeropuertos.value.find((item) => String(item.idAeropuerto) === String(idAeropuerto))
}

function normalizarVuelo(item) {
  const idOrigen = deepValue(item, ['idAeropuertoOrigen', 'id_aeropuerto_origen']) || null
  const idDestino = deepValue(item, ['idAeropuertoDestino', 'id_aeropuerto_destino']) || null
  const origen = resolverAeropuerto(idOrigen)
  const destino = resolverAeropuerto(idDestino)
  const fechaHoraSalida = deepValue(item, ['fechaHoraSalida', 'fecha_hora_salida']) || ''
  const fechaHoraLlegada = deepValue(item, ['fechaHoraLlegada', 'fecha_hora_llegada']) || ''

  return {
    idVuelo: deepValue(item, ['idVuelo', 'id_vuelo', 'id']) || null,
    numeroVuelo: deepValue(item, ['numeroVuelo', 'numero_vuelo']) || '',
    idAeropuertoOrigen: idOrigen,
    idAeropuertoDestino: idDestino,
    fechaHoraSalida,
    fechaHoraLlegada,
    precioBase: Number(deepValue(item, ['precioBase', 'precio_base']) || 0),
    duracionMin:
      Number(deepValue(item, ['duracionMin', 'duracion_min']) || 0) ||
      minutosEntreFechas(fechaHoraSalida, fechaHoraLlegada),
    estadoVuelo: deepValue(item, ['estadoVuelo', 'estado_vuelo']) || 'PROGRAMADO',
    origenCodigo: origen?.codigoIata || '',
    destinoCodigo: destino?.codigoIata || '',
    origenNombre: origen?.nombreCiudad || origen?.nombre || '',
    destinoNombre: destino?.nombreCiudad || destino?.nombre || '',
  }
}

function limpiarFormulario() {
  form.value = {
    numero_vuelo: '',
    id_aeropuerto_origen: '',
    id_aeropuerto_destino: '',
    fecha_hora_salida: '',
    fecha_hora_llegada: '',
    duracion_min: '',
    precio_base: '',
    estado_vuelo: 'PROGRAMADO',
  }
  errores.value = {}
  errorModal.value = ''
  yaValidado.value = false
}

function abrirCrear() {
  modo.value = 'crear'
  vueloActivo.value = null
  limpiarFormulario()
  form.value.numero_vuelo = obtenerSiguienteNumeroVuelo()
  modalAbierto.value = true
}

function abrirEditar(vuelo) {
  modo.value = 'editar'
  vueloActivo.value = vuelo
  form.value = {
    numero_vuelo: normalizarNumeroVueloFinal(vuelo.numeroVuelo || ''),
    id_aeropuerto_origen: String(vuelo.idAeropuertoOrigen || ''),
    id_aeropuerto_destino: String(vuelo.idAeropuertoDestino || ''),
    fecha_hora_salida: formatoFechaInput(vuelo.fechaHoraSalida),
    fecha_hora_llegada: formatoFechaInput(vuelo.fechaHoraLlegada),
    duracion_min: String(vuelo.duracionMin || minutosEntreFechas(vuelo.fechaHoraSalida, vuelo.fechaHoraLlegada) || ''),
    precio_base: vuelo.precioBase === 0 ? '0' : String(vuelo.precioBase ?? ''),
    estado_vuelo: String(vuelo.estadoVuelo || 'PROGRAMADO'),
  }
  errores.value = {}
  errorModal.value = ''
  yaValidado.value = false
  modalAbierto.value = true
}

function abrirEliminar(vuelo) {
  vueloActivo.value = vuelo
  errorModal.value = ''
  modalEliminar.value = true
}

async function abrirDetalle(vuelo) {
  modalDetalle.value = true
  cargandoDetalle.value = true
  errorModal.value = ''
  vueloDetalle.value = null

  try {
    const respuesta = await getVueloDetalleApi(vuelo.idVuelo)
    const data = deepValue(respuesta, ['data']) || respuesta
    vueloDetalle.value = normalizarVuelo(data)
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo cargar el detalle del vuelo.'
  } finally {
    cargandoDetalle.value = false
  }
}

function validar() {
  const nuevosErrores = {}
  const numeroVuelo = normalizarNumeroVueloFinal(form.value.numero_vuelo)
  const precioBase = Number(form.value.precio_base)
  const duracion = duracionFormulario.value

  if (!numeroVuelo) nuevosErrores.numero_vuelo = 'Ingresa el numero de vuelo.'
  else if (!/^AV\d{5}$/.test(numeroVuelo)) nuevosErrores.numero_vuelo = 'Usa el formato AV#####.'
  if (!form.value.id_aeropuerto_origen) nuevosErrores.id_aeropuerto_origen = 'Selecciona el aeropuerto de origen.'
  if (!form.value.id_aeropuerto_destino) nuevosErrores.id_aeropuerto_destino = 'Selecciona el aeropuerto de destino.'
  if (
    form.value.id_aeropuerto_origen &&
    form.value.id_aeropuerto_destino &&
    String(form.value.id_aeropuerto_origen) === String(form.value.id_aeropuerto_destino)
  ) {
    nuevosErrores.id_aeropuerto_destino = 'El destino debe ser diferente al origen.'
  }
  if (!form.value.fecha_hora_salida) nuevosErrores.fecha_hora_salida = 'Ingresa la fecha y hora de salida.'
  if (!form.value.duracion_min || Number.isNaN(duracion) || duracion <= 0) {
    nuevosErrores.duracion_min = 'Ingresa una duracion valida en minutos.'
  }
  if (!form.value.fecha_hora_llegada) nuevosErrores.fecha_hora_llegada = 'La llegada se calcula desde salida y duracion.'
  if (form.value.precio_base === '' || Number.isNaN(precioBase) || precioBase < 0) {
    nuevosErrores.precio_base = 'Ingresa un precio base valido.'
  }
  if (!form.value.estado_vuelo) nuevosErrores.estado_vuelo = 'Selecciona el estado del vuelo.'

  errores.value = nuevosErrores
  return !Object.keys(nuevosErrores).length
}

async function cargarTodo() {
  cargando.value = true
  error.value = ''

  try {
    const [respuestaAeropuertos, respuestaVuelos] = await Promise.all([
      getAeropuertosAdminApi({ page: 1, page_size: 200 }),
      getVuelosApi({ page: 1, page_size: 200 }),
    ])

    aeropuertos.value = extractItems(respuestaAeropuertos).map(normalizarAeropuerto)
    vuelos.value = extractItems(respuestaVuelos).map(normalizarVuelo)
  } catch (err) {
    error.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudieron cargar los vuelos.'
  } finally {
    cargando.value = false
  }
}

async function guardarVuelo() {
  yaValidado.value = true
  if (guardando.value || !validar()) return
  guardando.value = true
  errorModal.value = ''

  const payload = {
    numeroVuelo: normalizarNumeroVueloFinal(form.value.numero_vuelo),
    idAeropuertoOrigen: Number(form.value.id_aeropuerto_origen),
    idAeropuertoDestino: Number(form.value.id_aeropuerto_destino),
    fechaHoraSalida: new Date(form.value.fecha_hora_salida).toISOString(),
    fechaHoraLlegada: new Date(form.value.fecha_hora_llegada).toISOString(),
    duracionMin: duracionFormulario.value,
    precioBase: Number(form.value.precio_base),
    estadoVuelo: String(form.value.estado_vuelo || '').trim().toUpperCase(),
  }

  try {
    if (modo.value === 'crear') {
      await createVueloApi(payload)
    } else {
      await updateVueloApi(vueloActivo.value.idVuelo, payload)
      if (payload.estadoVuelo !== String(vueloActivo.value.estadoVuelo || '').toUpperCase()) {
        await cambiarEstadoVueloApi(vueloActivo.value.idVuelo, { estadoVuelo: payload.estadoVuelo })
      }
    }
    modalAbierto.value = false
    await cargarTodo()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo guardar el vuelo.'
  } finally {
    guardando.value = false
  }
}

async function confirmarEliminar() {
  if (!vueloActivo.value?.idVuelo || eliminando.value) return
  eliminando.value = true
  errorModal.value = ''

  try {
    await deleteVueloApi(vueloActivo.value.idVuelo)
    modalEliminar.value = false
    await cargarTodo()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo eliminar el vuelo.'
  } finally {
    eliminando.value = false
  }
}

function irAEscalas(vuelo) {
  router.push({ name: 'panel-vuelo-escalas', params: { id: vuelo.idVuelo } })
}

function irAAsientos(vuelo) {
  router.push({ name: 'panel-asientos', query: { vueloId: String(vuelo.idVuelo) } })
}

watch(
  [() => form.value.fecha_hora_salida, () => form.value.duracion_min],
  () => {
    const llegadaCalculada = sumarMinutosAFechaInput(form.value.fecha_hora_salida, form.value.duracion_min)
    form.value.fecha_hora_llegada = llegadaCalculada
    if (yaValidado.value) validar()
  },
)

watch(
  form,
  () => {
    form.value.numero_vuelo =
      modo.value === 'crear'
        ? obtenerSiguienteNumeroVuelo()
        : normalizarNumeroVueloInput(form.value.numero_vuelo)
    if (yaValidado.value) validar()
  },
  { deep: true },
)

onMounted(cargarTodo)
</script>

<template>
  <section class="space-y-5">
    <div class="flex items-start justify-between gap-4">
      <div>
        <h1 class="text-[2rem] font-bold text-navy">Gestion de Vuelos</h1>
        <p class="mt-1.5 text-sm text-text-muted">
          Administra vuelos y entra a escalas o mapa de asientos por cada ruta programada.
        </p>
      </div>

      <button
        type="button"
        class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy transition-colors hover:bg-gold-light"
        @click="abrirCrear"
      >
        + Nuevo Vuelo
      </button>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <section class="grid gap-4 rounded-[24px] bg-white p-4 shadow-sm md:grid-cols-[1fr_220px]">
      <InputApp
        v-model="busqueda"
        placeholder="Buscar por numero de vuelo, origen, destino o estado..."
      />
      <SelectApp v-model="estadoFiltro" :opciones="opcionesEstado" />
    </section>

    <section class="rounded-[24px] bg-white shadow-sm">
      <div class="overflow-x-auto">
        <div class="min-w-[1180px]">
          <div class="grid grid-cols-[1fr_1.5fr_1.5fr_1.2fr_1.2fr_0.9fr_0.9fr_260px] gap-3 bg-slate-50 px-5 py-3.5 text-sm font-semibold text-navy">
            <span>Vuelo</span>
            <span>Origen</span>
            <span>Destino</span>
            <span>Salida</span>
            <span>Llegada</span>
            <span>Precio</span>
            <span>Estado</span>
            <span class="text-right">Acciones</span>
          </div>

          <div v-if="cargando" class="px-5 py-5 text-sm text-text-muted">Cargando vuelos...</div>
          <div v-else-if="!vuelosFiltrados.length" class="px-5 py-5 text-sm text-text-muted">
            No hay vuelos registrados.
          </div>

          <div v-else class="divide-y divide-slate-100">
            <div
              v-for="vuelo in vuelosFiltrados"
              :key="vuelo.idVuelo"
              class="grid grid-cols-[1fr_1.5fr_1.5fr_1.2fr_1.2fr_0.9fr_0.9fr_260px] items-center gap-3 px-5 py-4"
            >
              <div class="min-w-0">
                <span class="block truncate text-sm font-semibold text-navy">{{ vuelo.numeroVuelo || '-' }}</span>
                <span class="text-xs text-text-muted">{{ duracionTexto(vuelo.duracionMin) }}</span>
              </div>
              <div class="min-w-0">
                <span class="block truncate text-sm text-navy">{{ vuelo.origenCodigo || '-' }}</span>
                <span class="block truncate text-xs text-text-muted">{{ vuelo.origenNombre || '-' }}</span>
              </div>
              <div class="min-w-0">
                <span class="block truncate text-sm text-navy">{{ vuelo.destinoCodigo || '-' }}</span>
                <span class="block truncate text-xs text-text-muted">{{ vuelo.destinoNombre || '-' }}</span>
              </div>
              <span class="truncate text-sm text-navy">{{ formatoFecha(vuelo.fechaHoraSalida) }}</span>
              <span class="truncate text-sm text-navy">{{ formatoFecha(vuelo.fechaHoraLlegada) }}</span>
              <span class="truncate text-sm text-navy">{{ money(vuelo.precioBase) }}</span>
              <span class="truncate text-sm text-navy">{{ capitalizarEstado(vuelo.estadoVuelo) }}</span>

              <div class="flex justify-end gap-2 text-sm">
                <button
                  type="button"
                  class="rounded-full border border-slate-200 px-3 py-1 font-medium text-navy hover:bg-slate-50"
                  @click="irAAsientos(vuelo)"
                >
                  Mapa
                </button>
                <button
                  type="button"
                  class="rounded-full border border-slate-200 px-3 py-1 font-medium text-navy hover:bg-slate-50"
                  @click="irAEscalas(vuelo)"
                >
                  Escalas
                </button>
                <button type="button" class="text-slate-500 hover:text-navy" title="Ver" @click="abrirDetalle(vuelo)">👁</button>
                <button type="button" class="text-slate-500 hover:text-navy" title="Editar" @click="abrirEditar(vuelo)">✎</button>
                <button
                  v-if="puedeEliminar"
                  type="button"
                  class="text-red-500 hover:text-red-600"
                  title="Eliminar"
                  @click="abrirEliminar(vuelo)"
                >
                  🗑
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <div
      v-if="modalDetalle"
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4"
      @click.self="modalDetalle = false"
    >
      <div class="w-full max-w-4xl rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">Detalle del Vuelo</h2>

        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorModal }}
        </div>

        <div v-else-if="cargandoDetalle" class="mt-6 text-sm text-text-muted">Cargando detalle...</div>

        <div v-else-if="vueloDetalle" class="mt-6 grid gap-5 md:grid-cols-2">
          <InputApp :model-value="vueloDetalle.numeroVuelo || '-'" label="Numero de vuelo" disabled />
          <InputApp :model-value="capitalizarEstado(vueloDetalle.estadoVuelo)" label="Estado" disabled />
          <InputApp :model-value="vueloDetalle.origenCodigo || '-'" label="Codigo origen" disabled />
          <InputApp :model-value="vueloDetalle.destinoCodigo || '-'" label="Codigo destino" disabled />
          <InputApp :model-value="vueloDetalle.origenNombre || '-'" label="Origen" disabled />
          <InputApp :model-value="vueloDetalle.destinoNombre || '-'" label="Destino" disabled />
          <InputApp :model-value="formatoFecha(vueloDetalle.fechaHoraSalida)" label="Salida" disabled />
          <InputApp :model-value="formatoFecha(vueloDetalle.fechaHoraLlegada)" label="Llegada" disabled />
          <InputApp :model-value="duracionTexto(vueloDetalle.duracionMin)" label="Duracion" disabled />
          <InputApp :model-value="money(vueloDetalle.precioBase)" label="Precio base" disabled />
        </div>

        <div class="mt-8 flex justify-end gap-3">
          <button
            type="button"
            class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50"
            @click="modalDetalle = false"
          >
            Cerrar
          </button>
          <button
            v-if="vueloDetalle"
            type="button"
            class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy hover:bg-gold-light"
            @click="() => { modalDetalle = false; abrirEditar(vueloDetalle) }"
          >
            Editar
          </button>
        </div>
      </div>
    </div>

    <div
      v-if="modalAbierto"
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4"
      @click.self="modalAbierto = false"
    >
      <div class="max-h-[90vh] w-full max-w-5xl overflow-y-auto rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">{{ modo === 'crear' ? 'Nuevo Vuelo' : 'Editar Vuelo' }}</h2>

        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorModal }}
        </div>

        <div class="mt-6 grid gap-5 md:grid-cols-2">
          <InputApp
            v-model="form.numero_vuelo"
            label="Numero de vuelo"
            placeholder="AV01005"
            :disabled="modo === 'crear'"
            :error="errores.numero_vuelo"
            requerido
          />
          <SelectApp
            v-model="form.estado_vuelo"
            label="Estado"
            :opciones="opcionesEstadoFormulario"
            :error="errores.estado_vuelo"
            requerido
          />
          <SelectApp
            v-model="form.id_aeropuerto_origen"
            label="Aeropuerto de origen"
            :opciones="opcionesAeropuertos"
            :error="errores.id_aeropuerto_origen"
            requerido
          />
          <SelectApp
            v-model="form.id_aeropuerto_destino"
            label="Aeropuerto de destino"
            :opciones="opcionesAeropuertos"
            :error="errores.id_aeropuerto_destino"
            requerido
          />
          <InputApp
            v-model="form.fecha_hora_salida"
            tipo="datetime-local"
            label="Fecha hora salida"
            :error="errores.fecha_hora_salida"
            requerido
          />
          <InputApp
            :model-value="form.fecha_hora_llegada"
            tipo="datetime-local"
            label="Fecha hora llegada"
            :error="errores.fecha_hora_llegada"
            disabled
          />
          <InputApp
            v-model="form.duracion_min"
            tipo="number"
            label="Duracion (min)"
            :error="errores.duracion_min"
            requerido
          />
          <InputApp
            v-model="form.precio_base"
            tipo="number"
            label="Precio base"
            :error="errores.precio_base"
            requerido
          />
          <InputApp :model-value="duracionFormulario > 0 ? duracionTexto(duracionFormulario) : 'Ingresa la duracion'" label="Resumen duracion" disabled />
        </div>

        <div class="mt-8 flex justify-end gap-3">
          <button
            type="button"
            class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50"
            @click="modalAbierto = false"
          >
            Cancelar
          </button>
          <button
            type="button"
            class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy hover:bg-gold-light disabled:opacity-60"
            :disabled="guardando || (yaValidado && Object.keys(errores).length > 0)"
            @click="guardarVuelo"
          >
            {{ guardando ? 'Guardando...' : 'Guardar' }}
          </button>
        </div>
      </div>
    </div>

    <div
      v-if="modalEliminar"
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4"
      @click.self="modalEliminar = false"
    >
      <div class="w-full max-w-xl rounded-[28px] bg-white p-8 shadow-2xl">
        <div v-if="errorModal" class="mb-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorModal }}
        </div>

        <div class="flex items-start gap-4">
          <div class="flex h-14 w-14 items-center justify-center rounded-full bg-red-100 text-3xl text-red-500">!</div>
          <div>
            <h2 class="text-3xl font-bold text-navy">Eliminar vuelo?</h2>
            <p class="mt-2 text-base text-text-muted">
              Se eliminara el vuelo <strong class="text-navy">{{ vueloActivo?.numeroVuelo || '-' }}</strong>.
            </p>
          </div>
        </div>

        <div class="mt-8 flex gap-4">
          <button
            type="button"
            class="flex-1 rounded-2xl border border-slate-300 px-5 py-4 font-semibold text-navy hover:bg-slate-50"
            @click="modalEliminar = false"
          >
            Cancelar
          </button>
          <button
            type="button"
            class="flex-1 rounded-2xl bg-red-500 px-5 py-4 font-semibold text-white hover:bg-red-600 disabled:opacity-60"
            :disabled="eliminando"
            @click="confirmarEliminar"
          >
            {{ eliminando ? 'Eliminando...' : 'Eliminar' }}
          </button>
        </div>
      </div>
    </div>
  </section>
</template>
