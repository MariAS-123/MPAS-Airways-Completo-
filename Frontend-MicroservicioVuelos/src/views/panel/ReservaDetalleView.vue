<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import InputApp from '@/components/base/InputApp.vue'
import { getBoletosApi } from '@/api/boletos.api'
import { getClientesApi } from '@/api/clientes.api'
import { getEquipajesBoletoApi } from '@/api/equipaje.api'
import { getPasajerosApi } from '@/api/pasajeros.api'
import { getReservaApi, pagarReservaApi } from '@/api/reservas.api'
import { getAsientosVueloApi, getVuelosApi } from '@/api/vuelos.api'
import { deepValue, extractItems } from '@/utils/portalCliente'

const route = useRoute()
const router = useRouter()

const TIPO_EQUIPAJE = 'BODEGA'
const PESO_BODEGA = 23
const PRECIO_BODEGA = 45
const IVA_DEFECTO = 0.15
const CLAVE_EQUIPAJE_BACKOFFICE = 'mpas_backoffice_equipaje'

const cargando = ref(true)
const guardando = ref(false)
const error = ref('')
const errorModal = ref('')
const reserva = ref(null)
const clientes = ref([])
const pasajerosCatalogo = ref([])
const vuelos = ref([])
const asientosVuelo = ref([])
const boletos = ref([])
const equipajesPorBoleto = ref({})
const equipajePendiente = ref({})
const modalPago = ref(false)
const modalEquipaje = ref(false)

const formPago = ref({
  cargo_servicio: '0',
})

const cliente = computed(() =>
  clientes.value.find((item) => String(item.idCliente) === String(reserva.value?.idCliente || '')) || null,
)

const vuelo = computed(() =>
  vuelos.value.find((item) => String(item.idVuelo) === String(reserva.value?.idVuelo || '')) || null,
)

const clienteDisplay = computed(() =>
  [cliente.value?.nombreCompleto, cliente.value?.correo].filter(Boolean).join(' / ') || cliente.value?.correo || '-',
)

const vueloDisplay = computed(() => {
  const codigo = reserva.value?.numeroVuelo || vuelo.value?.numeroVuelo || ''
  const ruta = vuelo.value?.ruta || ''
  return [codigo, ruta].filter(Boolean).join(' / ') || '-'
})

const puedePagar = computed(() => ['PEN', 'CON'].includes(String(reserva.value?.estadoReserva || '').toUpperCase()))
const facturaGenerada = computed(() => Boolean(reserva.value?.factura) || ['EMI', 'FIN'].includes(String(reserva.value?.estadoReserva || '').toUpperCase()))
const cargoServicioNumero = computed(() => Number(formPago.value.cargo_servicio || 0))

const pasajeros = computed(() => {
  const base = obtenerPasajerosReserva()
  const mapaPorBoleto = new Map(boletos.value.map((boleto) => [String(boleto.idBoleto), boleto]))

  return base.map((item, index) => {
    const boleto = item.idBoleto ? mapaPorBoleto.get(String(item.idBoleto)) : null
    const equipajes = boleto ? equipajesPorBoleto.value[String(boleto.idBoleto)] || [] : []
    const pendiente = item.idDetalle ? equipajePendiente.value[String(item.idDetalle)] || null : null

    return {
      idPasajero: item.idPasajero || boleto?.idPasajero || null,
      idDetalle: item.idDetalle || boleto?.idDetalle || null,
      nombre: item.nombre || boleto?.pasajero || `Pasajero ${index + 1}`,
      asiento: item.asiento || boleto?.asiento || 'Pendiente',
      idBoleto: item.idBoleto || boleto?.idBoleto || null,
      numeroBoleto: item.numeroBoleto || boleto?.numeroBoleto || '',
      equipajes,
      tieneEquipajeReal: equipajes.length > 0,
      equipajePendiente: Boolean(pendiente?.activo),
      tieneEquipaje: equipajes.length > 0 || Boolean(pendiente?.activo),
      tieneBoleto: Boolean(item.idBoleto || boleto?.idBoleto),
    }
  })
})

