<script setup>
defineProps({
  visible:           { type: Boolean, default: false },
  titulo:            { type: String,  default: 'Confirmar acción' },
  mensaje:           { type: String,  default: '' },
  etiquetaConfirmar: { type: String,  default: 'Confirmar' },
  etiquetaCancelar:  { type: String,  default: 'Cancelar' },
  variante:          { type: String,  default: 'primario' }, // primario | peligro
})
defineEmits(['confirmar', 'cancelar'])
</script>

<template>
  <Teleport to="body">
    <Transition name="dialog">
      <div
        v-if="visible"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm"
        @click.self="$emit('cancelar')"
      >
        <div class="bg-white rounded-2xl shadow-2xl max-w-md w-full p-6 transform">
          <h3 class="text-lg font-bold text-text-main mb-2">{{ titulo }}</h3>
          <p class="text-text-muted text-sm leading-relaxed mb-6">{{ mensaje }}</p>
          <div class="flex gap-3 justify-end">
            <button
              class="px-5 py-2.5 rounded-xl border border-border text-text-main text-sm font-medium hover:bg-gray-50 transition-colors"
              @click="$emit('cancelar')"
            >{{ etiquetaCancelar }}</button>
            <button
              :class="[
                'px-5 py-2.5 rounded-xl text-sm font-semibold transition-colors',
                variante === 'peligro'
                  ? 'bg-error text-white hover:bg-red-600'
                  : 'bg-gold text-navy hover:bg-gold-dark',
              ]"
              @click="$emit('confirmar')"
            >{{ etiquetaConfirmar }}</button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.dialog-enter-active, .dialog-leave-active { transition: all 0.2s ease; }
.dialog-enter-from, .dialog-leave-to { opacity: 0; }
.dialog-enter-from .bg-white, .dialog-leave-to .bg-white { transform: scale(0.95); }
</style>
