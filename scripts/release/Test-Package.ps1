param([Parameter(Mandatory=$true)][string]$Package, [string]$Destination)
$ErrorActionPreference = 'Stop'
if (-not $Destination) { $Destination = Join-Path ([IO.Path]::GetTempPath()) ('TechvoraPackage_' + [Guid]::NewGuid().ToString('N')) }
if (Test-Path -LiteralPath $Destination) { throw "Extraction directory already exists: $Destination" }
Expand-Archive -LiteralPath $Package -DestinationPath $Destination
$root = (Resolve-Path -LiteralPath $Destination).Path
$manifest = Get-Content -LiteralPath (Join-Path $root 'manifest.json') -Raw | ConvertFrom-Json
foreach ($entry in $manifest.files) {
    $file = [IO.Path]::GetFullPath((Join-Path $root $entry.path))
    if (-not $file.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Invalid file path in manifest'
    }
    if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $entry.sha256) { throw "Hash mismatch: $($entry.path)" }
}
$forbidden = Get-ChildItem -LiteralPath (Join-Path $root 'app') -Recurse -File | Where-Object {
    $_.FullName -match '[\\/](uploads|App_Data|logs)[\\/]' -or $_.Name -match '^(appsettings\..+\.json|secrets\.json|\.env.*)$'
}
if ($forbidden) { throw 'Extracted package contains local configuration or runtime files' }
Write-Host "Verified $($manifest.files.Count) file hashes; no local settings or runtime data."
Write-Output $root
