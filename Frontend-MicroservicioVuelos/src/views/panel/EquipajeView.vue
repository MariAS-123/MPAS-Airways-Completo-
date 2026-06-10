<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import InputApp from '@/components/base/InputApp.vue'
import SelectApp from '@/components/base/SelectApp.vue'
import { getBoletosApi } from '@/api/boletos.api'
import {
  cambiarEstadoEquipajeApi,
  getEquipajesBoletoApi,
} from '@/api/equipaje.api'
import { deepValue, extractItems } from '@/utils/portalCliente'

const TIPOS = ['MANO', 'BODEGA']
const ESTADOS = ['REGISTRADO', 'EMBARCADO', 'EN_TRANSITO', 'ENTREGADO', 'CANCELADO', 'PERDIDO', 'DANADO']

const cargando = ref(true)
const guardando = ref(false)
const eliminando = ref(false)
const cargandoDetalle = ref(false)
const error = ref('')
const errorModal = ref('')
const busqueda = ref('')
const tipoFiltro = ref('')
const estadoFiltro = ref('')
const equipajes = ref([])
const boletos = ref([])
const modalDetalle = ref(false)
const modalEstado = ref(false)
const equipajeActivo = ref(null)
const equipajeDetalle = ref(null)
const yaValidado = ref(false)
const errores = ref({})

const formEstado = ref({
  estado_equipaje: 'REGISTRADO',
})

const opcionesTipo = [
  { valor: '', etiqueta: 'Todos los tipos' },
  ...TIPOS.map((tipo) => ({ valor: tipo, etiqueta: tipoAmigable(tipo) })),
]

const opcionesEstado = [
  { valor: '', etiqueta: 'Todos los estados' },
  ...ESTADOS.map((estado) => ({ valor: estado, etiqueta: estadoAmigable(estado) })),
]

const opcionesEstadoCambio = ESTADOS.map((estado) => ({
  valor: estado,
  etiqueta: estadoAmigable(estado),
}))

const equipajesFiltrados = computed(() => {
  const termino = busqueda.value.trim().toLowerCase()
  return equipajes.value.filter((equipaje) => {
    if (!termino) return true
    return [equipaje.numeroEtiqueta, equipaje.numeroBoleto, equipaje.codigoReserva]
      .filter(Boolean)
      .some((valor) => String(valor).toLowerCase().includes(termino))
  })
})

function tipoAmigable(tipo) {
  return String(tipo || '').trim().toUpperCase() === 'BODEGA' ? 'Bodega' : 'Mano'
}

function estadoAmigable(estado) {
  return String(estado || '')
    .trim()
    .toLowerCase()
    .replace(/_/g, ' ')
    .replace(/\b\p{L}/gu, (letra) => letra.toUpperCase())
}

