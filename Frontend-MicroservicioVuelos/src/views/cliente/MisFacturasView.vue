<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { getClienteFacturasApi, getClienteReservaFacturaApi } from '@/api/facturas.api'
import { getClienteReservasApi } from '@/api/reservas.api'
import { deepValue, extractItems, longDate, money } from '@/utils/portalCliente'

const router = useRouter()
const cargando = ref(true)
const error = ref('')
const facturas = ref([])
const facturaActiva = ref(null)

function normalizarFactura(item) {
  return {
    idFactura: deepValue(item, ['idFactura', 'id_factura', 'id']) || null,
    numero: deepValue(item, ['numeroFactura', 'numero_factura', 'codigoFactura']) || '',
    fecha: deepValue(item, ['fechaEmision', 'fecha_emision', 'fechaFactura', 'fecha_factura']) || '',
    descripcion:
      deepValue(item, ['descripcion', 'descripcionFactura', 'descripcion_factura']) ||
      `${deepValue(item, ['codigoOrigen', 'codigo_origen']) || ''} → ${deepValue(item, ['codigoDestino', 'codigo_destino']) || ''}`.trim() ||
      'Factura de reserva',
    monto: Number(deepValue(item, ['total', 'totalFactura', 'total_factura']) || 0),
    estado: deepValue(item, ['estado', 'estadoFactura', 'estado_factura']) || '',
    raw: item,
  }
}

function colorEstado(estado) {
  if (String(estado).toUpperCase().includes('APR') || String(estado).toUpperCase().includes('PAG')) {
    return 'bg-emerald-100 text-emerald-700'
  }
  return 'bg-amber-100 text-amber-700'
}

function etiquetaEstado(estado) {
  const codigo = String(estado || '').toUpperCase()
  if (codigo.includes('APR') || codigo.includes('PAG')) return 'Aprobada'
  if (codigo.includes('PEN')) return 'Pendiente'
  if (codigo.includes('ANU') || codigo.includes('CAN')) return 'Anulada'
  return estado || 'Aprobada'
}

async function cargarFacturasCliente() {
  try {
    const respuesta = await getClienteFacturasApi()
    const items = extractItems(respuesta)
    if (items.length) return items
  } catch {
    // Si el listado general del portal falla, probamos factura por reserva.
  }

  const reservasRespuesta = await getClienteReservasApi()
  const reservas = extractItems(reservasRespuesta)
  const facturasPorReserva = await Promise.all(
    reservas
      .map((reserva) => deepValue(reserva, ['idReserva', 'id_reserva', 'id']))
      .filter(Boolean)
      .map(async (idReserva) => {
        try {
          const respuesta = await getClienteReservaFacturaApi(idReserva)
          const items = extractItems(respuesta)
          return items.length ? items : [respuesta?.data?.data ?? respuesta?.data].filter(Boolean)
        } catch {
          return []
        }
      }),
  )

  return facturasPorReserva.flat()
}

async function cargarFacturas() {
  cargando.value = true
  error.value = ''

  try {
    const items = await cargarFacturasCliente()
    facturas.value = items.map(normalizarFactura)
  } catch (err) {
    error.value = err.response?.data?.message || 'No se pudieron cargar las facturas.'
    facturas.value = []
  } finally {
    cargando.value = false
  }
}

onMounted(cargarFacturas)
</script>

<template>
  <section class="space-y-6">
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-3xl font-bold text-navy">Mis Facturas</h1>
        <p class="mt-2 text-text-muted">Historial completo de facturas del cliente.</p>
      </div>

      <button
        type="button"
        class="inline-flex items-center gap-2 rounded-2xl border border-slate-300 px-5 py-3 font-semibold text-navy transition-colors hover:bg-slate-50"
        @click="router.push({ name: 'cliente-portal' })"
      >
        <span aria-hidden="true">←</span>
        <span>Volver a Mi Portal de Viajes</span>
      </button>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <section class="overflow-hidden rounded-[30px] bg-white shadow-sm">
      <div v-if="cargando" class="p-8 text-text-muted">Cargando facturas...</div>

      <div v-else-if="!facturas.length" class="p-8 text-text-muted">
        Aún no existen facturas registradas para este cliente.
      </div>

      <template v-else>
        <div class="hidden grid-cols-[1.1fr_1fr_1.7fr_1fr_1fr_110px] gap-4 bg-slate-50 px-8 py-5 text-lg font-semibold text-navy lg:grid">
          <span>Número</span>
          <span>Fecha</span>
          <span>Descripción</span>
          <span>Monto</span>
          <span>Estado</span>
          <span>Acciones</span>
        </div>

        <div class="divide-y divide-slate-100">
          <div
            v-for="factura in facturas"
            :key="factura.idFactura"
            class="grid gap-4 px-8 py-6 lg:grid-cols-[1.1fr_1fr_1.7fr_1fr_1fr_110px] lg:items-center"
          >
            <div class="font-semibold text-navy">{{ factura.numero || `INV-${factura.idFactura}` }}</div>
            <div class="text-text-muted">{{ longDate(factura.fecha) }}</div>
            <div class="text-text-muted">{{ factura.descripcion }}</div>
            <div class="font-semibold text-navy">{{ money(factura.monto) }}</div>
            <div>
              <span class="inline-flex rounded-full px-4 py-1 text-sm font-semibold" :class="colorEstado(factura.estado)">
                {{ etiquetaEstado(factura.estado) }}
              </span>
            </div>
            <div>
              <button
                type="button"
                class="inline-flex h-10 w-10 items-center justify-center rounded-full border border-slate-200 text-navy transition-colors hover:bg-slate-50"
                @click="facturaActiva = factura"
              >
                <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7Z" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </template>
    </section>

    <section v-if="facturaActiva" class="rounded-[30px] bg-white p-8 shadow-sm">
      <div class="flex items-center justify-between gap-4">
        <h2 class="text-2xl font-semibold text-navy">Detalle de Factura</h2>
        <button
          type="button"
          class="rounded-2xl border border-slate-300 px-4 py-2 text-sm font-semibold text-navy transition-colors hover:bg-slate-50"
          @click="facturaActiva = null"
        >
          Cerrar
        </button>
      </div>

      <div class="mt-6 grid gap-4 text-text-muted sm:grid-cols-[180px_1fr]">
        <span>Número</span>
        <span class="font-semibold text-navy">{{ facturaActiva.numero || `INV-${facturaActiva.idFactura}` }}</span>
        <span>Fecha</span>
        <span class="font-semibold text-navy">{{ longDate(facturaActiva.fecha) }}</span>
        <span>Descripción</span>
        <span class="font-semibold text-navy">{{ facturaActiva.descripcion }}</span>
        <span>Estado</span>
        <span class="font-semibold text-navy">{{ etiquetaEstado(facturaActiva.estado) }}</span>
        <span>Monto</span>
        <span class="font-semibold text-navy">{{ money(facturaActiva.monto) }}</span>
      </div>
    </section>
  </section>
</template>
