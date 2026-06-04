# Respuesta real del MS Seguridad para integracion con el Bus

Este documento reemplaza la guia anterior y responde directamente la informacion solicitada para integrar el Bus con el microservicio real de Seguridad. La informacion fue validada contra el codigo actual del proyecto.

## Resumen ejecutivo

El MS Seguridad expone una API REST versionada con base `/api/v1`. Actualmente administra autenticacion, usuarios de aplicacion, roles y asignaciones usuario-rol. No se encontro gRPC habilitado ni endpoints internos especificos para el bus.

Tablas propias configuradas en EF Core:

- `seg.usuario_app`
- `seg.rol`
- `seg.usuarios_roles`

Roles mencionados por codigo:

- `ADMINISTRADOR`
- `CLIENTE`

Roles indicados como conocidos por documentacion previa, pero sin semilla encontrada en el codigo:

- `AEROLINEA`
- `BOOKING`

Importante: en el codigo actual no hay migraciones ni `HasData` para sembrar roles. El rol `CLIENTE` se busca por nombre durante `register-cliente`, por lo que debe existir previamente en la base de datos.

## 1. DTOs exactos de request/response

### `POST /api/v1/auth/login`

Controlador real: `AuthController`.

Request real: `LoginRequest`.

```json
{
  "username": "string",
  "password": "string"
}
```

Campos:

- `username`: string requerido. Maximo 50 caracteres segun validador.
- `password`: string requerido. Maximo 100 caracteres segun validador.

No recibe campos adicionales.

Response real de negocio: `LoginResponse`.

```json
{
  "token": "string",
  "usuario": "string",
  "expiracion": "2026-05-13T00:00:00Z",
  "roles": ["CLIENTE"]
}
```

Campos reales:

- `token`: JWT generado.
- `usuario`: username del usuario autenticado.
- `expiracion`: fecha UTC calculada con `DateTime.UtcNow.AddMinutes(JwtSettings:ExpirationMinutes)`.
- `roles`: lista de strings con los roles activos del usuario.

No devuelve:

- `id_usuario`
- `id_cliente`
- `refresh_token`

La respuesta HTTP viene envuelta en `ApiResponse<LoginResponse>`:

```json
{
  "success": true,
  "message": "Login exitoso.",
  "data": {
    "token": "string",
    "usuario": "string",
    "expiracion": "2026-05-13T00:00:00Z",
    "roles": ["CLIENTE"]
  },
  "errors": []
}
```

Nota de serializacion: los request DTOs usan `[JsonPropertyName]` en snake_case cuando aplica. Las respuestas no tienen `[JsonPropertyName]`; con `AddControllers()` por defecto en ASP.NET Core se serializan en camelCase.

### `POST /api/v1/auth/register-cliente`

Controlador real: `AuthController`.

Request real: `RegisterClienteRequest`.

```json
{
  "tipo_identificacion": "string",
  "numero_identificacion": "string",
  "nombres": "string",
  "apellidos": "string",
  "razon_social": "string",
  "correo": "string",
  "telefono": "string",
  "direccion": "string",
  "id_ciudad_residencia": 1,
  "id_pais_nacionalidad": 1,
  "fecha_nacimiento": "1990-01-01T00:00:00",
  "genero": "string",
  "username": "string",
  "password": "string"
}
```

Campos obligatorios por tipo C#:

- `tipo_identificacion`
- `numero_identificacion`
- `nombres`
- `correo`
- `telefono`
- `direccion`
- `username`
- `password`
- `id_ciudad_residencia`
- `id_pais_nacionalidad`

Campos nullable:

- `apellidos`
- `razon_social`
- `fecha_nacimiento`
- `genero`

Validacion real aplicada actualmente:

- `username` obligatorio.
- `password` obligatorio y minimo 8 caracteres.
- `correo` obligatorio.

Aunque el DTO recibe datos de cliente como nombres, documento, telefono, direccion, ciudad, pais, fecha y genero, el servicio actual no los usa para crear un cliente de negocio. Solo crea el usuario de aplicacion y asigna el rol `CLIENTE`.

Response real de negocio: `RegisterClienteResponse`.

```json
{
  "idCliente": 0,
  "idUsuario": 123,
  "username": "string",
  "rolAsignado": "CLIENTE"
}
```

Campos reales:

- `idCliente`: siempre `0` en el flujo actual.
- `idUsuario`: id generado del usuario en Seguridad.
- `username`: username creado.
- `rolAsignado`: siempre `CLIENTE` si el registro fue exitoso.

La respuesta HTTP viene envuelta en `ApiResponse<RegisterClienteResponse>`:

```json
{
  "success": true,
  "message": "Cuenta de cliente creada correctamente.",
  "data": {
    "idCliente": 0,
    "idUsuario": 123,
    "username": "string",
    "rolAsignado": "CLIENTE"
  },
  "errors": []
}
```

Observacion importante para el Bus:

- El comentario del codigo dice que MS Clientes ya creo el cliente y llama a Seguridad, pero el request no tiene `id_cliente`.
- En la implementacion actual, `IdCliente` se envia como `null` al crear `UsuarioApp`.
- Por eso el JWT de un usuario registrado por este endpoint no tendra claim `id_cliente` hasta que exista un mecanismo para guardar ese valor en `seg.usuario_app.id_cliente`.

## 2. Contrato del JWT

Configuracion real en `appsettings.json`:

```json
{
  "JwtSettings": {
    "Issuer": "SistemaVuelos",
    "Audience": "SistemaVuelosClientes",
    "ExpirationMinutes": 60
  }
}
```

Valores exactos:

- `JwtSettings:Issuer`: `SistemaVuelos`
- `JwtSettings:Audience`: `SistemaVuelosClientes`
- `JwtSettings:ExpirationMinutes`: `60`
- `JwtSettings:Secret`: existe en `appsettings.json`, pero no debe copiarse a documentos compartidos.

Firma:

- Algoritmo: HMAC SHA-256.
- Implementacion: `SecurityAlgorithms.HmacSha256`.
- Llave: `SymmetricSecurityKey` creada desde `JwtSettings:Secret`.

Claims emitidos:

```json
{
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "username",
  "username": "username",
  "id_cliente": "123",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "CLIENTE"
}
```

Detalle real:

- `ClaimTypes.Name`: username del usuario.
- `username`: username del usuario.
- `ClaimTypes.Role`: un claim por cada rol distinto del usuario.
- `id_cliente`: solo se agrega si `UsuarioApp.IdCliente` tiene valor.

Nombre real del claim de rol:

- No se llama `role`.
- No se llama `roles`.
- Se usa `ClaimTypes.Role`, cuyo valor es `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`.

Tipo real de `id_cliente` dentro del token:

- En la entidad es `int?`.
- En el JWT se serializa como string porque se crea con `idCliente.Value.ToString()`.
- Los consumidores deben parsearlo como entero cuando necesiten compararlo.

Configuracion de validacion JWT:

- `ValidateIssuerSigningKey = true`
- `ValidateIssuer = true`
- `ValidateAudience = true`
- `ValidateLifetime = true`
- `ClockSkew = TimeSpan.Zero`
- `RoleClaimType = ClaimTypes.Role`
- `NameClaimType = ClaimTypes.Name`
- `MapInboundClaims = false`

## 3. Endpoint interno para el Bus

