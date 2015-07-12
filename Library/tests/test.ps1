$zilfPath = "..\..\Zilf\bin\Debug\zilf.exe"
$zapfPath = "..\..\Zapf\bin\Debug\zapf.exe"
$czlrPath = ".\ConsoleZLR.exe"
$includeDir = ".."

function Compile-Zil {
    param ([string]$SrcFile = $(throw "SrcFile parameter is required."))
    $output = (& $zilfPath -ip $includeDir $SrcFile 2>&1)
    if ($LASTEXITCODE -eq 0) {
        return $true
    } else {
        Write-Error ($output | Out-String)
        return $false
    }
}

function Assemble-Zap {
    param ([string]$SrcFile = $(throw "SrcFile parameter is required."))
    $output = (& $zapfPath $SrcFile 2>&1)
    if ($LASTEXITCODE -eq 0) {
        return $true
    } else {
        Write-Error ($output | Out-String)
        return $false
    }
}

function Run-Zcode {
    param ([string]$StoryFile = $(throw "StoryFile parameter is required."))
    & $czlrPath -nowait -dumb $StoryFile
}

function Run-Test {
    param ($TestName = $(throw "TestName parameter is required."),
           [switch]$Silent = $false)
    
    $testFile = ".\test-" + $TestName + ".zil"
    
    Write-Progress -Activity "Testing $TestName" -Status "Compiling $testFile" -Id 1
    if (Compile-Zil $testFile) {
        $zapFile = [io.path]::ChangeExtension($testFile, ".zap")
        Write-Progress -Activity "Testing $TestName" -Status "Assembling $zapFile" -Id 1
        if (Assemble-Zap $zapFile) {
            $storyFile = [io.path]::ChangeExtension($zapFile, ".z3")
            Write-Progress -Activity "Testing $testFile" -Status "Executing $storyFile" -Id 1
            $output = $(Run-Zcode $storyFile)
            if ($output -match "^PASS$") {
                Write-Progress -Activity "Testing $testFile" -Status "Passed"
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

Set-Alias test Run-Test

function Get-TestNames {
    dir test-*.zil | foreach { $_.Name -replace '^test-(.*)\.zil$', '$1' }
}

function Test-All {
    $testNames = Get-TestNames
    $completed = 0
    
    foreach ($t in $testNames) {
        Write-Progress -Activity "Running all tests" -Status $t -PercentComplete (($completed) * 100 / $testNames.Count)
    
        if (Run-Test $t -Silent) {$status = "Pass"} else {$status = "Fail"}
        $completed++
        
        $hash = @{Name=$t; Status=$status}
        New-Object PSObject -Property $hash
    }
}
