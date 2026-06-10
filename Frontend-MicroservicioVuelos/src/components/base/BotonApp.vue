<script setup>
defineProps({
  variante:     { type: String, default: 'primario' }, // primario | secundario | peligro | outline
  tipo:         { type: String, default: 'button' },
  cargando:     { type: Boolean, default: false },
  deshabilitado:{ type: Boolean, default: false },
  ancho:        { type: Boolean, default: false },
})
</script>

<template>
  <button
    :type="tipo"
    :disabled="deshabilitado || cargando"
    :class="[
      'inline-flex items-center justify-center gap-2 px-5 py-3 rounded-xl font-semibold text-sm',
      'transition-all duration-200 select-none focus-visible:ring-2 focus-visible:ring-offset-2',
      ancho && 'w-full',
      variante === 'primario'  && 'bg-gold hover:bg-gold-dark text-navy focus-visible:ring-gold',
      variante === 'secundario'&& 'bg-white border border-border text-text-main hover:bg-gray-50 focus-visible:ring-blue-accent',
      variante === 'peligro'   && 'bg-error hover:bg-red-600 text-white focus-visible:ring-error',
      variante === 'outline'   && 'border-2 border-navy text-navy hover:bg-navy hover:text-white focus-visible:ring-navy',
      (deshabilitado || cargando) && 'opacity-60 cursor-not-allowed',
    ]"
  >
    <span
      v-if="cargando"
      class="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin"
    />
    <slot />
  </button>
</template>
