<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { getAsientosVueloBookingApi } from '@/api/vuelos.api'
import CheckoutStepper from '@/components/CheckoutStepper.vue'
import { useReservaStore } from '@/stores/reserva.store'
import { deepValue, extractItems } from '@/utils/portalCliente'

const router = useRouter()
const reserva = useReservaStore()

const columnas = ['A', 'B', 'C', 'D', 'E', 'F']
const filas = Array.from({ length: 28 }, (_, index) => index + 1)

const cargando = ref(false)
const errorGeneral = ref('')
const asientosApi = ref([])
const pasajeroActivo = ref(0)
const seleccionPorPasajero = ref([])

const vuelo = computed(() => reserva.vuelo)
const pasajeros = computed(() => reserva.pasajeros || [])

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

function obtenerCampo(obj, claves, fallback = null) {
  for (const clave of claves) {
    if (obj?.[clave] !== undefined && obj?.[clave] !== null) return obj[clave]
  }
  return deepValue(obj, claves) ?? fallback
}

function toBool(valor, fallback = false) {
  if (typeof valor === 'boolean') return valor
  if (typeof valor === 'number') return valor === 1
  if (typeof valor === 'string') {
    const normalizado = valor.trim().toLowerCase()
    if (['true', '1', 'si', 'sí', 'activo', 'activa', 'disponible', 'libre'].includes(normalizado)) return true
    if (['false', '0', 'no', 'bloqueado', 'reservado', 'ocupado', 'inactivo', 'inactiva'].includes(normalizado)) return false
  }
  return fallback
}

function normalizarDisponibilidad(asiento) {
  const disponible = obtenerCampo(asiento, ['disponible', 'available', 'isAvailable', 'activo'])
  if (disponible !== null) return toBool(disponible)

  const estado = String(obtenerCampo(asiento, ['estado', 'estadoAsiento', 'estado_asiento'], 'ACTIVO'))
    .trim()
    .toUpperCase()

  return ['ACTIVO', 'DISPONIBLE', 'LIBRE'].includes(estado)
}

function descomponerAsiento(numeroAsiento) {
  const valor = String(numeroAsiento || '').trim().toUpperCase()
  const formatoLetraNumero = valor.match(/^([A-F])(\d{1,2})$/)
  if (formatoLetraNumero) {
    return {
      columna: formatoLetraNumero[1],
      fila: Number(formatoLetraNumero[2]),
    }
  }

  const formatoNumeroLetra = valor.match(/^(\d{1,2})([A-F])$/)
  if (formatoNumeroLetra) {
    return {
      fila: Number(formatoNumeroLetra[1]),
      columna: formatoNumeroLetra[2],
    }
  }

  return null
}

function normalizarAsiento(asiento) {
  const numeroAsiento = obtenerCampo(asiento, ['numeroAsiento', 'numero_asiento'], '')
  const posicion = descomponerAsiento(numeroAsiento)

  return {
    idAsiento: obtenerCampo(asiento, ['idAsiento', 'id_asiento', 'id']),
    numeroAsiento,
    disponible: normalizarDisponibilidad(asiento),
    clase: obtenerCampo(asiento, ['clase'], 'ECONOMICA'),
    posicion: obtenerCampo(asiento, ['posicion'], ''),
    precioExtra: Number(obtenerCampo(asiento, ['precioExtra', 'precio_extra'], 0)),
    fila: posicion?.fila ?? null,
    columna: posicion?.columna ?? null,
  }
}

const mapaAsientos = computed(() => {
  const mapa = new Map()
  asientosApi.value.forEach((asiento) => {
    if (!asiento.fila || !asiento.columna) return
    mapa.set(`${asiento.fila}-${asiento.columna}`, asiento)
  })
  return mapa
})

const pasajerosConSeleccion = computed(() =>
  pasajeros.value.map((pasajero, indice) => ({
    indice,
    nombre: [pasajero.nombre_pasajero, pasajero.apellido_pasajero].filter(Boolean).join(' ') || `Pasajero ${indice + 1}`,
    asiento: seleccionPorPasajero.value[indice] || null,
  })),
)

const pasajeroActivoData = computed(() => pasajerosConSeleccion.value[pasajeroActivo.value] || null)

const pasajerosCompletos = computed(
  () =>
    pasajeros.value.length > 0 &&
    seleccionPorPasajero.value.length === pasajeros.value.length &&
    seleccionPorPasajero.value.every((item) => item?.idAsiento),
)

const asientosSeleccionadosIds = computed(() =>
  seleccionPorPasajero.value.map((item) => item?.idAsiento).filter(Boolean),
)

