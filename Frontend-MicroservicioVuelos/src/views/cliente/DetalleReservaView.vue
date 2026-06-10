<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getClienteReservaDetalleApi } from '@/api/reservas.api'
import { getClienteReservaFacturaApi } from '@/api/facturas.api'
import { getPasajeroApi } from '@/api/pasajeros.api'
import { getAsientoVueloDetalleApi } from '@/api/vuelos.api'
import { deepValue, extractItems, longDate, money, shortTime } from '@/utils/portalCliente'

const route = useRoute()
const router = useRouter()

const cargando = ref(true)
const error = ref('')
const detalle = ref(null)
const factura = ref(null)
const KEY_RESERVA_DETALLE = 'mpas_reserva_detalle'

const pasajeroPrincipal = computed(() => detalle.value?.pasajeros?.[0] || null)

function primero(...valores) {
  return valores.find((valor) => valor !== undefined && valor !== null && String(valor).trim() !== '')
}

function extraerPrimerObjeto(payload) {
  const data = payload?.data?.data ?? payload?.data ?? payload ?? {}
  const items = extractItems(data)
  return items[0] ?? data
}

function duracionLegible(minutos) {
  const total = Number(minutos || 0)
  const horas = Math.floor(total / 60)
  const mins = total % 60
  if (!horas) return `${mins}m`
  if (!mins) return `${horas}h`
  return `${horas}h ${mins}m`
}

function normalizarDetalle(payload) {
  const data = payload?.data?.data ?? payload?.data ?? payload ?? {}
  const reserva = deepValue(data, ['reserva']) || data
  const vuelo = deepValue(data, ['vuelo']) || deepValue(reserva, ['vuelo']) || {}
  const detalles = deepValue(data, ['detalles', 'detalleReserva', 'detallesReserva']) || deepValue(reserva, ['detalles']) || []
  const pasajeros = extractItems(deepValue(data, ['pasajeros']) || data.pasajeros || detalles || [])
  const idVuelo = deepValue(reserva, ['idVuelo', 'id_vuelo']) || deepValue(vuelo, ['idVuelo', 'id_vuelo'])

  return {
    codigoReserva: primero(deepValue(reserva, ['codigoReserva', 'codigo_reserva']), deepValue(data, ['codigoReserva', 'codigo_reserva'])) || '',
    numeroVuelo:
      primero(
        deepValue(reserva, ['numeroVuelo', 'numero_vuelo']),
        deepValue(vuelo, ['numeroVuelo', 'numero_vuelo']),
        idVuelo ? `Vuelo ${idVuelo}` : '',
      ) || '',
    fechaInicio:
      primero(
        deepValue(reserva, ['fechaInicio', 'fecha_inicio']),
        deepValue(vuelo, ['fechaInicio', 'fechaHoraSalida', 'fecha_hora_salida']),
        deepValue(reserva, ['fechaReservaUtc', 'fecha_reserva_utc']),
      ) || '',
    fechaFin: primero(deepValue(reserva, ['fechaFin', 'fecha_fin']), deepValue(vuelo, ['fechaFin', 'fechaHoraLlegada', 'fecha_hora_llegada'])) || '',
    duracionMin: deepValue(reserva, ['duracionMin', 'duracion_min']) || deepValue(vuelo, ['duracionMin', 'duracion_min']) || 0,
    codigoOrigen: primero(deepValue(reserva, ['codigoOrigen', 'codigo_origen']), deepValue(vuelo, ['codigoOrigen', 'codigo_origen'])) || '',
    codigoDestino: primero(deepValue(reserva, ['codigoDestino', 'codigo_destino']), deepValue(vuelo, ['codigoDestino', 'codigo_destino'])) || '',
    ciudadOrigen: primero(deepValue(reserva, ['ciudadOrigen', 'ciudad_origen']), deepValue(vuelo, ['ciudadOrigen', 'ciudad_origen'])) || '',
    ciudadDestino: primero(deepValue(reserva, ['ciudadDestino', 'ciudad_destino']), deepValue(vuelo, ['ciudadDestino', 'ciudad_destino'])) || '',
    pasajeros: pasajeros.map((item, index) => ({
      idPasajero: deepValue(item, ['idPasajero', 'id_pasajero']) || null,
      idAsiento: deepValue(item, ['idAsiento', 'id_asiento']) || null,
      nombre:
        [
          deepValue(item, ['nombrePasajero', 'nombre_pasajero', 'nombres', 'nombre']),
          deepValue(item, ['apellidoPasajero', 'apellido_pasajero', 'apellidos', 'apellido']),
        ].filter(Boolean).join(' ').trim() ||
        (deepValue(item, ['idPasajero', 'id_pasajero']) ? `Pasajero ${deepValue(item, ['idPasajero', 'id_pasajero'])}` : `Pasajero ${index + 1}`),
      asiento:
        primero(
          deepValue(item, ['numeroAsiento', 'numero_asiento', 'asiento', 'codigoAsiento', 'codigo_asiento']),
          deepValue(item, ['idAsiento', 'id_asiento']) ? `Asiento ${deepValue(item, ['idAsiento', 'id_asiento'])}` : '',
        ) || '',
      documento: deepValue(item, ['numeroDocumentoPasajero', 'numero_documento_pasajero', 'documento', 'numeroDocumento', 'numero_documento']) || '',
    })),
  }
}

