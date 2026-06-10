<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { getClienteBoletosApi, getClienteReservaBoletosApi } from '@/api/boletos.api'
import { getClienteReservasApi } from '@/api/reservas.api'
import { useAutenticacionStore } from '@/stores/autenticacion.store'
import { useClienteStore } from '@/stores/cliente.store'
import { deepValue, extractItems, leerPortalReservas, longDate } from '@/utils/portalCliente'

const router = useRouter()
const auth = useAutenticacionStore()
const cliente = useClienteStore()

const cargando = ref(true)
const cargandoDetalle = ref(false)
const error = ref('')
const reservasConBoletos = ref([])
const reservaExpandida = ref(null)
const boletosPorReserva = ref({})

const nombreVisible = computed(() =>
  cliente.nombreMostrado || auth.usuario?.username || 'Cliente',
)

function normalizarReserva(item) {
  const vuelo = deepValue(item, ['vuelo']) || {}

  return {
    idReserva:
      deepValue(item, ['idReserva', 'id_reserva', 'id']) ||
      deepValue(vuelo, ['idReserva', 'id_reserva']) ||
      null,
    codigoReserva:
      deepValue(item, ['codigoReserva', 'codigo_reserva']) ||
      deepValue(vuelo, ['codigoReserva', 'codigo_reserva']) ||
      '',
    numeroVuelo:
      deepValue(item, ['numeroVuelo', 'numero_vuelo']) ||
      deepValue(vuelo, ['numeroVuelo', 'numero_vuelo']) ||
      '',
    fecha:
      deepValue(item, ['fechaInicio', 'fecha_inicio', 'fechaVuelo', 'fecha_vuelo']) ||
      deepValue(vuelo, ['fechaHoraSalida', 'fecha_hora_salida']) ||
      '',
    origen:
      deepValue(item, ['codigoOrigen', 'codigo_origen']) ||
      deepValue(vuelo, ['codigoOrigen', 'codigo_origen']) ||
      '',
    destino:
      deepValue(item, ['codigoDestino', 'codigo_destino']) ||
      deepValue(vuelo, ['codigoDestino', 'codigo_destino']) ||
      '',
  }
}

function normalizarBoleto(item) {
  const vuelo = deepValue(item, ['vuelo']) || {}

  return {
    idBoleto: deepValue(item, ['idBoleto', 'id_boleto', 'id']) || null,
    idReserva:
      deepValue(item, ['idReserva', 'id_reserva']) ||
      deepValue(vuelo, ['idReserva', 'id_reserva']) ||
      null,
    codigoReserva:
      deepValue(item, ['codigoReserva', 'codigo_reserva']) ||
      deepValue(vuelo, ['codigoReserva', 'codigo_reserva']) ||
      '',
    numeroBoleto:
      deepValue(item, ['numeroBoleto', 'numero_boleto', 'codigoBoleto']) ||
      `BO-${deepValue(item, ['idBoleto', 'id_boleto', 'id']) || '001'}`,
    asiento:
      deepValue(item, ['numeroAsiento', 'numero_asiento', 'asiento', 'codigoAsiento', 'codigo_asiento']) ||
      deepValue(vuelo, ['numeroAsiento', 'numero_asiento']) ||
      '-',
    pasajero:
      [
        deepValue(item, ['nombrePasajero', 'nombre_pasajero']),
        deepValue(item, ['apellidoPasajero', 'apellido_pasajero']),
      ]
        .filter(Boolean)
        .join(' ')
        .trim() || nombreVisible.value,
    fecha:
      deepValue(item, ['fechaVuelo', 'fecha_vuelo', 'fechaInicio', 'fecha_inicio']) ||
      deepValue(vuelo, ['fechaHoraSalida', 'fecha_hora_salida']) ||
      '',
  }
}

function boletosLocalesReserva(reserva) {
  const local = leerPortalReservas().find((item) =>
    String(item.idReserva || '') === String(reserva?.idReserva || '')
      || String(item.codigoReserva || '') === String(reserva?.codigoReserva || ''),
  )

  return Array.isArray(local?.pasajeros)
    ? local.pasajeros.map((pasajero, index) => ({
        idBoleto: `${local.idReserva || local.numeroVuelo}-${index + 1}`,
        numeroBoleto: `BO-${local.numeroVuelo || 'AV1001'}-${String(index + 1).padStart(3, '0')}`,
        asiento: pasajero.asiento || '-',
        pasajero: pasajero.nombre || nombreVisible.value,
        fecha: local.fecha || '',
      }))
    : []
}

function completarAsientosConLocal(reserva, boletos) {
  const locales = boletosLocalesReserva(reserva)

  return boletos.map((boleto, index) => ({
    ...boleto,
    asiento:
      boleto.asiento && boleto.asiento !== '-'
        ? boleto.asiento
        : locales[index]?.asiento || '-',
  }))
}

