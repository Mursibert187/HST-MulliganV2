#!/usr/bin/env pwsh
<#
    Downloads the current Hearthstone Deck Tracker release and copies the
    SDK DLLs the plugin needs into lib/. Run once before building the
    solution locally, and from CI before msbuild.

    Usage:
      pwsh scripts/fetch-hdt.ps1
      pwsh scripts/fetch-hdt.ps1 -Version v1.28.3
      pwsh scripts/fetch-hdt.ps1 -HdtInstall "C:\Program Files (x86)\Hearthstone Deck Tracker"
#>
[CmdletBinding()]
param(
    [string]$Version = 'latest',
    [string]$HdtInstall,
    [string]$LibDir = (Join-Path $PSScriptRoot '..\lib')
)

$ErrorActionPreference = 'Stop'

$required = @(
    'HearthstoneDeckTracker.exe',
    'HearthDb.dll',
    'HearthMirror.dll',
    'HearthWatcher.dll',
    'HearthstoneCore.dll'
)

function Copy-FromInstall([string]$root) {
    if (-not (Test-Path $root)) { throw "HDT install not found: $root" }
    New-Item -ItemType Directory -Force -Path $LibDir | Out-Null
    foreach ($f in $required) {
        $src = Join-Path $root $f
        if (Test-Path $src) {
            Copy-Item $src -Destination $LibDir -Force
            Write-Host "  copied $f"
        } else {
            Write-Warning "missing in install: $f"
        }
    }
}

function Copy-FromRelease([string]$ver) {
    $tmp = Join-Path ([IO.Path]::GetTempPath()) "hdt-fetch-$([Guid]::NewGuid())"
    New-Item -ItemType Directory -Force -Path $tmp | Out-Null
    $api = if ($ver -eq 'latest') {
        'https://api.github.com/repos/HearthSim/Hearthstone-Deck-Tracker/releases/latest'
    } else {
        "https://api.github.com/repos/HearthSim/Hearthstone-Deck-Tracker/releases/tags/$ver"
    }
    Write-Host "Querying $api"
    $release = Invoke-RestMethod -Uri $api -Headers @{ 'User-Agent' = 'HST-MulliganV2-Fetch' }
    $asset = $release.assets | Where-Object { $_.name -like 'HDT-Installer*.zip' -or $_.name -like '*.zip' } | Select-Object -First 1
    if (-not $asset) { throw "No zip asset in release $($release.tag_name)" }
    $zip = Join-Path $tmp $asset.name
    Write-Host "Downloading $($asset.browser_download_url)"
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip
    $extracted = Join-Path $tmp 'ex'
    Expand-Archive -LiteralPath $zip -DestinationPath $extracted -Force
    $installRoot = Get-ChildItem $extracted -Recurse -Include 'HearthstoneDeckTracker.exe' -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty DirectoryName
    if (-not $installRoot) { throw "Could not locate HearthstoneDeckTracker.exe inside $zip" }
    Copy-FromInstall $installRoot
    Remove-Item $tmp -Recurse -Force
}

if ($HdtInstall) {
    Write-Host "Using local install: $HdtInstall"
    Copy-FromInstall $HdtInstall
} elseif ($env:HDT_INSTALL) {
    Write-Host "Using HDT_INSTALL env: $($env:HDT_INSTALL)"
    Copy-FromInstall $env:HDT_INSTALL
} else {
    Copy-FromRelease $Version
}

Write-Host "Done. Files in ${LibDir}:"
Get-ChildItem $LibDir | ForEach-Object { Write-Host "  $($_.Name)" }
