API de Vuelos — Booking Simulator | v3.0 | Abril 2026

# **API de Vuelos**
### Contrato REST — Booking Simulator

_Version 3.0 | Abril 2026_

|Version|v3.0 — Multi-Pasajero + Roles Completos|
|---|---|
|**Base URL (Prod)**|https://api.booking-sim.dev/vuelos/v1|
|**Autenticacion**|Bearer Token JWT|
|**Formato**|JSON (application/json)|
|**IVA aplicado**|15%|
|**Equipo**|Grupo Vuelos — Booking Sim|
|**Fecha**|Abril 2026|



Pagina 1 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

## **1. Informacion General**


**1.1 Roles del Sistema**

|Rol|Codigo|Descripcion|
|---|---|---|
|Administrador|ADMINISTRADOR|Acceso total a todos los recursos del sistema.|
|Aerolinea|AEROLINEA|Gestiona vuelos, escalas, asientos y equipaje.|
|Cliente|CLIENTE|Consulta vuelos y gestiona sus propias reservas y boletos.|



**1.2 Codigos HTTP**

|Codigo|Nombre|Descripcion|
|---|---|---|
|200|OK|Solicitud exitosa.|
|201|Created|Recurso creado exitosamente.|
|400|Bad Request|Datos invalidos o violacion de regla de negocio.|
|401|Unauthorized|Token JWT ausente, invalido o expirado.|
|403|Forbidden|Sin permisos para este recurso.|
|404|Not Found|El recurso no existe.|
|409|Conflict|Conflicto de unicidad: asiento ya reservado, correo duplicado, etc.|
|500|Internal Server Error|Error interno del servidor.|



**1.3 Formato de Respuesta Estandar**


**Exito:**

```
 { "success": true, "message": "OK", "data": { } }

```

**Error:**





**1.4 Seguridad de Datos**





|Regla|Descripcion|
|---|---|
|Contrasenas hasheadas|Las contrasenas NUNCA se almacenan en texto plano. El backend aplica hash<br>con salt antes de guardar en seg.USUARIO_APP (campos password_hash y<br>password_salt).|
|Contrasenas no se devuelven|Los campos password_hash y password_salt NUNCA aparecen en ningun<br>response de la API, sin importar el rol o endpoint consultado.|
|Contrasena solo viaja al<br>backend|El campo password unicamente viaja del frontend al backend en POST<br>/auth/login y POST /auth/register-cliente. Nunca en sentido contrario.|


Pagina 2 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026


|Regla|Descripcion|
|---|---|
|Tokens JWT|Expiran en 24 horas. Al hacer logout se invalidan inmediatamente mediante<br>blacklist — no pueden reutilizarse aunque no hayan expirado.|
|Autorizacion por rol|El backend valida en cada request que el rol del usuario tenga permiso para la<br>operacion. Un CLIENTE no puede acceder a recursos de otros clientes.|
|Auditoria segura|Los registros de auditoria NUNCA incluyen valores de password_hash,<br>password_salt ni tokens en datos_anteriores ni datos_nuevos.|





Pagina 3 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

## **2. Modulo Auth  /api/v1/auth**


Endpoints publicos — no requieren Bearer token.

|Endpoint|Metodo|Auth|
|---|---|---|
|POST /api/v1/auth/login|POST|AllowAnonymous|
|POST /api/v1/auth/register-cliente|POST|AllowAnonymous|
|POST /api/v1/auth/logout|POST|Autenticado|



_Autentica un usuario y devuelve el JWT._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|username|string|Si|Username registrado.|
|password|string|Si|Contrasena del usuario.|



**Ejemplo Request:**





**Ejemplo Response:**





_Registro unificado: crea el cliente con datos personales y el usuario/contrasena en un solo paso. Asigna_
_automaticamente el rol CLIENTE._





**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|tipo_identificacion|string|Si|CEDULA, PASAPORTE, RUC, OTRO.|
|numero_identificacion|string|Si|Numero de documento. Max 30 chars.|
|nombres|string|Si|Nombres. Max 160 chars.|
|apellidos|string|No|Apellidos. Max 160 chars.|
|correo|string|Si|Email valido. Max 150 chars.|
|telefono|string|Si|Telefono. Max 30 chars.|



Pagina 4 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|direccion|string|Si|Direccion. Max 250 chars.|
|id_ciudad_residencia|integer|Si|ID de ciudad del catalogo.|
|id_pais_nacionalidad|integer|Si|ID de pais del catalogo.|
|fecha_nacimiento|date|No|Formato YYYY-MM-DD.|
|genero|string|No|MASCULINO, FEMENINO, OTRO.|
|username|string|Si|Username elegido por el usuario.|
|password|string|Si|Contrasena (validacion de complejidad en el<br>frontend).|



**Ejemplo Request:**





**Ejemplo Response:**





_Invalida el token JWT actual. El token queda en blacklist y no puede reutilizarse._

**Ejemplo Request:**





**Ejemplo Response:**

```
 { "success": true, "message": "Sesion cerrada correctamente." }

```

Pagina 5 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

## **3. Modulo Catalogos Geograficos**


Paises, ciudades y aeropuertos. Lectura publica — escritura solo para roles administrativos.

|Endpoint|Metodo|Auth|
|---|---|---|
|GET /api/v1/paises|GET|AllowAnonymous|
|GET /api/v1/paises/{id_pais}|GET|AllowAnonymous|
|POST /api/v1/paises|POST|ADMINISTRADOR|
|PUT /api/v1/paises/{id_pais}|PUT|ADMINISTRADOR|
|DELETE /api/v1/paises/{id_pais}|DELETE|ADMINISTRADOR|
|GET /api/v1/ciudades|GET|AllowAnonymous|
|GET /api/v1/ciudades/{id_ciudad}|GET|AllowAnonymous|
|POST /api/v1/ciudades|POST|ADMINISTRADOR|
|PUT /api/v1/ciudades/{id_ciudad}|PUT|ADMINISTRADOR|
|DELETE /api/v1/ciudades/{id_ciudad}|DELETE|ADMINISTRADOR|
|GET /api/v1/aeropuertos|GET|AllowAnonymous|
|GET /api/v1/aeropuertos/{id_aeropuerto}|GET|AllowAnonymous|
|POST /api/v1/aeropuertos|POST|ADMINISTRADOR, AEROLINEA|
|PUT /api/v1/aeropuertos/{id_aeropuerto}|PUT|ADMINISTRADOR, AEROLINEA|
|DELETE /api/v1/aeropuertos/{id_aeropuerto}|DELETE|ADMINISTRADOR|



