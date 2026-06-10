import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { loginApi, logoutApi } from '@/api/autenticacion.api'

function parseJwtPayload(token) {
  try {
    const base64 = token.split('.')[1]
    if (!base64) return null

    const normalized = base64.replace(/-/g, '+').replace(/_/g, '/')
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=')
    const json = atob(padded)

    return JSON.parse(json)
  } catch {
    return null
  }
}

function extraerRolDesdePayload(payload) {
  if (!payload) return null

  return (
    payload.role ??
    payload.rol ??
    payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
    null
  )
}

function normalizarRol(value) {
  const raw = Array.isArray(value) ? value[0] : value
  const rol = String(raw || '').trim().toUpperCase()

  if (['CLIENTE', 'CUSTOMER', 'USUARIO', 'USER'].includes(rol)) return 'CLIENTE'
  if (['ADMINISTRADOR', 'ADMIN'].includes(rol)) return 'ADMINISTRADOR'
  if (['AEROLINEA', 'AEROLÍNEA', 'AIRLINE'].includes(rol)) return 'AEROLINEA'

  return rol || null
}

export const useAutenticacionStore = defineStore('autenticacion', () => {
  const leer = (clave) => localStorage.getItem(clave) ?? sessionStorage.getItem(clave) ?? null

  const token = ref(leer('token'))
  const usuario = ref(JSON.parse(leer('usuario') || 'null'))
  const rol = ref(normalizarRol(leer('rol')))

  const estaAutenticado = computed(() => !!token.value)
  const esAdministrador = computed(() => rol.value === 'ADMINISTRADOR')
  const esAerolinea = computed(() => rol.value === 'AEROLINEA')
  const esCliente = computed(() => rol.value === 'CLIENTE')

  async function login(credenciales, recordar = true) {
    const { data } = await loginApi(credenciales)
    const tokenRecibido = data.data.token
    const payload = parseJwtPayload(tokenRecibido)
    const rolRespuesta = Array.isArray(data.data.rol) ? data.data.rol[0] : data.data.rol

    token.value = tokenRecibido
    usuario.value = data.data.usuario
    rol.value = normalizarRol(rolRespuesta ?? extraerRolDesdePayload(payload))

    const storage = recordar ? localStorage : sessionStorage
    storage.setItem('token', token.value)
    storage.setItem('usuario', JSON.stringify(usuario.value))

    if (rol.value) {
      storage.setItem('rol', rol.value)
    } else {
      localStorage.removeItem('rol')
      sessionStorage.removeItem('rol')
    }

    return data
  }

  async function logout() {
    try {
      await logoutApi()
    } catch {
      // Si el token ya expiró o fue invalidado, igual limpiamos la sesión local.
    } finally {
      token.value = null
      usuario.value = null
      rol.value = null

      ;['token', 'usuario', 'rol'].forEach((clave) => {
        localStorage.removeItem(clave)
        sessionStorage.removeItem(clave)
      })
    }
  }

  return {
    token,
    usuario,
    rol,
    estaAutenticado,
    esAdministrador,
    esAerolinea,
    esCliente,
    login,
    logout,
  }
})
