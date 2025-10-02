param([string]$Configuration='Release')
$ErrorActionPreference = 'Stop'

# === Config ===
$category = 'Licencias'
$covOut   = "TestResults\CoverageReport\Licencias"

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
  Write-Error "No se encontraron proyectos de prueba (Unit/IntegrationTests)."
  exit 1
}

# --- Helpers ---
function Parse-TrxSummary {
  param([string]$TrxPath)
  if (-not (Test-Path $TrxPath)) { return $null }
  try {
    [xml]$xml = Get-Content -Raw $TrxPath
    $c = $xml.TestRun.ResultSummary.Counters
    if ($null -eq $c) { return $null }
    $total   = [int]$c.total
    $passed  = [int]$c.passed
    $failed  = ([int]$c.failed + [int]$c.error + [int]$c.notRunnable)
    $skipped = [int]$c.notExecuted
    return [pscustomobject]@{ Total=$total; Passed=$passed; Failed=$failed; Skipped=$skipped }
  } catch { return $null }
}

function RanZeroTestsDueToFilter {
  param([string[]]$Lines)
  return ($Lines -match 'No test matches the given testcase filter') -or
         ($Lines -match 'Ninguna prueba coincide con el filtro') -or
         ($Lines -match 'No tests found to run') -or
         ($Lines -match 'No se encontraron pruebas para ejecutar')
}

$covFiles = @()
$summary  = @()

foreach ($proj in $projects) {
  $name   = [IO.Path]::GetFileNameWithoutExtension($proj)
  $outDir = "TestResults\Licencias\$name"
  New-Item -ItemType Directory -Force -Path $outDir | Out-Null

  Write-Host "`n=== dotnet test $name ===" -ForegroundColor Yellow

¿  $filterExpr = "TestCategory~$category"
  $raw = dotnet test $proj -c $Configuration `
         --filter $filterExpr `
         --logger "trx;LogFileName=$name.trx" `
         --logger "console;verbosity=minimal" `
         --collect:"XPlat Code Coverage" `
         --results-directory $outDir 2>&1

¿  if (RanZeroTestsDueToFilter $raw) {
    Write-Warning "No se encontraron tests con '$filterExpr' en $name. Ejecutando SIN filtro para validar."
    $raw = dotnet test $proj -c $Configuration `
           --logger "trx;LogFileName=$name.trx" `
           --logger "console;verbosity=minimal" `
           --collect:"XPlat Code Coverage" `
           --results-directory $outDir 2>&1
  }

  # 3) Resumen desde TRX (independiente del idioma)
  $trx = Get-ChildItem $outDir -Filter "$name.trx" -ErrorAction SilentlyContinue | Select-Object -Last 1
  $sum = if ($trx) { Parse-TrxSummary $trx.FullName } else { $null }

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

  # 4) Acumular cobertura
  $cov = Get-ChildItem $outDir -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue
  if ($cov) { $covFiles += $cov.FullName }
}

# Reporte de cobertura
if ($covFiles.Count -gt 0) {
  dotnet tool update -g dotnet-reportgenerator-globaltool | Out-Null
  $null = reportgenerator -reports:($covFiles -join ';') -targetdir:$covOut -reporttypes:"TextSummary;Html" 2>&1
  Write-Host "Reporte HTML: $covOut\index.html" -ForegroundColor DarkGray
  if (Test-Path "$covOut\index.html") { ii "$covOut\index.html" }
} else {
  Write-Warning "No se encontraron archivos de cobertura (coverage.cobertura.xml)."
  Write-Host   "Instala coverlet.collector en tus proyectos de prueba:" -ForegroundColor DarkYellow
  Write-Host   "  dotnet add <ruta al .csproj de tests> package coverlet.collector" -ForegroundColor DarkYellow
}

# Tabla final
if ($summary.Count -gt 0) {
  Write-Host "`n== Totales por proyecto ==" -ForegroundColor Cyan
  $summary | Format-Table -AutoSize
}