const totalEquipajePendiente = computed(() =>
  pasajeros.value.reduce((acc, pasajero) => acc + (pasajero.tieneEquipajeReal ? 0 : pasajero.equipajePendiente ? PRECIO_BODEGA : 0), 0),
)

const tasaIva = computed(() => {
  const subtotalBase = Number(reserva.value?.subtotalReserva || 0)
  const ivaBase = Number(reserva.value?.valorIva || 0)
  if (subtotalBase > 0 && ivaBase >= 0) {
    const ratio = ivaBase / subtotalBase
    if (ratio > 0 && ratio < 1) return ratio
  }
  return IVA_DEFECTO
})

const subtotalVisible = computed(() => Number((Number(reserva.value?.subtotalReserva || 0) + totalEquipajePendiente.value).toFixed(2)))
const ivaVisible = computed(() => Number((subtotalVisible.value * tasaIva.value).toFixed(2)))
const totalVisible = computed(() => Number((subtotalVisible.value + ivaVisible.value).toFixed(2)))
const totalPagarEstimado = computed(() => Number((totalVisible.value + cargoServicioNumero.value).toFixed(2)))
const boletosEmitidos = computed(() => pasajeros.value.some((pasajero) => pasajero.tieneBoleto))
const equipajeRegistrado = computed(() => pasajeros.value.some((pasajero) => pasajero.tieneEquipajeReal))

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
  if (valor === 'PEN') return 'Pendiente'
  if (valor === 'CON') return 'Confirmada'
  if (valor === 'CAN') return 'Cancelada'
  if (valor === 'FIN') return 'Finalizada'
  if (valor === 'EMI') return 'Emitida'
  return valor || '-'
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
    nombreCompleto: [nombre, apellido].filter(Boolean).join(' ').trim(),
  }
}

function normalizarVuelo(item) {
  return {
    idVuelo: deepValue(item, ['idVuelo', 'id_vuelo', 'id']) || null,
    numeroVuelo: deepValue(item, ['numeroVuelo', 'numero_vuelo']) || '',
    fechaInicio: deepValue(item, ['fechaHoraSalida', 'fecha_hora_salida', 'fechaInicio', 'fecha_inicio']) || '',
    fechaFin: deepValue(item, ['fechaHoraLlegada', 'fecha_hora_llegada', 'fechaFin', 'fecha_fin']) || '',
    ruta: [
      deepValue(item, ['codigoOrigen', 'codigo_origen', 'origenNombre', 'origen']),
      deepValue(item, ['codigoDestino', 'codigo_destino', 'destinoNombre', 'destino']),
    ].filter(Boolean).join(' -> '),
  }
}

function normalizarAsiento(item) {
  return {
    idAsiento: deepValue(item, ['idAsiento', 'id_asiento', 'id']) || null,
    numeroAsiento: deepValue(item, ['numeroAsiento', 'numero_asiento']) || '',
  }
}

function normalizarReserva(item) {
  return {
    idReserva: deepValue(item, ['idReserva', 'id_reserva', 'id']) || null,
    codigoReserva: deepValue(item, ['codigoReserva', 'codigo_reserva']) || '',
    idCliente: deepValue(item, ['idCliente', 'id_cliente']) || null,
    idVuelo: deepValue(item, ['idVuelo', 'id_vuelo']) || null,
    numeroVuelo: deepValue(item, ['numeroVuelo', 'numero_vuelo']) || '',
    fechaReserva:
      deepValue(item, ['fechaReserva', 'fecha_reserva', 'createdAt', 'created_at', 'fechaInicio', 'fecha_inicio']) || '',
    fechaInicio: deepValue(item, ['fechaInicio', 'fecha_inicio']) || '',
    fechaFin: deepValue(item, ['fechaFin', 'fecha_fin']) || '',
    subtotalReserva: Number(deepValue(item, ['subtotalReserva', 'subtotal_reserva']) || 0),
    valorIva: Number(deepValue(item, ['valorIva', 'valor_iva']) || 0),
    totalReserva: Number(deepValue(item, ['totalReserva', 'total_reserva', 'total']) || 0),
    estadoReserva: deepValue(item, ['estadoReserva', 'estado_reserva']) || '',
    factura: deepValue(item, ['factura']) || null,
    pasajerosRaw:
      deepValue(item, ['pasajeros']) ||
      deepValue(item, ['detalles']) ||
      deepValue(item, ['reservaDetalles']) ||
      deepValue(item, ['reserva_detalles']) ||
      [],
  }
}

