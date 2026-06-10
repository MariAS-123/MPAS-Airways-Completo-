<script setup>
import { computed, nextTick, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import CheckoutStepper from '@/components/CheckoutStepper.vue'
import { useCatalogosStore } from '@/stores/catalogos.store'
import { useReservaStore } from '@/stores/reserva.store'

const router = useRouter()
const catalogos = useCatalogosStore()
const reserva = useReservaStore()

const errores = ref({})
const errorGeneral = ref('')
const navegando = ref(false)
const pasajeros = ref([])

const vuelo = computed(() => reserva.vuelo)
const opcionesPaises = computed(() =>
  catalogos.paises.map((pais) => ({
    valor: String(pais.idPais ?? pais.id_pais ?? pais.id),
    etiqueta: String(pais.nombre ?? pais.nombre_pais ?? ''),
  })),
)

function obtenerEtiquetaPais(idPais) {
  const idNormalizado = String(idPais || '')
  return opcionesPaises.value.find((pais) => pais.valor === idNormalizado)?.etiqueta || ''
}

function crearPasajeroVacio(indice = 0) {
  return {
    id: `${Date.now()}-${indice}-${Math.random().toString(16).slice(2, 8)}`,
    nombre_pasajero: '',
    apellido_pasajero: '',
    tipo_documento_pasajero: 'CEDULA',
    numero_documento_pasajero: '',
    fecha_nacimiento_pasajero: '',
    id_pais_nacionalidad: '',
    nacionalidad_pasajero: '',
    email_contacto_pasajero: '',
    telefono_contacto_pasajero: '',
    genero_pasajero: 'MASCULINO',
    requiere_asistencia: false,
    observaciones_pasajero: '',
    id_cliente: null,
  }
}

function clonarPasajero(pasajero, indice) {
  const idPaisNacionalidad = String(
    pasajero.id_pais_nacionalidad || pasajero.idPaisNacionalidad || '',
  )

  return {
    ...crearPasajeroVacio(indice),
    ...pasajero,
    id_pais_nacionalidad: idPaisNacionalidad,
    nacionalidad_pasajero:
      pasajero.nacionalidad_pasajero ||
      pasajero.nacionalidadPasajero ||
      obtenerEtiquetaPais(idPaisNacionalidad),
    id: pasajero.id || `${Date.now()}-${indice}-${Math.random().toString(16).slice(2, 8)}`,
  }
}

function cantidadObjetivo() {
  return Number(vuelo.value?.pasajeros || 1)
}

function actualizarCantidadReserva(cantidad) {
  if (!vuelo.value) return
  reserva.setVuelo({
    ...vuelo.value,
    pasajeros: cantidad,
  })
}

function inicializarPasajeros() {
  const guardados = Array.isArray(reserva.pasajeros) ? reserva.pasajeros : []
  const base = guardados.length
    ? guardados.map((pasajero, indice) => clonarPasajero(pasajero, indice))
    : Array.from({ length: cantidadObjetivo() }, (_, indice) => crearPasajeroVacio(indice))

  while (base.length < cantidadObjetivo()) {
    base.push(crearPasajeroVacio(base.length))
  }

  pasajeros.value = base
  actualizarCantidadReserva(base.length)
}

function agregarPasajero() {
  if (pasajeros.value.length >= 9) return
  pasajeros.value.push(crearPasajeroVacio(pasajeros.value.length))
  actualizarCantidadReserva(pasajeros.value.length)
  sincronizarStore()
}

function eliminarPasajero(indice) {
  if (pasajeros.value.length <= 1) return
  pasajeros.value.splice(indice, 1)
  actualizarCantidadReserva(pasajeros.value.length)
  sincronizarStore()
}

function sincronizarStore() {
  reserva.setPasajeros(
    pasajeros.value.map(({ id, ...pasajero }) => {
      const idPaisNacionalidad = String(pasajero.id_pais_nacionalidad || '')
      return {
        ...pasajero,
        id_pais_nacionalidad: idPaisNacionalidad,
        nacionalidad_pasajero: obtenerEtiquetaPais(idPaisNacionalidad),
      }
    }),
  )
}

function validarCedulaEcuador(valor) {
  const cedula = String(valor || '').replace(/\D/g, '')
  return /^\d{10}$/.test(cedula)
}

function validarDocumento(tipo, valor) {
  const documento = String(valor || '').trim().toUpperCase()
  if (!documento) return 'Ingresa el numero de documento.'

  if (tipo === 'CEDULA' && !validarCedulaEcuador(documento)) {
    return 'Ingrese una cedula valida.'
  }

  if (tipo === 'PASAPORTE' && !/^[A-Z0-9]{6,12}$/.test(documento)) {
    return 'El pasaporte debe tener entre 6 y 12 caracteres alfanumericos.'
  }

  if (tipo === 'RUC' && !/^\d{13}$/.test(documento)) {
    return 'El RUC debe tener 13 digitos.'
  }

  if (tipo === 'OTRO' && !/^[A-Z0-9-]{4,20}$/.test(documento)) {
    return 'El documento debe tener entre 4 y 20 caracteres validos.'
  }

  return ''
}

function normalizarFechaNacimiento(valor) {
  const texto = String(valor || '').trim()
  if (/^\d{4}-\d{2}-\d{2}$/.test(texto)) return texto

  const match = texto.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/)
  if (!match) return texto

  const dia = match[1].padStart(2, '0')
  const mes = match[2].padStart(2, '0')
  return `${match[3]}-${mes}-${dia}`
}

