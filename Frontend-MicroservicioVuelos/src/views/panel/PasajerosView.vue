<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import {
  createPasajeroApi,
  deletePasajeroApi,
  getPasajerosApi,
  updatePasajeroApi,
} from '@/api/pasajeros.api'
import { getClientesApi } from '@/api/clientes.api'
import { getPaisesApi } from '@/api/paises.api'
import { usePanelPermisos } from '@/composables/usePanelPermisos'
import { deepValue, extractItems } from '@/utils/portalCliente'

const TIPOS_DOCUMENTO = ['CEDULA', 'PASAPORTE', 'RUC', 'OTRO']
const GENEROS = ['MASCULINO', 'FEMENINO', 'OTRO']

const cargando = ref(true)
const guardando = ref(false)
const eliminando = ref(false)
const error = ref('')
const errorModal = ref('')
const busqueda = ref('')
const pasajeros = ref([])
const clientes = ref([])
const paises = ref([])
const modalAbierto = ref(false)
const modalEliminar = ref(false)
const modalDetalle = ref(false)
const modo = ref('crear')
const pasajeroActivo = ref(null)
const errores = ref({})
const yaValidado = ref(false)

const form = ref({
  nombre_pasajero: '',
  apellido_pasajero: '',
  tipo_documento_pasajero: '',
  numero_documento_pasajero: '',
  id_cliente: '',
  fecha_nacimiento_pasajero: '',
  id_pais_nacionalidad: '',
  email_contacto_pasajero: '',
  telefono_contacto_pasajero: '',
  genero_pasajero: '',
  requiere_asistencia: false,
  observaciones_pasajero: '',
  estado: 'ACT',
})

const opcionesTipoDocumento = computed(() =>
  TIPOS_DOCUMENTO.map((item) => ({ valor: item, etiqueta: item })),
)

const opcionesGenero = computed(() =>
  GENEROS.map((item) => ({ valor: item, etiqueta: capitalizarTexto(item) })),
)

const opcionesPaises = computed(() =>
  paises.value
    .slice()
    .sort((a, b) => String(a.nombre).localeCompare(String(b.nombre), 'es'))
    .map((pais) => ({ valor: String(pais.idPais), etiqueta: pais.nombre })),
)

const opcionesClientes = computed(() => [
  { valor: '', etiqueta: 'Sin cliente asociado' },
  ...clientes.value
    .slice()
    .sort((a, b) => String(a.nombreCompleto).localeCompare(String(b.nombreCompleto), 'es'))
    .map((c) => {
      const nombre = c.nombreCompleto || c.correo || `#${c.idCliente}`
      const sufijo = c.nombreCompleto && c.correo ? ` (${c.correo})` : ''
      return { valor: String(c.idCliente), etiqueta: `${nombre}${sufijo}` }
    }),
])
const { puedeEliminar } = usePanelPermisos()

const pasajerosFiltrados = computed(() => {
  const termino = busqueda.value.trim().toLowerCase()

  return [...pasajeros.value]
    .sort((a, b) => String(a.nombrePasajero).localeCompare(String(b.nombrePasajero), 'es'))
    .filter((p) => {
      if (!termino) return true
      return (
        String(p.nombrePasajero).toLowerCase().includes(termino) ||
        String(p.apellidoPasajero).toLowerCase().includes(termino) ||
        String(p.numeroDocumentoPasajero).toLowerCase().includes(termino) ||
        String(p.tipoDocumentoPasajero).toLowerCase().includes(termino) ||
        String(p.nombreCliente).toLowerCase().includes(termino) ||
        String(p.correoCliente).toLowerCase().includes(termino)
      )
    })
})

function capitalizarTexto(valor) {
  const limpio = String(valor || '').trim().toLowerCase().replace(/\s+/g, ' ')
  if (!limpio) return ''
  return limpio.replace(/\b\p{L}/gu, (match) => match.toUpperCase())
}

function formatearFecha(fecha) {
  if (!fecha) return '-'
  const d = new Date(`${String(fecha).slice(0, 10)}T00:00:00`)
  if (isNaN(d.getTime())) return String(fecha).slice(0, 10)
  return d.toLocaleDateString('es-ES', { day: 'numeric', month: 'long', year: 'numeric' })
}

