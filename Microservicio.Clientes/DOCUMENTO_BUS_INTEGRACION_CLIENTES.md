# Integracion de MS Clientes al Bus de Integracion

Este documento deja la informacion base que necesita el Bus de Integracion para conectarse con el microservicio de Clientes sin consultar directamente su base de datos.

## 1. Puerto dev

Configuracion real en `Microservicio.Clientes.Api/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5107"
    },
    "https": {
      "applicationUrl": "https://localhost:7247;http://localhost:5107"
    },
    "IIS Express": {
      "commandName": "IISExpress"
    }
  },
  "iisSettings": {
    "iisExpress": {
      "applicationUrl": "http://localhost:62193/",
      "sslPort": 44391
    }
  }
}
```

Puerto dev estilo IIS Express 44xxx:

```text
https://localhost:44391
```

Tambien existe el endpoint HTTP de IIS Express:

```text
http://localhost:62193
```

Si se ejecuta con perfil `https` de Kestrel:

```text
https://localhost:7247
http://localhost:5107
```

Para el bus, usar preferiblemente:

```text
CLIENTES_BASE_URL=https://localhost:44391
```

## 2. Interfaces de DataManagement/Interfaces

### IClienteDataService.cs

```csharp
// IClienteDataService.cs
using Microservicio.Clientes.DataManagement.Common;
using Microservicio.Clientes.DataManagement.Models;

namespace Microservicio.Clientes.DataManagement.Interfaces;

public interface IClienteDataService
{
    Task<DataPagedResult<ClienteDataModel>> GetPagedAsync(ClienteFiltroDataModel filtro);
    Task<ClienteDataModel?> GetByIdAsync(int id);
    Task<ClienteDataModel> CreateAsync(ClienteDataModel model);
    Task<ClienteDataModel?> UpdateAsync(ClienteDataModel model);
    Task<bool> DeleteAsync(int id, string modificadoPorUsuario);
}
```

### IPasajeroDataService.cs

```csharp
// IPasajeroDataService.cs
using Microservicio.Clientes.DataManagement.Common;
using Microservicio.Clientes.DataManagement.Models;

namespace Microservicio.Clientes.DataManagement.Interfaces;

public interface IPasajeroDataService
{
    Task<DataPagedResult<PasajeroDataModel>> GetPagedAsync(PasajeroFiltroDataModel filtro);
    Task<PasajeroDataModel?> GetByIdAsync(int id);
    Task<PasajeroDataModel> CreateAsync(PasajeroDataModel model);
    Task<PasajeroDataModel?> UpdateAsync(PasajeroDataModel model);
    Task<bool> DeleteAsync(int id, string modificadoPorUsuario);
}
```

## 3. Modelos de DataManagement/Models

### ClienteDataModel.cs

```csharp
// ClienteDataModel.cs
namespace Microservicio.Clientes.DataManagement.Models;

public class ClienteDataModel
{
    public int IdCliente { get; set; }
    public Guid ClienteGuid { get; set; }
    public string TipoIdentificacion { get; set; } = null!;
    public string NumeroIdentificacion { get; set; } = null!;
    public string Nombres { get; set; } = null!;
    public string? Apellidos { get; set; }
    public string? RazonSocial { get; set; }
    public string Correo { get; set; } = null!;
    public string Telefono { get; set; } = null!;
    public string Direccion { get; set; } = null!;
    public int IdCiudadResidencia { get; set; }
    public int IdPaisNacionalidad { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? Genero { get; set; }
    public string Estado { get; set; } = null!;
    public bool EsEliminado { get; set; }
    public string CreadoPorUsuario { get; set; } = null!;
    public DateTime FechaRegistroUtc { get; set; }
    public string? ModificadoPorUsuario { get; set; }
    public DateTime? FechaModificacionUtc { get; set; }
    public string? ModificacionIp { get; set; }
    public string ServicioOrigen { get; set; } = null!;
    public DateTime? FechaInhabilitacionUtc { get; set; }
    public string? MotivoInhabilitacion { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
```

### PasajeroDataModel.cs