function money(valor) {
  return new Intl.NumberFormat('es-EC', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(Number(valor || 0))
}

function badgeTipo(tipo) {
  return String(tipo || '').trim().toUpperCase() === 'BODEGA'
    ? 'bg-amber-100 text-amber-800'
    : 'bg-sky-100 text-sky-800'
}

function badgeEstado(estado) {
  const valor = String(estado || '').trim().toUpperCase()
  if (valor === 'REGISTRADO') return 'bg-slate-100 text-slate-700'
  if (valor === 'EMBARCADO' || valor === 'EN_TRANSITO') return 'bg-blue-100 text-blue-700'
  if (valor === 'ENTREGADO') return 'bg-emerald-100 text-emerald-700'
  if (valor === 'PERDIDO' || valor === 'DANADO') return 'bg-red-100 text-red-700'
  return 'bg-orange-100 text-orange-700'
}

function normalizarBoleto(item) {
  return {
    idBoleto: deepValue(item, ['idBoleto', 'id_boleto', 'id']) || null,
    numeroBoleto: deepValue(item, ['numeroBoleto', 'numero_boleto', 'codigoBoleto', 'codigo_boleto']) || '',
    codigoReserva:
      deepValue(item, ['codigoReserva', 'codigo_reserva']) ||
      deepValue(item, ['reserva', 'codigoReserva']) ||
      '',
  }
}

function resolverBoleto(idBoleto) {
  return boletos.value.find((boleto) => String(boleto.idBoleto) === String(idBoleto)) || null
}

function normalizarEquipaje(item) {
  const idBoleto = deepValue(item, ['idBoleto', 'id_boleto']) || null
  const boleto = resolverBoleto(idBoleto)
  const tipo = String(deepValue(item, ['tipo']) || '').trim().toUpperCase()

  return {
    idEquipaje: deepValue(item, ['idEquipaje', 'id_equipaje', 'id']) || null,
    idBoleto,
    numeroEtiqueta:
      deepValue(item, ['numeroEtiqueta', 'numero_etiqueta', 'etiqueta']) || '',
    numeroBoleto:
      deepValue(item, ['numeroBoleto', 'numero_boleto', 'boleto']) ||
      boleto?.numeroBoleto ||
      '',
    codigoReserva:
      deepValue(item, ['codigoReserva', 'codigo_reserva', 'reserva']) ||
      boleto?.codigoReserva ||
      '',
    tipo,
    pesoKg: Number(deepValue(item, ['pesoKg', 'peso_kg']) || 0),
    precioExtra:
      Number(deepValue(item, ['precioExtra', 'precio_extra']) ?? (tipo === 'BODEGA' ? 45 : 0)) || 0,
    estadoEquipaje:
      String(deepValue(item, ['estadoEquipaje', 'estado_equipaje', 'estado']) || 'REGISTRADO').trim().toUpperCase(),
    descripcionEquipaje: deepValue(item, ['descripcionEquipaje', 'descripcion_equipaje']) || '',
    dimensionesCm: deepValue(item, ['dimensionesCm', 'dimensiones_cm']) || '',
  }
}

async function cargarTodo() {
  cargando.value = true
  error.value = ''
  try {
    const respuestaBoletos = await getBoletosApi({ page: 1, page_size: 200 })

    boletos.value = extractItems(respuestaBoletos).map(normalizarBoleto)
    const respuestasEquipaje = await Promise.all(
      boletos.value.map(async (boleto) => {
        try {
          const respuesta = await getEquipajesBoletoApi(boleto.idBoleto)
          return extractItems(respuesta).map(normalizarEquipaje)
        } catch {
          return []
        }
      }),
    )

    const todos = respuestasEquipaje.flat()
    equipajes.value = todos.filter((equipaje) => {
      const matchTipo = !tipoFiltro.value || equipaje.tipo === tipoFiltro.value
      const matchEstado = !estadoFiltro.value || equipaje.estadoEquipaje === estadoFiltro.value
      return matchTipo && matchEstado
    })
  } catch (err) {
    error.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo cargar el equipaje.'
  } finally {
    cargando.value = false
  }
}

async function abrirDetalle(equipaje) {
  equipajeActivo.value = equipaje
  equipajeDetalle.value = equipaje
  errorModal.value = ''
  cargandoDetalle.value = false
  modalDetalle.value = true
}

function abrirEstado(equipaje) {
  equipajeActivo.value = equipaje
  formEstado.value = {
    estado_equipaje: String(equipaje.estadoEquipaje || 'REGISTRADO'),
  }
  errores.value = {}
  yaValidado.value = false
  errorModal.value = ''
  modalEstado.value = true
}

function validarEstado() {
  const nuevosErrores = {}
  if (!formEstado.value.estado_equipaje) nuevosErrores.estado_equipaje = 'Selecciona el estado.'
  errores.value = nuevosErrores
  return !Object.keys(nuevosErrores).length
}

async function guardarEstado() {
  yaValidado.value = true
  if (guardando.value || !equipajeActivo.value?.idEquipaje || !validarEstado()) return
  guardando.value = true
  errorModal.value = ''

  try {
    await cambiarEstadoEquipajeApi(equipajeActivo.value.idBoleto, equipajeActivo.value.idEquipaje, {
      estado_equipaje: String(formEstado.value.estado_equipaje || '').trim().toUpperCase(),
    })
    modalEstado.value = false
    await cargarTodo()
  } catch (err) {
    errorModal.value =
      err.response?.data?.errors?.join(' ') ||
      err.response?.data?.message ||
      'No se pudo actualizar el estado del equipaje.'
  } finally {
    guardando.value = false
  }
}

watch(
  [tipoFiltro, estadoFiltro],
  async () => {
    if (!cargando.value) await cargarTodo()
  },
)

watch(
  formEstado,
  () => {
    if (yaValidado.value) validarEstado()
  },
  { deep: true },
)

onMounted(cargarTodo)
</script>

<template>
  <section class="space-y-5">
    <div class="flex items-start justify-between gap-4">
      <div>
        <h1 class="text-[2rem] font-bold text-navy">Gestion de Equipaje</h1>
        <p class="mt-1.5 text-sm text-text-muted">Consulta equipajes emitidos y actualiza su estado operativo.</p>
      </div>
    </div>

    <div v-if="error" class="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
      {{ error }}
    </div>

    <section class="grid gap-4 rounded-[24px] bg-white p-4 shadow-sm md:grid-cols-[1fr_220px_220px]">
      <InputApp v-model="busqueda" placeholder="Buscar por etiqueta, boleto o reserva..." />
      <SelectApp v-model="tipoFiltro" :opciones="opcionesTipo" />
      <SelectApp v-model="estadoFiltro" :opciones="opcionesEstado" />
    </section>

    <section class="rounded-[24px] bg-white shadow-sm">
      <div class="overflow-x-auto">
        <table class="min-w-[1080px] w-full border-collapse">
          <thead class="bg-slate-50">
            <tr class="text-left text-sm font-semibold text-navy">
              <th class="px-5 py-3.5">Etiqueta</th>
              <th class="px-5 py-3.5">Boleto</th>
              <th class="px-5 py-3.5">Tipo</th>
              <th class="px-5 py-3.5">Peso</th>
              <th class="px-5 py-3.5">Precio extra</th>
              <th class="px-5 py-3.5">Estado</th>
              <th class="px-5 py-3.5 text-right">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="cargando">
              <td colspan="7" class="px-5 py-5 text-sm text-text-muted">Cargando equipaje...</td>
            </tr>
            <tr v-else-if="!equipajesFiltrados.length">
              <td colspan="7" class="px-5 py-5 text-sm text-text-muted">No hay equipajes registrados.</td>
            </tr>
            <tr
              v-for="equipaje in equipajesFiltrados"
              v-else
              :key="equipaje.idEquipaje"
              class="border-t border-slate-100 align-middle"
            >
              <td class="px-5 py-4 text-sm font-semibold text-navy">
                <div class="min-w-0">
                  <span class="block truncate">{{ equipaje.numeroEtiqueta || '-' }}</span>
                  <span v-if="equipaje.codigoReserva" class="block truncate text-xs text-text-muted">{{ equipaje.codigoReserva }}</span>
                </div>
              </td>
              <td class="px-5 py-4 text-sm text-navy">{{ equipaje.numeroBoleto || '-' }}</td>
              <td class="px-5 py-4">
                <span :class="['rounded-full px-3 py-1 text-xs font-semibold', badgeTipo(equipaje.tipo)]">
                  {{ tipoAmigable(equipaje.tipo) }}
                </span>
              </td>
              <td class="px-5 py-4 text-sm text-navy">{{ equipaje.pesoKg }} kg</td>
              <td class="px-5 py-4 text-sm text-navy">{{ money(equipaje.precioExtra) }}</td>
              <td class="px-5 py-4">
                <span :class="['rounded-full px-3 py-1 text-xs font-semibold', badgeEstado(equipaje.estadoEquipaje)]">
                  {{ estadoAmigable(equipaje.estadoEquipaje) }}
                </span>
              </td>
              <td class="px-5 py-4">
                <div class="flex justify-end gap-2 text-sm">
                  <button type="button" class="rounded-full border border-slate-200 px-3 py-1 text-navy" @click="abrirDetalle(equipaje)">Ver</button>
                  <button type="button" class="rounded-full border border-slate-200 px-3 py-1 text-navy" @click="abrirEstado(equipaje)">Estado</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <div v-if="modalDetalle" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalDetalle = false">
      <div class="w-full max-w-3xl rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">Detalle de Equipaje</h2>

        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{{ errorModal }}</div>
        <div v-else-if="cargandoDetalle" class="mt-6 text-sm text-text-muted">Cargando detalle...</div>

        <div v-else-if="equipajeDetalle" class="mt-6 grid gap-5 md:grid-cols-2">
          <InputApp :model-value="equipajeDetalle.numeroEtiqueta || '-'" label="Etiqueta" deshabilitado />
          <InputApp :model-value="equipajeDetalle.numeroBoleto || '-'" label="Boleto" deshabilitado />
          <InputApp :model-value="equipajeDetalle.codigoReserva || '-'" label="Reserva" deshabilitado />
          <InputApp :model-value="tipoAmigable(equipajeDetalle.tipo)" label="Tipo" deshabilitado />
          <InputApp :model-value="`${equipajeDetalle.pesoKg} kg`" label="Peso" deshabilitado />
          <InputApp :model-value="money(equipajeDetalle.precioExtra)" label="Precio extra" deshabilitado />
          <InputApp :model-value="estadoAmigable(equipajeDetalle.estadoEquipaje)" label="Estado" deshabilitado />
          <InputApp :model-value="equipajeDetalle.dimensionesCm || '-'" label="Dimensiones" deshabilitado />
          <div class="md:col-span-2">
            <InputApp :model-value="equipajeDetalle.descripcionEquipaje || '-'" label="Descripcion" deshabilitado />
          </div>
        </div>

        <div class="mt-8 flex justify-end gap-3">
          <button type="button" class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50" @click="modalDetalle = false">Cerrar</button>
        </div>
      </div>
    </div>

    <div v-if="modalEstado" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 px-4" @click.self="modalEstado = false">
      <div class="w-full max-w-lg rounded-[28px] bg-white p-7 shadow-2xl">
        <h2 class="text-3xl font-bold text-navy">Actualizar Estado</h2>
        <div v-if="errorModal" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{{ errorModal }}</div>
        <div class="mt-6 grid gap-5">
          <SelectApp v-model="formEstado.estado_equipaje" label="Estado" :opciones="opcionesEstadoCambio" :error="errores.estado_equipaje" requerido />
        </div>
        <div class="mt-8 flex justify-end gap-3">
          <button type="button" class="rounded-2xl border border-slate-300 px-5 py-3 text-sm font-semibold text-navy hover:bg-slate-50" @click="modalEstado = false">Cancelar</button>
          <button type="button" class="rounded-2xl bg-gold px-5 py-3 text-sm font-semibold text-navy hover:bg-gold-light disabled:opacity-60" :disabled="guardando" @click="guardarEstado">
            {{ guardando ? 'Guardando...' : 'Guardar' }}
          </button>
        </div>
      </div>
    </div>
  </section>
</template>
