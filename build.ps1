# ========================================
# NEON WIKI v1.1 - BUILD SCRIPT
# ========================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  NEON WIKI v1.0 - BUILD SCRIPT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar que existe .NET 8.0
Write-Host "[1/4] Verificando .NET 8.0..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: .NET no esta instalado" -ForegroundColor Red
    Write-Host "Descargar desde: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}
Write-Host "      .NET version: $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Restaurar paquetes NuGet
Write-Host "[2/4] Restaurando paquetes NuGet..." -ForegroundColor Yellow
Set-Location -Path "NeonWiki"
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Fallo al restaurar paquetes" -ForegroundColor Red
    exit 1
}
Write-Host "      Paquetes restaurados OK" -ForegroundColor Green
Write-Host ""

# Compilar en Release
Write-Host "[3/4] Compilando proyecto..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Fallo la compilacion" -ForegroundColor Red
    exit 1
}
Write-Host "      Compilacion exitosa" -ForegroundColor Green
Write-Host ""

# Mostrar ubicación del ejecutable
Write-Host "[4/4] Build completado!" -ForegroundColor Yellow
$exePath = Join-Path (Get-Location) "bin\Release\net8.0-windows\NeonWiki.exe"
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BUILD EXITOSO!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Ejecutable en:" -ForegroundColor Yellow
Write-Host "  $exePath" -ForegroundColor White
Write-Host ""
Write-Host "Para ejecutar:" -ForegroundColor Yellow
Write-Host "  .\bin\Release\net8.0-windows\NeonWiki.exe" -ForegroundColor White
Write-Host ""
Write-Host "Presiona cualquier tecla para salir..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Set-Location -Path ".."