async function enfocarPrimerError() {
  await nextTick()
  const primero = document.querySelector('[data-error-activo="true"]')
  primero?.scrollIntoView({ behavior: 'smooth', block: 'center' })
  primero?.querySelector('input, select, textarea')?.focus?.()
}

function validar() {
  const nuevosErrores = {}
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  const hoy = new Date()
  hoy.setHours(0, 0, 0, 0)

  pasajeros.value.forEach((pasajero, indice) => {
    pasajero.fecha_nacimiento_pasajero = normalizarFechaNacimiento(pasajero.fecha_nacimiento_pasajero)

    if (!pasajero.nombre_pasajero.trim()) nuevosErrores[`nombre-${indice}`] = 'Ingresa el nombre.'
    if (!pasajero.apellido_pasajero.trim()) nuevosErrores[`apellido-${indice}`] = 'Ingresa el apellido.'
    if (!pasajero.tipo_documento_pasajero) nuevosErrores[`tipo-documento-${indice}`] = 'Selecciona el tipo de documento.'
    const errorDocumento = validarDocumento(pasajero.tipo_documento_pasajero, pasajero.numero_documento_pasajero)
    if (errorDocumento) nuevosErrores[`documento-${indice}`] = errorDocumento
    if (!pasajero.fecha_nacimiento_pasajero) {
      nuevosErrores[`fecha-${indice}`] = 'Selecciona la fecha de nacimiento.'
    } else {
      const fechaNacimiento = new Date(`${pasajero.fecha_nacimiento_pasajero}T00:00:00`)
      if (fechaNacimiento > hoy) {
        nuevosErrores[`fecha-${indice}`] = 'La fecha de nacimiento no puede ser futura.'
      }
    }
    if (!pasajero.id_pais_nacionalidad) nuevosErrores[`nacionalidad-${indice}`] = 'Selecciona la nacionalidad.'
    if (!pasajero.email_contacto_pasajero.trim()) {
      nuevosErrores[`email-${indice}`] = 'Ingresa el email de contacto.'
    } else if (!emailRegex.test(pasajero.email_contacto_pasajero.trim())) {
      nuevosErrores[`email-${indice}`] = 'Ingresa un correo valido.'
    }
    if (!pasajero.telefono_contacto_pasajero.trim()) nuevosErrores[`telefono-${indice}`] = 'Ingresa el telefono de contacto.'
    if (!pasajero.genero_pasajero) nuevosErrores[`genero-${indice}`] = 'Selecciona el genero.'
  })

  errores.value = nuevosErrores
  const valido = Object.keys(nuevosErrores).length === 0
  errorGeneral.value = valido ? '' : 'Revisa los datos del pasajero antes de continuar.'
  return valido
}

async function continuarAsientos() {
  if (navegando.value) return
  if (!validar()) {
    await enfocarPrimerError()
    return
  }

  navegando.value = true
  try {
    sincronizarStore()
    await router.push({ name: 'seleccion-asientos' })
  } catch {
    errorGeneral.value = 'No se pudo continuar a la seleccion de asientos. Intenta nuevamente.'
  } finally {
    navegando.value = false
  }
}

onMounted(async () => {
  if (!vuelo.value) {
    router.replace({ name: 'buscar-vuelos' })
    return
  }

  await catalogos.cargarPaises(true).catch(() => {})
  inicializarPasajeros()
})
</script>

