import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

const KEY = 'mpas_reserva'

function _cargar() {
  try { return JSON.parse(sessionStorage.getItem(KEY) || 'null') } catch { return null }
}
function _guardar(d) { sessionStorage.setItem(KEY, JSON.stringify(d)) }

export const useReservaStore = defineStore('reserva', () => {
  const _init = _cargar()

  const vuelo     = ref(_init?.vuelo || null)
  const pasajeros = ref(_init?.pasajeros || [])
  const asientos  = ref(_init?.asientos || [])
  const equipaje  = ref(_init?.equipaje || [])

  // Hay una reserva en progreso si hay vuelo seleccionado
  const tienePendiente = computed(() => !!vuelo.value)

  // Totales — IVA 15% según contrato §11.9
  const subtotal = computed(() =>
    asientos.value.reduce((acc, a) => {
      const base  = vuelo.value?.precioBase || 0
      const extra = a.precioExtra || 0
      return acc + base + extra
    }, 0),
  )
  const iva   = computed(() => Number((subtotal.value * 0.15).toFixed(2)))
  const total = computed(() => Number((subtotal.value + iva.value).toFixed(2)))

  function setVuelo(v)     { vuelo.value = v;     _sync() }
  function setPasajeros(p) { pasajeros.value = p;  _sync() }
  function setAsientos(a)  { asientos.value = a;   _sync() }
  function setEquipaje(e)  { equipaje.value = e;   _sync() }

  function limpiar() {
    vuelo.value = null; pasajeros.value = []; asientos.value = []; equipaje.value = []
    sessionStorage.removeItem(KEY)
  }

  function _sync() {
    _guardar({ vuelo: vuelo.value, pasajeros: pasajeros.value, asientos: asientos.value, equipaje: equipaje.value })
  }

  return { vuelo, pasajeros, asientos, equipaje, tienePendiente, subtotal, iva, total, setVuelo, setPasajeros, setAsientos, setEquipaje, limpiar }
})
