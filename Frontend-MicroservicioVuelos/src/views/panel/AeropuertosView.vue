<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import { getPaisesApi } from '@/api/paises.api'
import { getCiudadesApi } from '@/api/ciudades.api'
import {
  createAeropuertoAdminApi,
  deleteAeropuertoAdminApi,
  getAeropuertoAdminApi,
  getAeropuertosAdminApi,
  updateAeropuertoAdminApi,
} from '@/api/aeropuertos.api'
import { deepValue, extractItems } from '@/utils/portalCliente'
import { useAutenticacionStore } from '@/stores/autenticacion.store'

const auth = useAutenticacionStore()

const cargando = ref(true)
const guardando = ref(false)
const eliminando = ref(false)
const cargandoCiudades = ref(false)
const cargandoDetalle = ref(false)
const error = ref('')
const errorModal = ref('')
const busqueda = ref('')
const aeropuertos = ref([])
const paises = ref([])
const ciudades = ref([])
const ciudadesFormulario = ref([])
const modalAbierto = ref(false)
const modalEliminar = ref(false)
const modalDetalle = ref(false)
const modo = ref('crear')
const aeropuertoActivo = ref(null)
const aeropuertoDetalle = ref(null)
const errores = ref({})

const form = ref({
  codigo_iata: '',
  codigo_icao: '',
  nombre: '',
  id_pais: '',
  id_ciudad: '',
  zona_horaria: '',
  latitud: '',
  longitud: '',
})

const puedeEliminar = computed(() => auth.rol === 'ADMINISTRADOR')

const opcionesPaises = computed(() =>
  [...paises.value]
    .sort((a, b) => String(a.nombre).localeCompare(String(b.nombre), 'es'))
    .map((pais) => ({
      valor: String(pais.idPais),
      etiqueta: pais.nombre,
    })),
)

const opcionesCiudades = computed(() =>
  [...ciudadesFormulario.value]
    .sort((a, b) => String(a.nombre).localeCompare(String(b.nombre), 'es'))
    .map((ciudad) => ({
      valor: String(ciudad.idCiudad),
      etiqueta: ciudad.nombre,
    })),
)

const aeropuertosFiltrados = computed(() => {
  const termino = busqueda.value.trim().toLowerCase()
  const base = [...aeropuertos.value].sort((a, b) =>
    String(a.nombre).localeCompare(String(b.nombre), 'es'),
  )

  if (!termino) return base

  return base.filter((aeropuerto) =>
    [
      aeropuerto.codigoIata,
      aeropuerto.codigoIcao,
      aeropuerto.nombre,
      aeropuerto.nombreCiudad,
      aeropuerto.nombrePais,
    ]
      .filter(Boolean)
      .some((valor) => String(valor).toLowerCase().includes(termino)),
  )
})

function capitalizarTexto(valor) {
  const limpio = String(valor || '')
    .trim()
    .toLowerCase()
    .replace(/\s+/g, ' ')

  if (!limpio) return ''

  return limpio.replace(/\b\p{L}/gu, (match) => match.toUpperCase())
}

function normalizarPais(item) {
  return {
    idPais: deepValue(item, ['idPais', 'id_pais', 'id']) || null,
    nombre: deepValue(item, ['nombre']) || '',
  }
}

function normalizarCiudad(item) {
  return {
    idCiudad: deepValue(item, ['idCiudad', 'id_ciudad', 'id']) || null,
    idPais: deepValue(item, ['idPais', 'id_pais']) || null,
    nombre: deepValue(item, ['nombre']) || '',
  }
}

function normalizarAeropuerto(item) {
  const idCiudad = deepValue(item, ['idCiudad', 'id_ciudad']) || null
  const idPais = deepValue(item, ['idPais', 'id_pais']) || null
  const ciudad = ciudades.value.find((entry) => String(entry.idCiudad) === String(idCiudad))
  const pais = paises.value.find(
    (entry) => String(entry.idPais) === String(idPais || ciudad?.idPais || ''),
  )

  return {
    idAeropuerto: deepValue(item, ['idAeropuerto', 'id_aeropuerto', 'id']) || null,
    codigoIata: deepValue(item, ['codigoIata', 'codigo_iata']) || '',
    codigoIcao: deepValue(item, ['codigoIcao', 'codigo_icao']) || '',
    nombre: deepValue(item, ['nombre']) || '',
    idCiudad,
    idPais,
    zonaHoraria: deepValue(item, ['zonaHoraria', 'zona_horaria']) || '',
    latitud: deepValue(item, ['latitud']) ?? '',
    longitud: deepValue(item, ['longitud']) ?? '',
    estado: deepValue(item, ['estado']) || '',
    nombreCiudad:
      deepValue(item, ['nombreCiudad', 'nombre_ciudad', 'ciudadNombre']) || ciudad?.nombre || '',
    nombrePais:
      deepValue(item, ['nombrePais', 'nombre_pais', 'paisNombre']) || pais?.nombre || '',
  }
}

