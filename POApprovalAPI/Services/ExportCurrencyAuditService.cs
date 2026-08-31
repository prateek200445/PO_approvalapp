using System.Globalization;
using ClosedXML.Excel;
using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Export-only audit: foreign-currency credit/debit notes where INR is posted
/// but stored FC/USD (currencyValue) is zero or a currency symbol.
/// Intercompany ledgers are always excluded (LedgerMaster, CommonLedgerMaster,
/// ac_interCompanyLedger, and ledgers named after group companies).
/// </summary>
public class ExportCurrencyAuditService
{
    private const int CommandTimeoutSeconds = 120;

    private static readonly HashSet<string> ForeignCurrencyTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "$", "USD", "US$", "€", "EUR", "Euro", "GBP", "CHF",
    };

    private readonly DatabaseService _database;

    public ExportCurrencyAuditService(DatabaseService database)
    {
        _database = database;
    }

    public static DateTime FinancialYearStart(DateTime asOf)
    {
        var y = asOf.Month >= 4 ? asOf.Year : asOf.Year - 1;
        return new DateTime(y, 4, 1);
    }

    public async Task<ExportCurrencyAuditResultDto> RunAuditAsync(
        string? companyValue,
        DateTime dateFrom,
        DateTime dateTo,
        decimal minInrAmount = 100m)
    {
        var from = dateFrom.Date;
        var to = dateTo.Date;
        if (from > to)
            (from, to) = (to, from);

        minInrAmount = Math.Max(0, minInrAmount);
        var companyLabel = ResolveCompanyLabel(companyValue);
        var creditCompanyFilter = BuildCompanyFilter(companyValue, "cn");
        var debitCompanyFilter = BuildCompanyFilter(companyValue, "dn");

        using var connection = _database.CreateConnection();

        var creditSql = $@"
SELECT
    'Credit Note' AS DocumentType,
    cn.CreditNoteNumber AS DocumentNo,
    cn.CreditNoteDate AS DocumentDate,
    cn.CompanyName,
    cn.PartyName,
    cn.SalesLedger AS LedgerName,
    CAST(cn.TotalCreditAmount AS decimal(18,4)) AS InrAmount,
    LTRIM(RTRIM(ISNULL(cn.currencyValue, ''))) AS StoredFc,
    LTRIM(RTRIM(ISNULL(cn.currency, ''))) AS Currency,
    CAST(ISNULL(cn.ExchangeRate, 0) AS decimal(18,6)) AS ExchangeRate
FROM CreditNote cn WITH (NOLOCK)
INNER JOIN LedgerMaster lm WITH (NOLOCK)
    ON lm.CompanyName = cn.CompanyName
   AND lm.LedgerName = cn.SalesLedger
WHERE {ExcludeIntercompanyLedgerSql("cn.CompanyName", "cn.SalesLedger", "lm")}
  AND lm.LedgerName LIKE '%Export%'
  AND cn.CreditNoteDate >= @DateFrom
  AND cn.CreditNoteDate < DATEADD(day, 1, @DateTo)
  AND ABS(ISNULL(cn.TotalCreditAmount, 0)) >= @MinInr
  AND {IsForeignCurrencySql("cn.currency")}
  AND {IsBadStoredFcSql("cn.currencyValue")}
  AND {creditCompanyFilter}";

        var debitSql = $@"
SELECT
    'Debit Note' AS DocumentType,
    dn.DebitNoteNumber AS DocumentNo,
    dn.sysdate AS DocumentDate,
    dn.CompanyName,
    dn.PartyName,
    dn.PurchaseLedger AS LedgerName,
    CAST(dn.TotalDebitAmount AS decimal(18,4)) AS InrAmount,
    LTRIM(RTRIM(ISNULL(dn.currencyValue, ''))) AS StoredFc,
    LTRIM(RTRIM(ISNULL(dn.currency, ''))) AS Currency,
    CAST(ISNULL(dn.ExchangeRate, 0) AS decimal(18,6)) AS ExchangeRate
FROM DebitNote dn WITH (NOLOCK)
INNER JOIN LedgerMaster lm WITH (NOLOCK)
    ON lm.CompanyName = dn.CompanyName
   AND lm.LedgerName = dn.PurchaseLedger
WHERE {ExcludeIntercompanyLedgerSql("dn.CompanyName", "dn.PurchaseLedger", "lm")}
  AND (lm.LedgerName LIKE '%Export%' OR lm.LedgerName LIKE '%Import%')
  AND dn.sysdate >= @DateFrom
  AND dn.sysdate < DATEADD(day, 1, @DateTo)
  AND ABS(ISNULL(dn.TotalDebitAmount, 0)) >= @MinInr
  AND {IsForeignCurrencySql("dn.currency")}
  AND {IsBadStoredFcSql("dn.currencyValue")}
  AND {debitCompanyFilter}";

        var param = new
        {
            DateFrom = from,
            DateTo = to,
            MinInr = (double)minInrAmount,
            GroupName = GetGroupName(companyValue),
            CompanyId = GetCompanyId(companyValue),
        };

        var creditRows = (await connection.QueryAsync<AuditRow>(
            creditSql, param, commandTimeout: CommandTimeoutSeconds)).ToList();
        var debitRows = (await connection.QueryAsync<AuditRow>(
            debitSql, param, commandTimeout: CommandTimeoutSeconds)).ToList();

        var items = creditRows
            .Concat(debitRows)
            .Select(MapRow)
            .OrderByDescending(i => i.DocumentDate, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.CompanyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.DocumentNo, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ExportCurrencyAuditResultDto
        {
            CompanyLabel = companyLabel,
            DateFrom = from.ToString("yyyy-MM-dd"),
            DateTo = to.ToString("yyyy-MM-dd"),
            MinInrAmount = minInrAmount,
            TotalCount = items.Count,
            CreditNoteCount = items.Count(i => i.DocumentType == "Credit Note"),
            DebitNoteCount = items.Count(i => i.DocumentType == "Debit Note"),
            TotalInrAmount = items.Sum(i => i.InrAmount),
            Items = items,
        };
    }

    public byte[] BuildExcel(ExportCurrencyAuditResultDto result)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Export FC audit");

        ws.Cell(1, 1).Value = "Export currency audit — INR posted, stored USD/FC missing";
        ws.Cell(2, 1).Value = $"Company: {result.CompanyLabel}";
        ws.Cell(3, 1).Value = $"Period: {result.DateFrom} to {result.DateTo}";
        ws.Cell(4, 1).Value = $"Min INR: {result.MinInrAmount:N2} | Rows: {result.TotalCount}";

        var headers = new[]
        {
            "Type", "Document no.", "Date", "Company", "Party", "Export ledger",
            "INR amount", "Stored FC", "Currency", "Exchange rate", "Calculated FC", "Issue",
        };

        for (var c = 0; c < headers.Length; c++)
            ws.Cell(6, c + 1).Value = headers[c];

        var row = 7;
        foreach (var item in result.Items)
        {
            ws.Cell(row, 1).Value = item.DocumentType;
            ws.Cell(row, 2).Value = item.DocumentNo;
            ws.Cell(row, 3).Value = item.DocumentDate;
            ws.Cell(row, 4).Value = item.CompanyName;
            ws.Cell(row, 5).Value = item.PartyName;
            ws.Cell(row, 6).Value = item.LedgerName;
            ws.Cell(row, 7).Value = (double)item.InrAmount;
            ws.Cell(row, 8).Value = item.StoredFc;
            ws.Cell(row, 9).Value = item.Currency;
            ws.Cell(row, 10).Value = (double)item.ExchangeRate;
            ws.Cell(row, 11).Value = (double)item.CalculatedFc;
            ws.Cell(row, 12).Value = item.Issue;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static ExportCurrencyAuditItemDto MapRow(AuditRow row)
    {
        var inr = row.InrAmount;
        var rate = row.ExchangeRate;
        var calculated = rate > 0 ? Math.Round(Math.Abs(inr) / rate, 2, MidpointRounding.AwayFromZero) : 0m;
        var stored = (row.StoredFc ?? "").Trim();

        return new ExportCurrencyAuditItemDto
        {
            DocumentType = row.DocumentType ?? "",
            DocumentNo = row.DocumentNo ?? "",
            DocumentDate = FormatDate(row.DocumentDate),
            CompanyName = (row.CompanyName ?? "").Trim(),
            PartyName = (row.PartyName ?? "").Trim(),
            LedgerName = (row.LedgerName ?? "").Trim(),
            InrAmount = inr,
            StoredFc = stored,
            Currency = NormalizeCurrency(row.Currency),
            ExchangeRate = rate,
            CalculatedFc = calculated,
            Issue = DescribeIssue(stored),
        };
    }

    private static string DescribeIssue(string storedFc)
    {
        if (string.IsNullOrWhiteSpace(storedFc))
            return "Foreign amount field is empty while INR is posted.";
        if (ForeignCurrencyTokens.Contains(storedFc))
            return "Foreign amount field contains currency symbol, not numeric USD/FC.";
        if (!decimal.TryParse(storedFc, NumberStyles.Any, CultureInfo.InvariantCulture, out var n)
            && !decimal.TryParse(storedFc, NumberStyles.Any, CultureInfo.CurrentCulture, out n))
            return "Foreign amount field is not a valid number.";
        return "Foreign amount is zero while INR is posted.";
    }

    private static string FormatDate(DateTime? dt) =>
        dt is { Year: > 1900 } d ? d.ToString("yyyy-MM-dd") : "";

    private static string NormalizeCurrency(string? currency)
    {
        var c = (currency ?? "").Trim();
        if (string.IsNullOrEmpty(c)) return "";
        if (c.StartsWith("Rs", StringComparison.OrdinalIgnoreCase)) return "Rs.";
        return c;
    }

    /// <summary>
    /// Intercompany ledgers must never appear in audit results.
    /// Matches Export Bill Overdue / Sales Dashboard IC rules.
    /// </summary>
    private static string ExcludeIntercompanyLedgerSql(string companyExpr, string ledgerExpr, string ledgerMasterAlias) =>
        $@"NOT {IsIntercompanyFlagSql($"{ledgerMasterAlias}.IsInterCompany")}
  AND NOT EXISTS (
    SELECT 1 FROM CommonLedgerMaster cm WITH (NOLOCK)
    WHERE LTRIM(RTRIM(cm.CompanyName)) = LTRIM(RTRIM({companyExpr}))
      AND LTRIM(RTRIM(cm.LedgerName)) = LTRIM(RTRIM({ledgerExpr}))
      AND {IsIntercompanyFlagSql("cm.IsInterCompany")}
  )
  AND NOT EXISTS (
    SELECT 1 FROM ac_interCompanyLedger icl WITH (NOLOCK)
    INNER JOIN LedgerMaster icLm WITH (NOLOCK) ON icl.LedgerId = icLm.srno
    WHERE LTRIM(RTRIM(icLm.CompanyName)) = LTRIM(RTRIM({companyExpr}))
      AND LTRIM(RTRIM(icLm.LedgerName)) = LTRIM(RTRIM({ledgerExpr}))
  )
  AND NOT EXISTS (
    SELECT 1 FROM FactoryInfo fi WITH (NOLOCK)
    WHERE LTRIM(RTRIM(fi.Name)) = LTRIM(RTRIM({ledgerExpr}))
      AND ISNULL(fi.Name, '') <> ''
  )";

    private static string IsIntercompanyFlagSql(string column) =>
        $@"LOWER(LTRIM(RTRIM(CONVERT(nvarchar(20), ISNULL({column}, 'no'))))) IN ('yes', 'y', '1', 'true')";

    private static string IsForeignCurrencySql(string column) =>
        $"(LTRIM(RTRIM(ISNULL({column}, ''))) IN ('$', 'USD', 'US$', N'€', 'EUR', 'Euro', 'GBP', 'CHF'))";

    private static string IsBadStoredFcSql(string column) =>
        $@"(
    LTRIM(RTRIM(ISNULL({column}, ''))) IN ('$', 'USD', 'US$', N'€', 'EUR', 'Euro', 'GBP', 'CHF')
    OR TRY_CAST({column} AS float) IS NULL
    OR ABS(ISNULL(TRY_CAST({column} AS float), 0)) < 0.01
)";

    private static string BuildCompanyFilter(string? companyValue, string alias)
    {
        LedgerSummaryService.ParseCompanyValue(NormalizeCompanyValue(companyValue), out var type, out _, out _);
        return type switch
        {
            1 => $@"EXISTS (
                SELECT 1 FROM FactoryInfo fi WITH (NOLOCK)
                WHERE fi.Name = {alias}.CompanyName AND fi.GroupName = @GroupName)",
            2 => $@"EXISTS (
                SELECT 1 FROM FactoryInfo fi WITH (NOLOCK)
                WHERE fi.Name = {alias}.CompanyName AND fi.srno = @CompanyId)",
            _ => "1=1",
        };
    }

    private static string NormalizeCompanyValue(string? companyValue)
    {
        var v = (companyValue ?? "").Trim();
        if (string.IsNullOrEmpty(v) || v.Equals("A", StringComparison.OrdinalIgnoreCase))
            return "A-all";
        return v;
    }

    private static string ResolveCompanyLabel(string? companyValue)
    {
        var v = NormalizeCompanyValue(companyValue);
        if (v.StartsWith('G') && v.Length > 2)
            return $"{v[2..]} (Group)";
        if (v.StartsWith('C') && v.Length > 2)
            return $"Company #{v[2..]}";
        return "All companies";
    }

    private static string GetGroupName(string? companyValue)
    {
        LedgerSummaryService.ParseCompanyValue(NormalizeCompanyValue(companyValue), out _, out var name, out _);
        return name;
    }

    private static int GetCompanyId(string? companyValue)
    {
        LedgerSummaryService.ParseCompanyValue(NormalizeCompanyValue(companyValue), out _, out _, out var id);
        return id;
    }

    private sealed class AuditRow
    {
        public string? DocumentType { get; set; }
        public string? DocumentNo { get; set; }
        public DateTime? DocumentDate { get; set; }
        public string? CompanyName { get; set; }
        public string? PartyName { get; set; }
        public string? LedgerName { get; set; }
        public decimal InrAmount { get; set; }
        public string? StoredFc { get; set; }
        public string? Currency { get; set; }
        public decimal ExchangeRate { get; set; }
    }
}