function normalizarBoleto(item) {
  return {
    idBoleto: deepValue(item, ['idBoleto', 'id_boleto', 'id']) || null,
    idReserva: deepValue(item, ['idReserva', 'id_reserva']) || null,
    idDetalle: deepValue(item, ['idDetalle', 'id_detalle']) || null,
    idPasajero: deepValue(item, ['idPasajero', 'id_pasajero']) || null,
    numeroBoleto:
      deepValue(item, ['numeroBoleto', 'numero_boleto', 'codigoBoleto']) ||
      `BO-${deepValue(item, ['idBoleto', 'id_boleto', 'id']) || '001'}`,
    asiento: deepValue(item, ['numeroAsiento', 'numero_asiento', 'asiento']) || '',
    pasajero:
      [
        deepValue(item, ['nombrePasajero', 'nombre_pasajero']),
        deepValue(item, ['apellidoPasajero', 'apellido_pasajero']),
      ]
        .filter(Boolean)
        .join(' ')
        .trim(),
  }
}

function normalizarEquipaje(item) {
  return {
    idEquipaje: deepValue(item, ['idEquipaje', 'id_equipaje', 'id']) || null,
    etiqueta: deepValue(item, ['etiqueta']) || '',
    tipo: deepValue(item, ['tipoEquipaje', 'tipo_equipaje', 'tipo']) || TIPO_EQUIPAJE,
    peso: Number(deepValue(item, ['pesoKg', 'peso_kg', 'peso']) || 0),
    precioExtra: Number(deepValue(item, ['precioExtra', 'precio_extra']) || PRECIO_BODEGA),
    estado: deepValue(item, ['estadoEquipaje', 'estado_equipaje', 'estado']) || 'REGISTRADO',
  }
}

function obtenerPasajerosReserva() {
  const items = Array.isArray(reserva.value?.pasajerosRaw) ? reserva.value.pasajerosRaw : []
  if (items.length) {
    return items.map((item, index) => {
      const idPasajero = deepValue(item, ['idPasajero', 'id_pasajero']) || null
      const catalogo = pasajerosCatalogo.value.find((entry) => String(entry.idPasajero) === String(idPasajero || ''))
      return {
        idPasajero,
        idDetalle: deepValue(item, ['idDetalle', 'id_detalle', 'id']) || null,
        nombre:
          [deepValue(item, ['nombrePasajero', 'nombre_pasajero']), deepValue(item, ['apellidoPasajero', 'apellido_pasajero'])]
            .filter(Boolean)
            .join(' ')
            .trim() ||
          catalogo?.nombreCompleto ||
          `Pasajero ${index + 1}`,
        asiento:
          deepValue(item, ['numeroAsiento', 'numero_asiento', 'asiento']) ||
          asientosVuelo.value.find((asiento) => String(asiento.idAsiento) === String(deepValue(item, ['idAsiento', 'id_asiento']) || ''))?.numeroAsiento ||
          '',
        idBoleto: deepValue(item, ['idBoleto', 'id_boleto']) || null,
        numeroBoleto: deepValue(item, ['numeroBoleto', 'numero_boleto']) || '',
      }
    })
  }

  return boletos.value.map((boleto, index) => ({
    idPasajero: boleto.idPasajero || null,
    idDetalle: boleto.idDetalle || null,
    nombre: boleto.pasajero || `Pasajero ${index + 1}`,
    asiento: boleto.asiento || '',
    idBoleto: boleto.idBoleto || null,
    numeroBoleto: boleto.numeroBoleto || '',
  }))
}

function claveStorageEquipaje(idReserva) {
  return `${CLAVE_EQUIPAJE_BACKOFFICE}:${idReserva}`
}