No existe actualmente un endpoint separado tipo:

- `POST /api/v1/internal/users/create-for-client`
- `POST /api/v1/internal/usuarios/create-for-client`
- `POST /api/v1/internal/token/validate`
- `POST /api/v1/auth/validate`
- `POST /api/v1/auth/introspect`

El endpoint disponible para crear usuario cliente es:

- `POST /api/v1/auth/register-cliente`

Problema actual para integracion:

- `register-cliente` no recibe `id_cliente`.
- Crea `UsuarioApp` con `IdCliente = null`.
- Devuelve `idCliente = 0`.
- Por lo tanto, no deja lista la relacion logica entre Seguridad y el cliente creado en MS Clientes.

Recomendacion para encajar con el Bus:

- Crear un endpoint interno nuevo para el Bus o MS Clientes, por ejemplo `POST /api/v1/internal/users/create-for-client`.
- Ese endpoint deberia recibir al menos `id_cliente`, `username`, `correo` y `password`.
- Debe guardar `UsuarioApp.IdCliente` con el id real del cliente.
- Debe asignar el rol `CLIENTE`.
- Debe devolver `idUsuario`, `usuarioGuid`, `idCliente`, `username` y `rolAsignado`.

Validacion de token:

- No hay endpoint publico/interno para validar un token.
- La validacion se hace por middleware JWT en los endpoints protegidos.
- La blacklist se consulta dentro del evento `OnTokenValidated`.
- El servicio de blacklist es `TokenBlacklistService` en memoria y registrado como singleton.

## 4. Entidad `UsuarioAppEntity`

Entidad real:

```csharp
public class UsuarioAppEntity
{
    public int IdUsuario { get; set; }
    public Guid UsuarioGuid { get; set; }
    public int? IdCliente { get; set; }
    public string Username { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string PasswordSalt { get; set; } = null!;
    public DateTime? FechaUltimoLogin { get; set; }
    public string EstadoUsuario { get; set; } = null!;
    public bool EsEliminado { get; set; }
    public bool Activo { get; set; }
    public string CreadoPorUsuario { get; set; } = null!;
    public DateTime FechaRegistroUtc { get; set; }
    public string? ModificadoPorUsuario { get; set; }
    public DateTime? FechaModificacionUtc { get; set; }
    public string? ModificacionIp { get; set; }
}
```

Campos y tipos relevantes:

- `IdUsuario`: `int`.
- `UsuarioGuid`: `Guid`.
- `IdCliente`: `int?`, referencia logica al MS Clientes, sin FK fisica.
- `Username`: `string`, maximo 50, requerido, unico.
- `Correo`: `string`, maximo 120, requerido, unico.
- `PasswordHash`: `string`, maximo 500, requerido.
- `PasswordSalt`: `string`, maximo 250, requerido.
- `FechaUltimoLogin`: `DateTime?`.
- `EstadoUsuario`: `string`, columna `char(3)`, default `ACT`.
- `EsEliminado`: `bool`, default `false`.
- `Activo`: `bool`, default `true`.
- `CreadoPorUsuario`: `string`, maximo 100, default `SYSTEM`.
- `FechaRegistroUtc`: `DateTime`, default `NOW() AT TIME ZONE 'UTC'`.
- `ModificadoPorUsuario`: `string?`, maximo 100.
- `FechaModificacionUtc`: `DateTime?`.
- `ModificacionIp`: `string?`, maximo 45.

Indices reales:

- `uq_usuario_app_guid`: unico sobre `usuario_guid`.
- `uq_usuario_app_username`: unico sobre `username`.
- `uq_usuario_app_correo`: unico sobre `correo`.
- `ix_usuario_app_id_cliente`: indice no unico sobre `id_cliente`.

Respuesta para la pregunta de tipo:

- `IdCliente` no es `long`.
- `IdCliente` es `int?`.

## 5. Configuracion de red

Configuracion real en `launchSettings.json`:

```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5062"
    },
    "https": {
      "applicationUrl": "https://localhost:7195;http://localhost:5062"
    },
    "IIS Express": {
      "applicationUrl": "http://localhost:59777/",
      "sslPort": 44375
    }
  }
}
```

URLs de desarrollo:

- Perfil HTTP: `http://localhost:5062`
- Perfil HTTPS: `https://localhost:7195`
- IIS Express HTTP: `http://localhost:59777/`
- IIS Express SSL: `https://localhost:44375`

Para un `SeguridadClient.cs` del Bus en desarrollo, la URL mas directa segun el perfil HTTP es:

```csharp
http://localhost:5062
```

gRPC:

- No hay gRPC habilitado actualmente.
- No se encontraron paquetes `Grpc.AspNetCore`.
- No se encontraron `.proto`.
- No se encontro `MapGrpcService`.
- La integracion disponible hoy es REST.

## 6. Endpoints REST conocidos reales

Base versionada:

```text
/api/v1
```

Auth:

- `POST /api/v1/auth/login`: publico.
- `POST /api/v1/auth/register-cliente`: publico.
- `POST /api/v1/auth/logout`: requiere JWT.

Usuarios:

- `GET /api/v1/usuarios`: requiere rol `ADMINISTRADOR`.
- `GET /api/v1/usuarios/{id_usuario}`: requiere rol `ADMINISTRADOR` o `CLIENTE`.
- `POST /api/v1/usuarios`: requiere rol `ADMINISTRADOR`.
- `PUT /api/v1/usuarios/{id_usuario}`: requiere rol `ADMINISTRADOR` o `CLIENTE`.
- `DELETE /api/v1/usuarios/{id_usuario}`: requiere rol `ADMINISTRADOR`.

Roles:

- `GET /api/v1/roles`: requiere rol `ADMINISTRADOR`.
- `GET /api/v1/roles/{id_rol}`: requiere rol `ADMINISTRADOR`.
- `POST /api/v1/roles`: requiere rol `ADMINISTRADOR`.
- `PUT /api/v1/roles/{id_rol}`: requiere rol `ADMINISTRADOR`.
- `DELETE /api/v1/roles/{id_rol}`: requiere rol `ADMINISTRADOR`.

Usuario-roles:

- `GET /api/v1/usuarios/{id_usuario}/roles`: requiere rol `ADMINISTRADOR`.
- `POST /api/v1/usuarios/{id_usuario}/roles`: requiere rol `ADMINISTRADOR`.
- `DELETE /api/v1/usuarios/{id_usuario}/roles/{id_rol}`: requiere rol `ADMINISTRADOR`.

## 7. Contrato recomendado para el Bus

El contrato actual no encaja completamente con un alta coordinada de cliente porque falta `id_cliente` en `register-cliente`. Para evitar codigo que no encaje, el Bus deberia usar un endpoint interno nuevo con este request:

```json
{
  "id_cliente": 123,
  "username": "cliente01",
  "correo": "cliente01@mail.com",
  "password": "Password123"
}
```

Respuesta sugerida:

```json
{
  "success": true,
  "message": "Usuario de cliente creado correctamente.",
  "data": {
    "idUsuario": 10,
    "usuarioGuid": "00000000-0000-0000-0000-000000000000",
    "idCliente": 123,
    "username": "cliente01",
    "rolAsignado": "CLIENTE"
  },
  "errors": []
}
```

