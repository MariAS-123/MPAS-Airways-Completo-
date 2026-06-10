<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { registerClienteApi } from '@/api/autenticacion.api'
import { getClienteApi } from '@/api/clientes.api'
import { createPasajeroApi } from '@/api/pasajeros.api'
import { createReservaApi, pagarReservaApi } from '@/api/reservas.api'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import CheckoutStepper from '@/components/CheckoutStepper.vue'
import { useAutenticacionStore } from '@/stores/autenticacion.store'
import { useCatalogosStore } from '@/stores/catalogos.store'
import { useReservaStore } from '@/stores/reserva.store'
import { guardarPortalReserva } from '@/utils/portalCliente'

const router = useRouter()
const auth = useAutenticacionStore()
const catalogos = useCatalogosStore()
const reserva = useReservaStore()

const COSTO_BODEGA = 45
const CARGO_SERVICIO = 0
const IVA = 0.15
const FECHA_HOY = new Date().toISOString().split('T')[0]
const KEY_CONFIRMACION = 'mpas_confirmacion'
const KEY_AEROLINEA_PROGRESO = 'aerolinea_progreso'
const API_BASE_URL = (import.meta.env.VITE_API_URL || 'https://middlewaremicroserviciovuelos-bpf7bsgqh8g2cea5.eastus-01.azurewebsites.net/api/v1').replace(/\/$/, '')

const mostrarModalAuth = ref(false)
const mostrarModalPagoSimulado = ref(false)
const tabAuth = ref('login')
const errorPago = ref('')
const errorAuth = ref('')
const errorPagoSimulado = ref('')
const procesandoPago = ref(false)
const procesandoPagoSimulado = ref(false)
const estadoPagoSimulado = ref('idle')
const cargandoCiudades = ref(false)
const estadoProceso = ref('')
const ciudades = ref([])
const idClienteRecienRegistrado = ref(null)

const loginForm = ref({
  username: '',
  password: '',
})

const registerForm = ref({
  tipo_identificacion: 'CEDULA',
  numero_identificacion: '',
  nombres: '',
  apellidos: '',
  correo: '',
  telefono: '',
  direccion: '',
  id_pais_nacionalidad: '',
  id_pais_residencia: '',
  id_ciudad_residencia: '',
  fecha_nacimiento: '',
  genero: 'MASCULINO',
  username: '',
  password: '',
  confirmar_password: '',
})

const erroresRegistro = ref({})
const erroresPagoSimulado = ref({})

const pagoSimuladoForm = ref({
  titular: '',
  numeroTarjeta: '',
  expiracion: '',
  cvv: '',
  tipo: 'CREDITO',
})

const vuelo = computed(() => reserva.vuelo)
const pasajeros = computed(() => reserva.pasajeros || [])
const asientos = computed(() => reserva.asientos || [])
const equipaje = computed(() => reserva.equipaje || [])

const opcionesPaises = computed(() =>
  catalogos.paises.map((pais) => ({
    valor: String(pais.idPais ?? pais.id_pais ?? pais.id),
    etiqueta: String(pais.nombre ?? pais.nombre_pais ?? '').replace(/\d{6,}$/u, '').trim(),
  })),
)

const opcionesCiudades = computed(() =>
  ciudades.value.map((ciudad) => ({
    valor: String(ciudad.idCiudad ?? ciudad.id_ciudad ?? ciudad.id),
    etiqueta: String(ciudad.nombre ?? '').replace(/\d{6,}$/u, '').trim(),
  })),
)

const opcionesTipoIdentificacion = [
  { valor: 'CEDULA', etiqueta: 'Cedula' },
  { valor: 'PASAPORTE', etiqueta: 'Pasaporte' },
  { valor: 'RUC', etiqueta: 'RUC' },
  { valor: 'OTRO', etiqueta: 'Otro' },
]

const opcionesGenero = [
  { valor: 'MASCULINO', etiqueta: 'Masculino' },
  { valor: 'FEMENINO', etiqueta: 'Femenino' },
  { valor: 'OTRO', etiqueta: 'Otro' },
]

const totalBodega = computed(() =>
  equipaje.value.reduce((acc, item) => acc + (item.equipajeBodega ? COSTO_BODEGA : 0), 0),
)

const subtotalVuelo = computed(() =>
  asientos.value.reduce((acc, asiento) => {
    const base = Number(vuelo.value?.precioBase || 0)
    const extra = Number(asiento?.precioExtra || 0)
    return acc + base + extra
  }, 0),
)

const subtotalGeneral = computed(() => subtotalVuelo.value + totalBodega.value)
const ivaGeneral = computed(() => Number((subtotalGeneral.value * IVA).toFixed(2)))
const totalPagar = computed(() => Number((subtotalGeneral.value + ivaGeneral.value + CARGO_SERVICIO).toFixed(2)))

const numeroTarjetaDigitos = computed(() =>
  String(pagoSimuladoForm.value.numeroTarjeta || '').replace(/\D/g, ''),
)

const numeroTarjetaValido = computed(() => numeroTarjetaDigitos.value.length === 16)
const expiracionTarjetaValida = computed(() =>
  validarExpiracionTarjeta(pagoSimuladoForm.value.expiracion),
)
const cvvValido = computed(() => /^\d{3,4}$/.test(String(pagoSimuladoForm.value.cvv || '').trim()))

const itemsPago = computed(() =>
  pasajeros.value.map((pasajero, indice) => {
    const asiento = asientos.value[indice] || null
    const bag = equipaje.value[indice] || null
    const costoEquipaje = bag?.equipajeBodega ? COSTO_BODEGA : 0
    const subtotalLinea = Number(vuelo.value?.precioBase || 0) + Number(asiento?.precioExtra || 0) + costoEquipaje

    return {
      indice,
      nombre: [pasajero.nombre_pasajero, pasajero.apellido_pasajero].filter(Boolean).join(' ') || `Pasajero ${indice + 1}`,
      pasajero,
      asiento,
      equipaje: bag,
      costoEquipaje,
      subtotalLinea,
      ivaLinea: Number((subtotalLinea * IVA).toFixed(2)),
      totalLinea: Number((subtotalLinea * (1 + IVA)).toFixed(2)),
    }
  }),
)

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

