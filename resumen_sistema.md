# Sistema de Gestión de Vuelos — Resumen Técnico (verificado en código)

**Proyecto:** Sistema de Gestión de Vuelos  
**Materia:** Integración de Sistemas — Pontificia Universidad Católica del Ecuador  
**Instructora:** María Paulina Astudillo  
**Stack:** .NET 10 / C# / ASP.NET Core / Entity Framework Core / PostgreSQL (Supabase) / Azure App Service (Linux)  
**Frontend:** React — Vercel ([frontend-microservicio-vuelos-five.vercel.app](https://frontend-microservicio-vuelos-integ.vercel.app/))  
**Última revisión contra código:** junio 2026 — usar este documento como base para migración.

---

## 1. Arquitectura general

Arquitectura de microservicios: cada servicio tiene su propia API y base de datos. El **único punto de entrada para el frontend** es el **Bus de Integración (Middleware)**. El frontend no debe llamar microservicios directamente.

```
Frontend (Vercel)
        ↓ HTTP REST + JWT
Bus de Integración (Middleware) — solo REST hacia los MS
        ↓ HTTP REST
MS Seguridad | MS Geografía | MS Aeropuertos | MS Vuelos | MS Clientes | MS ReservasF
        ↓
MS ReservasF ──gRPC (Grpc-Web)──► MS Vuelos (servidor gRPC)
        ↓
PostgreSQL (Supabase) — una BD por microservicio
```

### 1.1 Estructura del repositorio (importante para migración)

En esta carpeta coexisten **variantes duplicadas** del mismo dominio:

| Carpeta | REST público | gRPC |
|---------|--------------|------|
| `Microservicio.Vuelos` | Sí | No |
| `Microservicio.VuelosGRPC` | Sí | Servidor `VuelosGrpc` |
| `Mircroservicio.ReservasF` | Sí | Cliente HTTP → Vuelos |
| `Microservicio.ReservasFGRPC` | Sí | Cliente gRPC → Vuelos |

**Despliegue recomendado:** `Microservicio.VuelosGRPC` + `Microservicio.ReservasFGRPC` + `Middleware.Microservicio.VuelosV2SI`.

### 1.2 Bus y gRPC (estado real)

- El Bus **orquesta por HTTP** (`VuelosClient`, `ReservasFClient`, etc.).
- En `Program.cs` hay registro de `ReservasGrpcClient`, pero **no hay `ReservasFGrpcUrl` en appsettings** ni uso en orquestadores → **código a medias, no usar para migración hasta completarlo**.
- `GrpcExtensions.cs` indica explícitamente: el Bus usa solo REST.

---

## 2. Microservicios

### 2.1 MS Seguridad

**Azure:** `https://microservicioseguridadapi20260516222919-cchvcyhwhrhackgj.eastus-01.azurewebsites.net`

- Usuarios, login, JWT.
- Roles: `ADMINISTRADOR`, `AEROLINEA`, `CLIENTE`.
- JWT: Issuer `SistemaVuelos`, Audience `SistemaVuelosClientes`, expiración 60 min (config compartida con otros MS).

**Endpoints clave:** `POST /api/v1/auth/login`, `POST /api/v1/auth/register` (rutas según controllers del MS).

---

### 2.2 MS Geografía

**Azure:** `https://microserviciogeografiaapi20260516223506-dbfwbue6f7gsdbda.eastus-01.azurewebsites.net`

- Países y ciudades (catálogo).
- `GET /api/v1/paises`, `GET /api/v1/ciudades`, filtros por país.

---

### 2.3 MS Aeropuertos

**Azure:** `https://microservicioaeropuertosapi20260516224021-crfghycmdkexbqfy.eastus-01.azurewebsites.net`

- CRUD aeropuertos, IATA/ICAO, enlace a ciudad.
- **Quirk:** columnas `char(3)`/`char(4)` en PostgreSQL con padding — el Bus enriquece con trim/`IdCiudadRaw` donde aplica.

**Endpoints:** `GET /api/v1/aeropuertos`, `GET /api/v1/aeropuertos/{id}`, booking público en MS Vuelos.

---

### 2.4 MS Vuelos (`Microservicio.VuelosGRPC` en producción)

**Azure REST:** `https://microserviciovuelosapi-hwb0huh6d7ena8an.eastus-01.azurewebsites.net`  
**Azure gRPC:** `https://microserviciovuelosgrpc-awauh9c6f0huexfb.eastus-01.azurewebsites.net`

- Vuelos, escalas, asientos, booking público.
- Al crear vuelo: generación automática de asientos por capacidad/clase.
- Estados de vuelo: `PROGRAMADO`, `EN_VUELO`, `ATERRIZADO`, `CANCELADO`, `DEMORADO`.
- Asientos: flag `disponible` (bloqueo/liberación vía gRPC o REST admin).

**REST booking (MS y Bus reenvían):**

- `GET /api/v1/booking/vuelos/buscar`
- `GET /api/v1/booking/vuelos/{id}`
- `GET /api/v1/booking/vuelos/{id}/asientos`
- `GET /api/v1/booking/vuelos/{id}/escalas`
- `POST /api/v1/booking/vuelos/sesion-redirect` — JWT con `id_vuelo`, `url_retorno`

**No implementado en backend C# (solo documentado antes):** `buscar-ida-vuelta`, `sesion-redirect-ida-vuelta`, payload `IDA_VUELTA`. Validar en frontend si existe lógica solo en cliente.

**Servidor gRPC — 6 RPCs:**

```proto
service VuelosGrpc {
  rpc GetVuelo           (GetVueloRequest)        returns (VueloGrpcResponse);
  rpc ValidarVuelo       (GetVueloRequest)        returns (VueloGrpcResponse);
  rpc GetAsientosByVuelo (GetVueloRequest)        returns (AsientosGrpcResponse);
  rpc ValidarAsiento     (ValidarAsientoRequest)  returns (AsientoGrpcResponse);
  rpc BloquearAsiento    (BloquearAsientoRequest) returns (AsientoGrpcResponse);
  rpc LiberarAsiento     (LiberarAsientoRequest)  returns (AsientoGrpcResponse);
}
```

Kestrel: `Http1AndHttp2`, `UseGrpcWeb()`, `MapGrpcService<VuelosGrpcService>().EnableGrpcWeb()`.

---

### 2.5 MS Clientes

**Azure:** `https://microservicioclientesapi-d5gfb6btgghfa9gd.eastus-01.azurewebsites.net`

- Clientes y pasajeros.
- Registro integrado con Seguridad (creación de usuario vinculado, `id_cliente` en JWT).

**Endpoints:** `POST /api/v1/clientes/registro`, `GET /api/v1/clientes/{id}`, pasajeros por `id_cliente`.

---

### 2.6 MS ReservasF (`Microservicio.ReservasFGRPC` en producción)

**Azure:** `https://microservicioreservasf-fudfcsfwbec8bzbc.eastus-01.azurewebsites.net`

- Reservas, facturas, boletos, equipaje (esquema PostgreSQL `ventas`).
- Integración con Vuelos: **`VuelosGrpcIntegrationService`** (gRPC).
- Integración con Clientes: HTTP.

**Estados de reserva (código real):** `PEN`, `CON`, `CAN`, `FIN`, `EMI` — no existe `PAG`.  
Flujo típico: crear en `PEN` → al pagar pasa por `CON` → emisión `EMI`.

**Cálculo de precios (regla de negocio documentada):**

```
subtotal  = precio_vuelo + precio_asiento + equipaje_bodega (ej. 45 USD)
valor_iva = subtotal * 0.15
total     = subtotal + valor_iva + cargo_servicio (desde frontend)
```

**Endpoints clave:**

- `POST /api/v1/reservas`
- `PATCH /api/v1/reservas/{id}/pagar` — body `{ cargoServicio, equipaje[] }`
- `PATCH /api/v1/reservas/{id}/cancelar`
- `GET /api/v1/reservas`, facturas, boletos (filtros por cliente/reserva)

**Cuándo usa gRPC hacia Vuelos:**

| Momento | RPC / acción |
|---------|----------------|
| Crear reserva | `GetVuelo`, validación de asiento (`ValidarAsiento` vía `ObtenerAsiento`) — **no bloquea** |
| Pagar reserva | `BloquearAsiento` (`MarcarAsientoNoDisponibleAsync`) |
| Cancelar | **No** libera por gRPC desde ReservasF; lo hace el **Bus por REST** si la reserva estaba emitida |

**Fix aplicado:** `FacturaRepository.ObtenerPagedAsync` — paginación en BD (evita timeout por `ObtenerTodosAsync`).

**Cliente gRPC (ReservasF):** `GrpcWebHandler` + URL en `Integrations:Vuelos:GrpcUrl`.

---

## 3. Bus de Integración (Middleware)

**Azure:** `https://middlewaremicroserviciovuelos-bpf7bsgqh8g2cea5.eastus-01.azurewebsites.net`  
**Local:** IIS Express ~44366 (según `launchSettings`)

Orquestadores: `ReservaOrchestrator`, `VuelosOrchestrator`, `PortalClienteOrchestrator`, etc.  
Comunicación con MS: **HTTP + reenvío de JWT** (`TokenForwardingHandler`).

### 3.1 Booking (públicos — `AllowAnonymous`)

| Método | Ruta | Notas |
|--------|------|--------|
| GET | `/api/v1/booking/vuelos/buscar` | Proxy a MS Vuelos; resuelve IATA vía Aeropuertos si faltan códigos |
| GET | `/api/v1/booking/vuelos/{id_vuelo}` | Detalle enriquecido con nombres de aeropuertos |
| GET | `/api/v1/booking/vuelos/{id_vuelo}/escalas` | Escalas de un vuelo |
| GET | `/api/v1/booking/vuelos/{id_vuelo}/asientos` | Asientos |
| GET | `/api/v1/booking/aeropuertos` | Búsqueda aeropuertos |
| POST | `/api/v1/booking/vuelos/sesion-redirect` | JWT sesión aerolínea |

### 3.2 Reservas (JWT)

| Método | Ruta | Roles (`ReservasController`) |
|--------|------|------------------------------|
| GET | `/api/v1/reservas` | `ADMINISTRADOR`, `AEROLINEA` |
| GET | `/api/v1/reservas/{id}` | `ADMINISTRADOR`, `AEROLINEA`, `CLIENTE` |
| POST | `/api/v1/reservas` | `ADMINISTRADOR`, `AEROLINEA`, `CLIENTE` |
| PATCH | `/api/v1/reservas/{id}/pagar` | `ADMINISTRADOR`, `AEROLINEA`, `CLIENTE` |
| PATCH | `/api/v1/reservas/{id}/cancelar` | `ADMINISTRADOR`, `AEROLINEA`, `CLIENTE` |
| PATCH | `/api/v1/reservas/{id}/estado` | Admin/aerolínea |
| GET | `/api/v1/reservas/{id}/boletos` | Según controller |

`ReservaOrchestrator` al **crear**: valida vuelo, cliente, asientos y pasajeros vía REST; crea en ReservasF vía REST.  
Al **cancelar**: cancela en ReservasF; si estaba emitida, **libera asientos por REST** (`VuelosDataService.LiberarAsientoAsync`).

### 3.3 Portal cliente (`CLIENTE`)

Ruta base: `/api/v1/portal/cliente/`

- `GET reservas`, `reservas/{id}/detalle`, `reservas/by-codigo/{codigo}`
- `GET reservas/{id}/factura`, `reservas/{id}/boleto`
- `GET boletos`, `GET facturas`

### 3.4 Admin

CRUD vía Bus para aeropuertos, vuelos, escalas, asientos, clientes, pasajeros, reservas, boletos, facturas, equipaje, geografía, seguridad (según controllers en `Controllers/V1/`).

---

## 4. Implementación gRPC

### Motivación (alineada al código)

MS ReservasF consulta MS Vuelos en cada reserva y pago. gRPC concentra operaciones internas de **consulta, validación y bloqueo de asientos** con contrato Protobuf. El Bus **no participa** en ese canal.

### Uso por operación

| Operación | Quién | Protocolo |
|-----------|--------|-----------|
| Booking / admin vuelos | Frontend → Bus → Vuelos | REST |
| Crear reserva (validar vuelo/asiento) | ReservasF → Vuelos | gRPC |
| Pagar (bloquear asiento) | ReservasF → Vuelos | gRPC `BloquearAsiento` |
| Cancelar emitida (liberar) | Bus → Vuelos | REST (PATCH disponibilidad asiento) |
| Liberar asiento | RPC existe en Vuelos gRPC | Usado por servidor; cancelación vía Bus usa REST |

### Azure

- App Services Vuelos gRPC y ReservasF: HTTP/2, `WEBSITE_HTTP20ENABLED=1`, **gRPC-Web** (proxy Azure).
- Certificado: en dev a veces `DangerousAcceptAnyServerCertificateValidator` — no llevar a producción.

### Registro DI ReservasF

Excluir `IVueloIntegrationService` del registro automático para no pisar `VuelosGrpcIntegrationService`.

---

## 5. Flujo completo de una reserva (verificado)

```
 1. Frontend → GET /api/v1/booking/vuelos/buscar (Bus)
 2. Bus → MS Vuelos (REST)
 3. Frontend → POST /api/v1/booking/vuelos/sesion-redirect (Bus → Vuelos REST)
 4. JWT { id_vuelo, url_retorno } → frontend /aerolinea
 5. GET asientos (Bus → Vuelos REST)
 6. Login/registro (Bus → Seguridad + Clientes REST)
 7. POST /api/v1/reservas (Bus)
    → ReservaOrchestrator: validaciones REST (vuelo, cliente, asientos, pasajeros)
    → POST MS ReservasF (REST)
    → ReservasF: valida vuelo/asientos (gRPC), persiste reserva estado PEN
       (asientos NO se bloquean aún)
 8. PATCH /api/v1/reservas/{id}/pagar (Bus → ReservasF REST)
    → ReservasF: factura, BloquearAsiento (gRPC), boletos, estados CON/EMI
 9. Redirección url_retorno (frontend)

Cancelación:
 → PATCH cancelar (Bus → ReservasF)
 → Si emitida: Bus libera asientos (REST → Vuelos)
```

**Riesgo conocido:** entre pasos 7 y 8 dos usuarios pueden elegir el mismo asiento; el bloqueo ocurre al pagar.

---

## 6. Configuración

### Supabase

Session Pooler **puerto 5432**, `No Reset On Close=true`. Evitar Transaction Pooler 6543 para este proyecto.

### Bus — `appsettings.json` (URLs Mari, sin secretos)

```json
{
  "Microservicios": {
    "SeguridadBaseUrl": "https://microservicioseguridadapi20260516222919-cchvcyhwhrhackgj.eastus-01.azurewebsites.net",
    "GeografiaBaseUrl": "https://microserviciogeografiaapi20260516223506-dbfwbue6f7gsdbda.eastus-01.azurewebsites.net",
    "AeropuertosBaseUrl": "https://microservicioaeropuertosapi20260516224021-crfghycmdkexbqfy.eastus-01.azurewebsites.net",
    "VuelosBaseUrl": "https://microserviciovuelosapi-hwb0huh6d7ena8an.eastus-01.azurewebsites.net",
    "ClientesBaseUrl": "https://microservicioclientesapi-d5gfb6btgghfa9gd.eastus-01.azurewebsites.net",
    "ReservasFBaseUrl": "https://microservicioreservasf-fudfcsfwbec8bzbc.eastus-01.azurewebsites.net"
  }
}
```

JWT y connection strings: usar variables de entorno / Azure App Settings, no commitear valores reales.

### ReservasF — integración gRPC Vuelos

```json
"Integrations": {
  "Vuelos": {
    "GrpcUrl": "https://microserviciovuelosgrpc-awauh9c6f0huexfb.eastus-01.azurewebsites.net"
  }
}
```

---

## 7. Problemas resueltos (referencia)

| Problema | Solución |
|----------|----------|
| IATA null por `char` padding | Trim / campos raw en Bus |
| Timeout facturas paginadas | `ObtenerPagedAsync` en repositorio |
| 403 boletos/facturas portal | Rol `CLIENTE` en controllers MS |
| gRPC en Azure | gRPC-Web + HTTP/2 en App Service |
| `IVueloIntegrationService` pisado por convención | Exclusión en registro automático |
| Binding GET con DTO | `[FromQuery]` explícito |

---

## 8. Checklist pre-migración

Validar en el entorno destino (no asumir solo por este documento):

- [ ] Bus responde booking + reservas + portal cliente
- [ ] ReservasF desplegado es variante **GRPC** y alcanza URL gRPC de Vuelos
- [ ] Vuelos desplegado es variante **VuelosGRPC** (servidor gRPC activo)
- [ ] Flujo: crear reserva (PEN) → pagar (asiento bloqueado, EMI) → factura/boleto
- [ ] Cancelar reserva emitida libera asiento (vía Bus REST)
- [ ] JWT mismo issuer/audience/secret en todos los MS
- [ ] Secretos fuera de `appsettings` en repo
- [ ] Decidir si ida/vuelta se implementa en backend o solo en frontend

---

## 9. Documentación adicional en el repo

- `Microservicio.ReservasFGRPC/BUS_INTEGRACION_RESERVASF.md` — contrato detallado ReservasF ↔ Bus
- `Microservicio.VuelosGRPC/docs/guia-bus-integracion-ms-vuelos.md`
- `Microservicio.Seguridad/GUIA_BUS_INTEGRACION_SEGURIDAD.md`
- `Microservicio.Clientes/DOCUMENTO_BUS_INTEGRACION_CLIENTES.md`
- `Microservicio.Aeropuertos/BUS_INTEGRACION_AEROPUERTOS.md`

---

## 10. Historial de cambios del documento

| Versión | Cambio |
|---------|--------|
| Original | Resumen completo del equipo |
| Verificado jun-2026 | Corregido flujo gRPC (bloqueo al pagar), estados reserva, endpoints Bus, roles, ida/vuelta pendiente, Bus solo REST, duplicados de carpeta |
