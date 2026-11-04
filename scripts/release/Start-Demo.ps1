param(
    [string]$ConnectionString,
    [ValidateRange(1, 65535)][int]$Port = 5009,
    [ValidateSet(2, 3)][int]$Sprint = 3
)
$ErrorActionPreference = 'Stop'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install ASP.NET Core Runtime 8 first.' }
if (-not $ConnectionString) { $ConnectionString = $env:ConnectionStrings__DefaultConnection }
if (-not $ConnectionString) {
    $demoDb = 'TechvoraReleaseDemo_' + [Guid]::NewGuid().ToString('N')
    $ConnectionString = "Server=localhost;Database=$demoDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False"
}
$appDirectory = Join-Path $PSScriptRoot 'app'
if (-not (Test-Path (Join-Path $appDirectory 'EcommerceApp.dll'))) { throw 'Run this script from an extracted release package.' }
# Demo is an isolated non-production environment. Development User Secrets are
# deliberately not loaded when a reviewer runs a downloaded release.
$env:ASPNETCORE_ENVIRONMENT = 'Demo'
$env:DOTNET_ENVIRONMENT = 'Demo'
$env:ConnectionStrings__DefaultConnection = $ConnectionString
$env:Database__MigrateOnStartup = 'true'
$env:Demo__Enabled = 'true'
$env:Email__Enabled = 'false'
$env:Gemini__ApiKey = ''
$env:Ghn__Token = ''
$env:Vnpay__HashSecret = ''
$env:Release__ActiveSprint = [string]$Sprint
Push-Location $appDirectory
try {
    Write-Host "Techvora demo: http://localhost:$Port (Sprint $Sprint). Ctrl+C to stop."
    & dotnet EcommerceApp.dll --urls "http://127.0.0.1:$Port"
    if ($LASTEXITCODE -ne 0) { throw "Application exited with code $LASTEXITCODE" }
} finally { Pop-Location }
