<#!
.SYNOPSIS
  Build a macOS .pkg from a staged ZILF folder using fpm.

.DESCRIPTION
  Produces a signed-less macOS installer package (.pkg) using fpm -t osxpkg from
  the staged directory created by the Stage target. Installs under /opt/zilf and
  creates/removes symlinks in /usr/local/bin for `zilf` and `zapf`.

.PARAMETER Source
  Path to the staged directory, e.g. Package/Release/Stage/zilf-1.2.3-osx-arm64

.PARAMETER Destination
  Output directory for the generated .pkg file.

.PARAMETER Version
  Full package version (e.g., 1.2.3 or 1.2.3-beta1). Iteration is split if present.

.PARAMETER Arch
  Architecture label for filename (e.g., arm64 or x64). Used only for naming.

.EXAMPLE
  ./tools/make-macos-package.ps1 -Source Package/Release/Stage/zilf-1.2.3-osx-arm64 -Destination Package/Release/Packages -Version 1.2.3 -Arch arm64
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Source,

    [Parameter(Mandatory = $true)]
    [string]$Destination,

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [ValidateSet('arm64','x64')]
    [string]$Arch,

    [string]$Name = 'zilf'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-AbsolutePath([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { throw "Path is empty" }
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return (Join-Path -Path $PWD -ChildPath $Path)
}

function Split-Version([string]$LongVersion) {
    $m = [regex]::Match($LongVersion, '^(?<base>[^-]+?)(?:-(?<iter>.+))?$')
    if (-not $m.Success) { throw "Unable to parse version '$LongVersion'" }
    return @{ base = $m.Groups['base'].Value; iteration = ($m.Groups['iter'].Success ? $m.Groups['iter'].Value : $null); full = $LongVersion }
}

function New-TempScript([string]$Content) {
    $tmpDir = $env:RUNNER_TEMP
    if ([string]::IsNullOrWhiteSpace($tmpDir)) { $tmpDir = [System.IO.Path]::GetTempPath() }
    $file = Join-Path $tmpDir ("fpm-script-" + [System.Guid]::NewGuid().ToString('N'))
    Set-Content -Path $file -Value $Content -Encoding ascii -NoNewline:$false
    try { & chmod +x $file } catch { }
    return $file
}

# Resolve and validate paths
$Source = Resolve-AbsolutePath $Source
$Destination = Resolve-AbsolutePath $Destination
if (-not (Test-Path -LiteralPath $Source -PathType Container)) { throw "Source directory '$Source' not found" }
if (-not (Test-Path -LiteralPath $Destination)) { New-Item -ItemType Directory -Path $Destination | Out-Null }

$versionParts = Split-Version -LongVersion $Version

# Output filename must match aggregator: zilf-<version>-<os>-<arch>.pkg
$os = 'osx'
$pkgOut = Join-Path $Destination ("$Name-$Version-$os-$Arch.pkg")

# Post-install and pre-remove scripts: manage /usr/local/bin symlinks
$postInstall = @'
#!/bin/sh
set -e
BIN_DIR="/opt/zilf/bin"
mkdir -p /usr/local/bin
ln -sf "$BIN_DIR/zilf" /usr/local/bin/zilf
ln -sf "$BIN_DIR/zapf" /usr/local/bin/zapf
exit 0
'@

$preRemove = @'
#!/bin/sh
set -e
remove_link() {
  if [ -L "$1" ]; then
    TARGET="$(readlink "$1")"
    if [ "$TARGET" = "/opt/zilf/bin/zilf" ] || [ "$TARGET" = "/opt/zilf/bin/zapf" ]; then
      rm -f "$1"
    fi
  fi
}
remove_link /usr/local/bin/zilf
remove_link /usr/local/bin/zapf
exit 0
'@

$postInstallFile = New-TempScript -Content $postInstall
$preRemoveFile   = New-TempScript -Content $preRemove

# Build osxpkg with fpm
$common = @(
    '-s','dir',
    '-n', $Name,
    '-v', $versionParts.base,
    '--prefix','/opt/zilf',
    '--description','ZILF tools (compiler and assembler) for the Z-machine and ZIL.',
    '--license','Custom',
    '--vendor','ZILF Project',
    '--url','https://vaporware.atlassian.net/projects/ZILF',
    '--after-install', $postInstallFile,
    '--before-remove', $preRemoveFile,
    '--osxpkg-identifier-prefix', 'io.zilf',
    '-C', $Source
)
if ($versionParts.iteration) { $common += @('--iteration', $versionParts.iteration) }

Write-Host "Building macOS PKG: $pkgOut" -ForegroundColor Cyan
$fpmArgs = $common + @('-t','osxpkg','-p', $pkgOut, '.')
& fpm @fpmArgs
if ($LASTEXITCODE -ne 0) { throw "fpm failed creating osxpkg with exit code $LASTEXITCODE" }

Write-Host "Package created: $pkgOut" -ForegroundColor Green
