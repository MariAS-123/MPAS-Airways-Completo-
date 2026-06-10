import apiClient from './axios'

// POST /auth/login — AllowAnonymous
export const loginApi = (datos) =>
  apiClient.post('/auth/login', datos)

// POST /auth/logout — Autenticado
export const logoutApi = () =>
  apiClient.post('/auth/logout')

// POST /auth/register-cliente — AllowAnonymous, asigna rol CLIENTE automáticamente
export const registerClienteApi = (datos) =>
  apiClient.post('/auth/register-cliente', datos)
