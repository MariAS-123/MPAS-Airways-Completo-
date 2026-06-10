<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import { getAeropuertosAdminApi } from '@/api/aeropuertos.api'
import { createEscalaVueloApi, deleteEscalaVueloApi, getEscalasVueloApi, getVueloDetalleApi } from '@/api/vuelos.api'
import { usePanelPermisos } from '@/composables/usePanelPermisos'
import { deepValue, extractItems } from '@/utils/portalCliente'

const route = useRoute()
const router = useRouter()
const { puedeEliminar } = usePanelPermisos()

const cargando = ref(true)
const error = ref('')
const vuelo = ref(null)
const escalas = ref([])
const aeropuertos = ref([])
const errorEscalas = ref('')
const modalAbierto = ref(false)
const guardando = ref(false)
const modo = ref('crear')
const escalaActiva = ref(null)
const errorModal = ref('')
const errores = ref({})
const escalaAEliminar = ref(null)
const eliminandoEscala = ref(false)

const form = ref({
  id_aeropuerto: '',
  orden: '',
  fecha_hora_llegada: '',
  fecha_hora_salida: '',
  tipo_escala: '',
  terminal: '',
  puerta: '',
  observaciones: '',
})

const idVuelo = computed(() => route.params.id)

const escalasOrdenadas = computed(() =>
  [...escalas.value].sort((a, b) => Number(a.orden || 0) - Number(b.orden || 0)),
)

const opcionesAeropuertos = computed(() =>
  aeropuertos.value
    .map((item) => ({
      valor: String(item.idAeropuerto),
      etiqueta: `${item.codigoIata} - ${item.nombre}`,
    }))
    .sort((a, b) => a.etiqueta.localeCompare(b.etiqueta, 'es')),
)

const opcionesTipoEscala = computed(() => [
  { valor: 'TECNICA', etiqueta: 'Tecnica' },
  { valor: 'COMERCIAL', etiqueta: 'Comercial' },
  { valor: 'OPERATIVA', etiqueta: 'Operativa' },
])

const vueloBloqueado = computed(() =>
  ['EN_VUELO', 'ATERRIZADO', 'CANCELADO'].includes(String(vuelo.value?.estadoVuelo || '')),
)

const puedeGuardar = computed(
  () => !guardando.value && modalAbierto.value && !vueloBloqueado.value && Object.keys(errores.value).length === 0,
)

function normalizarAeropuerto(item) {
  return {
    idAeropuerto: deepValue(item, ['idAeropuerto', 'id_aeropuerto', 'id']) || null,
    codigoIata: deepValue(item, ['codigoIata', 'codigo_iata']) || '',
    nombre: deepValue(item, ['nombre']) || '',
    nombreCiudad: deepValue(item, ['nombreCiudad', 'nombre_ciudad', 'ciudadNombre']) || '',
    nombrePais: deepValue(item, ['nombrePais', 'nombre_pais', 'paisNombre']) || '',
  }
}

function resolverAeropuerto(idAeropuerto) {
  return aeropuertos.value.find((item) => String(item.idAeropuerto) === String(idAeropuerto))
}

function normalizarVuelo(item) {
  const idOrigen = deepValue(item, ['idAeropuertoOrigen', 'id_aeropuerto_origen']) || null
  const idDestino = deepValue(item, ['idAeropuertoDestino', 'id_aeropuerto_destino']) || null
  const origen = resolverAeropuerto(idOrigen)
  const destino = resolverAeropuerto(idDestino)

  return {
    idVuelo: deepValue(item, ['idVuelo', 'id_vuelo', 'id']) || null,
    idAeropuertoOrigen: idOrigen,
    idAeropuertoDestino: idDestino,
    numeroVuelo: deepValue(item, ['numeroVuelo', 'numero_vuelo']) || '',
    estadoVuelo: deepValue(item, ['estadoVuelo', 'estado_vuelo']) || '',
    fechaHoraSalida: deepValue(item, ['fechaHoraSalida', 'fecha_hora_salida']) || '',
    fechaHoraLlegada: deepValue(item, ['fechaHoraLlegada', 'fecha_hora_llegada']) || '',
    origenCodigo: origen?.codigoIata || '',
    destinoCodigo: destino?.codigoIata || '',
    origenNombre: origen?.nombreCiudad || origen?.nombre || '',
    destinoNombre: destino?.nombreCiudad || destino?.nombre || '',
  }
}