Hasta que ese endpoint exista, cualquier integracion que dependa de `id_cliente` en el JWT quedara incompleta.

## 8. Respuestas directas a las preguntas

### DTOs exactos

- Login recibe exactamente `username` y `password`.
- Login devuelve `token`, `usuario`, `expiracion`, `roles`, envuelto en `ApiResponse`.
- Login no devuelve `id_usuario` ni `id_cliente`.
- Register-cliente recibe datos personales/contacto mas `username` y `password`, pero actualmente solo usa `username`, `correo` y `password` para crear el usuario.
- Register-cliente no recibe `id_cliente`.
- Register-cliente devuelve `idCliente`, `idUsuario`, `username`, `rolAsignado`, envuelto en `ApiResponse`.

### JWT

- Issuer exacto: `SistemaVuelos`.
- Audience exacto: `SistemaVuelosClientes`.
- Claim de rol: `ClaimTypes.Role`, no `role` ni `roles`.
- Claim `id_cliente`: string dentro del JWT, originado desde `int? IdCliente`.

### Endpoint interno para el Bus

- No existe endpoint interno separado para crear usuario desde cliente.
- Solo existe `POST /api/v1/auth/register-cliente`.
- No existe endpoint para validar/introspectar token.

### Entidad UsuarioApp

- Tiene `UsuarioGuid`, `Correo`, `EstadoUsuario`, `Activo`, `EsEliminado`, auditoria y campos de password hash/salt.
- `IdCliente` es `int?`.
- No hay FK fisica hacia MS Clientes.

### Configuracion de red

- HTTP dev: `http://localhost:5062`.
- HTTPS dev: `https://localhost:7195`.
- IIS Express: `http://localhost:59777/` y SSL `44375`.
- Solo REST. No hay gRPC habilitado.
# Guia para integrar MS Seguridad con el Bus de Integracion

## 1. Contexto general

El sistema de vuelos se esta separando desde un monolito hacia varios microservicios. Cada microservicio debe tener su propio backend, su propia logica de negocio, sus propias capas y su propio contexto de base de datos. La regla principal es que ningun microservicio debe consultar directamente la base de datos de otro.

El microservicio actual corresponde a **MS Seguridad**. Su responsabilidad es autenticar usuarios, administrar usuarios de aplicacion, roles y asignaciones usuario-rol, y generar los JWT que usaran los demas microservicios. Los demas microservicios no deben generar tokens ni validar credenciales; solo deben validar tokens emitidos por Seguridad.

El Bus de Integracion debe coordinar procesos entre microservicios sin romper la autonomia de cada dominio. En especial, cuando Seguridad necesite relacionarse con Clientes, Auditoria u otro dominio, debe hacerlo mediante eventos, comandos, HTTP REST interno o gRPC, nunca mediante acceso directo a tablas ajenas.

## Respuesta actualizada para comenzar la integracion

Esta seccion responde directamente los 8 puntos pedidos para iniciar la integracion de MS Seguridad con el Bus de Integracion. La informacion marcada como **IMPLEMENTADO** existe en el codigo actual. La informacion marcada como **NO IMPLEMENTADO** no debe asumirse como disponible.

### 1. Configuracion de JWT

**IMPLEMENTADO**

- La generacion de JWT esta en `AuthService`.
- El algoritmo de firma es **HMAC SHA-256** (`SecurityAlgorithms.HmacSha256`).
- Se usa una **clave simetrica** tomada desde `JwtSettings:Secret`.
- La configuracion JWT se lee desde:
  - `JwtSettings:Secret`
  - `JwtSettings:Issuer`
  - `JwtSettings:Audience`
  - `JwtSettings:ExpirationMinutes`
- La validacion JWT esta configurada en `AuthenticationExtensions` con `AddJwtBearer`.
- Parametros de validacion usados:
  - `ValidateIssuerSigningKey = true`
  - `IssuerSigningKey = SymmetricSecurityKey`
  - `ValidateIssuer = true`
  - `ValidIssuer = JwtSettings:Issuer`
  - `ValidateAudience = true`
  - `ValidAudience = JwtSettings:Audience`
  - `ValidateLifetime = true`
  - `ClockSkew = TimeSpan.Zero`
  - `RoleClaimType = ClaimTypes.Role`
  - `NameClaimType = ClaimTypes.Name`
- Claims incluidos en el JWT:
  - `ClaimTypes.Name`: username del usuario.
  - `username`: username del usuario.
  - `ClaimTypes.Role`: se agrega un claim por cada rol activo del usuario.
  - `id_cliente`: solo se agrega si el usuario tiene `IdCliente` asignado.
- La expiracion del token se calcula con `DateTime.UtcNow.AddMinutes(JwtSettings:ExpirationMinutes)`.

**NO IMPLEMENTADO**

- No hay RSA, ECDSA ni JWKS.
- No hay firma asimetrica.
- No hay refresh tokens.
- No hay endpoint dedicado de introspeccion o validacion manual de token.
- No se debe compartir el valor real de `JwtSettings:Secret` en documentos, logs ni eventos.

### 2. API de Seguridad y endpoints

**IMPLEMENTADO**

La API expone endpoints versionados con base `/api/v1`. Tambien existe Swagger y un endpoint raiz que redirige a Swagger.

#### Endpoint raiz

- `GET /`
  - Redirige a `/swagger`.
  - No es endpoint de negocio.

#### Auth

Base: `/api/v1/auth`

- `POST /api/v1/auth/login`
  - Acceso: publico (`AllowAnonymous`).
  - Request:
    - `username`
    - `password`
  - Funcion:
    - Valida credenciales.
    - Verifica que el usuario este activo.
    - Genera JWT.
  - Response:
    - `token`
    - `usuario`
    - `expiracion`
    - `roles`

- `POST /api/v1/auth/register-cliente`
  - Acceso: publico actualmente (`AllowAnonymous`).
  - Request:
    - `tipo_identificacion`
    - `numero_identificacion`
    - `nombres`
    - `apellidos`
    - `razon_social`
    - `correo`
    - `telefono`
    - `direccion`
    - `id_ciudad_residencia`
    - `id_pais_nacionalidad`
    - `fecha_nacimiento`
    - `genero`
    - `username`
    - `password`
  - Funcion real actual:
    - Crea un usuario de aplicacion.
    - Busca el rol `CLIENTE`.
    - Asigna el rol `CLIENTE` al usuario.
    - No crea el cliente de negocio.
  - Observacion importante:
    - En el codigo actual `IdCliente` se guarda como `null` y la respuesta devuelve `IdCliente = 0`.
    - Para integracion final con MS Clientes, este flujo debe coordinarse por bus o por endpoint interno.
  - Response:
    - `idCliente`
    - `idUsuario`
    - `username`
    - `rolAsignado`

- `POST /api/v1/auth/logout`
  - Acceso: requiere JWT (`Authorize`).
  - Request:
    - No recibe body.
    - Usa el header `Authorization: Bearer {token}`.
  - Funcion:
    - Obtiene el token actual.
    - Lo agrega a una blacklist en memoria hasta la expiracion del JWT.
  - Response:
    - Mensaje de sesion cerrada correctamente.

#### Usuarios

Base: `/api/v1/usuarios`