**3.1 Paises**


_Lista todos los paises. Query params: estado (ACTIVO/INACTIVO), page, page_size._

**Ejemplo Response:**




```
GET /api/v1/paises/{id_pais}
```

**AllowAnonymous**

_Obtiene un pais por ID._

**Ejemplo Response:**





Pagina 6 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026


_Crea un nuevo pais._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|codigo_iso2|string|Si|2 letras mayusculas. Unico.|
|codigo_iso3|string|No|3 letras mayusculas. Unico.|
|nombre|string|Si|Nombre del pais. Unico.|
|continente|string|No|Continente.|



**Ejemplo Request:**





**Ejemplo Response:**





**3.2 Ciudades**


_Lista ciudades. Query params: id_pais, page, page_size._

**Ejemplo Response:**





_Crea una nueva ciudad._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|id_pais|integer|Si|ID del pais.|
|nombre|string|Si|Nombre. Unico por pais.|
|zona_horaria|string|No|Formato IANA (ej: America/Guayaquil).|



Pagina 7 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|codigo_postal|string|No|Codigo postal.|



**Ejemplo Request:**

```
 { "id_pais": 6, "nombre": "Cuenca", "zona_horaria": "America/Guayaquil" }

```

**Ejemplo Response:**





**3.3 Aeropuertos**


_Lista aeropuertos. Query params: estado (ACTIVO/INACTIVO), page, page_size._

**Ejemplo Response:**





_Obtiene un aeropuerto por ID._

**Ejemplo Response:**





_Crea un nuevo aeropuerto._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|codigo_iata|string|Si|3 letras mayusculas. Unico.|
|codigo_icao|string|No|4 letras mayusculas. Unico.|
|nombre|string|Si|Nombre completo.|



Pagina 8 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|id_ciudad|integer|No|ID de ciudad.|
|id_pais|integer|Si|ID de pais.|
|zona_horaria|string|No|Formato IANA.|
|latitud|decimal|No|Latitud geografica.|
|longitud|decimal|No|Longitud geografica.|



**Ejemplo Request:**





**Ejemplo Response:**





Pagina 9 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

## **4. Modulo Vuelos**









|Endpoint|Metodo|Auth|
|---|---|---|
|GET /api/v1/vuelos|GET|AllowAnonymous|
|GET /api/v1/vuelos/{id_vuelo}|GET|AllowAnonymous|
|POST /api/v1/vuelos|POST|ADMINISTRADOR, AEROLINEA|
|PUT /api/v1/vuelos/{id_vuelo}|PUT|ADMINISTRADOR, AEROLINEA|
|PATCH /api/v1/vuelos/{id_vuelo}/estado|PATCH|ADMINISTRADOR, AEROLINEA|
|DELETE /api/v1/vuelos/{id_vuelo}|DELETE|ADMINISTRADOR|
|GET /api/v1/vuelos/{id_vuelo}/escalas|GET|AllowAnonymous|
|GET /api/v1/vuelos/{id_vuelo}/escalas/{id_escala}|GET|AllowAnonymous|
|POST /api/v1/vuelos/{id_vuelo}/escalas|POST|ADMINISTRADOR, AEROLINEA|
|DELETE<br>/api/v1/vuelos/{id_vuelo}/escalas/{id_escala}|DELETE|ADMINISTRADOR, AEROLINEA|
|GET /api/v1/vuelos/{id_vuelo}/asientos|GET|AllowAnonymous|
|GET /api/v1/vuelos/{id_vuelo}/asientos/{id_asiento}|GET|AllowAnonymous|
|POST /api/v1/vuelos/{id_vuelo}/asientos|POST|ADMINISTRADOR, AEROLINEA|
|PATCH<br>/api/v1/vuelos/{id_vuelo}/asientos/{id_asiento}|PATCH|ADMINISTRADOR, AEROLINEA|


**4.1 Vuelos**





_Busca vuelos disponibles. Query params requeridos: id_aeropuerto_origen, id_aeropuerto_destino, fecha_salida._
_Opcionales: estado_vuelo (usar PROGRAMADO para Booking), page, page_size._





**Ejemplo Response:**





_Crea un nuevo vuelo. La fecha_hora_llegada se calcula automaticamente: fecha_hora_salida + duracion_min._


**NOTA:** fecha_hora_llegada = fecha_hora_salida + duracion_min. El backend la calcula automaticamente.


Pagina 10 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026


**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|id_aeropuerto_origen|integer|Si|ID del aeropuerto de origen. Distinto al destino.|
|id_aeropuerto_destino|integer|Si|ID del aeropuerto de destino.|
|numero_vuelo|string|Si|Codigo de vuelo (ej: AV1008). Unico por fecha y hora.|
|fecha_hora_salida|datetime|Si|ISO 8601. Debe ser fecha futura.|
|duracion_min|integer|Si|Duracion en minutos. Mayor a 0. Calcula<br>fecha_hora_llegada automaticamente.|
|precio_base|decimal|Si|Precio base por pasajero. Mayor a 0.|
|capacidad_total|integer|Si|Total de asientos del avion. Mayor a 0.|



**Ejemplo Request:**





**Ejemplo Response:**





_Cambia el estado operativo del vuelo._





**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|estado_vuelo|string|Si|PROGRAMADO, EN_VUELO, ATERRIZADO,<br>CANCELADO, DEMORADO.|
|motivo|string|No|Motivo del cambio (recomendado para CANCELADO<br>y DEMORADO).|



**Ejemplo Request:**

```
 { "estado_vuelo": "CANCELADO", "motivo": "Condiciones climaticas adversas" }

```

Pagina 11 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026


**Ejemplo Response:**

```
 { "success": true, "data": { "idVuelo": 1017, "estadoVuelo": "CANCELADO" }}

```

**4.2 Escalas**


_Lista las escalas de un vuelo ordenadas por campo orden. Array vacio = vuelo directo._

**Ejemplo Response:**





_Agrega una escala al vuelo. La duracion_min se calcula automaticamente._





**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|id_aeropuerto|integer|Si|Aeropuerto de escala. Distinto a origen y destino del<br>vuelo.|
|orden|integer|Si|Posicion en el itinerario. Unico por vuelo.|
|fecha_hora_llegada|datetime|Si|Hora de llegada a la escala.|
|fecha_hora_salida|datetime|Si|Hora de salida de la escala. Mayor a<br>fecha_hora_llegada.|
|tipo_escala|string|No|TECNICA o COMERCIAL. Default: COMERCIAL.|
|terminal|string|No|Terminal del aeropuerto.|
|puerta|string|No|Puerta de embarque.|



