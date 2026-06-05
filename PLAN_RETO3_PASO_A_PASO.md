# Plan Reto 3 — Paso a paso (lenguaje sencillo)

**Materia:** Integración de Sistemas  
**Qué estás construyendo:** Un “camino nuevo” solo para el **cliente del marketplace** (app móvil), con mensajes (RabbitMQ) y una puerta GraphQL.  
**Qué NO se toca:** El Bus actual, la web de administración, el booking que ya funciona en el navegador.

---

## Antes de empezar — Léelo una vez

### ¿Qué tienes hoy?

- Varios programas pequeños (**microservicios**): Seguridad, Vuelos, Clientes, Reservas, etc.
- Un **Bus** (Middleware) que junta todo para la web y el admin.
- Todo eso ya está en **Azure** y **te funciona**. No lo vamos a romper.

### ¿Qué son las carpetas duplicadas?

En tu computadora hay dos carpetas parecidas:

| Carpeta | En palabras simples |
|---------|---------------------|
| `Microservicio.Vuelos` | Versión vieja, solo web REST |
| `Microservicio.VuelosGRPC` | Versión buena: REST + canal rápido gRPC (la que usa Reservas) |
| `Mircroservicio.ReservasF` | Versión vieja |
| `Microservicio.ReservasFGRPC` | Versión buena: habla con Vuelos por gRPC |

**Para el Reto 3 trabaja SIEMPRE en:**

- `Microservicio.VuelosGRPC`
- `Microservicio.ReservasFGRPC`
- `Microservicio.Clientes`

**Ignora** las carpetas sin GRPC (son copia antigua), salvo que tu profe diga lo contrario.

### ¿Qué vas a agregar?

1. **Oyentes de mensajes** en 3 microservicios (como un buzón interno).
2. **RabbitMQ** en Docker (el cartero entre programas).
3. **Gateway nuevo** (`Marketplace.EventGateway`) con GraphQL (puerta solo del marketplace móvil).
4. **App móvil** en React Native.

### Orden que pide tu profesora (obligatorio)

```
Etapa 1 → Cambiar los 3 microservicios (mensajes)
Etapa 2 → Instalar y configurar RabbitMQ
Etapa 3 → Crear el Gateway con GraphQL
Etapa 4 → App móvil React Native
```

**Regla de oro:** Termina y prueba cada etapa antes de la siguiente.

### Rama de Git (recomendado)

```text
rama: feature/reto3-marketplace
```

Un commit cuando termines cada etapa. No mezcles etapas en un solo commit.

---

## Lo que NUNCA debes tocar en este reto

- Carpeta `Middleware.Microservicio.VuelosV2SI` (el Bus actual).
- `Microservicio.Seguridad`
- `Microservicio.Geografia`
- `Microservicio.Aeropuertos`
- Las URLs de Azure que ya usa el Bus (no las cambies en el Bus).
- La web de administración y el flujo del booking en navegador que ya funciona.

---

# ETAPA 1 — Preparar los microservicios para mensajes

**Objetivo de la etapa:** Que Vuelos, Clientes y Reservas “sepan escuchar y enviar mensajes”, **sin borrar** lo que ya hacen por internet (REST). Es como agregar un buzón nuevo al lado del teléfono que ya tenían.

**Al terminar esta etapa:** El código compila, la lógica vieja sigue igual, y los mensajes están definidos con nombre y forma. RabbitMQ todavía puede estar apagado (lo conectas en la Etapa 2).

---

## Paso 1.1 — Crear el “diccionario” de mensajes (todos usan las mismas palabras)

### Qué haces

Creas un proyecto pequeño solo con **nombres y formas** de los mensajes, para que nadie invente cosas distintas.

### Dónde

Carpeta nueva en la raíz del repo:

```text
Marketplace.Events.Contracts/
```

### Qué mensajes necesitas (lista del PDF, en español)