function horaLegible(valor) {
  if (!valor) return '--:--'
  return new Intl.DateTimeFormat('es-EC', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(new Date(valor))
}

function duracionLegible(minutos) {
  const total = Number(minutos || 0)
  const horas = Math.floor(total / 60)
  const mins = total % 60
  if (!horas) return `${mins}m`
  if (!mins) return `${horas}h`
  return `${horas}h ${mins}m`
}

function limpiarModal() {
  errorAuth.value = ''
  erroresRegistro.value = {}
}

function limpiarPagoSimulado() {
  errorPagoSimulado.value = ''
  erroresPagoSimulado.value = {}
  estadoPagoSimulado.value = 'idle'
}

function persistirPasajeroBackend(indice, idPasajero) {
  const actualizados = pasajeros.value.map((pasajero, idx) =>
    idx === indice
      ? {
          ...pasajero,
          id_pasajero_backend: idPasajero,
        }
      : pasajero,
  )

  reserva.setPasajeros(actualizados)
}

function logReservaBackend(etiqueta, detalle) {
  console.info(`[confirmar-reserva:${etiqueta}]`, JSON.stringify(detalle, null, 2))
}

function logRequestReservaBackend({ endpoint, method = 'POST', payload }) {
  logReservaBackend('request', {
    method,
    endpoint,
    url: `${API_BASE_URL}${endpoint}`,
    payload,
  })
}

function logResponseReservaBackend({ endpoint, method = 'POST', response }) {
  logReservaBackend('response', {
    method,
    endpoint,
    status: response?.status ?? null,
    data: response?.data ?? null,
  })
}

function logResumenPagoUi() {
  logReservaBackend('resumen-ui', {
    tarifaVuelo: subtotalVuelo.value,
    equipajeBodega: totalBodega.value,
    subtotal: subtotalGeneral.value,
    iva: ivaGeneral.value,
    cargoServicio: CARGO_SERVICIO,
    totalPagado: totalPagar.value,
  })
}

function validarCedulaEcuador(valor) {
  const cedula = String(valor || '').replace(/\D/g, '')
  return /^\d{10}$/.test(cedula)
}

function validarDocumentoIdentidad(tipo, valor) {
  const documento = String(valor || '').trim().toUpperCase()
  if (!documento) return 'Requerido.'

  if (tipo === 'CEDULA' && !validarCedulaEcuador(documento)) {
    return 'Ingrese una cedula valida.'
  }

  if (tipo === 'PASAPORTE' && !/^[A-Z0-9]{6,12}$/.test(documento)) {
    return 'El pasaporte debe tener entre 6 y 12 caracteres alfanumericos.'
  }

  if (tipo === 'RUC' && !/^\d{13}$/.test(documento)) {
    return 'El RUC debe tener 13 digitos.'
  }

  if (tipo === 'OTRO' && !/^[A-Z0-9-]{4,20}$/.test(documento)) {
    return 'Usa entre 4 y 20 letras, numeros o guiones.'
  }

  return ''
}

function validarExpiracionTarjeta(valor) {
  const match = String(valor || '').trim().match(/^(\d{2})\/(\d{2})$/)
  if (!match) return false

  const mes = Number(match[1])
  const anio = 2000 + Number(match[2])
  if (mes < 1 || mes > 12) return false

  const finMes = new Date(anio, mes, 0, 23, 59, 59)
  return finMes >= new Date()
}

function formatearNumeroTarjeta(valor) {
  return String(valor || '')
    .replace(/\D/g, '')
    .slice(0, 16)
    .replace(/(.{4})/g, '$1 ')
    .trim()
}

function handleNumeroTarjetaInput(event) {
  pagoSimuladoForm.value.numeroTarjeta = formatearNumeroTarjeta(event.target.value)

  if (numeroTarjetaValido.value && erroresPagoSimulado.value.numeroTarjeta) {
    const { numeroTarjeta, ...restoErrores } = erroresPagoSimulado.value
    erroresPagoSimulado.value = restoErrores
  }
}

function formatearExpiracionTarjeta(valor) {
  const digitos = String(valor || '').replace(/\D/g, '').slice(0, 4)
  if (digitos.length <= 2) return digitos
  return `${digitos.slice(0, 2)}/${digitos.slice(2)}`
}

function handleExpiracionTarjetaInput(event) {
  pagoSimuladoForm.value.expiracion = formatearExpiracionTarjeta(event.target.value)

  if (validarExpiracionTarjeta(pagoSimuladoForm.value.expiracion) && erroresPagoSimulado.value.expiracion) {
    const { expiracion, ...restoErrores } = erroresPagoSimulado.value
    erroresPagoSimulado.value = restoErrores
  }
}

function handleCvvInput(event) {
  pagoSimuladoForm.value.cvv = String(event.target.value || '').replace(/\D/g, '').slice(0, 4)

  if (cvvValido.value && erroresPagoSimulado.value.cvv) {
    const { cvv, ...restoErrores } = erroresPagoSimulado.value
    erroresPagoSimulado.value = restoErrores
  }
}

function validarPagoSimulado() {
  const errores = {}
  const numero = numeroTarjetaDigitos.value

  if (!pagoSimuladoForm.value.titular.trim()) errores.titular = 'Ingresa el nombre del titular.'
  if (!numero) errores.numeroTarjeta = 'Ingresa el numero de tarjeta.'
  else if (numero.length !== 16) errores.numeroTarjeta = 'Ingresa un numero de tarjeta de 16 digitos.'
  if (!pagoSimuladoForm.value.expiracion.trim()) errores.expiracion = 'Ingresa la fecha de expiracion.'
  else if (!validarExpiracionTarjeta(pagoSimuladoForm.value.expiracion)) errores.expiracion = 'Ingresa una fecha vigente en formato MM/AA.'
  if (!/^\d{3,4}$/.test(pagoSimuladoForm.value.cvv.trim())) errores.cvv = 'El CVV debe tener 3 o 4 digitos.'

  erroresPagoSimulado.value = errores
  return !Object.keys(errores).length
}

function abrirPagoSimulado() {
  limpiarPagoSimulado()
  mostrarModalPagoSimulado.value = true
}

function cerrarPagoSimulado() {
  if (procesandoPago.value || procesandoPagoSimulado.value) return
  mostrarModalPagoSimulado.value = false
  limpiarPagoSimulado()
}

function parseJwtPayload(token) {
  try {
    const base64 = token.split('.')[1]
    if (!base64) return null
    const normalized = base64.replace(/-/g, '+').replace(/_/g, '/')
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=')
    return JSON.parse(atob(padded))
  } catch {
    return null
  }
}

watch(
  () => registerForm.value.id_pais_residencia,
  async (idPais) => {
    registerForm.value.id_ciudad_residencia = ''
    ciudades.value = []
    if (!idPais) return
    cargandoCiudades.value = true
    try {
      ciudades.value = await catalogos.cargarCiudadesPorPais(idPais, true)
    } catch {
      ciudades.value = []
    } finally {
      cargandoCiudades.value = false
    }
  },
)

function abrirAutenticacion(tab = 'login') {
  tabAuth.value = tab
  mostrarModalAuth.value = true
  limpiarModal()
}

function cerrarAutenticacion() {
  if (procesandoPago.value) return
  mostrarModalAuth.value = false
  limpiarModal()
}

function obtenerValorProfundo(obj, claves) {
  for (const clave of claves) {
    if (obj?.[clave] !== undefined && obj?.[clave] !== null) return obj[clave]
  }

  for (const valor of Object.values(obj || {})) {
    if (valor && typeof valor === 'object') {
      const encontrado = obtenerValorProfundo(valor, claves)
      if (encontrado !== undefined && encontrado !== null) return encontrado
    }
  }

  return null
}

function obtenerIdClienteAutenticado() {
  return Number(
    obtenerValorProfundo(auth.usuario, [
      'idCliente',
      'id_cliente',
      'clienteId',
      'cliente_id',
      'id',
    ]) || 0,
  )
}

async function resolverIdClienteAutenticado() {
  const directos = [
    idClienteRecienRegistrado.value,
    obtenerIdClienteAutenticado(),
  ]

  const payload = parseJwtPayload(auth.token || '')
  const candidatosPayload = [
    payload?.idCliente,
    payload?.id_cliente,
    payload?.clienteId,
    payload?.cliente_id,
    payload?.sub,
    payload?.nameid,
    payload?.sid,
    payload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
  ]

  const candidatos = [...directos, ...candidatosPayload]
    .map((valor) => Number(valor || 0))
    .filter((valor, index, arr) => Number.isFinite(valor) && valor > 0 && arr.indexOf(valor) === index)

  for (const candidato of candidatos) {
    try {
      const { data } = await getClienteApi(candidato)
      const cliente = data?.data || {}
      const idCliente = Number(cliente.idCliente ?? cliente.id_cliente ?? cliente.id ?? candidato)
      if (idCliente > 0) return idCliente
    } catch {
      // Seguimos probando candidatos posibles del token o del usuario autenticado.
    }
  }

  return 0
}

function recolectarArreglos(valor, listas = []) {
  if (Array.isArray(valor)) {
    listas.push(valor)
    valor.forEach((item) => recolectarArreglos(item, listas))
    return listas
  }

  if (valor && typeof valor === 'object') {
    Object.values(valor).forEach((item) => recolectarArreglos(item, listas))
  }

  return listas
}

function normalizarDetallesReserva(data) {
  const listas = recolectarArreglos(data)
  const detalles = listas.flat().filter((item) => {
    if (!item || typeof item !== 'object') return false
    const tienePasajero = item.idPasajero ?? item.id_pasajero
    const tieneAsiento = item.idAsiento ?? item.id_asiento
    const tieneDetalle = item.idDetalle ?? item.id_detalle ?? item.id
    return tienePasajero && tieneAsiento && tieneDetalle
  })

  return detalles.map((detalle) => ({
    idDetalle: detalle.idDetalle ?? detalle.id_detalle ?? detalle.id ?? null,
    idPasajero: detalle.idPasajero ?? detalle.id_pasajero ?? null,
    idAsiento: detalle.idAsiento ?? detalle.id_asiento ?? null,
  }))
}

function validarRegistro() {
  const e = {}
  const correo = registerForm.value.correo.trim()
  const errorDocumento = validarDocumentoIdentidad(
    registerForm.value.tipo_identificacion,
    registerForm.value.numero_identificacion,
  )
  const hoy = new Date()
  hoy.setHours(0, 0, 0, 0)

  if (!registerForm.value.tipo_identificacion) e.tipo_identificacion = 'Selecciona el tipo.'
  if (errorDocumento) e.numero_identificacion = errorDocumento
  if (!registerForm.value.nombres.trim()) e.nombres = 'Requerido.'
  if (!registerForm.value.apellidos.trim()) e.apellidos = 'Requerido.'
  if (!correo) e.correo = 'Requerido.'
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(correo)) e.correo = 'Correo invalido.'
  if (!registerForm.value.telefono.trim()) e.telefono = 'Requerido.'
  if (!registerForm.value.direccion.trim()) e.direccion = 'Requerido.'
  if (!registerForm.value.id_pais_nacionalidad) e.id_pais_nacionalidad = 'Selecciona la nacionalidad.'
  if (!registerForm.value.id_pais_residencia) e.id_pais_residencia = 'Selecciona el pais.'
  if (!registerForm.value.id_ciudad_residencia) e.id_ciudad_residencia = 'Selecciona la ciudad.'
  if (!registerForm.value.fecha_nacimiento) {
    e.fecha_nacimiento = 'Selecciona la fecha.'
  } else {
    const fechaNacimiento = new Date(`${registerForm.value.fecha_nacimiento}T00:00:00`)
    if (fechaNacimiento > hoy) e.fecha_nacimiento = 'La fecha no puede ser futura.'
  }
  if (!registerForm.value.genero) e.genero = 'Selecciona el genero.'
  if (!registerForm.value.username.trim()) e.username = 'Requerido.'
  if (!registerForm.value.password) e.password = 'Requerido.'
  else if (registerForm.value.password.length < 8) e.password = 'Minimo 8 caracteres.'
  if (!registerForm.value.confirmar_password) e.confirmar_password = 'Requerido.'
  else if (registerForm.value.password !== registerForm.value.confirmar_password) e.confirmar_password = 'Las contrasenas no coinciden.'

  erroresRegistro.value = e
  return !Object.keys(e).length
}

