<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import { createClienteApi, deleteClienteApi, getClientesApi, updateClienteApi } from '@/api/clientes.api'
import { getCiudadesApi } from '@/api/ciudades.api'
import { getPaisesApi } from '@/api/paises.api'
import { usePanelPermisos } from '@/composables/usePanelPermisos'
import { deepValue, extractItems } from '@/utils/portalCliente'

const TIPOS_IDENTIFICACION = ['CEDULA', 'PASAPORTE', 'RUC', 'OTRO']
const GENEROS = ['MASCULINO', 'FEMENINO', 'OTRO']

const cargando = ref(true)
const guardando = ref(false)
const eliminando = ref(false)
const error = ref('')
const errorModal = ref('')
const busqueda = ref('')
const estadoFiltro = ref('')
const clientes = ref([])
const ciudades = ref([])
const paises = ref([])
const modalAbierto = ref(false)
const modalEliminar = ref(false)
const modalDetalle = ref(false)
const modo = ref('crear')
const clienteActivo = ref(null)
const errores = ref({})
const yaValidado = ref(false)

const form = ref({
  tipo_identificacion: '',
  numero_identificacion: '',
  nombres: '',
  apellidos: '',
  razon_social: '',
  correo: '',
  telefono: '',
  direccion: '',
  id_ciudad_residencia: '',
  id_pais_nacionalidad: '',
  fecha_nacimiento: '',
  nacionalidad: '',
  genero: '',
  estado: '',
})

const esRUC = computed(() => form.value.tipo_identificacion === 'RUC')
const { puedeEliminar } = usePanelPermisos()

const opcionesEstado = [
  { valor: 'ACT', etiqueta: 'Activo' },
  { valor: 'INA', etiqueta: 'Inactivo' },
]

const opcionesTipoIdentificacion = computed(() =>
  TIPOS_IDENTIFICACION.map((item) => ({ valor: item, etiqueta: item })),
)

const opcionesGenero = computed(() =>
  GENEROS.map((item) => ({ valor: item, etiqueta: capitalizarTexto(item) })),
)

const opcionesPaises = computed(() =>
  [...paises.value]
    .sort((a, b) => String(a.nombre).localeCompare(String(b.nombre), 'es'))
    .map((pais) => ({ valor: String(pais.idPais), etiqueta: pais.nombre })),
)

const opcionesCiudades = computed(() =>
  [...ciudades.value]
    .sort((a, b) => String(a.nombre).localeCompare(String(b.nombre), 'es'))
    .map((ciudad) => ({
      valor: String(ciudad.idCiudad),
      etiqueta: ciudad.nombrePais ? `${ciudad.nombre} · ${ciudad.nombrePais}` : ciudad.nombre,
    })),
)

const clientesFiltrados = computed(() => {
  const termino = busqueda.value.trim().toLowerCase()
  const estado = estadoFiltro.value.trim().toUpperCase()
  const base = [...clientes.value].sort((a, b) => String(a.nombres).localeCompare(String(b.nombres), 'es'))

  return base.filter((cliente) => {
    const matchBusqueda =
      !termino ||
      String(cliente.nombres).toLowerCase().includes(termino) ||
      String(cliente.apellidos).toLowerCase().includes(termino) ||
      String(cliente.numeroIdentificacion).toLowerCase().includes(termino) ||
      String(cliente.correo).toLowerCase().includes(termino)

    const estadoCliente = String(cliente.estado || '').toUpperCase()
    const matchEstado =
      !estado ||
      estadoCliente === estado ||
      (estado === 'ACTIVO' && estadoCliente === 'ACT') ||
      (estado === 'INACTIVO' && estadoCliente === 'INA')

    return matchBusqueda && matchEstado
  })
})

function capitalizarTexto(valor) {
  const limpio = String(valor || '').trim().toLowerCase().replace(/\s+/g, ' ')
  if (!limpio) return ''
  return limpio.replace(/\b\p{L}/gu, (match) => match.toUpperCase())
}

function estadoAmigable(estado) {
  const valor = String(estado || '').trim().toUpperCase()
  if (valor === 'ACT' || valor === 'ACTIVO') return 'Activo'
  if (valor === 'INA' || valor === 'INACTIVO') return 'Inactivo'
  return valor || '-'
}

function esActivo(estado) {
  const valor = String(estado || '').trim().toUpperCase()
  return valor === 'ACT' || valor === 'ACTIVO'
}