**Ejemplo Request:**





**Ejemplo Response:**



Pagina 12 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026





**4.3 Asientos**


_Lista asientos del vuelo. Query params: disponible (true/false), clase (ECONOMICA/EJECUTIVA/PRIMERA)._

**Ejemplo Response:**




```
POST /api/v1/vuelos/{id_vuelo}/asientos
```

**ADMINISTRADOR** **AEROLINEA**

_Crea un asiento. No puede exceder la capacidad_total del vuelo._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|numero_asiento|string|Si|Ej: A10, 14B. Unico por vuelo.|
|clase|string|Si|ECONOMICA, EJECUTIVA, PRIMERA.|
|precio_extra|decimal|No|Cargo adicional. Mayor o igual a 0. Default: 0.|
|posicion|string|No|VENTANA, PASILLO, CENTRO.|



**Ejemplo Request:**





**Ejemplo Response:**





_Actualiza disponibilidad u otros datos del asiento._

**Request Body:**


Pagina 13 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|disponible|boolean|No|true o false.|
|precio_extra|decimal|No|Nuevo cargo adicional.|



**Ejemplo Request:**

```
 { "disponible": false }

```

**Ejemplo Response:**

```
 { "success": true, "data": { "idAsiento": 1048, "disponible": false }}

```

Pagina 14 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

## **5. Modulo CRM — Clientes y Pasajeros**


|Endpoint|Metodo|Auth|
|---|---|---|
|GET /api/v1/clientes|GET|ADMINISTRADOR, AEROLINEA|
|GET /api/v1/clientes/{id_cliente}|GET|ADMINISTRADOR, AEROLINEA, CLIENTE<br>(solo propio)|
|POST /api/v1/clientes|POST|ADMINISTRADOR, AEROLINEA|
|PUT /api/v1/clientes/{id_cliente}|PUT|ADMINISTRADOR, CLIENTE (solo propio)|
|DELETE /api/v1/clientes/{id_cliente}|DELETE|ADMINISTRADOR|
|GET /api/v1/pasajeros|GET|ADMINISTRADOR, AEROLINEA|
|GET /api/v1/pasajeros/{id_pasajero}|GET|ADMINISTRADOR, AEROLINEA, CLIENTE<br>(solo propios)|
|POST /api/v1/pasajeros|POST|ADMINISTRADOR, AEROLINEA, CLIENTE|
|PUT /api/v1/pasajeros/{id_pasajero}|PUT|ADMINISTRADOR, AEROLINEA|





**5.1 Clientes**


_Lista clientes. Query params: numero_identificacion, correo, estado (ACT/INA), page, page_size._

**Ejemplo Response:**




```
POST /api/v1/clientes
```

**ADMINISTRADOR** **AEROLINEA**

_Crea un cliente directamente (sin crear usuario). Para registro con usuario usar /auth/register-cliente._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|tipo_identificacion|string|Si|CEDULA, PASAPORTE, RUC, OTRO.|
|numero_identificacion|string|Si|Numero de documento. Max 30 chars. Unico.|
|nombres|string|Si|Nombres. Max 160 chars.|
|apellidos|string|No|Apellidos. Max 160 chars.|
|correo|string|Si|Email valido. Unico. Max 150 chars.|



Pagina 15 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|telefono|string|Si|Telefono. Max 30 chars.|
|direccion|string|Si|Direccion. Max 250 chars.|
|id_ciudad_residencia|integer|Si|ID de ciudad del catalogo.|
|id_pais_nacionalidad|integer|Si|ID de pais del catalogo.|
|fecha_nacimiento|date|No|YYYY-MM-DD. Debe ser fecha pasada.|
|genero|string|No|MASCULINO, FEMENINO, OTRO.|



**Ejemplo Request:**





**Ejemplo Response:**





**5.2 Pasajeros**





_Registra un pasajero. Crear uno por cada persona que viajara. El id_pasajero se usa luego en el array pasajeros_
_del POST /reservas._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|nombre_pasajero|string|Si|Nombre. Max 100 chars.|
|apellido_pasajero|string|Si|Apellido. Max 100 chars.|
|tipo_documento_pasajero|string|Si|CEDULA, PASAPORTE, RUC, OTRO.|
|numero_documento_pasajero|string|Si|Numero de documento. Max 30 chars.|
|id_cliente|integer|No|ID del cliente si el pasajero tiene cuenta en el<br>sistema.|
|fecha_nacimiento_pasajero|date|No|YYYY-MM-DD. Para determinar categoria<br>adulto/nino/bebe.|



Pagina 16 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|email_contacto_pasajero|string|No|Email de contacto.|
|telefono_contacto_pasajero|string|No|Telefono de contacto.|
|genero_pasajero|string|No|MASCULINO, FEMENINO, OTRO.|
|requiere_asistencia|boolean|No|Default: false.|
|observaciones_pasajero|string|No|Observaciones adicionales.|



**Ejemplo Request:**





**Ejemplo Response:**





Pagina 17 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

## **6. Modulo Ventas — Reservas**


|Endpoint|Metodo|Auth|
|---|---|---|
|GET /api/v1/reservas|GET|ADMINISTRADOR, AEROLINEA|
|GET /api/v1/reservas/{id_reserva}|GET|ADMINISTRADOR, AEROLINEA, CLIENTE<br>(solo propia)|
|POST /api/v1/reservas|POST|ADMINISTRADOR, AEROLINEA, CLIENTE|
|PATCH /api/v1/reservas/{id_reserva}/pagar|PATCH|ADMINISTRADOR, AEROLINEA, CLIENTE|
|PATCH /api/v1/reservas/{id_reserva}/estado|PATCH|ADMINISTRADOR, AEROLINEA, CLIENTE<br>(solo CAN)|





**6.1 Ciclo de Vida**







|Estado|Nombre|Descripcion|Quien lo asigna|
|---|---|---|---|
|PEN|Pendiente|Reserva creada, pendiente de<br>pago.|Sistema (automatico al crear)|
|CON|Confirmada|Pago procesado, asientos<br>bloqueados.|Sistema (tras PATCH /pagar en factura)|
|CAN|Cancelada|Cancelada. Asientos liberados.|CLIENTE (propia), AEROLINEA,<br>ADMINISTRADOR, Sistema (pago<br>rechazado)|
|FIN|Finalizada|Vuelo completado.|Sistema (cuando el vuelo aterriza)|
|EMI|Emitida|Todos los boletos emitidos.|Sistema (cuando todos los detalles tienen<br>boleto)|