function limpiarFormulario() {
  form.value = {
    codigo_iata: '',
    codigo_icao: '',
    nombre: '',
    id_pais: '',
    id_ciudad: '',
    zona_horaria: '',
    latitud: '',
    longitud: '',
  }
  ciudadesFormulario.value = []
  errores.value = {}
  errorModal.value = ''
}

async function cargarCiudadesFormulario(idPais, preservarSeleccion = false) {
  ciudadesFormulario.value = []
  if (!idPais) {
    form.value.id_ciudad = ''
    return
  }

  cargandoCiudades.value = true
  try {
    const respuesta = await getCiudadesApi({ id_pais: Number(idPais), page: 1, page_size: 200 })
    ciudadesFormulario.value = extractItems(respuesta).map(normalizarCiudad)

    if (
      preservarSeleccion &&
      ciudadesFormulario.value.some(
        (ciudad) => String(ciudad.idCiudad) === String(form.value.id_ciudad),
      )
    ) {
      return
    }

    form.value.id_ciudad = ''
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudieron cargar las ciudades del pais.'
  } finally {
    cargandoCiudades.value = false
  }
}

function abrirCrear() {
  modo.value = 'crear'
  aeropuertoActivo.value = null
  limpiarFormulario()
  modalAbierto.value = true
}

async function abrirEditar(aeropuerto) {
  modo.value = 'editar'
  aeropuertoActivo.value = aeropuerto
  form.value = {
    codigo_iata: String(aeropuerto.codigoIata || ''),
    codigo_icao: String(aeropuerto.codigoIcao || ''),
    nombre: String(aeropuerto.nombre || ''),
    id_pais: String(aeropuerto.idPais || ''),
    id_ciudad: String(aeropuerto.idCiudad || ''),
    zona_horaria: String(aeropuerto.zonaHoraria || ''),
    latitud: aeropuerto.latitud === '' ? '' : String(aeropuerto.latitud),
    longitud: aeropuerto.longitud === '' ? '' : String(aeropuerto.longitud),
  }
  errores.value = {}
  errorModal.value = ''
  modalAbierto.value = true
  await cargarCiudadesFormulario(form.value.id_pais, true)
}

function abrirEliminar(aeropuerto) {
  aeropuertoActivo.value = aeropuerto
  errorModal.value = ''
  modalEliminar.value = true
}

async function abrirDetalle(aeropuerto) {
  cargandoDetalle.value = true
  errorModal.value = ''
  aeropuertoDetalle.value = null
  modalDetalle.value = true

  try {
    const respuesta = await getAeropuertoAdminApi(aeropuerto.idAeropuerto)
    aeropuertoDetalle.value = normalizarAeropuerto(deepValue(respuesta, ['data']) || respuesta)
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo cargar el detalle del aeropuerto.'
  } finally {
    cargandoDetalle.value = false
  }
}

function validar() {
  const nuevosErrores = {}
  const iata = form.value.codigo_iata.trim().toUpperCase()
  const icao = form.value.codigo_icao.trim().toUpperCase()

  if (!iata) nuevosErrores.codigo_iata = 'Ingresa el codigo IATA.'
  else if (iata.length !== 3) nuevosErrores.codigo_iata = 'Debe tener 3 letras.'

  if (icao && icao.length !== 4) nuevosErrores.codigo_icao = 'Debe tener 4 letras.'
  if (!form.value.nombre.trim()) nuevosErrores.nombre = 'Ingresa el nombre del aeropuerto.'
  if (!form.value.id_pais) nuevosErrores.id_pais = 'Selecciona el pais.'

  if (form.value.latitud !== '' && Number.isNaN(Number(form.value.latitud))) {
    nuevosErrores.latitud = 'Ingresa una latitud valida.'
  }

  if (form.value.longitud !== '' && Number.isNaN(Number(form.value.longitud))) {
    nuevosErrores.longitud = 'Ingresa una longitud valida.'
  }

  errores.value = nuevosErrores
  return !Object.keys(nuevosErrores).length
}

