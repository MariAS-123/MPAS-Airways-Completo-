<script setup>
const props = defineProps({
  pasoActual: { type: Number, default: 1 },
})

const pasos = [
  { numero: 1, etiqueta: 'Vuelos' },
  { numero: 2, etiqueta: 'Pasajeros' },
  { numero: 3, etiqueta: 'Asientos' },
  { numero: 4, etiqueta: 'Equipaje' },
  { numero: 5, etiqueta: 'Pago' },
  { numero: 6, etiqueta: 'Confirmacion' },
]

function estadoPaso(numero) {
  if (numero < props.pasoActual) return 'completado'
  if (numero === props.pasoActual) return 'actual'
  return 'pendiente'
}
</script>

<template>
  <section class="border-b border-slate-200 bg-white shadow-sm">
    <div class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <div class="grid grid-cols-3 gap-y-6 md:grid-cols-6 md:gap-x-4">
        <template v-for="paso in pasos" :key="paso.numero">
          <div class="flex items-center">
            <div class="flex flex-col items-center">
              <div
                class="flex h-11 w-11 items-center justify-center rounded-full border-2 text-base font-semibold transition-colors"
                :class="{
                  'border-emerald-500 bg-emerald-500 text-white': estadoPaso(paso.numero) === 'completado',
                  'border-gold bg-gold text-navy': estadoPaso(paso.numero) === 'actual',
                  'border-slate-200 bg-white text-slate-500': estadoPaso(paso.numero) === 'pendiente',
                }"
              >
                <svg
                  v-if="estadoPaso(paso.numero) === 'completado'"
                  class="h-5 w-5"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.4" d="M5 13l4 4L19 7" />
                </svg>
                <span v-else>{{ paso.numero }}</span>
              </div>

              <span
                class="mt-2 text-center text-sm font-medium"
                :class="{
                  'text-emerald-600': estadoPaso(paso.numero) === 'completado',
                  'text-navy': estadoPaso(paso.numero) === 'actual',
                  'text-slate-500': estadoPaso(paso.numero) === 'pendiente',
                }"
              >
                {{ paso.etiqueta }}
              </span>
            </div>

            <div
              v-if="paso.numero < pasos.length"
              class="mx-4 hidden h-0.5 flex-1 md:block"
              :class="{
                'bg-emerald-500': paso.numero < pasoActual,
                'bg-slate-200': paso.numero >= pasoActual,
              }"
            />
          </div>
        </template>
      </div>
    </div>
  </section>
</template>
