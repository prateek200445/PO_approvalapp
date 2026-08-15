# Wave 6 â€” Vendor master golden eval (run when Groq quota allows)
param([string]$BaseUrl = "http://localhost:5115")
$ErrorActionPreference = "Stop"

$cases = @(
  @{
    id = "bright_rubber_gst"
    message = "What is the GST number and email for vendor Bright Rubber?"
    checks = @("sql_has:Vendor|vw_VendorListwithBankdtls", "sql_has:Bright Rubber", "sql_has:NewGSTNo|GST|Email", "governed_warning", "rows_gt:0")
  },
  @{
    id = "chemline_bank"
    message = "Show bank account IFSC and payment terms for Chemline India Ltd"
    checks = @("sql_has:Vendor|vw_VendorListwithBankdtls", "sql_has:Chemline", "sql_has:IFSC|Bank", "rows_gt:0")
  },
  @{
    id = "vendor_code_lohia"
    message = "What is the vendor code for Lohia Corp Limited Gujarat?"
    checks = @("sql_has:Vendor", "sql_has:Lohia", "sql_has:VendorCode", "rows_gt:0")
  },
  @{
    id = "msme_vendors"
    message = "List MSME vendors with MSME number and firm name"
    checks = @("sql_has:Vendor|vendordata|vw_VendorListwithBankdtls", "sql_has:MSME|ISMSME", "rows_gt:0")
  },
  @{
    id = "messy_vendor"
    message = "bright rubber ka gst aur email kya hai"
    checks = @("sql_has:Vendor|vw_VendorListwithBankdtls|vendordata", "rows_gt:0")
  },
  @{
    id = "internal_vendors"
    message = "Which companies are listed as internal vendors?"
    checks = @("sql_has:InternalVendor", "rows_gt:0")
  },
  @{
    id = "bright_rubber_rates"
    message = "Show latest item rates from vendor Bright Rubber with Rate and NegoRate"
    checks = @("sql_has:VendorRate", "sql_has:Bright Rubber", "sql_has:Rate", "governed_warning", "rows_gt:0")
  },
  @{
    id = "item_wip00013_vendors"
    message = "Which vendors have rates for item WIP00013 and at what rates?"
    checks = @("sql_has:VendorRate|Vw_VendorItem|Vw_Quotation", "sql_has:WIP00013", "rows_gt:0")
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
    return -not ($answer.ToLowerInvariant() -match 'no (rows|records|data|vendors).*found')
  }
  if ($check -eq "governed_warning") {
    return -not [string]::IsNullOrWhiteSpace([string]$resp.warning)
  }
  return $false
}

$results=@(); $pass=0; $fail=0
Write-Host "Evaluating $($cases.Count) Vendor cases against $BaseUrl ..."
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
$out = Join-Path $PSScriptRoot "eval_vendor_results.json"
$results | ConvertTo-Json -Depth 6 | Set-Content $out -Encoding UTF8
Write-Host "`nPASS $pass / $($cases.Count)   FAIL $fail"
Write-Host "Wrote $out"
if ($fail -gt 0) { exit 1 } else { exit 0 }
