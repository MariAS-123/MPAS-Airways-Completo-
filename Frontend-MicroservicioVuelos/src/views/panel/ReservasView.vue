<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import { getClientesApi } from '@/api/clientes.api'
import { getPasajerosApi } from '@/api/pasajeros.api'
import {
  cambiarEstadoReservaApi,
  createReservaApi,
  deleteReservaApi,
  getReservaApi,
  getReservasApi,
  pagarReservaApi,
} from '@/api/reservas.api'
import { getAsientosVueloApi, getVuelosApi } from '@/api/vuelos.api'
import { usePanelPermisos } from '@/composables/usePanelPermisos'
import { deepValue, extractItems } from '@/utils/portalCliente'

const ESTADOS_RESERVA = ['PEN', 'CON', 'CAN', 'FIN', 'EMI']
const router = useRouter()
const { puedeEliminar } = usePanelPermisos()

const cargando = ref(true)
const guardando = ref(false)
const error = ref('')
const errorModal = ref('')
const busqueda = ref('')
const estadoFiltro = ref('')
const fechaFiltro = ref('')
const reservas = ref([])
const clientes = ref([])
const pasajeros = ref([])
const vuelos = ref([])
const asientosVuelo = ref([])
const modalCrear = ref(false)
const modalEstado = ref(false)
const modalPago = ref(false)
const modalCancelar = ref(false)
const reservaActiva = ref(null)
const errores = ref({})

const formEstado = ref({ estado_reserva: 'CAN', motivo_cancelacion: '' })
const formPago = ref({ cargo_servicio: '0' })
const formCrear = ref({
  id_cliente: '',
  id_vuelo: '',
  pasajeros_ids: [],
  asientos_por_pasajero: {},
})

const opcionesEstado = [
  { valor: '', etiqueta: 'Todos los estados' },
  ...ESTADOS_RESERVA.map((estado) => ({ valor: estado, etiqueta: estadoAmigable(estado) })),
]

const opcionesEstadoCambio = ESTADOS_RESERVA.map((estado) => ({
  valor: estado,
  etiqueta: estadoAmigable(estado),
}))

const opcionesClientes = computed(() =>
  clientes.value
    .slice()
    .sort((a, b) => String(a.nombreCompleto).localeCompare(String(b.nombreCompleto), 'es'))
    .map((cliente) => ({
      valor: String(cliente.idCliente),
      etiqueta: cliente.nombreCompleto || cliente.correo || `Cliente ${cliente.idCliente}`,
    })),
)

const opcionesVuelos = computed(() =>
  vuelos.value
    .slice()
    .sort((a, b) => new Date(a.fechaInicio || 0).getTime() - new Date(b.fechaInicio || 0).getTime())
    .map((vuelo) => ({
      valor: String(vuelo.idVuelo),
      etiqueta: `${vuelo.numeroVuelo} - ${vuelo.ruta} - ${fechaCorta(vuelo.fechaInicio)}`,
    })),
)

const pasajerosDisponibles = computed(() => {
  const idCliente = String(formCrear.value.id_cliente || '')
  const base = idCliente
    ? pasajeros.value.filter((pasajero) => String(pasajero.idCliente || '') === idCliente)
    : pasajeros.value

  return base
    .slice()
    .sort((a, b) => String(a.nombreCompleto).localeCompare(String(b.nombreCompleto), 'es'))
})

const pasajerosSeleccionados = computed(() =>
  pasajerosDisponibles.value.filter((pasajero) =>
    formCrear.value.pasajeros_ids.includes(String(pasajero.idPasajero)),
  ),
)

