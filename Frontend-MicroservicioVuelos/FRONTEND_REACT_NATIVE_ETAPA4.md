# Etapa 4 — App móvil Marketplace (React Native)

Documento para el equipo de **frontend**. Resume qué construyó backend/integración en Reto 3 y qué deben replicar en **React Native**, con la **misma experiencia de usuario** que la web Vue del marketplace, pero usando el **nuevo camino GraphQL + eventos** (no el Bus REST).

---

## 1. Contexto: dos caminos distintos

| | Web Vue (marketplace actual) | App React Native (Reto 4) |
|---|------------------------------|-----------------------------|
| **Puerta de entrada** | Middleware / Bus REST | **Marketplace.EventGateway** GraphQL |
| **Reservar** | `POST /api/v1/reservas` síncrono | 3 **mutations** + **polling** de estado |
| **Buscar vuelos** | `GET /api/v1/booking/vuelos/buscar` | Query GraphQL `buscarVuelos` |
| **Admin / web interna** | Sigue con Bus | **No la tocan** |

**Regla:** la app móvil **no debe llamar** al Middleware ni a los microservicios directamente. **Todo** va al Gateway GraphQL.

---

## 2. URL del backend (Azure — producción)

```text
GraphQL endpoint:
https://marketplaceeventgateway-cgf9bbfccqh7d8ev.eastus-01.azurewebsites.net/graphql
```

Explorar schema / probar queries: misma URL en el navegador (Nitro / Banana Cake Pop).

**Rama Git con todo el backend:** `graphql-rabbitmq`

---

## 3. Qué hace el backend (para que entiendan el flujo)

```
App RN  →  Gateway GraphQL  →  (consultas) HTTP a MS Vuelos / Seguridad
                           →  (acciones)   RabbitMQ CloudAMQP
                                              ↓
                                    Clientes / Vuelos / Reservas
                                              ↓
                                    Gateway escucha respuestas
                                              ↓
                                    estadoReserva (polling)
```

- **Queries** = lectura inmediata (vuelos, asientos, aeropuertos).
- **Mutations** = “encolé tu pedido” (no esperes reserva creada al instante).
- **estadoReserva** = consultar cada 2–3 s hasta `RESERVA_CREADA` o `RECHAZADA`.

Esto es **consistencia eventual** (requerido por el reto). La UI debe mostrar **“Reserva en proceso…”**, no “Listo” en el mismo click.

---

## 4. Pantallas a implementar (equivalente Vue → RN)

Repliquen la **misma interfaz y pasos** que el marketplace Vue; solo cambia **cómo** llaman al backend.

| # | Pantalla Vue (referencia UX) | React Native | API Gateway |
|---|------------------------------|--------------|-------------|
| 1 | Buscar vuelos (origen, destino, fecha) | Igual | `buscarVuelos` |
| 2 | Listado de resultados | Igual | parsear JSON de la query |
| 3 | Detalle del vuelo | Igual | `vuelo(idVuelo)` |
| 4 | Mapa / lista de asientos | Igual | `asientosVuelo(idVuelo, disponible: true)` |
| 5 | Login / registro cliente | Igual UX | `login` mutation |
| 6 | Datos de pasajero(s) | Igual | `registrarPasajeros` |
| 7 | Resumen y confirmar | Igual + **estado “procesando”** | `solicitarReserva` + polling |
| 8 | Confirmación / error | Igual | `estadoReserva` |

**Autocompletar aeropuertos (si existe en Vue):** query `aeropuertos(nombre, limit)`.

---

## 5. Autenticación

### Login

```graphql
mutation Login($username: String!, $password: String!) {
  login(input: { username: $username, password: $password }) {
    token
    usuario
    roles
    expiracion
  }
}
```

Credenciales de prueba (mismo Seguridad que la web):

```text
usuario: admin
password: admin123
```

### Header en todas las requests siguientes

```http
Authorization: Bearer <token>
Content-Type: application/json
```

El Gateway reenvía ese JWT en los eventos RabbitMQ (Reservas valida cliente con ese token).

Guardar en contexto/AsyncStorage: `token`, `idCliente` (si lo obtienen de otro endpoint o lo conocen en demo), datos de sesión.