function normalizarEstadoFormulario(estado) {
  const valor = String(estado || '').trim().toUpperCase()
  if (valor === 'INA' || valor === 'INACTIVO') return 'INA'
  return 'ACT'
}

function formatearFecha(fecha) {
  if (!fecha) return '-'
  const d = new Date(`${String(fecha).slice(0, 10)}T00:00:00`)
  if (isNaN(d.getTime())) return String(fecha).slice(0, 10)
  return d.toLocaleDateString('es-ES', { day: 'numeric', month: 'long', year: 'numeric' })
}

function normalizarPais(item) {
  return {
    idPais: deepValue(item, ['idPais', 'id_pais', 'id']) || null,
    nombre: deepValue(item, ['nombre']) || '',
  }
}

function normalizarCiudad(item) {
  const idPais = deepValue(item, ['idPais', 'id_pais']) || null
  return {
    idCiudad: deepValue(item, ['idCiudad', 'id_ciudad', 'id']) || null,
    nombre: deepValue(item, ['nombre']) || '',
    idPais,
    nombrePais:
      deepValue(item, ['nombrePais', 'nombre_pais', 'paisNombre']) ||
      paises.value.find((pais) => String(pais.idPais) === String(idPais))?.nombre ||
      '',
  }
}

function normalizarCliente(item) {
  const idCiudadResidencia = deepValue(item, ['idCiudadResidencia', 'id_ciudad_residencia']) || null
  const idPaisNacionalidad = deepValue(item, ['idPaisNacionalidad', 'id_pais_nacionalidad']) || null
  const nombres = deepValue(item, ['nombres']) || ''
  const apellidos = deepValue(item, ['apellidos']) || ''

  return {
    idCliente: deepValue(item, ['idCliente', 'id_cliente', 'id']) || null,
    tipoIdentificacion: deepValue(item, ['tipoIdentificacion', 'tipo_identificacion']) || '',
    numeroIdentificacion: deepValue(item, ['numeroIdentificacion', 'numero_identificacion']) || '',
    nombres,
    apellidos,
    razonSocial: deepValue(item, ['razonSocial', 'razon_social']) || '',
    nombreCompleto: [nombres, apellidos].filter(Boolean).join(' ').trim(),
    correo: deepValue(item, ['correo']) || '',
    telefono: deepValue(item, ['telefono']) || '',
    direccion: deepValue(item, ['direccion']) || '',
    idCiudadResidencia,
    idPaisNacionalidad,
    fechaNacimiento: deepValue(item, ['fechaNacimiento', 'fecha_nacimiento']) || '',
    nacionalidad: deepValue(item, ['nacionalidad']) || '',
    genero: deepValue(item, ['genero']) || '',
    estado: deepValue(item, ['estado']) || '',
    ciudadResidencia:
      deepValue(item, ['nombreCiudadResidencia', 'nombre_ciudad_residencia']) ||
      ciudades.value.find((ciudad) => String(ciudad.idCiudad) === String(idCiudadResidencia))?.nombre ||
      '',
    paisNacionalidad:
      deepValue(item, ['nombrePaisNacionalidad', 'nombre_pais_nacionalidad']) ||
      paises.value.find((pais) => String(pais.idPais) === String(idPaisNacionalidad))?.nombre ||
      '',
  }
}

function limpiarFormulario() {
  form.value = {
    tipo_identificacion: '',
    numero_identificacion: '',
    nombres: '',
    apellidos: '',
    razon_social: '',
    correo: '',
    telefono: '',
    direccion: '',
    id_ciudad_residencia: '',
    id_pais_nacionalidad: '',
    fecha_nacimiento: '',
    nacionalidad: '',
    genero: '',
    estado: '',
  }
  errores.value = {}
  errorModal.value = ''
  yaValidado.value = false
}

function abrirDetalle(cliente) {
  clienteActivo.value = cliente
  modalDetalle.value = true
}

function abrirCrear() {
  modo.value = 'crear'
  clienteActivo.value = null
  limpiarFormulario()
  modalAbierto.value = true
}