```csharp
// PasajeroDataModel.cs
namespace Microservicio.Clientes.DataManagement.Models;

public class PasajeroDataModel
{
    public int IdPasajero { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public int? IdCliente { get; set; }
    public string NombrePasajero { get; set; } = null!;
    public string ApellidoPasajero { get; set; } = null!;
    public string TipoDocumentoPasajero { get; set; } = null!;
    public string NumeroDocumentoPasajero { get; set; } = null!;
    public DateTime? FechaNacimientoPasajero { get; set; }
    public int? IdPaisNacionalidad { get; set; }
    public string? EmailContactoPasajero { get; set; }
    public string? TelefonoContactoPasajero { get; set; }
    public string? GeneroPasajero { get; set; }
    public bool RequiereAsistencia { get; set; }
    public string? ObservacionesPasajero { get; set; }
    public string Estado { get; set; } = null!;
    public bool EsEliminado { get; set; }
    public string CreadoPorUsuario { get; set; } = null!;
    public DateTime FechaRegistroUtc { get; set; }
    public string? ModificadoPorUsuario { get; set; }
    public DateTime? FechaModificacionUtc { get; set; }
    public string? ModificacionIp { get; set; }
}
```

## 4. Endpoints reales del MS Clientes

Todos los endpoints versionados usan el prefijo:

```text
/api/v1
```

Los endpoints protegidos requieren:

```http
Authorization: Bearer <jwt_emitido_por_ms_seguridad>
```

El JWT debe traer rol para `[Authorize(Roles = "...")]`. Para operaciones del rol `CLIENTE`, el token tambien debe traer el claim:

```text
id_cliente
```

### Clientes admin

Controlador real: `ClienteAdminController`

| Metodo | Endpoint | Roles | Uso |
| --- | --- | --- | --- |
| GET | `/api/v1/clientes` | `ADMINISTRADOR`, `AEROLINEA` | Listar clientes paginados/filtrados. |
| GET | `/api/v1/clientes/{id_cliente}` | `ADMINISTRADOR`, `AEROLINEA`, `CLIENTE` | Obtener cliente por id. Si el rol es `CLIENTE`, el servicio valida que sea su propio cliente. |
| POST | `/api/v1/clientes` | `ADMINISTRADOR`, `AEROLINEA` | Crear cliente desde flujo administrativo. |
| PUT | `/api/v1/clientes/{id_cliente}` | `ADMINISTRADOR`, `CLIENTE` | Actualizar cliente. Si el rol es `CLIENTE`, el servicio valida que sea su propio cliente. |
| DELETE | `/api/v1/clientes/{id_cliente}` | `ADMINISTRADOR` | Eliminar/inactivar cliente. |

Resumen tipo Aeropuertos:

```http
GET    /api/v1/clientes                 Roles: ADMINISTRADOR,AEROLINEA
GET    /api/v1/clientes/{id_cliente}    Roles: ADMINISTRADOR,AEROLINEA,CLIENTE
POST   /api/v1/clientes                 Roles: ADMINISTRADOR,AEROLINEA
PUT    /api/v1/clientes/{id_cliente}    Roles: ADMINISTRADOR,CLIENTE
DELETE /api/v1/clientes/{id_cliente}    Roles: ADMINISTRADOR
```

### Clientes portal

Controlador real: `ClientePortalController`

| Metodo | Endpoint | Roles | Uso |
| --- | --- | --- | --- |
| POST | `/api/v1/clientes/portal/registro` | Publico, `AllowAnonymous` | Registro publico de cliente. No requiere JWT. Usa `SELF_REGISTER` como usuario de auditoria. |
| GET | `/api/v1/clientes/portal/mi-perfil` | `CLIENTE` | Cliente obtiene su propio perfil usando el claim `id_cliente`. |
| PUT | `/api/v1/clientes/portal/mi-perfil` | `CLIENTE` | Cliente actualiza su propio perfil usando el claim `id_cliente`. |

Resumen:

```http
POST /api/v1/clientes/portal/registro    Publico / AllowAnonymous
GET  /api/v1/clientes/portal/mi-perfil   Roles: CLIENTE
PUT  /api/v1/clientes/portal/mi-perfil   Roles: CLIENTE
```