**6.2 POST /reservas — Crear Reserva**


_Crea la reserva con todos los pasajeros en una sola llamada. Estado inicial: PEN. El backend valida los totales_
_con IVA del 15%._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|id_cliente|integer|Si|ID del cliente que realiza la reserva.|
|id_vuelo|integer|Si|ID del vuelo seleccionado. Debe estar en estado<br>PROGRAMADO.|
|fecha_inicio|datetime|Si|Fecha y hora de salida del vuelo (ISO 8601).|
|fecha_fin|datetime|Si|Fecha y hora de llegada del vuelo (ISO 8601).|
|subtotal_reserva|decimal|Si|Suma de subtotalLinea de todos los pasajeros.|
|valor_iva|decimal|Si|subtotal_reserva * 0.15 (IVA 15%).|
|total_reserva|decimal|Si|subtotal_reserva + valor_iva.|



Pagina 18 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|origen_canal_reserva|string|No|BOOKING (recomendado). Default: WEB.|
|contacto_email|string|No|Email de contacto para notificaciones.|
|contacto_telefono|string|No|Telefono de contacto.|
|pasajeros|array|Si|Array con un objeto por cada pasajero. Ver estructura<br>abajo.|
|pasajeros[].id_pasajero|integer|Si|ID del pasajero creado con POST /pasajeros.|
|pasajeros[].id_asiento|integer|Si|ID del asiento seleccionado (disponible = true).|
|pasajeros[].subtotal_linea|decimal|Si|precio_base + precio_extra_asiento del pasajero.|
|pasajeros[].valor_iva_linea|decimal|Si|subtotal_linea * 0.15.|
|pasajeros[].total_linea|decimal|Si|subtotal_linea + valor_iva_linea.|



**Ejemplo Request:**





**Ejemplo Response:**





_Boton "Pagar" — endpoint principal del checkout. Recibe el cargo de servicio y ejecuta toda la cadena de pago_
_en una sola transaccion atomica. La reserva debe estar en estado PEN._


Pagina 19 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026





**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|cargo_servicio|decimal|Si|Cargo de la plataforma. Puede ser 0.|
|equipaje|array|No|Array con el equipaje por boleto. Ver estructura<br>abajo.|
|equipaje[].id_detalle|integer|Si|ID del detalle (pasajero) al que corresponde el<br>equipaje.|
|equipaje[].tipo|string|Si|MANO o BODEGA.|
|equipaje[].peso_kg|decimal|Si|Peso en kg. MANO: max 10kg. BODEGA: max<br>23kg.|
|equipaje[].descripcion_equipaje|string|No|Descripcion del equipaje.|



**Ejemplo Request:**





**Ejemplo Response:**





_Cambia el estado de la reserva. El CLIENTE solo puede enviar CAN (cancelar su propia reserva)._


Pagina 20 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026





**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|estado_reserva|string|Si|Estado nuevo. Ver transiciones validas.|
|motivo_cancelacion|string|No|Recomendado cuando estado_reserva = CAN.|



**Ejemplo Request:**

```
 { "estado_reserva": "CAN", "motivo_cancelacion": "El cliente cambio sus planes" }

```

**Ejemplo Response:**





Pagina 21 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

## **7. Modulo Ventas — Facturas, Boletos y Equipaje**



|Endpoint|Metodo|Auth|
|---|---|---|
|GET /api/v1/facturas|GET|ADMINISTRADOR, AEROLINEA|
|GET /api/v1/facturas/{id_factura}|GET|ADMINISTRADOR, AEROLINEA,<br>CLIENTE (solo propia)|
|POST /api/v1/facturas|POST|ADMINISTRADOR, AEROLINEA|
|PATCH /api/v1/facturas/{id_factura}/anular|PATCH|ADMINISTRADOR|
|GET /api/v1/boletos|GET|ADMINISTRADOR, AEROLINEA|
|GET /api/v1/boletos/{id_boleto}|GET|ADMINISTRADOR, AEROLINEA,<br>CLIENTE (solo propio)|
|POST /api/v1/boletos|POST|ADMINISTRADOR, AEROLINEA|
|PATCH /api/v1/boletos/{id_boleto}/cancelar|PATCH|ADMINISTRADOR, AEROLINEA|
|GET /api/v1/boletos/{id_boleto}/equipaje|GET|ADMINISTRADOR, AEROLINEA,<br>CLIENTE (solo propio)|
|POST /api/v1/boletos/{id_boleto}/equipaje|POST|ADMINISTRADOR, AEROLINEA|
|PATCH<br>/api/v1/boletos/{id_boleto}/equipaje/{id_equipaje}/estado|PATCH|ADMINISTRADOR, AEROLINEA|


**7.1 Facturas**





_Uso interno y administrativo unicamente. El flujo normal del checkout usa PATCH /reservas/{id}/pagar que crea la_
_factura automaticamente. Solo puede existir una factura por reserva._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|id_cliente|integer|Si|ID del cliente titular de la factura.|
|id_reserva|integer|Si|ID de la reserva.|
|subtotal|decimal|Si|Suma de subtotalLinea de todos los pasajeros.|
|valor_iva|decimal|Si|subtotal * 0.15 (IVA 15%).|
|cargo_servicio|decimal|Si|Cargo de la plataforma. Puede ser 0.|
|total|decimal|Si|subtotal + valor_iva + cargo_servicio.|
|observaciones_factura|string|No|Observaciones adicionales.|



**Ejemplo Request:**





**Ejemplo Response:**



Pagina 22 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026





_Anula una factura. Al anular: la reserva pasa a CAN y los asientos se liberan._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|motivo_inhabilitacion|string|No|Motivo de la anulacion.|



**Ejemplo Request:**

```
 { "motivo_inhabilitacion": "Error en datos de facturacion" }

```

**Ejemplo Response:**

```
 { "success": true, "data": { "idFactura": 23, "estado": "INA" }}

```

**7.2 Boletos**





_Obtiene el detalle completo de un boleto._

**Ejemplo Response:**





Pagina 23 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026


_Cancela un boleto. El asiento vuelve a disponible=true._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|motivo|string|No|Motivo de la cancelacion.|



**Ejemplo Request:**