function abrirEditar(cliente) {
  modo.value = 'editar'
  clienteActivo.value = cliente
  form.value = {
    tipo_identificacion: String(cliente.tipoIdentificacion || ''),
    numero_identificacion: String(cliente.numeroIdentificacion || ''),
    nombres: String(cliente.nombres || ''),
    apellidos: String(cliente.apellidos || ''),
    razon_social: String(cliente.razonSocial || ''),
    correo: String(cliente.correo || ''),
    telefono: String(cliente.telefono || ''),
    direccion: String(cliente.direccion || ''),
    id_ciudad_residencia: String(cliente.idCiudadResidencia || ''),
    id_pais_nacionalidad: String(cliente.idPaisNacionalidad || ''),
    fecha_nacimiento: cliente.fechaNacimiento ? String(cliente.fechaNacimiento).slice(0, 10) : '',
    nacionalidad: String(cliente.nacionalidad || ''),
    genero: String(cliente.genero || ''),
    estado: normalizarEstadoFormulario(cliente.estado),
  }
  errores.value = {}
  errorModal.value = ''
  modalAbierto.value = true
}

function abrirEliminar(cliente) {
  clienteActivo.value = cliente
  errorModal.value = ''
  modalEliminar.value = true
}

function validar() {
  const nuevosErrores = {}
  const correo = String(form.value.correo || '').trim()
  const fechaNacimiento = String(form.value.fecha_nacimiento || '').trim()
  const genero = String(form.value.genero || '').trim().toUpperCase()
  const GENEROS_VALIDOS = ['MASCULINO', 'FEMENINO', 'OTRO']

  if (!form.value.tipo_identificacion) nuevosErrores.tipo_identificacion = 'Selecciona el tipo de identificación.'
  if (!String(form.value.numero_identificacion || '').trim()) nuevosErrores.numero_identificacion = 'Ingresa el número de identificación.'
  if (!esRUC.value && !String(form.value.nombres || '').trim()) nuevosErrores.nombres = 'Ingresa los nombres.'
  if (esRUC.value && !String(form.value.razon_social || '').trim()) nuevosErrores.razon_social = 'La razón social es obligatoria para RUC.'
  if (!correo) nuevosErrores.correo = 'Ingresa el correo.'
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(correo)) nuevosErrores.correo = 'Ingresa un correo válido.'
  if (!String(form.value.telefono || '').trim()) nuevosErrores.telefono = 'Ingresa el teléfono.'
  if (!String(form.value.direccion || '').trim()) nuevosErrores.direccion = 'Ingresa la dirección.'
  if (!form.value.id_ciudad_residencia) nuevosErrores.id_ciudad_residencia = 'Selecciona la ciudad de residencia.'
  if (!form.value.id_pais_nacionalidad) nuevosErrores.id_pais_nacionalidad = 'Selecciona el país / nacionalidad.'
  if (modo.value === 'editar' && !form.value.estado) nuevosErrores.estado = 'Selecciona el estado.'
  if (genero && !GENEROS_VALIDOS.includes(genero)) nuevosErrores.genero = 'Género no válido.'
  if (fechaNacimiento) {
    const hoy = new Date()
    const fecha = new Date(`${fechaNacimiento}T00:00:00`)
    if (fecha >= new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate())) {
      nuevosErrores.fecha_nacimiento = 'La fecha de nacimiento debe ser pasada.'
    }
  }

  errores.value = nuevosErrores
  return !Object.keys(nuevosErrores).length
}

async function cargarCatalogos() {
  const [respuestaPaises, respuestaCiudades] = await Promise.all([
    getPaisesApi({ page: 1, page_size: 200 }),
    getCiudadesApi({ page: 1, page_size: 200 }),
  ])
  paises.value = extractItems(respuestaPaises).map(normalizarPais)
  ciudades.value = extractItems(respuestaCiudades).map(normalizarCiudad)
}

async function cargarClientes() {
  cargando.value = true
  error.value = ''
  try {
    const respuesta = await getClientesApi({ page: 1, page_size: 200 })
    clientes.value = extractItems(respuesta).map(normalizarCliente)
  } catch (err) {
    error.value = err.response?.data?.errors?.join(' ') || err.response?.data?.message || 'No se pudieron cargar los clientes.'
  } finally {
    cargando.value = false
  }
}

