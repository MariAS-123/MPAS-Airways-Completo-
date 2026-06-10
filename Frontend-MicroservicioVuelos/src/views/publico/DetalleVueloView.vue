<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getEscalasVueloBookingApi, getVueloBookingDetalleApi } from '@/api/vuelos.api'
import { useCatalogosStore } from '@/stores/catalogos.store'
import { useReservaStore } from '@/stores/reserva.store'
import CheckoutStepper from '@/components/CheckoutStepper.vue'

const route = useRoute()
const router = useRouter()
const catalogos = useCatalogosStore()
const reserva = useReservaStore()
const KEY_AEROLINEA_PROGRESO = 'aerolinea_progreso'

const cargando = ref(true)
const errorGeneral = ref('')
const vuelo = ref(null)
const escalas = ref([])

function normalizarLista(data) {
  if (Array.isArray(data)) return data
  if (Array.isArray(data?.items)) return data.items
  return []
}

function obtenerCampo(obj, claves, fallback = null) {
  for (const clave of claves) {
    if (obj?.[clave] !== undefined && obj?.[clave] !== null) return obj[clave]
  }
  return fallback
}

function limpiarNombreCatalogo(nombre = '') {
  return String(nombre).replace(/\d{6,}$/u, '').trim()
}

function moneda(valor) {
  return new Intl.NumberFormat('es-EC', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(Number(valor || 0))
}

function fechaLegible(valor) {
  if (!valor) return 'Fecha por confirmar'
  return new Intl.DateTimeFormat('es-EC', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(new Date(valor))
}

function horaLegible(valor) {
  if (!valor) return '--:--'
  return new Intl.DateTimeFormat('es-EC', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(new Date(valor))
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
  return catalogos.aeropuertos.find((a) => String(a.idAeropuerto ?? a.id) === String(id))
}

function codigoAeropuerto(id) {
  return obtenerCampo(aeropuertoPorId(id), ['codigoIata', 'codigo_iata'], '---')
}

function nombreAeropuerto(id) {
  const aeropuerto = aeropuertoPorId(id)
  return limpiarNombreCatalogo(obtenerCampo(aeropuerto, ['nombre', 'nombreAeropuerto'], 'Aeropuerto'))
}

function normalizarVuelo(v) {
  const origenId = obtenerCampo(v, ['idAeropuertoOrigen', 'id_aeropuerto_origen'])
  const destinoId = obtenerCampo(v, ['idAeropuertoDestino', 'id_aeropuerto_destino'])
  const numeroVuelo = obtenerCampo(v, ['numeroVuelo', 'numero_vuelo'], '')

  return {
    idVuelo: obtenerCampo(v, ['idVuelo', 'id']),
    numeroVuelo,
    codigoDisplay: numeroVuelo,
    origenId,
    destinoId,
    fechaHoraSalida: obtenerCampo(v, ['fechaHoraSalida', 'fecha_hora_salida']),
    fechaHoraLlegada: obtenerCampo(v, ['fechaHoraLlegada', 'fecha_hora_llegada']),
    duracionMin: Number(obtenerCampo(v, ['duracionMin', 'duracion_min'], 0)),
    precioBase: Number(obtenerCampo(v, ['precioBase', 'precio_base'], 0)),
    estado: obtenerCampo(v, ['estadoVuelo', 'estado_vuelo'], 'PROGRAMADO'),
    codigoOrigen: codigoAeropuerto(origenId),
    codigoDestino: codigoAeropuerto(destinoId),
    nombreOrigen: nombreAeropuerto(origenId),
    nombreDestino: nombreAeropuerto(destinoId),
    aeronave: 'Boeing 787-9 Dreamliner',
  }
}

const resumenPrecio = computed(() => {
  const base = Number(vuelo.value?.precioBase || reserva.vuelo?.precioBase || 0)
  const pasajeros = Number(reserva.vuelo?.pasajeros || 1)
  const subtotal = base * pasajeros
  const impuestos = Number((subtotal * 0.15).toFixed(2))
  const servicio = 15
  const total = Number((subtotal + impuestos + servicio).toFixed(2))

  return { pasajeros, subtotal, impuestos, servicio, total }
})

async function cargarDetalle() {
  cargando.value = true
  errorGeneral.value = ''

  try {
    await catalogos.cargarAeropuertos(true).catch(() => {})
    const [detalleResp, escalasResp] = await Promise.all([
      getVueloBookingDetalleApi(route.params.id),
      getEscalasVueloBookingApi(route.params.id),
    ])

    vuelo.value = normalizarVuelo(detalleResp.data?.data)
    escalas.value = normalizarLista(escalasResp.data?.data)
  } catch (error) {
    errorGeneral.value = error.response?.data?.message || 'No se pudo cargar el detalle del vuelo.'
  } finally {
    cargando.value = false
  }
}

function continuarReserva() {
  if (vuelo.value) {
    reserva.setVuelo({
      ...reserva.vuelo,
      ...vuelo.value,
    })
  }

  try {
    const progreso = JSON.parse(localStorage.getItem(KEY_AEROLINEA_PROGRESO) || 'null')
    if (progreso?.idAsiento) {
      reserva.setAsientos([
        {
          idAsiento: progreso.idAsiento,
          numeroAsiento: progreso.numeroAsiento || `Asiento ${progreso.idAsiento}`,
          precioExtra: Number(progreso.precioAsiento || 0),
        },
      ])
    }
  } catch {}

  router.push({ name: 'datos-pasajeros' })
}

onMounted(cargarDetalle)
</script>

<template>
  <div>
    <CheckoutStepper :paso-actual="1" />

    <section class="min-h-[calc(100vh-64px)] bg-background py-10">
      <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div v-if="cargando" class="rounded-[28px] bg-white p-10 text-center shadow-sm">
          <div class="mx-auto h-9 w-9 animate-spin rounded-full border-4 border-blue-accent/20 border-t-blue-accent" />
          <p class="mt-4 text-text-muted">Cargando detalle del vuelo...</p>
        </div>

        <div v-else-if="errorGeneral" class="rounded-[28px] border border-red-200 bg-red-50 p-6 text-red-700 shadow-sm">
          {{ errorGeneral }}
        </div>

        <div v-else-if="vuelo" class="grid gap-8 lg:grid-cols-[1.4fr_0.8fr]">
          <div class="space-y-8">
            <h1 class="text-3xl font-bold text-navy">Detalles del Vuelo</h1>

            <section class="overflow-hidden rounded-[28px] bg-white shadow-sm">
              <div class="bg-navy px-8 py-10 text-white">
                <div class="grid gap-8 md:grid-cols-3 md:items-center">
                  <div>
                    <p class="text-5xl font-light">{{ horaLegible(vuelo.fechaHoraSalida) }}</p>
                    <p class="mt-2 text-2xl font-semibold">{{ vuelo.nombreOrigen }}</p>
                    <p class="text-white/80">{{ vuelo.codigoOrigen }}</p>
                  </div>

                  <div class="text-center">
                    <p class="text-lg text-white/80">{{ duracionLegible(vuelo.duracionMin) }}</p>
                    <div class="mx-auto mt-3 flex max-w-[220px] items-center gap-2">
                      <span class="h-px flex-1 bg-white/30" />
                      <svg class="h-6 w-6 text-gold" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2 16l20-5-8 8-2.5-5.5L6 11l-4-1 20-5-5 20-3.5-7L2 16z" />
                      </svg>
                      <span class="h-px flex-1 bg-white/30" />
                    </div>
                    <p class="mt-3 text-gold">{{ escalas.length ? `${escalas.length} escalas` : 'Directo' }}</p>
                  </div>

                  <div class="md:text-right">
                    <p class="text-5xl font-light">{{ horaLegible(vuelo.fechaHoraLlegada) }}</p>
                    <p class="mt-2 text-2xl font-semibold">{{ vuelo.nombreDestino }}</p>
                    <p class="text-white/80">{{ vuelo.codigoDestino }}</p>
                  </div>
                </div>

                <div class="mt-8 border-t border-white/20 pt-6">
                  <div class="flex items-center gap-4">
                    <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/15">
                      <svg class="h-6 w-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2 16l20-5-8 8-2.5-5.5L6 11l-4-1 20-5-5 20-3.5-7L2 16z" />
                      </svg>
                    </div>
                    <div>
                      <p class="text-2xl font-semibold">{{ vuelo.codigoDisplay }}</p>
                      <p class="text-white/80">{{ vuelo.aeronave }}</p>
                    </div>
                  </div>
                </div>
              </div>

              <div class="space-y-8 px-8 py-8">
                <section>
                  <h2 class="text-2xl font-bold text-navy">Itinerario del Vuelo</h2>
                  <div class="mt-6 space-y-8">
                    <div class="relative pl-9">
                      <span class="absolute left-0 top-2 h-3 w-3 rounded-full bg-navy" />
                      <div class="absolute left-[5px] top-5 h-16 w-px bg-slate-300" />
                      <div class="flex items-center gap-3">
                        <p class="text-4xl font-light text-navy">{{ horaLegible(vuelo.fechaHoraSalida) }}</p>
                        <p class="text-text-muted">{{ fechaLegible(vuelo.fechaHoraSalida) }}</p>
                      </div>
                      <p class="mt-2 text-xl font-semibold text-navy">{{ vuelo.nombreOrigen }} ({{ vuelo.codigoOrigen }})</p>
                    </div>

                    <div class="relative pl-9">
                      <span class="absolute left-0 top-2 h-3 w-3 rounded-full bg-gold" />
                      <div class="flex items-center gap-3">
                        <p class="text-4xl font-light text-navy">{{ horaLegible(vuelo.fechaHoraLlegada) }}</p>
                        <p class="text-text-muted">{{ fechaLegible(vuelo.fechaHoraLlegada) }}</p>
                      </div>
                      <p class="mt-2 text-xl font-semibold text-navy">{{ vuelo.nombreDestino }} ({{ vuelo.codigoDestino }})</p>
                    </div>
                  </div>
                </section>

                <section class="border-t border-slate-200 pt-8">
                  <h2 class="text-2xl font-bold text-navy">Servicios a Bordo</h2>
                  <div class="mt-6 grid gap-4 md:grid-cols-4">
                    <div class="rounded-2xl bg-slate-50 p-5 text-center text-navy">WiFi Premium</div>
                    <div class="rounded-2xl bg-slate-50 p-5 text-center text-navy">Comida y Bebida</div>
                    <div class="rounded-2xl bg-slate-50 p-5 text-center text-navy">Entretenimiento</div>
                    <div class="rounded-2xl bg-slate-50 p-5 text-center text-navy">Asiento Reclinable</div>
                  </div>
                </section>

                <section class="border-t border-slate-200 pt-8">
                  <h2 class="text-2xl font-bold text-navy">Incluido en tu tarifa</h2>
                  <div class="mt-6 grid gap-4 md:grid-cols-2">
                    <div class="space-y-4 text-navy">
                      <p>✓ Equipaje de mano (8kg)</p>
                      <p>✓ Seleccion de asiento</p>
                    </div>
                    <div class="space-y-4 text-navy">
                      <p>Equipaje facturado (23kg) con costo adicional</p>
                      <p>✓ Cambio de vuelo*</p>
                    </div>
                  </div>
                  <p class="mt-4 text-sm text-text-muted">*Sujeto a disponibilidad y diferencias de tarifa</p>
                </section>

                <section class="rounded-2xl border border-blue-accent/20 bg-blue-50/60 p-6">
                  <h2 class="text-xl font-semibold text-blue-accent">Informacion Importante</h2>
                  <div class="mt-4 space-y-2 text-blue-accent">
                    <p>- Presentarse en el aeropuerto 3 horas antes de la salida</p>
                    <p>- Check-in online disponible 24h antes del vuelo</p>
                    <p>- Pasaporte o DNI vigente requerido para viajar</p>
                    <p>- Revisa los requisitos del destino antes de viajar</p>
                  </div>
                </section>
              </div>
            </section>
          </div>

          <aside class="lg:sticky lg:top-24 lg:self-start">
            <div class="rounded-[28px] bg-white p-8 shadow-sm">
              <h2 class="text-2xl font-bold text-navy">Resumen de Precio</h2>
              <div class="mt-6 space-y-4 text-text-muted">
                <div class="flex items-center justify-between">
                  <span>{{ resumenPrecio.pasajeros }} {{ resumenPrecio.pasajeros === 1 ? 'Adulto' : 'Pasajeros' }}</span>
                  <span>{{ moneda(resumenPrecio.subtotal) }}</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>Tasas e impuestos</span>
                  <span>{{ moneda(resumenPrecio.impuestos) }}</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>Cargos por servicio</span>
                  <span>{{ moneda(resumenPrecio.servicio) }}</span>
                </div>
              </div>

              <div class="mt-6 border-t border-slate-200 pt-6">
                <div class="flex items-center justify-between">
                  <span class="text-2xl font-semibold text-navy">Total</span>
                  <span class="text-5xl font-light text-navy">{{ moneda(resumenPrecio.total) }}</span>
                </div>
              </div>

              <button
                type="button"
                class="mt-8 w-full rounded-2xl bg-gold px-6 py-4 font-semibold text-navy transition-colors hover:bg-gold-light"
                @click="continuarReserva"
              >
                Continuar con Reserva
              </button>

              <button
                type="button"
                class="mt-4 w-full rounded-2xl border border-slate-300 px-6 py-4 font-semibold text-navy transition-colors hover:bg-slate-50"
                @click="router.push({ name: 'buscar-vuelos', query: route.query })"
              >
                Ver Otros Vuelos
              </button>

              <div class="mt-8 border-t border-slate-200 pt-6 text-text-muted">
                <p class="text-sm">Fecha de viaje</p>
                <p class="mt-1 text-lg font-semibold text-navy">{{ fechaLegible(vuelo.fechaHoraSalida) }}</p>
                <p class="mt-4 text-sm">Ruta</p>
                <p class="mt-1 text-lg font-semibold text-navy">{{ vuelo.nombreOrigen }} - {{ vuelo.nombreDestino }}</p>
              </div>
            </div>
          </aside>
        </div>
      </div>
    </section>
  </div>
</template>