```
 { "motivo": "Reserva cancelada por el cliente" }

```

**Ejemplo Response:**

```
 { "success": true, "data": { "idBoleto": 17, "estadoBoleto": "CANCELADO" }}

```

**7.3 Equipaje**





|Tipo|Peso Maximo|Costo|Quien lo registra|
|---|---|---|---|
|MANO|10 kg|Incluido — $0.00|Sistema automaticamente al emitir el<br>boleto|
|BODEGA|23 kg|$45.00 fijo por maleta|Sistema si el cliente selecciono bodega<br>en checkout|


_Registra el equipaje de un boleto. El precio_extra lo asigna el backend automaticamente (MANO=$0,_
_BODEGA=$45)._





**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|id_boleto|integer|Si|ID del boleto (debe coincidir con el de la ruta).|
|tipo|string|Si|MANO o BODEGA.|
|peso_kg|decimal|Si|Peso en kg. MANO: max 10kg. BODEGA: max 23kg.|
|descripcion_equipaje|string|No|Descripcion del equipaje.|
|dimensiones_cm|string|No|Formato LxAxA. Ej: 55x40x20.|



**Ejemplo Request:**





Pagina 24 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026





**Ejemplo Response:**




```
PATCH /api/v1/boletos/{id_boleto}/equipaje/{id_equipaje}/estado
```

**ADMINISTRADOR** **AEROLINEA**

_Actualiza el estado del equipaje en el flujo operativo._





**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|estado_equipaje|string|Si|EMBARCADO, EN_TRANSITO, ENTREGADO,<br>CANCELADO, PERDIDO, DANADO.|



**Ejemplo Request:**

```
 { "estado_equipaje": "EMBARCADO" }

```

**Ejemplo Response:**

```
 { "success": true, "data": { "idEquipaje": 15, "estadoEquipaje": "EMBARCADO" }}

```

Pagina 25 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

## **8. Portal Cliente  /api/v1/cliente**


Todos los endpoints requieren rol CLIENTE. El cliente solo accede a sus propios datos.







|Endpoint|Metodo|Descripcion|
|---|---|---|
|GET /api/v1/cliente/reservas|GET|Lista todas las reservas del cliente autenticado.|
|GET /api/v1/cliente/reservas/{id_reserva}|GET|Detalle de una reserva propia.|
|GET /api/v1/cliente/reservas/by-<br>codigo/{codigo}|GET|Buscar reserva por codigo (ej: VU-2026-0CA0A5).|
|GET<br>/api/v1/cliente/reservas/{id_reserva}/detalle|GET|Detalle enriquecido con pasajeros, vuelo, boletos y<br>equipajes.|
|GET<br>/api/v1/cliente/reservas/{id_reserva}/factura|GET|Factura de una reserva propia.|
|GET<br>/api/v1/cliente/reservas/{id_reserva}/boleto|GET|Boletos de una reserva propia.|
|GET /api/v1/cliente/boletos|GET|Todos los boletos del cliente.|
|GET /api/v1/cliente/facturas|GET|Todas las facturas del cliente.|


**8.1 Detalle Enriquecido de Reserva**


GET /api/v1/cliente/reservas/{id_reserva}/detalle — Respuesta completa:





Pagina 26 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026



Pagina 27 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

## **9. Modulo Seguridad**

|Endpoint|Metodo|Auth|
|---|---|---|
|GET /api/v1/roles|GET|ADMINISTRADOR|
|GET /api/v1/roles/{id_rol}|GET|ADMINISTRADOR|
|POST /api/v1/roles|POST|ADMINISTRADOR|
|PUT /api/v1/roles/{id_rol}|PUT|ADMINISTRADOR|
|DELETE /api/v1/roles/{id_rol}|DELETE|ADMINISTRADOR|
|GET /api/v1/usuarios|GET|ADMINISTRADOR|
|GET /api/v1/usuarios/{id_usuario}|GET|ADMINISTRADOR, CLIENTE (solo<br>propio)|
|POST /api/v1/usuarios|POST|ADMINISTRADOR|
|PUT /api/v1/usuarios/{id_usuario}|PUT|ADMINISTRADOR, CLIENTE (solo<br>propio)|
|DELETE /api/v1/usuarios/{id_usuario}|DELETE|ADMINISTRADOR|
|GET /api/v1/usuarios/{id_usuario}/roles|GET|ADMINISTRADOR|
|POST /api/v1/usuarios/{id_usuario}/roles|POST|ADMINISTRADOR|
|DELETE /api/v1/usuarios/{id_usuario}/roles/{id_rol}|DELETE|ADMINISTRADOR|



**9.1 Roles**

```
GET /api/v1/roles
```

**ADMINISTRADOR**

_Lista todos los roles del sistema._

**Ejemplo Response:**




```
POST /api/v1/roles
```

**ADMINISTRADOR**

_Crea un nuevo rol._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|nombre_rol|string|Si|Nombre unico del rol. Max 50 chars.|
|descripcion_rol|string|No|Descripcion. Max 200 chars.|



**Ejemplo Request:**

```
 { "nombre_rol": "SUPERVISOR", "descripcion_rol": "Supervisa operaciones" }

```

Pagina 28 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026


**Ejemplo Response:**





**9.2 Usuarios**


_Obtiene un usuario por ID. El CLIENTE solo puede ver su propio perfil._

**Ejemplo Response:**





_Crea un usuario interno (sin pasar por register-cliente). Para registro normal usar /auth/register-cliente._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|id_cliente|integer|No|ID del cliente a vincular.|
|username|string|Si|Username unico.|
|correo|string|Si|Email unico.|
|password|string|Si|Contrasena.|



**Ejemplo Request:**





**Ejemplo Response:**





_Actualiza datos del usuario. El CLIENTE solo puede editar su propio perfil (cambiar contrasena o correo)._

**Request Body:**


Pagina 29 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|correo|string|No|Nuevo correo. Unico.|
|password|string|No|Nueva contrasena.|



**Ejemplo Request:**

```
 { "correo": "mario_nuevo@mail.com", "password": "nuevaPass456" }

```

**Ejemplo Response:**

```
 { "success": true, "data": { "idUsuario": 205, "correo": "mario_nuevo@mail.com" }}

```

**9.3 Usuario-Roles**


_Asigna un rol a un usuario._

**Request Body:**

|Campo|Tipo|Req.|Descripcion|
|---|---|---|---|
|id_rol|integer|Si|ID del rol a asignar.|



**Ejemplo Request:**

