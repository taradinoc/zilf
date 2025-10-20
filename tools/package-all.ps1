param(
    [string[]]$RuntimeIdentifiers = @('win-x64', 'linux-x64', 'osx-x64', 'osx-arm64'),
    [string]$Configuration = 'Release',
    [switch]$StageOnly
)

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    $stageBase = Join-Path $repoRoot "Package/$Configuration/Stage"
    $packagesDir = Join-Path $repoRoot "Package/$Configuration/Packages"

    foreach ($rid in $RuntimeIdentifiers) {
        Write-Host "Staging $rid..." -ForegroundColor Cyan
        $arguments = @('Build.proj', '-t:Stage', "-p:Configuration=$Configuration", "-p:RuntimeIdentifier=$rid")
        & dotnet msbuild @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Stage failed for $rid with exit code $LASTEXITCODE."
        }

        if (-not (Test-Path $stageBase)) {
            throw "Stage directory '$stageBase' was not created."
        }

        $stageDir = Get-ChildItem -Path $stageBase -Directory | Where-Object { $_.Name.ToLower().EndsWith("-$rid") } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if (-not $stageDir) {
            throw "Could not locate staged output for RID '$rid' under '$stageBase'."
        }

        Write-Host "Stage directory: $($stageDir.FullName)" -ForegroundColor DarkGray

        if ($StageOnly) {
            continue
        }

        if (-not (Test-Path $packagesDir)) {
            New-Item -ItemType Directory -Path $packagesDir | Out-Null
        }

        $isWindowsRid = $rid -like 'win-*'
        $packageExtension = if ($isWindowsRid) { '.zip' } else { '.tar.gz' }
        $packagePath = Join-Path $packagesDir ($stageDir.Name + $packageExtension)
        if (Test-Path $packagePath) {
            Remove-Item $packagePath
        }

        if ($isWindowsRid) {
            Write-Host "Creating ZIP package $packagePath" -ForegroundColor Cyan
            Push-Location $stageDir.FullName
            try {
                Compress-Archive -Path * -DestinationPath $packagePath -Force
            }
            finally {
                Pop-Location
            }
        }
        else {
            Write-Host "Creating TAR package $packagePath" -ForegroundColor Cyan
            $tarCommand = Get-Command tar -ErrorAction SilentlyContinue
            if (-not $tarCommand) {
                throw "tar executable not found. Cannot create tarball for RID '$rid'."
            }

            $stageParent = Split-Path -Parent $stageDir.FullName
            $stageFolderName = Split-Path -Leaf $stageDir.FullName

            & $tarCommand.Source -czf $packagePath -C $stageParent $stageFolderName
            if ($LASTEXITCODE -ne 0) {
                throw "tar failed for RID '$rid' with exit code $LASTEXITCODE."
            }
        }
    }
}
finally {
    Pop-Location
}