---

## 6. Queries (lectura)

Varias queries devuelven **`String`** con JSON adentro (no objetos GraphQL tipados). Hay que hacer **`JSON.parse`** en el cliente.

### Buscar vuelos

```graphql
query BuscarVuelos($origen: String, $destino: String, $fecha: Date) {
  buscarVuelos(origen: $origen, destino: $destino, fecha: $fecha)
}
```

Ejemplo: `origen: "GYE"`, `destino: "UIO"`, `fecha: "2026-06-15"`.

### Detalle vuelo

```graphql
query Vuelo($idVuelo: Int!) {
  vuelo(idVuelo: $idVuelo)
}
```

### Asientos

```graphql
query Asientos($idVuelo: Int!, $disponible: Boolean) {
  asientosVuelo(idVuelo: $idVuelo, disponible: $disponible)
}
```

Respuesta típica (después de parsear):

```json
{
  "idVuelo": 25,
  "numeroVuelo": "AV0025",
  "resumen": { "totalAsientos": 168, "disponibles": 168, "ocupados": 0 },
  "asientos": [
    { "idAsiento": 4033, "numeroAsiento": "1A", "clase": "PRIMERA", "disponible": true, "precioExtra": 80.0 }
  ]
}
```

### Aeropuertos

```graphql
query Aeropuertos($nombre: String, $limit: Int) {
  aeropuertos(nombre: $nombre, limit: 5)
}
```

---

## 7. Saga de reserva (lo más importante)

### Concepto: `correlationId`

- UUID que **identifica toda la reserva** de punta a punta.
- **Generarlo al iniciar** el checkout (o dejar que la 1.ª mutation lo devuelva).
- **Reutilizar el mismo** en las 3 mutations y en `estadoReserva`.

Flujo recomendado en la app:

```text
1. Usuario elige vuelo + asiento
2. App genera correlationId (uuid v4) y lo guarda en estado global
3. seleccionarVuelo   → esperar / polling
4. registrarPasajeros
5. solicitarReserva
6. polling estadoReserva cada 2-3 s (máx ~30-60 s)
7. RESERVA_CREADA → pantalla éxito con codigoReserva
   RECHAZADA      → mostrar motivoRechazo
```

---

## 8. Mutations (escritura)

### Paso 1 — Seleccionar vuelo y asiento

```graphql
mutation SeleccionarVuelo($input: SeleccionarVueloInput!) {
  seleccionarVuelo(input: $input) {
    correlationId
    paso
    mensaje
  }
}
```

Variables ejemplo:

```json
{
  "input": {
    "correlationId": "b7e2a1c4-5d8f-4e3a-9b2c-1d6e8f0a2b4c",
    "idCliente": 3,
    "idVuelo": 25,
    "idAsiento": 4033
  }
}
```

### Paso 2 — Registrar pasajeros

```graphql
mutation RegistrarPasajeros($input: RegistrarPasajerosInput!) {
  registrarPasajeros(input: $input) {
    correlationId
    paso
    mensaje
  }
}
```

**Obligatorio:** `requiereAsistencia: false` (o true si aplica). Si no se envía, GraphQL puede rechazar la mutation.

```json
{
  "input": {
    "correlationId": "b7e2a1c4-5d8f-4e3a-9b2c-1d6e8f0a2b4c",
    "idCliente": 3,
    "pasajeros": [{
      "idPasajero": 3,
      "nombrePasajero": "Mari",
      "apellidoPasajero": "Astudillo",
      "tipoDocumentoPasajero": "CEDULA",
      "numeroDocumentoPasajero": "1724403033",
      "requiereAsistencia": false
    }]
  }
}
```

### Paso 3 — Solicitar reserva

```graphql
mutation SolicitarReserva($input: SolicitarReservaInput!) {
  solicitarReserva(input: $input) {
    correlationId
    paso
    mensaje
  }
}
```

