#requires -Version 5.1
<#
    Builds Peripheral Companion as a single, self-contained, portable .exe.
    Output: build\dist\PeripheralCompanion.exe  (copy this onto the USB stick)
    Requires the .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
#>
param(
    [string]$RuntimeIdentifier = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    Write-Host "Publishing for $RuntimeIdentifier ..." -ForegroundColor Cyan
    dotnet publish "src/PeripheralCompanion/PeripheralCompanion.csproj" `
        -c Release `
        -r $RuntimeIdentifier `
        --self-contained true `
        -o "build/dist"
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

    $exe = Join-Path $root "build/dist/PeripheralCompanion.exe"
    Write-Host "`nDone. Portable executable:" -ForegroundColor Green
    Write-Host "  $exe"
}
finally {
    Pop-Location
}
