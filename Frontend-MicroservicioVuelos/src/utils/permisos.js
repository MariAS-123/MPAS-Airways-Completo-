// Roles del sistema — fuente: Contrato API v3.0 §1.1
export const ROLES = Object.freeze({
  ADMINISTRADOR: 'ADMINISTRADOR',
  AEROLINEA:     'AEROLINEA',
  CLIENTE:       'CLIENTE',
})

const A  = ROLES.ADMINISTRADOR
const AE = ROLES.AEROLINEA
const C  = ROLES.CLIENTE

/**
 * Mapa de permisos por funcionalidad.
 * Derivado exclusivamente del contrato API v3.0.
 */
export const PERMISOS = Object.freeze({
  // Auth
  LOGOUT: [A, AE, C],

  // Catálogos — lectura pública (no requiere permiso), escritura restringida
  GESTIONAR_PAISES:      [A],
  GESTIONAR_CIUDADES:    [A],
  GESTIONAR_AEROPUERTOS: [A, AE],

  // Vuelos
  VER_VUELOS:     [A, AE, C],
  CREAR_VUELO:    [A, AE],
  EDITAR_VUELO:   [A, AE],
  CAMBIAR_ESTADO_VUELO: [A, AE],
  ELIMINAR_VUELO: [A],

  // Escalas
  VER_ESCALAS:    [A, AE, C],
  GESTIONAR_ESCALAS: [A, AE],

  // Asientos
  VER_ASIENTOS:   [A, AE, C],
  GESTIONAR_ASIENTOS: [A, AE],

  // CRM — Clientes
  LISTAR_CLIENTES:  [A, AE],
  VER_CLIENTE:      [A, AE, C],    // CLIENTE solo ve el propio
  CREAR_CLIENTE:    [A, AE],
  EDITAR_CLIENTE:   [A, C],        // CLIENTE solo edita el propio
  ELIMINAR_CLIENTE: [A],

  // CRM — Pasajeros
  LISTAR_PASAJEROS: [A, AE],
  VER_PASAJERO:     [A, AE, C],   // CLIENTE solo ve los propios
  CREAR_PASAJERO:   [A, AE, C],
  EDITAR_PASAJERO:  [A, AE],

  // Reservas
  LISTAR_RESERVAS:  [A, AE],
  VER_RESERVA:      [A, AE, C],   // CLIENTE solo ve la propia
  CREAR_RESERVA:    [A, AE, C],
  PAGAR_RESERVA:    [A, AE, C],
  CANCELAR_RESERVA: [A, AE, C],   // CLIENTE solo puede cancelar (CAN)
  CAMBIAR_ESTADO_RESERVA: [A, AE],

  // Facturas
  LISTAR_FACTURAS: [A, AE],
  VER_FACTURA:     [A, AE, C],    // CLIENTE solo ve la propia
  CREAR_FACTURA:   [A, AE],
  ANULAR_FACTURA:  [A],

  // Boletos
  LISTAR_BOLETOS:   [A, AE],
  VER_BOLETO:       [A, AE, C],   // CLIENTE solo ve el propio
  EMITIR_BOLETO:    [A, AE],
  CANCELAR_BOLETO:  [A, AE],

  // Equipaje
  VER_EQUIPAJE:     [A, AE, C],   // CLIENTE solo ve el propio
  GESTIONAR_EQUIPAJE: [A, AE],
  CAMBIAR_ESTADO_EQUIPAJE: [A, AE],

  // Portal Cliente (/api/v1/cliente/*)
  PORTAL_CLIENTE:   [C],

  // Seguridad
  GESTIONAR_USUARIOS: [A],
  GESTIONAR_ROLES:    [A],

  // Auditoría
  VER_AUDITORIA: [A],
})

/**
 * Devuelve true si el rol tiene el permiso indicado.
 */
export function tienePermiso(rol, permiso) {
  if (!rol || !permiso) return false
  return PERMISOS[permiso]?.includes(rol) ?? false
}

/**
 * Devuelve true si el rol tiene al menos uno de los permisos indicados.
 */
export function tieneAlgunPermiso(rol, permisos = []) {
  return permisos.some((p) => tienePermiso(rol, p))
}

/**
 * Devuelve true si el rol está en la lista de roles requeridos para una ruta.
 * Lista vacía significa ruta pública.
 */
export function puedeAccederRuta(rol, rolesRequeridos = []) {
  if (!rolesRequeridos.length) return true
  return rolesRequeridos.includes(rol)
}