| Nombre del mensaje | Quién lo envía primero | Quién lo recibe |
|--------------------|------------------------|-----------------|
| Cliente eligió vuelo y asiento | Gateway (más adelante) / prueba manual | Vuelos |
| Asiento pre-reservado (15 min) | Vuelos | Reservas |
| Pasajeros registrados | Gateway / prueba | Clientes |
| Pasajeros validados | Clientes | Gateway (más adelante) |
| Equipaje agregado | Gateway / prueba | Reservas |
| Reserva solicitada | Gateway / prueba | Reservas |
| Reserva creada | Reservas | Gateway (más adelante) |
| Factura generada | Reservas | Gateway |
| Boleto emitido | Reservas | Vuelos |
| Reserva rechazada (si falla) | Reservas | Gateway |

Cada mensaje debe llevar:

- Un **id único** del mensaje.
- Un **id de seguimiento** (`correlationId`) para saber que todos los pasos son del mismo cliente en el mismo viaje.
- La **fecha**.
- Los **datos** (id vuelo, id asiento, id cliente, etc.).

### Comandos

```bash
cd "RDA 2 Mari Backup Vuelos Completo con GRPC"
dotnet new classlib -n Marketplace.Events.Contracts -f net10.0
dotnet build Marketplace.Events.Contracts
```

### Cómo sabes que terminaste el paso 1.1

- [ ] El proyecto compila sin error.
- [ ] Tienes escrito en código los nombres de los mensajes de la tabla (aunque falten algunos detalles).

---

## Paso 1.2 — Reservas (lo más importante)

**Carpeta:** `Microservicio.ReservasFGRPC`

### Qué haces en palabras simples

1. Creas una carpeta `Messaging` dentro del proyecto de la API.
2. Programas un **oyente** que, cuando llegue el mensaje “reserva solicitada”, haga **exactamente lo mismo** que hoy hace el botón de crear reserva (usa el servicio que ya existe, no copies la lógica otra vez).
3. Cuando la reserva se guarda bien, **envías** otro mensaje: “reserva creada”.
4. Más adelante, otros oyentes harán factura y boleto (puedes hacerlo en partes).

### Qué NO haces

- No borras los controllers REST.
- No cambias las URLs que el Bus usa en Azure.
- No reescribes las reglas de precio o IVA.

### Paquetes que instalas (una vez)

En el proyecto API de Reservas:

```bash
dotnet add Microservicio.ReservasF.Api package RabbitMQ.Client
dotnet add Microservicio.ReservasF.Api reference ../../Marketplace.Events.Contracts
```

### Truco para esta etapa (RabbitMQ apagado)

En `appsettings.Development.json` pon algo como:

```json
"RabbitMQ": {
  "Enabled": false
}
```

Así el programa arranca **sin** RabbitMQ hasta la Etapa 2. El oyente solo se enciende cuando `Enabled` es `true`.

### Cómo sabes que terminaste el paso 1.2

- [ ] Compila.
- [ ] Crear reserva por **Swagger/REST del MS Reservas** sigue funcionando igual que antes.
- [ ] Existe el archivo del oyente “reserva solicitada” que llama al servicio de crear reserva que ya tenías.
- [ ] Existe código que **publicaría** “reserva creada” (aunque RabbitMQ esté apagado).

---

## Paso 1.3 — Vuelos

**Carpeta:** `Microservicio.VuelosGRPC`

### Qué haces

1. Carpeta `Messaging` en la API.
2. Oyente: “cliente eligió vuelo/asiento” → haces la pre-reserva (15 minutos según el PDF) → envías “asiento pre-reservado”.
3. Oyente: “boleto emitido” → bloqueas el asiento **definitivamente** (usa el mismo camino gRPC que ya usas al pagar).

### Qué NO haces

- No tocas el booking público REST.
- No tocas el servicio gRPC que ya expone Vuelos.

### Cómo sabes que terminaste el paso 1.3