- `GET /api/v1/usuarios`
  - Acceso: solo `ADMINISTRADOR`.
  - Query params:
    - `username`
    - `correo`
    - `activo`
    - `page`
    - `page_size`
  - Funcion:
    - Lista usuarios paginados.

- `GET /api/v1/usuarios/{id_usuario}`
  - Acceso: `ADMINISTRADOR` o `CLIENTE`.
  - Funcion:
    - Obtiene un usuario por id.
    - Si el rol es `CLIENTE`, compara el claim `id_cliente` contra el `IdCliente` del usuario consultado.

- `POST /api/v1/usuarios`
  - Acceso: solo `ADMINISTRADOR`.
  - Request:
    - `id_cliente`
    - `username`
    - `correo`
    - `password`
  - Funcion:
    - Crea usuario.
    - Genera salt.
    - Hashea password.

- `PUT /api/v1/usuarios/{id_usuario}`
  - Acceso: `ADMINISTRADOR` o `CLIENTE`.
  - Request:
    - `correo`
    - `password` opcional.
  - Funcion:
    - Actualiza correo.
    - Si viene password, genera nuevo salt y nuevo hash.
    - Si el rol es `CLIENTE`, valida el claim `id_cliente`.

- `DELETE /api/v1/usuarios/{id_usuario}`
  - Acceso: solo `ADMINISTRADOR`.
  - Funcion:
    - Elimina logicamente o desactiva usuario segun la logica de datos.

#### Roles

Base: `/api/v1/roles`

Nota: `RolController` tiene `ApiExplorerSettings(IgnoreApi = true)`, por lo que puede no aparecer en Swagger aunque si existe en codigo.

- `GET /api/v1/roles`
  - Acceso: solo `ADMINISTRADOR`.
  - Query params:
    - `nombreRol`
    - `estadoRol`
    - `page`
    - `pageSize`
  - Funcion:
    - Lista roles paginados.

- `GET /api/v1/roles/{id_rol}`
  - Acceso: solo `ADMINISTRADOR`.
  - Funcion:
    - Obtiene rol por id.

- `POST /api/v1/roles`
  - Acceso: solo `ADMINISTRADOR`.
  - Request:
    - `nombreRol`
    - `descripcionRol`
  - Funcion:
    - Crea rol.

- `PUT /api/v1/roles/{id_rol}`
  - Acceso: solo `ADMINISTRADOR`.
  - Request:
    - `nombreRol`
    - `descripcionRol`
  - Funcion:
    - Actualiza rol.

- `DELETE /api/v1/roles/{id_rol}`
  - Acceso: solo `ADMINISTRADOR`.
  - Funcion:
    - Elimina logicamente o desactiva rol segun la logica de datos.

#### Asignacion de roles a usuarios

Base: `/api/v1/usuarios/{id_usuario}/roles`

- `GET /api/v1/usuarios/{id_usuario}/roles`
  - Acceso: solo `ADMINISTRADOR`.
  - Query params:
    - `idUsuario`
    - `idRol`
    - `page`
    - `pageSize`
  - Funcion:
    - Lista roles asignados al usuario.

- `POST /api/v1/usuarios/{id_usuario}/roles`
  - Acceso: solo `ADMINISTRADOR`.
  - Request:
    - `idUsuario`
    - `idRol`
  - Funcion:
    - Asigna un rol al usuario.
    - El `id_usuario` de la ruta se copia sobre `request.IdUsuario`.

- `DELETE /api/v1/usuarios/{id_usuario}/roles/{id_rol}`
  - Acceso: solo `ADMINISTRADOR`.
  - Funcion:
    - Revoca/elimina la asignacion de ese rol al usuario.

#### Endpoint de plantilla

- `GET /WeatherForecast`
  - Existe en codigo como controller de plantilla.
  - No es parte del dominio Seguridad.
  - Se recomienda eliminarlo u ocultarlo antes de exponer el servicio.

**NO IMPLEMENTADO**

- No existe endpoint especifico para permisos.
- No existe endpoint interno formal para comandos del bus.
- No existe endpoint para consultar permisos efectivos.
- No existe endpoint de refresh token.
- No existe endpoint de introspeccion/revocacion distribuida.

### 3. Base de datos y entidades

**IMPLEMENTADO**

El `DbContext` real es `SistemaVuelosSeguridadDBContext`. Solo contiene tablas propias del dominio Seguridad:

- `seg.usuario_app`
- `seg.rol`
- `seg.usuarios_roles`

Tabla `seg.usuario_app`:

- `id_usuario`
- `usuario_guid`
- `id_cliente`
- `username`
- `correo`
- `password_hash`
- `password_salt`
- `fecha_ultimo_login`
- `estado_usuario`
- `es_eliminado`
- `activo`
- `creado_por_usuario`
- `fecha_registro_utc`
- `modificado_por_usuario`
- `fecha_modificacion_utc`
- `modificacion_ip`

Tabla `seg.rol`:

- `id_rol`
- `rol_guid`
- `nombre_rol`
- `descripcion_rol`
- `estado_rol`
- `es_eliminado`
- `activo`
- `creado_por_usuario`
- `fecha_registro_utc`
- `modificado_por_usuario`
- `fecha_modificacion_utc`

Tabla `seg.usuarios_roles`:

- `id_usuario_rol`
- `id_usuario`
- `id_rol`
- `estado_usuario_rol`
- `es_eliminado`
- `activo`
- `creado_por_usuario`
- `fecha_registro_utc`
- `modificado_por_usuario`
- `fecha_modificacion_utc`

Relaciones:

- `seg.usuarios_roles.id_usuario` tiene FK interna hacia `seg.usuario_app.id_usuario`.
- `seg.usuarios_roles.id_rol` tiene FK interna hacia `seg.rol.id_rol`.
- `seg.usuario_app.id_cliente` es solo referencia logica hacia MS Clientes. No hay FK fisica ni navigation property.

**NO IMPLEMENTADO**

- No hay tabla de permisos.
- No hay tabla rol-permiso.
- No hay tabla de refresh tokens.
- No hay tabla de blacklist distribuida.
- No hay outbox/inbox en base de datos.

### 4. Eventos y comandos

**IMPLEMENTADO**

- En el codigo actual no hay publicadores ni consumidores del bus.
- La guia documenta contratos recomendados, pero no hay implementacion runtime.

**EVENTOS RECOMENDADOS, NO IMPLEMENTADOS AUN**

- `Security.UserCreated`
- `Security.UserUpdated`
- `Security.UserDeleted`
- `Security.RoleCreated`
- `Security.UserRoleAssigned`
- `Security.UserRoleRevoked`
- `Security.UserLoggedIn`
- `Security.UserLoginFailed`
- `Security.UserLoggedOut`

**COMANDOS RECOMENDADOS, NO IMPLEMENTADOS AUN**

- `Security.CreateUserForClient`
- `Security.LinkUserToClient`
- `Security.AssignRoleToUser`
- `Security.DisableUser`

Campos minimos recomendados para eventos:

- `eventId`
- `eventType`
- `occurredAtUtc`
- `correlationId`
- `source`
- `schemaVersion`
- `payload`

Campos minimos recomendados para comandos:

- `commandId`
- `commandType`
- `occurredAtUtc`
- `correlationId`
- `source`
- `schemaVersion`
- `payload`

**NO IMPLEMENTADO**