async function autenticarYProcesar() {
  if (procesandoPago.value) return
  procesandoPago.value = true
  errorAuth.value = ''
  errorPago.value = ''

  try {
    estadoProceso.value = 'Autenticando...'
    await auth.login(
      {
        username: loginForm.value.username.trim(),
        password: loginForm.value.password,
      },
      true,
    )
  } catch (error) {
    errorAuth.value = error.response?.data?.message || 'No se pudo iniciar sesion.'
    estadoProceso.value = ''
    procesandoPago.value = false
    return
  }

  mostrarModalAuth.value = false

  procesandoPago.value = false
  abrirPagoSimulado()
}

async function registrarYProcesar() {
  if (procesandoPago.value || !validarRegistro()) return
  procesandoPago.value = true
  errorAuth.value = ''
  errorPago.value = ''

  try {
    estadoProceso.value = 'Creando cuenta...'
    const registroResp = await registerClienteApi({
      tipoIdentificacion: registerForm.value.tipo_identificacion,
      numeroIdentificacion: registerForm.value.numero_identificacion.trim(),
      nombres: registerForm.value.nombres.trim(),
      apellidos: registerForm.value.apellidos.trim(),
      correo: registerForm.value.correo.trim(),
      telefono: registerForm.value.telefono.trim(),
      direccion: registerForm.value.direccion.trim(),
      idCiudadResidencia: Number(registerForm.value.id_ciudad_residencia),
      idPaisNacionalidad: Number(registerForm.value.id_pais_nacionalidad),
      fechaNacimiento: registerForm.value.fecha_nacimiento,
      genero: registerForm.value.genero,
      username: registerForm.value.username.trim(),
      password: registerForm.value.password,
    })
    const registroData = registroResp.data?.data || {}
    idClienteRecienRegistrado.value = Number(
      registroData.idCliente ?? registroData.id_cliente ?? registroData.id ?? 0,
    ) || null
  } catch (error) {
    errorAuth.value = error.response?.data?.message || 'No se pudo crear la cuenta.'
    estadoProceso.value = ''
    procesandoPago.value = false
    return
  }

  try {
    estadoProceso.value = 'Autenticando...'
    await auth.login(
      {
        username: registerForm.value.username.trim(),
        password: registerForm.value.password,
      },
      true,
    )
  } catch (error) {
    errorAuth.value = error.response?.data?.message || 'La cuenta fue creada, pero no se pudo iniciar sesion.'
    estadoProceso.value = ''
    procesandoPago.value = false
    return
  }

  mostrarModalAuth.value = false

  procesandoPago.value = false
  abrirPagoSimulado()
}

