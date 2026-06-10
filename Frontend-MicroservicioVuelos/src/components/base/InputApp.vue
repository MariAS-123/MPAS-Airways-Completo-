<script setup>
defineProps({
  label:        { type: String,  default: '' },
  modelValue:   { default: '' },
  tipo:         { type: String,  default: 'text' },
  placeholder:  { type: String,  default: '' },
  error:        { type: String,  default: '' },
  requerido:    { type: Boolean, default: false },
  deshabilitado:{ type: Boolean, default: false },
  autocomplete: { type: String,  default: 'off' },
})
defineEmits(['update:modelValue'])
</script>

<template>
  <div class="flex flex-col gap-1.5">
    <label v-if="label" class="text-sm font-semibold text-text-main">
      {{ label }}<span v-if="requerido" class="text-error ml-0.5">*</span>
    </label>
    <input
      :type="tipo"
      :value="modelValue"
      :placeholder="placeholder"
      :disabled="deshabilitado"
      :autocomplete="autocomplete"
      :class="[
        'w-full px-4 py-3 rounded-xl bg-gray-100 text-text-main placeholder-text-muted text-sm',
        'border border-transparent focus:outline-none focus:border-blue-accent focus:ring-2 focus:ring-blue-accent/20',
        'transition-all duration-200',
        error && '!border-error focus:!ring-error/20',
        deshabilitado && 'opacity-50 cursor-not-allowed',
      ]"
      @input="$emit('update:modelValue', $event.target.value)"
    />
    <p v-if="error" class="text-xs text-error flex items-center gap-1">
      <svg class="w-3 h-3 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
      </svg>
      {{ error }}
    </p>
  </div>
</template>