```json
{
  "input": {
    "correlationId": "b7e2a1c4-5d8f-4e3a-9b2c-1d6e8f0a2b4c",
    "idCliente": 3,
    "idVuelo": 25,
    "subtotalReserva": 100.0,
    "valorIva": 15.0,
    "totalReserva": 115.0,
    "cargoServicio": 5.0,
    "origenCanalReserva": "APP",
    "contactoEmail": "cliente@mail.com",
    "contactoTelefono": "0999999999",
    "detalles": [{
      "idPasajero": 3,
      "idAsiento": 4033,
      "subtotalLinea": 100.0,
      "valorIvaLinea": 15.0,
      "totalLinea": 115.0
    }],
    "equipaje": [
      { "idPasajero": 3, "tipo": "MANO", "pesoKg": 8.0 },
      { "idPasajero": 3, "tipo": "BODEGA", "pesoKg": 20.0, "descripcionEquipaje": "Maleta mediana" }
    ]
  }
}
```

**Sin equipaje extra:** enviar `"equipaje": []` (o omitir el campo). La reserva queda **EMI** en Supabase pero **no** habrá filas en `ventas.equipaje`.

**Importante:**

- `origenCanalReserva` debe ser **`"APP"`** (no `"MARKETPLACE"`).
- `idAsiento` debe ser del **mismo vuelo** elegido (cada vuelo tiene IDs distintos).
- Misma lógica de precios que Vue:

```text
precio_equipaje = MANO → $0 (máx 10 kg) | BODEGA → $45 fijo (máx 23 kg)
subtotal        = precio_vuelo + precio_asiento + sum(precio_equipaje)
valor_iva       = subtotal * 0.15
total           = subtotal + valor_iva + cargo_servicio
```

| Campo equipaje | Tipo | Notas |
|----------------|------|--------|
| `idPasajero` | Int | Mismo pasajero que en `detalles` (no hace falta `idDetalle`; el MS lo resuelve al crear la reserva) |
| `tipo` | String | `"MANO"` o `"BODEGA"` |
| `pesoKg` | Decimal | Obligatorio, > 0 |
| `descripcionEquipaje` | String? | Opcional |

Lo que se envía en `equipaje` se persiste en **`ventas.equipaje`** al completar la saga (mismo flujo que Vue al pagar). Si el usuario no añade maletas, la lista vacía deja la reserva sin equipaje en BD.

---

## 9. Polling — estado de la saga

```graphql
query EstadoReserva($correlationId: UUID!) {
  estadoReserva(correlationId: $correlationId) {
    correlationId
    estado
    ultimoPaso
    idReserva
    codigoReserva
    idsPasajerosValidados
    motivoRechazo
    codigoError
    actualizadoEnUtc
  }
}
```

### Valores de `estado` (enum)

| estado | Significado | UI sugerida |
|--------|-------------|-------------|
| `ASIENTO_PRE_RESERVADO` | Vuelos OK | “Validando…” |
| `PASAJEROS_VALIDADOS` | Clientes OK | “Creando reserva…” |
| `RESERVA_EN_PROCESO` | Reservas procesando | Spinner |
| `RESERVA_CREADA` | **Éxito** | Pantalla confirmación |
| `RECHAZADA` | Error negocio | Mostrar `motivoRechazo` |

Ejemplo rechazo real (no es bug de red):

```text
"El pasajero 3 ya tiene una reserva activa en este vuelo."
```

→ Probar otro vuelo o pasajero.

Ejemplo éxito (probado en Azure):

```json
{
  "estado": "RESERVA_CREADA",
  "idReserva": 32,
  "codigoReserva": "RES-20260609-A0E765B1"
}
```

### Implementación polling (pseudocódigo)

```javascript
async function esperarReserva(correlationId, token) {
  const maxIntentos = 20;
  const intervaloMs = 3000;

  for (let i = 0; i < maxIntentos; i++) {
    const { data } = await client.query({
      query: ESTADO_RESERVA,
      variables: { correlationId },
      fetchPolicy: 'network-only',
      context: { headers: { Authorization: `Bearer ${token}` } }
    });

    const estado = data?.estadoReserva?.estado;

    if (estado === 'RESERVA_CREADA') return { ok: true, ...data.estadoReserva };
    if (estado === 'RECHAZADA') return { ok: false, ...data.estadoReserva };

    await sleep(intervaloMs);
  }
  return { ok: false, timeout: true };
}
```

