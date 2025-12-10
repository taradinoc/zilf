param(
    [string] $DestinationRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-DestinationPath {
    param([string] $RepoRoot)
    if ($DestinationRoot) { return $DestinationRoot }
    return Join-Path $RepoRoot 'thirdparty/glazer'
}

function Get-LatestReleaseJson {
    $apiUrl = 'https://gitlab.com/api/v4/projects/andwj%2Fglazer/releases/permalink/latest'
    Write-Host "Fetching Glazer release metadata from $apiUrl"
    return Invoke-RestMethod -Uri $apiUrl -UseBasicParsing
}

function Get-SourceTarballUrl {
    param($release)
    $sources = @($release.assets.sources)
    $preferred = $sources | Where-Object { $_.format -eq 'tar.gz' } | Select-Object -First 1
    if (-not $preferred) { $preferred = $sources | Select-Object -First 1 }
    if (-not $preferred) { throw 'No source asset found in Glazer release metadata.' }
    return $preferred.url
}

function Save-Source {
    param(
        [string] $Url,
        [string] $DownloadPath
    )
    Write-Host "Downloading Glazer source tarball from $Url"
    Invoke-WebRequest -Uri $Url -OutFile $DownloadPath -UseBasicParsing
}

function Expand-Source {
    param(
        [string] $TarPath,
        [string] $ExtractRoot
    )
    Write-Host "Extracting tarball to $ExtractRoot"
    tar -xf $TarPath -C $ExtractRoot
    $dirs = Get-ChildItem -Path $ExtractRoot -Directory | Select-Object -First 1
    if (-not $dirs) { throw 'Unable to locate extracted Glazer source directory.' }
    return $dirs.FullName
}

function Build-Glazer {
    param([string] $SourceDir)
    Write-Host "Building Glazer in $SourceDir"
    Push-Location $SourceDir
    try {
        if (-not (Get-Command make -ErrorAction SilentlyContinue)) {
            throw 'The "make" command is required to build Glazer. Install build-essential first.'
        }
        make
    }
    finally {
        Pop-Location
    }
}

function Find-GlazerBinary {
    param([string] $SourceDir)
    $candidates = Get-ChildItem -Path $SourceDir -Recurse -File | Where-Object { $_.Name -in 'glazer', 'glazer.exe' } | Select-Object -First 1
    if (-not $candidates) { throw 'Build completed but Glazer binary was not found.' }
    return $candidates.FullName
}

function Install-Glazer {
    param(
        [string] $BinaryPath,
        [string] $DestinationDir
    )
    New-Item -ItemType Directory -Path $DestinationDir -Force | Out-Null
    $destFile = Join-Path $DestinationDir 'glazer'
    Copy-Item -Path $BinaryPath -Destination $destFile -Force
    if (-not $IsWindows) {
        chmod +x $destFile
    }
    Write-Host "Glazer installed to $destFile"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$destination = Get-DestinationPath -RepoRoot $repoRoot
$existing = Join-Path $destination 'glazer'
if (Test-Path $existing) {
    Write-Host "Glazer already present at $existing; skipping download."
    return
}

$release = Get-LatestReleaseJson
$tarUrl = Get-SourceTarballUrl -release $release

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("glazer_build_" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

try {
    $tarPath = Join-Path $tempRoot 'glazer.tar.gz'
    Save-Source -Url $tarUrl -DownloadPath $tarPath
    $sourceDir = Expand-Source -TarPath $tarPath -ExtractRoot $tempRoot
    Build-Glazer -SourceDir $sourceDir
    $binary = Find-GlazerBinary -SourceDir $sourceDir
    Install-Glazer -BinaryPath $binary -DestinationDir $destination
}
finally {
    if (Test-Path $tempRoot) {
        Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
