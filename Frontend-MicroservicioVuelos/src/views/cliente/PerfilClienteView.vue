<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import { getClienteApi, updateClienteApi } from '@/api/clientes.api'
import { useAutenticacionStore } from '@/stores/autenticacion.store'
import { useCatalogosStore } from '@/stores/catalogos.store'
import { useClienteStore } from '@/stores/cliente.store'
import { cleanLabel, deepValue, resolveClienteId } from '@/utils/portalCliente'

const router = useRouter()
const auth = useAutenticacionStore()
const catalogos = useCatalogosStore()
const clienteStore = useClienteStore()

const cargando = ref(true)
const guardando = ref(false)
const editando = ref(false)
const error = ref('')
const ciudades = ref([])
const errores = ref({})
const idCliente = ref(0)
const sincronizandoPerfil = ref(false)

const form = ref({
  tipo_identificacion: 'CEDULA',
  numero_identificacion: '',
  nombres: '',
  apellidos: '',
  correo: '',
  telefono: '',
  direccion: '',
  id_pais_nacionalidad: '',
  id_pais_residencia: '',
  id_ciudad_residencia: '',
  fecha_nacimiento: '',
  genero: 'MASCULINO',
})

const opcionesTipoIdentificacion = [
  { valor: 'CEDULA', etiqueta: 'Cédula' },
  { valor: 'PASAPORTE', etiqueta: 'Pasaporte' },
  { valor: 'RUC', etiqueta: 'RUC' },
  { valor: 'OTRO', etiqueta: 'Otro' },
]

const opcionesGenero = [
  { valor: 'MASCULINO', etiqueta: 'Masculino' },
  { valor: 'FEMENINO', etiqueta: 'Femenino' },
  { valor: 'OTRO', etiqueta: 'Otro' },
]

const opcionesPaises = computed(() =>
  catalogos.paises.map((pais) => ({
    valor: String(pais.idPais ?? pais.id_pais ?? pais.id),
    etiqueta: cleanLabel(pais.nombre ?? pais.nombre_pais ?? ''),
  })),
)

const opcionesCiudades = computed(() =>
  ciudades.value.map((ciudad) => ({
    valor: String(ciudad.idCiudad ?? ciudad.id_ciudad ?? ciudad.id),
    etiqueta: cleanLabel(ciudad.nombre ?? ciudad.nombre_ciudad ?? ''),
  })),
)

const nombreCompleto = computed(() =>
  [form.value.nombres, form.value.apellidos].filter(Boolean).join(' ').trim() || auth.usuario?.username || 'Cliente',
)

function mensajeErrorApi(err, fallback) {
  const erroresApi = err?.response?.data?.errors
  if (Array.isArray(erroresApi) && erroresApi.length) {
    return erroresApi.join(' ')
  }

  if (erroresApi && typeof erroresApi === 'object') {
    const mensajes = Object.values(erroresApi)
      .flatMap((valor) => (Array.isArray(valor) ? valor : [valor]))
      .filter(Boolean)

    if (mensajes.length) return mensajes.join(' ')
  }

  return err?.response?.data?.message || err?.message || fallback
}

function construirForm(cliente) {
  const idPaisResidencia =
    cliente.id_pais_residencia ??
    cliente.idPaisResidencia ??
    cliente.id_pais ??
    cliente.idPais ??
    deepValue(cliente, ['id_pais_residencia', 'idPaisResidencia'])

  return {
    tipo_identificacion: String(cliente.tipo_identificacion ?? cliente.tipoIdentificacion ?? 'CEDULA'),
    numero_identificacion: String(cliente.numero_identificacion ?? cliente.numeroIdentificacion ?? ''),
    nombres: String(cliente.nombres ?? cliente.nombre ?? ''),
    apellidos: String(cliente.apellidos ?? cliente.apellido ?? ''),
    correo: String(cliente.correo ?? cliente.email ?? ''),
    telefono: String(cliente.telefono ?? ''),
    direccion: String(cliente.direccion ?? ''),
    id_pais_nacionalidad: String(cliente.id_pais_nacionalidad ?? cliente.idPaisNacionalidad ?? ''),
    id_pais_residencia: String(idPaisResidencia ?? ''),
    id_ciudad_residencia: String(cliente.id_ciudad_residencia ?? cliente.idCiudadResidencia ?? ''),
    fecha_nacimiento: String(cliente.fecha_nacimiento ?? cliente.fechaNacimiento ?? '').slice(0, 10),
    genero: String(cliente.genero ?? 'MASCULINO'),
  }
}

async function cargarCiudades(idPais) {
  if (!idPais) {
    ciudades.value = []
    return
  }

  try {
    const respuesta = await catalogos.cargarCiudadesPorPais(idPais, true)
    ciudades.value = Array.isArray(respuesta) ? respuesta : []
  } catch {
    ciudades.value = []
  }
}