const reservasFiltradas = computed(() => {
  const termino = busqueda.value.trim().toLowerCase()
  const estado = estadoFiltro.value.trim().toUpperCase()
  const fecha = fechaFiltro.value.trim()

  return reservas.value
    .slice()
    .sort((a, b) => new Date(b.fechaReserva || 0).getTime() - new Date(a.fechaReserva || 0).getTime())
    .filter((item) => {
      const matchBusqueda =
        !termino ||
        [
          item.codigoReserva,
          item.clienteDisplay,
          item.vueloDisplay,
        ]
          .filter(Boolean)
          .some((valor) => String(valor).toLowerCase().includes(termino))

      const matchEstado = !estado || String(item.estadoReserva || '').toUpperCase() === estado
      const matchFecha = !fecha || String(item.fechaReserva || '').slice(0, 10) === fecha
      return matchBusqueda && matchEstado && matchFecha
    })
})

function reservaEsPagable(reserva) {
  const estado = String(reserva?.estadoReserva || '').trim().toUpperCase()
  return ['PEN', 'CON'].includes(estado)
}

function estadoAmigable(estado) {
  const valor = String(estado || '').trim().toUpperCase()
  if (valor === 'PEN') return 'Pendiente'
  if (valor === 'CON') return 'Confirmada'
  if (valor === 'CAN') return 'Cancelada'
  if (valor === 'FIN') return 'Finalizada'
  if (valor === 'EMI') return 'Emitida'
  return valor || '-'
}

