<#!
.SYNOPSIS
	Build native Linux packages (DEB and RPM) from a staged ZILF folder using fpm.

.DESCRIPTION
	This script wraps fpm to produce .deb and .rpm packages from a staged directory
	created by tools/package-all.ps1 (Build.proj Stage target). It:
		- Parses version from $Env:ZILF_LONG_VERSION or the staged folder name
		- Splits version/iteration for RPM/DEB compliance (e.g. 1.2.3-beta1 -> 1.2.3 + beta1)
		- Maps RID architecture to Debian/RPM arch names
		- Installs under /opt/zilf using --prefix, preserving staged layout (bin/, sample/, zillib/)
		- Creates post-install/pre-remove scripts to manage /usr/bin symlinks (zilf, zapf, zilfpub)
		- Emits files named: zilf-<version>-linux-<arch>.deb/.rpm in the destination directory

.PARAMETER Source
	Path to the staged directory, e.g. Package/Release/Stage/zilf-1.2.3-linux-x64

.PARAMETER Destination
	Output directory for the generated packages.

.EXAMPLE
	./tools/make-linux-packages.ps1 -Source Package/Release/Stage/zilf-1.2.3-linux-x64 -Destination Package/Release/Packages

.NOTES
	- Requires fpm to be installed and available on PATH.
	- Intended for use on Linux runners (GitHub Actions uses pwsh on linux-* jobs).
#>

param(
	[Parameter(Mandatory = $true)]
	[string]$Source,

	[Parameter(Mandatory = $true)]
	[string]$Destination,

	[string]$Name = 'zilf',

	[Parameter(Mandatory = $true)]
	[string]$Version,

	[Parameter(Mandatory = $true)]
	[ValidateSet('linux')]
	[string]$Os,

	[Parameter(Mandatory = $true)]
	[ValidateSet('x64','arm64','arm')]
	[string]$Arch
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-AbsolutePath([string]$Path) {
		if ([string]::IsNullOrWhiteSpace($Path)) { throw "Path is empty" }
		if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
		return (Join-Path -Path $PWD -ChildPath $Path)
}

function Split-Version([string]$LongVersion) {
		# Returns a hashtable @{ base = '1.2.3'; iteration = 'beta1' (or $null); full = original }
		if ([string]::IsNullOrWhiteSpace($LongVersion)) { throw "Version string is empty" }
		$m = [regex]::Match($LongVersion, '^(?<base>[^-]+?)(?:-(?<iter>.+))?$')
		if (-not $m.Success) { throw "Unable to parse version '$LongVersion'" }
		return @{ base = $m.Groups['base'].Value; iteration = ($m.Groups['iter'].Success ? $m.Groups['iter'].Value : $null); full = $LongVersion }
}

function Get-ArchMaps([string]$Arch) {
		switch ($Arch) {
				'x64'   { return @{ deb = 'amd64';  rpm = 'x86_64' } }
				'arm64' { return @{ deb = 'arm64';  rpm = 'aarch64' } }
				'arm'   { return @{ deb = 'armhf';  rpm = 'armv7hl' } }
				default { throw "Unsupported architecture '$Arch' (expected x64, arm64, or arm)." }
		}
}

function New-TempScript([string]$Content) {
	$tmpDir = $env:RUNNER_TEMP
	if ([string]::IsNullOrWhiteSpace($tmpDir)) { $tmpDir = [System.IO.Path]::GetTempPath() }
	$file = Join-Path $tmpDir ("fpm-script-" + [System.Guid]::NewGuid().ToString('N') + ".sh")
	# Normalize newlines to LF to avoid /bin/sh^M issues and write without BOM
	$normalized = $Content -replace "`r`n", "`n" -replace "`r", "`n"
	$normalized | Out-File -FilePath $file -Encoding utf8NoBOM
	# Make it executable on *nix
	try { & chmod +x $file } catch { }
	return $file
}

# Resolve and validate paths
$Source = Resolve-AbsolutePath $Source
$Destination = Resolve-AbsolutePath $Destination
if (-not (Test-Path -LiteralPath $Source -PathType Container)) { throw "Source directory '$Source' not found" }
if (-not (Test-Path -LiteralPath $Destination)) { New-Item -ItemType Directory -Path $Destination | Out-Null }

# Determine version/OS/arch from parameters only
$versionParts = Split-Version -LongVersion $Version
if ($Os -ne 'linux') { throw "This script only supports linux packages; got os='$Os'" }
$archMaps = Get-ArchMaps -Arch $Arch

# Output filenames (must match aggregator's regex: zilf-<version>-<os>-<arch>.<ext>)
$debOut = Join-Path $Destination ("$Name-$Version-$Os-$Arch.deb")
$rpmOut = Join-Path $Destination ("$Name-$Version-$Os-$Arch.rpm")

# Post-install and pre-remove scripts to manage /usr/bin symlinks
$postInstall = @'
#!/bin/sh
set -e
BIN_DIR="/opt/zilf/bin"
ln -sf "$BIN_DIR/zilf" /usr/bin/zilf
ln -sf "$BIN_DIR/zapf" /usr/bin/zapf
ln -sf "$BIN_DIR/zilfpub" /usr/bin/zilfpub
exit 0
'@

$preRemove = @'
#!/bin/sh
set -e
remove_link() {
	if [ -L "$1" ]; then
		TARGET="$(readlink -f "$1" 2>/dev/null || readlink "$1")"
		if [ "$TARGET" = "/opt/zilf/bin/zilf" ] || [ "$TARGET" = "/opt/zilf/bin/zapf" ] || [ "$TARGET" = "/opt/zilf/bin/zilfpub" ]; then
			rm -f "$1"
		fi
	fi
}
remove_link /usr/bin/zilf
remove_link /usr/bin/zapf
remove_link /usr/bin/zilfpub
exit 0
'@

$postInstallFile = New-TempScript -Content $postInstall
$preRemoveFile   = New-TempScript -Content $preRemove

# Common fpm args
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
		'-C', $Source
)
if ($versionParts.iteration) { $common += @('--iteration', $versionParts.iteration) }

# Add runtime dependencies for native AOT .NET binaries
$debDeps = @('libicu76 | libicu75 | libicu74 | libicu73 | libicu72 | libicu71 | libicu70 | libicu69 | libicu68 | libicu67 | libicu66')
$rpmDeps = @('libicu')

$debCommon = $common + ($debDeps | ForEach-Object { @('-d', $_) })
$rpmCommon = $common + ($rpmDeps | ForEach-Object { @('-d', $_) })

# Build DEB
Write-Host "Building DEB: $debOut" -ForegroundColor Cyan
$debArgs = $debCommon + @('-t','deb','-a', $archMaps.deb, '-p', $debOut, '.')
& fpm @debArgs
if ($LASTEXITCODE -ne 0) { throw "fpm failed creating deb with exit code $LASTEXITCODE" }

# Build RPM
Write-Host "Building RPM: $rpmOut" -ForegroundColor Cyan
$rpmArgs = $rpmCommon + @('-t','rpm','-a', $archMaps.rpm, '-p', $rpmOut, '.')
& fpm @rpmArgs
if ($LASTEXITCODE -ne 0) { throw "fpm failed creating rpm with exit code $LASTEXITCODE" }

Write-Host "Packages created:" -ForegroundColor Green
Write-Host "  $debOut"
Write-Host "  $rpmOut"

