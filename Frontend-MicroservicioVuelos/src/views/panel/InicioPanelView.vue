<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { usePanelPermisos } from '@/composables/usePanelPermisos'

const { auth, esAdministrador, tituloPanel } = usePanelPermisos()
const router = useRouter()

const nombreVisible = computed(() => auth.usuario?.username || tituloPanel.value)

const modulos = computed(() => {
  return [
    {
      titulo: 'Configuracion Base',
      color: 'text-orange-600',
      fondo: 'from-orange-50 to-white',
      icono: 'CB',
      descripcion: esAdministrador.value
        ? 'Catalogos base y red operativa principal del sistema.'
        : 'Infraestructura visible para la operacion de la aerolinea.',
      items: esAdministrador.value
        ? [
            { nombre: 'Paises', ruta: { name: 'panel-paises' } },
            { nombre: 'Ciudades', ruta: { name: 'panel-ciudades' } },
            { nombre: 'Aeropuertos', ruta: { name: 'panel-aeropuertos' } },
          ]
        : [{ nombre: 'Aeropuertos', ruta: { name: 'panel-aeropuertos' } }],
    },
    {
      titulo: 'Gestion Operativa',
      color: 'text-blue-600',
      fondo: 'from-blue-50 to-white',
      icono: 'GO',
      descripcion: 'Vuelos, escalas, asientos y equipaje para la operacion diaria.',
      items: [
        { nombre: 'Vuelos', ruta: { name: 'panel-vuelos' } },
        { nombre: 'Escalas y rutas', ruta: { name: 'panel-vuelos' } },
        { nombre: 'Asientos', ruta: { name: 'panel-asientos' } },
        { nombre: 'Equipaje', ruta: { name: 'panel-equipaje' } },
      ],
    },
    {
      titulo: 'Gestion Comercial',
      color: 'text-emerald-600',
      fondo: 'from-emerald-50 to-white',
      icono: 'GC',
      descripcion: 'Seguimiento de clientes, pasajeros, reservas, boletos y facturacion.',
      items: [
        { nombre: 'Clientes', ruta: { name: 'panel-clientes' } },
        { nombre: 'Pasajeros', ruta: { name: 'panel-pasajeros' } },
        { nombre: 'Reservas', ruta: { name: 'panel-reservas' } },
        { nombre: 'Boletos', ruta: { name: 'panel-boletos' } },
        { nombre: 'Facturas', ruta: { name: 'panel-facturas' } },
      ],
    },
  ]
})

function navegar(item) {
  if (!item.ruta) return
  router.push(item.ruta)
}
</script>

<template>
  <section class="space-y-8">
    <div class="overflow-hidden rounded-[30px] bg-gradient-to-r from-white via-white to-[#f4f7fc] p-8 shadow-sm">
      <div class="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p class="text-sm font-semibold uppercase tracking-[0.28em] text-gold-dark">Centro de control</p>
          <h1 class="mt-3 text-4xl font-bold text-navy">Panel {{ tituloPanel }}</h1>
          <p class="mt-3 max-w-2xl text-lg text-text-muted">
            Bienvenido, {{ nombreVisible }}. Desde aqui administras la operacion y el seguimiento comercial sin salir del panel.
          </p>
        </div>

        <div class="grid gap-3 sm:grid-cols-3">
          <div class="rounded-2xl border border-slate-200 bg-white px-5 py-4">
            <p class="text-xs font-semibold uppercase tracking-[0.16em] text-text-muted">Rol activo</p>
            <p class="mt-2 text-lg font-semibold text-navy">{{ tituloPanel }}</p>
          </div>
          <div class="rounded-2xl border border-slate-200 bg-white px-5 py-4">
            <p class="text-xs font-semibold uppercase tracking-[0.16em] text-text-muted">Modulos</p>
            <p class="mt-2 text-lg font-semibold text-navy">{{ modulos.length }}</p>
          </div>
          <div class="rounded-2xl border border-slate-200 bg-white px-5 py-4">
            <p class="text-xs font-semibold uppercase tracking-[0.16em] text-text-muted">Estado</p>
            <p class="mt-2 text-lg font-semibold text-emerald-600">Operativo</p>
          </div>
        </div>
      </div>
    </div>

    <section class="rounded-[28px] bg-white p-6 shadow-sm">
      <h2 class="text-xl font-semibold text-navy">Modulos del sistema</h2>
      <p class="mt-2 text-sm text-text-muted">Accesos organizados por area para trabajar mas rapido dentro del panel.</p>
      <div class="mt-6 grid gap-6 xl:grid-cols-3">
        <article
          v-for="modulo in modulos"
          :key="modulo.titulo"
          class="overflow-hidden rounded-[28px] border border-slate-200 bg-gradient-to-br p-6 shadow-sm transition-transform hover:-translate-y-0.5"
          :class="modulo.fondo"
        >
          <div class="flex items-start gap-4">
            <div class="flex h-14 w-14 items-center justify-center rounded-2xl bg-white shadow-sm">
              <span :class="['text-sm font-bold tracking-[0.18em]', modulo.color]">{{ modulo.icono }}</span>
            </div>
            <div>
              <h3 :class="['text-2xl font-semibold', modulo.color]">{{ modulo.titulo }}</h3>
              <p class="mt-2 text-sm leading-6 text-text-muted">{{ modulo.descripcion }}</p>
            </div>
          </div>

          <div class="mt-6 space-y-2 border-t border-slate-200/80 pt-5">
            <button
              v-for="item in modulo.items"
              :key="item.nombre"
              type="button"
              class="flex w-full items-center justify-between rounded-2xl bg-white/80 px-4 py-3 text-left text-navy transition-colors hover:bg-white"
              @click="navegar(item)"
            >
              <span class="font-medium">{{ item.nombre }}</span>
              <span aria-hidden="true" class="text-lg">›</span>
            </button>
          </div>
        </article>
      </div>
    </section>
  </section>
</template>
