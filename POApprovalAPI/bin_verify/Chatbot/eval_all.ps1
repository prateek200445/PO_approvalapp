# Run all chatbot golden eval suites against a live API.
# Usage: powershell -File eval_all.ps1 [-BaseUrl http://localhost:5115] [-SleepSeconds 15] [-Suites po,payment,...]
param(
  [string]$BaseUrl = "http://localhost:5115",
  [int]$SleepSeconds = 15,
  [string[]]$Suites = @(
    "eval_po",
    "eval_payment",
    "eval_indent",
    "eval_ledger",
    "eval_finance",
    "eval_ops",
    "eval_inventory",
    "eval_sales",
    "eval_pr_quotation",
    "eval_mrn",
    "eval_stock",
    "eval_vendor",
    "eval_outward",
    "eval_production",
    "eval_despatch",
    "eval_gatepass",
    "eval_jobwork",
    "eval_debit_credit",
    "eval_users"
  )
)

$ErrorActionPreference = "Stop"
$totalFail = 0
$started = Get-Date

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Chatbot eval-all  BaseUrl=$BaseUrl" -ForegroundColor Cyan
Write-Host " SleepSeconds=$SleepSeconds  Suites=$($Suites.Count)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

foreach ($suite in $Suites) {
  $script = Join-Path $PSScriptRoot "$suite.ps1"
  if (-not (Test-Path $script)) {
    Write-Host "SKIP missing $suite.ps1" -ForegroundColor Yellow
    continue
  }
  Write-Host "`n>>> Running $suite.ps1" -ForegroundColor Cyan
  & $script -BaseUrl $BaseUrl -SleepSeconds $SleepSeconds
  if ($LASTEXITCODE -ne 0) {
    $totalFail++
    Write-Host ">>> $suite FAILED" -ForegroundColor Red
  }
  else {
    Write-Host ">>> $suite OK" -ForegroundColor Green
  }
}

$elapsed = (Get-Date) - $started
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " eval-all done in $([int]$elapsed.TotalMinutes)m $($elapsed.Seconds)s" -ForegroundColor Cyan
Write-Host " Failed suites: $totalFail / $($Suites.Count)" -ForegroundColor $(if ($totalFail -gt 0) { "Red" } else { "Green" })
Write-Host "========================================" -ForegroundColor Cyan

if ($totalFail -gt 0) { exit 1 } else { exit 0 }