function estadoAmigable(estado) {
  const v = String(estado || '').trim().toUpperCase()
  if (v === 'ACT' || v === 'ACTIVO') return 'Activo'
  if (v === 'INA' || v === 'INACTIVO') return 'Inactivo'
  return v || null
}

function esActivo(estado) {
  const v = String(estado || '').trim().toUpperCase()
  return v === 'ACT' || v === 'ACTIVO'
}

function normalizarEstadoFormulario(estado) {
  const valor = String(estado || '').trim().toUpperCase()
  if (valor === 'INA' || valor === 'INACTIVO') return 'INA'
  return 'ACT'
}

function normalizarPais(item) {
  return {
    idPais: deepValue(item, ['idPais', 'id_pais', 'id']) || null,
    nombre: deepValue(item, ['nombre', 'nombrePais', 'nombre_pais']) || '',
  }
}

function obtenerNombrePais(idPais) {
  if (!idPais) return ''
  return paises.value.find((pais) => String(pais.idPais) === String(idPais))?.nombre || ''
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
  const idCliente = deepValue(item, ['idCliente', 'id_cliente']) || null
  const idPaisNacionalidad = deepValue(item, ['idPaisNacionalidad', 'id_pais_nacionalidad']) || null

  // Prioridad: datos embebidos en el response → lookup en lista de clientes
  const nombreClienteApi =
    deepValue(item, ['nombreCliente', 'nombre_cliente', 'clienteNombre', 'cliente']) || ''
  const correoClienteApi =
    deepValue(item, ['correoCliente', 'correo_cliente', 'emailCliente']) || ''

  const clienteMatch = idCliente
    ? clientes.value.find((c) => String(c.idCliente) === String(idCliente))
    : null

  return {
    idPasajero: deepValue(item, ['idPasajero', 'id_pasajero', 'id']) || null,
    nombrePasajero: deepValue(item, ['nombrePasajero', 'nombre_pasajero']) || '',
    apellidoPasajero: deepValue(item, ['apellidoPasajero', 'apellido_pasajero']) || '',
    tipoDocumentoPasajero: deepValue(item, ['tipoDocumentoPasajero', 'tipo_documento_pasajero']) || '',
    numeroDocumentoPasajero: deepValue(item, ['numeroDocumentoPasajero', 'numero_documento_pasajero']) || '',
    idCliente,
    idPaisNacionalidad,
    nombreCliente: nombreClienteApi || clienteMatch?.nombreCompleto || '',
    correoCliente: correoClienteApi || clienteMatch?.correo || '',
    fechaNacimientoPasajero: deepValue(item, ['fechaNacimientoPasajero', 'fecha_nacimiento_pasajero']) || '',
    nacionalidad:
      deepValue(item, ['nombrePaisNacionalidad', 'nombre_pais_nacionalidad', 'nacionalidad']) ||
      obtenerNombrePais(idPaisNacionalidad),
    emailContactoPasajero: deepValue(item, ['emailContactoPasajero', 'email_contacto_pasajero']) || '',
    telefonoContactoPasajero: deepValue(item, ['telefonoContactoPasajero', 'telefono_contacto_pasajero']) || '',
    generoPasajero: deepValue(item, ['generoPasajero', 'genero_pasajero']) || '',
    requiereAsistencia: !!deepValue(item, ['requiereAsistencia', 'requiere_asistencia']),
    observacionesPasajero: deepValue(item, ['observacionesPasajero', 'observaciones_pasajero']) || '',
    estado: deepValue(item, ['estado']) || '',
  }
}

function limpiarFormulario() {
  form.value = {
    nombre_pasajero: '',
    apellido_pasajero: '',
    tipo_documento_pasajero: '',
    numero_documento_pasajero: '',
    id_cliente: '',
    fecha_nacimiento_pasajero: '',
    id_pais_nacionalidad: '',
    email_contacto_pasajero: '',
    telefono_contacto_pasajero: '',
    genero_pasajero: '',
    requiere_asistencia: false,
    observaciones_pasajero: '',
    estado: 'ACT',
  }
  errores.value = {}
  errorModal.value = ''
  yaValidado.value = false
}

