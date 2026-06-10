<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import {
  createAsientoVueloApi,
  deleteAsientoVueloApi,
  getAsientoVueloDetalleApi,
  getAsientosVueloApi,
  getVuelosApi,
  updateAsientoVueloApi,
} from '@/api/vuelos.api'
import { usePanelPermisos } from '@/composables/usePanelPermisos'
import { deepValue, extractItems } from '@/utils/portalCliente'

const route = useRoute()
const router = useRouter()
const { puedeEliminar } = usePanelPermisos()

const CLASES = ['ECONOMICA', 'EJECUTIVA', 'PRIMERA']
const POSICIONES = ['VENTANA', 'PASILLO', 'CENTRO']
const ESTADOS_NO_OPERABLES = ['CANCELADO', 'ATERRIZADO', 'INACTIVO', 'INA']

const cargando = ref(true)
const cargandoTabla = ref(false)
const cargandoDetalle = ref(false)
const guardando = ref(false)
const eliminando = ref(false)
const error = ref('')
const errorModal = ref('')
const busqueda = ref('')
const vueloFiltro = ref('')
const claseFiltro = ref('')
const posicionFiltro = ref('')
const disponibilidadFiltro = ref('')
const estadoFiltro = ref('')
const vuelos = ref([])
const asientos = ref([])
const modalAbierto = ref(false)
const modalDetalle = ref(false)
const modalEliminar = ref(false)
const modo = ref('crear')
const asientoActivo = ref(null)
const asientoDetalle = ref(null)
const errores = ref({})
const yaValidado = ref(false)

const form = ref({
  id_vuelo: '',
  numero_asiento: '',
  clase: '',
  disponible: 'true',
  precio_extra: '0',
  posicion: '',
})

const vueloSeleccionado = computed(() =>
  vuelos.value.find((vuelo) => String(vuelo.idVuelo) === String(vueloFiltro.value || '')) || null,
)

const vueloOperable = computed(() => {
  if (!vueloSeleccionado.value) return false
  return vueloSeleccionado.value.activo && !ESTADOS_NO_OPERABLES.includes(vueloSeleccionado.value.estadoVuelo)
})

const mensajeVuelo = computed(() => {
  if (!vueloSeleccionado.value) return 'Selecciona un vuelo para gestionar sus asientos.'
  if (!vueloSeleccionado.value.activo) return 'El vuelo seleccionado esta inactivo y no permite crear o editar asientos.'
  if (ESTADOS_NO_OPERABLES.includes(vueloSeleccionado.value.estadoVuelo)) {
    return `El vuelo esta en estado ${estadoVueloAmigable(vueloSeleccionado.value.estadoVuelo)} y no permite crear o editar asientos.`
  }
  return ''
})

const opcionesVuelo = computed(() =>
  vuelos.value
    .slice()
    .sort((a, b) => String(a.etiqueta).localeCompare(String(b.etiqueta), 'es'))
    .map((vuelo) => ({
      valor: String(vuelo.idVuelo),
      etiqueta: vuelo.etiqueta,
    })),
)

const opcionesClase = [
  { valor: '', etiqueta: 'Todas las clases' },
  ...CLASES.map((clase) => ({ valor: clase, etiqueta: claseAmigable(clase) })),
]

const opcionesClaseFormulario = CLASES.map((clase) => ({
  valor: clase,
  etiqueta: claseAmigable(clase),
}))

const opcionesPosicion = [
  { valor: '', etiqueta: 'Todas las posiciones' },
  ...POSICIONES.map((posicion) => ({ valor: posicion, etiqueta: posicionAmigable(posicion) })),
]

const opcionesPosicionFormulario = POSICIONES.map((posicion) => ({
  valor: posicion,
  etiqueta: posicionAmigable(posicion),
}))

const opcionesDisponibilidad = [
  { valor: '', etiqueta: 'Todas' },
  { valor: 'true', etiqueta: 'Disponible' },
  { valor: 'false', etiqueta: 'No disponible' },
]