- [ ] Compila.
- [ ] Buscar vuelos y ver asientos por REST del MS Vuelos sigue igual.
- [ ] Tienes los dos oyentes creados (aunque RabbitMQ esté apagado).

---

## Paso 1.4 — Clientes

**Carpeta:** `Microservicio.Clientes`

### Qué haces

1. Carpeta `Messaging`.
2. Oyente: “pasajeros registrados” → validas con la lógica que ya tienes → envías “pasajeros validados”.

### Cómo sabes que terminaste el paso 1.4

- [ ] Compila.
- [ ] Crear cliente y pasajero por REST sigue igual.

---

## ✅ Etapa 1 completa cuando

- [ ] Los 3 microservicios compilan con la carpeta Messaging.
- [ ] El “diccionario” `Marketplace.Events.Contracts` está referenciado.
- [ ] Probaste que REST de cada MS sigue igual (Swagger).
- [ ] **No tocaste** el Bus Middleware.
- [ ] Commit: `reto3-etapa1-mensajes-en-microservicios`

---

# ETAPA 2 — RabbitMQ (el cartero en tu PC)

**Objetivo de la etapa:** Instalar el programa que guarda y reparte mensajes, y **conectar** lo que hiciste en la Etapa 1.

**Al terminar:** Puedes mandar un mensaje de prueba y ver que Reservas crea una fila en la base de datos.

---

## Paso 2.1 — Instalar Docker

1. Instala **Docker Desktop** en Windows si no lo tienes.
2. Ábrelo y espera a que diga que está corriendo.

### Cómo sabes que terminaste

- [ ] Docker Desktop abre sin error.

---

## Paso 2.2 — Levantar RabbitMQ

### Qué haces

Creas una carpeta en el repo:

```text
docker/rabbitmq/docker-compose.yml
```

Con RabbitMQ y su pantalla de administración (puerto 15672).

### Comandos

```bash
cd docker/rabbitmq
docker compose up -d
```

### Cómo sabes que terminaste

- [ ] Abres en el navegador: `http://localhost:15672` (usuario/clave los pones en el compose).
- [ ] Ves el panel de RabbitMQ.

---

## Paso 2.3 — Crear “buzones” y reglas (topología)

En palabras simples configuras:

| Pieza | Para qué sirve |
|-------|----------------|
| **Exchange** (tipo topic) | La oficina de clasificación de correo |
| **Colas** | Buzones donde cae cada tipo de mensaje |
| **Bindings** | Reglas: “si el mensaje se llama X, va al buzón Y” |
| **DLQ** | Buzón de “mensajes que fallaron” para no perder nada |

Nombre sugerido del exchange: `vuelos.marketplace.events`  
Vhost sugerido: `vuelos-marketplace`

Puedes crear esto:

- A mano en la pantalla de RabbitMQ, o
- Con un archivo `definitions.json`, o
- Con un script que ejecutes una vez.

### Cómo sabes que terminaste

- [ ] Ves el exchange y las colas en el panel.
- [ ] Cada mensaje importante de la Etapa 1 tiene su cola.

---

## Paso 2.4 — Encender los microservicios en tu PC

### Qué haces

1. En cada MS, `RabbitMQ:Enabled` → `true` en desarrollo.
2. Host: `localhost`, puerto `5672`, usuario del compose.
3. Ejecutas cada API en local (Visual Studio o `dotnet run`).

### Prueba estrella (la más importante)

1. Sin usar el Bus ni el móvil.
2. En RabbitMQ, **publicas a mano** un mensaje “reserva solicitada” con datos de prueba (un id de cliente, vuelo, asiento que existan en tu base de datos).
3. Miras los logs de Reservas: debe procesar.
4. Miras la base de datos: debe aparecer una reserva en estado pendiente (PEN).
5. Debe publicarse “reserva creada” (lo ves en otra cola o en logs).

### Si algo falla

- El mensaje debe ir a la cola de errores (DLQ), no desaparecer.

### Cómo sabes que terminaste la Etapa 2

