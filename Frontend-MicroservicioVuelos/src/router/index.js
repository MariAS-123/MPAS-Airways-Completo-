import { createRouter, createWebHistory } from 'vue-router'
import { useAutenticacionStore } from '@/stores/autenticacion.store'

import LayoutPublico from '@/layouts/LayoutPublico.vue'
import LayoutCliente from '@/layouts/LayoutCliente.vue'
import LayoutPanel from '@/layouts/LayoutPanel.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: LayoutPublico,
      children: [
        {
          path: '',
          name: 'inicio',
          component: () => import('@/views/publico/InicioView.vue'),
        },
        {
          path: 'login',
          name: 'login',
          component: () => import('@/views/publico/LoginView.vue'),
        },
        {
          path: 'registro',
          name: 'registro',
          component: () => import('@/views/publico/RegistroView.vue'),
        },
        {
          path: 'search',
          alias: 'vuelos',
          name: 'buscar-vuelos',
          component: () => import('@/views/publico/BuscarVuelosView.vue'),
        },
        {
          path: 'flight/:id',
          name: 'detalle-vuelo',
          component: () => import('@/views/publico/DetalleVueloView.vue'),
        },
        {
          path: 'aerolinea',
          name: 'checkout-aerolinea',
          component: () => import('@/views/publico/AerolineaCheckoutView.vue'),
        },
        {
          path: 'passengers',
          name: 'datos-pasajeros',
          component: () => import('@/views/reserva/PasajerosView.vue'),
        },
        {
          path: 'seats',
          name: 'seleccion-asientos',
          component: () => import('@/views/reserva/AsientosView.vue'),
        },
        {
          path: 'baggage',
          name: 'equipaje',
          component: () => import('@/views/reserva/EquipajeView.vue'),
        },
        {
          path: 'payment',
          name: 'pago-reserva',
          component: () => import('@/views/reserva/PagoView.vue'),
        },
        {
          path: 'confirmation',
          name: 'confirmacion-reserva',
          component: () => import('@/views/reserva/ConfirmacionView.vue'),
        },
        {
          path: 'no-autorizado',
          name: 'no-autorizado',
          component: () => import('@/views/publico/NoAutorizadoView.vue'),
        },
      ],
    },
    {
      path: '/cliente',
      component: LayoutCliente,
      meta: { requiresAuth: true, roles: ['CLIENTE'] },
      children: [
        {
          path: '',
          name: 'cliente-perfil',
          component: () => import('@/views/cliente/PerfilClienteView.vue'),
        },
        {
          path: 'portal',
          name: 'cliente-portal',
          component: () => import('@/views/cliente/MisReservasView.vue'),
        },
        {
          path: 'reservas/:id',
          name: 'cliente-reserva-detalle',
          component: () => import('@/views/cliente/DetalleReservaView.vue'),
        },
        {
          path: 'facturas',
          name: 'cliente-facturas',
          component: () => import('@/views/cliente/MisFacturasView.vue'),
        },
        {
          path: 'boletos',
          name: 'cliente-boletos',
          component: () => import('@/views/cliente/MisBoletosView.vue'),
        },
      ],
    },
    {
      path: '/panel',
      component: LayoutPanel,
      meta: { requiresAuth: true, roles: ['ADMINISTRADOR', 'AEROLINEA'] },
      children: [
        {
          path: '',
          name: 'panel-inicio',
          component: () => import('@/views/panel/InicioPanelView.vue'),
        },
        {
          path: 'paises',
          name: 'panel-paises',
          component: () => import('@/views/panel/PaisesView.vue'),
          meta: { requiresAuth: true, roles: ['ADMINISTRADOR'] },
        },
        {
          path: 'ciudades',
          name: 'panel-ciudades',
          component: () => import('@/views/panel/CiudadesView.vue'),
          meta: { requiresAuth: true, roles: ['ADMINISTRADOR'] },
        },
        {
          path: 'aeropuertos',
          name: 'panel-aeropuertos',
          component: () => import('@/views/panel/AeropuertosView.vue'),
          meta: { requiresAuth: true, roles: ['ADMINISTRADOR', 'AEROLINEA'] },
        },
        {
          path: 'vuelos',
          name: 'panel-vuelos',
          component: () => import('@/views/panel/VuelosView.vue'),
          meta: { requiresAuth: true, roles: ['ADMINISTRADOR', 'AEROLINEA'] },
        },
        {
          path: 'asientos',
          name: 'panel-asientos',
          component: () => import('@/views/panel/AsientosView.vue'),
          meta: { requiresAuth: true, roles: ['ADMINISTRADOR', 'AEROLINEA'] },
        },
        {
          path: 'equipaje',
          name: 'panel-equipaje',
          component: () => import('@/views/panel/EquipajeView.vue'),
          meta: { requiresAuth: true, roles: ['ADMINISTRADOR', 'AEROLINEA'] },
        },
        {
          path: 'pasajeros',
          name: 'panel-pasajeros',
          component: () => import('@/views/panel/PasajerosView.vue'),
          meta: { requiresAuth: true, roles: ['ADMINISTRADOR', 'AEROLINEA'] },
        },
        {
          path: 'reservas',
          name: 'panel-reservas',
          component: () => import('@/views/panel/ReservasView.vue'),
          meta: { requiresAuth: true, roles: ['ADMINISTRADOR', 'AEROLINEA'] },
        },
        {
          path: 'facturas',
          name: 'panel-facturas',
          component: () => import('@/views/panel/FacturasView.vue'),
          meta: { requiresAuth: true, roles: ['ADMINISTRADOR', 'AEROLINEA'] },
        },
        {
          path: 'boletos',
          name: 'panel-boletos',
          component: () => import('@/views/panel/BoletosView.vue'),
          meta: { requiresAuth: true, roles: ['ADMINISTRADOR', 'AEROLINEA'] },
        },
        {
          path: 'reservas/:id',
          name: 'panel-reserva-detalle',
          component: () => import('@/views/panel/ReservaDetalleView.vue'),
          meta: { requiresAuth: true, roles: ['ADMINISTRADOR', 'AEROLINEA'] },
        },
        {
          path: 'vuelos/:id/escalas',
          name: 'panel-vuelo-escalas',
          component: () => import('@/views/panel/EscalasView.vue'),
          meta: { requiresAuth: true, roles: ['ADMINISTRADOR', 'AEROLINEA'] },
        },
        {
          path: 'clientes',
          name: 'panel-clientes',
          component: () => import('@/views/panel/ClientesView.vue'),
          meta: { requiresAuth: true, roles: ['ADMINISTRADOR', 'AEROLINEA'] },
        },
      ],
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: '/',
    },
  ],
})

router.beforeEach((to) => {
  const auth = useAutenticacionStore()

  const requiresAuth = to.matched.some((r) => r.meta.requiresAuth)
  const rolesReq = to.matched.find((r) => r.meta.roles)?.meta.roles ?? []

  if (requiresAuth && !auth.estaAutenticado) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (rolesReq.length && !rolesReq.includes(auth.rol)) {
    return { name: 'no-autorizado' }
  }
})

export default router
