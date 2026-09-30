# Downloads the latest Hearthstone Deck Tracker release and copies its assemblies to hdt-libs/,
# which the plugin compiles against. They are only referenced, never shipped.
param(
	[string]$Destination = (Join-Path $PSScriptRoot '..\hdt-libs')
)
$ErrorActionPreference = 'Stop'

$headers = @{ 'User-Agent' = 'HST-MulliganV2-build' }
if ($env:GITHUB_TOKEN) { $headers.Authorization = "Bearer $env:GITHUB_TOKEN" }

$release = Invoke-RestMethod 'https://api.github.com/repos/HearthSim/HDT-Releases/releases/latest' -Headers $headers
$asset = $release.assets | Where-Object { $_.name -like '*-full.nupkg' } | Select-Object -First 1
if (-not $asset) { throw "No *-full.nupkg in HDT release $($release.tag_name)" }

$work = Join-Path ([IO.Path]::GetTempPath()) "hdt-$($release.tag_name)"
$zip = "$work.zip"
Invoke-WebRequest $asset.browser_download_url -OutFile $zip -Headers $headers
Expand-Archive $zip $work -Force

$exe = Get-ChildItem $work -Recurse -Filter 'HearthstoneDeckTracker.exe' | Select-Object -First 1
if (-not $exe) { throw "HearthstoneDeckTracker.exe not found in $($asset.name)" }

New-Item -ItemType Directory -Force $Destination | Out-Null
Copy-Item (Join-Path $exe.DirectoryName '*') $Destination -Recurse -Force
Write-Host "HDT $($release.tag_name) assemblies copied to $Destination"