function normalizarEscala(item) {
  const aeropuerto = resolverAeropuerto(deepValue(item, ['idAeropuerto', 'id_aeropuerto']))

  return {
    idEscala: deepValue(item, ['idEscala', 'id_escala', 'id']) || null,
    idVuelo: deepValue(item, ['idVuelo', 'id_vuelo']) || null,
    idAeropuerto: deepValue(item, ['idAeropuerto', 'id_aeropuerto']) || null,
    orden: deepValue(item, ['orden']) || 0,
    fechaHoraLlegada: deepValue(item, ['fechaHoraLlegada', 'fecha_hora_llegada']) || '',
    fechaHoraSalida: deepValue(item, ['fechaHoraSalida', 'fecha_hora_salida']) || '',
    duracionMin: deepValue(item, ['duracionMin', 'duracion_min']) || 0,
    tipoEscala: deepValue(item, ['tipoEscala', 'tipo_escala']) || '',
    terminal: deepValue(item, ['terminal']) || '',
    puerta: deepValue(item, ['puerta']) || '',
    observaciones: deepValue(item, ['observaciones']) || '',
    aeropuertoNombre: aeropuerto?.nombre || 'Aeropuerto no encontrado',
    ciudadNombre: aeropuerto?.nombreCiudad || '',
    paisNombre: aeropuerto?.nombrePais || '',
  }
}

