<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import { getClientesApi } from '@/api/clientes.api'
import { anularFacturaApi, getFacturaApi, getFacturasApi } from '@/api/facturas.api'
import { getReservasApi } from '@/api/reservas.api'
import { deepValue, extractItems } from '@/utils/portalCliente'
import { useAutenticacionStore } from '@/stores/autenticacion.store'
import { useRoute } from 'vue-router'

const auth = useAutenticacionStore()
const route = useRoute()

const cargando = ref(true)
const cargandoDetalle = ref(false)
const procesando = ref(false)
const error = ref('')
const errorModal = ref('')
const busqueda = ref('')
const estadoFiltro = ref('')
const fechaFiltro = ref('')
const facturas = ref([])
const clientes = ref([])
const reservas = ref([])
const modalDetalle = ref(false)
const modalAnular = ref(false)
const facturaActiva = ref(null)
const facturaDetalle = ref(null)

const opcionesEstado = [
  { valor: '', etiqueta: 'Todos los estados' },
  { valor: 'ABI', etiqueta: 'Abierta' },
  { valor: 'APR', etiqueta: 'Aprobada' },
  { valor: 'INA', etiqueta: 'Anulada' },
]

const puedeAnular = computed(() => auth.rol === 'ADMINISTRADOR')

const facturasFiltradas = computed(() => {
  const termino = busqueda.value.trim().toLowerCase()
  const estado = estadoFiltro.value.trim().toUpperCase()
  const fecha = fechaFiltro.value.trim()

  return facturas.value
    .slice()
    .sort((a, b) => new Date(b.fechaFactura || 0).getTime() - new Date(a.fechaFactura || 0).getTime())
    .filter((factura) => {
      const matchBusqueda =
        !termino ||
        [factura.numeroFactura, factura.clienteDisplay, factura.reservaDisplay]
          .filter(Boolean)
          .some((valor) => String(valor).toLowerCase().includes(termino))

      const matchEstado = !estado || String(factura.estado || '').toUpperCase() === estado
      const matchFecha = !fecha || String(factura.fechaFactura || '').slice(0, 10) === fecha
      return matchBusqueda && matchEstado && matchFecha
    })
})

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

function estadoAmigable(estado) {
  const valor = String(estado || '').trim().toUpperCase()
  if (valor === 'ABI') return 'Abierta'
  if (valor === 'APR') return 'Aprobada'
  if (valor === 'INA') return 'Anulada'
  return valor || '-'
}

function badgeEstado(estado) {
  const valor = String(estado || '').trim().toUpperCase()
  if (valor === 'APR') return 'bg-emerald-100 text-emerald-700'
  if (valor === 'INA') return 'bg-red-100 text-red-700'
  return 'bg-slate-100 text-slate-700'
}

function normalizarCliente(item) {
  const nombres = deepValue(item, ['nombres']) || ''
  const apellidos = deepValue(item, ['apellidos']) || ''
  const razonSocial = deepValue(item, ['razonSocial', 'razon_social']) || ''
  return {
    idCliente: deepValue(item, ['idCliente', 'id_cliente', 'id']) || null,
    nombreCompleto: [nombres, apellidos].filter(Boolean).join(' ').trim() || razonSocial,
    correo: deepValue(item, ['correo']) || '',
  }
}

function normalizarReserva(item) {
  return {
    idReserva: deepValue(item, ['idReserva', 'id_reserva', 'id']) || null,
    codigoReserva: deepValue(item, ['codigoReserva', 'codigo_reserva']) || '',
  }
}