async function ejecutarCompraReal() {
  const idCliente = await resolverIdClienteAutenticado()
  if (!idCliente) {
    estadoProceso.value = ''
    errorPago.value = 'No se pudo identificar el cliente autenticado para crear la reserva.'
    return
  }

  try {
    logResumenPagoUi()

    estadoProceso.value = 'Creando pasajeros...'
    const pasajerosCreados = []

    for (const item of itemsPago.value) {
      let idPasajero = Number(item.pasajero.id_pasajero_backend || 0)

      if (!idPasajero) {
        const payloadPasajero = {
          idCliente,
          nombrePasajero: item.pasajero.nombre_pasajero.trim(),
          apellidoPasajero: item.pasajero.apellido_pasajero.trim(),
          tipoDocumentoPasajero: item.pasajero.tipo_documento_pasajero,
          numeroDocumentoPasajero: item.pasajero.numero_documento_pasajero.trim(),
          fechaNacimientoPasajero: item.pasajero.fecha_nacimiento_pasajero,
          idPaisNacionalidad: Number(item.pasajero.id_pais_nacionalidad || 0),
          emailContactoPasajero: item.pasajero.email_contacto_pasajero.trim(),
          telefonoContactoPasajero: item.pasajero.telefono_contacto_pasajero.trim(),
          generoPasajero: item.pasajero.genero_pasajero,
          requiereAsistencia: Boolean(item.pasajero.requiere_asistencia),
          observacionesPasajero: item.pasajero.observaciones_pasajero?.trim() || null,
        }

        logRequestReservaBackend({
          endpoint: '/pasajeros',
          payload: payloadPasajero,
        })

        const pasajeroResp = await createPasajeroApi(payloadPasajero)
        logResponseReservaBackend({
          endpoint: '/pasajeros',
          response: pasajeroResp,
        })

        const { data } = pasajeroResp
        const pasajeroReal = data?.data || {}
        idPasajero = pasajeroReal.idPasajero ?? pasajeroReal.id_pasajero ?? pasajeroReal.id

        if (idPasajero) {
          persistirPasajeroBackend(item.indice, idPasajero)
        }
      }

      if (!idPasajero) throw new Error('No se pudo obtener el id del pasajero creado.')

      pasajerosCreados.push({
        tempIndex: item.indice,
        idPasajero,
        idAsiento: item.asiento?.idAsiento,
        subtotalLinea: item.subtotalLinea,
        ivaLinea: item.ivaLinea,
        totalLinea: item.totalLinea,
      })
    }

    estadoProceso.value = 'Generando reserva...'
    const idVueloReserva = Number(vuelo.value?.idVuelo ?? vuelo.value?.id_vuelo ?? vuelo.value?.id ?? 0)
    const payload = {
      idCliente,
      idVuelo: idVueloReserva,
      subtotalReserva: subtotalGeneral.value,
      valorIva: ivaGeneral.value,
      totalReserva: Number((subtotalGeneral.value + ivaGeneral.value).toFixed(2)),
      origenCanalReserva: 'BOOKING',
      contactoEmail: pasajeros.value[0]?.email_contacto_pasajero || registerForm.value.correo || auth.usuario?.correo,
      contactoTelefono: pasajeros.value[0]?.telefono_contacto_pasajero || registerForm.value.telefono,
      observaciones: 'Reserva web',
      detalles: pasajerosCreados.map((item) => ({
        idPasajero: item.idPasajero,
        idAsiento: item.idAsiento,
        subtotalLinea: item.subtotalLinea,
        valorIvaLinea: item.ivaLinea,
        totalLinea: item.totalLinea,
      })),
    }

    logRequestReservaBackend({
      endpoint: '/reservas',
      payload,
    })

    const reservaResp = await createReservaApi(payload)
    logResponseReservaBackend({
      endpoint: '/reservas',
      response: reservaResp,
    })

    const reservaReal = reservaResp.data?.data || {}
    const idReserva = reservaReal.idReserva ?? reservaReal.id_reserva ?? reservaReal.id

    if (!idReserva) throw new Error('No se pudo obtener el id_reserva de la reserva creada.')

    const detalles = normalizarDetallesReserva(reservaResp.data?.data || reservaResp.data)

    estadoProceso.value = 'Procesando pago...'
    const equipajePayload = []

    for (const item of itemsPago.value) {
      const pasajeroCreado = pasajerosCreados[item.indice]
      const detalle = detalles.find(
        (d) =>
          String(d.idPasajero) === String(pasajeroCreado?.idPasajero) &&
          String(d.idAsiento) === String(item.asiento?.idAsiento),
      ) || detalles[item.indice]

      if (!detalle?.idDetalle) throw new Error('No se pudo asociar el id_detalle para el equipaje.')

      if (item.equipaje?.equipajeBodega) {
        equipajePayload.push({
          idDetalle: detalle.idDetalle,
          tipo: 'BODEGA',
          pesoKg: 23,
          descripcionEquipaje: 'Maleta de bodega',
        })
      }
    }

    const payloadPago = {
      cargoServicio: CARGO_SERVICIO,
      equipaje: equipajePayload,
    }

    logRequestReservaBackend({
      endpoint: `/reservas/${idReserva}/pagar`,
      method: 'PATCH',
      payload: payloadPago,
    })

    const pagoResp = await pagarReservaApi(idReserva, payloadPago)
    logResponseReservaBackend({
      endpoint: `/reservas/${idReserva}/pagar`,
      method: 'PATCH',
      response: pagoResp,
    })

    let progresoAerolinea = null
    try {
      progresoAerolinea = JSON.parse(localStorage.getItem(KEY_AEROLINEA_PROGRESO) || 'null')
    } catch {
      progresoAerolinea = null
    }

    const payloadConfirmacion = {
      idReserva,
      numeroVuelo: vuelo.value.numeroVuelo,
      codigoReserva: vuelo.value.numeroVuelo,
      fecha: vuelo.value.fechaHoraSalida,
      horario: `${horaLegible(vuelo.value.fechaHoraSalida)} - ${horaLegible(vuelo.value.fechaHoraLlegada)}`,
      ruta: `${vuelo.value.codigoOrigen || ''} - ${vuelo.value.codigoDestino || ''}`.trim(),
      tarifaVuelo: subtotalVuelo.value,
      equipajeBodegaTotal: totalBodega.value,
      subtotalGeneral: subtotalGeneral.value,
      ivaGeneral: ivaGeneral.value,
      cargoServicio: CARGO_SERVICIO,
      totalPagado: totalPagar.value,
      urlRetorno: progresoAerolinea?.urlRetorno || '',
      pasajeros: itemsPago.value.map((item) => ({
        nombre: item.nombre,
        documento: item.pasajero.numero_documento_pasajero,
        asiento: item.asiento?.numeroAsiento || '',
        email: item.pasajero.email_contacto_pasajero || '',
        telefono: item.pasajero.telefono_contacto_pasajero || '',
        equipajeBodega: Boolean(item.equipaje?.equipajeBodega),
      })),
    }

    sessionStorage.setItem(KEY_CONFIRMACION, JSON.stringify(payloadConfirmacion))
    guardarPortalReserva(payloadConfirmacion)
    localStorage.removeItem(KEY_AEROLINEA_PROGRESO)

    idClienteRecienRegistrado.value = null
    estadoProceso.value = ''
    reserva.limpiar()
    router.push({ name: 'confirmacion-reserva' })
  } catch (error) {
    estadoProceso.value = ''
    errorPago.value = error.response?.data?.message || error.message || 'No se pudo completar el pago.'
  }
}

