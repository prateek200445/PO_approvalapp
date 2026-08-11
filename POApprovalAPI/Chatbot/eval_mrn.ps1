# MRN golden eval: posts user-style questions to /api/chat and checks SQL/rows heuristics.
# Usage: powershell -File eval_mrn.ps1 [-BaseUrl http://localhost:5115]

param(
  [string]$BaseUrl = "http://localhost:5115"
)

$ErrorActionPreference = "Stop"
$cases = @(
  @{
    id = "rm283_items"
    message = "What all materials came in under receipt RM 283, with quantities?"
    checks = @("sql_has:Vw_StoreInwards|StoreInwards", "sql_has:RM 283", "rows_gt:0", "answer_not_empty")
  },
  @{
    id = "rm283_vendor_bill"
    message = "For receipt RM 283, who is the vendor and what is the bill number and amount?"
    checks = @("sql_has:Vw_StoreInwards|StoreInwardsPayment", "sql_has:RM 283", "rows_gt:0", "answer_not_no_data")
  },
  @{
    id = "rm269_paid_utr"
    message = "Has receipt RM 269 already been paid? If yes, show the payment number and UTR"
    checks = @("sql_has:BillPaymentEntry|vw_MRNToBillPayment", "sql_has:RM 269", "rows_gt:0", "has_payment_no", "answer_not_no_payment")
  },
  @{
    id = "rm283_po"
    message = "Which purchase order was this material receipt RM 283 made against?"
    checks = @("sql_has:Vw_StoreInwards|StoreInwards", "sql_has:RM 283", "sql_has:PONo|PoNO|PONo", "rows_gt:0")
  },
  @{
    id = "plastene_party_receipts"
    message = "Show recent goods receipts for Plastene Polyfilms Ltd-Purchase with party and bill"
    checks = @("sql_has:PartyName|Partyname", "sql_not:CompanyName\s*=\s*'Plastene Polyfilms Ltd-Purchase'", "rows_gt:0", "answer_not_no_data")
  },
  @{
    id = "oswal_pending_qty"
    message = "For company Oswal Extrusion Limited, list material receipts that still have pending quantity to receive"
    checks = @("sql_has:CompanyName", "sql_has:PendingQty", "sql_not:PartyName\s*=\s*'Oswal", "rows_gt:0", "answer_not_no_data")
  },
  @{
    id = "bill_ppl_d_540"
    message = "Find receipts linked to bill number PPL/D/540"
    checks = @("sql_has:Vw_StoreInwards|StoreInwardsPayment|vw_MRNList", "sql_has:PPL/D/540", "sql_not_primary_bpe_bill", "rows_gt:0", "answer_not_no_data")
  },
  @{
    id = "rm269_payment_amount"
    message = "Was there any payment raised against material receipt RM 269 and for how much?"
    checks = @("sql_has:BillPaymentEntry|vw_MRNToBillPayment", "sql_has:RM 269", "rows_gt:0", "has_payment_no", "answer_not_no_payment")
  },
  @{
    id = "messy_rm283_items"
    message = "wat items came in mrn RM 283 show qty"
    checks = @("sql_has:RM 283", "rows_gt:0")
  },
  @{
    id = "messy_rm269_paid"
    message = "is rm 269 paid already? giv payment no and utr if ther"
    checks = @("sql_has:RM 269", "rows_gt:0", "has_payment_no", "answer_not_no_payment")
  },
  @{
    id = "messy_plastene"
    message = "recnt goods reciepts for plastene polyfilms ltd-purchase with party bill"
    checks = @("rows_gt:0", "answer_not_no_data")
  },
  @{
    id = "messy_oswal_pending"
    message = "oswal extrusion limited which mrns still have pending qty left"
    checks = @("sql_has:CompanyName|PendingQty", "rows_gt:0", "answer_not_no_data")
  },
  @{
    id = "messy_bill_ppl"
    message = "find material reciept for bill PPL/D/540"
    checks = @("sql_has:PPL/D/540", "rows_gt:0", "answer_not_no_data", "sql_not_primary_bpe_bill")
  },
  @{
    id = "messy_rm269_how_much"
    message = "any payment raised on mrn RM 269 how much money"
    checks = @("rows_gt:0", "has_payment_no", "answer_not_no_payment")
  }
)