---

## 10. Stack sugerido en React Native

| Pieza | Recomendación |
|-------|----------------|
| Cliente GraphQL | **Apollo Client** o **urql** |
| UUID | `react-native-uuid` o `crypto.randomUUID()` |
| Estado global | Context / Zustand / Redux (guardar `correlationId`, `token`, carrito) |
| HTTP | El cliente GraphQL maneja POST a `/graphql` |
| Storage | AsyncStorage para token |

### Config Apollo (ejemplo)

```javascript
import { ApolloClient, InMemoryCache, createHttpLink } from '@apollo/client';
import { setContext } from '@apollo/client/link/context';

const httpLink = createHttpLink({
  uri: 'https://marketplaceeventgateway-cgf9bbfccqh7d8ev.eastus-01.azurewebsites.net/graphql',
});

const authLink = setContext((_, { headers }) => ({
  headers: {
    ...headers,
    authorization: token ? `Bearer ${token}` : '',
  },
}));

export const client = new ApolloClient({
  link: authLink.concat(httpLink),
  cache: new InMemoryCache(),
});
```

---

## 11. Diferencias clave vs Vue (no olvidar)

| Vue (Bus REST) | React Native (Gateway) |
|----------------|------------------------|
| Un solo `POST /reservas` y respuesta inmediata | 3 mutations + polling |
| URLs del Middleware | Solo URL GraphQL del Gateway |
| Respuestas JSON tipadas del Bus | Algunas queries son `String` → parsear |
| Sin `correlationId` | **Obligatorio** mismo UUID en toda la saga |
| — | Pantalla **“en proceso”** obligatoria |
| Pago en flujo web (`PATCH pagar`) | **Fuera de scope Reto 3** si el profe no pidió pago en app; la saga termina en `RESERVA_CREADA` (estado PEN) |

---

## 12. Lo que NO deben hacer

- No consumir el **Middleware** / Bus desde la app móvil.
- No llamar REST directo a Vuelos, Clientes o Reservas.
- No asumir que la reserva está lista al cerrar `solicitarReserva`.
- No reutilizar el mismo pasajero + vuelo si ya hay reserva activa (error de negocio).
- No usar `localhost` en dispositivo físico; usar URL Azure del Gateway.
- En emulador Android, `localhost` apunta al emulador, no a la PC — usar URL Azure o IP de la máquina si prueban local.

---

## 13. Checklist de entrega (Etapa 4)

- [ ] Mismas pantallas / flujo UX que marketplace Vue
- [ ] Login con JWT y header en mutations
- [ ] Buscar vuelos → detalle → asientos
- [ ] Saga completa con `correlationId` persistente
- [ ] UI “Reserva en proceso” + polling
- [ ] Manejo de `RECHAZADA` con mensaje al usuario
- [ ] Éxito muestra `codigoReserva` e `idReserva`
- [ ] Config URL Gateway en un solo archivo `.env` o `config.ts`
- [ ] Probado contra Azure (no solo local)

---

## 14. Referencias en el repo

| Recurso | Ubicación |
|---------|-----------|
| Gateway GraphQL (queries/mutations) | `Marketplace.EventGateway/Marketplace.EventGateway.Api/GraphQL/MarketplaceGraphQL.cs` |
| Pruebas manuales Gateway | `Marketplace.EventGateway/PRUEBAS_ETAPA3.md` |
| Plan completo Reto 3 | `PLAN_RETO3_PASO_A_PASO.md` |
| Flujo web actual (Vue + Bus) | `resumen_sistema.md` sección 5 |
| Estados saga | `Marketplace.Events.Contracts/Saga/MarketplaceSagaStatus.cs` |

---

## 15. Contacto / dudas backend

Si una mutation responde OK pero `estadoReserva` no avanza:

1. Verificar mismo `correlationId` en los 3 pasos.
2. Verificar header `Authorization`.
3. Probar la misma saga en el navegador: `.../graphql`.
4. Revisar con backend si CloudAMQP y MS en Azure están arriba.

---

*Última actualización: Etapa 3 validada en Azure (saga completa → `RESERVA_CREADA`). Rama: `graphql-rabbitmq`.*