- No hay RabbitMQ, Kafka, MassTransit ni Azure Service Bus configurado.
- No hay Outbox.
- No hay Inbox.
- No hay idempotencia por `messageId` o `commandId`.
- No hay DLQ desde este microservicio.
- No hay reintentos del bus configurados en este proyecto.

### 5. Autorizacion y gestion de roles

**IMPLEMENTADO**

- Roles gestionados en tabla `seg.rol`.
- Asignaciones gestionadas en tabla `seg.usuarios_roles`.
- Roles usados en codigo:
  - `ADMINISTRADOR`
  - `CLIENTE`
- `ADMINISTRADOR` puede administrar usuarios, roles y asignaciones.
- `CLIENTE` puede consultar/actualizar usuario solo si pasa la validacion de `id_cliente`.
- La autorizacion se aplica con:
  - `[Authorize]`
  - `[Authorize(Roles = "ADMINISTRADOR")]`
  - `[Authorize(Roles = "ADMINISTRADOR,CLIENTE")]`

**NO IMPLEMENTADO**

- No hay permisos granulares.
- No hay policies complejas por recurso.
- No hay claims de permisos.
- No hay matriz rol-permiso persistida.

### 6. Patrones de comunicacion e integracion

**IMPLEMENTADO**

- MS Seguridad emite JWT.
- Los demas microservicios deben validar JWT localmente con `Issuer`, `Audience`, firma y expiracion.
- MS Seguridad no debe compartir base de datos con otros microservicios.
- `id_cliente` se maneja como referencia logica, no como FK.

**NO IMPLEMENTADO**

- No existe todavia integracion tecnica con bus.
- No hay sagas implementadas.
- No hay compensaciones automatizadas implementadas.
- No hay contratos internos reales para comandos del bus.

Patron recomendado para registrar cliente:

1. MS Clientes crea el cliente en su propia base.
2. Bus envia comando `Security.CreateUserForClient`.
3. MS Seguridad crea usuario, hashea password y asigna rol `CLIENTE`.
4. MS Seguridad publica `Security.UserCreated` y `Security.UserRoleAssigned`.
5. Si algo falla, el bus ejecuta compensacion.

### 7. Logs y trazabilidad

**IMPLEMENTADO**

- Hay logging por consola y debug.
- `ExceptionHandlingMiddleware` registra errores con `TraceId` (`context.TraceIdentifier`).
- Las respuestas de error pueden devolver `TraceId`.

**NO IMPLEMENTADO**

- No hay `correlationId` estandar recibido/propagado en headers.
- No hay middleware de correlation id.
- No hay trazabilidad distribuida entre microservicios.
- No hay auditoria de eventos publicada al bus.
- No hay registro estructurado de login/logout como eventos.

Recomendacion para bus:

- Todos los comandos y eventos deben llevar `correlationId`.
- No se deben loggear passwords.
- No se deben loggear tokens JWT completos.
- No se deben loggear secrets.

### 8. Otras configuraciones de seguridad

**IMPLEMENTADO**

- Las contrasenas no se guardan en texto plano.
- Al crear/actualizar usuario se genera salt aleatorio de 32 bytes.
- El hash se calcula con `SHA-256(password + salt)`.
- Se guarda:
  - `password_hash`
  - `password_salt`
- En login se recalcula el hash con el password ingresado y el salt guardado.
- La comparacion se hace con `CryptographicOperations.FixedTimeEquals`.
- Logout agrega el JWT actual a una blacklist en memoria.

**NO IMPLEMENTADO**

- No se usa bcrypt.
- No se usa PBKDF2.
- No se usa Argon2.
- No hay refresh tokens.
- No hay blacklist distribuida.
- No hay revocacion centralizada para multiples instancias.
- No hay rotacion de llaves JWT implementada.

Riesgo importante:

- La blacklist actual vive solo en memoria. Si el servicio se reinicia, se pierde. Si hay varias instancias, cada instancia tendria su propia blacklist.

## 2. Estado actual del MS Seguridad

El proyecto esta separado por capas:

- `Microservicio.Seguridad.Api`: controllers, middleware, configuracion JWT, Swagger, CORS y registro de dependencias.
- `Microservicio.Seguridad.Business`: servicios de negocio, DTOs, validadores, excepciones y mappers.
- `Microservicio.Seguridad.DataManagement`: servicios de datos, modelos intermedios y Unit of Work.
- `Microservicio.Seguridad.DataAccess`: DbContext, entidades, configuraciones y repositorios.

La API registra autenticacion JWT en `Program.cs` mediante `AddJwtAuthentication`, registra autorizacion con `AddAuthorization`, agrega un `TokenBlacklistService` singleton y expone controllers versionados.

El `DbContext` real es `SistemaVuelosSeguridadDBContext`. Este contexto solo tiene tres tablas propias del dominio Seguridad:

- `Roles`
- `Usuarios`
- `UsuariosRoles`

La entidad `UsuarioAppEntity` tiene un campo `IdCliente`, pero en el codigo esta tratado como **referencia logica al MS Clientes**, no como relacion de base de datos. Esto es correcto para microservicios: Seguridad puede guardar el identificador externo del cliente si lo necesita para claims o autorizacion, pero no debe tener navigation property ni foreign key directa hacia la base de Clientes.

## 3. Responsabilidad exacta de MS Seguridad

MS Seguridad debe ser el unico responsable de:

- Login con username/password.
- Generacion de JWT.
- Validacion de credenciales.
- Hash y salt de contrasenas.
- Administracion de usuarios de aplicacion.
- Administracion de roles.
- Asignacion y revocacion de roles a usuarios.
- Claims de seguridad incluidos en el token.
- Logout o invalidacion de tokens, si se mantiene esa capacidad.
- Exposicion de contratos internos para que otros servicios creen o sincronicen cuentas cuando el flujo de negocio lo requiera.

MS Seguridad no debe ser responsable de:

- Crear clientes de negocio.
- Crear pasajeros.
- Administrar paises, ciudades, aeropuertos, vuelos, reservas, facturas, boletos o equipaje.
- Consultar bases de datos de otros microservicios.
- Ejecutar transacciones distribuidas con acceso directo a varias bases.
- Tener reglas de negocio de dominios externos.

## 4. Lo que NO debe existir en otros microservicios

En Geografia, Aeropuertos, Vuelos, Clientes, Ventas-Facturacion y Auditoria no deberian existir:

- `AuthController`
- `LoginRequest`
- `LoginResponse`
- `TokenBlacklistService`
- `ITokenBlacklistService`
- Metodos para generar token
- `PasswordHasher`
- Validacion de username/password
- Tablas `ROL`, `USUARIO_APP`, `USUARIOS_ROLES`

Esos servicios deben tener solamente:

- Configuracion para validar JWT.
- `AddJwtBearer`.
- `JwtSettings` o configuracion equivalente de validacion.
- Uso de `[Authorize]`.
- Uso de `[Authorize(Roles = "...")]` cuando aplique.

## 5. Contratos HTTP actuales de Seguridad

### Auth

Base route:

`/api/v{version}/auth`

Endpoints:

- `POST /api/v1/auth/login`
  - Publico.
  - Recibe `username` y `password`.
  - Valida credenciales contra Seguridad.
  - Devuelve token, usuario, expiracion y roles.