async function enriquecerPasajeros(detalleNormalizado) {
  const idVuelo = Number(String(detalleNormalizado.numeroVuelo || '').replace(/\D/g, '')) || null

  const pasajeros = await Promise.all(
    detalleNormalizado.pasajeros.map(async (pasajero) => {
      let pasajeroApi = null
      let asientoApi = null

      if (pasajero.idPasajero) {
        try {
          const respuesta = await getPasajeroApi(pasajero.idPasajero, { skipAuthRedirect: true })
          pasajeroApi = respuesta?.data?.data ?? respuesta?.data ?? null
        } catch {
          pasajeroApi = null
        }
      }

      if (idVuelo && pasajero.idAsiento) {
        try {
          const respuesta = await getAsientoVueloDetalleApi(idVuelo, pasajero.idAsiento, { skipAuthRedirect: true })
          asientoApi = respuesta?.data?.data ?? respuesta?.data ?? null
        } catch {
          asientoApi = null
        }
      }

      const nombreReal = [
        deepValue(pasajeroApi, ['nombrePasajero', 'nombre_pasajero', 'nombres', 'nombre']),
        deepValue(pasajeroApi, ['apellidoPasajero', 'apellido_pasajero', 'apellidos', 'apellido']),
      ].filter(Boolean).join(' ').trim()

      return {
        ...pasajero,
        nombre: nombreReal || pasajero.nombre,
        asiento:
          deepValue(asientoApi, ['numeroAsiento', 'numero_asiento', 'codigoAsiento', 'codigo_asiento', 'asiento']) ||
          pasajero.asiento,
        documento:
          deepValue(pasajeroApi, ['numeroDocumentoPasajero', 'numero_documento_pasajero', 'numeroIdentificacion', 'numero_identificacion', 'documento']) ||
          pasajero.documento,
      }
    }),
  )

  return {
    ...detalleNormalizado,
    pasajeros,
  }
}

function normalizarFactura(payload) {
  const data = payload?.data?.data ?? payload?.data ?? payload ?? {}
  return {
    subtotal: Number(deepValue(data, ['subtotal', 'subtotalReserva', 'subtotal_reserva']) || 0),
    iva: Number(deepValue(data, ['valorIva', 'valor_iva', 'iva']) || 0),
    total: Number(deepValue(data, ['total', 'totalReserva', 'total_reserva']) || 0),
    cargoServicio: Number(deepValue(data, ['cargoServicio', 'cargo_servicio']) || 0),
  }
}

async function cargarDetalle() {
  cargando.value = true
  error.value = ''

  try {
    const idReserva = route.params.id
    const [detalleResp, facturaResp] = await Promise.allSettled([
      getClienteReservaDetalleApi(idReserva),
      getClienteReservaFacturaApi(idReserva),
    ])

    if (detalleResp.status !== 'fulfilled') throw detalleResp.reason
    let detalleNormalizado = normalizarDetalle(detalleResp.value)
    try {
      const local = JSON.parse(sessionStorage.getItem(KEY_RESERVA_DETALLE) || 'null')
      if (local && String(local.idReserva || '') === String(idReserva)) {
        const localNormalizado = normalizarDetalle(local)
        detalleNormalizado = {
          ...detalleNormalizado,
          numeroVuelo: detalleNormalizado.numeroVuelo || localNormalizado.numeroVuelo,
          fechaInicio: detalleNormalizado.fechaInicio || localNormalizado.fechaInicio,
          fechaFin: detalleNormalizado.fechaFin || localNormalizado.fechaFin,
          duracionMin: detalleNormalizado.duracionMin || localNormalizado.duracionMin,
          codigoOrigen: detalleNormalizado.codigoOrigen || localNormalizado.codigoOrigen,
          codigoDestino: detalleNormalizado.codigoDestino || localNormalizado.codigoDestino,
          ciudadOrigen: detalleNormalizado.ciudadOrigen || localNormalizado.ciudadOrigen,
          ciudadDestino: detalleNormalizado.ciudadDestino || localNormalizado.ciudadDestino,
        }
      }
    } catch {
      // Si no hay reserva local seleccionada, usamos solo la respuesta del Bus.
    }
    detalle.value = await enriquecerPasajeros(detalleNormalizado)

    if (facturaResp.status === 'fulfilled') {
      factura.value = normalizarFactura(extraerPrimerObjeto(facturaResp.value))
    } else {
      factura.value = {
        subtotal: 0,
        iva: 0,
        total: 0,
        cargoServicio: 0,
      }
    }
  } catch (err) {
    error.value = err.response?.data?.message || 'No se pudo cargar el detalle de la reserva.'
  } finally {
    cargando.value = false
  }
}

