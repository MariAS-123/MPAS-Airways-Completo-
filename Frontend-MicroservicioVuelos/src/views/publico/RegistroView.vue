<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAutenticacionStore } from '@/stores/autenticacion.store'
import { useReservaStore } from '@/stores/reserva.store'
import { registerClienteApi } from '@/api/autenticacion.api'
import { useCatalogosStore } from '@/stores/catalogos.store'
import { IMAGENES } from '@/config/imagenes'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import BotonApp from '@/components/base/BotonApp.vue'

const router = useRouter()
const route = useRoute()
const auth = useAutenticacionStore()
const reserva = useReservaStore()
const catalogos = useCatalogosStore()

const ciudades = ref([])
const cargandoCiudades = ref(false)

const opcionesPaises = computed(() =>
  catalogos.paises.map((p) => ({
    valor: String(p.idPais ?? p.id_pais ?? p.id),
    etiqueta: limpiarNombreCatalogo(p.nombre),
  })),
)
const opcionesCiudades = computed(() =>
  ciudades.value.map((c) => ({
    valor: String(c.idCiudad ?? c.id_ciudad ?? c.id),
    etiqueta: limpiarNombreCatalogo(c.nombre),
  })),
)

const TIPOS_ID = [
  { valor: 'CEDULA', etiqueta: 'Cédula' },
  { valor: 'PASAPORTE', etiqueta: 'Pasaporte' },
  { valor: 'RUC', etiqueta: 'RUC' },
  { valor: 'OTRO', etiqueta: 'Otro' },
]
const GENEROS = [
  { valor: 'MASCULINO', etiqueta: 'Masculino' },
  { valor: 'FEMENINO', etiqueta: 'Femenino' },
  { valor: 'OTRO', etiqueta: 'Otro' },
]

function limpiarNombreCatalogo(nombre = '') {
  return String(nombre).replace(/\d{6,}$/u, '').trim()
}

onMounted(async () => {
  try {
    await catalogos.cargarPaises(true)
  } catch {}
})

const form = ref({
  tipo_identificacion: '',
  numero_identificacion: '',
  razon_social: '',
  nombres: '',
  apellidos: '',
  genero: '',
  fecha_nacimiento: '',
  id_pais_nacionalidad: '',
  pais_residencia_tmp: '',
  id_ciudad_residencia: '',
  correo: '',
  telefono: '',
  direccion: '',
  username: '',
  password: '',
  confirmar_password: '',
})

const esRUC = computed(() => form.value.tipo_identificacion === 'RUC')

watch(
  () => form.value.pais_residencia_tmp,
  async (idPais) => {
    ciudades.value = []
    form.value.id_ciudad_residencia = ''
    if (!idPais) return
    cargandoCiudades.value = true
    try {
      ciudades.value = await catalogos.cargarCiudadesPorPais(idPais)
    } catch {}
    finally {
      cargandoCiudades.value = false
    }
  },
)

const errores = ref({})
const errorGeneral = ref('')
const cargando = ref(false)

function validarPassword(p) {
  if (p.length < 8) return 'Mínimo 8 caracteres.'
  if (!/[A-Z]/.test(p)) return 'Debe incluir al menos una mayúscula.'
  if (!/[0-9]/.test(p)) return 'Debe incluir al menos un número.'
  if (!/[!@#$%^&*()_\-+={}[\]|\\:;"'<>,.?/~`]/.test(p)) {
    return 'Debe incluir al menos un carácter especial.'
  }
  return ''
}

function validar() {
  const e = {}
  if (!form.value.tipo_identificacion) e.tipo_identificacion = 'Selecciona el tipo.'
  if (!form.value.numero_identificacion.trim()) e.numero_identificacion = 'Requerido.'
  if (esRUC.value && !form.value.razon_social.trim()) e.razon_social = 'Requerido para RUC.'
  if (!form.value.nombres.trim()) e.nombres = 'Requerido.'
  if (!form.value.correo.trim()) e.correo = 'Requerido.'
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.value.correo)) e.correo = 'Correo inválido.'
  if (!form.value.telefono.trim()) e.telefono = 'Requerido.'
  if (!form.value.direccion.trim()) e.direccion = 'Requerido.'
  if (!form.value.id_pais_nacionalidad) e.id_pais_nacionalidad = 'Selecciona la nacionalidad.'
  if (!form.value.pais_residencia_tmp) e.pais_residencia_tmp = 'Selecciona el país.'
  if (!form.value.id_ciudad_residencia) e.id_ciudad_residencia = 'Selecciona la ciudad.'
  if (!form.value.username.trim()) e.username = 'Requerido.'
  if (!form.value.password) {
    e.password = 'Requerido.'
  } else {
    const pe = validarPassword(form.value.password)
    if (pe) e.password = pe
  }
  if (form.value.password !== form.value.confirmar_password) {
    e.confirmar_password = 'Las contraseñas no coinciden.'
  }
  errores.value = e
  return !Object.keys(e).length
}

