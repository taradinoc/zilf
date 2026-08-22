hg files --include='src/**/*.cs' | ForEach-Object {
    $filePath = (Resolve-Path $_).Path
    
    # 1. Detect existing BOM status safely using raw bytes
    $bytes = [System.IO.File]::ReadAllBytes($filePath)
    if ($bytes.Length -lt 3) { return } # Skip empty or near-empty files
    
    $hasBom = ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    $encoding = if ($hasBom) { [System.Text.Encoding]::UTF8 } else { New-Object System.Text.UTF8Encoding($false) }

    # 2. Read the text with the detected encoding
    $text = [System.IO.File]::ReadAllText($filePath, $encoding)

    # 3. Count line endings to find the majority
    $crlfCount = ([regex]::Matches($text, "\r\n")).Count
    $lfCount = ([regex]::Matches($text, "(?<!\r)\n")).Count

    # Skip files that are already 100% homogeneous
    if ($crlfCount -eq 0 -or $lfCount -eq 0) { return }

    # 4. Normalize ONLY the rogue line endings to minimize git/hg diff churn
    if ($crlfCount -gt $lfCount) {
        # Majority is DOS: Only change standalone \n to \r\n
        $normalized = [regex]::Replace($text, "(?<!\r)\n", "`r`n")
        [System.IO.File]::WriteAllText($filePath, $normalized, $encoding)
        Write-Host "Fixed lone LF breaks in: $filePath" -ForegroundColor Yellow
    }
    else {
        # Majority is Unix: Only change \r\n to \n
        $normalized = $text -replace "`r`n", "`n"
        [System.IO.File]::WriteAllText($filePath, $normalized, $encoding)
        Write-Host "Fixed lone CRLF breaks in: $filePath" -ForegroundColor Cyan
    }
}