const recargoAsientos = computed(() =>
  seleccionPorPasajero.value.reduce((total, asiento) => total + Number(asiento?.precioExtra || 0), 0),
)

const totalConAsientos = computed(() => {
  const base = Number(vuelo.value?.precioBase || 0) * pasajeros.value.length
  const subtotal = base + recargoAsientos.value
  const iva = Number((subtotal * 0.15).toFixed(2))
  return Number((subtotal + iva).toFixed(2))
})

function asientoPorPosicion(fila, columna) {
  return mapaAsientos.value.get(`${fila}-${columna}`) || null
}

function claseLegible(clase) {
  if (clase === 'PRIMERA') return 'Primera'
  if (clase === 'EJECUTIVA') return 'Ejecutiva'
  return 'Economica'
}

function posicionLegible(posicion) {
  if (posicion === 'VENTANA') return 'Ventana'
  if (posicion === 'PASILLO') return 'Pasillo'
  if (posicion === 'CENTRO') return 'Centro'
  return posicion || '-'
}

function estadoAsiento(asiento, fila, columna) {
  if (!asiento) return 'ocupado'

  const seleccionadoActual = seleccionPorPasajero.value[pasajeroActivo.value]?.idAsiento === asiento.idAsiento
  if (seleccionadoActual) return 'seleccionado'

  const seleccionadoPorOtro = asientosSeleccionadosIds.value.includes(asiento.idAsiento)
  if (seleccionadoPorOtro) return 'bloqueado'

  if (!asiento.disponible) return 'ocupado'

  return 'disponible'
}

function seleccionarAsiento(asiento) {
  if (!asiento || !asiento.disponible) return

  const yaTomadoPorOtro = seleccionPorPasajero.value.some(
    (seleccion, indice) => indice !== pasajeroActivo.value && seleccion?.idAsiento === asiento.idAsiento,
  )
  if (yaTomadoPorOtro) return

  seleccionPorPasajero.value[pasajeroActivo.value] = {
    idAsiento: asiento.idAsiento,
    numeroAsiento: asiento.numeroAsiento,
    precioExtra: asiento.precioExtra,
    fila: asiento.fila,
    columna: asiento.columna,
    pasajeroIndex: pasajeroActivo.value,
  }

  reserva.setAsientos(seleccionPorPasajero.value.filter(Boolean))

  const siguientePendiente = seleccionPorPasajero.value.findIndex((item) => !item?.idAsiento)
  if (siguientePendiente !== -1) {
    pasajeroActivo.value = siguientePendiente
  }
}

async function cargarAsientos() {
  if (!vuelo.value?.idVuelo) return

  cargando.value = true
  errorGeneral.value = ''
  try {
      const { data } = await getAsientosVueloBookingApi(vuelo.value.idVuelo)

    asientosApi.value = extractItems(data).map(normalizarAsiento)
  } catch (error) {
    errorGeneral.value = error.response?.data?.message || 'No se pudieron cargar los asientos del vuelo.'
  } finally {
    cargando.value = false
  }
}

function continuarEquipaje() {
  if (!pasajerosCompletos.value) return
  reserva.setAsientos(seleccionPorPasajero.value.filter(Boolean))
  router.push({ name: 'equipaje' })
}

onMounted(async () => {
  if (!vuelo.value) {
    router.replace({ name: 'buscar-vuelos' })
    return
  }

  if (!pasajeros.value.length) {
    router.replace({ name: 'datos-pasajeros' })
    return
  }

  seleccionPorPasajero.value = pasajeros.value.map((_, indice) => reserva.asientos[indice] || null)
  pasajeroActivo.value = Math.max(seleccionPorPasajero.value.findIndex((item) => !item?.idAsiento), 0)
  await cargarAsientos()
})
</script>