async function cargarTodo() {
  cargando.value = true
  error.value = ''

  try {
    const [paisesRespuesta, ciudadesRespuesta, aeropuertosRespuesta] = await Promise.all([
      getPaisesApi({ page: 1, page_size: 200 }),
      getCiudadesApi({ page: 1, page_size: 200 }),
      getAeropuertosAdminApi({ page: 1, page_size: 200 }),
    ])

    paises.value = extractItems(paisesRespuesta).map(normalizarPais)
    ciudades.value = extractItems(ciudadesRespuesta).map(normalizarCiudad)
    aeropuertos.value = extractItems(aeropuertosRespuesta).map(normalizarAeropuerto)
  } catch (err) {
    error.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudieron cargar los aeropuertos.'
  } finally {
    cargando.value = false
  }
}

async function guardarAeropuerto() {
  if (guardando.value || !validar()) return
  guardando.value = true
  error.value = ''
  errorModal.value = ''

  const payload = {
    codigoIata: form.value.codigo_iata.trim().toUpperCase(),
    nombre: capitalizarTexto(form.value.nombre),
    idPais: Number(form.value.id_pais),
  }

  if (form.value.codigo_icao.trim()) payload.codigoIcao = form.value.codigo_icao.trim().toUpperCase()
  if (form.value.id_ciudad) payload.idCiudad = Number(form.value.id_ciudad)
  if (form.value.zona_horaria.trim()) payload.zonaHoraria = form.value.zona_horaria.trim()
  if (form.value.latitud !== '') payload.latitud = Number(form.value.latitud)
  if (form.value.longitud !== '') payload.longitud = Number(form.value.longitud)

  try {
    if (modo.value === 'crear') {
      await createAeropuertoAdminApi(payload)
    } else {
      await updateAeropuertoAdminApi(aeropuertoActivo.value.idAeropuerto, payload)
    }

    modalAbierto.value = false
    await cargarTodo()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo guardar el aeropuerto.'
  } finally {
    guardando.value = false
  }
}

async function confirmarEliminar() {
  if (!aeropuertoActivo.value?.idAeropuerto || eliminando.value) return
  eliminando.value = true
  error.value = ''
  errorModal.value = ''

  try {
    await deleteAeropuertoAdminApi(aeropuertoActivo.value.idAeropuerto)
    modalEliminar.value = false
    await cargarTodo()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo eliminar el aeropuerto.'
  } finally {
    eliminando.value = false
  }
}

watch(
  () => form.value.id_pais,
  async (nuevoPais, anteriorPais) => {
    if (!modalAbierto.value || nuevoPais === anteriorPais) return
    await cargarCiudadesFormulario(nuevoPais)
  },
)

onMounted(cargarTodo)
</script>

