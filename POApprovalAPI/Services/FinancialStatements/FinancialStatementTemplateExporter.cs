using System.Text.Json;
using ClosedXML.Excel;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services.FinancialStatements;

public class FinancialStatementTemplateExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _templatesRoot;

    public FinancialStatementTemplateExporter(IWebHostEnvironment env)
    {
        _templatesRoot = Path.Combine(env.ContentRootPath, "Data", "FinancialStatements", "templates");
    }

    public bool HasTemplate(string companyKey)
    {
        var mappingPath = GetMappingPath(companyKey);
        if (!File.Exists(mappingPath))
            return false;

        var mapping = LoadMapping(mappingPath);
        return File.Exists(GetTemplatePath(mapping));
    }

    public byte[] Export(FinancialStatementResultDto result)
    {
        var mappingPath = GetMappingPath(result.CompanyKey);
        if (!File.Exists(mappingPath))
            throw new InvalidOperationException($"No Excel export template mapping for company '{result.CompanyKey}'.");

        var mapping = LoadMapping(mappingPath);
        var templatePath = GetTemplatePath(mapping);
        if (!File.Exists(templatePath))
            throw new InvalidOperationException($"Excel export template file not found: {templatePath}");

        using var workbook = new XLWorkbook(templatePath);
        var amounts = BuildAmountLookup(result);

        FillBalanceSheet(workbook, mapping, result, amounts);
        FillProfitAndLoss(workbook, mapping, result, amounts);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private void FillBalanceSheet(
        XLWorkbook workbook,
        FsExportMapping mapping,
        FinancialStatementResultDto result,
        Dictionary<string, decimal> amounts)
    {
        var sheetDef = mapping.BalanceSheet;
        var ws = workbook.Worksheet(sheetDef.SheetName);

        ws.Cell(sheetDef.CompanyNameCell).Value = result.CompanyName;
        if (!string.IsNullOrWhiteSpace(result.PeriodLabel))
        {
            ws.Cell(sheetDef.CurrentPeriodHeaderCell).Value =
                $"{result.PeriodLabel}\r\n(Rs. In Lakhs)";
        }

        var filled = new Dictionary<int, decimal>();
        foreach (var line in sheetDef.Lines)
        {
            var amount = ResolveBalanceSheetAmount(line, amounts, result);
            ws.Cell(line.Row, sheetDef.AmountColumn).Value = amount;
            filled[line.Row] = amount;
        }

        foreach (var subtotal in sheetDef.SubtotalRows)
        {
            var total = subtotal.SumRows.Sum(r => filled.GetValueOrDefault(r));
            total = Math.Round(total, 2, MidpointRounding.AwayFromZero);
            ws.Cell(subtotal.Row, sheetDef.AmountColumn).Value = total;
            filled[subtotal.Row] = total;
        }
    }

    private void FillProfitAndLoss(
        XLWorkbook workbook,
        FsExportMapping mapping,
        FinancialStatementResultDto result,
        Dictionary<string, decimal> amounts)
    {
        var sheetDef = mapping.ProfitLoss;
        var ws = workbook.Worksheet(sheetDef.SheetName);

        ws.Cell(sheetDef.CompanyNameCell).Value = result.CompanyName;
        if (!string.IsNullOrWhiteSpace(result.PeriodLabel))
        {
            ws.Cell(sheetDef.CurrentPeriodHeaderCell).Value =
                $"{result.PeriodLabel}\r\n(Rs. In Lakhs)";
        }

        foreach (var line in sheetDef.Lines)
        {
            if (!amounts.TryGetValue(NormalizeLabel(line.Label), out var amount))
                continue;

            ws.Cell(line.Row, sheetDef.AmountColumn).Value = amount;
        }
    }

    private static decimal ResolveBalanceSheetAmount(
        FsExportLineMapping line,
        Dictionary<string, decimal> amounts,
        FinancialStatementResultDto result)
    {
        var key = NormalizeLabel(line.Label);

        if (line.Section == "non-current")
        {
            return GetLoansAmount(result, nonCurrent: true);
        }

        if (line.Section == "current-total")
        {
            return GetLoansAmount(result, nonCurrent: false) + GetLoansAmount(result, nonCurrent: true);
        }

        if (key == NormalizeLabel("Deferred tax Asset"))
            return 0m;

        if (key == NormalizeLabel("Current investment"))
            return 0m;

        if (key == NormalizeLabel("Other long-term liabilities"))
            return 0m;

        return amounts.GetValueOrDefault(key);
    }

    private static decimal GetLoansAmount(FinancialStatementResultDto result, bool nonCurrent)
    {
        var assetSection = result.BalanceSheet
            .FirstOrDefault(s => s.Title.Contains("Assets", StringComparison.OrdinalIgnoreCase));

        if (assetSection == null)
            return 0m;

        var loansLines = assetSection.Lines
            .Where(l => !l.IsHeader && l.Label.Equals("Loans and advances", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (loansLines.Count == 0)
            return 0m;

        if (loansLines.Count == 1)
            return loansLines[0].AmountLakhs;

        return nonCurrent ? loansLines[0].AmountLakhs : loansLines[^1].AmountLakhs;
    }

    private static Dictionary<string, decimal> BuildAmountLookup(FinancialStatementResultDto result)
    {
        var lookup = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var section in result.BalanceSheet)
        {
            foreach (var line in section.Lines.Where(l => !l.IsHeader))
            {
                lookup[NormalizeLabel(line.Label)] = line.AmountLakhs;
            }
        }

        foreach (var line in result.ProfitAndLoss.Where(l => !l.IsHeader))
        {
            lookup[NormalizeLabel(line.Label)] = line.AmountLakhs;
        }

        return lookup;
    }

    private string GetMappingPath(string companyKey)
        => Path.Combine(_templatesRoot, companyKey.Trim(), "export-mapping.json");

    private static string GetTemplatePath(FsExportMapping mapping)
        => Path.Combine(
            Path.GetDirectoryName(mapping.ResolvedPath!)!,
            mapping.TemplateFile);

    private static FsExportMapping LoadMapping(string path)
    {
        var json = File.ReadAllText(path);
        var mapping = JsonSerializer.Deserialize<FsExportMapping>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Invalid export mapping: {path}");
        mapping.ResolvedPath = path;
        return mapping;
    }

    private static string NormalizeLabel(string label)
        => string.Join(' ', label.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

public class FsExportMapping
{
    public string CompanyKey { get; set; } = "";
    public string TemplateFile { get; set; } = "";
    public FsSheetExportMapping BalanceSheet { get; set; } = new();
    public FsSheetExportMapping ProfitLoss { get; set; } = new();
    internal string? ResolvedPath { get; set; }
}

public class FsSheetExportMapping
{
    public string SheetName { get; set; } = "";
    public string CompanyNameCell { get; set; } = "";
    public string CurrentPeriodHeaderCell { get; set; } = "";
    public int AmountColumn { get; set; } = 4;
    public List<FsExportLineMapping> Lines { get; set; } = [];
    public List<FsExportSubtotalMapping> SubtotalRows { get; set; } = [];
}

public class FsExportLineMapping
{
    public int Row { get; set; }
    public string Label { get; set; } = "";
    public string? Section { get; set; }
}

public class FsExportSubtotalMapping
{
    public int Row { get; set; }
    public List<int> SumRows { get; set; } = [];
}