async function handleRegistro() {
  if (!validar()) {
    setTimeout(() => document.querySelector('.text-error')?.scrollIntoView({ behavior: 'smooth', block: 'center' }), 50)
    return
  }

  cargando.value = true
  errorGeneral.value = ''
  try {
    const payload = {
      tipoIdentificacion: form.value.tipo_identificacion,
      numeroIdentificacion: form.value.numero_identificacion.trim(),
      nombres: form.value.nombres.trim(),
      correo: form.value.correo.trim(),
      telefono: form.value.telefono.trim(),
      direccion: form.value.direccion.trim(),
      idCiudadResidencia: Number(form.value.id_ciudad_residencia),
      idPaisNacionalidad: Number(form.value.id_pais_nacionalidad),
      username: form.value.username.trim(),
      password: form.value.password,
    }

    if (form.value.apellidos.trim()) payload.apellidos = form.value.apellidos.trim()
    if (form.value.genero) payload.genero = form.value.genero
    if (form.value.fecha_nacimiento) payload.fechaNacimiento = form.value.fecha_nacimiento
    if (esRUC.value) payload.razonSocial = form.value.razon_social.trim()

    await registerClienteApi(payload)
    await auth.login({ username: payload.username, password: payload.password }, true)

    const redirect = route.query.redirect
    if (redirect) {
      router.push(redirect)
    } else if (reserva.tienePendiente) {
      try {
        await router.push({ name: 'checkout-pago' })
      } catch {
        router.push('/cliente')
      }
    } else {
      router.push('/cliente')
    }
  } catch (error) {
    const status = error.response?.status
    const msg = error.response?.data?.message
    if (status === 409) {
      errorGeneral.value = msg || 'El correo o número de identificación ya están registrados.'
    } else if (msg) {
      errorGeneral.value = msg
    } else {
      errorGeneral.value = 'Error al crear la cuenta. Intenta nuevamente.'
    }
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    cargando.value = false
  }
}
</script>