onMounted(cargarDetalle)
</script>

<template>
  <section class="space-y-6">
    <div class="flex items-center justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold text-navy">Detalle de Reserva</h1>
        <p v-if="detalle?.codigoReserva" class="mt-2 text-text-muted">Código: {{ detalle.codigoReserva }}</p>
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

    <div v-if="cargando" class="rounded-[28px] bg-white p-8 text-text-muted shadow-sm">
      Cargando detalle de la reserva...
    </div>

    <template v-else-if="detalle">
      <section class="overflow-hidden rounded-[30px] bg-white shadow-sm">
        <div class="grid gap-8 bg-navy px-8 py-8 text-white md:grid-cols-[1fr_0.8fr_1fr] md:items-center">
          <div>
            <p class="text-4xl font-light">{{ shortTime(detalle.fechaInicio) }}</p>
            <p class="mt-3 text-2xl font-semibold">{{ detalle.codigoOrigen }}</p>
            <p class="mt-2 text-white/80">{{ detalle.ciudadOrigen }}</p>
          </div>

          <div class="text-center">
            <p class="text-lg text-white/80">{{ duracionLegible(detalle.duracionMin) }}</p>
            <div class="mt-3 flex items-center gap-4">
              <span class="h-px flex-1 bg-white/30" />
              <svg class="h-8 w-8 text-gold-light" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M10.18 9 6 11l-2 4 5.5-1.5L12 18l2-1-1.5-4.5L18 10l-1-2-5 1.5L10 5 8 6l2.18 3Z" />
              </svg>
              <span class="h-px flex-1 bg-white/30" />
            </div>
            <p class="mt-3 text-gold-light">Emitida</p>
          </div>

          <div class="text-right">
            <p class="text-4xl font-light">{{ shortTime(detalle.fechaFin) }}</p>
            <p class="mt-3 text-2xl font-semibold">{{ detalle.codigoDestino }}</p>
            <p class="mt-2 text-white/80">{{ detalle.ciudadDestino }}</p>
          </div>
        </div>

        <div class="grid gap-8 px-8 py-8 md:grid-cols-2">
          <div>
            <h2 class="text-2xl font-semibold text-navy">Información del Vuelo</h2>
            <div class="mt-6 space-y-5 text-text-muted">
              <div>
                <p>Vuelo</p>
                <p class="mt-1 text-xl font-semibold text-navy">{{ detalle.numeroVuelo }}</p>
              </div>
              <div>
                <p>Fecha</p>
                <p class="mt-1 text-xl font-semibold text-navy">{{ longDate(detalle.fechaInicio) }}</p>
              </div>
              <div>
                <p>Duración</p>
                <p class="mt-1 text-xl font-semibold text-navy">{{ duracionLegible(detalle.duracionMin) }}</p>
              </div>
            </div>
          </div>

          <div>
            <h2 class="text-2xl font-semibold text-navy">Detalles del Pasajero</h2>
            <div class="mt-6 space-y-5 text-text-muted">
              <div>
                <p>Nombre</p>
                <p class="mt-1 text-xl font-semibold text-navy">{{ pasajeroPrincipal?.nombre || '-' }}</p>
              </div>
              <div>
                <p>Asiento</p>
                <p class="mt-1 text-xl font-semibold text-navy">{{ pasajeroPrincipal?.asiento || '-' }}</p>
              </div>
              <div>
                <p>Documento</p>
                <p class="mt-1 text-xl font-semibold text-navy">{{ pasajeroPrincipal?.documento || '-' }}</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section class="rounded-[30px] bg-white p-8 shadow-sm">
        <h2 class="text-2xl font-semibold text-navy">Resumen de Pago</h2>
        <div class="mt-6 space-y-4 text-lg text-text-muted">
          <div class="flex items-center justify-between">
            <span>Tarifa base</span>
            <span>{{ money(factura?.subtotal || 0) }}</span>
          </div>
          <div class="flex items-center justify-between">
            <span>Tasas e impuestos</span>
            <span>{{ money(factura?.iva || 0) }}</span>
          </div>
          <div class="flex items-center justify-between">
            <span>Cargo por servicio</span>
            <span>{{ money(factura?.cargoServicio || 0) }}</span>
          </div>
          <div class="flex items-center justify-between border-t border-slate-200 pt-4 text-2xl font-semibold text-navy">
            <span>Total pagado</span>
            <span>{{ money(factura?.total || 0) }}</span>
          </div>
        </div>
      </section>
    </template>
  </section>
</template>