async function resolverPaisResidencia(cliente) {
  const idPaisDirecto =
    cliente.id_pais_residencia ??
    cliente.idPaisResidencia ??
    cliente.id_pais ??
    cliente.idPais ??
    deepValue(cliente, ['id_pais_residencia', 'idPaisResidencia'])

  if (idPaisDirecto) return String(idPaisDirecto)

  const idCiudad = String(cliente.id_ciudad_residencia ?? cliente.idCiudadResidencia ?? '')
  if (!idCiudad) return ''

  for (const pais of catalogos.paises) {
    const idPais = String(pais.idPais ?? pais.id_pais ?? pais.id ?? '')
    if (!idPais) continue

    const ciudadesPais = await catalogos.cargarCiudadesPorPais(idPais, true).catch(() => [])
    const encontrada = Array.isArray(ciudadesPais)
      ? ciudadesPais.find((ciudad) => String(ciudad.idCiudad ?? ciudad.id_ciudad ?? ciudad.id) === idCiudad)
      : null

    if (encontrada) return idPais
  }

  return ''
}

function validar() {
  const e = {}
  const correo = form.value.correo.trim()

  if (!form.value.tipo_identificacion) e.tipo_identificacion = 'Selecciona el tipo.'
  if (!form.value.numero_identificacion.trim()) e.numero_identificacion = 'Ingresa la identificación.'
  if (!form.value.nombres.trim()) e.nombres = 'Ingresa los nombres.'
  if (!form.value.correo.trim()) e.correo = 'Ingresa el correo.'
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(correo)) e.correo = 'Ingresa un correo válido.'
  if (!form.value.telefono.trim()) e.telefono = 'Ingresa el teléfono.'
  if (!form.value.direccion.trim()) e.direccion = 'Ingresa la dirección.'
  if (!form.value.id_pais_nacionalidad) e.id_pais_nacionalidad = 'Selecciona la nacionalidad.'
  if (!form.value.id_pais_residencia) e.id_pais_residencia = 'Selecciona el país.'
  if (!form.value.id_ciudad_residencia) e.id_ciudad_residencia = 'Selecciona la ciudad.'

  errores.value = e
  return Object.keys(e).length === 0
}

async function cargarPerfil() {
  cargando.value = true
  error.value = ''

  try {
    await catalogos.cargarPaises(true)
    idCliente.value = resolveClienteId(auth)
    if (!idCliente.value) throw new Error('No se pudo identificar el cliente autenticado.')

    const { data } = await getClienteApi(idCliente.value)
    const cliente = data?.data || {}
    clienteStore.setPerfil(cliente)
    sincronizandoPerfil.value = true
    const base = construirForm(cliente)
    base.id_pais_residencia = base.id_pais_residencia || (await resolverPaisResidencia(cliente))
    form.value = base
    await cargarCiudades(form.value.id_pais_residencia)
    sincronizandoPerfil.value = false
  } catch (err) {
    error.value = mensajeErrorApi(err, 'No se pudo cargar el perfil.')
  } finally {
    cargando.value = false
  }
}

async function guardarCambios() {
  if (guardando.value || !validar()) return
  guardando.value = true
  error.value = ''

  try {
    const payload = {
      tipoIdentificacion: String(form.value.tipo_identificacion || '').trim().toUpperCase(),
      numeroIdentificacion: form.value.numero_identificacion.trim(),
      nombres: form.value.nombres.trim(),
      apellidos: form.value.apellidos.trim(),
      correo: form.value.correo.trim(),
      telefono: form.value.telefono.trim(),
      direccion: form.value.direccion.trim(),
      idCiudadResidencia: Number(form.value.id_ciudad_residencia),
      idPaisNacionalidad: Number(form.value.id_pais_nacionalidad),
      ...(form.value.fecha_nacimiento ? { fechaNacimiento: form.value.fecha_nacimiento } : {}),
      ...(form.value.genero ? { genero: form.value.genero } : {}),
    }

    if (!payload.apellidos) delete payload.apellidos

    const { data } = await updateClienteApi(idCliente.value, payload)
    const clienteActualizado = data?.data || {
      ...clienteStore.perfil,
      ...payload,
      idCliente: idCliente.value,
    }
    clienteStore.setPerfil(clienteActualizado)
    form.value = construirForm(clienteActualizado)
    editando.value = false
    errores.value = {}
  } catch (err) {
    error.value = mensajeErrorApi(err, 'No se pudieron guardar los cambios.')
  } finally {
    guardando.value = false
  }
}

function cancelarEdicion() {
  if (clienteStore.perfil) {
    form.value = construirForm(clienteStore.perfil)
  }
  editando.value = false
  errores.value = {}
  error.value = ''
}

watch(
  () => form.value.id_pais_residencia,
  async (idPais, anterior) => {
    if (idPais === anterior) return
    if (sincronizandoPerfil.value) return
    form.value.id_ciudad_residencia = ''
    await cargarCiudades(idPais)
  },
)

onMounted(cargarPerfil)
</script>