function normalizarFactura(item) {
  const idCliente = deepValue(item, ['idCliente', 'id_cliente']) || null
  const idReserva = deepValue(item, ['idReserva', 'id_reserva']) || null
  const cliente = clientes.value.find((entry) => String(entry.idCliente) === String(idCliente))
  const reserva = reservas.value.find((entry) => String(entry.idReserva) === String(idReserva))

  const clienteNombre =
    deepValue(item, ['nombreCliente', 'nombre_cliente', 'clienteNombre']) ||
    cliente?.nombreCompleto ||
    ''
  const clienteCorreo = deepValue(item, ['correoCliente', 'correo_cliente']) || cliente?.correo || ''
  const numeroReserva = deepValue(item, ['codigoReserva', 'codigo_reserva']) || reserva?.codigoReserva || ''

  return {
    idFactura: deepValue(item, ['idFactura', 'id_factura', 'id']) || null,
    numeroFactura: deepValue(item, ['numeroFactura', 'numero_factura']) || '',
    idCliente,
    idReserva,
    clienteDisplay: [clienteNombre, clienteCorreo].filter(Boolean).join(' / ') || clienteCorreo || 'Cliente no disponible',
    reservaDisplay: numeroReserva || `Reserva ${idReserva || '-'}`,
    subtotal: Number(deepValue(item, ['subtotal']) || 0),
    valorIva: Number(deepValue(item, ['valorIva', 'valor_iva']) || 0),
    cargoServicio: Number(deepValue(item, ['cargoServicio', 'cargo_servicio']) || 0),
    total: Number(deepValue(item, ['total']) || 0),
    estado: deepValue(item, ['estado']) || 'ABI',
    fechaFactura:
      deepValue(item, ['fechaFactura', 'fecha_factura', 'createdAt', 'created_at', 'fechaEmision', 'fecha_emision']) || '',
    observaciones: deepValue(item, ['observacionesFactura', 'observaciones_factura']) || '',
  }
}

async function cargarTodo() {
  cargando.value = true
  error.value = ''

  try {
    const [respuestaClientes, respuestaReservas, respuestaFacturas] = await Promise.all([
      getClientesApi({ page: 1, page_size: 200 }),
      getReservasApi({ page: 1, page_size: 200 }),
      getFacturasApi({ page: 1, page_size: 200 }),
    ])

    clientes.value = extractItems(respuestaClientes).map(normalizarCliente)
    reservas.value = extractItems(respuestaReservas).map(normalizarReserva)
    facturas.value = extractItems(respuestaFacturas).map(normalizarFactura)
  } catch (err) {
    error.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudieron cargar las facturas.'
  } finally {
    cargando.value = false
  }
}

async function abrirDetalle(factura) {
  facturaActiva.value = factura
  facturaDetalle.value = null
  cargandoDetalle.value = true
  errorModal.value = ''
  modalDetalle.value = true

  try {
    const respuesta = await getFacturaApi(factura.idFactura)
    const data = deepValue(respuesta, ['data']) || respuesta
    facturaDetalle.value = normalizarFactura(data)
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo cargar el detalle de la factura.'
  } finally {
    cargandoDetalle.value = false
  }
}

function abrirAnular(factura) {
  facturaActiva.value = factura
  errorModal.value = ''
  modalAnular.value = true
}

async function anularFactura() {
  if (procesando.value || !facturaActiva.value?.idFactura) return
  procesando.value = true
  errorModal.value = ''

  try {
    await anularFacturaApi(facturaActiva.value.idFactura, {})
    modalAnular.value = false
    await cargarTodo()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo anular la factura.'
  } finally {
    procesando.value = false
  }
}

watch(
  () => route.query.buscar,
  (nuevoValor) => {
    if (typeof nuevoValor === 'string') busqueda.value = nuevoValor
  },
  { immediate: true },
)

onMounted(cargarTodo)
</script>

