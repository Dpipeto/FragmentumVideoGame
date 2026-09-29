<#
.SYNOPSIS
    Verifica el proyecto: compila sin abrir Unity y, opcionalmente, regenera y ejecuta las pruebas en Unity.

.DESCRIPTION
    Paso 1 (siempre): compila los scripts del juego, del Editor y de las pruebas con el SDK de .NET
    que trae Unity, contra las DLL de Unity. Tarda segundos.

    Paso 2 (con -Unity): abre Unity en modo batch para
      a) regenerar lo que generan las herramientas y preparar las escenas de nivel. Las escenas de
         prueba que ya existen no se reescriben; con -Forzar, sí (Fragmentum → Regenerar todo),
      b) ejecutar las pruebas EditMode y
      c) ejecutar las pruebas PlayMode.
    El proyecto debe estar CERRADO en el editor: Unity no abre dos veces el mismo proyecto.

.EXAMPLE
    .\Herramientas\verificar.ps1
    .\Herramientas\verificar.ps1 -Unity
    .\Herramientas\verificar.ps1 -Unity -Forzar
#>
param(
    [switch]$Unity,
    [switch]$Forzar,
    [string]$UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"
$herramientas = Split-Path -Parent $MyInvocation.MyCommand.Path
$proyecto = Split-Path -Parent $herramientas
$salida = Join-Path $herramientas "Verificacion\Salida"
New-Item -ItemType Directory -Force $salida | Out-Null

$datosUnity = Join-Path (Split-Path -Parent $UnityExe) "Data"
$dotnet = Join-Path $datosUnity "DotNetSdk\dotnet.exe"
if (-not (Test-Path $dotnet)) { throw "No se encontró el SDK de .NET de Unity en $dotnet. Ajusta -UnityExe." }

$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_NOLOGO = "1"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

Write-Host "== 1. Compilación sin Unity" -ForegroundColor Cyan
$texto = & $dotnet build (Join-Path $herramientas "Verificacion\Tests\Fragmentum.Tests.csproj") -nologo -v:m "-p:UnityData=$datosUnity" 2>&1 | Out-String
$lineas = $texto -split "`r?`n"
$errores = @($lineas | Where-Object { $_ -match ": error " } | Sort-Object -Unique)
$avisos = @($lineas | Where-Object { $_ -match ": warning CS" } | Sort-Object -Unique)
Write-Host "Errores: $($errores.Count)  Avisos: $($avisos.Count)"
$errores + $avisos | ForEach-Object { Write-Host "  $_" }
if ($errores.Count -gt 0) { exit 1 }

if (-not $Unity) { exit 0 }

function Invoke-UnityBatch([string[]]$argumentos, [string]$nombre) {
    $log = Join-Path $salida "$nombre.log"
    $todos = @('-batchmode', '-projectPath', "`"$proyecto`"", '-logFile', "`"$log`"") + $argumentos
    $p = Start-Process -FilePath $UnityExe -ArgumentList $todos -PassThru -Wait -WindowStyle Hidden
    $avisosUnity = @(Select-String -Path $log -Pattern "warning (CS|UAC)|error CS" | ForEach-Object { $_.Line })
    $avisosUnity | ForEach-Object { Write-Host "  $_" }
    return $p.ExitCode
}

Write-Host "== 2a. Regenerar todo en Unity" -ForegroundColor Cyan
$metodo = if ($Forzar) { 'Fragmentum.EditorTools.ConstruccionBatch.TodoForzado' } else { 'Fragmentum.EditorTools.ConstruccionBatch.Todo' }
$codigo = Invoke-UnityBatch @('-executeMethod', $metodo, '-quit') "regenerar"
if ($codigo -ne 0) { Write-Host "La regeneración falló (código $codigo). Mira Salida\regenerar.log." -ForegroundColor Red; exit 1 }

$fallos = 0
foreach ($plataforma in "EditMode", "PlayMode") {
    Write-Host "== 2. Pruebas $plataforma" -ForegroundColor Cyan
    $resultados = Join-Path $salida "resultados-$plataforma.xml"
    if (Test-Path $resultados) { Remove-Item $resultados }
    Invoke-UnityBatch @('-runTests', '-testPlatform', $plataforma, '-testResults', "`"$resultados`"") "pruebas-$plataforma" | Out-Null
    if (-not (Test-Path $resultados)) { Write-Host "Sin resultados: revisa Salida\pruebas-$plataforma.log" -ForegroundColor Red; $fallos++; continue }

    [xml]$xml = Get-Content $resultados
    $r = $xml.'test-run'
    Write-Host "Total: $($r.total)  Correctas: $($r.passed)  Fallidas: $($r.failed)"
    $xml.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
        Write-Host "  FALLO $($_.name): $($_.failure.message.'#cdata-section')" -ForegroundColor Red
    }
    $fallos += [int]$r.failed
}

exit ([int]($fallos -gt 0))