<template>
  <div class="min-h-[calc(100vh-64px)] bg-background px-4 py-10">
    <div class="mx-auto max-w-4xl">
      <div class="mb-8 text-center">
        <div class="inline-flex h-16 w-16 items-center justify-center overflow-hidden rounded-full bg-gold shadow-lg">
          <img :src="IMAGENES.logoPrincipal" alt="MPAS Airways" class="h-12 w-12 object-contain" />
        </div>
        <h1 class="mt-5 text-4xl font-bold">
          <span class="text-navy">MPAS</span>
          <span class="ml-2 text-gold">Airways</span>
        </h1>
        <p class="mt-3 text-lg text-text-muted">Crea tu cuenta para completar tu reserva</p>
      </div>

      <div class="rounded-[32px] bg-white p-6 shadow-[0_16px_50px_rgba(15,23,42,0.10)] sm:p-10">
        <Transition name="fade">
          <div
            v-if="errorGeneral"
            class="mb-6 flex items-start gap-2 rounded-xl border border-error/30 bg-red-50 px-4 py-3 text-sm text-error"
          >
            <svg class="mt-0.5 h-4 w-4 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
            </svg>
            {{ errorGeneral }}
          </div>
        </Transition>

        <form class="space-y-8" @submit.prevent="handleRegistro">
          <section>
            <h2 class="mb-4 border-b border-border pb-2 text-sm font-bold uppercase tracking-wider text-text-main">
              Datos de Identificación
            </h2>
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <SelectApp
                v-model="form.tipo_identificacion"
                label="Tipo de Identificación"
                :opciones="TIPOS_ID"
                placeholder="Seleccionar..."
                :error="errores.tipo_identificacion"
                requerido
              />
              <InputApp
                v-model="form.numero_identificacion"
                label="Número de Identificación"
                :error="errores.numero_identificacion"
                requerido
              />
              <div v-if="esRUC" class="sm:col-span-2">
                <InputApp
                  v-model="form.razon_social"
                  label="Razón Social"
                  placeholder="Nombre de la empresa"
                  :error="errores.razon_social"
                  requerido
                />
              </div>
            </div>
          </section>

          <section>
            <h2 class="mb-4 border-b border-border pb-2 text-sm font-bold uppercase tracking-wider text-text-main">
              Datos Personales
            </h2>
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <InputApp
                v-model="form.nombres"
                label="Nombres"
                autocomplete="given-name"
                :error="errores.nombres"
                requerido
              />
              <InputApp
                v-model="form.apellidos"
                label="Apellidos"
                autocomplete="family-name"
                :error="errores.apellidos"
              />
              <SelectApp
                v-model="form.genero"
                label="Género"
                :opciones="GENEROS"
                placeholder="Seleccionar..."
                :error="errores.genero"
              />
              <InputApp
                v-model="form.fecha_nacimiento"
                label="Fecha de Nacimiento"
                tipo="date"
                autocomplete="bday"
                :error="errores.fecha_nacimiento"
              />
              <SelectApp
                v-model="form.id_pais_nacionalidad"
                label="Nacionalidad"
                :opciones="opcionesPaises"
                placeholder="Seleccionar país..."
                :cargando="catalogos.cargandoPaises"
                :error="errores.id_pais_nacionalidad"
                requerido
              />
              <SelectApp
                v-model="form.pais_residencia_tmp"
                label="País"
                :opciones="opcionesPaises"
                placeholder="Seleccionar país..."
                :cargando="catalogos.cargandoPaises"
                :error="errores.pais_residencia_tmp"
                requerido
              />
            </div>
          </section>

          <section>
            <h2 class="mb-4 border-b border-border pb-2 text-sm font-bold uppercase tracking-wider text-text-main">
              Datos de Contacto
            </h2>
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <InputApp
                v-model="form.correo"
                label="Correo Electrónico"
                tipo="email"
                autocomplete="email"
                :error="errores.correo"
                requerido
              />
              <InputApp
                v-model="form.telefono"
                label="Teléfono"
                tipo="tel"
                autocomplete="tel"
                :error="errores.telefono"
                requerido
              />
              <div class="sm:col-span-2">
                <InputApp
                  v-model="form.direccion"
                  label="Dirección"
                  autocomplete="street-address"
                  :error="errores.direccion"
                  requerido
                />
              </div>
              <SelectApp
                v-model="form.id_ciudad_residencia"
                label="Ciudad de Residencia"
                :opciones="opcionesCiudades"
                placeholder="Selecciona primero el país..."
                :cargando="cargandoCiudades"
                :deshabilitado="!form.pais_residencia_tmp"
                :error="errores.id_ciudad_residencia"
                requerido
              />
            </div>
          </section>

          <section>
            <h2 class="mb-4 border-b border-border pb-2 text-sm font-bold uppercase tracking-wider text-text-main">
              Datos de Acceso
            </h2>
            <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <InputApp
                v-model="form.username"
                label="Nombre de Usuario"
                autocomplete="username"
                :error="errores.username"
                requerido
              />
              <InputApp
                v-model="form.password"
                label="Contraseña"
                tipo="password"
                autocomplete="new-password"
                :error="errores.password"
                requerido
              />
              <div class="sm:col-span-2">
                <InputApp
                  v-model="form.confirmar_password"
                  label="Confirmar Contraseña"
                  tipo="password"
                  autocomplete="new-password"
                  :error="errores.confirmar_password"
                  requerido
                />
              </div>
            </div>
            <p class="mt-3 text-xs text-text-muted">
              La contraseña debe tener mínimo 8 caracteres, una mayúscula, un número y un carácter especial.
            </p>
          </section>

          <div class="flex flex-col gap-4 pt-2 sm:flex-row">
            <BotonApp variante="secundario" ancho @click="router.push('/')">
              Cancelar
            </BotonApp>
            <BotonApp tipo="submit" variante="primario" ancho :cargando="cargando">
              Crear Cuenta y Continuar
            </BotonApp>
          </div>
        </form>

        <p class="mt-8 text-center text-sm text-text-muted">
          ¿Ya tienes cuenta?
          <RouterLink to="/login" class="font-semibold text-navy hover:underline">
            Inicia sesión
          </RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.25s;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