- `POST /api/v1/auth/register-cliente`
  - Publico actualmente.
  - Crea un usuario de aplicacion y le asigna el rol `CLIENTE`.
  - El codigo actual indica que el cliente real lo crea MS Clientes.
  - Actualmente el servicio ignora los datos de cliente y retorna `IdCliente = 0`; esto deberia ajustarse cuando exista el bus.

- `POST /api/v1/auth/logout`
  - Requiere `[Authorize]`.
  - Agrega el token a una blacklist en memoria hasta la expiracion.

### Usuarios

Base route:

`/api/v1/usuarios`

Endpoints:

- `GET /api/v1/usuarios`
  - Solo `ADMINISTRADOR`.
  - Lista usuarios paginados.

- `GET /api/v1/usuarios/{id_usuario}`
  - `ADMINISTRADOR` o `CLIENTE`.
  - Si el rol es `CLIENTE`, valida el claim `id_cliente`.

- `POST /api/v1/usuarios`
  - Solo `ADMINISTRADOR`.
  - Crea usuario.

- `PUT /api/v1/usuarios/{id_usuario}`
  - `ADMINISTRADOR` o `CLIENTE`.
  - Actualiza usuario.

- `DELETE /api/v1/usuarios/{id_usuario}`
  - Solo `ADMINISTRADOR`.
  - Elimina logicamente usuario.

### Roles

Base route:

`/api/v1/roles`

Endpoints protegidos por `ADMINISTRADOR`:

- `GET /api/v1/roles`
- `GET /api/v1/roles/{id_rol}`
- `POST /api/v1/roles`
- `PUT /api/v1/roles/{id_rol}`
- `DELETE /api/v1/roles/{id_rol}`

El controller tiene `ApiExplorerSettings(IgnoreApi = true)`, por lo que podria no aparecer en Swagger.

### Usuario-Rol

Base route:

`/api/v1/usuarios/{id_usuario}/roles`

Endpoints protegidos por `ADMINISTRADOR`:

- `GET /api/v1/usuarios/{id_usuario}/roles`
- `POST /api/v1/usuarios/{id_usuario}/roles`
- `DELETE /api/v1/usuarios/{id_usuario}/roles/{id_rol}`

## 6. Contrato JWT actual

El token se genera en `AuthService`.

Configuracion requerida:

- `JwtSettings:Secret`
- `JwtSettings:Issuer`
- `JwtSettings:Audience`
- `JwtSettings:ExpirationMinutes`

Algoritmo actual:

- HMAC SHA-256 usando `JwtSettings:Secret`.

Claims actuales:

- `ClaimTypes.Name`: username.
- `username`: username.
- `ClaimTypes.Role`: uno por cada rol.
- `id_cliente`: solo si el usuario tiene `IdCliente`.

Los demas microservicios deben validar:

- Firma del token.
- Issuer.
- Audience.
- Expiracion.
- Roles.

Para integracion futura, se recomienda estandarizar los claims:

- `sub`: identificador estable del usuario, idealmente `UsuarioGuid` o `IdUsuario`.
- `username`: nombre de usuario.
- `roles`: roles o multiples role claims.
- `id_cliente`: solo cuando el usuario este asociado a un cliente.
- `jti`: identificador unico del token.
- `iat`: fecha de emision.

Tambien se recomienda evaluar pasar de secreto simetrico HMAC a firma asimetrica RSA/ECDSA con JWKS. Con HMAC todos los microservicios necesitan conocer el secreto, lo cual aumenta el riesgo. Con llaves asimetricas, Seguridad firma con llave privada y los demas servicios validan con llave publica.

## 7. Papel del Bus de Integracion

El bus no debe reemplazar la logica interna de Seguridad. El bus debe coordinar procesos entre dominios.

Responsabilidades recomendadas del bus:

- Publicar eventos de dominio entre microservicios.
- Recibir comandos de orquestacion cuando un proceso involucra mas de un microservicio.
- Garantizar idempotencia de mensajes.
- Manejar reintentos.
- Manejar dead-letter queue.
- Guardar correlacion entre pasos de una saga.
- Coordinar compensaciones cuando una parte del flujo falle.
- Evitar que un servicio consulte directamente la base de datos de otro.

El bus no deberia:

- Generar JWT.
- Validar passwords.
- Acceder a tablas de Seguridad.
- Contener reglas internas de usuarios, roles o hashing.
- Convertirse en una base de datos compartida.

## 8. Eventos recomendados emitidos por Seguridad

Estos eventos son utiles para que Auditoria, Clientes u otros microservicios reaccionen sin acoplarse a la base de Seguridad.

### `Security.UserCreated`

Cuando se crea un usuario.

Payload sugerido:

```json
{
  "eventId": "uuid",
  "occurredAtUtc": "2026-05-13T00:00:00Z",
  "source": "MS-Seguridad",
  "correlationId": "uuid",
  "userId": 123,
  "userGuid": "uuid",
  "username": "cliente01",
  "email": "cliente01@email.com",
  "idCliente": 456,
  "createdBy": "SYSTEM"
}
```

### `Security.UserUpdated`

Cuando se actualiza username, correo, estado, password o referencia de cliente.

Payload sugerido:

```json
{
  "eventId": "uuid",
  "occurredAtUtc": "2026-05-13T00:00:00Z",
  "source": "MS-Seguridad",
  "correlationId": "uuid",
  "userId": 123,
  "userGuid": "uuid",
  "username": "cliente01",
  "email": "cliente01@email.com",
  "idCliente": 456,
  "status": "ACT",
  "active": true,
  "modifiedBy": "ADMINISTRADOR"
}
```

### `Security.UserDeleted`

Cuando se elimina logicamente un usuario.

Payload sugerido:

```json
{
  "eventId": "uuid",
  "occurredAtUtc": "2026-05-13T00:00:00Z",
  "source": "MS-Seguridad",
  "correlationId": "uuid",
  "userId": 123,
  "userGuid": "uuid",
  "username": "cliente01",
  "deletedBy": "ADMINISTRADOR"
}
```

### `Security.RoleCreated`

Cuando se crea un rol.

Payload sugerido:

```json
{
  "eventId": "uuid",
  "occurredAtUtc": "2026-05-13T00:00:00Z",
  "source": "MS-Seguridad",
  "correlationId": "uuid",
  "roleId": 1,
  "roleGuid": "uuid",
  "roleName": "CLIENTE"
}
```

### `Security.UserRoleAssigned`

Cuando se asigna un rol a un usuario.

Payload sugerido:

```json
{
  "eventId": "uuid",
  "occurredAtUtc": "2026-05-13T00:00:00Z",
  "source": "MS-Seguridad",
  "correlationId": "uuid",
  "userId": 123,
  "roleId": 2,
  "roleName": "CLIENTE",
  "assignedBy": "ADMINISTRADOR"
}
```

### `Security.UserRoleRevoked`

Cuando se revoca un rol.

Payload sugerido:

```json
{
  "eventId": "uuid",
  "occurredAtUtc": "2026-05-13T00:00:00Z",
  "source": "MS-Seguridad",
  "correlationId": "uuid",
  "userId": 123,
  "roleId": 2,
  "roleName": "CLIENTE",
  "revokedBy": "ADMINISTRADOR"
}
```

### `Security.UserLoggedIn`

Evento opcional para Auditoria. No debe incluir password ni token completo.

Payload sugerido:

```json
{
  "eventId": "uuid",
  "occurredAtUtc": "2026-05-13T00:00:00Z",
  "source": "MS-Seguridad",
  "correlationId": "uuid",
  "username": "cliente01",
  "userId": 123,
  "success": true,
  "ip": "127.0.0.1",
  "userAgent": "browser"
}
```

### `Security.UserLoginFailed`

Evento opcional para Auditoria y seguridad.

Payload sugerido:

```json
{
  "eventId": "uuid",
  "occurredAtUtc": "2026-05-13T00:00:00Z",
  "source": "MS-Seguridad",
  "correlationId": "uuid",
  "username": "cliente01",
  "reason": "INVALID_CREDENTIALS",
  "ip": "127.0.0.1"
}
```

### `Security.UserLoggedOut`

Evento opcional para Auditoria.

Payload sugerido:

```json
{
  "eventId": "uuid",
  "occurredAtUtc": "2026-05-13T00:00:00Z",
  "source": "MS-Seguridad",
  "correlationId": "uuid",
  "username": "cliente01",
  "tokenId": "jti"
}
```

## 9. Comandos recomendados hacia Seguridad

Cuando otros microservicios necesiten que Seguridad haga algo, deben enviar comandos o llamar endpoints internos. El comando representa una intencion y Seguridad decide si la acepta.

### `Security.CreateUserForClient`

Usado por MS Clientes o por el bus durante registro de cliente.

Payload sugerido:

```json
{
  "commandId": "uuid",
  "correlationId": "uuid",
  "requestedBy": "MS-Clientes",
  "idCliente": 456,
  "username": "cliente01",
  "email": "cliente01@email.com",
  "password": "plain-text-only-in-command-if-absolutely-needed",
  "role": "CLIENTE"
}
```

Importante: si se envia password por el bus, el canal debe estar cifrado, autenticado y con trazabilidad. Idealmente, el password solo viaja una vez y nunca queda en logs.

### `Security.LinkUserToClient`

Usado cuando primero se crea el usuario y luego el cliente, o cuando Clientes confirma el `IdCliente`.

Payload sugerido:

```json
{
  "commandId": "uuid",
  "correlationId": "uuid",
  "userId": 123,
  "idCliente": 456
}
```

### `Security.AssignRoleToUser`

Usado para asignar roles de forma controlada.

Payload sugerido:

```json
{
  "commandId": "uuid",
  "correlationId": "uuid",
  "userId": 123,
  "roleName": "CLIENTE",
  "requestedBy": "MS-Seguridad"
}
```

### `Security.DisableUser`

Usado cuando un cliente se desactiva, se detecta fraude o un administrador bloquea una cuenta.

Payload sugerido:

```json
{
  "commandId": "uuid",
  "correlationId": "uuid",
  "userId": 123,
  "reason": "CLIENT_DISABLED",
  "requestedBy": "MS-Clientes"
}
```

## 10. Saga recomendada: registro de cliente con usuario

El flujo actual de `register-cliente` esta a medio camino: el DTO recibe datos de cliente, pero el servicio realmente solo crea usuario y rol. Para microservicios, conviene definir una saga clara.

### Opcion A: Clientes orquesta y Seguridad crea credenciales

1. Cliente envia solicitud de registro al MS Clientes.
2. MS Clientes valida datos de cliente, ciudad y pais mediante Geografia.
3. MS Clientes crea el cliente en su propia base.
4. MS Clientes envia comando `Security.CreateUserForClient` a Seguridad, con `idCliente`, username, correo y password.
5. Seguridad crea `UsuarioApp`, hashea password y asigna rol `CLIENTE`.
6. Seguridad publica `Security.UserCreated` y `Security.UserRoleAssigned`.
7. MS Clientes guarda la relacion logica `idUsuario` si su modelo la necesita.
8. Auditoria registra los eventos.

Compensacion:

- Si Seguridad falla despues de crear el cliente, MS Clientes puede marcar el cliente como `PENDIENTE_CUENTA` o iniciar compensacion para desactivar/eliminar logicamente el cliente.
- No se recomienda borrar fisicamente si ya hubo eventos publicados.

### Opcion B: Bus orquesta la saga completa

1. API Gateway o frontend envia solicitud al Bus/Orquestador.
2. Bus manda comando a MS Clientes para crear cliente.
3. MS Clientes responde/publica `Clients.ClientCreated`.
4. Bus manda comando `Security.CreateUserForClient`.
5. Seguridad publica `Security.UserCreated`.
6. Bus marca saga como completada.

Compensacion:

- Si Clientes crea el cliente pero Seguridad falla, el bus emite `Clients.DisableClient` o `Clients.MarkClientRegistrationFailed`.
- Si Seguridad crea usuario pero Clientes falla, el bus emite `Security.DisableUser`.

Recomendacion: para este sistema, la opcion B es mas alineada con la idea de un Bus de Integracion que maneje transaccionalidad distribuida.

## 11. Saga recomendada: login

El login no necesita bus para funcionar.

Flujo:

1. Frontend llama a `POST /api/v1/auth/login`.
2. Seguridad valida username/password.
3. Seguridad genera JWT.
4. Seguridad puede publicar `Security.UserLoggedIn` o `Security.UserLoginFailed` hacia Auditoria.
5. Frontend usa el JWT para llamar a otros microservicios.
6. Los otros microservicios solo validan el JWT.

No debe pasar:

- El bus no debe generar JWT.
- Clientes, Vuelos, Ventas o Geografia no deben validar password.
- Ningun servicio debe consultar `USUARIO_APP` para autenticar.

## 12. Saga recomendada: cambio de rol

1. Administrador llama a Seguridad para asignar o revocar rol.
2. Seguridad valida que usuario y rol existan y esten activos.
3. Seguridad guarda el cambio en `UsuariosRoles`.
4. Seguridad publica `Security.UserRoleAssigned` o `Security.UserRoleRevoked`.
5. Auditoria registra el cambio.
6. Otros servicios pueden actualizar caches de permisos si los tienen.

Nota: los JWT ya emitidos pueden quedar con roles antiguos hasta expirar. Si se requiere revocacion inmediata, se necesita una estrategia compartida: tokens cortos, refresh tokens, `jti` con blacklist distribuida, o introspeccion.

## 13. Transaccionalidad distribuida

Dentro de Seguridad ya existe `UnitOfWork` para transacciones locales con Entity Framework. Eso solo protege operaciones dentro de la base de Seguridad.

Para transacciones entre microservicios, no se debe intentar una transaccion SQL distribuida entre bases. El bus debe usar patrones de consistencia eventual:

- Saga orchestration.
- Outbox pattern.
- Inbox pattern.
- Idempotency keys.
- Reintentos con backoff.
- Dead-letter queue.
- Correlation ID por flujo.
- Compensaciones explicitas.

### Outbox en Seguridad

Cuando Seguridad cree usuario o asigne rol, deberia guardar en la misma transaccion:

1. Cambio en tablas de Seguridad.
2. Evento pendiente en tabla `OutboxMessages`.

Luego un publicador lee `OutboxMessages` y publica al bus. Asi se evita el problema de guardar en base pero fallar al publicar evento.

### Inbox en Seguridad