function cargarEquipajePendiente() {
  const idReserva = reserva.value?.idReserva
  if (!idReserva) {
    equipajePendiente.value = {}
    return
  }

  try {
    const raw = JSON.parse(localStorage.getItem(claveStorageEquipaje(idReserva)) || '{}')
    equipajePendiente.value = raw && typeof raw === 'object' ? raw : {}
  } catch {
    equipajePendiente.value = {}
  }
}

function guardarEquipajePendiente() {
  const idReserva = reserva.value?.idReserva
  if (!idReserva) return
  localStorage.setItem(claveStorageEquipaje(idReserva), JSON.stringify(equipajePendiente.value))
}

function limpiarEquipajePendiente() {
  const idReserva = reserva.value?.idReserva
  if (idReserva) localStorage.removeItem(claveStorageEquipaje(idReserva))
  equipajePendiente.value = {}
}

function asegurarDetalleEquipaje(idDetalle) {
  const clave = String(idDetalle || '')
  if (!clave) return null
  if (!equipajePendiente.value[clave]) {
    equipajePendiente.value[clave] = {
      activo: false,
      descripcion: '',
      dimensiones: '',
    }
  }
  return equipajePendiente.value[clave]
}

function valorPendiente(idDetalle, campo) {
  const detalle = equipajePendiente.value[String(idDetalle || '')]
  return detalle?.[campo] || ''
}

function alternarEquipajePendiente(pasajero) {
  if (!pasajero?.idDetalle || !puedePagar.value || pasajero.tieneEquipajeReal) return
  const detalle = asegurarDetalleEquipaje(pasajero.idDetalle)
  if (!detalle) return
  detalle.activo = !detalle.activo
  if (!detalle.activo) {
    detalle.descripcion = ''
    detalle.dimensiones = ''
  }
  guardarEquipajePendiente()
}

function actualizarCampoEquipaje(idDetalle, campo, valor) {
  const detalle = asegurarDetalleEquipaje(idDetalle)
  if (!detalle) return
  detalle[campo] = String(valor || '')
  detalle.activo = true
  guardarEquipajePendiente()
}

function construirPayloadEquipajePago() {
  return pasajeros.value
    .filter((pasajero) => pasajero.idDetalle && pasajero.equipajePendiente && !pasajero.tieneEquipajeReal)
    .map((pasajero) => ({
      id_detalle: pasajero.idDetalle,
      tipo: TIPO_EQUIPAJE,
      peso_kg: PESO_BODEGA,
      descripcion_equipaje: valorPendiente(pasajero.idDetalle, 'descripcion') || 'Equipaje de bodega adicional',
      dimensiones: valorPendiente(pasajero.idDetalle, 'dimensiones') || undefined,
    }))
}