<template>
  <section class="space-y-6">
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h1 class="text-3xl font-bold text-navy">Mi Perfil</h1>
        <p class="mt-2 text-text-muted">Gestiona la información personal del cliente autenticado.</p>
      </div>

      <button
        type="button"
        class="rounded-2xl bg-navy px-5 py-3 font-semibold text-white transition-colors hover:bg-navy/90"
        @click="router.push({ name: 'cliente-portal' })"
      >
        Mi Portal de Viajes
      </button>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <div v-if="cargando" class="rounded-[28px] bg-white p-8 text-text-muted shadow-sm">
      Cargando información del cliente...
    </div>

    <div v-else class="grid gap-6 lg:grid-cols-[0.78fr_1.4fr]">
      <aside class="rounded-[30px] bg-white p-8 text-center shadow-sm">
        <div class="mx-auto flex h-32 w-32 items-center justify-center rounded-full bg-slate-100 text-navy">
          <svg class="h-16 w-16" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M16 14a4 4 0 10-8 0m8 0a4 4 0 11-8 0m8 0v1a2 2 0 002 2h1m-11-3v1a2 2 0 01-2 2H5" />
          </svg>
        </div>

        <h2 class="mt-6 text-3xl font-bold text-navy">{{ nombreCompleto }}</h2>
        <p class="mt-2 text-text-muted">{{ form.correo || auth.usuario?.username }}</p>

        <div class="mt-8 rounded-[24px] bg-slate-50 px-6 py-5 text-left">
          <p class="text-sm uppercase tracking-[0.24em] text-gold-dark">Cuenta Cliente</p>
          <p class="mt-3 text-lg font-semibold text-navy">{{ auth.usuario?.username || 'Cliente MPAS' }}</p>
        </div>
      </aside>

      <section class="rounded-[30px] bg-white p-8 shadow-sm">
        <div class="flex flex-col gap-4 border-b border-slate-200 pb-6 sm:flex-row sm:items-center sm:justify-between">
          <h2 class="text-3xl font-semibold text-navy">Información Personal</h2>

          <div class="flex gap-3">
            <button
              v-if="!editando"
              type="button"
              class="rounded-2xl bg-navy px-5 py-3 font-semibold text-white transition-colors hover:bg-navy/90"
              @click="editando = true"
            >
              Editar información
            </button>

            <template v-else>
              <button
                type="button"
                class="rounded-2xl border border-slate-300 px-5 py-3 font-semibold text-navy transition-colors hover:bg-slate-50"
                @click="cancelarEdicion"
              >
                Cancelar
              </button>
              <button
                type="button"
                class="rounded-2xl bg-gold px-5 py-3 font-semibold text-navy transition-colors hover:bg-gold-light disabled:cursor-not-allowed disabled:bg-gold/50"
                :disabled="guardando"
                @click="guardarCambios"
              >
                {{ guardando ? 'Guardando...' : 'Guardar cambios' }}
              </button>
            </template>
          </div>
        </div>

        <div class="mt-8 grid gap-5 md:grid-cols-2">
          <SelectApp v-model="form.tipo_identificacion" label="Tipo de identificación" :opciones="opcionesTipoIdentificacion" :error="errores.tipo_identificacion" :deshabilitado="!editando" requerido />
          <InputApp v-model="form.numero_identificacion" label="Número de identificación" :error="errores.numero_identificacion" :deshabilitado="!editando" requerido />
          <InputApp v-model="form.nombres" label="Nombres" :error="errores.nombres" :deshabilitado="!editando" requerido />
          <InputApp v-model="form.apellidos" label="Apellidos" :deshabilitado="!editando" />
          <InputApp v-model="form.correo" label="Email" tipo="email" :error="errores.correo" :deshabilitado="!editando" requerido />
          <InputApp v-model="form.telefono" label="Teléfono" :error="errores.telefono" :deshabilitado="!editando" requerido />
          <InputApp v-model="form.direccion" label="Dirección" :error="errores.direccion" :deshabilitado="!editando" requerido />
          <InputApp v-model="form.fecha_nacimiento" label="Fecha de nacimiento" tipo="date" :deshabilitado="!editando" />
          <SelectApp v-model="form.id_pais_nacionalidad" label="Nacionalidad" :opciones="opcionesPaises" placeholder="Seleccionar..." :error="errores.id_pais_nacionalidad" :cargando="catalogos.cargandoPaises" :deshabilitado="!editando" requerido />
          <SelectApp v-model="form.genero" label="Género" :opciones="opcionesGenero" placeholder="Seleccionar..." :deshabilitado="!editando" />
          <SelectApp v-model="form.id_pais_residencia" label="País de residencia" :opciones="opcionesPaises" placeholder="Seleccionar..." :error="errores.id_pais_residencia" :cargando="catalogos.cargandoPaises" :deshabilitado="!editando" requerido />
          <SelectApp v-model="form.id_ciudad_residencia" label="Ciudad de residencia" :opciones="opcionesCiudades" placeholder="Seleccionar..." :error="errores.id_ciudad_residencia" :deshabilitado="!editando || !form.id_pais_residencia" requerido />
        </div>
      </section>
    </div>
  </section>
</template>
