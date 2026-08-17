using System.Text.Json;
using ClosedXML.Excel;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services.FinancialStatements;

public class FinancialStatementService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly TrialBalanceExcelService _tbExcel;
    private readonly LedgerGroupMappingService _mappingService;
    private readonly FinancialStatementEngine _engine;
    private readonly FinancialStatementTemplateStore _templates;
    private readonly FinancialStatementTemplateExporter _templateExporter;
    private readonly WorkbookPresentationRulesExtractor _workbookRulesExtractor;
    private readonly PresentationRulesService _presentationRules;

    public FinancialStatementService(
        TrialBalanceExcelService tbExcel,
        LedgerGroupMappingService mappingService,
        FinancialStatementEngine engine,
        FinancialStatementTemplateStore templates,
        FinancialStatementTemplateExporter templateExporter,
        WorkbookPresentationRulesExtractor workbookRulesExtractor,
        PresentationRulesService presentationRules)
    {
        _tbExcel = tbExcel;
        _mappingService = mappingService;
        _engine = engine;
        _templates = templates;
        _templateExporter = templateExporter;
        _workbookRulesExtractor = workbookRulesExtractor;
        _presentationRules = presentationRules;
    }

    public TrialBalancePreviewResponse Preview(Stream stream, string fileName, string? sheetName, int? headerRow)
        => _tbExcel.Preview(stream, fileName, sheetName, headerRow);

    public FinancialStatementResultDto GenerateFromStream(
        Stream stream,
        GenerateFinancialStatementRequest request)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var rows = _tbExcel.ParseWorkbook(workbook, request.Mapping);
        var companyKey = string.IsNullOrWhiteSpace(request.CompanyKey) ? "default" : request.CompanyKey.Trim();

        var fileRules = _presentationRules.GetRules(companyKey);
        var workbookRules = _workbookRulesExtractor.Extract(workbook, companyKey, bytes);
        var rules = PresentationRulesMerger.Merge(fileRules, workbookRules);

        return GenerateFromRows(rows, request, rules, workbook);
    }

    public FinancialStatementResultDto GenerateFromRows(
        IReadOnlyList<TrialBalanceRowDto> rows,
        GenerateFinancialStatementRequest request,
        PresentationRules? rulesOverride = null,
        XLWorkbook? sourceWorkbook = null)
    {
        var companyKey = string.IsNullOrWhiteSpace(request.CompanyKey) ? "default" : request.CompanyKey.Trim();
        var lookup = _mappingService.BuildLookup(companyKey, rows);

        if (sourceWorkbook != null)
            _workbookRulesExtractor.MergeGroupSheetLookup(sourceWorkbook, lookup);

        if (request.OverrideMappings != null)
        {
            foreach (var map in request.OverrideMappings)
            {
                if (string.IsNullOrWhiteSpace(map.Ledger) || string.IsNullOrWhiteSpace(map.Group))
                    continue;
                lookup[map.Ledger.Trim()] = map.Group.Trim();
            }
        }

        var companyName = string.IsNullOrWhiteSpace(request.CompanyName)
            ? companyKey
            : request.CompanyName.Trim();

        var rules = rulesOverride ?? PresentationRulesMerger.Merge(
            _presentationRules.GetRules(companyKey),
            sourceWorkbook != null ? _workbookRulesExtractor.Extract(sourceWorkbook, companyKey) : null);

        return _engine.Generate(
            companyKey,
            companyName,
            request.PeriodLabel?.Trim() ?? "",
            rows,
            lookup,
            rules);
    }

    public IReadOnlyList<LedgerGroupMappingDto> GetMappings(string companyKey)
        => _mappingService.GetMappings(companyKey);

    public List<CompanyMappingSummaryDto> ListCompanies()
        => _mappingService.ListCompanies();

    public void SaveMappings(SaveMappingRequest request)
        => _mappingService.SaveMappings(request);

    public IReadOnlyList<string> GetScheduleGroups()
        => _templates.GetAllScheduleGroups();

    public byte[] ExportExcel(FinancialStatementResultDto result)
    {
        if (_templateExporter.HasTemplate(result.CompanyKey))
            return _templateExporter.Export(result);

        using var workbook = new XLWorkbook();

        WriteSchedulesSheet(workbook, result);
        WriteBalanceSheetSheet(workbook, result);
        WriteProfitLossSheet(workbook, result);
        WriteUnmappedSheet(workbook, result);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteSchedulesSheet(XLWorkbook workbook, FinancialStatementResultDto result)
    {
        var ws = workbook.Worksheets.Add("Schedules");
        ws.Cell(1, 1).Value = result.CompanyName;
        ws.Cell(2, 1).Value = result.PeriodLabel;
        var row = 4;

        ws.Cell(row, 1).Value = "Note";
        ws.Cell(row, 2).Value = "Schedule";
        ws.Cell(row, 3).Value = "Line item";
        ws.Cell(row, 4).Value = "Amount (Lakhs)";
        ws.Cell(row, 5).Value = "Ledgers";
        ws.Range(row, 1, row, 5).Style.Font.Bold = true;
        row++;

        foreach (var note in result.Schedules)
        {
            foreach (var line in note.Lines)
            {
                ws.Cell(row, 1).Value = note.Note;
                ws.Cell(row, 2).Value = note.Title;
                ws.Cell(row, 3).Value = line.Label;
                ws.Cell(row, 4).Value = line.AmountLakhs;
                ws.Cell(row, 5).Value = line.LedgerCount;
                row++;
            }

            ws.Cell(row, 2).Value = $"{note.Title} — Total";
            ws.Cell(row, 4).Value = note.TotalLakhs;
            ws.Range(row, 2, row, 4).Style.Font.Bold = true;
            row += 2;
        }

        ws.Columns().AdjustToContents();
    }

    private static void WriteBalanceSheetSheet(XLWorkbook workbook, FinancialStatementResultDto result)
    {
        var ws = workbook.Worksheets.Add("Balance Sheet");
        ws.Cell(1, 1).Value = result.CompanyName;
        ws.Cell(2, 1).Value = "Balance Sheet";
        ws.Cell(3, 1).Value = result.PeriodLabel;
        var row = 5;

        ws.Cell(row, 1).Value = "Particulars";
        ws.Cell(row, 2).Value = "Note";
        ws.Cell(row, 3).Value = "Amount (Lakhs)";
        ws.Range(row, 1, row, 3).Style.Font.Bold = true;
        row++;

        foreach (var section in result.BalanceSheet)
        {
            ws.Cell(row, 1).Value = section.Title;
            ws.Range(row, 1, row, 3).Style.Font.Bold = true;
            row++;

            foreach (var line in section.Lines)
            {
                if (line.IsHeader)
                {
                    ws.Cell(row, 1).Value = line.Label;
                    ws.Cell(row, 1).Style.Font.Italic = true;
                }
                else
                {
                    ws.Cell(row, 1).Value = line.Label;
                    ws.Cell(row, 2).Value = line.Note;
                    ws.Cell(row, 3).Value = line.AmountLakhs;
                }
                row++;
            }

            ws.Cell(row, 1).Value = $"Total — {section.Title}";
            ws.Cell(row, 3).Value = section.SectionTotalLakhs;
            ws.Range(row, 1, row, 3).Style.Font.Bold = true;
            row += 2;
        }

        ws.Columns().AdjustToContents();
    }

    private static void WriteProfitLossSheet(XLWorkbook workbook, FinancialStatementResultDto result)
    {
        var ws = workbook.Worksheets.Add("P&L");
        ws.Cell(1, 1).Value = result.CompanyName;
        ws.Cell(2, 1).Value = "Statement of Profit and Loss";
        ws.Cell(3, 1).Value = result.PeriodLabel;
        var row = 5;

        ws.Cell(row, 1).Value = "Particulars";
        ws.Cell(row, 2).Value = "Note";
        ws.Cell(row, 3).Value = "Amount (Lakhs)";
        ws.Range(row, 1, row, 3).Style.Font.Bold = true;
        row++;

        foreach (var line in result.ProfitAndLoss)
        {
            ws.Cell(row, 1).Value = line.Label;
            ws.Cell(row, 2).Value = line.Note;

            if (!line.IsHeader)
                ws.Cell(row, 3).Value = line.AmountLakhs;

            if (line.IsHeader)
                ws.Cell(row, 1).Style.Font.Italic = true;
            if (line.IsSubtotal)
                ws.Range(row, 1, row, 3).Style.Font.Bold = true;

            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private static void WriteUnmappedSheet(XLWorkbook workbook, FinancialStatementResultDto result)
    {
        var ws = workbook.Worksheets.Add("Unmapped");
        ws.Cell(1, 1).Value = "Unmapped ledgers";
        ws.Cell(2, 1).Value = result.UnmappedLedgers;
        var row = 4;

        ws.Cell(row, 1).Value = "Ledger";
        ws.Cell(row, 2).Value = "Closing";
        ws.Cell(row, 3).Value = "Closing (Lakhs)";
        ws.Range(row, 1, row, 3).Style.Font.Bold = true;
        row++;

        foreach (var item in result.Unmapped)
        {
            ws.Cell(row, 1).Value = item.Ledger;
            ws.Cell(row, 2).Value = item.Closing;
            ws.Cell(row, 3).Value = item.ClosingLakhs;
            row++;
        }

        ws.Columns().AdjustToContents();
    }
}
