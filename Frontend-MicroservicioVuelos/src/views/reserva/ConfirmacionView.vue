<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import CheckoutStepper from '@/components/CheckoutStepper.vue'

const router = useRouter()
const KEY_CONFIRMACION = 'mpas_confirmacion'

const confirmacion = ref(null)
const enviandoEmail = ref(false)
const emailEnviado = ref(false)

const pasajeroPrincipal = computed(() => confirmacion.value?.pasajeros?.[0] || null)
const pasajeros = computed(() => confirmacion.value?.pasajeros || [])
const pasajerosConBodega = computed(() => pasajeros.value.filter((item) => item.equipajeBodega))

function moneda(valor) {
  return new Intl.NumberFormat('es-EC', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(Number(valor || 0))
}

function fechaLegible(valor) {
  if (!valor) return 'Fecha por confirmar'
  return new Intl.DateTimeFormat('es-EC', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(new Date(valor))
}

function telefonoLegible(valor) {
  return String(valor || '').trim() || '-'
}

function etiquetaEquipaje(index) {
  const sufijo = String(index + 1).padStart(3, '0')
  return `${confirmacion.value?.numeroVuelo || 'AV1001'}-${sufijo}`
}

function boletoEmitido(index) {
  const sufijo = String(index + 1).padStart(3, '0')
  return `TK-${confirmacion.value?.numeroVuelo || 'AV1001'}-${sufijo}`
}

function volverDespuesConfirmacion() {
  sessionStorage.removeItem(KEY_CONFIRMACION)
  if (confirmacion.value?.urlRetorno) {
    window.location.assign(confirmacion.value.urlRetorno)
    return
  }
  router.push({ name: 'inicio' })
}

async function simularEnvioEmail() {
  if (enviandoEmail.value) return
  enviandoEmail.value = true

  await new Promise((resolve) => setTimeout(resolve, 1400))

  enviandoEmail.value = false
  emailEnviado.value = true

  setTimeout(() => {
    volverDespuesConfirmacion()
  }, 1200)
}

onMounted(() => {
  try {
    confirmacion.value = JSON.parse(sessionStorage.getItem(KEY_CONFIRMACION) || 'null')
  } catch {
    confirmacion.value = null
  }
})
</script>

<template>
  <div>
    <CheckoutStepper :paso-actual="6" />

    <section class="min-h-[calc(100vh-64px)] bg-background py-10">
      <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div class="rounded-[32px] bg-white p-6 shadow-sm sm:p-10">
          <div class="text-center">
            <div class="mx-auto flex h-20 w-20 items-center justify-center rounded-full bg-emerald-100 text-emerald-600">
              <span class="text-4xl">✓</span>
            </div>
            <h1 class="mt-6 text-4xl font-bold text-navy sm:text-5xl">¡Reserva Confirmada!</h1>
            <p class="mt-3 text-lg text-text-muted sm:text-xl">Tu reserva ha sido procesada exitosamente</p>
          </div>

          <div
            v-if="confirmacion"
            class="mx-auto mt-8 max-w-5xl rounded-[30px] bg-navy px-6 py-8 text-center text-white sm:px-8 sm:py-9"
          >
            <p class="text-xl font-semibold sm:text-2xl">Código de Reserva</p>
            <p class="mt-4 text-5xl font-bold tracking-wide sm:text-6xl">{{ confirmacion.codigoReserva || 'AV1001' }}</p>
            <p class="mt-3 text-base text-white/80 sm:text-lg">Guarda este código para gestionar tu reserva</p>
          </div>

          <div v-if="confirmacion" class="mt-10 space-y-8">
            <div class="grid gap-6 lg:grid-cols-2">
              <section class="rounded-[28px] bg-slate-50 p-6">
                <h2 class="text-xl font-semibold text-navy sm:text-2xl">Detalles del Vuelo</h2>
                <div class="mt-6 grid gap-4 text-sm text-text-muted sm:grid-cols-[120px_1fr] sm:text-base">
                  <span>Vuelo:</span>
                  <span class="font-semibold text-navy">{{ confirmacion.numeroVuelo }}</span>
                  <span>Ruta:</span>
                  <span class="font-semibold text-navy">{{ confirmacion.ruta }}</span>
                  <span>Fecha:</span>
                  <span class="font-semibold text-navy">{{ fechaLegible(confirmacion.fecha) }}</span>
                  <span>Horario:</span>
                  <span class="font-semibold text-navy">{{ confirmacion.horario }}</span>
                  <span>Asiento:</span>
                  <span class="font-semibold text-navy">{{ pasajeroPrincipal?.asiento || '-' }}</span>
                </div>
              </section>

              <section class="rounded-[28px] bg-slate-50 p-6">
                <h2 class="text-xl font-semibold text-navy sm:text-2xl">Pasajero</h2>
                <div class="mt-6 grid gap-4 text-sm text-text-muted sm:grid-cols-[120px_1fr] sm:text-base">
                  <span>Nombre:</span>
                  <span class="font-semibold text-navy">{{ pasajeroPrincipal?.nombre || '-' }}</span>
                  <span>Documento:</span>
                  <span class="font-semibold text-navy">{{ pasajeroPrincipal?.documento || '-' }}</span>
                  <span>Email:</span>
                  <span class="font-semibold text-navy">{{ pasajeroPrincipal?.email || '-' }}</span>
                  <span>Teléfono:</span>
                  <span class="font-semibold text-navy">{{ telefonoLegible(pasajeroPrincipal?.telefono) }}</span>
                </div>
              </section>
            </div>

            <section class="rounded-[28px] bg-slate-50 p-6">
              <h2 class="text-xl font-semibold text-navy sm:text-2xl">Equipaje Registrado</h2>
              <div class="mt-6 space-y-4">
                <div class="rounded-[22px] bg-white px-5 py-5">
                  <div class="flex items-center justify-between gap-4">
                    <div>
                      <p class="text-lg font-semibold text-navy sm:text-xl">Equipaje de Mano (10kg)</p>
                      <p class="mt-1 text-sm text-text-muted sm:text-base">Incluido</p>
                    </div>
                    <p class="text-lg font-semibold text-emerald-500 sm:text-xl">Incluido</p>
                  </div>
                </div>

                <div
                  v-for="(pasajero, index) in pasajerosConBodega"
                  :key="`bodega-${index}`"
                  class="rounded-[22px] bg-white px-5 py-5"
                >
                  <div class="flex items-center justify-between gap-4">
                    <div>
                      <p class="text-lg font-semibold text-navy sm:text-xl">Equipaje de Bodega (23kg)</p>
                      <p class="mt-1 text-sm text-text-muted sm:text-base">Etiqueta: {{ etiquetaEquipaje(index) }}</p>
                    </div>
                    <p class="text-lg font-semibold text-navy sm:text-xl">{{ moneda(45) }}</p>
                  </div>
                </div>

                <div
                  v-if="!pasajerosConBodega.length"
                  class="rounded-[22px] bg-white px-5 py-5 text-sm text-text-muted sm:text-base"
                >
                  No se registró equipaje de bodega adicional en esta reserva.
                </div>
              </div>
            </section>

            <section class="rounded-[28px] bg-slate-50 p-6">
              <h2 class="text-xl font-semibold text-navy sm:text-2xl">Boleto Emitido</h2>
              <div class="mt-6 space-y-4">
                <div
                  v-for="(pasajero, index) in pasajeros"
                  :key="`boleto-${index}`"
                  class="rounded-[22px] bg-white px-5 py-5"
                >
                  <div class="flex items-center justify-between gap-4">
                    <div>
                      <p class="text-base text-navy sm:text-lg">Número de Boleto</p>
                      <p class="mt-2 text-2xl font-bold text-navy sm:text-3xl">{{ boletoEmitido(index) }}</p>
                    </div>
                    <div class="text-right">
                      <p class="text-sm text-text-muted">Estado</p>
                      <p class="mt-2 inline-flex rounded-full bg-emerald-100 px-4 py-1 text-base font-semibold text-emerald-600">Emitido</p>
                    </div>
                  </div>
                </div>
              </div>
            </section>

            <section class="rounded-[28px] bg-slate-50 p-6">
              <h2 class="text-xl font-semibold text-navy sm:text-2xl">Factura</h2>
              <div class="mt-6 rounded-[22px] bg-white px-6 py-6">
                <div class="flex flex-col gap-4 border-b border-slate-200 pb-5 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <p class="text-base text-navy sm:text-lg">Número de Factura</p>
                    <p class="mt-2 text-3xl font-bold text-navy sm:text-4xl">INV-{{ confirmacion.numeroVuelo || 'AV1001' }}</p>
                  </div>
                  <div class="text-right">
                    <p class="text-sm text-text-muted">Fecha</p>
                    <p class="mt-2 text-xl font-semibold text-navy sm:text-2xl">{{ fechaLegible(confirmacion.fecha) }}</p>
                  </div>
                </div>

                <div class="mt-6 space-y-3 text-sm text-text-muted sm:text-base">
                  <div class="flex items-center justify-between">
                    <span>Tarifa del vuelo</span>
                    <span>{{ moneda(confirmacion.tarifaVuelo) }}</span>
                  </div>
                  <div class="flex items-center justify-between">
                    <span>Equipaje de bodega</span>
                    <span>{{ moneda(confirmacion.equipajeBodegaTotal) }}</span>
                  </div>
                  <div class="flex items-center justify-between">
                    <span>Subtotal</span>
                    <span>{{ moneda(confirmacion.subtotalGeneral) }}</span>
                  </div>
                  <div class="flex items-center justify-between">
                    <span>IVA (15%)</span>
                    <span>{{ moneda(confirmacion.ivaGeneral) }}</span>
                  </div>
                  <div class="flex items-center justify-between">
                    <span>Cargo por servicio</span>
                    <span>{{ moneda(confirmacion.cargoServicio) }}</span>
                  </div>
                </div>

                <div class="mt-6 flex items-center justify-between border-t border-slate-200 pt-5">
                  <span class="text-xl font-semibold text-navy sm:text-2xl">Total Pagado</span>
                  <span class="text-2xl font-bold text-navy sm:text-3xl">{{ moneda(confirmacion.totalPagado) }}</span>
                </div>
              </div>
            </section>

            <div class="border-t border-slate-200 pt-8 text-center">
              <button
                type="button"
                class="rounded-2xl bg-gold px-8 py-3 text-lg font-semibold text-navy transition-colors hover:bg-gold-light disabled:cursor-not-allowed disabled:bg-gold/50"
                :disabled="enviandoEmail || emailEnviado"
                @click="simularEnvioEmail"
              >
                {{ enviandoEmail ? 'Enviando...' : emailEnviado ? 'Enviado correctamente' : 'Enviar por Email' }}
              </button>

              <p v-if="emailEnviado" class="mt-4 text-base text-emerald-600 sm:text-lg">
                La confirmación fue enviada de forma simulada. Redirigiendo al inicio...
              </p>

              <button
                type="button"
                class="mt-6 block w-full text-center text-lg font-medium text-navy transition-colors hover:text-blue-accent sm:text-xl"
                @click="volverDespuesConfirmacion"
              >
                {{ confirmacion.urlRetorno ? 'Volver a la aerolinea' : 'Volver al Inicio' }}
              </button>
            </div>
          </div>

          <div v-else class="mt-10 rounded-[28px] border border-slate-200 bg-slate-50 p-8 text-center text-base text-text-muted sm:text-lg">
            No encontramos la información de confirmación guardada. Puedes volver al inicio y empezar una nueva reserva.
          </div>
        </div>
      </div>
    </section>
  </div>
</template>
