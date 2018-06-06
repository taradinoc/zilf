$zilfPath = "..\..\Zilf\bin\Debug\zilf.exe"
$zapfPath = "..\..\Zapf\bin\Debug\zapf.exe"
$czlrPath = ".\ConsoleZLR.exe"
$includeDir = ".."

function Invoke-Zilf {
    param ([string]$SrcFile = $(throw "SrcFile parameter is required."))
    $output = (& $zilfPath -ip $includeDir $SrcFile 2>&1)
    if ($LASTEXITCODE -eq 0) {
        return $true
    } else {
        Write-Error ($output | Out-String)
        return $false
    }
}

function Invoke-Zapf {
    param ([string]$SrcFile = $(throw "SrcFile parameter is required."))
    $output = (& $zapfPath $SrcFile 2>&1)
    if ($LASTEXITCODE -eq 0) {
        return $true
    } else {
        Write-Error ($output | Out-String)
        return $false
    }
}

function Invoke-ZLR {
    param ([string]$StoryFile = $(throw "StoryFile parameter is required."))
    & $czlrPath -nowait -dumb $StoryFile
}

function Test-Scenario {
    param ($TestName = $(throw "TestName parameter is required."),
           [switch]$Silent = $false)

    $testFile = ".\test-" + $TestName + ".zil"

    Write-Progress -Activity "Testing $TestName" -Status "Compiling $testFile" -Id 1
    if (Invoke-Zilf $testFile) {
        $zapFile = [io.path]::ChangeExtension($testFile, ".zap")
        Write-Progress -Activity "Testing $TestName" -Status "Assembling $zapFile" -Id 1
        if (Invoke-Zapf $zapFile) {
            $storyFile = [io.path]::ChangeExtension($zapFile, ".z3")
            Write-Progress -Activity "Testing $testFile" -Status "Executing $storyFile" -Id 1
            $output = $(Invoke-ZLR $storyFile)
            if ($output -match "^PASS$") {
                Write-Progress -Activity "Testing $testFile" -Status "OK"
                return $true
            } else {
                Write-Progress -Activity "Testing $testFile" -Status "Failed"
                if (!$Silent) {
                    Write-Host ($output | Out-String)
                    return
                }
            }
        }
    }
    return $false
}

Set-Alias test Test-Scenario

function Get-ScenarioNames {
    Get-ChildItem test-*.zil | ForEach-Object { $_.Name -replace '^test-(.*)\.zil$', '$1' }
}

function Test-Scenarios {
    $testNames = Get-ScenarioNames
    $completed = 0

    foreach ($t in $testNames) {
        Write-Progress -Activity "Running all tests" -Status $t -PercentComplete (($completed) * 100 / $testNames.Count)

        if (Test-Scenario $t -Silent) {$status = "OK"} else {$status = "Fail"}
        $completed++

        $hash = @{Name=$t; Status=$status}
        New-Object PSObject -Property $hash
    }
}

Set-Alias testall Test-Scenarios