function money(valor) {
  return new Intl.NumberFormat('es-EC', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(Number(valor || 0))
}

function formatearFecha(valor) {
  if (!valor) return '-'
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return String(valor)
  return new Intl.DateTimeFormat('es-EC', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(fecha)
}

function fechaCorta(valor) {
  if (!valor) return '-'
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return String(valor).slice(0, 10)
  return new Intl.DateTimeFormat('es-EC', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(fecha)
}

function normalizarCliente(item) {
  const nombres = deepValue(item, ['nombres']) || ''
  const apellidos = deepValue(item, ['apellidos']) || ''
  const razonSocial = deepValue(item, ['razonSocial', 'razon_social']) || ''
  return {
    idCliente: deepValue(item, ['idCliente', 'id_cliente', 'id']) || null,
    nombreCompleto: [nombres, apellidos].filter(Boolean).join(' ').trim() || razonSocial,
    correo: deepValue(item, ['correo']) || '',
    telefono: deepValue(item, ['telefono']) || '',
  }
}

function normalizarPasajero(item) {
  const nombre = deepValue(item, ['nombrePasajero', 'nombre_pasajero']) || ''
  const apellido = deepValue(item, ['apellidoPasajero', 'apellido_pasajero']) || ''
  return {
    idPasajero: deepValue(item, ['idPasajero', 'id_pasajero', 'id']) || null,
    idCliente: deepValue(item, ['idCliente', 'id_cliente']) || null,
    nombreCompleto: [nombre, apellido].filter(Boolean).join(' ').trim() || `Pasajero ${deepValue(item, ['idPasajero', 'id_pasajero', 'id']) || ''}`,
    documento: deepValue(item, ['numeroDocumentoPasajero', 'numero_documento_pasajero']) || '',
  }
}

function normalizarAsiento(item) {
  return {
    idAsiento: deepValue(item, ['idAsiento', 'id_asiento', 'id']) || null,
    numeroAsiento: deepValue(item, ['numeroAsiento', 'numero_asiento']) || '',
    clase: deepValue(item, ['clase']) || 'ECONOMICA',
    posicion: deepValue(item, ['posicion']) || '',
    disponible: Boolean(deepValue(item, ['disponible']) ?? true),
    precioExtra: Number(deepValue(item, ['precioExtra', 'precio_extra']) || 0),
  }
}

function normalizarVuelo(item) {
  return {
    idVuelo: deepValue(item, ['idVuelo', 'id_vuelo', 'id']) || null,
    numeroVuelo: deepValue(item, ['numeroVuelo', 'numero_vuelo']) || '',
    fechaInicio: deepValue(item, ['fechaHoraSalida', 'fecha_hora_salida', 'fechaInicio', 'fecha_inicio']) || '',
    fechaFin: deepValue(item, ['fechaHoraLlegada', 'fecha_hora_llegada', 'fechaFin', 'fecha_fin']) || '',
    precioBase: Number(deepValue(item, ['precioBase', 'precio_base']) || 0),
    ruta: [
      deepValue(item, ['codigoOrigen', 'codigo_origen', 'origenNombre', 'origen']),
      deepValue(item, ['codigoDestino', 'codigo_destino', 'destinoNombre', 'destino']),
    ].filter(Boolean).join(' → '),
  }
}

function normalizarReserva(item) {
  const idCliente = deepValue(item, ['idCliente', 'id_cliente']) || null
  const cliente = clientes.value.find((entry) => String(entry.idCliente) === String(idCliente))
  const idVuelo = deepValue(item, ['idVuelo', 'id_vuelo']) || null
  const vuelo = vuelos.value.find((entry) => String(entry.idVuelo) === String(idVuelo))
  const clienteNombre =
    deepValue(item, ['nombreCliente', 'nombre_cliente', 'clienteNombre']) ||
    cliente?.nombreCompleto ||
    ''
  const clienteCorreo = deepValue(item, ['contactoEmail', 'contacto_email']) || cliente?.correo || ''
  const numeroVuelo = deepValue(item, ['numeroVuelo', 'numero_vuelo']) || vuelo?.numeroVuelo || ''
  const rutaVuelo = vuelo?.ruta || ''

  return {
    idReserva: deepValue(item, ['idReserva', 'id_reserva', 'id']) || null,
    codigoReserva: deepValue(item, ['codigoReserva', 'codigo_reserva']) || '',
    idCliente,
    idVuelo,
    numeroVuelo,
    fechaReserva:
      deepValue(item, ['fechaReserva', 'fecha_reserva', 'createdAt', 'created_at', 'fechaInicio', 'fecha_inicio']) || '',
    fechaInicio: deepValue(item, ['fechaInicio', 'fecha_inicio']) || '',
    fechaFin: deepValue(item, ['fechaFin', 'fecha_fin']) || '',
    totalReserva: Number(deepValue(item, ['totalReserva', 'total_reserva', 'total']) || 0),
    subtotalReserva: Number(deepValue(item, ['subtotalReserva', 'subtotal_reserva']) || 0),
    valorIva: Number(deepValue(item, ['valorIva', 'valor_iva']) || 0),
    estadoReserva: deepValue(item, ['estadoReserva', 'estado_reserva']) || '',
    clienteNombre,
    clienteDisplay: [clienteNombre, clienteCorreo].filter(Boolean).join(' / ') || clienteCorreo || 'Cliente no disponible',
    contactoEmail: clienteCorreo,
    contactoTelefono: deepValue(item, ['contactoTelefono', 'contacto_telefono']) || cliente?.telefono || '',
    vueloDisplay: [numeroVuelo, rutaVuelo].filter(Boolean).join(' / ') || numeroVuelo || 'Vuelo no disponible',
    pasajeros: deepValue(item, ['pasajeros']) || [],
  }
}

async function cargarTodo() {
  cargando.value = true
  error.value = ''
  try {
    const [respuestaClientes, respuestaPasajeros, respuestaVuelos, respuestaReservas] = await Promise.all([
      getClientesApi({ page: 1, page_size: 200 }),
      getPasajerosApi({ page: 1, page_size: 200 }),
      getVuelosApi({ page: 1, page_size: 200 }),
      getReservasApi({ page: 1, page_size: 200 }),
    ])

    clientes.value = extractItems(respuestaClientes).map(normalizarCliente)
    pasajeros.value = extractItems(respuestaPasajeros).map(normalizarPasajero)
    vuelos.value = extractItems(respuestaVuelos).map(normalizarVuelo)
    reservas.value = extractItems(respuestaReservas).map(normalizarReserva)
  } catch (err) {
    error.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudieron cargar las reservas.'
  } finally {
    cargando.value = false
  }
}

function abrirDetalle(reserva) {
  router.push({ name: 'panel-reserva-detalle', params: { id: reserva.idReserva } })
}

function abrirCrear() {
  formCrear.value = {
    id_cliente: '',
    id_vuelo: '',
    pasajeros_ids: [],
    asientos_por_pasajero: {},
  }
  asientosVuelo.value = []
  errores.value = {}
  errorModal.value = ''
  modalCrear.value = true
}

function abrirEstado(reserva) {
  reservaActiva.value = reserva
  formEstado.value = {
    estado_reserva: String(reserva.estadoReserva || 'CAN'),
    motivo_cancelacion: '',
  }
  errorModal.value = ''
  modalEstado.value = true
}

function abrirPago(reserva) {
  if (!reservaEsPagable(reserva)) return
  reservaActiva.value = reserva
  formPago.value = { cargo_servicio: '0' }
  errorModal.value = ''
  modalPago.value = true
}

function abrirCancelar(reserva) {
  reservaActiva.value = reserva
  formEstado.value = {
    estado_reserva: 'CAN',
    motivo_cancelacion: '',
  }
  errorModal.value = ''
  modalCancelar.value = true
}

function togglePasajero(idPasajero) {
  const id = String(idPasajero)
  const actual = formCrear.value.pasajeros_ids.slice()
  const existe = actual.includes(id)
  formCrear.value.pasajeros_ids = existe ? actual.filter((item) => item !== id) : [...actual, id]
  if (existe) {
    const siguiente = { ...formCrear.value.asientos_por_pasajero }
    delete siguiente[id]
    formCrear.value.asientos_por_pasajero = siguiente
  }
}

function obtenerOpcionesAsiento(idPasajero) {
  const asientoActual = String(formCrear.value.asientos_por_pasajero?.[String(idPasajero)] || '')
  const usados = new Set(
    Object.entries(formCrear.value.asientos_por_pasajero || {})
      .filter(([pasajeroId, asientoId]) => pasajeroId !== String(idPasajero) && asientoId)
      .map(([, asientoId]) => String(asientoId)),
  )

  return asientosVuelo.value
    .filter((asiento) => !usados.has(String(asiento.idAsiento)) || String(asiento.idAsiento) === asientoActual)
    .map((asiento) => ({
      valor: String(asiento.idAsiento),
      etiqueta: `${asiento.numeroAsiento} - ${asiento.clase} - ${money(asiento.precioExtra)}`,
    }))
}

function validarCreacion() {
  const nuevosErrores = {}
  if (!formCrear.value.id_cliente) nuevosErrores.id_cliente = 'Selecciona el cliente.'
  if (!formCrear.value.id_vuelo) nuevosErrores.id_vuelo = 'Selecciona el vuelo.'
  if (!formCrear.value.pasajeros_ids.length) nuevosErrores.pasajeros_ids = 'Selecciona al menos un pasajero.'
  for (const pasajeroId of formCrear.value.pasajeros_ids) {
    if (!formCrear.value.asientos_por_pasajero?.[pasajeroId]) {
      nuevosErrores[`asiento_${pasajeroId}`] = 'Selecciona un asiento.'
    }
  }
  errores.value = nuevosErrores
  return !Object.keys(nuevosErrores).length
}

async function cargarAsientosVuelo(idVuelo) {
  if (!idVuelo) {
    asientosVuelo.value = []
    return
  }

  try {
    const respuesta = await getAsientosVueloApi(idVuelo, {
      page: 1,
      page_size: 200,
      disponible: true,
    })
    asientosVuelo.value = extractItems(respuesta).map(normalizarAsiento)
  } catch (err) {
    asientosVuelo.value = []
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudieron cargar los asientos del vuelo.'
  }
}

async function crearReserva() {
  if (guardando.value || !validarCreacion()) return
  guardando.value = true
  errorModal.value = ''
  try {
    const cliente = clientes.value.find((item) => String(item.idCliente) === String(formCrear.value.id_cliente))
    const vuelo = vuelos.value.find((item) => String(item.idVuelo) === String(formCrear.value.id_vuelo))
    const pasajerosPayload = formCrear.value.pasajeros_ids.map((idPasajero) => {
      const idAsiento = Number(formCrear.value.asientos_por_pasajero[idPasajero])
      const asiento = asientosVuelo.value.find((item) => Number(item.idAsiento) === idAsiento)
      const subtotalLinea = Number((Number(vuelo?.precioBase || 0) + Number(asiento?.precioExtra || 0)).toFixed(2))
      const valorIvaLinea = Number((subtotalLinea * 0.15).toFixed(2))
      const totalLinea = Number((subtotalLinea + valorIvaLinea).toFixed(2))

      return {
        id_pasajero: Number(idPasajero),
        id_asiento: idAsiento,
        subtotal_linea: subtotalLinea,
        valor_iva_linea: valorIvaLinea,
        total_linea: totalLinea,
      }
    })

    const subtotalReserva = Number(
      pasajerosPayload.reduce((total, pasajero) => total + Number(pasajero.subtotal_linea || 0), 0).toFixed(2),
    )
    const valorIva = Number(
      pasajerosPayload.reduce((total, pasajero) => total + Number(pasajero.valor_iva_linea || 0), 0).toFixed(2),
    )
    const totalReserva = Number((subtotalReserva + valorIva).toFixed(2))

    const payload = {
      idCliente: Number(formCrear.value.id_cliente),
      idVuelo: Number(formCrear.value.id_vuelo),
      fechaInicio: vuelo?.fechaInicio || null,
      fechaFin: vuelo?.fechaFin || null,
      subtotalReserva,
      valorIva,
      totalReserva,
      origenCanalReserva: 'PRESENCIAL',
      contactoEmail: cliente?.correo || null,
      contactoTelefono: cliente?.telefono || null,
      detalles: pasajerosPayload.map((pasajero) => ({
        idPasajero: pasajero.id_pasajero,
        idAsiento: pasajero.id_asiento,
        subtotalLinea: pasajero.subtotal_linea,
        valorIvaLinea: pasajero.valor_iva_linea,
        totalLinea: pasajero.total_linea,
      })),
    }

    await createReservaApi(payload)
    modalCrear.value = false
    await cargarTodo()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo crear la reserva.'
  } finally {
    guardando.value = false
  }
}

async function cambiarEstado() {
  if (guardando.value || !reservaActiva.value?.idReserva) return
  guardando.value = true
  errorModal.value = ''
  try {
    await cambiarEstadoReservaApi(reservaActiva.value.idReserva, {
      estado_reserva: formEstado.value.estado_reserva,
      motivo_cancelacion: formEstado.value.motivo_cancelacion || undefined,
    })
    modalEstado.value = false
    await cargarTodo()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo cambiar el estado de la reserva.'
  } finally {
    guardando.value = false
  }
}

async function pagarReserva() {
  if (guardando.value || !reservaActiva.value?.idReserva) return
  guardando.value = true
  errorModal.value = ''
  try {
    await pagarReservaApi(reservaActiva.value.idReserva, {
      cargo_servicio: Number(formPago.value.cargo_servicio || 0),
      equipaje: [],
    })
    modalPago.value = false
    await cargarTodo()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo pagar la reserva.'
  } finally {
    guardando.value = false
  }
}

async function cancelarReserva() {
  if (guardando.value || !reservaActiva.value?.idReserva) return
  guardando.value = true
  errorModal.value = ''
  try {
    await deleteReservaApi(reservaActiva.value.idReserva)
    modalCancelar.value = false
    await cargarTodo()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo cancelar la reserva.'
  } finally {
    guardando.value = false
  }
}

watch(
  () => formCrear.value.id_cliente,
  (nuevoCliente) => {
    if (!nuevoCliente) {
      formCrear.value.pasajeros_ids = []
      return
    }
    formCrear.value.pasajeros_ids = formCrear.value.pasajeros_ids.filter((idPasajero) =>
      pasajerosDisponibles.value.some((pasajero) => String(pasajero.idPasajero) === String(idPasajero)),
    )
    const permitidos = new Set(formCrear.value.pasajeros_ids)
    formCrear.value.asientos_por_pasajero = Object.fromEntries(
      Object.entries(formCrear.value.asientos_por_pasajero || {}).filter(([idPasajero]) => permitidos.has(idPasajero)),
    )
  },
)

watch(
  () => formCrear.value.id_vuelo,
  async (nuevoVuelo) => {
    formCrear.value.asientos_por_pasajero = {}
    if (!nuevoVuelo) {
      asientosVuelo.value = []
      return
    }
    await cargarAsientosVuelo(Number(nuevoVuelo))
  },
)

onMounted(cargarTodo)
</script>

<template>
  <section class="space-y-5">
    <div class="flex items-start justify-between gap-4">
      <div>
        <h1 class="text-[2rem] font-bold text-navy">Gestion de Reservas</h1>
      </div>

      <button
        type="button"
        class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy transition-colors hover:bg-gold-light"
        @click="abrirCrear"
      >
        + Nueva Reserva
      </button>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <section class="grid gap-4 rounded-[24px] bg-white p-4 shadow-sm md:grid-cols-[1fr_220px_220px]">
      <InputApp v-model="busqueda" placeholder="Buscar por codigo reserva, cliente o numero vuelo..." />
      <SelectApp v-model="estadoFiltro" :opciones="opcionesEstado" />
      <InputApp v-model="fechaFiltro" tipo="date" />
    </section>

    <section class="rounded-[24px] bg-white shadow-sm">
      <div class="overflow-x-auto">
        <div class="min-w-[1200px]">
          <div class="grid grid-cols-[1fr_1.3fr_1fr_1.1fr_0.9fr_0.9fr_240px] gap-3 bg-slate-50 px-5 py-3.5 text-sm font-semibold text-navy">
            <span>Codigo reserva</span>
            <span>Cliente</span>
            <span>Vuelo</span>
            <span>Fecha reserva</span>
            <span>Total</span>
            <span>Estado</span>
            <span class="text-right">Acciones</span>
          </div>

          <div v-if="cargando" class="px-5 py-5 text-sm text-text-muted">Cargando reservas...</div>
          <div v-else-if="!reservasFiltradas.length" class="px-5 py-5 text-sm text-text-muted">No hay reservas registradas.</div>

          <div v-else class="divide-y divide-slate-100">
            <div
              v-for="reserva in reservasFiltradas"
              :key="reserva.idReserva"
              class="grid grid-cols-[1fr_1.3fr_1fr_1.1fr_0.9fr_0.9fr_240px] items-center gap-3 px-5 py-4"
            >
              <span class="truncate text-sm text-navy">{{ reserva.codigoReserva || '-' }}</span>
              <span class="truncate text-sm text-navy">{{ reserva.clienteDisplay }}</span>
              <span class="truncate text-sm text-navy">{{ reserva.vueloDisplay }}</span>
              <span class="truncate text-sm text-navy">{{ formatearFecha(reserva.fechaReserva) }}</span>
              <span class="truncate text-sm text-navy">{{ money(reserva.totalReserva) }}</span>
              <span class="truncate">
                <span class="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-navy">
                  {{ estadoAmigable(reserva.estadoReserva) }}
                </span>
              </span>
              <div class="flex justify-end gap-2 text-sm">
                <button type="button" class="rounded-full border border-slate-200 px-3 py-1 text-navy" @click="abrirDetalle(reserva)">Ver</button>
                <button
                  type="button"
                  class="rounded-full border px-3 py-1"
                  :class="
                    reservaEsPagable(reserva)
                      ? 'border-slate-200 text-navy'
                      : 'cursor-not-allowed border-slate-100 bg-slate-100 text-slate-400'
                  "
                  :disabled="!reservaEsPagable(reserva)"
                  :title="reservaEsPagable(reserva) ? 'Pagar reserva' : 'Esta reserva ya no admite pago'"
                  @click="abrirPago(reserva)"
                >
                  Pagar
                </button>
                <button type="button" class="rounded-full border border-slate-200 px-3 py-1 text-navy" @click="abrirEstado(reserva)">Cambiar estado</button>
                <button v-if="puedeEliminar" type="button" class="rounded-full border border-red-200 px-3 py-1 text-red-600" @click="abrirCancelar(reserva)">Cancelar</button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <div v-if="modalCrear" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalCrear = false">
      <div class="max-h-[90vh] w-full max-w-5xl overflow-y-auto rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">Nueva Reserva</h2>
        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{{ errorModal }}</div>

        <div class="mt-6 grid gap-5 md:grid-cols-2">
          <SelectApp v-model="formCrear.id_cliente" label="Cliente" :opciones="opcionesClientes" :error="errores.id_cliente" requerido />
          <SelectApp v-model="formCrear.id_vuelo" label="Vuelo" :opciones="opcionesVuelos" :error="errores.id_vuelo" requerido />
        </div>

        <div class="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div class="flex items-center justify-between gap-4">
            <div>
              <h3 class="text-lg font-semibold text-navy">Pasajeros asociados</h3>
              <p class="text-sm text-text-muted">Selecciona pasajeros y asigna un asiento disponible a cada uno.</p>
            </div>
            <span class="text-sm font-medium text-navy">{{ formCrear.pasajeros_ids.length }} seleccionados</span>
          </div>

          <p v-if="errores.pasajeros_ids" class="mt-3 text-sm text-red-600">{{ errores.pasajeros_ids }}</p>

          <div v-if="pasajerosDisponibles.length" class="mt-4 grid gap-3 md:grid-cols-2">
            <label
              v-for="pasajero in pasajerosDisponibles"
              :key="pasajero.idPasajero"
              class="flex items-start gap-3 rounded-2xl border border-slate-200 bg-white px-4 py-3"
            >
              <input
                :checked="formCrear.pasajeros_ids.includes(String(pasajero.idPasajero))"
                type="checkbox"
                class="mt-1 h-4 w-4 rounded accent-navy"
                @change="togglePasajero(pasajero.idPasajero)"
              />
              <div>
                <p class="font-semibold text-navy">{{ pasajero.nombreCompleto }}</p>
                <p class="text-sm text-text-muted">{{ pasajero.documento || 'Sin documento' }}</p>
              </div>
            </label>
          </div>

          <div v-else class="mt-4 text-sm text-text-muted">
            No hay pasajeros disponibles para el cliente seleccionado.
          </div>
        </div>

        <div v-if="formCrear.id_vuelo && pasajerosSeleccionados.length" class="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <div class="flex items-center justify-between gap-4">
            <div>
              <h3 class="text-lg font-semibold text-navy">Asignacion de asientos</h3>
              <p class="text-sm text-text-muted">Cada pasajero debe tener un asiento distinto para guardar la reserva.</p>
            </div>
            <span class="text-sm font-medium text-navy">{{ asientosVuelo.length }} disponibles</span>
          </div>

          <div v-if="asientosVuelo.length" class="mt-4 grid gap-4 md:grid-cols-2">
            <div v-for="pasajero in pasajerosSeleccionados" :key="`asiento-${pasajero.idPasajero}`" class="rounded-2xl border border-slate-200 bg-white p-4">
              <p class="font-semibold text-navy">{{ pasajero.nombreCompleto }}</p>
              <p class="mb-3 text-sm text-text-muted">{{ pasajero.documento || 'Sin documento' }}</p>
              <SelectApp
                :model-value="formCrear.asientos_por_pasajero[String(pasajero.idPasajero)] || ''"
                label="Asiento"
                :opciones="obtenerOpcionesAsiento(pasajero.idPasajero)"
                :error="errores[`asiento_${pasajero.idPasajero}`]"
                requerido
                @update:model-value="
                  (valor) => {
                    formCrear.asientos_por_pasajero = {
                      ...formCrear.asientos_por_pasajero,
                      [String(pasajero.idPasajero)]: valor,
                    }
                  }
                "
              />
            </div>
          </div>

          <div v-else class="mt-4 text-sm text-text-muted">
            No hay asientos disponibles para el vuelo seleccionado.
          </div>
        </div>

        <div class="mt-4 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-text-muted">
          La reserva se crea en pendiente, con asientos asignados y sin equipaje, sin pago, sin factura y sin boletos.
        </div>

        <div class="mt-8 flex justify-end gap-3">
          <button type="button" class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50" @click="modalCrear = false">Cancelar</button>
          <button type="button" class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy hover:bg-gold-light disabled:opacity-60" :disabled="guardando" @click="crearReserva">{{ guardando ? 'Guardando...' : 'Guardar' }}</button>
        </div>
      </div>
    </div>

    <div v-if="modalEstado" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalEstado = false">
      <div class="w-full max-w-xl rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">Cambiar Estado</h2>
        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{{ errorModal }}</div>
        <div class="mt-6 grid gap-5">
          <SelectApp v-model="formEstado.estado_reserva" label="Estado" :opciones="opcionesEstadoCambio" />
          <InputApp v-model="formEstado.motivo_cancelacion" label="Motivo" />
        </div>
        <div class="mt-8 flex justify-end gap-3">
          <button type="button" class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50" @click="modalEstado = false">Cancelar</button>
          <button type="button" class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy hover:bg-gold-light disabled:opacity-60" :disabled="guardando" @click="cambiarEstado">{{ guardando ? 'Guardando...' : 'Guardar' }}</button>
        </div>
      </div>
    </div>

    <div v-if="modalPago" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalPago = false">
      <div class="w-full max-w-4xl rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">Pagar Reserva</h2>
        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{{ errorModal }}</div>
        <div class="mt-6 grid gap-5">
          <InputApp v-model="formPago.cargo_servicio" tipo="number" label="Cargo servicio" />
          <div class="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-text-muted">
            Este pago back office no registra equipaje, asientos, boletos ni factura completa en esta etapa.
          </div>
        </div>
        <div class="mt-8 flex justify-end gap-3">
          <button type="button" class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50" @click="modalPago = false">Cancelar</button>
          <button type="button" class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy hover:bg-gold-light disabled:opacity-60" :disabled="guardando" @click="pagarReserva">{{ guardando ? 'Procesando...' : 'Pagar' }}</button>
        </div>
      </div>
    </div>

    <div v-if="modalCancelar" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalCancelar = false">
      <div class="w-full max-w-xl rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">Cancelar Reserva</h2>
        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{{ errorModal }}</div>
        <div class="mt-6">
          <InputApp v-model="formEstado.motivo_cancelacion" label="Motivo de cancelacion" />
        </div>
        <div class="mt-8 flex justify-end gap-3">
          <button type="button" class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50" @click="modalCancelar = false">Volver</button>
          <button type="button" class="rounded-2xl bg-red-500 px-5 py-3 text-sm font-semibold text-white hover:bg-red-600 disabled:opacity-60" :disabled="guardando" @click="cancelarReserva">{{ guardando ? 'Cancelando...' : 'Cancelar' }}</button>
        </div>
      </div>
    </div>
  </section>
</template>
