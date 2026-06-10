<script setup>
defineProps({
  texto: { type: String, default: 'Cargando...' },
  mini:  { type: Boolean, default: false },
})
</script>

<template>
  <div :class="['flex flex-col items-center justify-center gap-3', mini ? 'py-4' : 'py-12']">
    <div
      :class="[
        'border-4 border-gold border-t-transparent rounded-full animate-spin',
        mini ? 'w-6 h-6' : 'w-12 h-12',
      ]"
    />
    <p v-if="!mini" class="text-text-muted text-sm">{{ texto }}</p>
  </div>
</template>
