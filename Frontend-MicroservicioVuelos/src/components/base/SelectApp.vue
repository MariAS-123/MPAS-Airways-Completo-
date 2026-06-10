<script setup>
defineProps({
  label:        { type: String,  default: '' },
  modelValue:   { default: '' },
  opciones:     { type: Array,   default: () => [] }, // [{ valor, etiqueta }]
  placeholder:  { type: String,  default: 'Seleccionar...' },
  error:        { type: String,  default: '' },
  requerido:    { type: Boolean, default: false },
  deshabilitado:{ type: Boolean, default: false },
  cargando:     { type: Boolean, default: false },
})
defineEmits(['update:modelValue'])
</script>

<template>
  <div class="flex flex-col gap-1.5">
    <label v-if="label" class="text-sm font-semibold text-text-main">
      {{ label }}<span v-if="requerido" class="text-error ml-0.5">*</span>
    </label>
    <div class="relative">
      <select
        :value="modelValue"
        :disabled="deshabilitado || cargando"
        :class="[
          'w-full px-4 py-3 pr-10 rounded-xl bg-gray-100 text-sm appearance-none',
          'border border-transparent focus:outline-none focus:border-blue-accent focus:ring-2 focus:ring-blue-accent/20',
          'transition-all duration-200 cursor-pointer',
          !modelValue ? 'text-text-muted' : 'text-text-main',
          error && '!border-error focus:!ring-error/20',
          (deshabilitado || cargando) && 'opacity-50 cursor-not-allowed',
        ]"
        @change="$emit('update:modelValue', $event.target.value)"
      >
        <option value="" disabled>{{ placeholder }}</option>
        <option
          v-for="op in opciones"
          :key="op.valor"
          :value="op.valor"
        >{{ op.etiqueta }}</option>
      </select>

      <div class="absolute right-3 top-1/2 -translate-y-1/2 pointer-events-none text-text-muted">
        <svg v-if="!cargando" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
        </svg>
        <span v-else class="w-4 h-4 border-2 border-text-muted border-t-transparent rounded-full animate-spin block"/>
      </div>
    </div>
    <p v-if="error" class="text-xs text-error flex items-center gap-1">
      <svg class="w-3 h-3 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
      </svg>
      {{ error }}
    </p>
  </div>
</template>
