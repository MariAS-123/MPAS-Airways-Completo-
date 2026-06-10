<script setup>
import { computed, onMounted, ref } from 'vue'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import { createPaisApi, deletePaisApi, getPaisesApi, updatePaisApi } from '@/api/paises.api'
import { deepValue, extractItems } from '@/utils/portalCliente'

const CONTINENTES = [
  'America del Norte',
  'America Central',
  'America del Sur',
  'Europa',
  'Asia',
  'Africa',
  'Oceania',
]

const cargando = ref(true)
const guardando = ref(false)
const eliminando = ref(false)
const error = ref('')
const errorModal = ref('')
const busqueda = ref('')
const paises = ref([])
const modalAbierto = ref(false)
const modalEliminar = ref(false)
const modo = ref('crear')
const paisActivo = ref(null)
const errores = ref({})

const form = ref({
  codigo_iso2: '',
  codigo_iso3: '',
  nombre: '',
  continente: '',
})

const opcionesContinente = computed(() =>
  CONTINENTES.map((continente) => ({
    valor: continente,
    etiqueta: continente,
  })),
)

const paisesOrdenados = computed(() => {
  const termino = busqueda.value.trim().toLowerCase()
  const base = [...paises.value].sort((a, b) => String(a.nombre).localeCompare(String(b.nombre), 'es'))

  if (!termino) return base

  return base.filter((pais) => {
    const nombre = String(pais.nombre || '').toLowerCase()
    const iso2 = String(pais.codigoIso2 || '').toLowerCase()
    const iso3 = String(pais.codigoIso3 || '').toLowerCase()
    const continente = String(pais.continente || '').toLowerCase()
    const estado = String(pais.estado || '').toLowerCase()
    return (
      nombre.includes(termino) ||
      iso2.includes(termino) ||
      iso3.includes(termino) ||
      continente.includes(termino) ||
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
    codigoIso2: deepValue(item, ['codigoIso2', 'codigo_iso2']) || '',
    codigoIso3: deepValue(item, ['codigoIso3', 'codigo_iso3']) || '',
    nombre: deepValue(item, ['nombre']) || '',
    continente: deepValue(item, ['continente']) || '',
    estado: deepValue(item, ['estado']) || '',
  }
}

function limpiarFormulario() {
  form.value = {
    codigo_iso2: '',
    codigo_iso3: '',
    nombre: '',
    continente: '',
  }
  errores.value = {}
  errorModal.value = ''
}

function abrirCrear() {
  modo.value = 'crear'
  paisActivo.value = null
  limpiarFormulario()
  modalAbierto.value = true
}

function abrirEditar(pais) {
  modo.value = 'editar'
  paisActivo.value = pais
  form.value = {
    codigo_iso2: String(pais.codigoIso2 || ''),
    codigo_iso3: String(pais.codigoIso3 || ''),
    nombre: String(pais.nombre || ''),
    continente: String(pais.continente || ''),
  }
  errores.value = {}
  errorModal.value = ''
  modalAbierto.value = true
}

function abrirEliminar(pais) {
  paisActivo.value = pais
  errorModal.value = ''
  modalEliminar.value = true
}

function validar() {
  const nuevosErrores = {}
  const iso2 = form.value.codigo_iso2.trim().toUpperCase()
  const iso3 = form.value.codigo_iso3.trim().toUpperCase()

  if (!form.value.nombre.trim()) nuevosErrores.nombre = 'Ingresa el nombre del pais.'
  if (!iso2) nuevosErrores.codigo_iso2 = 'Ingresa el codigo ISO2.'
  else if (iso2.length !== 2) nuevosErrores.codigo_iso2 = 'Debe tener 2 letras.'
  if (iso3 && iso3.length !== 3) nuevosErrores.codigo_iso3 = 'Debe tener 3 letras.'
  if (!form.value.continente) nuevosErrores.continente = 'Selecciona un continente.'

  errores.value = nuevosErrores
  return !Object.keys(nuevosErrores).length
}

async function cargarPaises() {
  cargando.value = true
  error.value = ''

  try {
    const respuesta = await getPaisesApi({ page: 1, page_size: 200 })
    paises.value = extractItems(respuesta).map(normalizarPais)
  } catch (err) {
    error.value = err.response?.data?.errors?.join(' ') || err.response?.data?.message || 'No se pudieron cargar los paises.'
  } finally {
    cargando.value = false
  }
}

async function guardarPais() {
  if (guardando.value || !validar()) return
  guardando.value = true
  error.value = ''
  errorModal.value = ''

  const payload = {
    codigoIso2: form.value.codigo_iso2.trim().toUpperCase(),
    nombre: capitalizarTexto(form.value.nombre),
    continente: form.value.continente,
  }

  if (form.value.codigo_iso3.trim()) {
    payload.codigoIso3 = form.value.codigo_iso3.trim().toUpperCase()
  }

  try {
    if (modo.value === 'crear') {
      await createPaisApi(payload)
    } else {
      await updatePaisApi(paisActivo.value.idPais, payload)
    }

    modalAbierto.value = false
    await cargarPaises()
  } catch (err) {
    errorModal.value = err.response?.data?.errors?.join(' ') || err.response?.data?.message || 'No se pudo guardar el pais.'
  } finally {
    guardando.value = false
  }
}

async function confirmarEliminar() {
  if (!paisActivo.value?.idPais || eliminando.value) return
  eliminando.value = true
  error.value = ''
  errorModal.value = ''

  try {
    await deletePaisApi(paisActivo.value.idPais)
    modalEliminar.value = false
    await cargarPaises()
  } catch (err) {
    errorModal.value = err.response?.data?.errors?.join(' ') || err.response?.data?.message || 'No se pudo eliminar el pais.'
  } finally {
    eliminando.value = false
  }
}

onMounted(cargarPaises)
</script>

<template>
  <section class="space-y-5">
    <div class="flex items-start justify-between gap-4">
      <div>
        <h1 class="text-[2rem] font-bold text-navy">Gestion de Paises</h1>
        <p class="mt-1.5 text-sm text-text-muted">Administra los paises donde opera MPAS Airways (Solo ADMIN).</p>
      </div>

      <button
        type="button"
        class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy transition-colors hover:bg-gold-light"
        @click="abrirCrear"
      >
        + Nuevo Pais
      </button>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <section class="rounded-[24px] bg-white p-4 shadow-sm">
      <InputApp v-model="busqueda" placeholder="Buscar por pais, codigo o continente..." />
    </section>

    <section class="overflow-hidden rounded-[24px] bg-white shadow-sm">
      <div class="grid grid-cols-[2fr_0.7fr_0.7fr_1.2fr_0.8fr_110px] gap-4 bg-slate-50 px-5 py-3.5 text-sm font-semibold text-navy">
        <span>Pais</span>
        <span>ISO2</span>
        <span>ISO3</span>
        <span>Continente</span>
        <span>Estado</span>
        <span class="text-right">Acciones</span>
      </div>

      <div v-if="cargando" class="px-5 py-5 text-sm text-text-muted">Cargando paises...</div>
      <div v-else-if="!paisesOrdenados.length" class="px-5 py-5 text-sm text-text-muted">No hay paises registrados.</div>

      <div v-else class="divide-y divide-slate-100">
        <div
          v-for="pais in paisesOrdenados"
          :key="pais.idPais"
          class="grid grid-cols-[2fr_0.7fr_0.7fr_1.2fr_0.8fr_110px] items-center gap-4 px-5 py-4"
        >
          <div class="flex items-center gap-3">
            <div class="flex h-11 w-11 items-center justify-center rounded-2xl bg-slate-100 text-navy">
              <span class="text-lg">🌐</span>
            </div>
            <span class="text-lg font-semibold text-navy">{{ pais.nombre }}</span>
          </div>

          <span class="text-sm text-navy">{{ pais.codigoIso2 }}</span>
          <span class="text-sm text-navy">{{ pais.codigoIso3 || '-' }}</span>
          <span class="text-sm text-navy">{{ pais.continente || '-' }}</span>
          <span class="text-sm text-navy">{{ pais.estado || '-' }}</span>

          <div class="flex justify-end gap-3 text-base">
            <button type="button" class="text-slate-500 hover:text-navy" @click="abrirEditar(pais)">✎</button>
            <button type="button" class="text-red-500 hover:text-red-600" @click="abrirEliminar(pais)">🗑</button>
          </div>
        </div>
      </div>
    </section>

    <div v-if="modalAbierto" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalAbierto = false">
      <div class="w-full max-w-2xl rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">{{ modo === 'crear' ? 'Nuevo Pais' : 'Editar Pais' }}</h2>

        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorModal }}
        </div>

        <div class="mt-6 grid gap-5 md:grid-cols-2">
          <InputApp
            v-model="form.nombre"
            label="Nombre"
            :error="errores.nombre"
            requerido
            @update:model-value="form.nombre = capitalizarTexto($event)"
          />
          <SelectApp
            v-model="form.continente"
            label="Continente"
            :opciones="opcionesContinente"
            :error="errores.continente"
            requerido
          />
          <InputApp v-model="form.codigo_iso2" label="Codigo ISO2" :error="errores.codigo_iso2" requerido />
          <InputApp v-model="form.codigo_iso3" label="Codigo ISO3" :error="errores.codigo_iso3" />
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
            @click="guardarPais"
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
            <h2 class="text-3xl font-bold text-navy">¿Eliminar pais?</h2>
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