- [ ] Los 3 MS se conectan a RabbitMQ (logs sin error de conexión).
- [ ] Prueba “reserva solicitada” funciona de punta a punta en local.
- [ ] Azure del sistema viejo **no lo cambiaste** (esto es solo en tu PC).
- [ ] Commit: `reto3-etapa2-rabbitmq-local`

**Más adelante (cuando toque):** mismo RabbitMQ en Azure estudiantil; solo cambias host/usuario en configuración.

---

# ETAPA 3 — Gateway nuevo (GraphQL)

**Objetivo de la etapa:** Un programa **nuevo y separado** que será la **única puerta** de la app móvil del marketplace.

**No es** el Bus viejo. Es otro proyecto.

---

## Paso 3.1 — Crear el proyecto Gateway

### Dónde

```text
Marketplace.EventGateway/
  Marketplace.EventGateway.Api/
```

### Comandos

```bash
dotnet new webapi -n Marketplace.EventGateway.Api -f net10.0
dotnet add package HotChocolate.AspNetCore
dotnet add package RabbitMQ.Client
```

### Qué hace este programa (dos trabajos)

| Trabajo | Cómo | Ejemplo |
|---------|------|---------|
| **Consultas rápidas** | GraphQL pregunta → Gateway llama por HTTP a Vuelos/Aeropuertos/Seguridad (mismas URLs que ya tienes en Azure, copiadas en el Gateway) | Buscar vuelos, ver asientos |
| **Acciones del cliente** | GraphQL mutation → Gateway **mete un mensaje** en RabbitMQ → responde “recibido, tu número de seguimiento es…” | Confirmar reserva |

### Qué copias del Bus (sin modificar el Bus)

En el `appsettings` del Gateway, copias las URLs de:

- Vuelos REST
- Aeropuertos
- Seguridad (login)
- Clientes (si hace falta)

**Solo copia.** No edites el archivo del Middleware.

---

## Paso 3.2 — Pantalla de consultas (GraphQL Query)

Implementas consultas para:

- Buscar vuelos
- Ver un vuelo
- Ver asientos
- Buscar aeropuertos
- Ver estado de la reserva (`correlationId`)

El cliente móvil usará esto y obtiene respuesta **al instante**.

---

## Paso 3.3 — Acciones (GraphQL Mutation) → RabbitMQ

Cada vez que el usuario hace algo importante:

1. Gateway publica el mensaje correcto.
2. Responde algo como: “Aceptado, tu código de seguimiento es ABC-123”.
3. El microservicio (Etapa 1) hace el trabajo detrás.
4. Gateway escucha “reserva creada” o “rechazada” y guarda el estado para que la app pregunte después.

---

## Paso 3.4 — Probar sin móvil

Herramientas: navegador en `/graphql` (Banana Cake Pop) o Postman.

### Escenario de prueba

1. Consulta: buscar vuelos → deben salir datos.
2. Mutation: confirmar reserva (con datos reales de tu BD de prueba).
3. Consulta: estado de reserva con el código de seguimiento → primero “en proceso”, luego “creada”.

### Cómo sabes que terminaste la Etapa 3

- [ ] Gateway corre en local.
- [ ] Consultas devuelven datos reales.
- [ ] Mutations mueven mensajes en RabbitMQ y Reservas crea la reserva.
- [ ] El Bus y la web admin en Azure siguen igual.
- [ ] Commit: `reto3-etapa3-gateway-graphql`

---

# ETAPA 4 — App móvil (React Native)

**Objetivo:** Solo el flujo del **cliente** en el marketplace. La administración sigue en la web vieja con el Bus REST.

---

## Paso 4.1 — Crear la app

```bash
npx create-expo-app mobile-marketplace
```

Instalar cliente GraphQL (Apollo o similar).

Configurar la URL del Gateway: `http://TU_IP_LOCAL:puerto/graphql` (en el celular emulador a veces no es `localhost`; usa la IP de tu PC).

---