async function guardarCliente() {
  yaValidado.value = true
  if (guardando.value || !validar()) return
  guardando.value = true
  errorModal.value = ''

  const payload = {
    tipoIdentificacion: String(form.value.tipo_identificacion || '').trim().toUpperCase(),
    numeroIdentificacion: String(form.value.numero_identificacion || '').trim(),
    nombres: capitalizarTexto(form.value.nombres),
    apellidos: capitalizarTexto(form.value.apellidos),
    correo: String(form.value.correo || '').trim().toLowerCase(),
    telefono: String(form.value.telefono || '').trim(),
    direccion: String(form.value.direccion || '').trim(),
    idCiudadResidencia: Number(form.value.id_ciudad_residencia),
    idPaisNacionalidad: Number(form.value.id_pais_nacionalidad),
  }

  if (form.value.razon_social) payload.razonSocial = String(form.value.razon_social || '').trim()
  if (form.value.fecha_nacimiento) payload.fechaNacimiento = form.value.fecha_nacimiento
  if (form.value.nacionalidad) payload.nacionalidad = String(form.value.nacionalidad || '').trim()
  if (form.value.genero) payload.genero = String(form.value.genero || '').trim().toUpperCase()
  if (modo.value === 'editar') payload.estado = normalizarEstadoFormulario(form.value.estado)
  if (!payload.apellidos) delete payload.apellidos

  try {
    if (modo.value === 'crear') {
      await createClienteApi(payload)
    } else {
      await updateClienteApi(clienteActivo.value.idCliente, payload)
    }
    modalAbierto.value = false
    await cargarClientes()
  } catch (err) {
    errorModal.value = err.response?.data?.errors?.join(' ') || err.response?.data?.message || 'No se pudo guardar el cliente.'
  } finally {
    guardando.value = false
  }
}

async function confirmarEliminar() {
  if (!clienteActivo.value?.idCliente || eliminando.value) return
  eliminando.value = true
  errorModal.value = ''
  try {
    await deleteClienteApi(clienteActivo.value.idCliente)
    modalEliminar.value = false
    await cargarClientes()
  } catch (err) {
    errorModal.value = err.response?.data?.errors?.join(' ') || err.response?.data?.message || 'No se pudo eliminar el cliente.'
  } finally {
    eliminando.value = false
  }
}

watch(
  () => form.value.nombres,
  (valor) => {
    form.value.nombres = capitalizarTexto(valor)
  },
)

watch(
  () => form.value.apellidos,
  (valor) => {
    form.value.apellidos = capitalizarTexto(valor)
  },
)

watch(
  form,
  () => {
    if (yaValidado.value) validar()
  },
  { deep: true },
)

onMounted(async () => {
  await cargarCatalogos()
  await cargarClientes()
})
</script>