<template>
  <div>
    <CheckoutStepper :paso-actual="2" />

    <section class="min-h-[calc(100vh-64px)] bg-background py-10">
      <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p class="text-sm font-semibold uppercase tracking-[0.28em] text-gold-dark">Paso 2</p>
            <h1 class="mt-3 text-3xl font-bold text-navy">Informacion de Pasajeros</h1>
            <p class="mt-3 text-text-muted">
              Primero registramos los datos de todas las personas que viajaran. Luego asignaremos un asiento por cada pasajero.
            </p>
          </div>

          <button
            type="button"
            class="inline-flex items-center justify-center gap-2 rounded-2xl bg-navy px-5 py-3 font-semibold text-white transition-colors hover:bg-navy/90"
            @click="agregarPasajero"
          >
            <span class="text-lg leading-none">+</span>
            <span>Agregar Pasajero</span>
          </button>
        </div>

        <div
          v-if="errorGeneral"
          class="mt-6 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
        >
          {{ errorGeneral }}
        </div>

        <div class="mt-8 grid gap-8 lg:grid-cols-[1.4fr_0.8fr]">
          <div class="space-y-6">
            <article
              v-for="(pasajero, indice) in pasajeros"
              :key="pasajero.id"
              class="rounded-[28px] bg-white p-6 shadow-sm sm:p-8"
            >
              <div class="flex items-center justify-between gap-4 border-b border-slate-200 pb-6">
                <div class="flex items-center gap-4">
                  <div class="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-navy">
                    <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 14a4 4 0 10-8 0m8 0a4 4 0 11-8 0m8 0v1a2 2 0 002 2h1m-11-3v1a2 2 0 01-2 2H5" />
                    </svg>
                  </div>
                  <h2 class="text-2xl font-semibold text-navy">Pasajero {{ indice + 1 }}</h2>
                </div>

                <button
                  v-if="pasajeros.length > 1"
                  type="button"
                  class="rounded-xl p-2 text-red-500 transition-colors hover:bg-red-50"
                  @click="eliminarPasajero(indice)"
                >
                  <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6M9 7V4a1 1 0 011-1h4a1 1 0 011 1v3m-7 0h8" />
                  </svg>
                </button>
              </div>

              <div class="mt-8 grid gap-6 md:grid-cols-2">
                <label class="block" :data-error-activo="!!errores[`nombre-${indice}`]">
                  <span class="mb-2 block font-medium text-navy">Nombre</span>
                  <input
                    v-model="pasajero.nombre_pasajero"
                    type="text"
                    placeholder="Ej. Gabriela"
                    class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                  />
                  <span v-if="errores[`nombre-${indice}`]" class="mt-2 block text-sm text-red-500">{{ errores[`nombre-${indice}`] }}</span>
                </label>

                <label class="block" :data-error-activo="!!errores[`apellido-${indice}`]">
                  <span class="mb-2 block font-medium text-navy">Apellido</span>
                  <input
                    v-model="pasajero.apellido_pasajero"
                    type="text"
                    placeholder="Ej. Aguilar"
                    class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                  />
                  <span v-if="errores[`apellido-${indice}`]" class="mt-2 block text-sm text-red-500">{{ errores[`apellido-${indice}`] }}</span>
                </label>

                <label class="block" :data-error-activo="!!errores[`tipo-documento-${indice}`]">
                  <span class="mb-2 block font-medium text-navy">Tipo de Documento</span>
                  <select
                    v-model="pasajero.tipo_documento_pasajero"
                    class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                  >
                    <option value="CEDULA">Cedula</option>
                    <option value="PASAPORTE">Pasaporte</option>
                    <option value="RUC">RUC</option>
                    <option value="OTRO">Otro</option>
                  </select>
                  <span v-if="errores[`tipo-documento-${indice}`]" class="mt-2 block text-sm text-red-500">{{ errores[`tipo-documento-${indice}`] }}</span>
                </label>

                <label class="block" :data-error-activo="!!errores[`documento-${indice}`]">
                  <span class="mb-2 block font-medium text-navy">Numero de Documento</span>
                  <input
                    v-model="pasajero.numero_documento_pasajero"
                    type="text"
                    inputmode="text"
                    placeholder="Ej. 1710034065"
                    class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                  />
                  <span class="mt-1.5 block text-xs text-slate-400">Ejemplo cedula: 1723457898. Se validan 10 digitos.</span>
                  <span v-if="errores[`documento-${indice}`]" class="mt-2 block text-sm text-red-500">{{ errores[`documento-${indice}`] }}</span>
                </label>

                <label class="block" :data-error-activo="!!errores[`fecha-${indice}`]">
                  <span class="mb-2 block font-medium text-navy">Fecha de Nacimiento</span>
                  <input
                    v-model="pasajero.fecha_nacimiento_pasajero"
                    type="date"
                    :max="new Date().toISOString().split('T')[0]"
                    class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                  />
                  <span v-if="errores[`fecha-${indice}`]" class="mt-2 block text-sm text-red-500">{{ errores[`fecha-${indice}`] }}</span>
                </label>

                <label class="block" :data-error-activo="!!errores[`nacionalidad-${indice}`]">
                  <span class="mb-2 block font-medium text-navy">Nacionalidad</span>
                  <select
                    v-model="pasajero.id_pais_nacionalidad"
                    class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                  >
                    <option value="">Seleccionar pais...</option>
                    <option v-for="pais in opcionesPaises" :key="pais.valor" :value="pais.valor">
                      {{ pais.etiqueta }}
                    </option>
                  </select>
                  <span v-if="errores[`nacionalidad-${indice}`]" class="mt-2 block text-sm text-red-500">{{ errores[`nacionalidad-${indice}`] }}</span>
                </label>

                <label class="block" :data-error-activo="!!errores[`email-${indice}`]">
                  <span class="mb-2 block font-medium text-navy">Email de Contacto</span>
                  <input
                    v-model="pasajero.email_contacto_pasajero"
                    type="email"
                    placeholder="Ej. nombre@email.com"
                    class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                  />
                  <span v-if="errores[`email-${indice}`]" class="mt-2 block text-sm text-red-500">{{ errores[`email-${indice}`] }}</span>
                </label>

                <label class="block" :data-error-activo="!!errores[`telefono-${indice}`]">
                  <span class="mb-2 block font-medium text-navy">Telefono de Contacto</span>
                  <input
                    v-model="pasajero.telefono_contacto_pasajero"
                    type="text"
                    placeholder="Ej. 0987654321"
                    class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                  />
                  <span v-if="errores[`telefono-${indice}`]" class="mt-2 block text-sm text-red-500">{{ errores[`telefono-${indice}`] }}</span>
                </label>

                <label class="block md:col-span-2" :data-error-activo="!!errores[`genero-${indice}`]">
                  <span class="mb-2 block font-medium text-navy">Genero</span>
                  <select
                    v-model="pasajero.genero_pasajero"
                    class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                  >
                    <option value="MASCULINO">Masculino</option>
                    <option value="FEMENINO">Femenino</option>
                    <option value="OTRO">Otro</option>
                  </select>
                  <span v-if="errores[`genero-${indice}`]" class="mt-2 block text-sm text-red-500">{{ errores[`genero-${indice}`] }}</span>
                </label>

                <label class="flex items-center gap-3 md:col-span-2">
                  <input
                    v-model="pasajero.requiere_asistencia"
                    type="checkbox"
                    class="h-5 w-5 rounded border-slate-300 text-blue-accent focus:ring-blue-accent"
                  />
                  <span class="font-medium text-navy">Requiere asistencia especial</span>
                </label>

                <label class="block md:col-span-2">
                  <span class="mb-2 block font-medium text-navy">Observaciones</span>
                  <textarea
                    v-model="pasajero.observaciones_pasajero"
                    rows="4"
                    placeholder="Alergias, necesidades especiales, etc."
                    class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                  />
                </label>
              </div>
            </article>
          </div>

          <aside class="lg:sticky lg:top-24 lg:self-start">
            <div class="rounded-[28px] bg-white p-8 shadow-sm">
              <h2 class="text-2xl font-bold text-navy">Resumen del Vuelo</h2>
              <div class="mt-6 space-y-4 text-text-muted">
                <div class="flex items-center justify-between">
                  <span>Codigo</span>
                  <span class="font-semibold text-navy">{{ vuelo?.numeroVuelo }}</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>Fecha</span>
                  <span class="font-semibold text-navy">{{ vuelo?.fechaHoraSalida ? new Intl.DateTimeFormat('es-EC', { day: 'numeric', month: 'long', year: 'numeric' }).format(new Date(vuelo.fechaHoraSalida)) : 'Fecha por confirmar' }}</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>Pasajeros</span>
                  <span class="font-semibold text-navy">{{ pasajeros.length }}</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>Clase</span>
                  <span class="font-semibold text-navy">{{ vuelo?.clase }}</span>
                </div>
              </div>

              <div class="mt-6 border-t border-slate-200 pt-6">
                <div class="flex items-center justify-between">
                  <span class="text-2xl font-semibold text-navy">Desde</span>
                  <span class="text-4xl font-light text-navy">${{ Number(vuelo?.precioBase || 0).toFixed(2) }}</span>
                </div>
              </div>

              <button
                type="button"
                class="mt-8 w-full rounded-2xl bg-gold px-6 py-4 font-semibold text-navy transition-colors hover:bg-gold-light disabled:cursor-not-allowed disabled:bg-gold/60"
                :disabled="navegando"
                @click="continuarAsientos"
              >
                {{ navegando ? 'Continuando...' : 'Continuar a Asientos' }}
              </button>

              <button
                type="button"
                class="mt-4 w-full rounded-2xl border border-slate-300 px-6 py-4 font-semibold text-navy transition-colors hover:bg-slate-50"
                @click="router.push({ name: 'detalle-vuelo', params: { id: vuelo?.idVuelo } })"
              >
                Volver al vuelo
              </button>
            </div>
          </aside>
        </div>
      </div>
    </section>
  </div>
</template>