const opcionesDisponibilidadFormulario = [
  { valor: 'true', etiqueta: 'Disponible' },
  { valor: 'false', etiqueta: 'No disponible' },
]

const opcionesEstado = [
  { valor: '', etiqueta: 'Todos los estados' },
  { valor: 'ACTIVO', etiqueta: 'Activo' },
  { valor: 'BLOQUEADO', etiqueta: 'Bloqueado' },
  { valor: 'RESERVADO', etiqueta: 'Reservado' },
  { valor: 'OCUPADO', etiqueta: 'Ocupado' },
]

const asientosFiltrados = computed(() => {
  const termino = busqueda.value.trim().toLowerCase()
  return asientos.value.filter((asiento) => {
    if (!termino) return true
    return [asiento.numeroAsiento, asiento.vueloCodigo, asiento.vueloRuta]
      .filter(Boolean)
      .some((valor) => String(valor).toLowerCase().includes(termino))
  })
})

function claseAmigable(valor) {
  if (valor === 'PRIMERA') return 'Primera'
  if (valor === 'EJECUTIVA') return 'Ejecutiva'
  if (valor === 'ECONOMICA') return 'Economica'
  return valor || '-'
}

function posicionAmigable(valor) {
  if (valor === 'VENTANA') return 'Ventana'
  if (valor === 'PASILLO') return 'Pasillo'
  if (valor === 'CENTRO') return 'Centro'
  return valor || '-'
}

function estadoVueloAmigable(valor) {
  return String(valor || '')
    .trim()
    .toLowerCase()
    .replace(/_/g, ' ')
    .replace(/\b\p{L}/gu, (letra) => letra.toUpperCase())
}

