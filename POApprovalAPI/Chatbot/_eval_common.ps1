function Test-EvalCheck {
    param([string]$check, $resp)

    $sql = [string]$resp.sql
    $answer = [string]$resp.answer
    $rowCount = [int]$resp.rowCount

    if ($check -like "sql_has:*") {
        foreach ($a in ($check.Substring(8) -split '\|')) {
            if ($sql -like "*$a*") { return $true }
        }
        return $false
    }
    if ($check -like "sql_not_has:*") {
        foreach ($a in ($check.Substring(12) -split '\|')) {
            if ($sql -like "*$a*") { return $false }
        }
        return $true
    }
    if ($check -like "rows_gt:*") {
        return $rowCount -gt [int]$check.Substring(8)
    }
    if ($check -eq "rows_gte:1") {
        return $rowCount -ge 1
    }
    if ($check -eq "answer_not_empty") {
        return -not [string]::IsNullOrWhiteSpace($answer)
    }
    if ($check -eq "governed_warning") {
        return -not [string]::IsNullOrWhiteSpace([string]$resp.warning)
    }
    return $false
}

function Invoke-EvalSuite {
    param(
        [string]$SuiteName,
        [array]$Cases,
        [string]$BaseUrl,
        [int]$SleepSeconds,
        [string]$ResultsFile
    )

    $results = @()
    $pass = 0
    $fail = 0

    Write-Host "Evaluating $($Cases.Count) $SuiteName cases against $BaseUrl ..."
    foreach ($c in $Cases) {
        Write-Host "`n=== $($c.id) ==="
        Write-Host "Q: $($c.message)"
        try {
            $body = @{ message = $c.message; topK = 4 } | ConvertTo-Json
            $resp = Invoke-RestMethod -Uri "$BaseUrl/api/chat" -Method POST -ContentType "application/json" -Body $body -TimeoutSec 180
            $failed = @()
            foreach ($chk in $c.checks) {
                if (-not (Test-EvalCheck $chk $resp)) { $failed += $chk }
            }
            $ok = $failed.Count -eq 0
            if ($ok) { $pass++; Write-Host "PASS" -ForegroundColor Green }
            else { $fail++; Write-Host "FAIL: $($failed -join ', ')" -ForegroundColor Red }
            Write-Host "SQL: $($resp.sql)"
            Write-Host "Rows: $($resp.rowCount)  Warning: $($resp.warning)"
            if ($resp.answer) {
                $preview = $resp.answer.Substring(0, [Math]::Min(180, $resp.answer.Length))
                Write-Host "Answer: $preview..."
            }
            $results += [pscustomobject]@{
                id = $c.id
                pass = $ok
                failedChecks = ($failed -join ';')
                rowCount = $resp.rowCount
                warning = $resp.warning
                sql = $resp.sql
                answer = $resp.answer
            }
        }
        catch {
            $fail++
            Write-Host "ERROR: $_" -ForegroundColor Red
            $results += [pscustomobject]@{
                id = $c.id
                pass = $false
                failedChecks = "http_error"
                rowCount = 0
                warning = "$_"
                sql = ""
                answer = ""
            }
        }
        if ($SleepSeconds -gt 0) { Start-Sleep -Seconds $SleepSeconds }
    }

    $out = Join-Path $PSScriptRoot $ResultsFile
    $results | ConvertTo-Json -Depth 6 | Set-Content -Path $out -Encoding UTF8
    Write-Host "`n$SuiteName PASS $pass / $($Cases.Count)   FAIL $fail"
    Write-Host "Wrote $out"
    return $fail
}
