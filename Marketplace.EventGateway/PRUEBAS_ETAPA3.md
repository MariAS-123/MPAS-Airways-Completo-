# Pruebas Etapa 3 — Marketplace.EventGateway (Hot Chocolate)

**Hot Chocolate** es la librería GraphQL para .NET. Incluye **Banana Cake Pop**: la UI en `/graphql` para probar sin app móvil.

## Antes de probar

1. Docker RabbitMQ: `docker compose up -d` en `docker/rabbitmq`
2. Levantar en Visual Studio:
   - `Microservicio.Clientes.Api`
   - `Microservicio.Vuelos.Api` (gRPC)
   - `Microservicio.ReservasF.Api`
   - `Marketplace.EventGateway.Api`
3. Reservas: login vía Gateway (JWT en `AccessToken`) o `ServiceToken` en Development

## Abrir Banana Cake Pop

**http://localhost:5290/graphql** (perfil http)

---

## 1. Login

```graphql
mutation Login {
  login(input: { username: "admin", password: "admin123" }) {
    token
    usuario
    roles
  }
}
```

HTTP Headers en Banana Cake Pop:

```json
{
  "Authorization": "Bearer TU_TOKEN"
}
```

---

## 2. Consultas

```graphql
query { aeropuertos(nombre: "Guaya", limit: 5) }
query { buscarVuelos(origen: "GYE", destino: "UIO", fecha: "2026-06-15") }
query { vuelo(idVuelo: 27) }
query { asientosVuelo(idVuelo: 27, disponible: true) }
```

---

## 3. Mutations (mismo correlationId en los 3)

Ver ejemplos completos en el chat o publicar paso a paso: `seleccionarVuelo` → `registrarPasajeros` → `solicitarReserva`.

---

## 4. Estado saga

```graphql
query {
  estadoReserva(correlationId: "UUID") {
    estado
    ultimoPaso
    idReserva
    codigoReserva
    motivoRechazo
  }
}
```