### Pasajeros

Controlador real: `PasajeroAdminController`

| Metodo | Endpoint | Roles | Uso |
| --- | --- | --- | --- |
| GET | `/api/v1/pasajeros` | `ADMINISTRADOR`, `AEROLINEA` | Listar pasajeros paginados/filtrados. |
| GET | `/api/v1/pasajeros/{id_pasajero}` | `ADMINISTRADOR`, `AEROLINEA`, `CLIENTE` | Obtener pasajero por id. Si el rol es `CLIENTE`, el servicio valida pertenencia al cliente del token. |
| POST | `/api/v1/pasajeros` | `ADMINISTRADOR`, `AEROLINEA`, `CLIENTE` | Crear pasajero. Si el rol es `CLIENTE`, el API fuerza `request.IdCliente` con el claim `id_cliente`. |
| PUT | `/api/v1/pasajeros/{id_pasajero}` | `ADMINISTRADOR`, `AEROLINEA` | Actualizar pasajero. |

Resumen tipo Aeropuertos:

```http
GET  /api/v1/pasajeros                  Roles: ADMINISTRADOR,AEROLINEA
GET  /api/v1/pasajeros/{id_pasajero}    Roles: ADMINISTRADOR,AEROLINEA,CLIENTE
POST /api/v1/pasajeros                  Roles: ADMINISTRADOR,AEROLINEA,CLIENTE
PUT  /api/v1/pasajeros/{id_pasajero}    Roles: ADMINISTRADOR,AEROLINEA
```

Nota: aunque `IPasajeroDataService` tiene `DeleteAsync`, actualmente no hay endpoint `DELETE /api/v1/pasajeros/{id_pasajero}` expuesto en el controlador.

## 5. Contratos recomendados para el Bus de Integracion

El bus no debe conectarse a la base de datos de Clientes. Debe usar eventos, comandos o estos endpoints HTTP.

Operaciones principales que el bus debe coordinar:

- Crear cliente y provisionar usuario en MS Seguridad.
- Consultar cliente para Ventas-Facturacion.
- Consultar pasajero para Ventas-Facturacion.
- Publicar cambios de cliente/pasajero hacia otros microservicios.
- Enviar eventos de auditoria.

### Eventos que MS Clientes deberia publicar

```text
clientes.cliente.creado.v1
clientes.cliente.actualizado.v1
clientes.cliente.inactivado.v1
clientes.pasajero.creado.v1
clientes.pasajero.actualizado.v1
clientes.pasajero.inactivado.v1
```

### Comandos/eventos relacionados con Seguridad

Para crear usuario asociado al cliente, evitar que el evento publico lleve password. Usar un comando privado hacia Seguridad:

```text
seguridad.crear-usuario-cliente.command.v1
```

Respuesta esperada desde Seguridad:

```text
seguridad.usuario-cliente.creado.v1
seguridad.usuario-cliente.creacion-fallida.v1
```

### Metadatos minimos de mensajes

```json
{
  "eventId": "uuid",
  "eventType": "clientes.cliente.creado.v1",
  "occurredAtUtc": "2026-05-13T00:00:00Z",
  "source": "MS_CLIENTES",
  "correlationId": "uuid",
  "causationId": "uuid",
  "schemaVersion": 1,
  "data": {}
}
```

## 6. Reglas importantes para la integracion

- MS Clientes no debe generar JWT; solo valida tokens emitidos por MS Seguridad.
- MS Clientes no debe consultar bases de datos de Seguridad, Geografia, Ventas-Facturacion ni Auditoria.
- El bus debe tratar `ClienteDataModel.IdCliente` y `PasajeroDataModel.IdPasajero` como identificadores internos del dominio Clientes.
- Los consumidores deben ser idempotentes usando `eventId` y/o `correlationId`.
- Para publicar eventos de forma confiable, implementar patron Outbox en MS Clientes.
- Para consumir respuestas del bus, implementar patron Inbox en MS Clientes.
- El flujo de registro cliente + usuario debe manejarse como saga con compensacion, no con transaccion distribuida.
