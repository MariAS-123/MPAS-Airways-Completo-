<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { IMAGENES } from '@/config/imagenes'
import { useAutenticacionStore } from '@/stores/autenticacion.store'
import { useClienteStore } from '@/stores/cliente.store'

const auth = useAutenticacionStore()
const cliente = useClienteStore()
const router = useRouter()

const nombreVisible = computed(() =>
  cliente.nombreMostrado || auth.usuario?.username || 'Cliente',
)

async function handleLogout() {
  cliente.limpiar()
  await auth.logout()
  router.push('/')
}
</script>

<template>
  <div class="min-h-screen bg-background">
    <header class="sticky top-0 z-40 border-b border-white/10 bg-navy shadow-lg">
      <div class="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 sm:px-6 lg:px-8">
        <RouterLink to="/" class="flex items-center gap-3">
          <img :src="IMAGENES.logoPrincipal" alt="MPAS Airways" class="h-10 object-contain" />
        </RouterLink>

        <nav class="hidden items-center gap-8 text-base text-white/90 md:flex">
          <RouterLink to="/" class="transition-colors hover:text-gold-light">Inicio</RouterLink>
          <RouterLink :to="{ name: 'buscar-vuelos' }" class="transition-colors hover:text-gold-light">Vuelos</RouterLink>
        </nav>

        <div class="flex items-center gap-3">
          <RouterLink
            :to="{ name: 'cliente-perfil' }"
            class="hidden text-base font-semibold text-gold transition-colors hover:text-gold-light md:block"
          >
            {{ nombreVisible }}
          </RouterLink>
          <button
            type="button"
            class="rounded-2xl border border-gold/50 bg-gold-light px-4 py-2 text-sm font-semibold text-navy transition-colors hover:bg-gold"
            @click="handleLogout"
          >
            Cerrar sesión
          </button>
        </div>
      </div>
    </header>

    <main class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <RouterView />
    </main>
  </div>
</template>