function abrirDetalle(pasajero) {
  pasajeroActivo.value = pasajero
  modalDetalle.value = true
}

function abrirCrear() {
  modo.value = 'crear'
  pasajeroActivo.value = null
  limpiarFormulario()
  modalAbierto.value = true
}

function abrirEditar(pasajero) {
  modo.value = 'editar'
  pasajeroActivo.value = pasajero
  form.value = {
    nombre_pasajero: String(pasajero.nombrePasajero || ''),
    apellido_pasajero: String(pasajero.apellidoPasajero || ''),
    tipo_documento_pasajero: String(pasajero.tipoDocumentoPasajero || ''),
    numero_documento_pasajero: String(pasajero.numeroDocumentoPasajero || ''),
    id_cliente: pasajero.idCliente ? String(pasajero.idCliente) : '',
    fecha_nacimiento_pasajero: pasajero.fechaNacimientoPasajero
      ? String(pasajero.fechaNacimientoPasajero).slice(0, 10)
      : '',
    id_pais_nacionalidad: pasajero.idPaisNacionalidad ? String(pasajero.idPaisNacionalidad) : '',
    email_contacto_pasajero: String(pasajero.emailContactoPasajero || ''),
    telefono_contacto_pasajero: String(pasajero.telefonoContactoPasajero || ''),
    genero_pasajero: String(pasajero.generoPasajero || ''),
    requiere_asistencia: !!pasajero.requiereAsistencia,
    observaciones_pasajero: String(pasajero.observacionesPasajero || ''),
    estado: 'ACT',
  }
  errores.value = {}
  errorModal.value = ''
  yaValidado.value = false
  modalAbierto.value = true
}

function abrirEliminar(pasajero) {
  pasajeroActivo.value = pasajero
  errorModal.value = ''
  modalEliminar.value = true
}

function validar() {
  const nuevosErrores = {}
  const GENEROS_VALIDOS = ['MASCULINO', 'FEMENINO', 'OTRO']
  const email = String(form.value.email_contacto_pasajero || '').trim()
  const genero = String(form.value.genero_pasajero || '').trim().toUpperCase()
  const fechaNac = String(form.value.fecha_nacimiento_pasajero || '').trim()

  if (!String(form.value.nombre_pasajero || '').trim())
    nuevosErrores.nombre_pasajero = 'Ingresa el nombre.'
  if (!String(form.value.apellido_pasajero || '').trim())
    nuevosErrores.apellido_pasajero = 'Ingresa el apellido.'
  const tipoDoc = String(form.value.tipo_documento_pasajero || '').trim().toUpperCase()
  if (!tipoDoc)
    nuevosErrores.tipo_documento_pasajero = 'Selecciona el tipo de documento.'
  else if (!['CEDULA', 'PASAPORTE', 'RUC', 'OTRO'].includes(tipoDoc))
    nuevosErrores.tipo_documento_pasajero = 'Tipo de documento no válido.'
  if (!String(form.value.numero_documento_pasajero || '').trim())
    nuevosErrores.numero_documento_pasajero = 'Ingresa el número de documento.'
  if (!String(form.value.id_pais_nacionalidad || '').trim())
    nuevosErrores.id_pais_nacionalidad = 'Selecciona la nacionalidad.'
  if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))
    nuevosErrores.email_contacto_pasajero = 'Ingresa un email válido.'
  if (genero && !GENEROS_VALIDOS.includes(genero))
    nuevosErrores.genero_pasajero = 'Género no válido.'
  if (fechaNac) {
    const hoy = new Date()
    const fecha = new Date(`${fechaNac}T00:00:00`)
    if (fecha >= new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate()))
      nuevosErrores.fecha_nacimiento_pasajero = 'La fecha de nacimiento debe ser pasada.'
  }
  errores.value = nuevosErrores
  return !Object.keys(nuevosErrores).length
}