<template>
  <div>
    <CheckoutStepper :paso-actual="3" />

    <section class="min-h-[calc(100vh-64px)] bg-background py-10">
      <div class="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <div v-if="!vuelo" class="rounded-[28px] bg-white p-10 text-center shadow-sm">
          <p class="text-lg font-semibold text-navy">Primero selecciona un vuelo.</p>
          <button
            type="button"
            class="mt-6 rounded-2xl bg-gold px-6 py-3 font-semibold text-navy"
            @click="router.push({ name: 'buscar-vuelos' })"
          >
            Ir a vuelos
          </button>
        </div>

        <div v-else class="grid gap-8 lg:grid-cols-[1.35fr_0.8fr]">
          <div class="rounded-[28px] bg-white p-8 shadow-sm">
            <p class="text-sm font-semibold uppercase tracking-[0.28em] text-gold-dark">Paso 3</p>
            <h1 class="mt-3 text-3xl font-bold text-navy">Seleccion de asientos</h1>
            <p class="mt-4 text-text-muted">
              Asigna un asiento por cada pasajero. Los asientos ocupados no se pueden elegir.
            </p>

            <div class="mt-8 flex flex-wrap gap-3">
              <button
                v-for="pasajero in pasajerosConSeleccion"
                :key="pasajero.indice"
                type="button"
                class="rounded-2xl border px-4 py-3 text-left transition-colors"
                :class="
                  pasajeroActivo === pasajero.indice
                    ? 'border-gold bg-gold/10'
                    : pasajero.asiento
                      ? 'border-emerald-200 bg-emerald-50'
                      : 'border-slate-200 bg-slate-50'
                "
                @click="pasajeroActivo = pasajero.indice"
              >
                <p class="font-semibold text-navy">{{ pasajero.nombre }}</p>
                <p class="mt-1 text-sm text-text-muted">
                  {{ pasajero.asiento?.numeroAsiento || 'Sin asiento seleccionado' }}
                </p>
              </button>
            </div>

            <div class="mt-8 rounded-[24px] border border-slate-200 p-6">
              <div class="flex items-center justify-center gap-6 text-sm text-navy">
                <div class="flex items-center gap-2">
                  <span class="h-6 w-6 rounded-md border border-blue-100 bg-sky-100" />
                  <span>Disponible</span>
                </div>
                <div class="flex items-center gap-2">
                  <span class="h-6 w-6 rounded-md bg-gold" />
                  <span>Seleccionado</span>
                </div>
                <div class="flex items-center gap-2">
                  <span class="h-6 w-6 rounded-md bg-slate-200" />
                  <span>Ocupado</span>
                </div>
              </div>

              <div class="mt-5 flex flex-wrap items-center justify-center gap-4 text-xs text-text-muted">
                <span class="rounded-full bg-slate-100 px-3 py-1">Filas 1-4: Primera +$80</span>
                <span class="rounded-full bg-slate-100 px-3 py-1">Filas 5-10: Ejecutiva +$40</span>
                <span class="rounded-full bg-slate-100 px-3 py-1">Filas 11-28: Economica +$0</span>
              </div>

              <div class="mt-6 flex justify-center">
                <div class="rounded-t-2xl bg-navy px-6 py-3 text-sm font-semibold text-white">Cabina</div>
              </div>

              <div v-if="cargando" class="py-16 text-center">
                <div class="mx-auto h-9 w-9 animate-spin rounded-full border-4 border-blue-accent/20 border-t-blue-accent" />
                <p class="mt-4 text-text-muted">Cargando asientos...</p>
              </div>

              <div v-else-if="errorGeneral" class="mt-6 rounded-2xl border border-red-200 bg-red-50 p-4 text-red-700">
                {{ errorGeneral }}
              </div>

              <div v-else class="mt-8 overflow-x-auto">
                <div class="mx-auto min-w-[420px] max-w-[520px]">
                  <div
                    v-for="fila in filas"
                    :key="fila"
                    class="grid grid-cols-[32px_repeat(3,40px)_28px_repeat(3,40px)] items-center gap-3 py-1"
                  >
                    <span class="text-right text-sm text-text-muted">{{ fila }}</span>

                    <template v-for="columna in columnas" :key="`${fila}-${columna}`">
                      <div v-if="columna === 'D'" class="w-4" />
                      <button
                        v-if="asientoPorPosicion(fila, columna) || true"
                        type="button"
                        :title="
                          estadoAsiento(asientoPorPosicion(fila, columna), fila, columna) === 'ocupado'
                            ? 'Asiento no disponible'
                            : `${fila}${columna}`
                        "
                        class="group relative flex h-10 w-10 items-center justify-center rounded-md border text-sm font-medium transition"
                        :class="{
                          'cursor-pointer border-blue-100 bg-sky-100 text-navy hover:bg-sky-200': estadoAsiento(asientoPorPosicion(fila, columna), fila, columna) === 'disponible',
                          'cursor-pointer border-gold bg-gold text-navy': estadoAsiento(asientoPorPosicion(fila, columna), fila, columna) === 'seleccionado',
                          'cursor-not-allowed border-slate-200 bg-slate-200 text-slate-400': ['ocupado', 'bloqueado'].includes(estadoAsiento(asientoPorPosicion(fila, columna), fila, columna)),
                        }"
                        @click="seleccionarAsiento(asientoPorPosicion(fila, columna))"
                      >
                        <span
                          v-if="estadoAsiento(asientoPorPosicion(fila, columna), fila, columna) === 'seleccionado'"
                          class="text-base"
                        >
                          ✓
                        </span>
                        <span v-else>{{ columna }}</span>
                        <span
                          v-if="['ocupado', 'bloqueado'].includes(estadoAsiento(asientoPorPosicion(fila, columna), fila, columna))"
                          class="pointer-events-none absolute inset-0 hidden items-center justify-center rounded-md bg-slate-300/85 text-base text-slate-500 group-hover:flex"
                        >
                          ⃠
                        </span>
                      </button>
                    </template>
                  </div>
                </div>
              </div>

              <div class="mt-8 border-t border-slate-200 pt-6">
                <div class="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
                  <div>
                    <p class="text-sm text-text-muted">Asiento seleccionado para {{ pasajeroActivoData?.nombre }}</p>
                    <p class="mt-2 text-3xl font-light text-navy">
                      {{ pasajeroActivoData?.asiento?.numeroAsiento || '--' }}
                    </p>
                    <div v-if="pasajeroActivoData?.asiento" class="mt-2 space-y-1 text-sm text-text-muted">
                      <p>Clase: {{ claseLegible(pasajeroActivoData.asiento.clase) }}</p>
                      <p>Posicion: {{ posicionLegible(pasajeroActivoData.asiento.posicion) }}</p>
                      <p>Recargo: {{ moneda(pasajeroActivoData.asiento.precioExtra || 0) }}</p>
                    </div>
                  </div>

                  <button
                    type="button"
                    class="inline-flex items-center justify-center gap-2 rounded-2xl bg-gold px-6 py-3 font-semibold text-navy transition-colors hover:bg-gold-light disabled:cursor-not-allowed disabled:bg-gold/40"
                    :disabled="!pasajerosCompletos"
                    @click="continuarEquipaje"
                  >
                    <span>Continuar</span>
                    <span>→</span>
                  </button>
                </div>
              </div>
            </div>
          </div>

          <aside class="rounded-[28px] bg-white p-8 shadow-sm">
            <h2 class="text-2xl font-bold text-navy">Resumen del Vuelo</h2>
            <div class="mt-6 space-y-4 text-text-muted">
              <div class="flex items-center justify-between">
                <span>Codigo</span>
                <span class="font-semibold text-navy">{{ vuelo.numeroVuelo }}</span>
              </div>
              <div class="flex items-center justify-between">
                <span>Fecha</span>
                <span class="font-semibold text-navy">{{ fechaLegible(vuelo.fechaHoraSalida) }}</span>
              </div>
              <div class="flex items-center justify-between">
                <span>Pasajeros</span>
                <span class="font-semibold text-navy">{{ pasajeros.length }}</span>
              </div>
              <div class="flex items-center justify-between">
                <span>Clase</span>
                <span class="font-semibold text-navy">{{ vuelo.clase }}</span>
              </div>
            </div>

            <div class="mt-6 border-t border-slate-200 pt-6">
              <div class="flex items-center justify-between">
                <span class="text-2xl font-semibold text-navy">Desde</span>
                <span class="text-4xl font-light text-navy">{{ moneda(vuelo.precioBase) }}</span>
              </div>
            </div>

            <div class="mt-8 space-y-3">
              <div
                v-for="pasajero in pasajerosConSeleccion"
                :key="`seleccion-${pasajero.indice}`"
                class="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3"
              >
                <p class="font-semibold text-navy">{{ pasajero.nombre }}</p>
                <p class="mt-1 text-sm text-text-muted">
                  {{ pasajero.asiento?.numeroAsiento || 'Pendiente de seleccionar' }}
                </p>
                <p v-if="pasajero.asiento" class="mt-1 text-xs text-text-muted">
                  {{ claseLegible(pasajero.asiento.clase) }} · {{ moneda(pasajero.asiento.precioExtra || 0) }}
                </p>
              </div>
            </div>

            <div class="mt-6 border-t border-slate-200 pt-6 text-text-muted">
              <div class="flex items-center justify-between">
                <span>Recargo por asientos</span>
                <span class="font-semibold text-navy">{{ moneda(recargoAsientos) }}</span>
              </div>
              <div class="mt-3 flex items-center justify-between">
                <span class="text-lg font-semibold text-navy">Total estimado</span>
                <span class="text-3xl font-light text-navy">{{ moneda(totalConAsientos) }}</span>
              </div>
            </div>

            <button
              type="button"
              class="mt-8 w-full rounded-2xl border border-slate-300 px-6 py-4 font-semibold text-navy transition-colors hover:bg-slate-50"
              @click="router.push({ name: 'datos-pasajeros' })"
            >
              Volver a pasajeros
            </button>
          </aside>
        </div>
      </div>
    </section>
  </div>
</template>
