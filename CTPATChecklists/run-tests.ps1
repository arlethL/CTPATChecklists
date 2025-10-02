param(
  [ValidateSet('All','Login','Licencias','Otros')]
  [string]$Category = 'All',
  [string]$Configuration = 'Release',
  [int]$Threshold = 70
)

$solutionDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location (Split-Path $solutionDir)

$filter = ""
if ($Category -ne 'All') { $filter = "--filter `"TestCategory=$Category`"" }

Write-Host "== Restore =="; dotnet restore
Write-Host "== Build =="; dotnet build -c $Configuration --no-restore

Write-Host "== Test: Unit =="
dotnet test tests/CTPATChecklists.UnitTests/CTPATChecklists.UnitTests.csproj `
  -c $Configuration --no-build $filter `
  /p:CollectCoverage=true `
  /p:CoverletOutput=TestResults/coverage.unit.cobertura.xml `
  /p:CoverletOutputFormat=cobertura `
  /p:Threshold=$Threshold /p:ThresholdType=line `
  --logger "trx;LogFileName=unit.trx"

Write-Host "== Test: Integration =="
dotnet test tests/CTPATChecklists.IntegrationTests/CTPATChecklists.IntegrationTests.csproj `
  -c $Configuration --no-build $filter `
  /p:CollectCoverage=true `
  /p:CoverletOutput=TestResults/coverage.int.cobertura.xml `
  /p:CoverletOutputFormat=cobertura `
  /p:Threshold=$Threshold /p:ThresholdType=line `
  --logger "trx;LogFileName=int.trx"

# Reporte HTML de cobertura
# Prereq (una sola vez): dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator `
  -reports:**/TestResults/coverage.*.cobertura.xml `
  -targetdir:TestResults/CoverageReport `
  -reporttypes:Html

Write-Host "== Listo =="
Write-Host "Resultados TRX:  **/TestResults/*.trx"
Write-Host "Cobertura HTML:   TestResults/CoverageReport/index.html"