```
 { "id_rol": 2 }

```

**Ejemplo Response:**





Pagina 30 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

## **10. Modulo Auditoria  /api/v1/auditoria**


Solo ADMINISTRADOR. Los registros son de solo lectura — nunca se modifican ni eliminan. El sistema los
genera automaticamente con cada INSERT, UPDATE o DELETE en cualquier tabla.

|Endpoint|Metodo|Auth|
|---|---|---|
|GET /api/v1/auditoria|GET|ADMINISTRADOR|
|GET /api/v1/auditoria/{id_auditoria}|GET|ADMINISTRADOR|



_Lista logs de auditoria. Query params: tabla_afectada, operacion (INSERT/UPDATE/DELETE), usuario_ejecutor,_
_fecha_desde, fecha_hasta, page, page_size._

**Ejemplo Response:**





Pagina 31 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

## **12. Flujo de Compra Completo**


|Paso|Endpoint|Actor|Descripcion|
|---|---|---|---|
|1|GET /api/v1/aeropuertos|Frontend|Cargar catalogo para autocompletado.<br>Cachear localmente.|
|2|GET /api/v1/vuelos|Frontend|Buscar vuelos con origen, destino y fecha.<br>Filtrar PROGRAMADO.|
|3|GET /api/v1/vuelos/{id}/escalas|Frontend|Mostrar escalas al usuario.|
|4|GET /api/v1/vuelos/{id}/asientos|Frontend|Mostrar mapa de asientos (disponible=true).|
|5|(Frontend)|Frontend|Usuario llena datos de pasajeros. Se guardan<br>en sessionStorage.|
|6|(Frontend)|Frontend|Pantalla de pago: LOGIN o REGISTRO<br>(/auth/register-cliente o /auth/login).|
|7|POST /api/v1/pasajeros x N|Backend|Registrar cada pasajero (uno por persona que<br>viaja).|
|8|POST /api/v1/reservas|Backend|Crear reserva con array de pasajeros. Estado:<br>PEN.|
|9|PATCH /reservas/{id}/pagar|Cliente|Boton "Pagar". Envia cargo_servicio y<br>equipaje. El backend crea la factura, confirma<br>la reserva, emite boletos y registra equipaje<br>en una sola transaccion atomica.|
|10|(Automatico)|Sistema|(1) Factura creada->APR, (2) Reserva->CON,<br>(3) Asientos bloqueados, (4) Boletos emitidos,<br>(5) Equipaje registrado, (6) Reserva->EMI.|





Pagina 32 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

## **11. Reglas de Negocio**


**11.1 Auth**

|1|El username se guarda exactamente como el usuario lo ingresa (no se autogenera).|
|---|---|
|**2**|La validacion de complejidad de contrasena (mayuscula, numero, caracter especial, minimo 8 chars) se<br>realiza en el frontend.|
|**3**|El token JWT expira en 24 horas.|
|**4**|Al hacer logout el token se invalida inmediatamente (blacklist).|
|**5**|Al registrarse con /auth/register-cliente el rol CLIENTE se asigna automaticamente.|
|**6**|Si el correo o numero de identificacion ya existe retorna 409 Conflict.|



**11.2 Paises y Ciudades**

|1|El codigo ISO2 debe ser unico y exactamente 2 caracteres mayusculas.|
|---|---|
|**2**|El codigo ISO3 debe ser unico y exactamente 3 caracteres mayusculas.|
|**3**|No se pueden eliminar fisicamente si tienen registros dependientes.|
|**4**|Nombre de pais debe ser unico en el sistema.|
|**5**|Nombre de ciudad debe ser unico por pais.|



**11.3 Aeropuertos**

|1|El codigo IATA debe ser unico, exactamente 3 letras mayusculas.|
|---|---|
|**2**|El codigo ICAO debe ser unico, exactamente 4 letras mayusculas.|
|**3**|No se puede eliminar un aeropuerto si tiene vuelos activos asociados.|
|**4**|Un aeropuerto debe pertenecer a un pais existente.|



**11.4 Vuelos**

|1|fecha_hora_llegada se calcula automaticamente: fecha_hora_salida + duracion_min.|
|---|---|
|**2**|Si se modifica fecha_hora_salida o duracion_min, el backend recalcula fecha_hora_llegada.|
|**3**|El aeropuerto de origen debe ser distinto al de destino.|
|**4**|fecha_hora_salida debe ser una fecha futura al momento de crear el vuelo.|
|**5**|precio_base debe ser mayor a 0.|
|**6**|capacidad_total debe ser mayor a 0.|
|**7**|duracion_min debe ser mayor a 0.|
|**8**|No se puede crear un vuelo con el mismo numero_vuelo en la misma fecha y hora.|
|**9**|No se puede eliminar un vuelo con reservas en estado PEN o CON.|



Pagina 33 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

|10|Al cancelar un vuelo (CANCELADO), todas las reservas asociadas pasan automaticamente a CAN y los<br>asientos se liberan.|
|---|---|
|**11**|No se puede modificar fecha_hora_salida cuando el vuelo esta en estado EN_VUELO.|



**11.5 Escalas**

|1|El aeropuerto de escala debe ser distinto al origen y destino del vuelo.|
|---|---|
|**2**|fecha_hora_llegada_escala debe ser posterior a fecha_hora_salida del vuelo.|
|**3**|fecha_hora_salida_escala debe ser posterior a fecha_hora_llegada_escala.|
|**4**|fecha_hora_salida_escala debe ser anterior a fecha_hora_llegada del vuelo.|
|**5**|El campo orden no puede repetirse en el mismo vuelo.|
|**6**|duracion_min de la escala se calcula automaticamente: fecha_hora_salida_escala -<br>fecha_hora_llegada_escala.|
|**7**|No se pueden agregar escalas a un vuelo en estado EN_VUELO, ATERRIZADO o CANCELADO.|



**11.6 Asientos**

|1|numero_asiento debe ser unico por vuelo.|
|---|---|
|**2**|precio_extra debe ser mayor o igual a 0.|
|**3**|No se pueden crear mas asientos que la capacidad_total del vuelo.|
|**4**|No se puede eliminar un asiento con reserva activa (PEN o CON).|
|**5**|Al cancelarse una reserva, todos los asientos del detalle vuelven a disponible=true.|
|**6**|Al confirmarse una reserva (CON), todos los asientos del detalle pasan a disponible=false.|



**11.7 Clientes**