async function cargarPasajeros() {
  cargando.value = true
  error.value = ''
  try {
    const [respuestaPasajeros, respuestaClientes, respuestaPaises] = await Promise.all([
      getPasajerosApi({ page: 1, page_size: 200 }),
      getClientesApi({ page: 1, page_size: 200 }),
      getPaisesApi({ page: 1, page_size: 200 }),
    ])
    // Clientes primero para que normalizarPasajero pueda hacer lookup
    clientes.value = extractItems(respuestaClientes).map(normalizarCliente)
    paises.value = extractItems(respuestaPaises).map(normalizarPais)
    pasajeros.value = extractItems(respuestaPasajeros).map(normalizarPasajero)
  } catch (err) {
    error.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudieron cargar los pasajeros.'
  } finally {
    cargando.value = false
  }
}

async function guardarPasajero() {
  yaValidado.value = true
  if (guardando.value || !validar()) return
  guardando.value = true
  errorModal.value = ''

  // Leer valores del form en el momento exacto del envío
  const nombrePasajero = capitalizarTexto(String(form.value.nombre_pasajero ?? ''))
  const apellidoPasajero = capitalizarTexto(String(form.value.apellido_pasajero ?? ''))
  const tipoDocumentoPasajero = String(form.value.tipo_documento_pasajero ?? '').trim().toUpperCase()
  const numeroDocumentoPasajero = String(form.value.numero_documento_pasajero ?? '').trim()

  if (!nombrePasajero || !apellidoPasajero || !tipoDocumentoPasajero || !numeroDocumentoPasajero) {
    errorModal.value = 'Completa los campos obligatorios antes de guardar.'
    guardando.value = false
    return
  }

  const payload = {
    id_cliente: form.value.id_cliente ? Number(form.value.id_cliente) : null,
    nombre_pasajero: nombrePasajero,
    apellido_pasajero: apellidoPasajero,
    tipo_documento_pasajero: tipoDocumentoPasajero,
    numero_documento_pasajero: numeroDocumentoPasajero,
    fecha_nacimiento_pasajero: form.value.fecha_nacimiento_pasajero
      ? `${String(form.value.fecha_nacimiento_pasajero).slice(0, 10)}T00:00:00`
      : null,
    id_pais_nacionalidad: Number(form.value.id_pais_nacionalidad),
    email_contacto_pasajero: String(form.value.email_contacto_pasajero ?? '').trim()
      ? String(form.value.email_contacto_pasajero).trim().toLowerCase()
      : null,
    telefono_contacto_pasajero: String(form.value.telefono_contacto_pasajero ?? '').trim()
      ? String(form.value.telefono_contacto_pasajero).trim()
      : null,
    genero_pasajero: form.value.genero_pasajero
      ? String(form.value.genero_pasajero).trim().toUpperCase()
      : null,
    requiere_asistencia: !!form.value.requiere_asistencia,
    observaciones_pasajero: String(form.value.observaciones_pasajero ?? '').trim()
      ? String(form.value.observaciones_pasajero).trim()
      : null,
  }
  try {
    if (modo.value === 'crear') {
      await createPasajeroApi(payload)
    } else {
      const payloadActualizacion = {
        id_cliente: form.value.id_cliente ? Number(form.value.id_cliente) : null,
        nombre_pasajero: nombrePasajero,
        apellido_pasajero: apellidoPasajero,
        tipo_documento_pasajero: tipoDocumentoPasajero,
        numero_documento_pasajero: numeroDocumentoPasajero,
        fecha_nacimiento_pasajero: form.value.fecha_nacimiento_pasajero
          ? `${String(form.value.fecha_nacimiento_pasajero).slice(0, 10)}T00:00:00`
          : null,
        id_pais_nacionalidad: Number(form.value.id_pais_nacionalidad),
        email_contacto_pasajero: String(form.value.email_contacto_pasajero ?? '').trim()
          ? String(form.value.email_contacto_pasajero).trim().toLowerCase()
          : null,
        telefono_contacto_pasajero: String(form.value.telefono_contacto_pasajero ?? '').trim()
          ? String(form.value.telefono_contacto_pasajero).trim()
          : null,
        genero_pasajero: form.value.genero_pasajero
          ? String(form.value.genero_pasajero).trim()
          : null,
        requiere_asistencia: !!form.value.requiere_asistencia,
        observaciones_pasajero: String(form.value.observaciones_pasajero ?? '').trim()
          ? String(form.value.observaciones_pasajero).trim()
          : null,
      }

      await updatePasajeroApi(pasajeroActivo.value.idPasajero, payloadActualizacion)
    }
    modalAbierto.value = false
    await cargarPasajeros()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo guardar el pasajero.'
  } finally {
    guardando.value = false
  }
}

