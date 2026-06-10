import { computed } from 'vue'
import { useAutenticacionStore } from '@/stores/autenticacion.store'

export function usePanelPermisos() {
  const auth = useAutenticacionStore()

  const esAdministrador = computed(() => auth.rol === 'ADMINISTRADOR')
  const esAerolinea = computed(() => auth.rol === 'AEROLINEA')
  const puedeEliminar = computed(() => esAdministrador.value)
  const tituloPanel = computed(() => (esAdministrador.value ? 'Administrador' : 'Aerolinea'))

  return {
    auth,
    esAdministrador,
    esAerolinea,
    puedeEliminar,
    tituloPanel,
  }
}