function formatoFecha(valor) {
  if (!valor) return '-'
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return '-'
  return new Intl.DateTimeFormat('es-EC', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(fecha)
}

function formatoFechaInput(valor) {
  if (!valor) return ''
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return ''
  const iso = new Date(fecha.getTime() - fecha.getTimezoneOffset() * 60000).toISOString()
  return iso.slice(0, 16)
}

const duracionCalculada = computed(() => {
  const llegada = form.value.fecha_hora_llegada ? new Date(form.value.fecha_hora_llegada) : null
  const salida = form.value.fecha_hora_salida ? new Date(form.value.fecha_hora_salida) : null

  if (!llegada || !salida) return ''
  if (Number.isNaN(llegada.getTime()) || Number.isNaN(salida.getTime())) return ''

  const diff = Math.round((salida.getTime() - llegada.getTime()) / 60000)
  if (diff <= 0) return ''
  return `${diff} min`
})

const rangoEscalaTexto = computed(() => {
  if (!vuelo.value?.fechaHoraSalida || !vuelo.value?.fechaHoraLlegada) return ''
  return `Rango permitido para esta escala: desde ${formatoFecha(vuelo.value.fechaHoraSalida)} hasta ${formatoFecha(vuelo.value.fechaHoraLlegada)}.`
})

function duracionTexto(escala) {
  const minutos = Number(escala.duracionMin || 0)
  if (!minutos) return '0 min'
  if (minutos < 60) return `${minutos} min`
  const horas = Math.floor(minutos / 60)
  const resto = minutos % 60
  return resto ? `${horas}h ${resto}m` : `${horas}h`
}

function obtenerDuracionMinutos() {
  const llegada = form.value.fecha_hora_llegada ? new Date(form.value.fecha_hora_llegada) : null
  const salida = form.value.fecha_hora_salida ? new Date(form.value.fecha_hora_salida) : null

  if (!llegada || !salida) return 0
  if (Number.isNaN(llegada.getTime()) || Number.isNaN(salida.getTime())) return 0

  return Math.round((salida.getTime() - llegada.getTime()) / 60000)
}

function puedeSubir(index) {
  return index > 0
}

function puedeBajar(index) {
  return index < escalasOrdenadas.value.length - 1
}

function sincronizarOrdenVisual(lista) {
  return lista.map((escala, index) => ({
    ...escala,
    orden: index + 1,
  }))
}

function moverEscala(index, direccion) {
  const lista = [...escalasOrdenadas.value]
  const destino = index + direccion

  if (destino < 0 || destino >= lista.length) return

  const temporal = lista[index]
  lista[index] = lista[destino]
  lista[destino] = temporal

  escalas.value = sincronizarOrdenVisual(lista)
  errorEscalas.value = ''
  // TODO: persistir reordenamiento cuando backend exponga endpoint para actualizar orden de escalas.
}

function sugerirSiguienteOrden() {
  return escalasOrdenadas.value.length + 1
}

function limpiarFormulario() {
  form.value = {
    id_aeropuerto: '',
    orden: String(sugerirSiguienteOrden()),
    fecha_hora_llegada: '',
    fecha_hora_salida: '',
    tipo_escala: '',
    terminal: '',
    puerta: '',
    observaciones: '',
  }
  errores.value = {}
  errorModal.value = ''
}

function abrirCrear() {
  if (vueloBloqueado.value) {
    errorEscalas.value = 'No puedes agregar escalas cuando el vuelo esta EN_VUELO, ATERRIZADO o CANCELADO.'
    return
  }
  modo.value = 'crear'
  escalaActiva.value = null
  limpiarFormulario()
  modalAbierto.value = true
}

function abrirEditar(escala) {
  if (vueloBloqueado.value) {
    errorEscalas.value = 'No puedes editar escalas cuando el vuelo esta EN_VUELO, ATERRIZADO o CANCELADO.'
    return
  }
  modo.value = 'editar'
  escalaActiva.value = escala
  form.value = {
    id_aeropuerto: String(escala.idAeropuerto || ''),
    orden: String(escala.orden || ''),
    fecha_hora_llegada: formatoFechaInput(escala.fechaHoraLlegada),
    fecha_hora_salida: formatoFechaInput(escala.fechaHoraSalida),
    tipo_escala: String(escala.tipoEscala || ''),
    terminal: String(escala.terminal || ''),
    puerta: String(escala.puerta || ''),
    observaciones: String(escala.observaciones || ''),
  }
  errores.value = {}
  errorModal.value = ''
  modalAbierto.value = true
}

function validarFormulario() {
  const nuevosErrores = {}
  const llegada = form.value.fecha_hora_llegada ? new Date(form.value.fecha_hora_llegada) : null
  const salida = form.value.fecha_hora_salida ? new Date(form.value.fecha_hora_salida) : null
  const salidaVuelo = vuelo.value?.fechaHoraSalida ? new Date(vuelo.value.fechaHoraSalida) : null
  const llegadaVuelo = vuelo.value?.fechaHoraLlegada ? new Date(vuelo.value.fechaHoraLlegada) : null
  const aeropuertoVueloOrigen = String(vuelo.value?.idAeropuertoOrigen || '')
  const aeropuertoVueloDestino = String(vuelo.value?.idAeropuertoDestino || '')
  const aeropuertoEscala = String(form.value.id_aeropuerto || '')
  const ordenIngresado = Number(form.value.orden || 0)
  const ordenDuplicado = escalasOrdenadas.value.some(
    (escala) =>
      Number(escala.orden) === ordenIngresado &&
      String(escala.idEscala || '') !== String(escalaActiva.value?.idEscala || ''),
  )

  if (vueloBloqueado.value) {
    nuevosErrores.general = 'No se permiten cambios de escalas para este estado de vuelo.'
  }

  if (!form.value.id_aeropuerto) nuevosErrores.id_aeropuerto = 'Selecciona el aeropuerto.'
  if (!form.value.orden || Number(form.value.orden) <= 0) nuevosErrores.orden = 'Ingresa un orden valido.'
  if (!form.value.fecha_hora_llegada) nuevosErrores.fecha_hora_llegada = 'Ingresa la fecha de llegada.'
  if (!form.value.fecha_hora_salida) nuevosErrores.fecha_hora_salida = 'Ingresa la fecha de salida.'
  if (!form.value.tipo_escala) nuevosErrores.tipo_escala = 'Selecciona el tipo de escala.'

  if (aeropuertoEscala && aeropuertoEscala === aeropuertoVueloOrigen) {
    nuevosErrores.id_aeropuerto = 'El aeropuerto de escala no puede ser el origen del vuelo.'
  }

  if (aeropuertoEscala && aeropuertoEscala === aeropuertoVueloDestino) {
    nuevosErrores.id_aeropuerto = 'El aeropuerto de escala no puede ser el destino del vuelo.'
  }

  if (ordenDuplicado) {
    nuevosErrores.orden = 'El orden ya existe para este vuelo.'
  }

  if (llegada && salida && !Number.isNaN(llegada.getTime()) && !Number.isNaN(salida.getTime())) {
    if (salidaVuelo && !Number.isNaN(salidaVuelo.getTime()) && llegada.getTime() <= salidaVuelo.getTime()) {
      nuevosErrores.fecha_hora_llegada = 'La llegada de la escala debe ser posterior a la salida del vuelo.'
    }

    if (salida.getTime() <= llegada.getTime()) {
      nuevosErrores.fecha_hora_salida = 'La salida debe ser posterior a la llegada.'
    }

    if (llegadaVuelo && !Number.isNaN(llegadaVuelo.getTime()) && salida.getTime() >= llegadaVuelo.getTime()) {
      nuevosErrores.fecha_hora_salida = 'La salida de la escala debe ser anterior a la llegada final del vuelo.'
    }
  }

  if (obtenerDuracionMinutos() <= 0 && form.value.fecha_hora_llegada && form.value.fecha_hora_salida) {
    nuevosErrores.fecha_hora_salida = 'La duracion calculada debe ser mayor a 0.'
  }

  errores.value = nuevosErrores
  return !Object.keys(nuevosErrores).length
}

async function guardarEscala() {
  if (guardando.value || !validarFormulario()) return
  guardando.value = true
  errorModal.value = ''

  try {
    if (modo.value === 'editar') {
      modalAbierto.value = false
      return
    }

    const payload = {
      IdAeropuerto: Number(form.value.id_aeropuerto),
      Orden: Number(form.value.orden),
      FechaHoraLlegada: new Date(form.value.fecha_hora_llegada).toISOString(),
      FechaHoraSalida: new Date(form.value.fecha_hora_salida).toISOString(),
      DuracionMin: obtenerDuracionMinutos(),
      TipoEscala: String(form.value.tipo_escala || '').trim().toUpperCase(),
      Terminal: String(form.value.terminal || '').trim() || null,
      Puerta: String(form.value.puerta || '').trim() || null,
    }

    await createEscalaVueloApi(idVuelo.value, payload)
    await cargarTodo()
    modalAbierto.value = false
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo guardar la escala.'
  } finally {
    guardando.value = false
  }
}

function pedirEliminarEscala(escala) {
  if (vueloBloqueado.value) return
  escalaAEliminar.value = escala
  errorEscalas.value = ''
}

async function confirmarEliminarEscala() {
  if (!escalaAEliminar.value || eliminandoEscala.value) return
  eliminandoEscala.value = true
  errorEscalas.value = ''

  try {
    await deleteEscalaVueloApi(idVuelo.value, escalaAEliminar.value.idEscala)
    escalaAEliminar.value = null
    await cargarTodo()
  } catch (err) {
    errorEscalas.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo eliminar la escala.'
  } finally {
    eliminandoEscala.value = false
  }
}

watch(
  () => ({ ...form.value, estadoVuelo: vuelo.value?.estadoVuelo, totalEscalas: escalasOrdenadas.value.length }),
  () => {
    if (!modalAbierto.value) return
    validarFormulario()
  },
  { deep: true },
)

async function cargarTodo() {
  cargando.value = true
  error.value = ''

  try {
    const aeropuertosRespuesta = await getAeropuertosAdminApi({ page: 1, page_size: 200 })
    aeropuertos.value = extractItems(aeropuertosRespuesta).map(normalizarAeropuerto)

    const [vueloRespuesta, escalasRespuesta] = await Promise.all([
      getVueloDetalleApi(idVuelo.value),
      getEscalasVueloApi(idVuelo.value),
    ])

    vuelo.value = normalizarVuelo(deepValue(vueloRespuesta, ['data']) || vueloRespuesta)

    const dataEscalas =
      deepValue(escalasRespuesta, ['data']) ||
      extractItems(escalasRespuesta) ||
      []

    escalas.value = (Array.isArray(dataEscalas) ? dataEscalas : []).map(normalizarEscala)
  } catch (err) {
    error.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudieron cargar las escalas del vuelo.'
  } finally {
    cargando.value = false
  }
}

function volverAVuelos() {
  router.push({ name: 'panel-vuelos' })
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
          @click="volverAVuelos"
        >
          ← Volver a vuelos
        </button>
        <h1 class="text-[2rem] font-bold text-navy">Gestion de Escalas</h1>
        <p class="mt-1.5 text-sm text-text-muted">
          Administra las escalas del vuelo seleccionado sin mezclar otros vuelos.
        </p>
      </div>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <section v-if="vuelo" class="rounded-[24px] bg-white p-6 shadow-sm">
      <div class="grid gap-5 md:grid-cols-4">
        <div>
          <p class="text-xs uppercase tracking-[0.24em] text-text-muted">Vuelo</p>
          <p class="mt-2 text-2xl font-semibold text-navy">{{ vuelo.numeroVuelo }}</p>
        </div>
        <div>
          <p class="text-xs uppercase tracking-[0.24em] text-text-muted">Ruta</p>
          <p class="mt-2 text-base font-medium text-navy">
            {{ vuelo.origenCodigo }} - {{ vuelo.origenNombre }}
          </p>
          <p class="text-sm text-text-muted">→ {{ vuelo.destinoCodigo }} - {{ vuelo.destinoNombre }}</p>
        </div>
        <div>
          <p class="text-xs uppercase tracking-[0.24em] text-text-muted">Horarios</p>
          <p class="mt-2 text-sm text-navy">Salida: {{ formatoFecha(vuelo.fechaHoraSalida) }}</p>
          <p class="text-sm text-navy">Llegada: {{ formatoFecha(vuelo.fechaHoraLlegada) }}</p>
        </div>
        <div>
          <p class="text-xs uppercase tracking-[0.24em] text-text-muted">Estado</p>
          <span class="mt-2 inline-flex rounded-full bg-blue-100 px-3 py-1 text-xs font-semibold text-blue-700">
            {{ vuelo.estadoVuelo || '-' }}
          </span>
        </div>
      </div>
    </section>

    <section class="rounded-[24px] bg-white p-6 shadow-sm">
      <div class="mb-5 flex items-center justify-between gap-4">
        <div>
          <h2 class="text-2xl font-semibold text-navy">Escalas</h2>
          <p class="mt-1 text-sm text-text-muted">Lista ordenada por el campo orden del vuelo.</p>
        </div>

        <button
          type="button"
          class="rounded-2xl bg-gold px-4 py-2.5 text-sm font-semibold text-navy transition-colors hover:bg-gold-light disabled:cursor-not-allowed disabled:opacity-60"
          :disabled="vueloBloqueado"
          @click="abrirCrear"
        >
          + Agregar escala
        </button>
      </div>

      <div
        v-if="errorEscalas"
        class="mb-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
      >
        {{ errorEscalas }}
      </div>

      <div
        v-if="vueloBloqueado"
        class="mb-4 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-navy"
      >
        Este vuelo está en estado {{ vuelo?.estadoVuelo }}. No se permiten crear ni editar escalas.
      </div>

      <div v-if="cargando" class="py-8 text-sm text-text-muted">Cargando escalas...</div>

      <div v-else-if="!escalasOrdenadas.length" class="rounded-2xl bg-slate-50 px-5 py-8 text-center text-sm text-text-muted">
        Este vuelo todavia no tiene escalas registradas. El orden sugerido para la primera escala es {{ sugerirSiguienteOrden() }}.
      </div>

      <div v-else class="space-y-4">
        <article
          v-for="(escala, index) in escalasOrdenadas"
          :key="escala.idEscala"
          class="rounded-[22px] border border-slate-100 bg-slate-50/70 p-5"
        >
          <div class="grid gap-4 md:grid-cols-[80px_1.6fr_1fr_1fr_110px_120px_110px_110px_1fr_180px] md:items-start">
            <div>
              <p class="text-xs uppercase tracking-[0.2em] text-text-muted">Orden</p>
              <p class="mt-2 text-xl font-semibold text-navy">{{ escala.orden }}</p>
            </div>

            <div>
              <p class="text-xs uppercase tracking-[0.2em] text-text-muted">Aeropuerto</p>
              <p class="mt-2 text-sm font-semibold text-navy">{{ escala.aeropuertoNombre }}</p>
              <p class="text-sm text-text-muted">{{ escala.ciudadNombre }} <span v-if="escala.paisNombre">· {{ escala.paisNombre }}</span></p>
            </div>

            <div>
              <p class="text-xs uppercase tracking-[0.2em] text-text-muted">Llegada</p>
              <p class="mt-2 text-sm text-navy">{{ formatoFecha(escala.fechaHoraLlegada) }}</p>
            </div>

            <div>
              <p class="text-xs uppercase tracking-[0.2em] text-text-muted">Salida</p>
              <p class="mt-2 text-sm text-navy">{{ formatoFecha(escala.fechaHoraSalida) }}</p>
            </div>

            <div>
              <p class="text-xs uppercase tracking-[0.2em] text-text-muted">Duracion</p>
              <p class="mt-2 text-sm text-navy">{{ duracionTexto(escala) }}</p>
            </div>

            <div>
              <p class="text-xs uppercase tracking-[0.2em] text-text-muted">Tipo</p>
              <p class="mt-2 text-sm text-navy">{{ escala.tipoEscala || '-' }}</p>
            </div>

            <div>
              <p class="text-xs uppercase tracking-[0.2em] text-text-muted">Terminal</p>
              <p class="mt-2 text-sm text-navy">{{ escala.terminal || '-' }}</p>
            </div>

            <div>
              <p class="text-xs uppercase tracking-[0.2em] text-text-muted">Puerta</p>
              <p class="mt-2 text-sm text-navy">{{ escala.puerta || '-' }}</p>
            </div>

            <div>
              <p class="text-xs uppercase tracking-[0.2em] text-text-muted">Observaciones</p>
              <p class="mt-2 text-sm text-navy">{{ escala.observaciones || '-' }}</p>
            </div>

            <div>
              <p class="text-xs uppercase tracking-[0.2em] text-text-muted">Acciones</p>
              <div class="mt-2 flex flex-wrap gap-2">
                <button
                  type="button"
                  class="rounded-full border border-slate-200 px-3 py-1 text-xs font-medium text-navy disabled:cursor-not-allowed disabled:opacity-60"
                  :disabled="vueloBloqueado"
                  @click="abrirEditar(escala)"
                >
                  Editar
                </button>
                <button
                  v-if="puedeEliminar"
                  type="button"
                  class="rounded-full border border-red-200 px-3 py-1 text-xs font-medium text-red-600 disabled:cursor-not-allowed disabled:opacity-60"
                  :disabled="vueloBloqueado"
                  @click="pedirEliminarEscala(escala)"
                >
                  Eliminar
                </button>
                <button
                  type="button"
                  class="rounded-full border border-slate-200 px-3 py-1 text-xs font-medium text-navy disabled:cursor-not-allowed disabled:opacity-40"
                  :disabled="!puedeSubir(index)"
                  @click="moverEscala(index, -1)"
                >
                  Subir
                </button>
                <button
                  type="button"
                  class="rounded-full border border-slate-200 px-3 py-1 text-xs font-medium text-navy disabled:cursor-not-allowed disabled:opacity-40"
                  :disabled="!puedeBajar(index)"
                  @click="moverEscala(index, 1)"
                >
                  Bajar
                </button>
              </div>
            </div>
          </div>
        </article>
      </div>

      <p class="mt-5 text-xs text-text-muted">
        TODO: conectar persistencia de reordenamiento cuando backend exponga endpoint para actualizar orden de escalas.
      </p>
    </section>

    <div
      v-if="modalAbierto"
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4"
      @click.self="modalAbierto = false"
    >
      <div class="w-full max-w-4xl rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">
          {{ modo === 'crear' ? 'Nueva Escala' : 'Editar Escala' }}
        </h2>

        <div
          v-if="errorModal"
          class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
        >
          {{ errorModal }}
        </div>

        <div
          v-if="errores.general"
          class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
        >
          {{ errores.general }}
        </div>

        <div
          v-if="rangoEscalaTexto"
          class="mt-4 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-navy"
        >
          {{ rangoEscalaTexto }}
        </div>

        <div class="mt-6 grid gap-5 md:grid-cols-2">
          <SelectApp
            v-model="form.id_aeropuerto"
            label="Aeropuerto"
            :opciones="opcionesAeropuertos"
            :error="errores.id_aeropuerto"
            requerido
          />
          <InputApp
            v-model="form.orden"
            tipo="number"
            label="Orden"
            :error="errores.orden"
            requerido
          />
          <InputApp
            v-model="form.fecha_hora_llegada"
            tipo="datetime-local"
            label="Fecha hora llegada"
            :error="errores.fecha_hora_llegada"
            requerido
          />
          <InputApp
            v-model="form.fecha_hora_salida"
            tipo="datetime-local"
            label="Fecha hora salida"
            :error="errores.fecha_hora_salida"
            requerido
          />
          <SelectApp
            v-model="form.tipo_escala"
            label="Tipo de escala"
            :opciones="opcionesTipoEscala"
            :error="errores.tipo_escala"
            requerido
          />
          <InputApp :model-value="duracionCalculada || 'Se calcula automaticamente'" label="Duracion" deshabilitado />
          <InputApp v-model="form.terminal" label="Terminal" />
          <InputApp v-model="form.puerta" label="Puerta" />
          <InputApp
            v-model="form.observaciones"
            class="md:col-span-2"
            label="Observaciones"
            placeholder="Observaciones operativas de la escala"
          />
        </div>

        <div class="mt-8 flex justify-end gap-3">
          <button
            type="button"
            class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50"
            @click="modalAbierto = false"
          >
            Cancelar
          </button>
          <button
            type="button"
            class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy hover:bg-gold-light disabled:opacity-60"
            :disabled="!puedeGuardar"
            @click="guardarEscala"
          >
            {{ guardando ? 'Guardando...' : 'Guardar escala' }}
          </button>
        </div>
      </div>
    </div>

    <div
      v-if="escalaAEliminar"
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4"
      @click.self="escalaAEliminar = null"
    >
      <div class="w-full max-w-lg rounded-[28px] bg-white p-7 shadow-2xl">
        <h3 class="text-2xl font-bold text-navy">Eliminar escala</h3>
        <p class="mt-3 text-sm text-text-muted">
          Esta acción eliminará la escala {{ escalaAEliminar.orden }} del aeropuerto {{ escalaAEliminar.aeropuertoNombre }}.
        </p>
        <div class="mt-7 flex justify-end gap-3">
          <button
            type="button"
            class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50"
            @click="escalaAEliminar = null"
          >
            Cancelar
          </button>
          <button
            type="button"
            class="rounded-2xl bg-red-500 px-5 py-3 text-sm font-semibold text-white hover:bg-red-600 disabled:opacity-60"
            :disabled="eliminandoEscala"
            @click="confirmarEliminarEscala"
          >
            {{ eliminandoEscala ? 'Eliminando...' : 'Eliminar' }}
          </button>
        </div>
      </div>
    </div>
  </section>
</template>