<template>
  <section class="space-y-5">
    <div class="flex items-start justify-between gap-4">
      <div>
        <h1 class="text-[2rem] font-bold text-navy">Gestion de Facturas</h1>
        <p class="mt-1.5 text-sm text-text-muted">Modulo operativo de consulta y seguimiento de facturas.</p>
      </div>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <section class="grid gap-4 rounded-[24px] bg-white p-4 shadow-sm md:grid-cols-[1fr_220px_220px]">
      <InputApp v-model="busqueda" placeholder="Buscar por numero factura, cliente o reserva..." />
      <SelectApp v-model="estadoFiltro" :opciones="opcionesEstado" />
      <InputApp v-model="fechaFiltro" tipo="date" />
    </section>

    <section class="rounded-[24px] bg-white shadow-sm">
      <div class="overflow-x-auto">
        <table class="min-w-[1320px] w-full border-collapse">
          <thead class="bg-slate-50">
            <tr class="text-left text-sm font-semibold text-navy">
              <th class="px-5 py-3.5">Numero factura</th>
              <th class="px-5 py-3.5">Cliente</th>
              <th class="px-5 py-3.5">Reserva</th>
              <th class="px-5 py-3.5">Subtotal</th>
              <th class="px-5 py-3.5">IVA</th>
              <th class="px-5 py-3.5">Cargo servicio</th>
              <th class="px-5 py-3.5">Total</th>
              <th class="px-5 py-3.5">Estado</th>
              <th class="px-5 py-3.5 text-right">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="cargando">
              <td colspan="9" class="px-5 py-5 text-sm text-text-muted">Cargando facturas...</td>
            </tr>
            <tr v-else-if="!facturasFiltradas.length">
              <td colspan="9" class="px-5 py-5 text-sm text-text-muted">No hay facturas registradas.</td>
            </tr>
            <tr
              v-for="factura in facturasFiltradas"
              v-else
              :key="factura.idFactura"
              class="border-t border-slate-100 align-middle"
            >
              <td class="px-5 py-4 text-sm font-semibold text-navy">
                <div class="min-w-0">
                  <span class="block truncate">{{ factura.numeroFactura || '-' }}</span>
                  <span class="block truncate text-xs text-text-muted">{{ formatearFecha(factura.fechaFactura) }}</span>
                </div>
              </td>
              <td class="px-5 py-4 text-sm text-navy">{{ factura.clienteDisplay }}</td>
              <td class="px-5 py-4 text-sm text-navy">{{ factura.reservaDisplay }}</td>
              <td class="px-5 py-4 text-sm text-navy">{{ money(factura.subtotal) }}</td>
              <td class="px-5 py-4 text-sm text-navy">{{ money(factura.valorIva) }}</td>
              <td class="px-5 py-4 text-sm text-navy">{{ money(factura.cargoServicio) }}</td>
              <td class="px-5 py-4 text-sm text-navy">{{ money(factura.total) }}</td>
              <td class="px-5 py-4">
                <span :class="['rounded-full px-3 py-1 text-xs font-semibold', badgeEstado(factura.estado)]">
                  {{ estadoAmigable(factura.estado) }}
                </span>
              </td>
              <td class="px-5 py-4">
                <div class="flex justify-end gap-2 text-sm">
                  <button type="button" class="rounded-full border border-slate-200 px-3 py-1 text-navy" @click="abrirDetalle(factura)">Ver</button>
                  <button
                    v-if="puedeAnular"
                    type="button"
                    class="rounded-full border border-red-200 px-3 py-1 text-red-600"
                    @click="abrirAnular(factura)"
                  >
                    Anular
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <div v-if="modalDetalle" class="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-950/60 px-4 py-8 backdrop-blur-sm" @click.self="modalDetalle = false">
      <div class="w-full max-w-4xl overflow-hidden rounded-[30px] bg-white shadow-2xl">
        <div class="bg-[linear-gradient(135deg,#12284A_0%,#1B406E_100%)] px-6 py-5 text-white">
          <p class="text-xs font-semibold uppercase tracking-[0.28em] text-white/65">Comprobante emitido</p>
          <h2 class="mt-2 text-[2rem] font-bold">Detalle de Factura</h2>
          <p class="mt-2 max-w-2xl text-sm text-white/80">
            Revisa la emision, el estado y los montos finales de esta factura desde un resumen mas claro.
          </p>
        </div>

        <div class="max-h-[calc(100vh-150px)] overflow-y-auto px-6 py-6">
          <div v-if="errorModal" class="mb-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {{ errorModal }}
          </div>
          <div v-else-if="cargandoDetalle" class="text-sm text-text-muted">Cargando detalle...</div>

          <div v-else-if="facturaDetalle" class="space-y-6">
            <div class="grid gap-3 md:grid-cols-4">
              <div class="rounded-[22px] border border-slate-200 bg-slate-50 px-4 py-4">
                <p class="text-xs font-semibold uppercase tracking-[0.14em] text-text-muted">Factura</p>
                <p class="mt-2 text-xl font-semibold text-navy">{{ facturaDetalle.numeroFactura || '-' }}</p>
              </div>
              <div class="rounded-[22px] border border-slate-200 bg-slate-50 px-4 py-4">
                <p class="text-xs font-semibold uppercase tracking-[0.14em] text-text-muted">Reserva</p>
                <p class="mt-2 text-base font-semibold text-navy">{{ facturaDetalle.reservaDisplay }}</p>
              </div>
              <div class="rounded-[22px] border border-slate-200 bg-slate-50 px-4 py-4">
                <p class="text-xs font-semibold uppercase tracking-[0.14em] text-text-muted">Fecha</p>
                <p class="mt-2 text-base font-semibold text-navy">{{ formatearFecha(facturaDetalle.fechaFactura) }}</p>
              </div>
              <div class="rounded-[22px] border border-emerald-100 bg-emerald-50 px-4 py-4">
                <p class="text-xs font-semibold uppercase tracking-[0.14em] text-emerald-700">Estado</p>
                <p class="mt-2 text-xl font-semibold text-emerald-700">{{ estadoAmigable(facturaDetalle.estado) }}</p>
              </div>
            </div>

            <div class="grid gap-4 md:grid-cols-[1.2fr_0.8fr]">
              <div class="rounded-[24px] border border-slate-200 bg-[linear-gradient(180deg,#ffffff_0%,#f8fbff_100%)] p-5">
                <p class="text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Cliente facturado</p>
                <h3 class="mt-3 text-xl font-semibold text-navy">{{ facturaDetalle.clienteDisplay }}</h3>
                <p class="mt-3 text-sm leading-6 text-text-muted">
                  Documento comercial asociado a la reserva {{ facturaDetalle.reservaDisplay }}.
                </p>
              </div>

              <div class="rounded-[24px] border border-gold/40 bg-[linear-gradient(135deg,#fff8e7_0%,#fffdf7_100%)] p-5">
                <p class="text-xs font-semibold uppercase tracking-[0.18em] text-navy/65">Total facturado</p>
                <p class="mt-3 text-[2rem] font-semibold text-navy">{{ money(facturaDetalle.total) }}</p>
                <p class="mt-3 text-sm text-text-muted">Monto final aprobado para esta emision.</p>
              </div>
            </div>

            <div class="grid gap-4 lg:grid-cols-[1.1fr_0.9fr]">
              <div class="rounded-[24px] border border-slate-200 bg-white p-5">
                <p class="text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Desglose de montos</p>
                <div class="mt-4 grid gap-3 sm:grid-cols-3">
                  <div class="rounded-[20px] border border-slate-200 bg-slate-50 px-4 py-4">
                    <p class="text-xs font-semibold uppercase tracking-[0.12em] text-text-muted">Subtotal</p>
                    <p class="mt-2 text-xl font-semibold text-navy">{{ money(facturaDetalle.subtotal) }}</p>
                  </div>
                  <div class="rounded-[20px] border border-slate-200 bg-slate-50 px-4 py-4">
                    <p class="text-xs font-semibold uppercase tracking-[0.12em] text-text-muted">IVA</p>
                    <p class="mt-2 text-xl font-semibold text-navy">{{ money(facturaDetalle.valorIva) }}</p>
                  </div>
                  <div class="rounded-[20px] border border-slate-200 bg-slate-50 px-4 py-4">
                    <p class="text-xs font-semibold uppercase tracking-[0.12em] text-text-muted">Cargo servicio</p>
                    <p class="mt-2 text-xl font-semibold text-navy">{{ money(facturaDetalle.cargoServicio) }}</p>
                  </div>
                </div>
              </div>

              <div class="rounded-[24px] border border-slate-200 bg-[linear-gradient(180deg,#ffffff_0%,#f8fbff_100%)] p-5">
                <p class="text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Observaciones</p>
                <p class="mt-4 text-sm leading-7 text-text-muted">
                  {{ facturaDetalle.observaciones || 'Sin observaciones registradas para esta factura.' }}
                </p>
              </div>
            </div>
          </div>

          <div class="mt-8 flex justify-end gap-3">
            <button type="button" class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy transition hover:bg-slate-50" @click="modalDetalle = false">Cerrar</button>
          </div>
        </div>
      </div>
    </div>

    <div v-if="modalAnular" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalAnular = false">
      <div class="w-full max-w-xl rounded-[28px] bg-white p-8 shadow-2xl">
        <div v-if="errorModal" class="mb-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{{ errorModal }}</div>
        <div class="flex items-start gap-4">
          <div class="flex h-14 w-14 items-center justify-center rounded-full bg-red-100 text-3xl text-red-500">!</div>
          <div>
            <h2 class="text-3xl font-bold text-navy">Anular factura?</h2>
            <p class="mt-2 text-base text-text-muted">
              Se anulara la factura <strong class="text-navy">{{ facturaActiva?.numeroFactura || '-' }}</strong>.
            </p>
          </div>
        </div>
        <div class="mt-8 flex gap-4">
          <button type="button" class="flex-1 rounded-2xl border border-slate-300 px-5 py-4 font-semibold text-navy hover:bg-slate-50" @click="modalAnular = false">Cancelar</button>
          <button type="button" class="flex-1 rounded-2xl bg-red-500 px-5 py-4 font-semibold text-white hover:bg-red-600 disabled:opacity-60" :disabled="procesando" @click="anularFactura">
            {{ procesando ? 'Procesando...' : 'Anular' }}
          </button>
        </div>
      </div>
    </div>
  </section>
</template>