function money(valor) {
  return new Intl.NumberFormat('es-EC', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(Number(valor || 0))
}

function toBool(valor) {
  if (typeof valor === 'boolean') return valor
  if (typeof valor === 'string') return valor.trim().toLowerCase() === 'true'
  return Boolean(valor)
}

function estadoAmigable(valor) {
  const estado = String(valor || '').trim().toUpperCase()
  if (estado === 'ACTIVO') return 'Activo'
  if (estado === 'BLOQUEADO') return 'Bloqueado'
  if (estado === 'RESERVADO') return 'Reservado'
  if (estado === 'OCUPADO') return 'Ocupado'
  return estado || '-'
}

function badgeClase(clase) {
  if (clase === 'PRIMERA') return 'bg-amber-100 text-amber-800'
  if (clase === 'EJECUTIVA') return 'bg-sky-100 text-sky-800'
  return 'bg-slate-100 text-slate-700'
}

function badgeDisponibilidad(disponible) {
  return toBool(disponible) ? 'bg-emerald-100 text-emerald-700' : 'bg-rose-100 text-rose-700'
}

function badgeEstado(estado) {
  if (estado === 'RESERVADO' || estado === 'OCUPADO') return 'bg-orange-100 text-orange-700'
  if (estado === 'BLOQUEADO') return 'bg-slate-200 text-slate-700'
  return 'bg-navy/10 text-navy'
}

function normalizarVuelo(item) {
  const idVuelo = deepValue(item, ['idVuelo', 'id_vuelo', 'id']) || null
  const numeroVuelo = deepValue(item, ['numeroVuelo', 'numero_vuelo']) || ''
  const origen = deepValue(item, ['codigoOrigen', 'codigo_origen', 'origenNombre', 'origen']) || ''
  const destino = deepValue(item, ['codigoDestino', 'codigo_destino', 'destinoNombre', 'destino']) || ''
  const ruta = [origen, destino].filter(Boolean).join(' -> ')

  return {
    idVuelo,
    numeroVuelo,
    ruta,
    estadoVuelo: String(deepValue(item, ['estadoVuelo', 'estado_vuelo']) || '').trim().toUpperCase(),
    activo: deepValue(item, ['activo']) !== false,
    etiqueta: [numeroVuelo, ruta].filter(Boolean).join(' / ') || `Vuelo ${idVuelo}`,
  }
}

function normalizarAsiento(item, idVuelo) {
  const vuelo = vuelos.value.find((entry) => String(entry.idVuelo) === String(idVuelo))
  const disponible = toBool(deepValue(item, ['disponible']) ?? true)
  const estado = String(
    deepValue(item, ['estado', 'estadoAsiento', 'estado_asiento']) ||
      (disponible ? 'ACTIVO' : 'BLOQUEADO'),
  )
    .trim()
    .toUpperCase()

  return {
    idAsiento: deepValue(item, ['idAsiento', 'id_asiento', 'id']) || null,
    idVuelo,
    vueloCodigo: vuelo?.numeroVuelo || '',
    vueloRuta: vuelo?.ruta || '',
    numeroAsiento: deepValue(item, ['numeroAsiento', 'numero_asiento']) || '',
    clase: String(deepValue(item, ['clase']) || '').trim().toUpperCase() || 'ECONOMICA',
    posicion: String(deepValue(item, ['posicion']) || '').trim().toUpperCase(),
    precioExtra: Number(deepValue(item, ['precioExtra', 'precio_extra']) || 0),
    disponible,
    estado,
  }
}

function limpiarFormulario() {
  form.value = {
    id_vuelo: vueloFiltro.value || '',
    numero_asiento: '',
    clase: '',
    disponible: 'true',
    precio_extra: '0',
    posicion: '',
  }
  errores.value = {}
  errorModal.value = ''
  yaValidado.value = false
}

function abrirCrear() {
  if (!vueloFiltro.value) {
    error.value = 'Selecciona primero el vuelo que vas a administrar.'
    return
  }
  modo.value = 'crear'
  asientoActivo.value = null
  limpiarFormulario()
  modalAbierto.value = true
}

function abrirEditar(asiento) {
  if (!vueloOperable.value) return
  modo.value = 'editar'
  asientoActivo.value = asiento
  form.value = {
    id_vuelo: String(asiento.idVuelo || ''),
    numero_asiento: String(asiento.numeroAsiento || ''),
    clase: String(asiento.clase || ''),
    disponible: String(Boolean(asiento.disponible)),
    precio_extra: String(asiento.precioExtra ?? 0),
    posicion: String(asiento.posicion || ''),
  }
  errores.value = {}
  errorModal.value = ''
  yaValidado.value = false
  modalAbierto.value = true
}

async function abrirVer(asiento) {
  asientoActivo.value = asiento
  asientoDetalle.value = null
  cargandoDetalle.value = true
  errorModal.value = ''
  modalDetalle.value = true

  try {
    const respuesta = await getAsientoVueloDetalleApi(asiento.idVuelo, asiento.idAsiento)
    const data = deepValue(respuesta, ['data']) || respuesta
    asientoDetalle.value = normalizarAsiento(data, asiento.idVuelo)
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo cargar el detalle del asiento.'
  } finally {
    cargandoDetalle.value = false
  }
}

function abrirEliminar(asiento) {
  asientoActivo.value = asiento
  errorModal.value = ''
  modalEliminar.value = true
}

function validarFormulario() {
  const nuevosErrores = {}
  const numero = String(form.value.numero_asiento || '').trim().toUpperCase()
  const precio = Number(form.value.precio_extra)
  const posicion = String(form.value.posicion || '').trim().toUpperCase()

  if (!form.value.id_vuelo) nuevosErrores.id_vuelo = 'Selecciona el vuelo.'
  if (!numero) nuevosErrores.numero_asiento = 'Ingresa el numero del asiento.'
  if (numero.length > 10) nuevosErrores.numero_asiento = 'El numero no puede superar 10 caracteres.'
  if (!form.value.clase || !CLASES.includes(String(form.value.clase || '').trim().toUpperCase())) {
    nuevosErrores.clase = 'Selecciona una clase valida.'
  }
  if (form.value.precio_extra === '' || Number.isNaN(precio) || precio < 0) {
    nuevosErrores.precio_extra = 'El precio extra debe ser mayor o igual a 0.'
  }
  if (posicion && !POSICIONES.includes(posicion)) {
    nuevosErrores.posicion = 'Selecciona una posicion valida.'
  }

  errores.value = nuevosErrores
  return !Object.keys(nuevosErrores).length
}

async function cargarVuelos() {
  const respuestaVuelos = await getVuelosApi({ page: 1, page_size: 100 })
  vuelos.value = extractItems(respuestaVuelos).map(normalizarVuelo)
}

async function cargarAsientos() {
  if (!vueloFiltro.value) {
    asientos.value = []
    return
  }

  cargandoTabla.value = true
  error.value = ''

  try {
    const params = {
      page: 1,
      page_size: 100,
    }

    if (claseFiltro.value) params.clase = claseFiltro.value
    if (posicionFiltro.value) params.posicion = posicionFiltro.value
    if (disponibilidadFiltro.value) params.disponible = disponibilidadFiltro.value
    if (estadoFiltro.value) params.estado = estadoFiltro.value
    if (busqueda.value.trim()) params.numero_asiento = busqueda.value.trim().toUpperCase()

    const respuesta = await getAsientosVueloApi(Number(vueloFiltro.value), params)
    asientos.value = extractItems(respuesta).map((item) => normalizarAsiento(item, Number(vueloFiltro.value)))
  } catch (err) {
    error.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudieron cargar los asientos.'
  } finally {
    cargandoTabla.value = false
  }
}

async function cargarTodo() {
  cargando.value = true
  error.value = ''
  try {
    await cargarVuelos()
    await cargarAsientos()
  } catch (err) {
    error.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo cargar el modulo de asientos.'
  } finally {
    cargando.value = false
  }
}

async function guardarAsiento() {
  yaValidado.value = true
  if (guardando.value || !validarFormulario()) return
  if (!vueloOperable.value) {
    errorModal.value = mensajeVuelo.value || 'El vuelo no permite editar asientos.'
    return
  }

  guardando.value = true
  errorModal.value = ''

  const payload = {
    idVuelo: Number(form.value.id_vuelo),
    numeroAsiento: String(form.value.numero_asiento || '').trim().toUpperCase(),
    clase: String(form.value.clase || '').trim().toUpperCase(),
    disponible: toBool(form.value.disponible),
    precioExtra: Number(form.value.precio_extra || 0),
    posicion: String(form.value.posicion || '').trim().toUpperCase() || null,
  }

  try {
    if (modo.value === 'crear') {
      await createAsientoVueloApi(Number(form.value.id_vuelo), payload)
    } else {
      await updateAsientoVueloApi(Number(form.value.id_vuelo), asientoActivo.value.idAsiento, payload)
    }

    modalAbierto.value = false
    await cargarAsientos()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo guardar el asiento.'
  } finally {
    guardando.value = false
  }
}

async function cambiarDisponibilidad(asiento) {
  if (guardando.value || !vueloOperable.value) return
  guardando.value = true
  error.value = ''

  try {
    await updateAsientoVueloApi(asiento.idVuelo, asiento.idAsiento, {
      idVuelo: asiento.idVuelo,
      numeroAsiento: asiento.numeroAsiento,
      clase: asiento.clase,
      disponible: !asiento.disponible,
      precioExtra: asiento.precioExtra,
      posicion: asiento.posicion || null,
    })
    await cargarAsientos()
  } catch (err) {
    error.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo cambiar la disponibilidad del asiento.'
  } finally {
    guardando.value = false
  }
}

async function confirmarEliminar() {
  if (!asientoActivo.value?.idAsiento || eliminando.value) return
  eliminando.value = true
  errorModal.value = ''

  try {
    await deleteAsientoVueloApi(asientoActivo.value.idVuelo, asientoActivo.value.idAsiento)
    modalEliminar.value = false
    await cargarAsientos()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo eliminar el asiento.'
  } finally {
    eliminando.value = false
  }
}

function gestionarMapa() {
  if (!vueloFiltro.value) {
    error.value = 'Selecciona un vuelo para gestionar sus asientos.'
    return
  }
  error.value = ''
  cargarAsientos()
}

watch(
  () => vueloFiltro.value,
  async (nuevoVuelo) => {
    router.replace({ name: 'panel-asientos', query: nuevoVuelo ? { vueloId: String(nuevoVuelo) } : {} })
    if (!cargando.value) await cargarAsientos()
  },
)

watch(
  [claseFiltro, posicionFiltro, disponibilidadFiltro, estadoFiltro],
  async () => {
    if (!cargando.value && vueloFiltro.value) await cargarAsientos()
  },
)

watch(
  () => busqueda.value,
  async () => {
    if (!cargando.value && vueloFiltro.value) await cargarAsientos()
  },
)

watch(
  form,
  () => {
    if (yaValidado.value) validarFormulario()
  },
  { deep: true },
)

onMounted(async () => {
  if (route.query.vueloId) {
    vueloFiltro.value = String(route.query.vueloId)
  }
  await cargarTodo()
})
</script>

<template>
  <section class="space-y-5">
    <div class="flex items-start justify-between gap-4">
      <div>
        <h1 class="text-[2rem] font-bold text-navy">Gestion de Asientos</h1>
        <p class="mt-1.5 text-sm text-text-muted">
          Modulo operativo para consultar y administrar disponibilidad de asientos por vuelo.
        </p>
      </div>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <section class="grid gap-4 rounded-[24px] bg-white p-4 shadow-sm md:grid-cols-2 xl:grid-cols-[1.2fr_1fr_220px_220px_220px_220px]">
      <InputApp v-model="busqueda" placeholder="Buscar por asiento o vuelo..." />
      <SelectApp v-model="vueloFiltro" :opciones="opcionesVuelo" label="Vuelo" />
      <SelectApp v-model="claseFiltro" :opciones="opcionesClase" label="Clase" />
      <SelectApp v-model="posicionFiltro" :opciones="opcionesPosicion" label="Posicion" />
      <SelectApp v-model="disponibilidadFiltro" :opciones="opcionesDisponibilidad" label="Disponibilidad" />
      <SelectApp v-model="estadoFiltro" :opciones="opcionesEstado" label="Estado" />
    </section>

    <section class="rounded-[24px] bg-white p-5 shadow-sm">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 class="text-lg font-semibold text-navy">Vuelo seleccionado</h2>
          <p class="text-sm text-text-muted">
            {{ vueloSeleccionado?.etiqueta || 'Aun no seleccionas un vuelo.' }}
          </p>
        </div>
        <span
          v-if="vueloSeleccionado"
          class="rounded-full px-3 py-1 text-xs font-semibold"
          :class="vueloOperable ? 'bg-emerald-100 text-emerald-700' : 'bg-orange-100 text-orange-700'"
        >
          {{ vueloOperable ? 'Operable' : 'No operable' }}
        </span>
      </div>

      <p v-if="mensajeVuelo" class="mt-3 text-sm text-text-muted">
        {{ mensajeVuelo }}
      </p>
    </section>

    <section class="rounded-[24px] bg-white shadow-sm">
      <div class="overflow-x-auto">
        <table class="min-w-[1240px] w-full border-collapse">
          <thead class="bg-slate-50">
            <tr class="text-left text-sm font-semibold text-navy">
              <th class="px-5 py-3.5">Vuelo</th>
              <th class="px-5 py-3.5">Asiento</th>
              <th class="px-5 py-3.5">Clase</th>
              <th class="px-5 py-3.5">Posicion</th>
              <th class="px-5 py-3.5">Precio extra</th>
              <th class="px-5 py-3.5">Disponible</th>
              <th class="px-5 py-3.5">Estado</th>
              <th class="px-5 py-3.5">Ruta</th>
              <th class="px-5 py-3.5 text-right">Acciones</th>
            </tr>
          </thead>

          <tbody>
            <tr v-if="cargando || cargandoTabla">
              <td colspan="9" class="px-5 py-5 text-sm text-text-muted">Cargando asientos...</td>
            </tr>
            <tr v-else-if="!vueloFiltro">
              <td colspan="9" class="px-5 py-5 text-sm text-text-muted">Selecciona un vuelo para ver sus asientos.</td>
            </tr>
            <tr v-else-if="!asientosFiltrados.length">
              <td colspan="9" class="px-5 py-5 text-sm text-text-muted">No hay asientos registrados con los filtros actuales.</td>
            </tr>

            <tr
              v-for="asiento in asientosFiltrados"
              v-else
              :key="`${asiento.idVuelo}-${asiento.idAsiento}`"
              class="border-t border-slate-100 align-middle"
            >
              <td class="px-5 py-4 text-sm font-semibold text-navy">{{ asiento.vueloCodigo || '-' }}</td>
              <td class="px-5 py-4 text-sm text-navy">{{ asiento.numeroAsiento || '-' }}</td>
              <td class="px-5 py-4">
                <span :class="['rounded-full px-3 py-1 text-xs font-semibold', badgeClase(asiento.clase)]">
                  {{ claseAmigable(asiento.clase) }}
                </span>
              </td>
              <td class="px-5 py-4 text-sm text-navy">{{ posicionAmigable(asiento.posicion) }}</td>
              <td class="px-5 py-4 text-sm text-navy">{{ money(asiento.precioExtra) }}</td>
              <td class="px-5 py-4">
                <span :class="['rounded-full px-3 py-1 text-xs font-semibold', badgeDisponibilidad(asiento.disponible)]">
                  {{ asiento.disponible ? 'Disponible' : 'No disponible' }}
                </span>
              </td>
              <td class="px-5 py-4">
                <span :class="['rounded-full px-3 py-1 text-xs font-semibold', badgeEstado(asiento.estado)]">
                  {{ estadoAmigable(asiento.estado) }}
                </span>
              </td>
              <td class="px-5 py-4 text-sm text-navy">{{ asiento.vueloRuta || '-' }}</td>
              <td class="px-5 py-4">
                <div class="flex justify-end gap-2 text-sm">
                  <button type="button" class="rounded-full border border-slate-200 px-3 py-1 text-navy" @click="abrirVer(asiento)">Ver</button>
                  <button
                    type="button"
                    class="rounded-full border border-slate-200 px-3 py-1 text-navy disabled:opacity-50"
                    :disabled="!vueloOperable"
                    @click="abrirEditar(asiento)"
                  >
                    Editar
                  </button>
                  <button
                    type="button"
                    class="rounded-full border border-slate-200 px-3 py-1 text-navy disabled:opacity-50"
                    :disabled="!vueloOperable"
                    @click="cambiarDisponibilidad(asiento)"
                  >
                    {{ asiento.disponible ? 'Bloquear' : 'Habilitar' }}
                  </button>
                  <button v-if="puedeEliminar" type="button" class="rounded-full border border-red-200 px-3 py-1 text-red-600" @click="abrirEliminar(asiento)">Eliminar</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <div v-if="modalAbierto" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalAbierto = false">
      <div class="w-full max-w-4xl rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">{{ modo === 'crear' ? 'Nuevo Asiento' : 'Editar Asiento' }}</h2>

        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorModal }}
        </div>

        <div class="mt-6 grid gap-5 md:grid-cols-2">
          <SelectApp v-model="form.id_vuelo" label="Vuelo" :opciones="opcionesVuelo" :error="errores.id_vuelo" requerido />
          <InputApp v-model="form.numero_asiento" label="Numero" :error="errores.numero_asiento" requerido />
          <SelectApp v-model="form.clase" label="Clase" :opciones="opcionesClaseFormulario" :error="errores.clase" requerido />
          <SelectApp v-model="form.posicion" label="Posicion" :opciones="opcionesPosicionFormulario" :error="errores.posicion" />
          <InputApp v-model="form.precio_extra" tipo="number" label="Precio extra" :error="errores.precio_extra" requerido />
          <SelectApp v-model="form.disponible" label="Disponible" :opciones="opcionesDisponibilidadFormulario" />
        </div>

        <div class="mt-8 flex justify-end gap-3">
          <button type="button" class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50" @click="modalAbierto = false">Cancelar</button>
          <button type="button" class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy hover:bg-gold-light disabled:opacity-60" :disabled="guardando" @click="guardarAsiento">
            {{ guardando ? 'Guardando...' : 'Guardar' }}
          </button>
        </div>
      </div>
    </div>

    <div v-if="modalDetalle" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalDetalle = false">
      <div class="w-full max-w-3xl rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">Detalle del Asiento</h2>

        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorModal }}
        </div>

        <div v-else-if="cargandoDetalle" class="mt-6 text-sm text-text-muted">Cargando detalle...</div>

        <div v-else-if="asientoDetalle" class="mt-6 grid gap-5 md:grid-cols-2">
          <InputApp :model-value="asientoDetalle.vueloCodigo || '-'" label="Vuelo" deshabilitado />
          <InputApp :model-value="asientoDetalle.numeroAsiento" label="Asiento" deshabilitado />
          <InputApp :model-value="claseAmigable(asientoDetalle.clase)" label="Clase" deshabilitado />
          <InputApp :model-value="posicionAmigable(asientoDetalle.posicion)" label="Posicion" deshabilitado />
          <InputApp :model-value="money(asientoDetalle.precioExtra)" label="Precio extra" deshabilitado />
          <InputApp :model-value="asientoDetalle.disponible ? 'Disponible' : 'No disponible'" label="Disponibilidad" deshabilitado />
          <InputApp :model-value="estadoAmigable(asientoDetalle.estado)" label="Estado" deshabilitado />
          <InputApp :model-value="asientoDetalle.vueloRuta || '-'" label="Ruta" deshabilitado />
        </div>

        <div class="mt-8 flex justify-end gap-3">
          <button type="button" class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50" @click="modalDetalle = false">Cerrar</button>
        </div>
      </div>
    </div>

    <div v-if="modalEliminar" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalEliminar = false">
      <div class="w-full max-w-xl rounded-[28px] bg-white p-8 shadow-2xl">
        <div v-if="errorModal" class="mb-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorModal }}
        </div>

        <div class="flex items-start gap-4">
          <div class="flex h-14 w-14 items-center justify-center rounded-full bg-red-100 text-3xl text-red-500">!</div>
          <div>
            <h2 class="text-3xl font-bold text-navy">Eliminar asiento?</h2>
            <p class="mt-2 text-base text-text-muted">
              Se eliminara el asiento <strong class="text-navy">{{ asientoActivo?.numeroAsiento || '-' }}</strong>.
            </p>
          </div>
        </div>

        <div class="mt-8 flex gap-4">
          <button type="button" class="flex-1 rounded-2xl border border-slate-300 px-5 py-4 font-semibold text-navy hover:bg-slate-50" @click="modalEliminar = false">Cancelar</button>
          <button type="button" class="flex-1 rounded-2xl bg-red-500 px-5 py-4 font-semibold text-white hover:bg-red-600 disabled:opacity-60" :disabled="eliminando" @click="confirmarEliminar">
            {{ eliminando ? 'Eliminando...' : 'Eliminar' }}
          </button>
        </div>
      </div>
    </div>
  </section>
</template>