function extraerBoletosRespuesta(responseLike) {
  const items = extractItems(responseLike)
  if (items.length) return items

  const raw = responseLike?.data?.data ?? responseLike?.data ?? responseLike ?? null
  const idBoleto = deepValue(raw, ['idBoleto', 'id_boleto', 'id'])

  return idBoleto ? [raw] : []
}

function fallbackLocal() {
  return leerPortalReservas().map((item) => {
    const [origen = '', destino = ''] = String(item.ruta || '')
      .split('-')
      .map((parte) => parte.trim())

    return {
      idReserva: item.idReserva || null,
      codigoReserva: item.codigoReserva || '',
      numeroVuelo: item.numeroVuelo || '',
      fecha: item.fecha || '',
      origen,
      destino,
      boletos: (item.pasajeros || []).map((pasajero, index) => ({
        idBoleto: `${item.idReserva || item.numeroVuelo}-${index + 1}`,
        numeroBoleto: `BO-${item.numeroVuelo || 'AV1001'}-${String(index + 1).padStart(3, '0')}`,
        asiento: pasajero.asiento || '-',
        pasajero: pasajero.nombre || nombreVisible.value,
        fecha: item.fecha || '',
      })),
    }
  })
}

async function cargarReservasCliente() {
  const respuesta = await getClienteReservasApi()
  return extractItems(respuesta)
}

async function cargarBoletosCliente() {
  const respuesta = await getClienteBoletosApi()
  return extractItems(respuesta)
}

async function cargarBoletosDeReservas(reservasBase) {
  const resultados = await Promise.all(
    reservasBase
      .filter((reserva) => reserva.idReserva)
      .map(async (reserva) => {
        try {
          const respuesta = await getClienteReservaBoletosApi(reserva.idReserva)
          return extraerBoletosRespuesta(respuesta).map((boleto) => ({
            ...boleto,
            idReserva: deepValue(boleto, ['idReserva', 'id_reserva']) || reserva.idReserva,
            codigoReserva: deepValue(boleto, ['codigoReserva', 'codigo_reserva']) || reserva.codigoReserva,
            numeroVuelo: deepValue(boleto, ['numeroVuelo', 'numero_vuelo']) || reserva.numeroVuelo,
            fechaVuelo: deepValue(boleto, ['fechaVuelo', 'fecha_vuelo']) || reserva.fecha,
          }))
        } catch {
          return []
        }
      }),
  )

  return resultados.flat()
}

async function cargarBoletosReserva(idReserva) {
  if (!idReserva) return
  cargandoDetalle.value = true

  try {
    const respuesta = await getClienteReservaBoletosApi(idReserva)
    const local = reservasConBoletos.value.find((item) => String(item.idReserva) === String(idReserva))
    const boletos = completarAsientosConLocal(local, extraerBoletosRespuesta(respuesta).map(normalizarBoleto))

    boletosPorReserva.value = {
      ...boletosPorReserva.value,
      [idReserva]: boletos.length ? boletos : local?.boletos || [],
    }
  } catch {
    const local = reservasConBoletos.value.find((item) => String(item.idReserva) === String(idReserva))
    boletosPorReserva.value = {
      ...boletosPorReserva.value,
      [idReserva]: local?.boletos || [],
    }
  } finally {
    cargandoDetalle.value = false
  }
}

async function toggleReserva(reserva) {
  const clave = reserva.idReserva || reserva.codigoReserva
  if (reservaExpandida.value === clave) {
    reservaExpandida.value = null
    return
  }

  reservaExpandida.value = clave

  if (reserva.idReserva && !boletosPorReserva.value[reserva.idReserva]) {
    await cargarBoletosReserva(reserva.idReserva)
  }
}

async function cargarBoletos() {
  cargando.value = true
  error.value = ''

  try {
    const reservasItems = await cargarReservasCliente()
    const reservasBase = reservasItems.map(normalizarReserva)
    let boletos = []

    try {
      boletos = await cargarBoletosCliente()
    } catch {
      boletos = []
    }

    if (!boletos.length) {
      boletos = await cargarBoletosDeReservas(reservasBase)
    }

    if (boletos.length && boletos.every((item) => !deepValue(item, ['idReserva', 'id_reserva']) && !deepValue(item, ['codigoReserva', 'codigo_reserva']))) {
      boletos = await cargarBoletosDeReservas(reservasBase)
    }

    const boletosAgrupados = new Map()

    boletos.forEach((item) => {
      const idReserva =
        deepValue(item, ['idReserva', 'id_reserva']) ||
        deepValue(item, ['vuelo', 'idReserva', 'id_reserva']) ||
        null
      const codigoReserva = deepValue(item, ['codigoReserva', 'codigo_reserva']) || ''

      if (!idReserva && !codigoReserva) return

      const clave = String(idReserva || codigoReserva)
      const actuales = boletosAgrupados.get(clave) || []
      const reserva = reservasBase.find((entry) =>
        String(entry.idReserva) === String(idReserva) ||
        String(entry.codigoReserva || '') === String(codigoReserva),
      )
      actuales.push(...completarAsientosConLocal(reserva, [normalizarBoleto(item)]))
      boletosAgrupados.set(clave, actuales)
    })

    reservasConBoletos.value = reservasBase
      .map((reserva) => ({
        ...reserva,
        boletos: boletosAgrupados.get(String(reserva.idReserva)) || boletosAgrupados.get(String(reserva.codigoReserva)) || [],
      }))
      .filter((reserva) => reserva.boletos.length)
  } catch (err) {
    reservasConBoletos.value = fallbackLocal()

    if (!reservasConBoletos.value.length) {
      error.value = err.response?.data?.message || 'No se pudieron cargar los boletos.'
    }
  } finally {
    cargando.value = false
  }
}

