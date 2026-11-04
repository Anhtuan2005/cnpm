param(
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(-[A-Za-z0-9.]+)?$')][string]$Version = '1.0.0',
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo 'artifacts/releases' }
$null = New-Item -ItemType Directory -Force -Path $OutputDirectory
$outputRoot = (Resolve-Path $OutputDirectory).Path
$stage = Join-Path $outputRoot ('stage-' + [Guid]::NewGuid().ToString('N'))
$app = Join-Path $stage 'app'
$zip = Join-Path $outputRoot "Techvora-$Version.zip"
if (Test-Path -LiteralPath $zip) { throw "Package already exists: $zip. Choose a new output directory." }
$null = New-Item -ItemType Directory -Path $stage
Push-Location $repo
try {
    & dotnet publish src/backend/EcommerceApp.csproj --configuration Release --output $app --self-contained false -p:UseAppHost=false -p:Version=$Version --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }
    $forbidden = Get-ChildItem -LiteralPath $app -File -Recurse | Where-Object {
        $_.FullName -match '[\\/](uploads|App_Data|logs|node_modules|obj)[\\/]' -or
        $_.Name -match '^appsettings\.(?!json$)' -or
        $_.Name -match '^(secrets\.json|\.env.*)$'
    }
    if ($forbidden) { throw ('Package contains excluded runtime or local files: ' + ($forbidden.Name -join ', ')) }
    # Ship only reviewed base settings. A caller must supply the database server.
    $settingsPath = Join-Path $app 'appsettings.json'
    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    $settings.ConnectionStrings.DefaultConnection = ''
    $settings.Demo.Enabled = $false
    [IO.File]::WriteAllText($settingsPath, ($settings | ConvertTo-Json -Depth 20), [Text.UTF8Encoding]::new($false))
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Start-Demo.ps1') -Destination $stage
    Copy-Item -LiteralPath (Join-Path $repo 'README.md') -Destination $stage
    foreach ($path in @('docs/lab6', 'docs/user-guide', 'docs/design/architecture.md')) {
        $source = Join-Path $repo $path
        if (Test-Path -LiteralPath $source) {
            $target = Join-Path $stage (Split-Path $path -Parent)
            $null = New-Item -ItemType Directory -Force -Path $target
            Copy-Item -LiteralPath $source -Destination $target -Recurse
        }
    }
    $revision = (& git rev-parse HEAD).Trim()
    $manifest = [ordered]@{
        version = $Version
        sourceCommit = $revision
        sourceHasChanges = [bool](& git status --porcelain)
        framework = 'net8.0; framework-dependent'
        requirements = @('ASP.NET Core Runtime 8', 'SQL Server', 'PowerShell for Start-Demo.ps1')
        excluded = @('uploads', 'App_Data', 'logs', 'local settings', 'API credentials')
        files = @(Get-ChildItem -LiteralPath $stage -Recurse -File | Sort-Object FullName | ForEach-Object {
            @{path=$_.FullName.Substring($stage.Length + 1).Replace('\','/'); sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
        })
    }
    [IO.File]::WriteAllText((Join-Path $stage 'manifest.json'), ($manifest | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
    $digest = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText((Join-Path $outputRoot 'SHA256SUMS.txt'), "$digest  Techvora-$Version.zip`n", [Text.UTF8Encoding]::new($false))
    Write-Host "Package: $zip"
    Write-Host "SHA256: $digest"
} finally { Pop-Location }