function Test-Check([string]$check, $resp) {
  $sql = [string]$resp.sql
  $answer = [string]$resp.answer
  $rows = @($resp.rows)
  $rowCount = [int]$resp.rowCount

  if ($check -like "sql_has:*") {
    $alts = ($check.Substring(8) -split '\|')
    foreach ($a in $alts) {
      if ($sql -match [regex]::Escape($a) -or $sql -match $a) { return $true }
    }
    # also try case-insensitive contains for plain tokens
    foreach ($a in $alts) {
      if ($sql -like "*$a*") { return $true }
    }
    return $false
  }
  if ($check -like "sql_not:*") {
    $pat = $check.Substring(8)
    return -not ($sql -match $pat)
  }
  if ($check -eq "sql_not_primary_bpe_bill") {
    $usesStore = ($sql -match 'Vw_StoreInwards|StoreInwardsPayment|vw_MRNList') -and ($sql -match 'BillNo')
    $usesBpeBill = ($sql -match 'BillPaymentEntry') -and ($sql -match 'BillNo\s*=')
    if ($usesStore) { return $true }
    return -not $usesBpeBill
  }
  if ($check -like "rows_gt:*") {
    $n = [int]($check.Substring(8))
    return $rowCount -gt $n
  }
  if ($check -eq "has_payment_no") {
    foreach ($r in $rows) {
      $pn = $null
      if ($r.PSObject.Properties.Name -contains "PaymentNo") { $pn = $r.PaymentNo }
      elseif ($r -is [hashtable] -and $r.ContainsKey("PaymentNo")) { $pn = $r["PaymentNo"] }
      else {
        foreach ($p in $r.PSObject.Properties) {
          if ($p.Name -eq "PaymentNo") { $pn = $p.Value; break }
        }
      }
      if ($null -ne $pn -and "$pn".Trim() -ne "") { return $true }
    }
    return $false
  }
  if ($check -eq "answer_not_empty") {
    return -not [string]::IsNullOrWhiteSpace($answer)
  }
  if ($check -eq "answer_not_no_data") {
    $a = $answer.ToLowerInvariant()
    if ($rowCount -le 0) { return $false }
    return -not ($a -match 'no (receipts|records|rows|data|material)' -and $a -match 'found|returned|linked')
  }
  if ($check -eq "answer_not_no_payment") {
    $a = $answer.ToLowerInvariant()
    if ($rowCount -le 0) { return $false }
    # fail if claims no payment while we have payment rows
    if ($a -match 'no payment' -or $a -match 'not been paid' -or $a -match 'has not been paid') {
      # allow if it also lists payment numbers (unlikely)
      if ($a -match 'prq/') { return $true }
      return $false
    }
    return $true
  }
  return $false
}

$results = @()
$pass = 0
$fail = 0

Write-Host "Evaluating $($cases.Count) MRN cases against $BaseUrl ..."
foreach ($c in $cases) {
  Write-Host "`n=== $($c.id) ==="
  Write-Host "Q: $($c.message)"
  try {
    $body = @{ message = $c.message; topK = 3 } | ConvertTo-Json
    $resp = Invoke-RestMethod -Uri "$BaseUrl/api/chat" -Method POST -ContentType "application/json" -Body $body -TimeoutSec 180
    $failed = @()
    foreach ($chk in $c.checks) {
      if (-not (Test-Check $chk $resp)) { $failed += $chk }
    }
    $ok = $failed.Count -eq 0
    if ($ok) { $pass++; Write-Host "PASS" -ForegroundColor Green }
    else { $fail++; Write-Host "FAIL: $($failed -join ', ')" -ForegroundColor Red }
    Write-Host "SQL: $($resp.sql)"
    Write-Host "Rows: $($resp.rowCount)  Warning: $($resp.warning)"
    Write-Host "Answer: $($resp.answer.Substring(0, [Math]::Min(220, $resp.answer.Length)))..."
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
    $errBody = ""
    try {
      $ex = $_.Exception
      if ($ex.Response) {
        $reader = New-Object System.IO.StreamReader($ex.Response.GetResponseStream())
        $errBody = $reader.ReadToEnd()
      }
    } catch {}
    Write-Host "ERROR: $_ $errBody" -ForegroundColor Red
    $results += [pscustomobject]@{
      id = $c.id
      pass = $false
      failedChecks = "http_error"
      rowCount = 0
      warning = "$_ $errBody"
      sql = ""
      answer = ""
    }
  }
  Start-Sleep -Seconds 12
}

$out = Join-Path $PSScriptRoot "eval_mrn_results.json"
$results | ConvertTo-Json -Depth 6 | Set-Content -Path $out -Encoding UTF8
Write-Host "`n=============================="
Write-Host "PASS $pass / $($cases.Count)   FAIL $fail"
Write-Host "Wrote $out"
if ($fail -gt 0) { exit 1 } else { exit 0 }
