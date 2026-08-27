# Phase 1 chatbot accuracy — portal parity and eval coverage

## Governed query sources (Sales Dashboard parity)

| User intent | ERP source | Status |
|-------------|------------|--------|
| Country-wise sales by FY | `vw_Countrywise_sales_dashboard` | Wired |
| Total sales by FY | `vw_Sales_EBIDTA` (excl. Intergroup) | Wired |
| Sales by product group / sub-group | `vw_Sales_EBIDTA` | Wired |
| Total purchase by FY | `vw_Purchase_EBIDTA` | Wired |
| Top export customers | `vw_Salesvoucher` | Wired |
| Ledger count | `LedgerMaster` COUNT | Wired |
| Ledger / account groups | `LedgerMaster.Under` | Wired |
| Full ledger voucher statement | `sp_ac_LedgerSummary_BankRecoDate` | **Not in chat** (SELECT-only) |

## Eval suites

Run against a live API (`dotnet run` in POApprovalAPI):

```powershell
cd POApprovalAPI\Chatbot

# Single domain
powershell -File eval_po.ps1 -BaseUrl http://localhost:5115 -SleepSeconds 15

# All suites (Phase 1 core + existing)
powershell -File eval_all.ps1 -BaseUrl http://localhost:5115 -SleepSeconds 15

# Quick smoke (core approval + ledger + sales dashboard)
powershell -File eval_all.ps1 -Suites eval_po,eval_payment,eval_indent,eval_ledger,eval_sales
```

Results JSON written next to each script (`eval_*_results.json`).

## Requirements

- API running on port 5115 (or pass `-BaseUrl`)
- Python + fastembed for schema RAG
- `GEMINI_API_KEY` or `GROQ_API_KEY` in `POApprovalAPI/.env`
- SQL Server connectivity (live ERP)

Exit code 1 if any case fails.
