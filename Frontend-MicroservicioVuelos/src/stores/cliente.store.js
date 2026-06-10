import { computed, ref } from 'vue'
import { defineStore } from 'pinia'

const KEY = 'mpas_cliente_perfil'

function leerPerfil() {
  try {
    return JSON.parse(sessionStorage.getItem(KEY) || 'null')
  } catch {
    return null
  }
}

export const useClienteStore = defineStore('cliente', () => {
  const perfil = ref(leerPerfil())

  const nombreMostrado = computed(() => {
    const nombres = String(perfil.value?.nombres ?? perfil.value?.nombre ?? '').trim()
    const apellidos = String(perfil.value?.apellidos ?? perfil.value?.apellido ?? '').trim()
    return [nombres, apellidos].filter(Boolean).join(' ').trim()
  })

  function setPerfil(value) {
    perfil.value = value || null
    if (perfil.value) {
      sessionStorage.setItem(KEY, JSON.stringify(perfil.value))
    } else {
      sessionStorage.removeItem(KEY)
    }
  }

  function limpiar() {
    perfil.value = null
    sessionStorage.removeItem(KEY)
  }

  return {
    perfil,
    nombreMostrado,
    setPerfil,
    limpiar,
  }
})