Cuando Seguridad consuma comandos desde el bus, deberia guardar `commandId` o `messageId` ya procesados. Si el bus reintenta el mismo comando, Seguridad debe responder de forma idempotente sin duplicar usuarios, roles o asignaciones.

## 14. Seguridad para comunicacion interna

El bus y los microservicios deben autenticarse entre si. No basta con confiar en que estan en la misma red.

Recomendaciones:

- Usar mTLS, client credentials o tokens internos de servicio a servicio.
- Incluir `correlationId` en todos los comandos y eventos.
- No loggear passwords, tokens JWT completos ni secretos.
- No enviar password en eventos. Si se requiere password para crear usuario, debe ir en comando privado y protegido, nunca en evento publico.
- Separar eventos publicos de comandos privados.
- Validar autorizacion interna: no cualquier servicio deberia poder mandar `DisableUser` o `AssignRoleToUser`.

## 15. Auditoria

MS Auditoria debe escuchar eventos, no consultar tablas de Seguridad.

Eventos minimos auditables:

- Login exitoso.
- Login fallido.
- Logout.
- Usuario creado.
- Usuario actualizado.
- Usuario eliminado.
- Rol creado.
- Rol actualizado.
- Rol eliminado.
- Rol asignado.
- Rol revocado.

Cada evento deberia incluir:

- `eventId`
- `correlationId`
- `causationId`
- `source`
- `occurredAtUtc`
- `actor`
- datos relevantes del recurso

## 16. Reglas para otros microservicios al validar JWT

Cada microservicio debe tener configurado `AddJwtBearer` con los mismos valores de validacion:

- `Issuer`
- `Audience`
- llave de firma
- `ValidateLifetime = true`
- `ValidateIssuerSigningKey = true`
- `RoleClaimType` compatible con los tokens de Seguridad

Los controllers de otros microservicios deben usar:

- `[Authorize]` para endpoints autenticados.
- `[Authorize(Roles = "ADMINISTRADOR")]` para endpoints solo admin.
- `[Authorize(Roles = "ADMINISTRADOR,CLIENTE")]` cuando ambos roles puedan acceder.

Ojo: en tu codigo actual el rol usado es `ADMINISTRADOR`, no `ADMIN`. Conviene estandarizar un solo nombre de rol en todos los microservicios.

## 17. Riesgos tecnicos actuales detectados

1. **Blacklist de tokens en memoria**

   `TokenBlacklistService` usa `ConcurrentDictionary` en memoria. Esto funciona en una sola instancia, pero falla si hay varias instancias de Seguridad, reinicios o despliegues. Para produccion conviene Redis, base de datos, introspeccion, refresh tokens o tokens de corta duracion.

2. **JWT con secreto simetrico compartido**

   Con HMAC, todos los microservicios necesitan el mismo secreto para validar. Si un servicio se compromete, el secreto se expone. Mejor usar llaves asimetricas y publicar llave publica/JWKS.

3. **`register-cliente` no esta alineado completamente**

   El request trae datos completos del cliente, pero Seguridad no crea cliente y devuelve `IdCliente = 0`. Para la arquitectura final, este endpoint deberia transformarse en un endpoint interno mas claro, por ejemplo `POST /api/v1/internal/users/client-account`, o moverse a comandos del bus.

4. **`IdCliente` no siempre se carga en el token**

   El token solo incluye `id_cliente` si el usuario tiene `IdCliente`. Si el registro deja `IdCliente = null`, los endpoints que dependen de ese claim para rol `CLIENTE` pueden fallar.

5. **No se ve Outbox/Inbox**

   Para integracion confiable con bus, faltan tablas o componentes de outbox/inbox.

6. **Roles deben ser consistentes**

   La descripcion inicial menciona `[Authorize(Roles = "ADMIN")]`, pero el codigo usa `ADMINISTRADOR`. Hay que elegir una convencion global.

7. **No loggear informacion sensible**

   Los eventos y comandos no deben registrar password, token completo ni secrets.

## 18. Cambios recomendados antes de integrar el bus

Prioridad alta:

- Definir contratos definitivos de eventos y comandos de Seguridad.
- Agregar `correlationId`, `eventId`, `commandId` y `occurredAtUtc`.
- Implementar Outbox en Seguridad.
- Implementar Inbox/idempotencia para comandos recibidos.
- Redisenar `register-cliente` como flujo de saga con Clientes.
- Asegurar que `IdCliente` se vincule correctamente al usuario antes de emitir tokens de cliente.
- Estandarizar roles (`ADMINISTRADOR`, `CLIENTE`, etc.).

Prioridad media:

- Cambiar JWT simetrico a asimetrico con JWKS.
- Reemplazar blacklist en memoria por mecanismo distribuido o usar tokens de vida corta.
- Crear endpoints internos separados de endpoints publicos.
- Agregar health checks.
- Agregar trazabilidad distribuida.

Prioridad baja:

- Ocultar o eliminar `WeatherForecastController`.
- Revisar nombres de archivos como `AutenticationExtensions.cs`, que parece tener un typo.
- Documentar ejemplos OpenAPI para endpoints internos.

## 19. Guia breve para una IA o equipo que no tenga el proyecto

Si otra IA va a construir el Bus de Integracion, debe entender esto:

MS Seguridad es el unico microservicio que autentica credenciales y genera JWT. Su base de datos solo contiene usuarios, roles y asignaciones usuario-rol. Otros microservicios nunca deben tener tablas de usuarios/roles ni generar tokens. Cuando otro dominio necesite crear una cuenta, bloquear un usuario, asociar un usuario a un cliente o auditar un login, debe comunicarse mediante el bus o endpoints internos.

El bus debe implementar sagas, no transacciones SQL distribuidas. Para el registro de cliente, el bus debe coordinar MS Clientes y MS Seguridad. Clientes crea el cliente en su propia base; Seguridad crea el usuario, hashea la contrasena, asigna rol CLIENTE y guarda la referencia logica `idCliente`. Si una parte falla, el bus debe ejecutar compensaciones como desactivar cliente o desactivar usuario.

Seguridad debe publicar eventos como `Security.UserCreated`, `Security.UserUpdated`, `Security.UserDeleted`, `Security.UserRoleAssigned`, `Security.UserRoleRevoked`, `Security.UserLoggedIn` y `Security.UserLoginFailed`. Seguridad debe consumir comandos como `Security.CreateUserForClient`, `Security.LinkUserToClient`, `Security.AssignRoleToUser` y `Security.DisableUser`. Todos los mensajes deben tener `eventId` o `commandId`, `correlationId`, fecha UTC, origen y version de contrato.

Los demas microservicios solo validan JWT con la configuracion de issuer, audience, firma y expiracion. La autorizacion se hace con claims de rol. Si el usuario es cliente, el claim `id_cliente` permite limitar acceso a sus propios recursos.

Para confiabilidad, Seguridad debe usar Outbox al publicar eventos e Inbox/idempotencia al consumir comandos. El bus debe manejar reintentos, dead-letter queue, correlacion y compensaciones.

## 20. Resumen final

El Bus de Integracion debe tratar a MS Seguridad como la autoridad de identidad y permisos. No debe duplicar su logica ni su base de datos. Su trabajo es coordinar procesos entre Seguridad y los demas dominios, especialmente Clientes y Auditoria, manteniendo consistencia eventual mediante eventos, comandos, sagas, outbox, inbox e idempotencia.

