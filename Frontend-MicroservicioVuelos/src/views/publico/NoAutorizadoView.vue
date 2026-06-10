<script setup>
import { useRouter } from 'vue-router'

const router = useRouter()
</script>

<template>
  <div class="flex flex-col items-center justify-center py-24 px-4 text-center">
    <h1 class="text-3xl font-bold text-error">403 — Sin autorización</h1>
    <p class="text-text-muted mt-3">No tienes permisos para acceder a esta sección.</p>
    <button
      class="mt-6 px-5 py-2 bg-navy text-white rounded hover:bg-navy-light transition-colors"
      @click="router.push('/')"
    >
      Volver al inicio
    </button>
  </div>
</template>