## Paso 4.2 — Pantallas mínimas (en orden)

| # | Pantalla | Qué hace |
|---|----------|----------|
| 1 | Buscar vuelos | Llama al Gateway (consulta) |
| 2 | Detalle y asientos | Consulta al Gateway |
| 3 | Login | Gateway → Seguridad |
| 4 | Pasajeros | Mutation → mensaje |
| 5 | Confirmar reserva | Mutation → mensaje + muestra “en proceso” |
| 6 | Estado reserva | Consulta cada pocos segundos hasta “lista” |

### Comportamiento importante (consistencia eventual)

Después de confirmar, la app **no** debe decir al instante “listo pagado”. Debe decir **“tu reserva se está procesando”** y refrescar hasta ver confirmación. Eso es lo correcto en eventos (lo pide el PDF).

---

## Paso 4.3 — Prueba final

1. App móvil → Gateway → RabbitMQ → Reservas → mensaje de vuelta → estado en app.
2. En paralelo, abre la web admin: debe seguir funcionando con el Bus, sin usar la app móvil.

### Cómo sabes que terminaste la Etapa 4

- [ ] Flujo completo en emulador o celular.
- [ ] Admin web intacto.
- [ ] Commit: `reto3-etapa4-app-movil`

---

# Resumen en una hoja (imprímela o tenla al lado)

```
ETAPA 1 — Código de mensajes en 3 MS (GRPC + Clientes)
          → Diccionario Marketplace.Events.Contracts
          → Messaging en Reservas, Vuelos, Clientes
          → REST sigue igual | Bus NO se toca

ETAPA 2 — Docker + RabbitMQ + conectar Enabled=true
          → Probar mensaje "reserva solicitada" a mano

ETAPA 3 — Proyecto nuevo Marketplace.EventGateway
          → GraphQL consultas = HTTP a MS
          → GraphQL mutations = RabbitMQ
          → Probar con /graphql sin móvil

ETAPA 4 — React Native solo marketplace
          → Pantalla "en proceso" + polling de estado
          → Admin sigue en web + Bus
```

---

# Preguntas frecuentes (respuestas cortas)

**¿Uso Vuelos y VuelosGRPC juntos?**  
No. Solo programas en **VuelosGRPC**. La otra carpeta es copia vieja.

**¿Tengo que cambiar Azure ahora?**  
No al inicio. Etapas 1 y 2 en tu PC. Azure del sistema viejo queda igual hasta que subas el Gateway (Etapa 3) si el profe lo pide.

**¿El Bus sigue sirviendo?**  
Sí, para admin y web. El móvil nuevo usa el Gateway, no el Bus.

**¿Qué pasa con gRPC?**  
Sigue dentro de Reservas y Vuelos como hoy. Los mensajes solo **disparan** esa lógica; no la reemplazan.

**¿Puedo saltarme una etapa?**  
Mejor no. Tu profesora puso el orden por una razón: primero contratos en MS, luego cartero, luego puerta GraphQL, luego móvil.

---

# Cuando trabajes “en conjunto” (tú + compañero/a + IA)

| Sesión | Enfoque |
|--------|---------|
| Sesión A | Etapa 1.1 + 1.2 (contratos + Reservas) |
| Sesión B | Etapa 1.3 + 1.4 (Vuelos + Clientes) + compilar todo |
| Sesión C | Etapa 2 completa (Docker + prueba manual RabbitMQ) |
| Sesión D | Etapa 3 Gateway (consultas primero, mutations después) |
| Sesión E | Etapa 4 app móvil pantalla por pantalla |

Al cerrar cada sesión: checklist de la etapa + commit en Git.

---

**Documento relacionado:** `resumen_sistema.md` (cómo funciona hoy el sistema).  
**PDF del curso:** `Planificacion_Reto3_Bus_Eventos_Vuelos.pdf`

*Última actualización: alineado al orden del profesor (MS → RabbitMQ → Gateway → React Native).*
