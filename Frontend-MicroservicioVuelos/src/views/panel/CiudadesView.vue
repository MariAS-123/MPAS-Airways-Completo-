<script setup>
import { computed, onMounted, ref } from 'vue'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import { createCiudadApi, deleteCiudadApi, getCiudadesApi, updateCiudadApi } from '@/api/ciudades.api'
import { getPaisesApi } from '@/api/paises.api'
import { deepValue, extractItems } from '@/utils/portalCliente'

const cargando = ref(true)
const guardando = ref(false)
const eliminando = ref(false)
const error = ref('')
const errorModal = ref('')
const busqueda = ref('')
const ciudades = ref([])
const paises = ref([])
const modalAbierto = ref(false)
const modalEliminar = ref(false)
const modo = ref('crear')
const ciudadActiva = ref(null)
const errores = ref({})

const form = ref({
  id_pais: '',
  nombre: '',
  zona_horaria: '',
  codigo_postal: '',
})

const opcionesPaises = computed(() =>
  [...paises.value]
    .sort((a, b) => String(a.nombre).localeCompare(String(b.nombre), 'es'))
    .map((pais) => ({
      valor: String(pais.idPais),
      etiqueta: pais.nombre,
    })),
)

const ciudadesFiltradas = computed(() => {
  const termino = busqueda.value.trim().toLowerCase()
  const base = [...ciudades.value].sort((a, b) => String(a.nombre).localeCompare(String(b.nombre), 'es'))

  if (!termino) return base

  return base.filter((ciudad) => {
    const nombreCiudad = String(ciudad.nombre || '').toLowerCase()
    const nombrePais = String(ciudad.nombrePais || '').toLowerCase()
    const zona = String(ciudad.zonaHoraria || '').toLowerCase()
    const codigoPostal = String(ciudad.codigoPostal || '').toLowerCase()
    const estado = String(ciudad.estado || '').toLowerCase()
    return (
      nombreCiudad.includes(termino) ||
      nombrePais.includes(termino) ||
      zona.includes(termino) ||
      codigoPostal.includes(termino) ||
      estado.includes(termino)
    )
  })
})

function capitalizarTexto(valor) {
  const limpio = String(valor || '').trim().toLowerCase().replace(/\s+/g, ' ')
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
  const idPais = deepValue(item, ['idPais', 'id_pais']) || null
  return {
    idCiudad: deepValue(item, ['idCiudad', 'id_ciudad', 'id']) || null,
    idPais,
    nombre: deepValue(item, ['nombre']) || '',
    zonaHoraria: deepValue(item, ['zonaHoraria', 'zona_horaria']) || '',
    codigoPostal: deepValue(item, ['codigoPostal', 'codigo_postal']) || '',
    estado: deepValue(item, ['estado']) || '',
    nombrePais:
      deepValue(item, ['nombrePais', 'nombre_pais', 'paisNombre']) ||
      paises.value.find((pais) => String(pais.idPais) === String(idPais))?.nombre ||
      '',
  }
}

function limpiarFormulario() {
  form.value = {
    id_pais: '',
    nombre: '',
    zona_horaria: '',
    codigo_postal: '',
  }
  errores.value = {}
  errorModal.value = ''
}

function abrirCrear() {
  modo.value = 'crear'
  ciudadActiva.value = null
  limpiarFormulario()
  modalAbierto.value = true
}

function abrirEditar(ciudad) {
  modo.value = 'editar'
  ciudadActiva.value = ciudad
  form.value = {
    id_pais: String(ciudad.idPais || ''),
    nombre: String(ciudad.nombre || ''),
    zona_horaria: String(ciudad.zonaHoraria || ''),
    codigo_postal: String(ciudad.codigoPostal || ''),
  }
  errores.value = {}
  errorModal.value = ''
  modalAbierto.value = true
}

function abrirEliminar(ciudad) {
  ciudadActiva.value = ciudad
  errorModal.value = ''
  modalEliminar.value = true
}

function validar() {
  const nuevosErrores = {}
  if (!form.value.id_pais) nuevosErrores.id_pais = 'Selecciona el pais.'
  if (!form.value.nombre.trim()) nuevosErrores.nombre = 'Ingresa el nombre de la ciudad.'

  errores.value = nuevosErrores
  return !Object.keys(nuevosErrores).length
}

async function cargarTodo() {
  cargando.value = true
  error.value = ''

  try {
    const [paisesRespuesta, ciudadesRespuesta] = await Promise.all([
      getPaisesApi({ page: 1, page_size: 200 }),
      getCiudadesApi({ page: 1, page_size: 200 }),
    ])

    paises.value = extractItems(paisesRespuesta).map(normalizarPais)
    ciudades.value = extractItems(ciudadesRespuesta).map(normalizarCiudad)
  } catch (err) {
    error.value = err.response?.data?.errors?.join(' ') || err.response?.data?.message || 'No se pudieron cargar las ciudades.'
  } finally {
    cargando.value = false
  }
}

