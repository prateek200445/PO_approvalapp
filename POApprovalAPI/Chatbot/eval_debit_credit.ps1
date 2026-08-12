# Wave 5 â€” Debit/Credit note golden eval (run when Groq quota allows)
param([string]$BaseUrl = "http://localhost:5115")
$ErrorActionPreference = "Stop"

$cases = @(
  @{
    id = "oswal_debit_notes"
    message = "Show recent debit notes for Oswal Extrusion Limited with party and amount"
    checks = @("sql_has:DebitNote|vw_DebitNote", "sql_has:Oswal", "sql_has:TotalDebitAmount|PartyName", "rows_gt:0")
  },
  @{
    id = "debit_by_number"
    message = "Show debit note OEL/DB/26-27/16 amount party and type"
    checks = @("sql_has:DebitNote|vw_DebitNote", "sql_has:OEL/DB/26-27/16", "rows_gt:0")
  },
  @{
    id = "ppl_credit_notes"
    message = "List credit notes for Plastene Polyfilms Limited with total credit amount and party"
    checks = @("sql_has:CreditNote|vw_creditnote", "sql_has:Polyfilms|PPL", "sql_has:TotalCredit|totalcredit|Party", "rows_gt:0")
  },
  @{
    id = "credit_by_number"
    message = "Details of credit note PPL/CR/26-27/9"
    checks = @("sql_has:CreditNote|vw_creditnote", "sql_has:PPL/CR/26-27/9", "rows_gt:0")
  },
  @{
    id = "messy_dn"
    message = "oswal extrusion debit notes provisional amounts"
    checks = @("sql_has:DebitNote|vw_DebitNote", "sql_has:Oswal", "rows_gt:0")
  },
  @{
    id = "messy_cn"
    message = "polyfilms credit notes to commercial bag company"
    checks = @("sql_has:CreditNote|vw_creditnote", "rows_gt:0")
  }
)

function Test-Check([string]$check, $resp) {
  $sql = [string]$resp.sql
  $answer = [string]$resp.answer
  $rowCount = [int]$resp.rowCount
  if ($check -like "sql_has:*") {
    foreach ($a in ($check.Substring(8) -split '\|')) { if ($sql -like "*$a*") { return $true } }
    return $false
  }
  if ($check -like "sql_not:*") { return -not ($sql -match $check.Substring(8)) }
  if ($check -like "rows_gt:*") { return $rowCount -gt [int]$check.Substring(8) }
  if ($check -eq "answer_not_no_data") {
    if ($rowCount -le 0) { return $false }
    return -not ($answer.ToLowerInvariant() -match 'no (rows|records|data|notes).*found')
  }
  return $false
}

$results=@(); $pass=0; $fail=0
Write-Host "Evaluating $($cases.Count) Debit/Credit note cases against $BaseUrl ..."
foreach ($c in $cases) {
  Write-Host "`n=== $($c.id) ==="
  Write-Host "Q: $($c.message)"
  try {
    $body = @{ message = $c.message; topK = 4 } | ConvertTo-Json
    $resp = Invoke-RestMethod -Uri "$BaseUrl/api/chat" -Method POST -ContentType "application/json" -Body $body -TimeoutSec 180
  } catch {
    $resp = [pscustomobject]@{ sql=""; answer="$_"; rowCount=0; warning="$_" }
    Write-Host "ERROR: $_" -ForegroundColor Red
  }
  $failed=@()
  foreach ($chk in $c.checks) { if (-not (Test-Check $chk $resp)) { $failed += $chk } }
  if ($failed.Count -eq 0) { $pass++; Write-Host "PASS" -ForegroundColor Green }
  else { $fail++; Write-Host "FAIL: $($failed -join ', ')" -ForegroundColor Red }
  Write-Host "SQL: $($resp.sql)"
  Write-Host "Rows: $($resp.rowCount)  Warning: $($resp.warning)"
  $ans=[string]$resp.answer; if ($ans.Length -gt 220) { $ans = $ans.Substring(0,220)+"..." }
  Write-Host "Answer: $ans"
  $results += [pscustomobject]@{ id=$c.id; pass=($failed.Count -eq 0); failedChecks=($failed -join ';'); sql=$resp.sql; rowCount=$resp.rowCount; answer=$resp.answer; warning=$resp.warning }
  Start-Sleep -Seconds 60
}
$out = Join-Path $PSScriptRoot "eval_debit_credit_results.json"
$results | ConvertTo-Json -Depth 6 | Set-Content $out -Encoding UTF8
Write-Host "`nPASS $pass / $($cases.Count)   FAIL $fail"
Write-Host "Wrote $out"
if ($fail -gt 0) { exit 1 } else { exit 0 }
