<script setup>
import { IMAGENES } from '@/config/imagenes'

const enlacesCorporativos = [
  { etiqueta: 'Inicio', to: '/' },
  { etiqueta: 'Vuelos', to: '/vuelos' },
  { etiqueta: 'Iniciar sesión', to: '/login' },
  { etiqueta: 'Registro', to: '/registro' },
]

const redes = [
  { etiqueta: 'Facebook', href: 'https://facebook.com' },
  { etiqueta: 'Instagram', href: 'https://instagram.com' },
  { etiqueta: 'LinkedIn', href: 'https://linkedin.com' },
]
</script>

<template>
  <footer class="bg-navy pb-6 pt-12 text-white">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
      <div class="grid gap-10 md:grid-cols-[1.4fr_1fr_1fr]">
        <div>
          <img :src="IMAGENES.logoPrincipal" alt="MPAS Airways" class="mb-4 h-9 object-contain" />
          <p class="max-w-sm text-sm leading-6 text-white/70">
            MPAS Airways conecta destinos con una experiencia de reserva elegante, clara y segura.
          </p>
        </div>

        <div>
          <h3 class="mb-4 text-sm font-semibold uppercase tracking-[0.24em] text-gold">Corporativo</h3>
          <div class="space-y-3 text-sm">
            <RouterLink
              v-for="enlace in enlacesCorporativos"
              :key="enlace.to"
              :to="enlace.to"
              class="block text-white/70 transition-colors hover:text-white"
            >
              {{ enlace.etiqueta }}
            </RouterLink>
          </div>
        </div>

        <div>
          <h3 class="mb-4 text-sm font-semibold uppercase tracking-[0.24em] text-gold">Redes</h3>
          <div class="space-y-3 text-sm">
            <a
              v-for="red in redes"
              :key="red.etiqueta"
              :href="red.href"
              target="_blank"
              rel="noreferrer"
              class="block text-white/70 transition-colors hover:text-white"
            >
              {{ red.etiqueta }}
            </a>
          </div>
        </div>
      </div>

      <div class="mt-10 border-t border-white/10 pt-5 text-center text-xs text-white/45">
        © {{ new Date().getFullYear() }} MPAS Airways. Todos los derechos reservados.
      </div>
    </div>
  </footer>
</template>