onMounted(cargarBoletos)
</script>

<template>
  <section class="space-y-6">
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-3xl font-bold text-navy">Mis Boletos</h1>
        <p class="mt-2 text-text-muted">Consulta los boletos agrupados por cada reserva.</p>
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
      Cargando boletos...
    </div>

    <div v-else-if="!reservasConBoletos.length" class="rounded-[28px] bg-white p-8 text-text-muted shadow-sm">
      Aún no existen boletos emitidos para este cliente.
    </div>

    <div v-else class="space-y-6">
      <article
        v-for="reserva in reservasConBoletos"
        :key="reserva.idReserva || `${reserva.numeroVuelo}-${reserva.fecha}`"
        class="overflow-hidden rounded-[28px] bg-white shadow-sm"
      >
        <div class="bg-[linear-gradient(120deg,#12284A_0%,#2F4360_100%)] px-8 py-8 text-white">
          <div class="flex items-start justify-between gap-4">
            <div class="flex h-14 w-14 items-center justify-center rounded-[18px] bg-white/10">
              <svg class="h-8 w-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M5 9a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v2a2 2 0 0 0 0 4v2a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2v-2a2 2 0 0 0 0-4V9Z" />
              </svg>
            </div>

            <div class="text-right">
              <p class="text-white/70">Reserva</p>
              <p class="mt-1 text-3xl font-semibold">
                {{ reserva.codigoReserva || `Reserva ${reserva.idReserva}` }}
              </p>
            </div>
          </div>

          <div class="mt-10">
            <p class="text-4xl font-light">{{ reserva.origen || 'Origen' }} → {{ reserva.destino || 'Destino' }}</p>
            <p class="mt-2 text-2xl text-white/80">{{ reserva.numeroVuelo || 'Vuelo emitido' }}</p>
          </div>
        </div>

        <div class="px-8 py-8">
          <div class="grid gap-6 md:grid-cols-4">
            <div>
              <p class="text-text-muted">Fecha</p>
              <p class="mt-2 text-2xl font-semibold text-navy">{{ longDate(reserva.fecha) }}</p>
            </div>
            <div>
              <p class="text-text-muted">Boletos</p>
              <p class="mt-2 text-2xl font-semibold text-navy">{{ reserva.boletos.length }}</p>
            </div>
            <div class="md:col-span-2 md:flex md:items-end md:justify-end">
              <button
                type="button"
                class="rounded-2xl border border-blue-accent px-5 py-3 font-semibold text-blue-accent transition-colors hover:bg-blue-accent hover:text-white"
                @click="toggleReserva(reserva)"
              >
                {{ reservaExpandida === (reserva.idReserva || reserva.codigoReserva) ? 'Ocultar boletos' : 'Ver boletos de esta reserva' }}
              </button>
            </div>
          </div>

          <div
            v-if="reservaExpandida === (reserva.idReserva || reserva.codigoReserva)"
            class="mt-8 space-y-4 border-t border-slate-200 pt-6"
          >
            <p class="text-lg font-semibold text-navy">Boletos de la reserva</p>

            <div v-if="cargandoDetalle && reserva.idReserva && !boletosPorReserva[reserva.idReserva]" class="text-text-muted">
              Cargando boletos...
            </div>

            <div
              v-for="boleto in (reserva.idReserva ? boletosPorReserva[reserva.idReserva] || reserva.boletos : reserva.boletos)"
              :key="boleto.idBoleto || boleto.numeroBoleto"
              class="grid gap-4 rounded-2xl bg-slate-50 px-5 py-4 md:grid-cols-2"
            >
              <div>
                <p class="text-sm text-text-muted">Boleto</p>
                <p class="mt-1 font-semibold text-navy">{{ boleto.numeroBoleto }}</p>
              </div>
              <div>
                <p class="text-sm text-text-muted">Asiento</p>
                <p class="mt-1 font-semibold text-navy">{{ boleto.asiento }}</p>
              </div>
            </div>
          </div>
        </div>
      </article>
    </div>
  </section>
</template>
