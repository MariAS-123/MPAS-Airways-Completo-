<script setup>
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { IMAGENES } from '@/config/imagenes'
import { useCatalogosStore } from '@/stores/catalogos.store'

const router = useRouter()
const catalogos = useCatalogosStore()

const tipoViaje = ref('IDA_VUELTA')
const cargandoBusqueda = ref(false)
const errores = ref({})
const form = ref({
  origen: '',
  destino: '',
  salida: '',
  regreso: '',
})

const opcionesAeropuertos = computed(() => catalogos.opcionesAeropuertos)

const ventajas = [
  {
    titulo: 'Red Global',
    texto: 'Más de 200 destinos en 6 continentes. Conectamos el mundo entero.',
    color: 'bg-blue-50 text-navy',
    icono: 'globe',
  },
  {
    titulo: 'Servicio Premium',
    texto: 'Atención personalizada 24/7. Tu comodidad es nuestra prioridad.',
    color: 'bg-amber-50 text-gold-dark',
    icono: 'star',
  },
  {
    titulo: 'Seguridad Total',
    texto: 'Flota moderna y protocolos de seguridad de clase mundial.',
    color: 'bg-slate-100 text-navy',
    icono: 'shield',
  },
]

function iconPath(icono) {
  if (icono === 'globe') {
    return 'M12 2a10 10 0 100 20 10 10 0 000-20zm6.93 9h-3.17a15.7 15.7 0 00-1.8-6.2A8.02 8.02 0 0118.93 11zM12 4.07c.98 1.13 2.36 3.4 2.78 6.93H9.22C9.64 7.47 11.02 5.2 12 4.07zM4.07 13h3.17c.25 2.2.9 4.36 1.8 6.2A8.02 8.02 0 014.07 13zM7.24 11H4.07a8.02 8.02 0 015-6.2c-.9 1.84-1.55 4-1.83 6.2zm1.98 2h5.56c-.42 3.53-1.8 5.8-2.78 6.93-.98-1.13-2.36-3.4-2.78-6.93zm6.54 6.2c.9-1.84 1.55-4 1.83-6.2h3.17a8.02 8.02 0 01-5 6.2z'
  }
  if (icono === 'star') {
    return 'M11.05 3.93c.3-.92 1.6-.92 1.9 0l1.45 4.46a1 1 0 00.95.69h4.69c.97 0 1.37 1.24.59 1.81l-3.8 2.76a1 1 0 00-.36 1.12l1.45 4.46c.3.92-.76 1.68-1.54 1.12l-3.8-2.76a1 1 0 00-1.18 0l-3.8 2.76c-.78.56-1.84-.2-1.54-1.12l1.45-4.46a1 1 0 00-.36-1.12L3.37 10.9c-.78-.57-.38-1.81.59-1.81h4.69a1 1 0 00.95-.69l1.45-4.46z'
  }
  return 'M12 2l7 4v6c0 5-3.11 9.57-7 10-3.89-.43-7-5-7-10V6l7-4zm0 4.18L7 8.94V12c0 3.6 2.06 6.95 5 7.84 2.94-.89 5-4.24 5-7.84V8.94l-5-2.76z'
}

function validar() {
  const nuevosErrores = {}
  if (!form.value.origen) nuevosErrores.origen = 'Selecciona el aeropuerto de origen.'
  if (!form.value.destino) nuevosErrores.destino = 'Selecciona el aeropuerto de destino.'
  if (!form.value.salida) nuevosErrores.salida = 'Selecciona la fecha de salida.'
  if (tipoViaje.value === 'IDA_VUELTA' && !form.value.regreso) {
    nuevosErrores.regreso = 'Selecciona la fecha de regreso.'
  }
  if (form.value.origen && form.value.destino && form.value.origen === form.value.destino) {
    nuevosErrores.destino = 'El destino debe ser diferente al origen.'
  }

  errores.value = nuevosErrores
  return !Object.keys(nuevosErrores).length
}

function buscarVuelos() {
  if (!validar()) return

  cargandoBusqueda.value = true
  router
    .push({
      path: '/vuelos',
      query: {
        origen: form.value.origen,
        destino: form.value.destino,
        salida: form.value.salida,
        regreso: tipoViaje.value === 'IDA_VUELTA' ? form.value.regreso : '',
        tipo: tipoViaje.value,
      },
    })
    .finally(() => {
      cargandoBusqueda.value = false
    })
}

function cargarAeropuertosSiHaceFalta() {
  catalogos.cargarAeropuertos(true).catch(() => {})
}
</script>

