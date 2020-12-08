$slnDir = if (Test-Path env:ZILF_SLN_PATH) { $env:ZILF_SLN_PATH } else { "..\.." }
$zilfProjectPath = $slnDir + "\src\Zilf\Zilf.csproj"
$zapfProjectPath = $slnDir + "\src\Zapf\Zapf.csproj"
$includeDir = $slnDir + "\zillib"
$testsDir = $slnDir + "\zillib\tests"

$zlrSlnDir = if (Test-Path env:ZLR_SLN_PATH) { $env:ZLR_SLN_PATH } else { $slnDir + "\..\ZLR" }
$zlrProjectPath = $zlrSlnDir + "\ConsoleZLR\ConsoleZLR.csproj"

function Invoke-Zilf {
    param ([string]$SrcFile = $(throw "SrcFile parameter is required."))
    $output = (& dotnet run --project $zilfProjectPath -- -ip $includeDir $SrcFile 2>&1)
    if ($LASTEXITCODE -eq 0) {
        return $true
    } else {
        Write-Error ($output | Out-String)
        return $false
    }
}

Set-Alias izilf Invoke-Zilf

function Invoke-Zapf {
    param ([string]$SrcFile = $(throw "SrcFile parameter is required."))
    $output = (& dotnet run --project $zapfProjectPath -- $SrcFile 2>&1)
    if ($LASTEXITCODE -eq 0) {
        return $true
    } else {
        Write-Error ($output | Out-String)
        return $false
    }
}

Set-Alias izapf Invoke-Zapf

function Invoke-ZLR {
    param ([string]$StoryFile = $(throw "StoryFile parameter is required."))
    & dotnet run --project $zlrProjectPath -- -nowait -dumb $StoryFile
}

Set-Alias izlr Invoke-ZLR

function Test-Scenario {
    param ($TestName = $(throw "TestName parameter is required."),
           [switch]$Silent = $false)

    $testFile = $testsDir + "\test-" + $TestName + ".zil"

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
Set-Alias ts Test-Scenario

function Get-Scenarios {
    Get-ChildItem ($testsDir + "\test-*.zil") | ForEach-Object { $_.Name -replace '^.*test-(.*)\.zil$', '$1' }
}

Set-Alias gss Get-Scenarios

function Test-Scenarios {
    $testNames = Get-Scenarios
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
Set-Alias tss Test-Scenarios