async function cargarEquipajesBoletos() {
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

async function cargarAsientosReserva() {
  if (!reserva.value?.idVuelo) {
    asientosVuelo.value = []
    return
  }

  try {
    const respuesta = await getAsientosVueloApi(reserva.value.idVuelo, { page: 1, page_size: 100 })
    asientosVuelo.value = extractItems(respuesta).map(normalizarAsiento)
  } catch {
    asientosVuelo.value = []
  }
}

async function cargarTodo() {
  cargando.value = true
  error.value = ''
  try {
    const [respuestaReserva, respuestaClientes, respuestaPasajeros, respuestaVuelos, respuestaBoletos] = await Promise.all([
      getReservaApi(route.params.id),
      getClientesApi({ page: 1, page_size: 200 }),
      getPasajerosApi({ page: 1, page_size: 200 }),
      getVuelosApi({ page: 1, page_size: 200 }),
      getBoletosApi({ page: 1, page_size: 200 }),
    ])

    reserva.value = normalizarReserva(deepValue(respuestaReserva, ['data']) || respuestaReserva)
    cargarEquipajePendiente()
    clientes.value = extractItems(respuestaClientes).map(normalizarCliente)
    pasajerosCatalogo.value = extractItems(respuestaPasajeros).map(normalizarPasajero)
    vuelos.value = extractItems(respuestaVuelos).map(normalizarVuelo)
    boletos.value = extractItems(respuestaBoletos)
      .map(normalizarBoleto)
      .filter((boleto) => String(boleto.idReserva || '') === String(reserva.value?.idReserva || ''))

    await cargarAsientosReserva()
    await cargarEquipajesBoletos()
  } catch (err) {
    error.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo cargar el detalle de la reserva.'
  } finally {
    cargando.value = false
  }
}

function abrirEquipaje() {
  errorModal.value = ''
  modalEquipaje.value = true
}

function irAFacturas() {
  router.push({ name: 'panel-facturas', query: { buscar: reserva.value?.codigoReserva || '' } })
}

async function procesarPago() {
  if (guardando.value || !reserva.value?.idReserva) return
  guardando.value = true
  errorModal.value = ''

  try {
    await pagarReservaApi(reserva.value.idReserva, {
      cargo_servicio: cargoServicioNumero.value,
      equipaje: construirPayloadEquipajePago(),
    })
    modalPago.value = false
    limpiarEquipajePendiente()
    await cargarTodo()
    irAFacturas()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo procesar el pago de la reserva.'
  } finally {
    guardando.value = false
  }
}

onMounted(cargarTodo)
</script>

<template>
  <section class="space-y-5">
    <div class="flex items-start justify-between gap-4">
      <div>
        <button
          type="button"
          class="mb-3 rounded-full border border-slate-200 px-3 py-1 text-xs font-medium text-navy transition hover:border-navy hover:bg-slate-50"
          @click="router.push({ name: 'panel-reservas' })"
        >
          Volver a reservas
        </button>
        <h1 class="text-[2rem] font-bold text-navy">Detalle de Reserva</h1>
      </div>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <section v-else-if="cargando" class="rounded-[24px] bg-white p-6 text-sm text-text-muted shadow-sm">
      Cargando detalle...
    </section>

    <template v-else-if="reserva">
      <section class="rounded-[24px] bg-white p-6 shadow-sm">
        <div class="grid gap-5 md:grid-cols-4">
          <InputApp :model-value="reserva.codigoReserva || '-'" label="Codigo reserva" deshabilitado />
          <InputApp :model-value="estadoAmigable(reserva.estadoReserva)" label="Estado" deshabilitado />
          <InputApp :model-value="formatearFecha(reserva.fechaReserva)" label="Fecha reserva" deshabilitado />
          <InputApp :model-value="money(totalVisible)" label="Total" deshabilitado />
        </div>
      </section>

      <section class="rounded-[24px] bg-white p-6 shadow-sm">
        <div class="flex flex-wrap gap-3">
          <button
            type="button"
            class="rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-navy shadow-sm transition hover:-translate-y-0.5 hover:bg-slate-50"
            @click="abrirEquipaje"
          >
            Registrar equipaje
          </button>
          <button
            type="button"
            class="rounded-2xl bg-gold px-4 py-2.5 text-sm font-semibold text-navy shadow-sm transition hover:-translate-y-0.5 hover:bg-gold-light disabled:opacity-60"
            :disabled="!puedePagar"
            @click="modalPago = true"
          >
            Procesar pago
          </button>
          <button
            type="button"
            class="rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-navy shadow-sm transition hover:-translate-y-0.5 hover:bg-slate-50"
            @click="irAFacturas"
          >
            Ver factura
          </button>
        </div>
      </section>

      <section class="grid gap-5 lg:grid-cols-2">
        <div class="rounded-[24px] bg-white p-6 shadow-sm">
          <h2 class="text-2xl font-semibold text-navy">Vuelo</h2>
          <div class="mt-5 grid gap-4 md:grid-cols-2">
            <InputApp :model-value="vueloDisplay" label="Vuelo" deshabilitado />
            <InputApp :model-value="formatearFecha(reserva.fechaInicio || vuelo?.fechaInicio)" label="Salida" deshabilitado />
            <InputApp :model-value="formatearFecha(reserva.fechaFin || vuelo?.fechaFin)" label="Llegada" deshabilitado />
            <InputApp :model-value="pasajeros.some((item) => item.asiento && item.asiento !== 'Pendiente') ? 'Asignados' : 'Pendiente'" label="Asientos" deshabilitado />
          </div>
        </div>

        <div class="rounded-[24px] bg-white p-6 shadow-sm">
          <h2 class="text-2xl font-semibold text-navy">Cliente</h2>
          <div class="mt-5 grid gap-4 md:grid-cols-2">
            <InputApp :model-value="clienteDisplay" label="Cliente" deshabilitado />
            <InputApp :model-value="cliente?.correo || '-'" label="Correo" deshabilitado />
            <InputApp :model-value="cliente?.telefono || '-'" label="Telefono" deshabilitado />
            <InputApp :model-value="estadoAmigable(reserva.estadoReserva)" label="Estado actual" deshabilitado />
          </div>
        </div>
      </section>

      <section class="rounded-[24px] bg-white p-6 shadow-sm">
        <h2 class="text-2xl font-semibold text-navy">Pasajeros</h2>
        <div v-if="pasajeros.length" class="mt-5 grid gap-4">
          <article
            v-for="(pasajero, index) in pasajeros"
            :key="`${pasajero.idDetalle || pasajero.nombre}-${index}`"
            class="rounded-2xl border border-slate-200 bg-slate-50 p-4"
          >
            <div class="grid gap-4 md:grid-cols-4">
              <InputApp :model-value="pasajero.nombre" :label="`Pasajero ${index + 1}`" deshabilitado />
              <InputApp :model-value="pasajero.asiento || 'Pendiente'" label="Asiento" deshabilitado />
              <InputApp :model-value="pasajero.numeroBoleto || 'Pendiente'" label="Boleto" deshabilitado />
              <InputApp
                :model-value="pasajero.tieneEquipajeReal ? '1 bodega registrada' : pasajero.equipajePendiente ? '1 bodega por cobrar' : 'Pendiente'"
                label="Equipaje"
                deshabilitado
              />
            </div>
          </article>
        </div>
        <div v-else class="mt-5 text-sm text-text-muted">No hay pasajeros asociados.</div>
      </section>

      <section class="rounded-[24px] bg-white p-6 shadow-sm">
        <h2 class="text-2xl font-semibold text-navy">Operacion</h2>
        <div class="mt-5 grid gap-4 md:grid-cols-4">
          <InputApp :model-value="pasajeros.some((item) => item.asiento && item.asiento !== 'Pendiente') ? 'Asignados' : 'Pendiente'" label="Asientos" deshabilitado />
          <InputApp :model-value="facturaGenerada ? 'Generada' : 'Pendiente'" label="Factura" deshabilitado />
          <InputApp :model-value="equipajeRegistrado ? 'Registrado' : totalEquipajePendiente ? `Pendiente ${money(totalEquipajePendiente)}` : 'Pendiente'" label="Equipaje" deshabilitado />
          <InputApp :model-value="boletosEmitidos ? 'Emitidos' : 'Pendiente'" label="Boletos" deshabilitado />
        </div>
      </section>
    </template>

    <div v-if="modalPago" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/60 px-4 backdrop-blur-sm" @click.self="modalPago = false">
      <div class="w-full max-w-3xl overflow-hidden rounded-[32px] bg-white shadow-2xl">
        <div class="bg-[linear-gradient(135deg,#12284A_0%,#1C3D69_100%)] px-7 py-6 text-white">
          <p class="text-xs font-semibold uppercase tracking-[0.28em] text-white/65">Cierre comercial</p>
          <h2 class="mt-2 text-3xl font-bold">Procesar Pago</h2>
          <p class="mt-2 max-w-2xl text-sm text-white/80">
            Confirma el cobro final de la reserva con sus extras antes de emitir la documentacion del viaje.
          </p>
        </div>

        <div class="px-7 py-7">
          <div v-if="errorModal" class="mb-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {{ errorModal }}
          </div>

          <div class="grid gap-5">
            <InputApp v-model="formPago.cargo_servicio" tipo="number" label="Cargo servicio" />

            <div class="rounded-[24px] border border-blue-100 bg-blue-50 px-5 py-4 text-sm text-blue-900">
              El sistema generara el cobro completo con factura, boletos y equipajes asociados en un solo paso.
            </div>

            <div class="grid gap-4 md:grid-cols-4">
              <div class="rounded-[24px] border border-slate-200 bg-slate-50 px-4 py-4">
                <p class="text-xs font-semibold uppercase tracking-[0.14em] text-text-muted">Subtotal base</p>
                <p class="mt-3 text-2xl font-semibold text-navy">{{ money(Number(reserva?.subtotalReserva || 0)) }}</p>
              </div>
              <div class="rounded-[24px] border border-emerald-100 bg-emerald-50 px-4 py-4">
                <p class="text-xs font-semibold uppercase tracking-[0.14em] text-emerald-700">Extras equipaje</p>
                <p class="mt-3 text-2xl font-semibold text-emerald-700">{{ money(totalEquipajePendiente) }}</p>
              </div>
              <div class="rounded-[24px] border border-slate-200 bg-slate-50 px-4 py-4">
                <p class="text-xs font-semibold uppercase tracking-[0.14em] text-text-muted">IVA estimado</p>
                <p class="mt-3 text-2xl font-semibold text-navy">{{ money(ivaVisible) }}</p>
              </div>
              <div class="rounded-[24px] border border-gold/40 bg-gold/10 px-4 py-4">
                <p class="text-xs font-semibold uppercase tracking-[0.14em] text-navy/70">Total estimado</p>
                <p class="mt-3 text-2xl font-semibold text-navy">{{ money(totalPagarEstimado) }}</p>
              </div>
            </div>
          </div>

          <div class="mt-8 flex justify-end gap-3">
            <button type="button" class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy transition hover:bg-slate-50" @click="modalPago = false">Cancelar</button>
            <button type="button" class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy shadow-sm transition hover:-translate-y-0.5 hover:bg-gold-light disabled:opacity-60" :disabled="guardando" @click="procesarPago">
              {{ guardando ? 'Procesando...' : 'Pagar y generar factura' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <div v-if="modalEquipaje" class="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-950/60 px-4 py-8 backdrop-blur-sm" @click.self="modalEquipaje = false">
      <div class="w-full max-w-[900px] overflow-hidden rounded-[28px] bg-white shadow-2xl">
        <div class="bg-[linear-gradient(135deg,#0E7C66_0%,#0A596B_100%)] px-5 py-4 text-white">
          <p class="text-xs font-semibold uppercase tracking-[0.28em] text-white/65">Extra por pasajero</p>
          <h2 class="mt-2 text-[1.8rem] font-bold">Registrar equipaje</h2>
          <p class="mt-2 max-w-xl text-sm text-white/80">
            Activa solo los extras que realmente se cobraran para que el total final de la reserva quede correcto.
          </p>
        </div>

        <div class="max-h-[calc(100vh-150px)] overflow-y-auto px-5 py-5">
          <div v-if="errorModal" class="mb-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {{ errorModal }}
          </div>

          <div class="grid gap-3 md:grid-cols-[1.1fr_0.9fr]">
            <div class="rounded-[20px] border border-slate-200 bg-slate-50 px-4 py-4">
              <p class="text-xs font-semibold uppercase tracking-[0.16em] text-text-muted">Politica vigente</p>
              <p class="mt-2 text-base font-semibold text-navy">1 equipaje de bodega por pasajero</p>
              <p class="mt-2 text-sm text-text-muted">
                Peso fijo de {{ PESO_BODEGA }} kg y valor fijo de {{ money(PRECIO_BODEGA) }} por cada extra activado.
              </p>
            </div>
            <div class="rounded-[20px] border border-emerald-100 bg-emerald-50 px-4 py-4">
              <p class="text-xs font-semibold uppercase tracking-[0.16em] text-emerald-700">Impacto en cobro</p>
              <p class="mt-2 text-base font-semibold text-emerald-700">Se suma al total final</p>
              <p class="mt-2 text-sm text-emerald-800/80">
                Lo que marques aqui se incorporara automaticamente a la factura cuando proceses el pago.
              </p>
            </div>
          </div>

          <div class="mt-6 grid gap-4">
            <article
              v-for="(pasajero, index) in pasajeros"
              :key="`equipaje-${pasajero.idDetalle || pasajero.idBoleto || index}`"
              class="rounded-[22px] border border-slate-200 bg-[linear-gradient(180deg,#ffffff_0%,#f8fbff_100%)] p-4 shadow-sm"
            >
              <div class="mb-3 flex items-center justify-between gap-4">
                <div>
                  <p class="text-xs font-semibold uppercase tracking-[0.16em] text-text-muted">Pasajero {{ index + 1 }}</p>
                  <h3 class="mt-1 text-[1.05rem] font-semibold text-navy">{{ pasajero.nombre }}</h3>
                </div>
                <span
                  :class="[
                    'rounded-full px-3 py-1 text-xs font-semibold',
                    pasajero.tieneEquipajeReal
                      ? 'bg-emerald-100 text-emerald-700'
                      : pasajero.equipajePendiente
                        ? 'bg-gold/15 text-navy'
                        : 'bg-slate-100 text-slate-600',
                  ]"
                >
                  {{ pasajero.tieneEquipajeReal ? 'Ya registrado' : pasajero.equipajePendiente ? 'Pendiente de cobro' : 'Sin extra' }}
                </span>
              </div>

              <div class="grid gap-3 md:grid-cols-[0.75fr_0.85fr_0.85fr_140px] md:items-end">
                <InputApp :model-value="pasajero.asiento || 'Pendiente'" label="Asiento" deshabilitado />
                <InputApp :model-value="pasajero.numeroBoleto || 'Se emitira con el pago'" label="Boleto" deshabilitado />
                <InputApp :model-value="pasajero.tieneEquipajeReal ? 'Incluido en emision' : pasajero.equipajePendiente ? money(PRECIO_BODEGA) : 'Sin extra'" label="Equipaje bodega" deshabilitado />
                <button
                  type="button"
                  class="rounded-2xl border border-slate-300 bg-white px-4 py-3 text-sm font-semibold text-navy shadow-sm transition hover:-translate-y-0.5 hover:bg-slate-50 disabled:opacity-50"
                  :disabled="!pasajero.idDetalle || !puedePagar || pasajero.tieneEquipajeReal"
                  @click="alternarEquipajePendiente(pasajero)"
                >
                  {{ pasajero.tieneEquipajeReal ? 'Registrado' : pasajero.equipajePendiente ? 'Quitar' : 'Agregar' }}
                </button>
              </div>

              <div v-if="pasajero.equipajePendiente && !pasajero.tieneEquipajeReal" class="mt-4 grid gap-4 md:grid-cols-2">
                <InputApp
                  :model-value="valorPendiente(pasajero.idDetalle, 'descripcion')"
                  label="Descripcion"
                  @update:model-value="actualizarCampoEquipaje(pasajero.idDetalle, 'descripcion', $event)"
                />
                <InputApp
                  :model-value="valorPendiente(pasajero.idDetalle, 'dimensiones')"
                  label="Dimensiones"
                  @update:model-value="actualizarCampoEquipaje(pasajero.idDetalle, 'dimensiones', $event)"
                />
              </div>
            </article>
          </div>

          <div class="mt-5 rounded-[20px] border border-emerald-200 bg-[linear-gradient(135deg,#ecfdf5_0%,#f0fdf9_100%)] px-4 py-4">
            <p class="text-xs font-semibold uppercase tracking-[0.16em] text-emerald-700">Resumen del extra</p>
            <div class="mt-3 flex flex-wrap items-end justify-between gap-4">
              <div>
                <p class="text-sm text-emerald-800/80">Valor pendiente actual</p>
                <p class="mt-1 text-[1.8rem] font-semibold text-emerald-700">{{ money(totalEquipajePendiente) }}</p>
              </div>
              <p class="max-w-xl text-sm leading-6 text-emerald-900/75">
                Este valor se incorporara automaticamente al cobro total cuando confirmes el pago de la reserva.
              </p>
            </div>
          </div>

          <div class="mt-8 flex justify-end gap-3">
            <button type="button" class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy transition hover:bg-slate-50" @click="modalEquipaje = false">Cerrar</button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