async function guardarCiudad() {
  if (guardando.value || !validar()) return
  guardando.value = true
  error.value = ''
  errorModal.value = ''

  const payload = {
    idPais: Number(form.value.id_pais),
    nombre: capitalizarTexto(form.value.nombre),
  }

  if (form.value.zona_horaria.trim()) {
    payload.zonaHoraria = form.value.zona_horaria.trim()
  }

  if (form.value.codigo_postal.trim()) {
    payload.codigoPostal = form.value.codigo_postal.trim()
  }

  try {
    if (modo.value === 'crear') {
      await createCiudadApi(payload)
    } else {
      await updateCiudadApi(ciudadActiva.value.idCiudad, payload)
    }

    modalAbierto.value = false
    await cargarTodo()
  } catch (err) {
    errorModal.value = err.response?.data?.errors?.join(' ') || err.response?.data?.message || 'No se pudo guardar la ciudad.'
  } finally {
    guardando.value = false
  }
}

async function confirmarEliminar() {
  if (!ciudadActiva.value?.idCiudad || eliminando.value) return
  eliminando.value = true
  error.value = ''
  errorModal.value = ''

  try {
    await deleteCiudadApi(ciudadActiva.value.idCiudad)
    modalEliminar.value = false
    await cargarTodo()
  } catch (err) {
    errorModal.value = err.response?.data?.errors?.join(' ') || err.response?.data?.message || 'No se pudo eliminar la ciudad.'
  } finally {
    eliminando.value = false
  }
}

onMounted(cargarTodo)
</script>

<template>
  <section class="space-y-5">
    <div class="flex items-start justify-between gap-4">
      <div>
        <h1 class="text-[2rem] font-bold text-navy">Gestion de Ciudades</h1>
        <p class="mt-1.5 text-sm text-text-muted">Administra las ciudades con servicio de MPAS Airways (Solo ADMIN).</p>
      </div>

      <button
        type="button"
        class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy transition-colors hover:bg-gold-light"
        @click="abrirCrear"
      >
        + Nueva Ciudad
      </button>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <section class="rounded-[24px] bg-white p-4 shadow-sm">
      <InputApp v-model="busqueda" placeholder="Buscar por ciudad, pais, zona o codigo postal..." />
    </section>

    <section class="overflow-hidden rounded-[24px] bg-white shadow-sm">
      <div class="grid grid-cols-[1.4fr_1.1fr_1.2fr_0.9fr_0.7fr_110px] gap-4 bg-slate-50 px-5 py-3.5 text-sm font-semibold text-navy">
        <span>Ciudad</span>
        <span>Pais</span>
        <span>Zona horaria</span>
        <span>Codigo postal</span>
        <span>Estado</span>
        <span class="text-right">Acciones</span>
      </div>

      <div v-if="cargando" class="px-5 py-5 text-sm text-text-muted">Cargando ciudades...</div>
      <div v-else-if="!ciudadesFiltradas.length" class="px-5 py-5 text-sm text-text-muted">No hay ciudades registradas.</div>

      <div v-else class="divide-y divide-slate-100">
        <div
          v-for="ciudad in ciudadesFiltradas"
          :key="ciudad.idCiudad"
          class="grid grid-cols-[1.4fr_1.1fr_1.2fr_0.9fr_0.7fr_110px] items-center gap-4 px-5 py-4"
        >
          <div class="flex items-center gap-3">
            <div class="flex h-11 w-11 items-center justify-center rounded-2xl bg-slate-100 text-navy">
              <span class="text-lg">🏙</span>
            </div>
            <span class="text-lg font-semibold text-navy">{{ ciudad.nombre }}</span>
          </div>

          <span class="text-sm text-navy">{{ ciudad.nombrePais || '-' }}</span>
          <span class="text-sm text-navy">{{ ciudad.zonaHoraria || '-' }}</span>
          <span class="text-sm text-navy">{{ ciudad.codigoPostal || '-' }}</span>
          <span class="text-sm text-navy">{{ ciudad.estado || '-' }}</span>

          <div class="flex justify-end gap-3 text-base">
            <button type="button" class="text-slate-500 hover:text-navy" @click="abrirEditar(ciudad)">✎</button>
            <button type="button" class="text-red-500 hover:text-red-600" @click="abrirEliminar(ciudad)">🗑</button>
          </div>
        </div>
      </div>
    </section>

    <div v-if="modalAbierto" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalAbierto = false">
      <div class="w-full max-w-2xl rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">{{ modo === 'crear' ? 'Nueva Ciudad' : 'Editar Ciudad' }}</h2>

        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorModal }}
        </div>

        <div class="mt-6 grid gap-5 md:grid-cols-2">
          <SelectApp v-model="form.id_pais" label="Pais" :opciones="opcionesPaises" :error="errores.id_pais" requerido />
          <InputApp
            v-model="form.nombre"
            label="Nombre"
            :error="errores.nombre"
            requerido
            @update:model-value="form.nombre = capitalizarTexto($event)"
          />
          <InputApp v-model="form.zona_horaria" label="Zona horaria" placeholder="America/Guayaquil" />
          <InputApp v-model="form.codigo_postal" label="Codigo postal" />
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
            @click="guardarCiudad"
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
            <h2 class="text-3xl font-bold text-navy">¿Eliminar ciudad?</h2>
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
