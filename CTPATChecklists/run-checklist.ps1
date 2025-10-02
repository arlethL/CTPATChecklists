param([string]$Configuration='Release')
$ErrorActionPreference = 'Stop'

# === Config ===
$category = 'Checklist'                            
$covOut   = "TestResults\CoverageReport\Checklist" 

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repoRoot
Write-Host "RepoRoot: $repoRoot" -ForegroundColor Cyan
Write-Host "Running category: $category" -ForegroundColor Cyan

$projects = Get-ChildItem -Recurse -File -Filter *.csproj |
  Where-Object {
    $_.Name -match '(?i)tests?' -or
    $_.DirectoryName -match '(?i)\\tests?\\' -or
    $_.Name -match '(?i)(unit|integration)tests'
  } |
  Select-Object -Expand FullName

if (-not $projects) {
  Write-Error "No se encontraron proyectos de prueba (nombres con 'Tests', 'UnitTests' o 'IntegrationTests')."
  exit 1
}


function Parse-TrxSummary {
  param([string]$TrxPath)
  if (-not (Test-Path $TrxPath)) { return $null }
  try {
    [xml]$xml = Get-Content -Raw $TrxPath
    $c = $xml.TestRun.ResultSummary.Counters
    if ($null -eq $c) { return $null }
    # Los atributos típicos en TRX:
    # total, executed, passed, failed, error, timeout, aborted, inconclusive,
    # passedButRunAborted, notRunnable, notExecuted, disconnected, warning, completed, inProgress, pending
    $total  = [int]$c.total
    $passed = [int]$c.passed
    $failed = ([int]$c.failed + [int]$c.error + [int]$c.notRunnable)
    $skipped = [int]$c.notExecuted
    return [pscustomobject]@{ Total=$total; Passed=$passed; Failed=$failed; Skipped=$skipped }
  } catch {
    return $null
  }
}

function RanZeroTestsDueToFilter {
  param([string[]]$Lines)
  # Mensajes típicos cuando el filtro no encuentra pruebas (inglés o español)
  return ($Lines -match 'No test matches the given testcase filter') -or
         ($Lines -match 'Ninguna prueba coincide con el filtro') -or
         ($Lines -match 'No tests found to run') -or
         ($Lines -match 'No se encontraron pruebas para ejecutar')
}

$covFiles = @()
$summary  = @()

# === Ejecutar cada proyecto de pruebas ===
foreach ($proj in $projects) {
  $name   = [IO.Path]::GetFileNameWithoutExtension($proj)
  $outDir = "TestResults\checklist\$name"
  New-Item -ItemType Directory -Force -Path $outDir | Out-Null

  Write-Host "`n=== dotnet test $name ===" -ForegroundColor Yellow

  # 1) Intento con filtro por categoría (contains)
  $filterExpr = "TestCategory~$category"
  $raw = dotnet test $proj -c $Configuration `
         --filter $filterExpr `
         --logger "trx;LogFileName=$name.trx" `
         --logger "console;verbosity=minimal" `
         --collect:"XPlat Code Coverage" `
         --results-directory $outDir 2>&1

  # 2) Fallback: si el filtro no encuentra nada, ejecutar sin filtro para validar el pipeline
  if (RanZeroTestsDueToFilter $raw) {
      Write-Warning "No se encontraron tests con '$filterExpr' en $name. Ejecutando SIN filtro para validar."
      $raw = dotnet test $proj -c $Configuration `
             --logger "trx;LogFileName=$name.trx" `
             --logger "console;verbosity=minimal" `
             --collect:"XPlat Code Coverage" `
             --results-directory $outDir 2>&1
  }

  # 3) Leer el TRX para mostrar el resumen (independiente del idioma)
  $trx = Get-ChildItem $outDir -Filter "$name.trx" -ErrorAction SilentlyContinue | Select-Object -Last 1
  $sum = $null
  if ($trx) { $sum = Parse-TrxSummary $trx.FullName }

  if ($null -ne $sum) {
    if ($sum.Failed -eq 0) {
      Write-Host ("PASS  {0}  (Total:{1}  Passed:{2}  Skipped:{3})" -f $name,$sum.Total,$sum.Passed,$sum.Skipped) -ForegroundColor Green
    } else {
      Write-Host ("FAIL  {0}  (Total:{1}  Passed:{2}  Failed:{3}  Skipped:{4})" -f $name,$sum.Total,$sum.Passed,$sum.Failed,$sum.Skipped) -ForegroundColor Red
    }
    $summary += [pscustomobject]@{ Project=$name; Total=$sum.Total; Passed=$sum.Passed; Failed=$sum.Failed; Skipped=$sum.Skipped }
  } else {
    Write-Warning "No se pudo leer el resumen de $name (puede no haber tests)."
  }

  # 4) Acumular archivos de cobertura (si coverlet.collector está instalado)
  $cov = Get-ChildItem $outDir -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue
  if ($cov) { $covFiles += $cov.FullName }
}

# === Generar reporte de cobertura ===
if ($covFiles.Count -gt 0) {
  dotnet tool update -g dotnet-reportgenerator-globaltool | Out-Null
$rg = reportgenerator -reports:($covFiles -join ';') -targetdir:$covOut -reporttypes:"TextSummary;Html" 2>&1
  $rg | ForEach-Object {
    if ($_ -match 'Line coverage|Branch coverage|Method coverage|Cobertura de líneas|Cobertura de ramas|Cobertura de métodos') {
      Write-Host $_
    }
  }
  Write-Host "Reporte HTML: $covOut\index.html" -ForegroundColor DarkGray
  if (Test-Path "$covOut\index.html") { ii "$covOut\index.html" }
} else {
  Write-Warning "No se encontraron archivos de cobertura (coverage.cobertura.xml)."
  Write-Host   "Si no ves cobertura, instala coverlet.collector en TUS PROYECTOS DE PRUEBA:" -ForegroundColor DarkYellow
  Write-Host   "  dotnet add <ruta al .csproj de tests> package coverlet.collector" -ForegroundColor DarkYellow
}

# === Tabla final ===
if ($summary.Count -gt 0) {
  Write-Host "`n== Totales por proyecto ==" -ForegroundColor Cyan
  $summary | Format-Table -AutoSize
}