<template>
  <div>
    <section class="relative overflow-hidden bg-navy">
      <img
        :src="IMAGENES.heroAvion"
        alt="Avión comercial de MPAS Airways"
        class="absolute inset-0 h-full w-full object-cover object-center opacity-20"
      />
      <div class="absolute inset-0 bg-[linear-gradient(90deg,rgba(11,31,59,0.95)_0%,rgba(11,31,59,0.88)_38%,rgba(19,51,92,0.72)_100%)]" />

      <div class="relative mx-auto max-w-7xl px-4 pb-16 pt-12 sm:px-6 lg:px-8 lg:pb-24 lg:pt-20">
        <div class="mx-auto max-w-4xl text-center">
          <h1 class="text-5xl font-extrabold leading-tight text-white sm:text-6xl">
            Vuela con <span class="text-gold">Elegancia</span>
          </h1>
          <p class="mx-auto mt-6 max-w-3xl text-xl leading-9 text-white/90">
            Descubre el mundo con MPAS Airways. Conectamos destinos, creamos experiencias.
          </p>
        </div>

        <div class="mx-auto mt-14 max-w-5xl rounded-[30px] bg-white p-5 shadow-[0_22px_65px_rgba(7,16,32,0.22)] sm:p-8">
          <div class="mb-7 flex flex-wrap gap-3">
            <button
              type="button"
              class="rounded-2xl px-6 py-3 text-lg font-semibold transition-colors"
              :class="tipoViaje === 'IDA_VUELTA' ? 'bg-navy text-white' : 'bg-slate-100 text-text-main hover:bg-slate-200'"
              @click="tipoViaje = 'IDA_VUELTA'"
            >
              Ida y Vuelta
            </button>
            <button
              type="button"
              class="rounded-2xl px-6 py-3 text-lg font-semibold transition-colors"
              :class="tipoViaje === 'SOLO_IDA' ? 'bg-navy text-white' : 'bg-slate-100 text-text-main hover:bg-slate-200'"
              @click="tipoViaje = 'SOLO_IDA'"
            >
              Solo Ida
            </button>
          </div>

          <form class="space-y-6" @submit.prevent="buscarVuelos">
            <div class="grid gap-5 lg:grid-cols-2">
              <label class="block">
                <span class="mb-2 flex items-center gap-2 text-lg font-semibold text-navy">
                  <svg class="h-5 w-5 text-blue-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.9" d="M12 21c4.97-4.244 7.5-7.744 7.5-10.5a7.5 7.5 0 10-15 0c0 2.756 2.53 6.256 7.5 10.5z" />
                    <circle cx="12" cy="10.5" r="2.25" stroke-width="1.9" />
                  </svg>
                  Origen
                </span>
                <select
                  v-model="form.origen"
                  class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-5 py-4 text-lg text-text-main outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                  @focus="cargarAeropuertosSiHaceFalta"
                >
                  <option value="">¿Desde dónde viajas?</option>
                  <option v-for="opcion in opcionesAeropuertos" :key="`origen-${opcion.valor}`" :value="opcion.valor">
                    {{ opcion.etiqueta }}
                  </option>
                </select>
                <p v-if="errores.origen" class="mt-2 text-sm text-error">{{ errores.origen }}</p>
              </label>

              <label class="block">
                <span class="mb-2 flex items-center gap-2 text-lg font-semibold text-navy">
                  <svg class="h-5 w-5 text-blue-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.9" d="M12 21c4.97-4.244 7.5-7.744 7.5-10.5a7.5 7.5 0 10-15 0c0 2.756 2.53 6.256 7.5 10.5z" />
                    <circle cx="12" cy="10.5" r="2.25" stroke-width="1.9" />
                  </svg>
                  Destino
                </span>
                <select
                  v-model="form.destino"
                  class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-5 py-4 text-lg text-text-main outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                  @focus="cargarAeropuertosSiHaceFalta"
                >
                  <option value="">¿A dónde quieres ir?</option>
                  <option v-for="opcion in opcionesAeropuertos" :key="`destino-${opcion.valor}`" :value="opcion.valor">
                    {{ opcion.etiqueta }}
                  </option>
                </select>
                <p v-if="errores.destino" class="mt-2 text-sm text-error">{{ errores.destino }}</p>
              </label>
            </div>

            <div class="grid gap-5 md:grid-cols-2">
              <label class="block">
                <span class="mb-2 flex items-center gap-2 text-lg font-semibold text-navy">
                  <svg class="h-5 w-5 text-blue-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.9" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                  </svg>
                  Salida
                </span>
                <input
                  v-model="form.salida"
                  type="date"
                  class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-5 py-4 text-lg text-text-main outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                />
                <p v-if="errores.salida" class="mt-2 text-sm text-error">{{ errores.salida }}</p>
              </label>

              <label v-if="tipoViaje === 'IDA_VUELTA'" class="block">
                <span class="mb-2 flex items-center gap-2 text-lg font-semibold text-navy">
                  <svg class="h-5 w-5 text-blue-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.9" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                  </svg>
                  Regreso
                </span>
                <input
                  v-model="form.regreso"
                  type="date"
                  class="w-full rounded-2xl border border-slate-200 bg-slate-50 px-5 py-4 text-lg text-text-main outline-none transition focus:border-blue-accent focus:ring-4 focus:ring-blue-accent/10"
                />
                <p v-if="errores.regreso" class="mt-2 text-sm text-error">{{ errores.regreso }}</p>
              </label>

            </div>

            <button
              type="submit"
              class="flex w-full items-center justify-center gap-3 rounded-2xl bg-gold px-6 py-4 text-xl font-semibold text-navy transition-colors hover:bg-gold-light"
            >
              <span
                v-if="cargandoBusqueda"
                class="h-5 w-5 animate-spin rounded-full border-2 border-navy border-t-transparent"
              />
              <svg v-else class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-4.35-4.35m1.85-5.15a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              Buscar Vuelos
            </button>
          </form>
        </div>
      </div>
    </section>

    <section class="bg-white py-20">
      <div class="mx-auto grid max-w-7xl gap-12 px-4 sm:px-6 lg:grid-cols-[1.1fr_1fr] lg:px-8">
        <div class="overflow-hidden rounded-[28px] shadow-[0_24px_70px_rgba(12,31,59,0.18)]">
          <img :src="IMAGENES.heroAvion" alt="Quiénes somos en MPAS Airways" class="h-full w-full object-cover" />
        </div>

        <div class="flex flex-col justify-center">
          <span class="text-sm font-semibold uppercase tracking-[0.26em] text-gold-dark">Quiénes Somos</span>
          <h2 class="mt-5 text-4xl font-bold text-navy sm:text-5xl">Una aerolínea pensada para viajar con estilo</h2>
          <p class="mt-6 text-lg leading-8 text-text-muted">
            En MPAS Airways diseñamos experiencias de viaje claras, elegantes y bien conectadas. Nuestro enfoque combina tecnología, atención cercana y rutas pensadas para que reservar sea tan agradable como el vuelo mismo.
          </p>
          <p class="mt-4 text-lg leading-8 text-text-muted">
            Desde la búsqueda de destinos hasta la gestión de tu reserva, queremos que cada detalle transmita confianza, comodidad y una identidad premium.
          </p>

          <div class="mt-8 grid gap-6 sm:grid-cols-2">
            <div class="rounded-3xl bg-slate-50 px-6 py-5">
              <p class="text-3xl font-extrabold text-gold-dark">200+</p>
              <p class="mt-1 text-sm font-medium text-text-muted">Destinos conectados globalmente</p>
            </div>
            <div class="rounded-3xl bg-slate-50 px-6 py-5">
              <p class="text-3xl font-extrabold text-gold-dark">24/7</p>
              <p class="mt-1 text-sm font-medium text-text-muted">Acompañamiento y soporte continuo</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section class="bg-[#F7F8FB] py-20">
      <div class="mx-auto max-w-7xl px-4 text-center sm:px-6 lg:px-8">
        <h2 class="text-4xl font-bold text-navy">¿Por qué volar con MPAS Airways?</h2>
        <p class="mt-4 text-xl text-text-muted">Experiencia premium en cada detalle</p>

        <div class="mt-14 grid gap-8 lg:grid-cols-3">
          <article
            v-for="ventaja in ventajas"
            :key="ventaja.titulo"
            class="rounded-[28px] bg-white px-8 py-10 text-center shadow-[0_10px_25px_rgba(15,23,42,0.08)]"
          >
            <div class="mx-auto flex h-16 w-16 items-center justify-center rounded-full" :class="ventaja.color">
              <svg class="h-8 w-8" fill="currentColor" viewBox="0 0 24 24">
                <path :d="iconPath(ventaja.icono)" />
              </svg>
            </div>
            <h3 class="mt-8 text-3xl font-bold text-navy">{{ ventaja.titulo }}</h3>
            <p class="mt-5 text-lg leading-8 text-text-muted">{{ ventaja.texto }}</p>
          </article>
        </div>
      </div>
    </section>

    <section class="bg-white py-20">
      <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div class="relative overflow-hidden rounded-[30px] bg-[linear-gradient(120deg,#183761_0%,#1E497C_100%)] px-8 py-16 text-center shadow-[0_20px_50px_rgba(11,31,59,0.18)] sm:px-16">
          <img
            :src="IMAGENES.heroAvion"
            alt="Explorar destinos"
            class="pointer-events-none absolute inset-0 h-full w-full object-cover opacity-10"
          />
          <div class="relative mx-auto max-w-3xl">
            <h2 class="text-4xl font-bold text-white">¿Listo para tu próxima aventura?</h2>
            <p class="mt-5 text-xl leading-9 text-white/90">
              Reserva hoy y aprovecha nuestras ofertas exclusivas en vuelos internacionales
            </p>
            <button
              type="button"
              class="mt-10 inline-flex items-center justify-center gap-3 rounded-2xl bg-gold px-9 py-4 text-xl font-semibold text-navy transition-colors hover:bg-gold-light"
              @click="router.push('/vuelos')"
            >
              Explorar Destinos
              <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2 16l20-5-8 8-2.5-5.5L6 11l-4-1 20-5-5 20-3.5-7L2 16z" />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>