<template>
  <section class="space-y-5">
    <div class="flex items-start justify-between gap-4">
      <div>
        <h1 class="text-[2rem] font-bold text-navy">Gestion de Aeropuertos</h1>
        <p class="mt-1.5 text-sm text-text-muted">Administra la red de aeropuertos (Admin y Aerolinea).</p>
      </div>

      <button
        type="button"
        class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy transition-colors hover:bg-gold-light"
        @click="abrirCrear"
      >
        + Nuevo Aeropuerto
      </button>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <section class="rounded-[24px] bg-white p-4 shadow-sm">
      <InputApp v-model="busqueda" placeholder="Buscar por codigo IATA, nombre, ciudad o pais..." />
    </section>

    <section class="overflow-hidden rounded-[24px] bg-white shadow-sm">
      <div class="grid grid-cols-[0.9fr_2fr_1.1fr_1.1fr_140px] gap-4 bg-slate-50 px-5 py-3.5 text-sm font-semibold text-navy">
        <span>Codigo IATA</span>
        <span>Nombre del Aeropuerto</span>
        <span>Ciudad</span>
        <span>Pais</span>
        <span class="text-right">Acciones</span>
      </div>

      <div v-if="cargando" class="px-5 py-5 text-sm text-text-muted">Cargando aeropuertos...</div>
      <div v-else-if="!aeropuertosFiltrados.length" class="px-5 py-5 text-sm text-text-muted">No hay aeropuertos registrados.</div>

      <div v-else class="divide-y divide-slate-100">
        <div
          v-for="aeropuerto in aeropuertosFiltrados"
          :key="aeropuerto.idAeropuerto"
          class="grid grid-cols-[0.9fr_2fr_1.1fr_1.1fr_140px] items-center gap-4 px-5 py-4"
        >
          <div class="flex items-center gap-3">
            <div class="flex h-11 w-11 items-center justify-center rounded-2xl bg-slate-100 text-navy">
              <span class="text-lg">📍</span>
            </div>
            <span class="text-lg font-semibold text-navy">{{ aeropuerto.codigoIata }}</span>
          </div>

          <span class="text-sm font-medium text-navy">{{ aeropuerto.nombre }}</span>
          <span class="text-sm text-navy">{{ aeropuerto.nombreCiudad || '-' }}</span>
          <span class="text-sm text-navy">{{ aeropuerto.nombrePais || '-' }}</span>

          <div class="flex justify-end gap-3 text-base">
            <button type="button" class="text-slate-500 hover:text-navy" @click="abrirDetalle(aeropuerto)">👁</button>
            <button type="button" class="text-slate-500 hover:text-navy" @click="abrirEditar(aeropuerto)">✎</button>
            <button
              v-if="puedeEliminar"
              type="button"
              class="text-red-500 hover:text-red-600"
              @click="abrirEliminar(aeropuerto)"
            >
              🗑
            </button>
          </div>
        </div>
      </div>
    </section>

    <div v-if="modalDetalle" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalDetalle = false">
      <div class="w-full max-w-3xl rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">Detalle del Aeropuerto</h2>

        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorModal }}
        </div>

        <div v-else-if="cargandoDetalle" class="mt-6 text-sm text-text-muted">Cargando detalle...</div>

        <div v-else-if="aeropuertoDetalle" class="mt-6 grid gap-5 md:grid-cols-2">
          <InputApp :model-value="aeropuertoDetalle.codigoIata || '-'" label="Codigo IATA" disabled />
          <InputApp :model-value="aeropuertoDetalle.codigoIcao || '-'" label="Codigo ICAO" disabled />
          <InputApp :model-value="aeropuertoDetalle.nombre || '-'" label="Nombre" disabled />
          <InputApp :model-value="aeropuertoDetalle.estado || '-'" label="Estado" disabled />
          <InputApp :model-value="aeropuertoDetalle.nombrePais || '-'" label="Pais" disabled />
          <InputApp :model-value="aeropuertoDetalle.nombreCiudad || '-'" label="Ciudad" disabled />
          <InputApp :model-value="aeropuertoDetalle.zonaHoraria || '-'" label="Zona horaria" disabled />
          <InputApp :model-value="aeropuertoDetalle.latitud === '' ? '-' : String(aeropuertoDetalle.latitud)" label="Latitud" disabled />
          <InputApp :model-value="aeropuertoDetalle.longitud === '' ? '-' : String(aeropuertoDetalle.longitud)" label="Longitud" disabled />
        </div>

        <div class="mt-8 flex justify-end">
          <button
            type="button"
            class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50"
            @click="modalDetalle = false"
          >
            Cerrar
          </button>
        </div>
      </div>
    </div>

    <div v-if="modalAbierto" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalAbierto = false">
      <div class="w-full max-w-4xl rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">{{ modo === 'crear' ? 'Nuevo Aeropuerto' : 'Editar Aeropuerto' }}</h2>

        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorModal }}
        </div>

        <div class="mt-6 grid gap-5 md:grid-cols-2">
          <InputApp v-model="form.codigo_iata" label="Codigo IATA" :error="errores.codigo_iata" requerido />
          <InputApp v-model="form.codigo_icao" label="Codigo ICAO" :error="errores.codigo_icao" />
          <InputApp
            v-model="form.nombre"
            label="Nombre"
            :error="errores.nombre"
            requerido
            @update:model-value="form.nombre = capitalizarTexto($event)"
          />
          <SelectApp v-model="form.id_pais" label="Pais" :opciones="opcionesPaises" :error="errores.id_pais" requerido />
          <SelectApp
            v-model="form.id_ciudad"
            label="Ciudad"
            :opciones="opcionesCiudades"
            :error="errores.id_ciudad"
            :cargando="cargandoCiudades"
          />
          <InputApp v-model="form.zona_horaria" label="Zona horaria" placeholder="America/Guayaquil" />
          <InputApp v-model="form.latitud" label="Latitud" :error="errores.latitud" />
          <InputApp v-model="form.longitud" label="Longitud" :error="errores.longitud" />
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
            :disabled="guardando"
            @click="guardarAeropuerto"
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
            <h2 class="text-3xl font-bold text-navy">Eliminar aeropuerto?</h2>
            <p class="mt-2 text-base text-text-muted">Esta accion no se puede deshacer.</p>
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
