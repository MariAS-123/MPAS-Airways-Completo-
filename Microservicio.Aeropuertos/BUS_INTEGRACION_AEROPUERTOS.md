# Integracion Bus - MS Aeropuertos

Este documento deja solamente la informacion que el Bus de Integracion necesita conocer del microservicio de Aeropuertos para construir el flujo de creacion de aeropuertos.

## 1. Puerto dev de MS Aeropuertos

El puerto equivalente al estilo de Seguridad `44375` y Geografia `44395` es:

```text
https://localhost:44363
```

Ese valor sale de `Microservicio.Aeropuertos.Api/Properties/launchSettings.json`, perfil `IIS Express`, propiedad `sslPort`.

Tambien existen perfiles Kestrel para desarrollo:

```text
https://localhost:7051
http://localhost:5196
```

Para el Bus, si se esta siguiendo la misma convencion de los puertos `443xx`, usar `44363`.

## 2. Interfaz DataManagement

Archivo:

```text
Microservicio.Aeropuertos.DataManagement/Interfaces/IAeropuertoDataService.cs
```

Metodos exactos:

```csharp
Task<DataPagedResult<AeropuertoDataModel>> GetPagedAsync(
    AeropuertoFiltroDataModel filtro,
    CancellationToken cancellationToken = default);

Task<AeropuertoDataModel?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default);

Task<AeropuertoDataModel?> GetByCodigoIataAsync(
    string codigoIata,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<AeropuertoDataModel>> GetByPaisAsync(
    int idPais,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<AeropuertoDataModel>> GetByCiudadAsync(
    int idCiudad,
    CancellationToken cancellationToken = default);

Task<AeropuertoDataModel> CreateAsync(
    AeropuertoDataModel model,
    CancellationToken cancellationToken = default);

Task<AeropuertoDataModel?> UpdateAsync(
    AeropuertoDataModel model,
    CancellationToken cancellationToken = default);

Task<bool> DeleteAsync(
    int id,
    string modificadoPorUsuario,
    CancellationToken cancellationToken = default);
```

## 3. Modelo AeropuertoDataModel

Archivo:

```text
Microservicio.Aeropuertos.DataManagement/Models/AeropuertoDataModel.cs
```

Campos exactos:

```csharp
public int IdAeropuerto { get; set; }

public byte[] RowVersion { get; set; } = null!;

public string CodigoIata { get; set; } = null!;

public string? CodigoIcao { get; set; }

public string Nombre { get; set; } = null!;

public int? IdCiudad { get; set; }

public int IdPais { get; set; }

public string? ZonaHoraria { get; set; }

public decimal? Latitud { get; set; }

public decimal? Longitud { get; set; }

public string Estado { get; set; } = null!;

public bool Eliminado { get; set; }

public DateTime FechaRegistroUtc { get; set; }

public string CreadoPorUsuario { get; set; } = null!;

public string? ModificadoPorUsuario { get; set; }

public DateTime? FechaModificacionUtc { get; set; }

public string? ModificacionIp { get; set; }
```

## Flujo esperado desde el Bus

El Bus ya tiene disponibles:

```text
IGeografiaDataService.ValidarPaisActivoAsync()
IGeografiaDataService.ValidarCiudadActivaEnPaisAsync()
ISeguridadDataService
```

Flujo para crear aeropuerto:

```text
Bus recibe request
  -> ValidarPaisActivo con MS Geografia
  -> ValidarCiudadActivaEnPais con MS Geografia
  -> Llamar a MS Aeropuertos para crear
```

Endpoint actual de creacion en MS Aeropuertos:

```text
POST /api/v1/aeropuertos
```

El endpoint esta protegido con JWT y permite roles:

```text
ADMINISTRADOR
AEROLINEA
```

Por eso, cuando el Bus llame a Aeropuertos para crear, debe reenviar o emitir un JWT valido segun la estrategia definida con `ISeguridadDataService`.

## Resumen rapido para implementar en el Bus

```text
MS Aeropuertos dev HTTPS IIS Express: https://localhost:44363
MS Aeropuertos dev HTTPS Kestrel:     https://localhost:7051
MS Aeropuertos dev HTTP Kestrel:      http://localhost:5196
Crear aeropuerto:                     POST /api/v1/aeropuertos
Contrato interno de datos:            IAeropuertoDataService.CreateAsync(AeropuertoDataModel model, CancellationToken cancellationToken = default)
```
