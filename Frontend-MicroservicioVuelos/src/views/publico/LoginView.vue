<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAutenticacionStore } from '@/stores/autenticacion.store'
import { useReservaStore } from '@/stores/reserva.store'
import { IMAGENES } from '@/config/imagenes'
import InputApp from '@/components/base/InputApp.vue'
import BotonApp from '@/components/base/BotonApp.vue'

const router = useRouter()
const route = useRoute()
const auth = useAutenticacionStore()
const reserva = useReservaStore()

const form = ref({ username: '', password: '' })
const errores = ref({})
const errorGeneral = ref('')
const cargando = ref(false)

function validar() {
  const e = {}
  if (!form.value.username.trim()) e.username = 'El usuario es requerido.'
  if (!form.value.password) e.password = 'La contraseña es requerida.'
  errores.value = e
  return !Object.keys(e).length
}

async function handleLogin() {
  if (!validar()) return
  cargando.value = true
  errorGeneral.value = ''
  try {
    await auth.login(
      { username: form.value.username.trim(), password: form.value.password },
      true,
    )

    const redirect = route.query.redirect
    if (redirect) {
      router.push(redirect)
    } else if (auth.esCliente && reserva.tienePendiente) {
      try {
        await router.push({ name: 'checkout-pago' })
      } catch {
        router.push('/cliente')
      }
    } else if (auth.esCliente) {
      router.push('/cliente')
    } else {
      router.push('/panel')
    }
  } catch (error) {
    const status = error.response?.status
    const msg = error.response?.data?.message
    if (status === 401 || status === 400) {
      errorGeneral.value = 'Usuario o contraseña incorrectos.'
    } else if (msg) {
      errorGeneral.value = msg
    } else {
      errorGeneral.value = 'No se pudo iniciar sesión. Intenta nuevamente.'
    }
  } finally {
    cargando.value = false
  }
}
</script>

<template>
  <div class="min-h-[calc(100vh-64px)] bg-background px-4 py-10">
    <div class="mx-auto max-w-md">
      <div class="mb-8 text-center">
        <div class="mx-auto inline-flex h-20 w-20 items-center justify-center rounded-full bg-gold shadow-lg">
          <img :src="IMAGENES.logoPrincipal" alt="MPAS Airways" class="h-12 w-12 object-contain" />
        </div>
        <h1 class="mt-6 text-[2.2rem] font-bold leading-tight">
          <span class="text-navy">MPAS</span>
          <span class="ml-2 text-gold-dark">Airways</span>
        </h1>
        <p class="mt-3 text-lg text-text-muted">Inicia sesión en tu cuenta</p>
      </div>

      <div class="rounded-[28px] bg-white p-8 shadow-[0_16px_40px_rgba(15,23,42,0.10)]">
        <Transition name="fade">
          <div
            v-if="errorGeneral"
            class="mb-5 rounded-2xl border border-error/30 bg-red-50 px-4 py-3 text-sm text-error"
          >
            {{ errorGeneral }}
          </div>
        </Transition>

        <form class="space-y-5" @submit.prevent="handleLogin">
          <InputApp
            v-model="form.username"
            label="Usuario"
            placeholder="Ingresa tu usuario"
            autocomplete="username"
            :error="errores.username"
            requerido
          />

          <InputApp
            v-model="form.password"
            label="Contraseña"
            tipo="password"
            placeholder="Ingresa tu contraseña"
            autocomplete="current-password"
            :error="errores.password"
            requerido
          />

          <div class="flex justify-end text-sm">
            <button
              type="button"
              class="font-semibold text-navy transition-colors hover:text-blue-accent"
              @click="alert('Contacta al administrador para recuperar tu contraseña.')"
            >
              ¿Olvidaste tu contraseña?
            </button>
          </div>

          <BotonApp tipo="submit" variante="primario" ancho :cargando="cargando">
            Iniciar Sesión
          </BotonApp>
        </form>

        <div class="mt-8 border-t border-slate-200 pt-8 text-center">
          <p class="text-sm text-text-muted">
            ¿No tienes cuenta?
            <RouterLink
              :to="{ name: 'registro', query: route.query.redirect ? { redirect: route.query.redirect } : {} }"
              class="font-semibold text-navy hover:underline"
            >
              Regístrate aquí
            </RouterLink>
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.25s;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
