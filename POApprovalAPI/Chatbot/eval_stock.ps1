# Wave 4 â€” Warehouse / stock golden eval (run when Groq quota allows)
param([string]$BaseUrl = "http://localhost:5115")
$ErrorActionPreference = "Stop"

$cases = @(
  @{
    id = "stock_item_wip00013"
    message = "What is stock in hand for item WIP00013 at Oswal Extrusion Limited by warehouse?"
    checks = @("sql_has:WareHouse|vw_itemwiseStock|vw_inventoryitemwarehouse", "sql_has:WIP00013", "sql_has:Oswal", "sql_has:StkInHand", "rows_gt:0")
  },
  @{
    id = "oswal_top_stock"
    message = "Show top stock items by stock in hand for Oswal Extrusion Limited"
    checks = @("sql_has:WareHouse|vw_itemwiseStock", "sql_has:CompanyName", "sql_has:StkInHand", "sql_not:CompName", "rows_gt:0")
  },
  @{
    id = "below_reorder"
    message = "List items below reorder level at Oswal Extrusion Limited"
    checks = @("sql_has:WareHouse", "sql_has:ReOrder", "sql_has:StkInHand", "sql_has:Oswal", "rows_gt:0")
  },
  @{
    id = "godown_list"
    message = "List warehouses or godowns for Oswal Extrusion Limited"
    checks = @("sql_has:WareHouseMaster|WareHouse|vw_itemwiseStock", "sql_has:Oswal", "rows_gt:0")
  },
  @{
    id = "stock_by_group"
    message = "Show stock by group for Oswal Extrusion Limited FIBC items"
    checks = @("sql_has:vw_inventoryitemwarehouse|WareHouse", "sql_has:Oswal", "rows_gt:0")
  },
  @{
    id = "messy_stock"
    message = "oswal extrusion how much stock of WIP00013 in liner bag godown"
    checks = @("sql_has:WareHouse|vw_itemwiseStock", "sql_has:WIP00013", "rows_gt:0")
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
  if ($check -like "sql_not:*") {
    $ban = $check.Substring(8)
    if ($ban -eq "CompName") { return -not [regex]::IsMatch($sql, '(?i)\bCompName\b') }
    return -not ($sql -match $ban)
  }
  if ($check -like "rows_gt:*") { return $rowCount -gt [int]$check.Substring(8) }
  if ($check -eq "answer_not_no_data") {
    if ($rowCount -le 0) { return $false }
    return -not ($answer.ToLowerInvariant() -match 'no (rows|records|data|stock).*found')
  }
  return $false
}

$results=@(); $pass=0; $fail=0
Write-Host "Evaluating $($cases.Count) Warehouse/Stock cases against $BaseUrl ..."
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
$out = Join-Path $PSScriptRoot "eval_stock_results.json"
$results | ConvertTo-Json -Depth 6 | Set-Content $out -Encoding UTF8
Write-Host "`nPASS $pass / $($cases.Count)   FAIL $fail"
Write-Host "Wrote $out"
if ($fail -gt 0) { exit 1 } else { exit 0 }