<template>
  <section class="space-y-5">
    <div class="flex items-start justify-between gap-4">
      <div>
        <h1 class="text-[2rem] font-bold text-navy">Gestión de Clientes</h1>
        <p class="mt-1.5 text-sm text-text-muted">Administra los clientes del sistema sin mezclarlos con pasajeros.</p>
      </div>

      <button
        type="button"
        class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy transition-colors hover:bg-gold-light"
        @click="abrirCrear"
      >
        + Nuevo Cliente
      </button>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <section class="grid gap-4 rounded-[24px] bg-white p-4 shadow-sm md:grid-cols-[1fr_220px]">
      <InputApp v-model="busqueda" placeholder="Buscar por nombres, apellidos, identificación o correo..." />
      <SelectApp
        v-model="estadoFiltro"
        :opciones="[
          { valor: '', etiqueta: 'Todos los estados' },
          { valor: 'ACT', etiqueta: 'Activo' },
          { valor: 'INA', etiqueta: 'Inactivo' },
        ]"
      />
    </section>

    <section class="rounded-[24px] bg-white shadow-sm">
      <div class="overflow-x-auto">
        <div class="min-w-[900px]">
          <div class="grid grid-cols-[1fr_1fr_1.3fr_1.6fr_1fr_1.2fr_1.2fr_0.7fr_88px] gap-3 bg-slate-50 px-5 py-3.5 text-sm font-semibold text-navy">
            <span class="whitespace-nowrap">Nombres</span>
            <span class="whitespace-nowrap">Apellidos</span>
            <span class="whitespace-nowrap">N° identificación</span>
            <span class="whitespace-nowrap">Correo</span>
            <span class="whitespace-nowrap">Teléfono</span>
            <span class="whitespace-nowrap">Ciudad residencia</span>
            <span class="whitespace-nowrap">País / Nac.</span>
            <span class="whitespace-nowrap">Estado</span>
            <span class="whitespace-nowrap text-right">Acciones</span>
          </div>

          <div v-if="cargando" class="px-5 py-5 text-sm text-text-muted">Cargando clientes...</div>
          <div v-else-if="!clientesFiltrados.length" class="px-5 py-5 text-sm text-text-muted">No hay clientes registrados.</div>

          <div v-else class="divide-y divide-slate-100">
            <div
              v-for="cliente in clientesFiltrados"
              :key="cliente.idCliente"
              class="grid grid-cols-[1fr_1fr_1.3fr_1.6fr_1fr_1.2fr_1.2fr_0.7fr_88px] items-center gap-3 px-5 py-4"
            >
              <span class="truncate text-sm text-navy" :title="cliente.nombres">{{ cliente.nombres || '-' }}</span>
              <span class="truncate text-sm text-navy" :title="cliente.apellidos">{{ cliente.apellidos || '-' }}</span>
              <span class="truncate text-sm text-navy" :title="cliente.numeroIdentificacion">{{ cliente.numeroIdentificacion || '-' }}</span>
              <span class="truncate text-sm text-navy" :title="cliente.correo">{{ cliente.correo || '-' }}</span>
              <span class="truncate text-sm text-navy">{{ cliente.telefono || '-' }}</span>
              <span class="truncate text-sm text-navy" :title="cliente.ciudadResidencia">{{ cliente.ciudadResidencia || '-' }}</span>
              <span class="truncate text-sm text-navy" :title="cliente.paisNacionalidad">{{ cliente.paisNacionalidad || '-' }}</span>
              <span class="truncate text-sm text-navy">{{ estadoAmigable(cliente.estado) }}</span>

              <div class="flex justify-end gap-3 text-base">
                <button type="button" class="text-slate-500 hover:text-navy" title="Ver" @click="abrirDetalle(cliente)">👁</button>
                <button type="button" class="text-slate-500 hover:text-navy" title="Editar" @click="abrirEditar(cliente)">✎</button>
                <button v-if="puedeEliminar" type="button" class="text-red-500 hover:text-red-600" title="Eliminar" @click="abrirEliminar(cliente)">🗑</button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <div v-if="modalDetalle" class="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-950/50 px-4 py-10" @click.self="modalDetalle = false">
      <div class="w-full max-w-3xl rounded-[28px] bg-white shadow-2xl">

        <!-- Encabezado -->
        <div class="flex items-start justify-between gap-4 rounded-t-[28px] bg-navy px-8 py-7">
          <div>
            <p class="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Cliente</p>
            <h2 class="mt-1 text-2xl font-bold text-white">
              {{ clienteActivo?.razonSocial || clienteActivo?.nombreCompleto || '-' }}
            </h2>
            <p v-if="clienteActivo?.razonSocial && clienteActivo?.nombreCompleto" class="mt-0.5 text-sm text-slate-300">
              {{ clienteActivo.nombreCompleto }}
            </p>
          </div>
          <span
            class="mt-1 shrink-0 rounded-full px-3 py-1 text-xs font-semibold"
            :class="esActivo(clienteActivo?.estado) ? 'bg-emerald-500/20 text-emerald-300' : 'bg-red-500/20 text-red-300'"
          >
            {{ estadoAmigable(clienteActivo?.estado) }}
          </span>
        </div>

        <div class="space-y-6 p-8">

          <!-- Identificación -->
          <div>
            <p class="mb-3 text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Identificación</p>
            <div class="grid gap-3 md:grid-cols-2">
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Tipo</p>
                <p class="mt-1 font-semibold text-navy">{{ clienteActivo?.tipoIdentificacion || '-' }}</p>
              </div>
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Número</p>
                <p class="mt-1 font-semibold text-navy">{{ clienteActivo?.numeroIdentificacion || '-' }}</p>
              </div>
              <div v-if="clienteActivo?.razonSocial" class="rounded-2xl bg-slate-50 p-4 md:col-span-2">
                <p class="text-xs text-text-muted">Razón social</p>
                <p class="mt-1 font-semibold text-navy">{{ clienteActivo.razonSocial }}</p>
              </div>
            </div>
          </div>

          <!-- Contacto -->
          <div>
            <p class="mb-3 text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Contacto</p>
            <div class="grid gap-3 md:grid-cols-2">
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Correo</p>
                <p class="mt-1 font-semibold text-navy">{{ clienteActivo?.correo || '-' }}</p>
              </div>
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Teléfono</p>
                <p class="mt-1 font-semibold text-navy">{{ clienteActivo?.telefono || '-' }}</p>
              </div>
              <div class="rounded-2xl bg-slate-50 p-4 md:col-span-2">
                <p class="text-xs text-text-muted">Dirección</p>
                <p class="mt-1 font-semibold text-navy">{{ clienteActivo?.direccion || '-' }}</p>
              </div>
            </div>
          </div>

          <!-- Ubicación -->
          <div>
            <p class="mb-3 text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Ubicación</p>
            <div class="grid gap-3 md:grid-cols-2">
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Ciudad de residencia</p>
                <p class="mt-1 font-semibold text-navy">{{ clienteActivo?.ciudadResidencia || '-' }}</p>
              </div>
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">País / Nacionalidad</p>
                <p class="mt-1 font-semibold text-navy">{{ clienteActivo?.paisNacionalidad || '-' }}</p>
              </div>
            </div>
          </div>

          <!-- Datos personales -->
          <div>
            <p class="mb-3 text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">Datos personales</p>
            <div class="grid gap-3 md:grid-cols-3">
              <div class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Fecha de nacimiento</p>
                <p class="mt-1 font-semibold text-navy">{{ formatearFecha(clienteActivo?.fechaNacimiento) }}</p>
              </div>
              <div v-if="clienteActivo?.nacionalidad" class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Nacionalidad</p>
                <p class="mt-1 font-semibold text-navy">{{ clienteActivo.nacionalidad }}</p>
              </div>
              <div v-if="clienteActivo?.genero" class="rounded-2xl bg-slate-50 p-4">
                <p class="text-xs text-text-muted">Género</p>
                <p class="mt-1 font-semibold text-navy">{{ capitalizarTexto(clienteActivo.genero) }}</p>
              </div>
            </div>
          </div>

        </div>

        <!-- Pie -->
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
            @click="() => { modalDetalle = false; abrirEditar(clienteActivo) }"
          >
            Editar
          </button>
        </div>
      </div>
    </div>

    <div v-if="modalAbierto" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalAbierto = false">
      <div class="max-h-[90vh] w-full max-w-5xl overflow-y-auto rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">{{ modo === 'crear' ? 'Nuevo Cliente' : 'Editar Cliente' }}</h2>

        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorModal }}
        </div>

        <div class="mt-6 grid gap-5 md:grid-cols-2">
          <SelectApp v-model="form.tipo_identificacion" label="Tipo de identificación" :opciones="opcionesTipoIdentificacion" :error="errores.tipo_identificacion" requerido />
          <InputApp v-model="form.numero_identificacion" label="Número de identificación" :error="errores.numero_identificacion" requerido />
          <InputApp v-model="form.nombres" label="Nombres" :error="errores.nombres" requerido />
          <InputApp v-model="form.apellidos" label="Apellidos" />
          <div v-if="esRUC" class="md:col-span-2">
            <InputApp v-model="form.razon_social" label="Razón social" :error="errores.razon_social" :requerido="esRUC" />
          </div>
          <InputApp v-model="form.correo" label="Correo" :error="errores.correo" requerido />
          <InputApp v-model="form.telefono" label="Teléfono" :error="errores.telefono" requerido />
          <div class="md:col-span-2">
            <InputApp v-model="form.direccion" label="Dirección" :error="errores.direccion" requerido />
          </div>
          <SelectApp v-model="form.id_ciudad_residencia" label="Ciudad de residencia" :opciones="opcionesCiudades" :error="errores.id_ciudad_residencia" requerido />
          <SelectApp v-model="form.id_pais_nacionalidad" label="País / Nacionalidad" :opciones="opcionesPaises" :error="errores.id_pais_nacionalidad" requerido />
          <InputApp v-model="form.fecha_nacimiento" label="Fecha de nacimiento" tipo="date" :error="errores.fecha_nacimiento" />
          <InputApp v-model="form.nacionalidad" label="Nacionalidad" />
          <SelectApp v-model="form.genero" label="Género" :opciones="opcionesGenero" />
          <SelectApp v-if="modo === 'editar'" v-model="form.estado" label="Estado" :opciones="opcionesEstado" :error="errores.estado" requerido />
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
            @click="guardarCliente"
          >
            {{ guardando ? 'Guardando...' : 'Guardar' }}
          </button>
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
            <h2 class="text-3xl font-bold text-navy">¿Eliminar cliente?</h2>
            <p class="mt-2 text-base text-text-muted">Esta acción no se puede deshacer.</p>
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