|1|correo debe ser unico en el sistema y tener formato valido (contener @ y dominio).|
|---|---|
|**2**|numero_identificacion debe ser unico en el sistema.|
|**3**|tipo_identificacion acepta: CEDULA, PASAPORTE, RUC, OTRO. TARJETA_IDENTIDAD fue eliminado.|
|**4**|Si tipo_identificacion = RUC entonces razon_social es obligatoria.|
|**5**|fecha_nacimiento debe ser una fecha pasada.|
|**6**|No se puede eliminar un cliente con reservas activas.|
|**7**|Al inhabilitar un cliente (soft delete), su usuario asociado tambien se inhabilita.|



**11.8 Pasajeros**

|1|Los datos del pasajero se guardan en el frontend (sessionStorage) durante el checkout.|
|---|---|
|**2**|El POST /pasajeros se ejecuta despues del login, antes de crear la reserva.|
|**3**|Categoria se determina automaticamente por edad: Bebe (0-1 ano), Nino (2-11 anos), Adulto (12+ anos).|



Pagina 34 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026


**4** Un pasajero no puede tener dos reservas en el mismo vuelo.


**11.9 Reservas**

|1|codigo_reserva se genera automaticamente en formato VU-YYYY-XXXXXX (XXXXXX = hex aleatorio).|
|---|---|
|**2**|Estado inicial siempre es PEN.|
|**3**|Estados validos: PEN, CON, CAN, FIN, EMI. El estado EXP no existe en este sistema.|
|**4**|El backend valida los totales con IVA del 15%:|
|**5**|subtotalLinea = precio_base + precio_extra_asiento|
|**6**|valorIvaLinea = subtotalLinea * 0.15|
|**7**|totalLinea = subtotalLinea + valorIvaLinea|
|**8**|subtotalReserva = suma de todos los subtotalLinea|
|**9**|valorIva = subtotalReserva * 0.15|
|**10**|totalReserva = subtotalReserva + valorIva|
|**11**|Si los totales no coinciden con el calculo del backend (tolerancia +/-0.01) retorna 400.|
|**12**|No puede haber dos reservas para el mismo asiento en el mismo vuelo.|
|**13**|No puede haber dos reservas para el mismo pasajero en el mismo vuelo.|
|**14**|No se puede reservar un asiento con disponible=false.|
|**15**|No se puede reservar en un vuelo con estado distinto a PROGRAMADO.|
|**16**|Transiciones validas: PEN->CAN, PEN->CON (sistema), CON->CAN, CON->EMI (sistema), EMI->FIN<br>(sistema).|
|**17**|El CLIENTE solo puede cambiar el estado a CAN (cancelar su propia reserva).|



**11.10 Facturas**

|1|numero_factura se genera automaticamente en formato FA-YYYY-XXXXXX.|
|---|---|
|**2**|Solo puede existir una factura por reserva.|
|**3**|La factura se crea automaticamente al ejecutar PATCH /reservas/{id}/pagar. No requiere estado previo CON<br>— el endpoint /pagar se encarga de toda la transicion.|
|**4**|IVA siempre es del 15%. valor_iva = subtotal * 0.15.|
|**5**|total = subtotal + valor_iva + cargo_servicio. Siempre mayor o igual a subtotal.|
|**6**|Estado inicial: ABI. Flujo: ABI -> APR (tras pago) -> INA (anulada).|
|**7**|Al ejecutar PATCH /reservas/{id}/pagar: (1) Crea factura->APR, (2) Reserva->CON, (3) Asientos-<br>>disponible=false, (4) Boletos emitidos por cada detalle, (5) Equipaje registrado por cada boleto, (6)<br>Reserva->EMI. Todo en una sola transaccion atomica con rollback si falla.|
|**8**|No se puede anular una factura con boletos activos.|
|**9**|Al anular una factura, la reserva pasa a CAN y los asientos se liberan.|



**11.11 Boletos**


Pagina 35 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

|1|codigo_boleto se genera automaticamente en formato BO-YYYY-XXXXXX.|
|---|---|
|**2**|Se emite UN boleto por cada id_detalle (por cada pasajero en la reserva).|
|**3**|No se puede emitir un boleto si la factura no esta en estado APR.|
|**4**|precio_final = precio_vuelo_base + precio_asiento_extra + impuestos_boleto + cargo_equipaje.|
|**5**|impuestos_boleto = precio_vuelo_base * 0.15.|
|**6**|Estado inicial: ACTIVO. Un boleto no se elimina, solo se cancela.|
|**7**|Al cancelarse un boleto, el asiento vuelve a disponible=true.|
|**8**|La reserva pasa a EMI automaticamente cuando TODOS los detalles tienen boleto emitido.|



**11.12 Equipaje**

|1|numero_etiqueta se genera automaticamente en formato EQ-XXXXXXXXXX.|
|---|---|
|**2**|Equipaje de MANO: incluido en el boleto, sin costo (precio_extra = $0.00). Peso maximo: 10 kg.|
|**3**|Equipaje de BODEGA: costo fijo de $45.00 por maleta. Peso maximo: 23 kg.|
|**4**|El precio_extra lo asigna el backend automaticamente segun el tipo (MANO=0, BODEGA=45).|
|**5**|El cargo de $45 se suma al cargo_equipaje del boleto y se recalcula el precio_final.|
|**6**|El equipaje solo se registra durante el checkout (antes de pagar). Post-venta es presencial.|
|**7**|peso_kg debe ser mayor a 0.|
|**8**|Estado inicial: REGISTRADO.|
|**9**|Transiciones validas: REGISTRADO->EMBARCADO->EN_TRANSITO->ENTREGADO.|
|**10**|Excepciones desde cualquier estado: CANCELADO, PERDIDO, DANADO.|
|**11**|No se puede registrar equipaje en un boleto cancelado.|



**11.13 Auditoria**

|1|Todo INSERT, UPDATE y DELETE genera automaticamente un registro en crm.AUDITORIA_LOG.|
|---|---|
|**2**|Se registra: tabla afectada, operacion, datos anteriores (JSON), datos nuevos (JSON), usuario ejecutor, IP<br>origen y fecha UTC.|
|**3**|Los registros de auditoria NUNCA se modifican ni eliminan manualmente.|


## **13. Flujo Detallado del Cliente — Paso a Paso**


Este flujo describe exactamente como el sistema debe comportarse desde la perspectiva del CLIENTE, usando
los endpoints del contrato. Los roles AEROLINEA y ADMINISTRADOR tienen acceso directo a los endpoints sin
pasar por el flujo visual de checkout.


