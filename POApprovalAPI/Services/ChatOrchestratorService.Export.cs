using System.Text.Json;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private static readonly JsonSerializerOptions ExportJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ChatExportResult> ExportAsync(ChatExportRequest request, CancellationToken ct = default)
    {
        if (request.ExportContext != null && !string.IsNullOrWhiteSpace(request.ExportContext.Kind))
            return await ExportFromContextAsync(request.ExportContext, ct);

        if (string.IsNullOrWhiteSpace(request.Sql))
            throw new ArgumentException("Sql or exportContext is required.");

        return await ExportCsvAsync(request.Sql, ct);
    }

    private async Task<ChatExportResult> ExportFromContextAsync(ChatExportContext context, CancellationToken ct)
    {
        List<Dictionary<string, object?>> rows;
        int? totalCount;

        switch (context.Kind)
        {
            case ChatExportKinds.ErpAgeing:
            {
                var plan = context.Plan.Deserialize<AgeingReportPlan>(ExportJsonOptions)
                    ?? throw new InvalidOperationException("Invalid ageing export plan.");
                plan.MaxRows = MaxExportRows;
                var result = await _ageingService.ExecuteAsync(plan, ct);
                rows = result.Rows;
                totalCount = result.TotalCount ?? rows.Count;
                break;
            }
            case ChatExportKinds.ErpFinance:
            {
                var plan = context.Plan.Deserialize<ErpFinanceReportPlan>(ExportJsonOptions)
                    ?? throw new InvalidOperationException("Invalid finance export plan.");
                plan.MaxRows = MaxExportRows;
                var result = await _financeReportService.ExecuteAsync(plan, ct);
                rows = result.Rows;
                totalCount = result.TotalCount ?? rows.Count;
                break;
            }
            case ChatExportKinds.ErpInventory:
            {
                var plan = context.Plan.Deserialize<ErpInventoryReportPlan>(ExportJsonOptions)
                    ?? throw new InvalidOperationException("Invalid inventory export plan.");
                plan.MaxRows = MaxExportRows;
                var result = await _inventoryReportService.ExecuteAsync(plan, ct);
                rows = result.Rows;
                totalCount = result.TotalCount ?? rows.Count;
                break;
            }
            case ChatExportKinds.ErpLedgerStatement:
            {
                var plan = context.Plan.Deserialize<LedgerStatementPlan>(ExportJsonOptions)
                    ?? throw new InvalidOperationException("Invalid ledger statement export plan.");
                plan.MaxRows = MaxExportRows;
                var result = await _ledgerStatementChat.ExecuteAsync(plan, ct);
                rows = result.Rows;
                totalCount = result.TotalCount ?? rows.Count;
                break;
            }
            default:
                throw new InvalidOperationException($"Unsupported export kind: {context.Kind}");
        }

        var truncated = totalCount > rows.Count || rows.Count >= MaxExportRows;
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        return new ChatExportResult
        {
            CsvBytes = BuildCsvBytes(rows),
            FileName = $"assistant-export-{stamp}.csv",
            RowCount = rows.Count,
            TotalCount = totalCount,
            Truncated = truncated,
        };
    }

    private static ChatExportContext? BuildExportContext(
        AgeingReportPlan? ageingPlan,
        ErpFinanceReportPlan? financePlan,
        ErpInventoryReportPlan? inventoryPlan,
        LedgerStatementPlan? ledgerPlan)
    {
        if (ageingPlan != null)
        {
            return new ChatExportContext
            {
                Kind = ChatExportKinds.ErpAgeing,
                Plan = JsonSerializer.SerializeToElement(ageingPlan, ExportJsonOptions),
            };
        }

        if (financePlan != null)
        {
            return new ChatExportContext
            {
                Kind = ChatExportKinds.ErpFinance,
                Plan = JsonSerializer.SerializeToElement(financePlan, ExportJsonOptions),
            };
        }

        if (inventoryPlan != null)
        {
            return new ChatExportContext
            {
                Kind = ChatExportKinds.ErpInventory,
                Plan = JsonSerializer.SerializeToElement(inventoryPlan, ExportJsonOptions),
            };
        }

        if (ledgerPlan != null)
        {
            return new ChatExportContext
            {
                Kind = ChatExportKinds.ErpLedgerStatement,
                Plan = JsonSerializer.SerializeToElement(ledgerPlan, ExportJsonOptions),
            };
        }

        return null;
    }
}