async function handlePagar() {
  errorPago.value = ''

  if (!auth.estaAutenticado) {
    abrirAutenticacion('login')
    return
  }

  if (procesandoPago.value) return
  abrirPagoSimulado()
}

async function confirmarPagoSimulado() {
  errorPago.value = ''
  errorPagoSimulado.value = ''

  if (procesandoPago.value || procesandoPagoSimulado.value || !validarPagoSimulado()) return

  procesandoPagoSimulado.value = true
  estadoPagoSimulado.value = 'processing'

  try {
    estadoProceso.value = ''
    await new Promise((resolve) => setTimeout(resolve, 1200))
    estadoPagoSimulado.value = 'approved'
    await new Promise((resolve) => setTimeout(resolve, 850))

    estadoPagoSimulado.value = 'confirming'
    procesandoPago.value = true
    await ejecutarCompraReal()

    if (errorPago.value) {
      mostrarModalPagoSimulado.value = false
      estadoPagoSimulado.value = 'idle'
    }
  } finally {
    procesandoPagoSimulado.value = false
    procesandoPago.value = false
  }
}

onMounted(async () => {
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

  await catalogos.cargarPaises(true).catch(() => {})
})
</script>

<template>
  <div>
    <CheckoutStepper :paso-actual="5" />

    <section class="min-h-[calc(100vh-64px)] bg-background py-10">
      <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h1 class="text-3xl font-bold text-navy">Resumen y Pago</h1>

        <div v-if="errorPago" class="mt-6 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorPago }}
        </div>

        <div v-if="estadoProceso" class="mt-6 rounded-2xl border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-accent">
          {{ estadoProceso }}
        </div>

        <div class="mt-8 grid gap-8 lg:grid-cols-[1.35fr_0.7fr]">
          <div class="space-y-6">
            <section class="rounded-[28px] bg-white p-8 shadow-sm">
              <h2 class="text-2xl font-semibold text-navy">Detalles del Vuelo</h2>
              <div class="mt-6 grid gap-4 text-text-muted sm:grid-cols-[160px_1fr]">
                <span>Vuelo:</span>
                <span class="font-semibold text-navy">{{ vuelo.numeroVuelo }}</span>
                <span>Ruta:</span>
                <span class="font-semibold text-navy">{{ vuelo.codigoOrigen || '' }} - {{ vuelo.codigoDestino || '' }}</span>
                <span>Fecha:</span>
                <span class="font-semibold text-navy">{{ fechaLegible(vuelo.fechaHoraSalida) }}</span>
                <span>Horario:</span>
                <span class="font-semibold text-navy">
                  {{ horaLegible(vuelo.fechaHoraSalida) }} - {{ horaLegible(vuelo.fechaHoraLlegada) }}
                  ({{ duracionLegible(vuelo.duracionMin) }})
                </span>
              </div>
            </section>

            <section class="rounded-[28px] bg-white p-8 shadow-sm">
              <h2 class="text-2xl font-semibold text-navy">Pasajeros</h2>
              <div class="mt-6 space-y-4">
                <div
                  v-for="item in itemsPago"
                  :key="`pasajero-pago-${item.indice}`"
                  class="rounded-2xl bg-slate-50 px-5 py-4"
                >
                  <p class="font-semibold text-navy">{{ item.nombre }}</p>
                  <p class="mt-1 text-sm text-text-muted">
                    {{ item.pasajero.tipo_documento_pasajero }}: {{ item.pasajero.numero_documento_pasajero }}
                  </p>
                  <p class="text-sm text-text-muted">{{ item.pasajero.email_contacto_pasajero }}</p>
                  <p class="mt-2 text-sm font-medium text-navy">Asiento: {{ item.asiento?.numeroAsiento || 'Pendiente' }}</p>
                </div>
              </div>
            </section>

            <section class="rounded-[28px] bg-white p-8 shadow-sm">
              <h2 class="text-2xl font-semibold text-navy">Equipaje</h2>
              <div class="mt-6 space-y-4">
                <div
                  v-for="item in itemsPago"
                  :key="`equipaje-pago-${item.indice}`"
                  class="grid gap-2 rounded-2xl bg-slate-50 px-5 py-4 sm:grid-cols-[1fr_auto]"
                >
                  <div>
                    <p class="font-semibold text-navy">{{ item.nombre }}</p>
                    <p class="mt-1 text-sm text-text-muted">Equipaje de mano (10kg)</p>
                    <p v-if="item.equipaje?.equipajeBodega" class="text-sm text-text-muted">Equipaje de bodega (23kg)</p>
                  </div>
                  <div class="text-right">
                    <p class="text-sm font-medium text-emerald-600">Incluido</p>
                    <p v-if="item.equipaje?.equipajeBodega" class="mt-1 font-semibold text-navy">{{ moneda(COSTO_BODEGA) }}</p>
                  </div>
                </div>
              </div>
            </section>
          </div>

          <aside class="lg:sticky lg:top-24 lg:self-start">
            <div class="rounded-[28px] bg-white p-8 shadow-sm">
              <h2 class="text-2xl font-semibold text-navy">Resumen de Precio</h2>
              <div class="mt-6 space-y-4 text-text-muted">
                <div class="flex items-center justify-between">
                  <span>Tarifa del vuelo</span>
                  <span>{{ moneda(subtotalVuelo) }}</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>Equipaje de bodega</span>
                  <span>{{ moneda(totalBodega) }}</span>
                </div>
                <div class="flex items-center justify-between border-t border-slate-200 pt-4">
                  <span>Subtotal</span>
                  <span>{{ moneda(subtotalGeneral) }}</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>IVA (15%)</span>
                  <span>{{ moneda(ivaGeneral) }}</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>Cargo por servicio</span>
                  <span>{{ moneda(CARGO_SERVICIO) }}</span>
                </div>
              </div>

              <div class="mt-6 border-t border-slate-200 pt-6">
                <div class="flex items-center justify-between">
                  <span class="text-2xl font-semibold text-navy">Total a pagar</span>
                  <span class="text-4xl font-light text-navy">{{ moneda(totalPagar) }}</span>
                </div>
              </div>

              <button
                type="button"
                class="mt-8 w-full rounded-2xl bg-gold px-6 py-4 font-semibold text-navy transition-colors hover:bg-gold-light disabled:cursor-not-allowed disabled:bg-gold/50"
                :disabled="procesandoPago"
                @click="handlePagar"
              >
                Confirmar reserva
              </button>
            </div>
          </aside>
        </div>
      </div>
    </section>

    <div
      v-if="mostrarModalAuth"
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4 py-8"
      @click.self="cerrarAutenticacion"
    >
      <div class="max-h-[90vh] w-full max-w-4xl overflow-y-auto rounded-[28px] bg-white p-6 shadow-2xl sm:p-8">
        <div class="flex items-center justify-between gap-4">
          <div>
            <h2 class="text-2xl font-bold text-navy">Autenticacion para completar la compra</h2>
            <p class="mt-2 text-text-muted">
              Inicia sesion o crea tu cuenta para continuar con el pago y confirmar la reserva.
            </p>
          </div>
          <button type="button" class="rounded-xl p-2 text-slate-500 hover:bg-slate-100" @click="cerrarAutenticacion">
            ×
          </button>
        </div>

        <div class="mt-6 flex gap-3">
          <button
            type="button"
            class="rounded-2xl px-4 py-2 font-semibold transition-colors"
            :class="tabAuth === 'login' ? 'bg-navy text-white' : 'bg-slate-100 text-navy'"
            @click="tabAuth = 'login'; limpiarModal()"
          >
            Iniciar sesion
          </button>
          <button
            type="button"
            class="rounded-2xl px-4 py-2 font-semibold transition-colors"
            :class="tabAuth === 'registro' ? 'bg-navy text-white' : 'bg-slate-100 text-navy'"
            @click="tabAuth = 'registro'; limpiarModal()"
          >
            Crear cuenta
          </button>
        </div>

        <div v-if="errorAuth" class="mt-6 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorAuth }}
        </div>

        <form v-if="tabAuth === 'login'" class="mt-6 space-y-4" @submit.prevent="autenticarYProcesar">
          <InputApp v-model="loginForm.username" label="Usuario" requerido />
          <InputApp v-model="loginForm.password" label="Contrasena" tipo="password" requerido />
          <button
            type="submit"
            class="w-full rounded-2xl bg-gold px-6 py-4 font-semibold text-navy transition-colors hover:bg-gold-light disabled:cursor-not-allowed disabled:bg-gold/50"
            :disabled="procesandoPago"
          >
            Ingresar y pagar
          </button>
        </form>

        <form v-else class="mt-6 grid gap-4 sm:grid-cols-2" @submit.prevent="registrarYProcesar">
          <SelectApp
            v-model="registerForm.tipo_identificacion"
            label="Tipo identificacion"
            :opciones="opcionesTipoIdentificacion"
            :error="erroresRegistro.tipo_identificacion"
            requerido
          />
          <InputApp v-model="registerForm.numero_identificacion" label="Numero identificacion" :error="erroresRegistro.numero_identificacion" requerido />
          <InputApp v-model="registerForm.nombres" label="Nombres" :error="erroresRegistro.nombres" requerido />
          <InputApp v-model="registerForm.apellidos" label="Apellidos" :error="erroresRegistro.apellidos" requerido />
          <InputApp v-model="registerForm.correo" label="Correo" tipo="email" :error="erroresRegistro.correo" requerido />
          <InputApp v-model="registerForm.telefono" label="Telefono" :error="erroresRegistro.telefono" requerido />
          <div class="sm:col-span-2">
            <InputApp v-model="registerForm.direccion" label="Direccion" :error="erroresRegistro.direccion" requerido />
          </div>
          <SelectApp
            v-model="registerForm.id_pais_residencia"
            label="Pais"
            :opciones="opcionesPaises"
            placeholder="Seleccionar..."
            :error="erroresRegistro.id_pais_residencia"
            :cargando="catalogos.cargandoPaises"
            requerido
          />
          <SelectApp
            v-model="registerForm.id_ciudad_residencia"
            label="Ciudad"
            :opciones="opcionesCiudades"
            placeholder="Seleccionar..."
            :error="erroresRegistro.id_ciudad_residencia"
            :cargando="cargandoCiudades"
            :deshabilitado="!registerForm.id_pais_residencia"
            requerido
          />
          <SelectApp
            v-model="registerForm.id_pais_nacionalidad"
            label="Nacionalidad"
            :opciones="opcionesPaises"
            placeholder="Seleccionar..."
            :error="erroresRegistro.id_pais_nacionalidad"
            :cargando="catalogos.cargandoPaises"
            requerido
          />
          <InputApp v-model="registerForm.fecha_nacimiento" label="Fecha nacimiento" tipo="date" :error="erroresRegistro.fecha_nacimiento" requerido />
          <SelectApp
            v-model="registerForm.genero"
            label="Genero"
            :opciones="opcionesGenero"
            :error="erroresRegistro.genero"
            requerido
          />
          <InputApp v-model="registerForm.username" label="Username" :error="erroresRegistro.username" requerido />
          <InputApp v-model="registerForm.password" label="Password" tipo="password" :error="erroresRegistro.password" requerido />
          <div class="sm:col-span-2">
            <InputApp v-model="registerForm.confirmar_password" label="Confirmar password" tipo="password" :error="erroresRegistro.confirmar_password" requerido />
          </div>

          <p class="sm:col-span-2 rounded-2xl bg-slate-50 px-4 py-3 text-sm text-text-muted">
            Los datos registrados se usaran para generar la factura de esta reserva.
          </p>

          <button
            type="submit"
            class="sm:col-span-2 w-full rounded-2xl bg-gold px-6 py-4 font-semibold text-navy transition-colors hover:bg-gold-light disabled:cursor-not-allowed disabled:bg-gold/50"
            :disabled="procesandoPago"
          >
            Crear cuenta y pagar
          </button>
        </form>
      </div>
    </div>

    <div
      v-if="mostrarModalPagoSimulado"
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4 py-8"
      @click.self="cerrarPagoSimulado"
    >
      <form
        class="w-full max-w-2xl rounded-[28px] bg-white p-6 shadow-2xl sm:p-8"
        @submit.prevent="confirmarPagoSimulado"
      >
        <div class="flex items-start justify-between gap-4">
          <div>
            <p class="text-sm font-semibold uppercase tracking-[0.24em] text-gold-dark">Pago seguro</p>
            <h2 class="mt-2 text-2xl font-bold text-navy">Pasarela de pago</h2>
            <p class="mt-2 text-sm text-text-muted">
              Ingresa los datos de tu tarjeta para validar el pago y continuar con la reserva.
            </p>
          </div>
          <button type="button" class="rounded-xl p-2 text-slate-500 hover:bg-slate-100" @click="cerrarPagoSimulado">
            x
          </button>
        </div>

        <div
          v-if="estadoPagoSimulado === 'processing' || estadoPagoSimulado === 'confirming'"
          class="mt-8 flex min-h-[440px] flex-col items-center justify-center text-center"
        >
          <div class="relative h-40 w-40">
            <div class="absolute inset-0 rounded-full border border-navy/15"></div>
            <div class="absolute inset-4 rounded-full border border-dashed border-blue-accent/35"></div>
            <div class="absolute inset-0 animate-[spin_3.2s_linear_infinite]">
              <div class="absolute left-1/2 top-0 flex h-12 w-12 -translate-x-1/2 items-center justify-center rounded-full bg-blue-accent text-white shadow-lg shadow-blue-accent/20">
                <svg class="h-7 w-7" fill="currentColor" viewBox="0 0 64 64" aria-hidden="true">
                  <path d="M58.9 36.4 36.5 29.8V8.7C36.5 5.7 34.5 3 32 3s-4.5 2.7-4.5 5.7v21.1L5.1 36.4C3.9 36.8 3 37.9 3 39.2v3.2c0 1 .9 1.8 1.9 1.5L27.5 39v9.8l-7.3 5.3c-.7.5-1.1 1.3-1.1 2.2v2.2c0 .8.8 1.3 1.5 1L32 55.3l11.4 4.2c.7.3 1.5-.2 1.5-1v-2.2c0-.9-.4-1.7-1.1-2.2l-7.3-5.3V39l22.6 4.9c1 .2 1.9-.5 1.9-1.5v-3.2c0-1.3-.9-2.4-2.1-2.8Z" />
                </svg>
              </div>
            </div>
            <div class="absolute inset-12 rounded-full bg-slate-50 shadow-inner"></div>
          </div>
          <h3 class="mt-6 text-2xl font-bold text-navy">
            {{ estadoPagoSimulado === 'confirming' ? 'Confirmando reserva' : 'Procesando pago' }}
          </h3>
          <p class="mt-2 max-w-sm text-sm text-text-muted">
            {{ estadoPagoSimulado === 'confirming' ? 'Estamos generando tu reserva y boleto.' : 'Validando tu transaccion de forma segura...' }}
          </p>
          <p class="mt-5 rounded-2xl bg-slate-50 px-5 py-3 text-lg font-semibold text-navy">{{ moneda(totalPagar) }}</p>
        </div>

        <div
          v-else-if="estadoPagoSimulado === 'approved'"
          class="mt-8 flex min-h-[440px] flex-col items-center justify-center text-center"
        >
          <div class="flex h-28 w-28 items-center justify-center rounded-full bg-emerald-500 text-white shadow-lg shadow-emerald-500/20">
            <svg class="h-14 w-14" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
            </svg>
          </div>
          <h3 class="mt-6 text-2xl font-bold text-navy">Pago aprobado</h3>
          <p class="mt-2 max-w-sm text-sm text-text-muted">Estamos confirmando tu reserva.</p>
        </div>

        <template v-else>
        <div v-if="errorPagoSimulado" class="mt-6 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {{ errorPagoSimulado }}
        </div>

        <div class="mt-6 rounded-2xl bg-slate-50 px-5 py-5">
          <div class="flex items-center justify-between gap-4">
            <div>
              <p class="text-sm text-text-muted">Total a pagar</p>
              <p class="mt-1 text-3xl font-bold text-navy">{{ moneda(totalPagar) }}</p>
            </div>
            <div class="text-right text-sm text-text-muted">
              <p>{{ itemsPago.length }} pasajero{{ itemsPago.length === 1 ? '' : 's' }}</p>
              <p>IVA incluido</p>
            </div>
          </div>
        </div>

        <div class="mt-6 grid gap-4 sm:grid-cols-2">
          <label class="block sm:col-span-2">
            <span class="mb-2 block text-sm font-semibold text-navy">Titular de la tarjeta</span>
            <input
              v-model="pagoSimuladoForm.titular"
              type="text"
              autocomplete="cc-name"
              class="w-full rounded-xl border border-transparent bg-gray-100 px-4 py-3 text-sm text-text-main outline-none transition focus:border-blue-accent focus:ring-2 focus:ring-blue-accent/20"
              :class="erroresPagoSimulado.titular && '!border-error focus:!ring-error/20'"
              placeholder="Nombre Apellido"
            />
            <p v-if="erroresPagoSimulado.titular" class="mt-1.5 text-xs text-error">{{ erroresPagoSimulado.titular }}</p>
          </label>

          <label class="block sm:col-span-2">
            <span class="mb-2 block text-sm font-semibold text-navy">Numero de tarjeta</span>
            <div class="relative">
              <input
                :value="pagoSimuladoForm.numeroTarjeta"
                type="text"
                inputmode="numeric"
                autocomplete="cc-number"
                maxlength="19"
                class="w-full rounded-xl border border-transparent bg-gray-100 px-4 py-3 pr-12 text-sm text-text-main outline-none transition focus:border-blue-accent focus:ring-2 focus:ring-blue-accent/20"
                :class="[
                  erroresPagoSimulado.numeroTarjeta && '!border-error focus:!ring-error/20',
                  numeroTarjetaValido && !erroresPagoSimulado.numeroTarjeta && '!border-emerald-400 focus:!border-emerald-500 focus:!ring-emerald-500/20',
                ]"
                placeholder="4111 1111 1111 1111"
                @input="handleNumeroTarjetaInput"
              />
              <span
                v-if="numeroTarjetaValido && !erroresPagoSimulado.numeroTarjeta"
                class="pointer-events-none absolute right-4 top-1/2 flex h-6 w-6 -translate-y-1/2 items-center justify-center rounded-full bg-emerald-500 text-white"
                aria-hidden="true"
              >
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
                </svg>
              </span>
            </div>
            <p v-if="erroresPagoSimulado.numeroTarjeta" class="mt-1.5 text-xs text-error">{{ erroresPagoSimulado.numeroTarjeta }}</p>
          </label>

          <label class="block">
            <span class="mb-2 block text-sm font-semibold text-navy">Expiracion</span>
            <div class="relative">
              <input
                :value="pagoSimuladoForm.expiracion"
                type="text"
                inputmode="numeric"
                autocomplete="cc-exp"
                maxlength="5"
                class="w-full rounded-xl border border-transparent bg-gray-100 px-4 py-3 pr-12 text-sm text-text-main outline-none transition focus:border-blue-accent focus:ring-2 focus:ring-blue-accent/20"
                :class="[
                  erroresPagoSimulado.expiracion && '!border-error focus:!ring-error/20',
                  expiracionTarjetaValida && !erroresPagoSimulado.expiracion && '!border-emerald-400 focus:!border-emerald-500 focus:!ring-emerald-500/20',
                ]"
                placeholder="MM/AA"
                @input="handleExpiracionTarjetaInput"
              />
              <span
                v-if="expiracionTarjetaValida && !erroresPagoSimulado.expiracion"
                class="pointer-events-none absolute right-4 top-1/2 flex h-6 w-6 -translate-y-1/2 items-center justify-center rounded-full bg-emerald-500 text-white"
                aria-hidden="true"
              >
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
                </svg>
              </span>
            </div>
            <p v-if="erroresPagoSimulado.expiracion" class="mt-1.5 text-xs text-error">{{ erroresPagoSimulado.expiracion }}</p>
          </label>

          <label class="block">
            <span class="mb-2 block text-sm font-semibold text-navy">CVV</span>
            <div class="relative">
              <input
                :value="pagoSimuladoForm.cvv"
                type="password"
                inputmode="numeric"
                autocomplete="cc-csc"
                maxlength="4"
                class="w-full rounded-xl border border-transparent bg-gray-100 px-4 py-3 pr-12 text-sm text-text-main outline-none transition focus:border-blue-accent focus:ring-2 focus:ring-blue-accent/20"
                :class="[
                  erroresPagoSimulado.cvv && '!border-error focus:!ring-error/20',
                  cvvValido && !erroresPagoSimulado.cvv && '!border-emerald-400 focus:!border-emerald-500 focus:!ring-emerald-500/20',
                ]"
                placeholder="123"
                @input="handleCvvInput"
              />
              <span
                v-if="cvvValido && !erroresPagoSimulado.cvv"
                class="pointer-events-none absolute right-4 top-1/2 flex h-6 w-6 -translate-y-1/2 items-center justify-center rounded-full bg-emerald-500 text-white"
                aria-hidden="true"
              >
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
                </svg>
              </span>
            </div>
            <p v-if="erroresPagoSimulado.cvv" class="mt-1.5 text-xs text-error">{{ erroresPagoSimulado.cvv }}</p>
          </label>

          <label class="block sm:col-span-2">
            <span class="mb-2 block text-sm font-semibold text-navy">Tipo de pago</span>
            <select
              v-model="pagoSimuladoForm.tipo"
              class="w-full rounded-xl border border-transparent bg-gray-100 px-4 py-3 text-sm text-text-main outline-none transition focus:border-blue-accent focus:ring-2 focus:ring-blue-accent/20"
            >
              <option value="CREDITO">Credito</option>
              <option value="DEBITO">Debito</option>
            </select>
          </label>
        </div>

        <button
          type="submit"
          class="mt-6 w-full rounded-2xl bg-gold px-6 py-4 font-semibold text-navy transition-colors hover:bg-gold-light disabled:cursor-not-allowed disabled:bg-gold/50"
          :disabled="procesandoPago || procesandoPagoSimulado"
        >
          {{ procesandoPagoSimulado || procesandoPago ? 'Procesando...' : 'Procesar pago' }}
        </button>
        </template>
      </form>
    </div>
  </div>
</template>
