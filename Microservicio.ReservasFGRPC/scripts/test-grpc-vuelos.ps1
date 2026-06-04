# Verifica la conexión gRPC ReservasF -> Vuelos.
# Requisitos: MS Vuelos corriendo en https://localhost:7006 (perfil https)
#             MS ReservasF corriendo en https://localhost:7280

param(
    [int]$IdVuelo = 1,
    [string]$ReservasFBaseUrl = "https://localhost:7280",
    [switch]$FullTest
)

$ErrorActionPreference = "Stop"

if ($PSVersionTable.PSVersion.Major -lt 6) {
    add-type @"
        using System.Net;
        using System.Security.Cryptography.X509Certificates;
        public class TrustAllCertsPolicy : ICertificatePolicy {
            public bool CheckValidationResult(
                ServicePoint srvPoint, X509Certificate certificate,
                WebRequest request, int certificateProblem) {
                return true;
            }
        }
"@
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
}
else {
    $script:SkipCert = $true
}

function Invoke-TestRequest {
    param([string]$Url)

    if ($PSVersionTable.PSVersion.Major -ge 6) {
        return Invoke-RestMethod -Uri $Url -Method Get -SkipCertificateCheck
    }

    return Invoke-RestMethod -Uri $Url -Method Get
}

Write-Host ""
Write-Host "=== Prueba gRPC MS ReservasF -> MS Vuelos ===" -ForegroundColor Cyan
Write-Host "ReservasF : $ReservasFBaseUrl"
Write-Host "Id vuelo  : $IdVuelo"
Write-Host ""

$endpoint = if ($FullTest) { "test" } else { "ping" }
$url = "$ReservasFBaseUrl/api/v1/internal/grpc/vuelos/${endpoint}?id_vuelo=$IdVuelo"

Write-Host "GET $url" -ForegroundColor DarkGray
Write-Host ""

try {
    $response = Invoke-TestRequest -Url $url

    if ($response.success) {
        Write-Host "OK  $($response.message)" -ForegroundColor Green
        Write-Host ""
        $response.data | ConvertTo-Json -Depth 6
        exit 0
    }

    Write-Host "FAIL  $($response.message)" -ForegroundColor Red
    if ($response.errors) {
        $response.errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    }
    exit 1
}
catch {
    $statusCode = $null
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $body = $reader.ReadToEnd() | ConvertFrom-Json
            Write-Host "HTTP $statusCode  $($body.message)" -ForegroundColor Red
            if ($body.errors) {
                $body.errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
            }
            if ($body.data) {
                $body.data | ConvertTo-Json -Depth 6
            }
            exit 1
        }
        catch { }
    }

    Write-Host "ERROR  No se pudo contactar MS ReservasF." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Verifica que ambos servicios estén corriendo:" -ForegroundColor DarkGray
    Write-Host "  1. MS Vuelos  : dotnet run --project Microservicio.Vuelos.Api --launch-profile https"
    Write-Host "  2. MS ReservasF: dotnet run --project Microservicio.ReservasF.Api --launch-profile https"
    exit 2
}