async function confirmarEliminar() {
  if (!pasajeroActivo.value?.idPasajero || eliminando.value) return
  eliminando.value = true
  errorModal.value = ''
  try {
    await deletePasajeroApi(pasajeroActivo.value.idPasajero)
    modalEliminar.value = false
    await cargarPasajeros()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo eliminar el pasajero.'
  } finally {
    eliminando.value = false
  }
}

watch(
  () => form.value.nombre_pasajero,
  (valor) => { form.value.nombre_pasajero = capitalizarTexto(valor) },
)

watch(
  () => form.value.apellido_pasajero,
  (valor) => { form.value.apellido_pasajero = capitalizarTexto(valor) },
)

watch(form, () => { if (yaValidado.value) validar() }, { deep: true })

onMounted(cargarPasajeros)
</script>

<template>
  <section class="space-y-5">

    <!-- Encabezado -->
    <div class="flex items-start justify-between gap-4">
      <div>
        <h1 class="text-[2rem] font-bold text-navy">Gestión de Pasajeros</h1>
        <p class="mt-1.5 text-sm text-text-muted">Administra los pasajeros registrados en el sistema.</p>
      </div>
      <button
        type="button"
        class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy transition-colors hover:bg-gold-light"
        @click="abrirCrear"
      >
        + Nuevo Pasajero
      </button>
    </div>

    <!-- Error global -->
    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <!-- Buscador -->
    <section class="rounded-[24px] bg-white p-4 shadow-sm">
      <InputApp v-model="busqueda" placeholder="Buscar por nombre, documento o cliente asociado..." />
    </section>

    <!-- Tabla -->
    <section class="rounded-[24px] bg-white shadow-sm">
      <div class="overflow-x-auto">
        <div class="min-w-[960px]">

          <!-- Encabezado tabla -->
          <div class="grid grid-cols-[1fr_1fr_1.4fr_1fr_1fr_1fr_0.7fr_88px] gap-3 bg-slate-50 px-5 py-3.5 text-sm font-semibold text-navy">
            <span class="whitespace-nowrap">Nombres</span>
            <span class="whitespace-nowrap">Apellidos</span>
            <span class="whitespace-nowrap">Documento</span>
            <span class="whitespace-nowrap">Nacionalidad</span>
            <span class="whitespace-nowrap">Fecha nacimiento</span>
            <span class="whitespace-nowrap">Cliente asociado</span>
            <span class="whitespace-nowrap">Estado</span>
            <span class="whitespace-nowrap text-right">Acciones</span>
          </div>

          <!-- Estado de carga -->
          <div v-if="cargando" class="px-5 py-6 text-sm text-text-muted">
            Cargando pasajeros...
          </div>
          <div v-else-if="!pasajerosFiltrados.length" class="px-5 py-6 text-sm text-text-muted">
            No hay pasajeros registrados.
          </div>

          <!-- Filas -->
          <div v-else class="divide-y divide-slate-100">
            <div
              v-for="pasajero in pasajerosFiltrados"
              :key="pasajero.idPasajero"
              class="grid grid-cols-[1fr_1fr_1.4fr_1fr_1fr_1fr_0.7fr_88px] items-center gap-3 px-5 py-4"
            >
              <span class="truncate text-sm text-navy" :title="pasajero.nombrePasajero">
                {{ pasajero.nombrePasajero || '-' }}
              </span>
              <span class="truncate text-sm text-navy" :title="pasajero.apellidoPasajero">
                {{ pasajero.apellidoPasajero || '-' }}
              </span>
              <div class="min-w-0">
                <span class="block truncate text-sm font-medium text-navy" :title="pasajero.numeroDocumentoPasajero">
                  {{ pasajero.numeroDocumentoPasajero || '-' }}
                </span>
                <span class="text-xs text-text-muted">{{ pasajero.tipoDocumentoPasajero || '' }}</span>
              </div>
              <span class="truncate text-sm text-navy">{{ pasajero.nacionalidad || '-' }}</span>
              <span class="truncate text-sm text-navy">{{ formatearFecha(pasajero.fechaNacimientoPasajero) }}</span>
              <div class="min-w-0">
                <span
                  v-if="pasajero.nombreCliente"
                  class="block truncate text-sm text-navy"
                  :title="pasajero.nombreCliente"
                >
                  {{ pasajero.nombreCliente }}
                </span>
                <span
                  v-else-if="pasajero.correoCliente"
                  class="block truncate text-sm text-navy"
                  :title="pasajero.correoCliente"
                >
                  {{ pasajero.correoCliente }}
                </span>
                <span v-else class="text-sm text-text-muted">Sin cliente asociado</span>
                <span v-if="pasajero.nombreCliente && pasajero.correoCliente" class="block truncate text-xs text-text-muted" :title="pasajero.correoCliente">
                  {{ pasajero.correoCliente }}
                </span>
              </div>
              <span>
                <span
                  v-if="estadoAmigable(pasajero.estado)"
                  class="rounded-full px-2 py-0.5 text-xs font-semibold"
                  :class="esActivo(pasajero.estado) ? 'bg-emerald-100 text-emerald-700' : 'bg-red-100 text-red-600'"
                >
                  {{ estadoAmigable(pasajero.estado) }}
                </span>
                <span v-else class="text-sm text-text-muted">-</span>
              </span>
              <div class="flex justify-end gap-3 text-base">
                <button type="button" class="text-slate-500 hover:text-navy" title="Ver" @click="abrirDetalle(pasajero)">👁</button>
                <button type="button" class="text-slate-500 hover:text-navy" title="Editar" @click="abrirEditar(pasajero)">✎</button>
                <button v-if="puedeEliminar" type="button" class="text-red-500 hover:text-red-600" title="Eliminar" @click="abrirEliminar(pasajero)">🗑</button>
              </div>
            </div>
          </div>

        </div>
      </div>
    </section>

    <!-- Modal detalle -->
    <div
      v-if="modalDetalle"
      class="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-950/50 px-4 py-10"
      @click.self="modalDetalle = false"
    >
      <div class="w-full max-w-3xl rounded-[28px] bg-white shadow-2xl">

        <div class="flex items-start justify-between gap-4 rounded-t-[28px] bg-navy px-8 py-7">
          <div>
            <p class="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Pasajero</p>
            <h2 class="mt-1 text-2xl font-bold text-white">
              {{ [pasajeroActivo?.nombrePasajero, pasajeroActivo?.apellidoPasajero].filter(Boolean).join(' ') || '-' }}
            </h2>
            <p v-if="pasajeroActivo?.tipoDocumentoPasajero" class="mt-0.5 text-sm text-slate-300">
              {{ pasajeroActivo.tipoDocumentoPasajero }} · {{ pasajeroActivo.numeroDocumentoPasajero }}
            </p>
          </div>
          <span
            v-if="estadoAmigable(pasajeroActivo?.estado)"
            class="mt-1 shrink-0 rounded-full px-3 py-1 text-xs font-semibold"
            :class="esActivo(pasajeroActivo?.estado) ? 'bg-emerald-500/20 text-emerald-300' : 'bg-red-500/20 text-red-300'"
          >
            {{ estadoAmigable(pasajeroActivo?.estado) }}
          </span>
        </div>

        <div class="space-y-6 p-8">

          <!-- Documento -->
          <div>
            <p class="mb-3 text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Documento</p>
            <div class="grid gap-3 md:grid-cols-2">
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Tipo</p>
                <p class="mt-1 font-semibold text-navy">{{ pasajeroActivo?.tipoDocumentoPasajero || '-' }}</p>
              </div>
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Número</p>
                <p class="mt-1 font-semibold text-navy">{{ pasajeroActivo?.numeroDocumentoPasajero || '-' }}</p>
              </div>
            </div>
          </div>

          <!-- Datos personales -->
          <div>
            <p class="mb-3 text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Datos personales</p>
            <div class="grid gap-3 md:grid-cols-3">
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Fecha de nacimiento</p>
                <p class="mt-1 font-semibold text-navy">{{ formatearFecha(pasajeroActivo?.fechaNacimientoPasajero) }}</p>
              </div>
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Nacionalidad</p>
                <p class="mt-1 font-semibold text-navy">{{ pasajeroActivo?.nacionalidad || '-' }}</p>
              </div>
              <div v-if="pasajeroActivo?.generoPasajero" class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Género</p>
                <p class="mt-1 font-semibold text-navy">{{ capitalizarTexto(pasajeroActivo.generoPasajero) }}</p>
              </div>
            </div>
          </div>

          <!-- Contacto -->
          <div v-if="pasajeroActivo?.emailContactoPasajero || pasajeroActivo?.telefonoContactoPasajero">
            <p class="mb-3 text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Contacto</p>
            <div class="grid gap-3 md:grid-cols-2">
              <div v-if="pasajeroActivo?.emailContactoPasajero" class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Email</p>
                <p class="mt-1 font-semibold text-navy">{{ pasajeroActivo.emailContactoPasajero }}</p>
              </div>
              <div v-if="pasajeroActivo?.telefonoContactoPasajero" class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Teléfono</p>
                <p class="mt-1 font-semibold text-navy">{{ pasajeroActivo.telefonoContactoPasajero }}</p>
              </div>
            </div>
          </div>

          <!-- Asistencia y observaciones -->
          <div>
            <p class="mb-3 text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Servicios</p>
            <div class="grid gap-3" :class="pasajeroActivo?.observacionesPasajero ? 'md:grid-cols-2' : ''">
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Asistencia especial</p>
                <p class="mt-1 font-semibold text-navy">
                  {{ pasajeroActivo?.requiereAsistencia ? 'Sí, requiere asistencia' : 'No requiere asistencia especial' }}
                </p>
              </div>
              <div v-if="pasajeroActivo?.observacionesPasajero" class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Observaciones</p>
                <p class="mt-1 font-semibold text-navy">{{ pasajeroActivo.observacionesPasajero }}</p>
              </div>
            </div>
          </div>

          <!-- Cliente asociado -->
          <div>
            <p class="mb-3 text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Cliente asociado</p>
            <div
              v-if="pasajeroActivo?.nombreCliente || pasajeroActivo?.correoCliente"
              class="rounded-2xl bg-slate-50 p-4"
            >
              <p v-if="pasajeroActivo.nombreCliente" class="font-semibold text-navy">
                {{ pasajeroActivo.nombreCliente }}
              </p>
              <p
                v-if="pasajeroActivo.correoCliente"
                class="mt-0.5 text-sm"
                :class="pasajeroActivo.nombreCliente ? 'text-text-muted' : 'font-semibold text-navy'"
              >
                {{ pasajeroActivo.correoCliente }}
              </p>
            </div>
            <div v-else class="rounded-2xl bg-slate-50 p-4">
              <p class="text-sm text-text-muted">Sin cliente asociado</p>
            </div>
          </div>

        </div>

        <div class="flex justify-end gap-3 border-t border-slate-100 px-8 py-5">
          <button
            type="button"
            class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50"
            @click="modalDetalle = false"
          >
            Cerrar
          </button>
          <button
            type="button"
            class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy hover:bg-gold-light"
            @click="() => { modalDetalle = false; abrirEditar(pasajeroActivo) }"
          >
            Editar
          </button>
        </div>
      </div>
    </div>

    <!-- Modal crear / editar -->
    <div
      v-if="modalAbierto"
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4"
      @click.self="modalAbierto = false"
    >
      <div class="max-h-[90vh] w-full max-w-3xl overflow-y-auto rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">
          {{ modo === 'crear' ? 'Nuevo Pasajero' : 'Editar Pasajero' }}
        </h2>

        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorModal }}
        </div>

        <div class="mt-6 grid gap-5 md:grid-cols-2">

          <!-- Identificación -->
          <InputApp
            v-model="form.nombre_pasajero"
            label="Nombre"
            :error="errores.nombre_pasajero"
            requerido
          />
          <InputApp
            v-model="form.apellido_pasajero"
            label="Apellido"
            :error="errores.apellido_pasajero"
            requerido
          />
          <SelectApp
            v-model="form.tipo_documento_pasajero"
            label="Tipo de documento"
            :opciones="opcionesTipoDocumento"
            :error="errores.tipo_documento_pasajero"
            requerido
          />
          <InputApp
            v-model="form.numero_documento_pasajero"
            label="Número de documento"
            :error="errores.numero_documento_pasajero"
            requerido
          />

          <!-- Datos personales -->
          <InputApp
            v-model="form.fecha_nacimiento_pasajero"
            label="Fecha de nacimiento"
            tipo="date"
            :error="errores.fecha_nacimiento_pasajero"
          />
          <SelectApp
            v-model="form.id_pais_nacionalidad"
            label="Nacionalidad"
            :opciones="opcionesPaises"
            :error="errores.id_pais_nacionalidad"
            requerido
          />
          <SelectApp
            v-model="form.genero_pasajero"
            label="Género"
            :opciones="opcionesGenero"
            :error="errores.genero_pasajero"
          />
          <!-- Contacto -->
          <InputApp
            v-model="form.email_contacto_pasajero"
            label="Email de contacto"
            :error="errores.email_contacto_pasajero"
          />
          <InputApp
            v-model="form.telefono_contacto_pasajero"
            label="Teléfono de contacto"
          />

          <!-- Cliente asociado (ocupa fila completa) -->
          <div class="md:col-span-2">
            <SelectApp
              v-model="form.id_cliente"
              label="Cliente asociado"
              :opciones="opcionesClientes"
            />
            <p class="mt-1.5 text-xs text-text-muted">
              Opcional. Vincula este pasajero a un cliente registrado en el sistema.
            </p>
          </div>

          <!-- Asistencia especial -->
          <div class="md:col-span-2">
            <div class="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3.5">
              <input
                id="requiere_asistencia"
                v-model="form.requiere_asistencia"
                type="checkbox"
                class="h-4 w-4 rounded accent-navy"
              />
              <div>
                <label for="requiere_asistencia" class="cursor-pointer text-sm font-semibold text-navy">
                  Requiere asistencia especial
                </label>
                <p class="text-xs text-text-muted">Silla de ruedas, asistencia en embarque, etc.</p>
              </div>
            </div>
          </div>

          <!-- Observaciones -->
          <div class="md:col-span-2">
            <InputApp
              v-model="form.observaciones_pasajero"
              label="Observaciones"
            />
          </div>

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
            :disabled="guardando || (yaValidado && Object.keys(errores).length > 0)"
            @click="guardarPasajero"
          >
            {{ guardando ? 'Guardando...' : 'Guardar' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Modal eliminar -->
    <div
      v-if="modalEliminar"
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4"
      @click.self="modalEliminar = false"
    >
      <div class="w-full max-w-xl rounded-[28px] bg-white p-8 shadow-2xl">
        <div v-if="errorModal" class="mb-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorModal }}
        </div>

        <div class="flex items-start gap-4">
          <div class="flex h-14 w-14 shrink-0 items-center justify-center rounded-full bg-red-100 text-3xl text-red-500">!</div>
          <div>
            <h2 class="text-3xl font-bold text-navy">¿Eliminar pasajero?</h2>
            <p class="mt-2 text-base text-text-muted">
              Se eliminará a
              <strong class="text-navy">
                {{ [pasajeroActivo?.nombrePasajero, pasajeroActivo?.apellidoPasajero].filter(Boolean).join(' ') }}
              </strong>.
              Esta acción no se puede deshacer.
            </p>
          </div>
        </div>

        <div class="mt-8 flex gap-4">
          <button
            type="button"
            class="flex-1 rounded-2xl border border-slate-300 px-5 py-4 font-semibold text-navy hover:bg-slate-50"
            @click="modalEliminar = false"
          >
            Cancelar
          </button>
          <button
            type="button"
            class="flex-1 rounded-2xl bg-red-500 px-5 py-4 font-semibold text-white hover:bg-red-600 disabled:opacity-60"
            :disabled="eliminando"
            @click="confirmarEliminar"
          >
            {{ eliminando ? 'Eliminando...' : 'Eliminar' }}
          </button>
        </div>
      </div>
    </div>

  </section>
</template>