**FASE 1 — Busqueda de Vuelos (sin login)**


Pagina 36 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026





**PASO 1 — Carga del buscador**
Al cargar la pagina de Booking el frontend llama en background:

```
 GET /api/v1/aeropuertos?estado=ACTIVO

```

Se cachea localmente para el autocompletado de origen y destino. El usuario nunca ve esta llamada.

**PASO 2 — Busqueda de vuelos**
El usuario ingresa origen, destino, fecha y cantidad de pasajeros. El frontend resuelve los codigos IATA a IDs
internos desde el cache y llama:





El usuario ve la lista de vuelos disponibles con precio, duracion y numero de escalas.

**PASO 3 — Detalle del vuelo seleccionado**
El usuario hace clic en un vuelo para ver el itinerario completo:

```
 GET /api/v1/vuelos/{id_vuelo}/escalas

```

Si el array viene vacio el vuelo es directo. Si tiene escalas se muestran los aeropuertos de transito y tiempos de
espera.

**PASO 4 — Seleccion de asientos**
El usuario ve el mapa de asientos del avion:

```
 GET /api/v1/vuelos/{id_vuelo}/asientos?disponible=true

```

Por cada pasajero el usuario selecciona su asiento. El frontend calcula en tiempo real:





Todo se guarda en sessionStorage. Aun no hay llamadas al backend.


**FASE 2 — Datos de Pasajeros y Equipaje (sin login)**





**PASO 5 — Formulario de pasajeros**
Por cada pasajero el usuario llena:

|Campo|Obligatorio|Nota|
|---|---|---|
|nombre_pasajero|Si||



Pagina 37 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026

|Campo|Obligatorio|Nota|
|---|---|---|
|apellido_pasajero|Si||
|tipo_documento_pasajero|Si|CEDULA, PASAPORTE, RUC, OTRO|
|numero_documento_pasajero|Si||
|fecha_nacimiento_pasajero|Si|Determina categoria: Bebe (0-1), Nino (2-11), Adulto (12+)|
|id_pais_nacionalidad|No|ID del pais del catalogo|
|requiere_asistencia|No|Default: false|



Todo se guarda en sessionStorage. Aun no hay llamadas al backend.

**PASO 6 — Seleccion de equipaje**
Por cada pasajero se muestra:






|Tipo|Incluido|Costo|Limite|Accion|
|---|---|---|---|---|
|MANO|Si|$0.00|Max 10 kg|Siempre incluido. El frontend valida que no<br>supere 10 kg.|
|BODEGA|No|$45.00 por<br>maleta|Max 23 kg|Opcional. El usuario puede agregar una o<br>mas maletas.|



El frontend actualiza el total en tiempo real sumando $45 por cada maleta de bodega seleccionada. Todo se
guarda en sessionStorage.

**PASO 7 — Resumen antes de pagar**
El usuario ve el resumen completo con todos los valores calculados por el frontend antes de proceder al pago:





**FASE 3 — Login o Registro**





**PASO 8 — Autenticacion**
Opcion A — El usuario ya tiene cuenta:





Pagina 38 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026





Opcion B — El usuario no tiene cuenta (registro en el momento):





**FASE 4 — Creacion de Pasajeros y Reserva (con JWT)**





**PASO 9 — Registro de pasajeros**
Una llamada por cada pasajero, en orden. El frontend guarda cada id_pasajero recibido:





**PASO 10 — Creacion de la reserva**
Una sola llamada con todos los pasajeros y asientos:


Pagina 39 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026





**FASE 5 — Pago (boton "Pagar")**





**PASO 11 — Ejecutar el pago**
El usuario hace clic en el boton "Pagar". El frontend envia el cargo de servicio y el equipaje seleccionado en
sessionStorage:





**El backend ejecuta en orden atomico:**







|Paso<br>interno|Accion|Resultado|
|---|---|---|
|1|Crea la factura|FA-2026-XXXXXX estado: APR|
|2|Reserva -> CON|Pago confirmado|
|3|Asientos -> disponible=false|A10 y A11 bloqueados|
|4|Emite boleto pasajero 1|BO-2026-F8147F idDetalle: 501|
|5|Emite boleto pasajero 2|BO-2026-A3291C idDetalle: 502|


Pagina 40 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026











|Paso<br>interno|Accion|Resultado|
|---|---|---|
|6|Registra equipaje bodega (pasajero<br>1)|EQ-3829471034 $45.00|
|7|Registra equipaje mano (pasajero 1)|EQ-1827364512 $0.00|
|8|Registra equipaje mano (pasajero 2)|EQ-9182736451 $0.00|
|9|Reserva -> EMI|Todos los boletos emitidos|


**Response del backend:**





**FASE 6 — Confirmacion y Consulta Post-Pago**


**PASO 12 — Pantalla de confirmacion**
El frontend muestra al usuario los codigos de su compra y puede consultarlos cuando quiera:












|Que consultar|Endpoint|Auth|
|---|---|---|
|Ver detalle completo de la reserva|GET<br>/api/v1/cliente/reservas/{id_reserva}/detalle|CLIENTE|
|Buscar reserva por codigo|GET /api/v1/cliente/reservas/by-<br>codigo/VU-2026-0CA0A5|CLIENTE|
|Ver sus boletos|GET /api/v1/cliente/boletos|CLIENTE|
|Ver sus facturas|GET /api/v1/cliente/facturas|CLIENTE|
|Cancelar su reserva|PATCH<br>/api/v1/reservas/{id_reserva}/estado  {<br>estado_reserva: CAN }|CLIENTE|





Pagina 41 de 42


API de Vuelos — Booking Simulator | v3.0 | Abril 2026


**Resultado Final de la Compra**

|Entidad|Codigo|Estado|Monto|
|---|---|---|---|
|Reserva|VU-2026-0CA0A5|EMI|-|
|Factura|FA-2026-D4243C|APR|$462.00|
|Boleto Pasajero 1|BO-2026-F8147F|ACTIVO|$263.50|
|Boleto Pasajero 2|BO-2026-A3291C|ACTIVO|$218.50|
|Equipaje BODEGA (Pas. 1)|EQ-3829471034|REGISTRADO|$45.00|
|Equipaje MANO (Pas. 1)|EQ-1827364512|REGISTRADO|$0.00|
|Equipaje MANO (Pas. 2)|EQ-9182736451|REGISTRADO|$0.00|



Pagina 42 de 42


