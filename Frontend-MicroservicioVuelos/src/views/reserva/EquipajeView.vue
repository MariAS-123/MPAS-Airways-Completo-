<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import CheckoutStepper from '@/components/CheckoutStepper.vue'
import { useReservaStore } from '@/stores/reserva.store'

const router = useRouter()
const reserva = useReservaStore()

const COSTO_BODEGA = 45
const equipajeLocal = ref([])

const vuelo = computed(() => reserva.vuelo)
const pasajeros = computed(() => reserva.pasajeros || [])
const asientos = computed(() => reserva.asientos || [])

function moneda(valor) {
  return new Intl.NumberFormat('es-EC', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(Number(valor || 0))
}

function nombrePasajero(pasajero, indice) {
  return [pasajero?.nombre_pasajero, pasajero?.apellido_pasajero].filter(Boolean).join(' ') || `Pasajero ${indice + 1}`
}

const itemsEquipaje = computed(() =>
  pasajeros.value.map((pasajero, indice) => {
    const asiento = asientos.value[indice] || null
    const guardado = equipajeLocal.value[indice] || null

    return {
      pasajeroIndex: indice,
      nombre: nombrePasajero(pasajero, indice),
      idAsiento: asiento?.idAsiento || null,
      numeroAsiento: asiento?.numeroAsiento || 'Pendiente',
      equipajeMano: true,
      equipajeBodega: Boolean(guardado?.equipajeBodega),
      pesoManoKg: 10,
      pesoBodegaKg: guardado?.equipajeBodega ? 23 : null,
    }
  }),
)

const totalExtra = computed(() =>
  itemsEquipaje.value.reduce((total, item) => total + (item.equipajeBodega ? COSTO_BODEGA : 0), 0),
)

function sincronizarStore() {
  reserva.setEquipaje(itemsEquipaje.value)
}

function inicializarEquipaje() {
  equipajeLocal.value = pasajeros.value.map((_, indice) => ({
    pasajeroIndex: indice,
    equipajeBodega: Boolean(reserva.equipaje?.[indice]?.equipajeBodega),
  }))
  sincronizarStore()
}

function setBodega(indice, valor) {
  equipajeLocal.value[indice] = {
    pasajeroIndex: indice,
    equipajeBodega: valor,
  }
  sincronizarStore()
}

function continuarPago() {
  sincronizarStore()
  router.push({ name: 'pago-reserva' })
}

onMounted(() => {
  if (!vuelo.value) {
    router.replace({ name: 'buscar-vuelos' })
    return
  }

  if (!pasajeros.value.length) {
    router.replace({ name: 'datos-pasajeros' })
    return
  }

  if (!asientos.value.length) {
    router.replace({ name: 'seleccion-asientos' })
    return
  }

  inicializarEquipaje()
})
</script>

<template>
  <div>
    <CheckoutStepper :paso-actual="4" />

    <section class="min-h-[calc(100vh-64px)] bg-background py-10">
      <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div class="grid gap-8 lg:grid-cols-[1.35fr_0.8fr]">
          <div class="space-y-6">
            <div class="rounded-[28px] bg-white p-8 shadow-sm">
              <p class="text-sm font-semibold uppercase tracking-[0.28em] text-gold-dark">Paso 4</p>
              <h1 class="mt-3 text-3xl font-bold text-navy">Equipaje</h1>
              <p class="mt-4 text-text-muted">
                MPAS Airways permite agregar 1 equipaje de bodega extra por pasajero, con un peso maximo de 23 kg y un costo fijo de $45.
              </p>
            </div>

            <article
              v-for="item in itemsEquipaje"
              :key="item.pasajeroIndex"
              class="rounded-[28px] bg-white p-6 shadow-sm sm:p-8"
            >
              <div class="flex flex-col gap-2 border-b border-slate-200 pb-5 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <h2 class="text-2xl font-semibold text-navy">{{ item.nombre }}</h2>
                  <p class="mt-1 text-sm text-text-muted">Asiento seleccionado: {{ item.numeroAsiento }}</p>
                </div>
                <span class="rounded-full bg-slate-100 px-3 py-1 text-sm font-medium text-slate-600">
                  Pasajero {{ item.pasajeroIndex + 1 }}
                </span>
              </div>

              <div class="mt-6 space-y-4">
                <div class="flex flex-col gap-4 rounded-[24px] border border-emerald-200 bg-emerald-50/60 p-5 sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    <p class="text-xl font-semibold text-navy">Equipaje de Mano</p>
                    <p class="mt-1 text-sm text-text-muted">Maximo 10kg por pasajero.</p>
                    <p class="mt-2 text-sm font-medium text-emerald-600">Siempre incluido en tu tarifa</p>
                  </div>
                  <div class="text-right">
                    <p class="text-sm text-text-muted">Costo</p>
                    <p class="text-lg font-semibold text-emerald-600">Incluido</p>
                  </div>
                </div>

                <div class="rounded-[24px] border border-slate-200 p-5">
                  <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                    <div>
                      <p class="text-xl font-semibold text-navy">Equipaje de Bodega</p>
                      <p class="mt-1 text-sm text-text-muted">Maximo 23kg por pasajero.</p>
                    </div>
                    <div class="text-right">
                      <p class="text-sm text-text-muted">Costo fijo</p>
                      <p class="text-2xl font-light text-navy">{{ moneda(COSTO_BODEGA) }}</p>
                    </div>
                  </div>

                  <div class="mt-5 grid gap-3 sm:grid-cols-2">
                    <button
                      type="button"
                      class="rounded-2xl border px-4 py-3 font-semibold transition-colors"
                      :class="
                        !item.equipajeBodega
                          ? 'border-navy bg-navy text-white'
                          : 'border-slate-200 bg-slate-50 text-navy hover:bg-slate-100'
                      "
                      @click="setBodega(item.pasajeroIndex, false)"
                    >
                      No agregar
                    </button>

                    <button
                      type="button"
                      class="rounded-2xl border px-4 py-3 font-semibold transition-colors"
                      :class="
                        item.equipajeBodega
                          ? 'border-gold bg-gold text-navy'
                          : 'border-slate-200 bg-slate-50 text-navy hover:bg-slate-100'
                      "
                      @click="setBodega(item.pasajeroIndex, true)"
                    >
                      Agregar 1 maleta - {{ moneda(COSTO_BODEGA) }}
                    </button>
                  </div>
                </div>
              </div>
            </article>

            <section class="rounded-[28px] border border-blue-accent/20 bg-blue-50/60 p-6">
              <h2 class="text-xl font-semibold text-blue-accent">Informacion de Equipaje</h2>
              <div class="mt-4 space-y-2 text-sm text-blue-accent">
                <p>- El equipaje de mano (10kg) esta siempre incluido.</p>
                <p>- El equipaje de bodega (23kg) tiene un costo adicional de {{ moneda(COSTO_BODEGA) }}.</p>
                <p>- Solo se permite 1 maleta de bodega extra por pasajero.</p>
                <p>- Si no deseas agregar equipaje, puedes continuar sin problema.</p>
              </div>
            </section>
          </div>

          <aside class="lg:sticky lg:top-24 lg:self-start">
            <div class="rounded-[28px] bg-white p-8 shadow-sm">
              <h2 class="text-2xl font-bold text-navy">Resumen de Equipaje</h2>
              <div class="mt-6 space-y-4 text-text-muted">
                <div class="flex items-center justify-between">
                  <span>Equipaje de mano (10kg)</span>
                  <span class="font-semibold text-emerald-600">Incluido</span>
                </div>

                <div
                  v-for="item in itemsEquipaje"
                  :key="`resumen-${item.pasajeroIndex}`"
                  class="flex items-center justify-between border-t border-slate-100 pt-4"
                >
                  <div>
                    <p class="font-medium text-navy">{{ item.nombre }}</p>
                    <p class="text-sm text-text-muted">{{ item.equipajeBodega ? '1 maleta de bodega' : 'Sin bodega extra' }}</p>
                  </div>
                  <span class="font-semibold text-navy">{{ item.equipajeBodega ? moneda(COSTO_BODEGA) : moneda(0) }}</span>
                </div>
              </div>

              <div class="mt-6 border-t border-slate-200 pt-6">
                <div class="flex items-center justify-between">
                  <span class="text-2xl font-semibold text-navy">Costo adicional</span>
                  <span class="text-4xl font-light text-navy">{{ moneda(totalExtra) }}</span>
                </div>
              </div>

              <button
                type="button"
                class="mt-8 w-full rounded-2xl bg-gold px-6 py-4 font-semibold text-navy transition-colors hover:bg-gold-light"
                @click="continuarPago"
              >
                Continuar
              </button>

              <button
                type="button"
                class="mt-4 w-full rounded-2xl border border-slate-300 px-6 py-4 font-semibold text-navy transition-colors hover:bg-slate-50"
                @click="router.push({ name: 'seleccion-asientos' })"
              >
                Volver
              </button>
            </div>
          </aside>
        </div>
      </div>
    </section>
  </div>
</template>
