import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { getAeropuertosApi, getCiudadesApi, getPaisesApi } from '@/api/catalogos.api'
import { deepValue, extractItems } from '@/utils/portalCliente'

const CACHE_KEY = 'mpas_catalogos_cache'
const CACHE_TTL = 1000 * 60 * 60 * 6

function leerCache() {
  try {
    const raw = localStorage.getItem(CACHE_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

function guardarCache(payload) {
  localStorage.setItem(CACHE_KEY, JSON.stringify({ ...payload, timestamp: Date.now() }))
}

function limpiarNombreCatalogo(nombre = '') {
  return String(nombre).replace(/\d{6,}$/u, '').trim()
}

function textoCatalogo(valor, claves = []) {
  if (!valor) return ''
  if (typeof valor !== 'object') return limpiarNombreCatalogo(valor)

  for (const clave of claves) {
    const encontrado = valor?.[clave]
    if (encontrado !== undefined && encontrado !== null && typeof encontrado !== 'object') {
      return limpiarNombreCatalogo(encontrado)
    }
  }

  return ''
}

function normalizarPais(item) {
  return {
    ...item,
    idPais: deepValue(item, ['idPais', 'id_pais', 'id']) || null,
    nombre: textoCatalogo(
      deepValue(item, ['nombre', 'nombrePais', 'nombre_pais', 'paisNombre']),
      ['nombre', 'nombrePais', 'nombre_pais'],
    ),
  }
}

function normalizarCiudad(item) {
  const pais = deepValue(item, ['pais', 'Pais'])

  return {
    ...item,
    idCiudad: deepValue(item, ['idCiudad', 'id_ciudad', 'id']) || null,
    idPais: deepValue(item, ['idPais', 'id_pais', 'idPaisResidencia']) || deepValue(pais, ['idPais', 'id_pais', 'id']) || null,
    nombre: textoCatalogo(
      deepValue(item, ['nombre', 'nombreCiudad', 'nombre_ciudad', 'ciudadNombre']),
      ['nombre', 'nombreCiudad', 'nombre_ciudad'],
    ),
    nombrePais: textoCatalogo(
      deepValue(item, ['nombrePais', 'nombre_pais', 'paisNombre']) || pais,
      ['nombre', 'nombrePais', 'nombre_pais'],
    ),
  }
}

export const useCatalogosStore = defineStore('catalogos', () => {
  const aeropuertos = ref([])
  const paises = ref([])
  const ciudadesPorPais = ref({})

  const cargandoAeropuertos = ref(false)
  const cargandoPaises = ref(false)

  const opcionesAeropuertos = computed(() =>
    aeropuertos.value.map((a) => {
      const ciudad = textoCatalogo(a.ciudad ?? a.nombreCiudad ?? a.ciudad_nombre, ['nombre', 'nombreCiudad', 'nombre_ciudad'])
      const pais = textoCatalogo(a.pais ?? a.nombrePais ?? a.pais_nombre, ['nombre', 'nombrePais', 'nombre_pais'])
      const codigo = textoCatalogo(a.codigoIata ?? a.codigo_iata ?? a.iata)
      const nombre = textoCatalogo(a.nombre ?? a.nombreAeropuerto, ['nombre', 'nombreAeropuerto']) || 'Aeropuerto'
      const partes = [codigo, nombre].filter(Boolean)
      const extras = [ciudad, pais].filter(Boolean).join(', ')

      return {
        valor: String(a.idAeropuerto ?? a.id_aeropuerto ?? a.id),
        etiqueta: extras ? `${partes.join(' - ')} (${extras})` : partes.join(' - '),
      }
    }),
  )

  async function cargarAeropuertos(force = false) {
    if (cargandoAeropuertos.value) return
    if (!force && aeropuertos.value.length) return

    const cache = leerCache()
    if (
      !force &&
      cache &&
      Array.isArray(cache.aeropuertos) &&
      Date.now() - (cache.timestamp ?? 0) < CACHE_TTL
    ) {
      aeropuertos.value = cache.aeropuertos
      return
    }

    cargandoAeropuertos.value = true
    try {
      const respuesta = await getAeropuertosApi({ estado: 'ACTIVO', page: 1, page_size: 200 })
      aeropuertos.value = extractItems(respuesta)
      guardarCache({ aeropuertos: aeropuertos.value })
    } finally {
      cargandoAeropuertos.value = false
    }
  }

  async function cargarPaises(force = false) {
    if (cargandoPaises.value) return
    if (!force && paises.value.length) return

    cargandoPaises.value = true
    try {
      const respuesta = await getPaisesApi({ page: 1, page_size: 200 })
      paises.value = extractItems(respuesta)
        .map(normalizarPais)
        .filter((pais) => pais.idPais && pais.nombre)
    } finally {
      cargandoPaises.value = false
    }
  }

  async function cargarCiudadesPorPais(idPais, force = false) {
    const key = String(idPais)
    if (!key) return []
    if (!force && ciudadesPorPais.value[key]?.length) {
      return ciudadesPorPais.value[key]
    }

    const respuesta = await getCiudadesApi({ id_pais: idPais, page: 1, page_size: 200 })
    const ciudades = extractItems(respuesta)
      .map(normalizarCiudad)
      .filter((ciudad) => ciudad.idCiudad && (!ciudad.idPais || String(ciudad.idPais) === key))
    ciudadesPorPais.value = {
      ...ciudadesPorPais.value,
      [key]: ciudades,
    }
    return ciudades
  }

  return {
    aeropuertos,
    paises,
    ciudadesPorPais,
    cargandoAeropuertos,
    cargandoPaises,
    opcionesAeropuertos,
    cargarAeropuertos,
    cargarPaises,
    cargarCiudadesPorPais,
  }
})
