<script setup>
import { computed, onMounted, ref } from 'vue'
import InputApp from '@/components/base/InputApp.vue'
import { getBoletoApi, getBoletosApi } from '@/api/boletos.api'
import { getClientesApi } from '@/api/clientes.api'
import { getEquipajesBoletoApi } from '@/api/equipaje.api'
import { getPasajerosApi } from '@/api/pasajeros.api'
import { getReservasApi } from '@/api/reservas.api'
import { deepValue, extractItems } from '@/utils/portalCliente'

const cargando = ref(true)
const cargandoDetalle = ref(false)
const error = ref('')
const errorModal = ref('')
const busqueda = ref('')
const boletos = ref([])
const clientes = ref([])
const pasajerosCatalogo = ref([])
const reservas = ref([])
const equipajesPorBoleto = ref({})
const modalDetalle = ref(false)
const boletoDetalle = ref(null)

const boletosFiltrados = computed(() => {
  const termino = busqueda.value.trim().toLowerCase()

  return boletos.value
    .slice()
    .sort((a, b) => new Date(b.fechaVuelo || 0).getTime() - new Date(a.fechaVuelo || 0).getTime())
    .filter((boleto) => {
      if (!termino) return true
      return [boleto.numeroBoleto, boleto.pasajeroDisplay, boleto.reservaDisplay, boleto.asiento]
        .filter(Boolean)
        .some((valor) => String(valor).toLowerCase().includes(termino))
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

function normalizarPasajero(item) {
  return {
    idPasajero: deepValue(item, ['idPasajero', 'id_pasajero', 'id']) || null,
    nombreCompleto: [
      deepValue(item, ['nombrePasajero', 'nombre_pasajero']),
      deepValue(item, ['apellidoPasajero', 'apellido_pasajero']),
    ]
      .filter(Boolean)
      .join(' ')
      .trim(),
  }
}

function normalizarReserva(item) {
  return {
    idReserva: deepValue(item, ['idReserva', 'id_reserva', 'id']) || null,
    idCliente: deepValue(item, ['idCliente', 'id_cliente']) || null,
    codigoReserva: deepValue(item, ['codigoReserva', 'codigo_reserva']) || '',
    numeroVuelo: deepValue(item, ['numeroVuelo', 'numero_vuelo']) || '',
  }
}

function normalizarEquipaje(item) {
  return {
    idEquipaje: deepValue(item, ['idEquipaje', 'id_equipaje', 'id']) || null,
    etiqueta: deepValue(item, ['etiqueta']) || '',
    tipo: deepValue(item, ['tipoEquipaje', 'tipo_equipaje', 'tipo']) || '',
    pesoKg: Number(deepValue(item, ['pesoKg', 'peso_kg', 'peso']) || 0),
    precioExtra: Number(deepValue(item, ['precioExtra', 'precio_extra']) || 0),
    estado: deepValue(item, ['estadoEquipaje', 'estado_equipaje', 'estado']) || '',
  }
}

function normalizarBoleto(item) {
  const idReserva = deepValue(item, ['idReserva', 'id_reserva']) || null
  const idCliente = deepValue(item, ['idCliente', 'id_cliente']) || null
  const idPasajero = deepValue(item, ['idPasajero', 'id_pasajero']) || null

  const reserva = reservas.value.find((entry) => String(entry.idReserva) === String(idReserva || ''))
  const cliente = clientes.value.find((entry) => String(entry.idCliente) === String(idCliente || reserva?.idCliente || ''))
  const pasajero = pasajerosCatalogo.value.find((entry) => String(entry.idPasajero) === String(idPasajero || ''))

  const nombrePasajero =
    [
      deepValue(item, ['nombrePasajero', 'nombre_pasajero']),
      deepValue(item, ['apellidoPasajero', 'apellido_pasajero']),
    ]
      .filter(Boolean)
      .join(' ')
      .trim() ||
    pasajero?.nombreCompleto ||
    cliente?.nombreCompleto ||
    'Pasajero no disponible'

  const numeroBoleto =
    deepValue(item, ['numeroBoleto', 'numero_boleto', 'codigoBoleto']) ||
    `BO-${deepValue(item, ['idBoleto', 'id_boleto', 'id']) || '001'}`

  return {
    idBoleto: deepValue(item, ['idBoleto', 'id_boleto', 'id']) || null,
    idReserva,
    idCliente: idCliente || reserva?.idCliente || null,
    idPasajero,
    numeroBoleto,
    pasajeroDisplay: nombrePasajero,
    clienteDisplay: [cliente?.nombreCompleto, cliente?.correo].filter(Boolean).join(' / ') || cliente?.correo || '-',
    reservaDisplay: deepValue(item, ['codigoReserva', 'codigo_reserva']) || reserva?.codigoReserva || `Reserva ${idReserva || '-'}`,
    numeroVuelo: deepValue(item, ['numeroVuelo', 'numero_vuelo']) || reserva?.numeroVuelo || '',
    asiento: deepValue(item, ['numeroAsiento', 'numero_asiento', 'asiento']) || '-',
    fechaVuelo:
      deepValue(item, ['fechaVuelo', 'fecha_vuelo', 'fechaInicio', 'fecha_inicio']) ||
      deepValue(item, ['fechaHoraSalida', 'fecha_hora_salida']) ||
      '',
    cargoEquipaje: Number(deepValue(item, ['cargoEquipaje', 'cargo_equipaje']) || 0),
  }
}

function cantidadEquipajes(idBoleto) {
  return equipajesPorBoleto.value[String(idBoleto)]?.length || 0
}

async function cargarEquipajes() {
  const mapa = {}

  await Promise.all(
    boletos.value
      .filter((boleto) => boleto.idBoleto)
      .map(async (boleto) => {
        try {
          const respuesta = await getEquipajesBoletoApi(boleto.idBoleto)
          mapa[String(boleto.idBoleto)] = extractItems(respuesta).map(normalizarEquipaje)
        } catch {
          mapa[String(boleto.idBoleto)] = []
        }
      }),
  )

  equipajesPorBoleto.value = mapa
}

async function cargarTodo() {
  cargando.value = true
  error.value = ''

  try {
    const [respuestaClientes, respuestaPasajeros, respuestaReservas, respuestaBoletos] = await Promise.all([
      getClientesApi({ page: 1, page_size: 200 }),
      getPasajerosApi({ page: 1, page_size: 200 }),
      getReservasApi({ page: 1, page_size: 200 }),
      getBoletosApi({ page: 1, page_size: 200 }),
    ])

    clientes.value = extractItems(respuestaClientes).map(normalizarCliente)
    pasajerosCatalogo.value = extractItems(respuestaPasajeros).map(normalizarPasajero)
    reservas.value = extractItems(respuestaReservas).map(normalizarReserva)
    boletos.value = extractItems(respuestaBoletos).map(normalizarBoleto)

    await cargarEquipajes()
  } catch (err) {
    error.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudieron cargar los boletos.'
  } finally {
    cargando.value = false
  }
}

async function abrirDetalle(boleto) {
  boletoDetalle.value = null
  cargandoDetalle.value = true
  errorModal.value = ''
  modalDetalle.value = true

  try {
    const respuesta = await getBoletoApi(boleto.idBoleto)
    const data = deepValue(respuesta, ['data']) || respuesta
    const detalle = normalizarBoleto(data)
    boletoDetalle.value = {
      ...detalle,
      equipajes: equipajesPorBoleto.value[String(detalle.idBoleto)] || [],
    }
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo cargar el detalle del boleto.'
  } finally {
    cargandoDetalle.value = false
  }
}

onMounted(cargarTodo)
</script>

<template>
  <section class="space-y-5">
    <div class="overflow-hidden rounded-[28px] bg-[linear-gradient(135deg,#ffffff_0%,#f7faff_100%)] p-6 shadow-sm">
      <div class="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p class="text-sm font-semibold uppercase tracking-[0.28em] text-gold-dark">Emision y seguimiento</p>
          <h1 class="mt-2 text-[2rem] font-bold text-navy">Gestion de Boletos</h1>
          <p class="mt-2 max-w-2xl text-sm text-text-muted">
            Consulta boletos emitidos, asientos asignados y equipaje asociado desde una sola vista operativa.
          </p>
        </div>

        <div class="grid gap-3 sm:grid-cols-3">
          <div class="rounded-2xl border border-slate-200 bg-white px-5 py-4">
            <p class="text-xs font-semibold uppercase tracking-[0.14em] text-text-muted">Emitidos</p>
            <p class="mt-2 text-2xl font-semibold text-navy">{{ boletos.length }}</p>
          </div>
          <div class="rounded-2xl border border-emerald-100 bg-emerald-50 px-5 py-4">
            <p class="text-xs font-semibold uppercase tracking-[0.14em] text-emerald-700">Con equipaje</p>
            <p class="mt-2 text-2xl font-semibold text-emerald-700">
              {{ boletos.filter((item) => cantidadEquipajes(item.idBoleto)).length }}
            </p>
          </div>
          <div class="rounded-2xl border border-slate-200 bg-white px-5 py-4">
            <p class="text-xs font-semibold uppercase tracking-[0.14em] text-text-muted">Cargos equipaje</p>
            <p class="mt-2 text-2xl font-semibold text-navy">
              {{ money(boletos.reduce((acc, item) => acc + Number(item.cargoEquipaje || 0), 0)) }}
            </p>
          </div>
        </div>
      </div>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <section class="rounded-[24px] bg-white p-4 shadow-sm">
      <InputApp v-model="busqueda" placeholder="Buscar por boleto, pasajero, reserva o asiento..." />
    </section>

    <section class="rounded-[24px] bg-white shadow-sm">
      <div class="overflow-x-auto">
        <table class="min-w-[1240px] w-full border-collapse">
          <thead class="bg-slate-50">
            <tr class="text-left text-sm font-semibold text-navy">
              <th class="px-5 py-3.5">Boleto</th>
              <th class="px-5 py-3.5">Pasajero</th>
              <th class="px-5 py-3.5">Reserva</th>
              <th class="px-5 py-3.5">Vuelo</th>
              <th class="px-5 py-3.5">Asiento</th>
              <th class="px-5 py-3.5">Equipaje</th>
              <th class="px-5 py-3.5">Cargo equipaje</th>
              <th class="px-5 py-3.5">Fecha</th>
              <th class="px-5 py-3.5 text-right">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="cargando">
              <td colspan="9" class="px-5 py-5 text-sm text-text-muted">Cargando boletos...</td>
            </tr>
            <tr v-else-if="!boletosFiltrados.length">
              <td colspan="9" class="px-5 py-5 text-sm text-text-muted">No hay boletos emitidos.</td>
            </tr>
            <tr
              v-for="boleto in boletosFiltrados"
              v-else
              :key="boleto.idBoleto"
              class="border-t border-slate-100 align-middle"
            >
              <td class="px-5 py-4 text-sm font-semibold text-navy">{{ boleto.numeroBoleto }}</td>
              <td class="px-5 py-4 text-sm text-navy">{{ boleto.pasajeroDisplay }}</td>
              <td class="px-5 py-4 text-sm text-navy">{{ boleto.reservaDisplay }}</td>
              <td class="px-5 py-4 text-sm text-navy">{{ boleto.numeroVuelo || '-' }}</td>
              <td class="px-5 py-4 text-sm text-navy">{{ boleto.asiento }}</td>
              <td class="px-5 py-4">
                <span
                  :class="[
                    'rounded-full px-3 py-1 text-xs font-semibold',
                    cantidadEquipajes(boleto.idBoleto) ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-600',
                  ]"
                >
                  {{ cantidadEquipajes(boleto.idBoleto) ? `${cantidadEquipajes(boleto.idBoleto)} registrado(s)` : 'Sin equipaje' }}
                </span>
              </td>
              <td class="px-5 py-4 text-sm text-navy">{{ money(boleto.cargoEquipaje) }}</td>
              <td class="px-5 py-4 text-sm text-navy">{{ formatearFecha(boleto.fechaVuelo) }}</td>
              <td class="px-5 py-4">
                <div class="flex justify-end gap-2 text-sm">
                  <button
                    type="button"
                    class="rounded-full border border-slate-200 bg-white px-3 py-1 text-navy shadow-sm transition hover:-translate-y-0.5 hover:bg-slate-50"
                    @click="abrirDetalle(boleto)"
                  >
                    Ver
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <div v-if="modalDetalle" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/60 px-4 backdrop-blur-sm" @click.self="modalDetalle = false">
      <div class="w-full max-w-5xl overflow-hidden rounded-[32px] bg-white shadow-2xl">
        <div class="bg-[linear-gradient(135deg,#12284A_0%,#1E436F_100%)] px-7 py-6 text-white">
          <p class="text-xs font-semibold uppercase tracking-[0.28em] text-white/65">Documento emitido</p>
          <h2 class="mt-2 text-3xl font-bold">Detalle de Boleto</h2>
          <p class="mt-2 max-w-3xl text-sm text-white/80">
            Visualiza la emision del pasajero, su asiento y el equipaje asociado con un formato mas claro.
          </p>
        </div>

        <div class="px-7 py-7">
          <div v-if="errorModal" class="mb-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {{ errorModal }}
          </div>
          <div v-else-if="cargandoDetalle" class="text-sm text-text-muted">Cargando detalle...</div>

          <div v-else-if="boletoDetalle" class="space-y-6">
            <div class="grid gap-4 md:grid-cols-4">
              <div class="rounded-[24px] border border-slate-200 bg-slate-50 px-4 py-4">
                <p class="text-xs font-semibold uppercase tracking-[0.14em] text-text-muted">Boleto</p>
                <p class="mt-3 text-2xl font-semibold text-navy">{{ boletoDetalle.numeroBoleto }}</p>
              </div>
              <div class="rounded-[24px] border border-slate-200 bg-slate-50 px-4 py-4">
                <p class="text-xs font-semibold uppercase tracking-[0.14em] text-text-muted">Reserva</p>
                <p class="mt-3 text-lg font-semibold text-navy">{{ boletoDetalle.reservaDisplay }}</p>
              </div>
              <div class="rounded-[24px] border border-slate-200 bg-slate-50 px-4 py-4">
                <p class="text-xs font-semibold uppercase tracking-[0.14em] text-text-muted">Asiento</p>
                <p class="mt-3 text-2xl font-semibold text-navy">{{ boletoDetalle.asiento }}</p>
              </div>
              <div class="rounded-[24px] border border-emerald-100 bg-emerald-50 px-4 py-4">
                <p class="text-xs font-semibold uppercase tracking-[0.14em] text-emerald-700">Cargo equipaje</p>
                <p class="mt-3 text-2xl font-semibold text-emerald-700">{{ money(boletoDetalle.cargoEquipaje) }}</p>
              </div>
            </div>

            <div class="grid gap-5 md:grid-cols-2">
              <div class="rounded-[26px] border border-slate-200 bg-[linear-gradient(180deg,#ffffff_0%,#f8fbff_100%)] p-5">
                <p class="text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Pasajero</p>
                <h3 class="mt-3 text-2xl font-semibold text-navy">{{ boletoDetalle.pasajeroDisplay }}</h3>
                <p class="mt-2 text-sm text-text-muted">{{ boletoDetalle.clienteDisplay }}</p>
              </div>
              <div class="rounded-[26px] border border-slate-200 bg-[linear-gradient(180deg,#ffffff_0%,#f8fbff_100%)] p-5">
                <p class="text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Vuelo</p>
                <h3 class="mt-3 text-2xl font-semibold text-navy">{{ boletoDetalle.numeroVuelo || '-' }}</h3>
                <p class="mt-2 text-sm text-text-muted">{{ formatearFecha(boletoDetalle.fechaVuelo) }}</p>
              </div>
            </div>

            <div class="rounded-[28px] border border-slate-200 bg-[linear-gradient(180deg,#f8fbff_0%,#ffffff_100%)] p-6">
              <div class="flex items-center justify-between gap-4">
                <div>
                  <p class="text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Equipaje asociado</p>
                  <h3 class="mt-2 text-2xl font-semibold text-navy">Resumen por pieza</h3>
                </div>
                <div class="rounded-full bg-navy px-4 py-2 text-sm font-semibold text-white">
                  {{ boletoDetalle.equipajes?.length || 0 }} pieza(s)
                </div>
              </div>

              <div v-if="boletoDetalle.equipajes?.length" class="mt-5 grid gap-4">
                <article
                  v-for="equipaje in boletoDetalle.equipajes"
                  :key="equipaje.idEquipaje"
                  class="rounded-[24px] border border-slate-200 bg-white px-5 py-5 shadow-sm"
                >
                  <div class="grid gap-4 md:grid-cols-5">
                    <div>
                      <p class="text-xs font-semibold uppercase tracking-[0.12em] text-text-muted">Etiqueta</p>
                      <p class="mt-2 text-lg font-semibold text-navy">{{ equipaje.etiqueta || 'Por asignar' }}</p>
                    </div>
                    <div>
                      <p class="text-xs font-semibold uppercase tracking-[0.12em] text-text-muted">Tipo</p>
                      <p class="mt-2 text-lg font-semibold text-navy">{{ equipaje.tipo || '-' }}</p>
                    </div>
                    <div>
                      <p class="text-xs font-semibold uppercase tracking-[0.12em] text-text-muted">Peso</p>
                      <p class="mt-2 text-lg font-semibold text-navy">{{ equipaje.pesoKg }} kg</p>
                    </div>
                    <div>
                      <p class="text-xs font-semibold uppercase tracking-[0.12em] text-text-muted">Precio extra</p>
                      <p class="mt-2 text-lg font-semibold text-navy">{{ money(equipaje.precioExtra) }}</p>
                    </div>
                    <div>
                      <p class="text-xs font-semibold uppercase tracking-[0.12em] text-text-muted">Estado</p>
                      <span class="mt-2 inline-flex rounded-full bg-emerald-100 px-3 py-1 text-sm font-semibold text-emerald-700">
                        {{ equipaje.estado || '-' }}
                      </span>
                    </div>
                  </div>
                </article>
              </div>
              <div v-else class="mt-5 rounded-[22px] border border-slate-200 bg-white px-5 py-5 text-sm text-text-muted">
                Este boleto no tiene equipaje asociado.
              </div>
            </div>
          </div>

          <div class="mt-8 flex justify-end gap-3">
            <button type="button" class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy transition hover:bg-slate-50" @click="modalDetalle = false">Cerrar</button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
