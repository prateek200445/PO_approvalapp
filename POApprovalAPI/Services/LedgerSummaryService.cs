using System.Data;
using ClosedXML.Excel;
using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class LedgerSummaryService
{
    private readonly DatabaseService _database;

    public LedgerSummaryService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<LedgerCompanyOption>> GetCompaniesAsync()
    {
        using var connection = _database.CreateConnection();

        var companies = (await connection.QueryAsync<(int SrNo, string Name, string? GroupName)>(@"
SELECT fi.srno AS SrNo, fi.Name, fi.GroupName
FROM FactoryInfo fi WITH (NOLOCK)
WHERE ISNULL(fi.Name, '') <> ''
ORDER BY fi.Name")).ToList();

        var options = new List<LedgerCompanyOption>();

        var groups = companies
            .Select(c => (c.GroupName ?? "").Trim())
            .Where(g => g.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g)
            .ToList();

        foreach (var group in groups)
        {
            options.Add(new LedgerCompanyOption
            {
                Value = $"G-{group}",
                Label = $"{group} (Group)",
                CompanyType = 1,
                CompanyName = group,
                CompanyId = 0,
            });
        }

        foreach (var c in companies)
        {
            options.Add(new LedgerCompanyOption
            {
                Value = $"C-{c.SrNo}",
                Label = c.Name.Trim(),
                CompanyType = 2,
                CompanyName = "",
                CompanyId = c.SrNo,
            });
        }

        return options;
    }

    public async Task<IReadOnlyList<LedgerNameOption>> GetLedgersAsync(string companyValue)
    {
        if (string.IsNullOrWhiteSpace(companyValue) || companyValue.Length < 2)
            throw new ArgumentException("Company is required.");

        ParseCompanyValue(companyValue, out var companyType, out var companyName, out var companyId);

        using var connection = _database.CreateConnection();

        var rows = await connection.QueryAsync<(string LedgerId, string LedgerName)>(@"
SELECT DISTINCT
    l.LedgerName AS LedgerId,
    l.LedgerName
FROM ledgermaster l WITH (NOLOCK)
WHERE ISNULL(l.LedgerName, '') <> ''
  AND (
        @CompanyType = 0
     OR (@CompanyType = 1 AND EXISTS (
            SELECT 1 FROM FactoryInfo fi WITH (NOLOCK)
            WHERE fi.Name = l.CompanyName AND fi.GroupName = @CompanyName
        ))
     OR (@CompanyType = 2 AND EXISTS (
            SELECT 1 FROM FactoryInfo fi WITH (NOLOCK)
            WHERE fi.Name = l.CompanyName AND fi.srno = @CompanyId
        ))
  )
ORDER BY l.LedgerName",
            new
            {
                CompanyType = companyType,
                CompanyName = companyName,
                CompanyId = companyId,
            });

        return rows
            .Select(r => new LedgerNameOption
            {
                LedgerId = r.LedgerName.Trim(),
                LedgerName = r.LedgerName.Trim(),
            })
            .ToList();
    }

    public async Task<IReadOnlyList<LedgerNameOption>> GetLedgersForCompaniesAsync(IEnumerable<string> companyValues)
    {
        var values = companyValues
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (values.Count == 0)
            throw new ArgumentException("Select at least one company.");

        if (values.Count == 1)
            return await GetLedgersAsync(values[0]);

        // Parallel fetch instead of sequential round-trips
        var parts = await Task.WhenAll(values.Select(GetLedgersAsync));
        var map = new Dictionary<string, LedgerNameOption>(StringComparer.OrdinalIgnoreCase);
        foreach (var list in parts)
        {
            foreach (var ledger in list)
                map[ledger.LedgerName] = ledger;
        }

        return map.Values.OrderBy(l => l.LedgerName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<LedgerSummaryResultDto> QueryAsync(LedgerSummaryQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LedgerName))
            throw new ArgumentException("Ledger name is required.");
        if (request.DateTo.Date < request.DateFrom.Date)
            throw new ArgumentException("To date must be on or after From date.");

        var result = await QuerySingleAsync(request);
        result.CompanyCount = 1;
        result.LedgerCount = 1;
        result.PairCount = 1;
        return result;
    }

    public async Task<LedgerSummaryResultDto> QueryBatchAsync(LedgerSummaryBatchQueryRequest request)
    {
        if (request.Companies == null || request.Companies.Count == 0)
            throw new ArgumentException("Select at least one company.");
        if (request.LedgerNames == null || request.LedgerNames.Count == 0)
            throw new ArgumentException("Select at least one ledger.");
        if (request.DateTo.Date < request.DateFrom.Date)
            throw new ArgumentException("To date must be on or after From date.");

        var companies = request.Companies
            .Where(c => !string.IsNullOrWhiteSpace(c.Value))
            .GroupBy(c => c.Value.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var ledgers = request.LedgerNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var pairCount = companies.Count * ledgers.Count;
        if (pairCount > 40)
            throw new ArgumentException("Too many combinations (max 40). Narrow company or ledger selection.");

        var combined = new LedgerSummaryResultDto
        {
            CompanyCount = companies.Count,
            LedgerCount = ledgers.Count,
            PairCount = pairCount,
        };

        // Bounded parallelism so SQL Server isn't flooded
        using var gate = new SemaphoreSlim(4);
        var tasks = new List<Task<LedgerSummaryResultDto>>();

        foreach (var company in companies)
        {
            foreach (var ledger in ledgers)
            {
                var companyCopy = company;
                var ledgerCopy = ledger;
                tasks.Add(Task.Run(async () =>
                {
                    await gate.WaitAsync();
                    try
                    {
                        var single = await QuerySingleAsync(new LedgerSummaryQueryRequest
                        {
                            CompanyType = companyCopy.CompanyType,
                            CompanyName = companyCopy.CompanyName,
                            CompanyId = companyCopy.CompanyId,
                            LedgerName = ledgerCopy,
                            DateFrom = request.DateFrom,
                            DateTo = request.DateTo,
                            Currency = request.Currency,
                            InterestCal = request.InterestCal,
                        });

                        var displayCompany = string.IsNullOrWhiteSpace(companyCopy.Label)
                            ? companyCopy.Value
                            : companyCopy.Label;

                        foreach (var row in single.Rows)
                        {
                            row.LedgerName = ledgerCopy;
                            // Group selections are one entity — don't keep member company names
                            if (companyCopy.CompanyType == 1 || string.IsNullOrWhiteSpace(row.CompanyName))
                                row.CompanyName = displayCompany;
                        }

                        return single;
                    }
                    finally
                    {
                        gate.Release();
                    }
                }));
            }
        }

        var parts = await Task.WhenAll(tasks);
        foreach (var part in parts)
        {
            combined.DebitTotal += part.DebitTotal;
            combined.CreditTotal += part.CreditTotal;
            combined.Rows.AddRange(part.Rows);
        }

        // One shared running balance per ledger (group/companies combined chronologically)
        combined.Rows = WithSharedRunningBalancePerLedger(combined.Rows);
        RecalcCombinedBalances(combined);

        return combined;
    }

    /// <summary>
    /// Within each ledger, openings first then date order, with one continuous running balance.
    /// </summary>
    private static List<LedgerSummaryRowDto> WithSharedRunningBalancePerLedger(IEnumerable<LedgerSummaryRowDto> source)
    {
        var result = new List<LedgerSummaryRowDto>();

        foreach (var group in source
            .GroupBy(r => (r.LedgerName ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var ordered = group
                .OrderBy(r => r.IsOpening ? 0 : 1)
                .ThenBy(r => r.Date ?? DateTime.MinValue)
                .ThenBy(r => r.VoucherNo ?? "", StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.CompanyName ?? "", StringComparer.OrdinalIgnoreCase)
                .ToList();

            decimal running = 0m;
            foreach (var row in ordered)
            {
                running += row.Debit - row.Credit;
                row.Closing = running;
                result.Add(row);
            }
        }

        return result;
    }

    private static void RecalcCombinedBalances(LedgerSummaryResultDto combined)
    {
        combined.OpeningBalance = combined.Rows
            .Where(r => r.IsOpening)
            .Sum(r => r.Debit - r.Credit);

        combined.ClosingBalance = combined.Rows
            .GroupBy(r => (r.LedgerName ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
            .Sum(g => g.LastOrDefault()?.Closing ?? 0m);
    }

    private async Task<LedgerSummaryResultDto> QuerySingleAsync(LedgerSummaryQueryRequest request)
    {
        using var connection = _database.CreateConnection();

        var table = new DataTable();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "sp_ac_LedgerSummary_BankRecoDate";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 120;

            cmd.Parameters.AddWithValue("@CompanyType", request.CompanyType);
            cmd.Parameters.AddWithValue("@CompanyName", (object?)request.CompanyName ?? "");
            cmd.Parameters.AddWithValue("@CompanyId", request.CompanyId);
            cmd.Parameters.AddWithValue("@LedgerName", request.LedgerName.Trim());
            cmd.Parameters.AddWithValue("@Currency", string.IsNullOrWhiteSpace(request.Currency) ? DBNull.Value : request.Currency);
            cmd.Parameters.AddWithValue("@DateFrom", request.DateFrom.Date);
            cmd.Parameters.AddWithValue("@DateTo", request.DateTo.Date);
            cmd.Parameters.AddWithValue("@InterestCal", request.InterestCal);

            await using var reader = await cmd.ExecuteReaderAsync();
            table.Load(reader);
        }

        var mapped = MapResult(table, request.DateTo.Date, request.InterestCal, request.LedgerName.Trim());
        return mapped;
    }

    public static void ParseCompanyValue(string value, out int companyType, out string companyName, out int companyId)
    {
        companyType = 0;
        companyName = "";
        companyId = 0;

        var v = value.Trim();
        if (v.Length < 2)
            throw new ArgumentException("Invalid company selection.");

        switch (char.ToUpperInvariant(v[0]))
        {
            case 'A':
                companyType = 0;
                break;
            case 'G':
                companyType = 1;
                companyName = v.Length > 2 ? v[2..] : "";
                break;
            case 'C':
                companyType = 2;
                companyId = int.TryParse(v.Length > 2 ? v[2..] : "", out var id) ? id : 0;
                break;
            default:
                throw new ArgumentException("Invalid company selection.");
        }
    }

    private static LedgerSummaryResultDto MapResult(DataTable table, DateTime dateTo, decimal interestRate, string ledgerName)
    {
        var result = new LedgerSummaryResultDto();
        if (table.Rows.Count == 0) return result;

        var opening = 0m;
        var first = table.Rows[0];
        if (string.Equals(Convert.ToString(first["VoucherType"]), "z", StringComparison.OrdinalIgnoreCase))
            opening = ToDecimal(first["amount"]);

        result.OpeningBalance = opening;

        var closing = 0m;
        var periodDebit = 0m;
        var periodCredit = 0m;

        foreach (DataRow dr in table.Rows)
        {
            var amount = ToDecimal(dr["amount"]);
            var isOpening = string.Equals(Convert.ToString(dr["VoucherType"]), "z", StringComparison.OrdinalIgnoreCase);
            var date = ToDate(dr["Date"]);
            var excRate = ToDecimal(HasColumn(table, "ExcRate") ? dr["ExcRate"] : 0);
            var currency = Convert.ToString(dr["Currency"]) ?? "";
            if (currency.Equals("rs", StringComparison.OrdinalIgnoreCase))
                currency = "Rs.";

            var debit = amount > 0 ? amount : 0m;
            var credit = amount <= 0 ? Math.Abs(amount) : 0m;

            if (!isOpening)
            {
                periodDebit += debit;
                periodCredit += credit;
            }

            closing += amount;

            decimal? debitFc = null;
            decimal? creditFc = null;
            if (excRate != 0)
            {
                debitFc = amount > 0 ? amount / excRate : 0m;
                creditFc = amount <= 0 ? Math.Abs(amount / excRate) : 0m;
            }

            if (currency == "Rs." && !isOpening)
            {
                debitFc = null;
                creditFc = null;
            }

            var days = 0;
            if (date.HasValue)
                days = (dateTo - date.Value.Date).Days + 1;

            var interest = days != 0 ? (amount * days * interestRate) / 36500m : 0m;
            if (HasColumn(table, "InterstCal") || HasColumn(table, "InterestCal"))
            {
                var col = HasColumn(table, "InterstCal") ? "InterstCal" : "InterestCal";
                var spInterest = ToDecimal(dr[col]);
                if (spInterest != 0) interest = spInterest;
            }

            var closingFc = HasColumn(table, "fClosingBalance")
                ? ToDecimal(dr["fClosingBalance"])
                : 0m;

            result.Rows.Add(new LedgerSummaryRowDto
            {
                CompanyName = Convert.ToString(dr["CompanyName"]) ?? "",
                LedgerName = ledgerName,
                Date = date,
                Particulars = Convert.ToString(dr["LedgerName"]) ?? "",
                VoucherType = isOpening ? "Opening Balance" : (Convert.ToString(dr["VoucherType"]) ?? ""),
                VoucherNo = Convert.ToString(dr["voucherno"]) ?? "",
                VoucherRef = Convert.ToString(dr["VoucherRef"]) ?? "",
                Debit = debit,
                Credit = credit,
                Currency = string.IsNullOrWhiteSpace(currency) || currency == "Rs." ? null : currency,
                DebitFc = debitFc,
                CreditFc = creditFc,
                ExcRate = excRate == 0 || Math.Round(amount / (excRate == 0 ? 1 : excRate), 2) == 0 ? 0 : excRate,
                Closing = closing,
                ClosingFc = closingFc,
                Days = days,
                Interest = interest,
                IsOpening = isOpening,
                ApprovalStatus = HasColumn(table, "ApprovalStatus") ? Convert.ToString(dr["ApprovalStatus"]) : null,
            });
        }

        result.DebitTotal = periodDebit;
        result.CreditTotal = periodCredit;
        result.ClosingBalance = closing;
        return result;
    }

    public byte[] BuildExport(LedgerSummaryExportRequest request)
    {
        var result = request.Result ?? new LedgerSummaryResultDto();
        var rows = result.Rows ?? new List<LedgerSummaryRowDto>();

        using var workbook = new XLWorkbook();
        var summary = workbook.Worksheets.Add("Summary");

        summary.Cell(1, 1).Value = "Report";
        summary.Cell(1, 2).Value = "Ledger Summary";
        summary.Cell(2, 1).Value = "Period";
        summary.Cell(2, 2).Value = FormatPeriod(request.DateFrom, request.DateTo);
        summary.Cell(3, 1).Value = "Companies";
        summary.Cell(3, 2).Value = FormatList(request.CompanyLabels, result.CompanyCount);
        summary.Cell(4, 1).Value = "Ledgers";
        summary.Cell(4, 2).Value = FormatList(request.LedgerNames, result.LedgerCount);

        for (var r = 1; r <= 4; r++)
        {
            StyleSummaryLabel(summary.Cell(r, 1), XLColor.FromHtml("#1F4E79"), XLColor.White);
            summary.Cell(r, 2).Style.Font.Bold = true;
        }
        summary.Range(1, 1, 4, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        summary.Range(1, 1, 4, 2).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        summary.Cell(6, 1).Value = "Metric";
        summary.Cell(6, 2).Value = "Value";
        var metricHeader = summary.Range(6, 1, 6, 2);
        metricHeader.Style.Font.Bold = true;
        metricHeader.Style.Font.FontColor = XLColor.White;
        metricHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#2F5496");
        metricHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var metrics = new (string Label, string Tone, Action<IXLCell> Write)[]
        {
            ("Opening Balance", "other", c => { c.Value = result.OpeningBalance; c.Style.NumberFormat.Format = "#,##0.00"; }),
            ("Period Debit", "total", c => { c.Value = result.DebitTotal; c.Style.NumberFormat.Format = "#,##0.00"; }),
            ("Period Credit", "total", c => { c.Value = result.CreditTotal; c.Style.NumberFormat.Format = "#,##0.00"; }),
            ("Closing Balance", "matched", c => { c.Value = result.ClosingBalance; c.Style.NumberFormat.Format = "#,##0.00"; }),
            ("Ledgers", "other", c => c.Value = result.LedgerCount > 0 ? result.LedgerCount : CountLedgers(rows)),
            ("Transaction Rows", "other", c => c.Value = rows.Count),
        };

        for (var i = 0; i < metrics.Length; i++)
        {
            var excelRow = i + 7;
            summary.Cell(excelRow, 1).Value = metrics[i].Label;
            metrics[i].Write(summary.Cell(excelRow, 2));
            ApplySummaryRowTone(summary.Range(excelRow, 1, excelRow, 2), metrics[i].Tone);
            summary.Cell(excelRow, 2).Style.Font.Bold = true;
        }
        summary.Range(6, 1, 6 + metrics.Length, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        summary.Range(6, 1, 6 + metrics.Length, 2).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        summary.Columns().AdjustToContents();

        var detail = workbook.Worksheets.Add("Ledger Details");
        var headers = new[]
        {
            "Company", "Ledger Name", "Date", "Particulars", "Voucher", "Voucher No", "Voucher Ref",
            "Debit (INR)", "Credit (INR)", "Currency", "Running Balance",
        };
        for (var c = 0; c < headers.Length; c++)
            detail.Cell(1, c + 1).Value = headers[c];

        var headerRange = detail.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2F5496");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.WrapText = true;

        // Ensure shared per-ledger running balances (same as query)
        rows = WithSharedRunningBalancePerLedger(rows);
        result.Rows = rows;

        // One block per ledger: Opening → txns (shared stream) → Closing
        var sections = rows
            .GroupBy(r => (r.LedgerName ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var excelRowNum = 2;
        for (var s = 0; s < sections.Count; s++)
        {
            if (s > 0)
                excelRowNum++;

            var ledger = sections[s].Key;
            var sectionRows = sections[s].ToList(); // already openings-first + date order from helper

            var companyNames = sectionRows
                .Select(r => (r.CompanyName ?? "").Trim())
                .Where(c => c.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            decimal openDebit = 0m, openCredit = 0m, openRunning = 0m;
            decimal periodDebit = 0m, periodCredit = 0m;

            foreach (var o in sectionRows.Where(r => r.IsOpening))
            {
                openDebit += o.Debit;
                openCredit += o.Credit;
                openRunning = o.Closing; // last opening row after shared accumulation
            }

            foreach (var t in sectionRows.Where(r => !r.IsOpening))
            {
                periodDebit += t.Debit;
                periodCredit += t.Credit;
            }

            var closingBalance = sectionRows.Count > 0 ? sectionRows[^1].Closing : 0m;

            var companyLabel = companyNames.Count switch
            {
                0 => "",
                1 => companyNames[0],
                _ => $"{companyNames.Count} companies",
            };

            WriteDetailRow(
                detail, excelRowNum, companyLabel, ledger, null, "Opening Balance", "",
                "", "", openDebit, openCredit, null, openRunning, "opening");
            excelRowNum++;

            foreach (var t in sectionRows.Where(r => !r.IsOpening))
            {
                WriteDetailRow(
                    detail, excelRowNum, t.CompanyName ?? "", ledger, t.Date, t.Particulars, t.VoucherType,
                    t.VoucherNo, t.VoucherRef, t.Debit, t.Credit, t.Currency, t.Closing, "txn");
                excelRowNum++;
            }

            WriteDetailRow(
                detail, excelRowNum, companyLabel, ledger, null, "Closing Balance", "",
                "", "", periodDebit, periodCredit, null, closingBalance, "closing");
            excelRowNum++;
        }

        var lastDataRow = Math.Max(1, excelRowNum - 1);
        var tableRange = detail.Range(1, 1, lastDataRow, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        detail.SheetView.FreezeRows(1);
        detail.Columns().AdjustToContents();
        detail.Column(1).Width = Math.Min(Math.Max(detail.Column(1).Width, 18), 36);
        detail.Column(2).Width = Math.Min(Math.Max(detail.Column(2).Width, 22), 40);
        detail.Column(4).Width = Math.Min(Math.Max(detail.Column(4).Width, 28), 48);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteDetailRow(
        IXLWorksheet sheet,
        int row,
        string company,
        string ledger,
        DateTime? date,
        string particulars,
        string voucher,
        string voucherNo,
        string voucherRef,
        decimal debit,
        decimal credit,
        string? currency,
        decimal runningBalance,
        string kind)
    {
        sheet.Cell(row, 1).Value = company;
        sheet.Cell(row, 2).Value = ledger;
        if (date.HasValue)
        {
            sheet.Cell(row, 3).Value = date.Value.Date;
            sheet.Cell(row, 3).Style.DateFormat.Format = "dd-MMM-yyyy";
        }
        sheet.Cell(row, 4).Value = particulars;
        sheet.Cell(row, 5).Value = voucher;
        sheet.Cell(row, 6).Value = voucherNo;
        sheet.Cell(row, 7).Value = voucherRef;
        var alwaysShowAmounts = kind is "opening" or "closing";
        if (alwaysShowAmounts || debit != 0) sheet.Cell(row, 8).Value = debit;
        if (alwaysShowAmounts || credit != 0) sheet.Cell(row, 9).Value = credit;
        sheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
        sheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
        sheet.Cell(row, 10).Value = currency ?? "";
        sheet.Cell(row, 11).Value = runningBalance;
        sheet.Cell(row, 11).Style.NumberFormat.Format = "#,##0.00";

        if (alwaysShowAmounts)
        {
            var range = sheet.Range(row, 1, row, 11);
            range.Style.Font.Bold = true;
            if (kind == "opening")
            {
                range.Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");
                range.Style.Font.FontColor = XLColor.FromHtml("#1F4E79");
            }
            else
            {
                range.Style.Fill.BackgroundColor = XLColor.FromHtml("#C6EFCE");
                range.Style.Font.FontColor = XLColor.FromHtml("#006100");
            }
        }
    }

    private static void StyleSummaryLabel(IXLCell cell, XLColor background, XLColor fontColor)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = fontColor;
        cell.Style.Fill.BackgroundColor = background;
    }

    private static void ApplySummaryRowTone(IXLRange range, string tone)
    {
        var (bg, fg) = tone switch
        {
            "matched" => (XLColor.FromHtml("#C6EFCE"), XLColor.FromHtml("#006100")),
            "total" => (XLColor.FromHtml("#DDEBF7"), XLColor.FromHtml("#1F4E79")),
            _ => (XLColor.FromHtml("#E7E6E6"), XLColor.FromHtml("#333333")),
        };
        range.Style.Fill.BackgroundColor = bg;
        range.Style.Font.FontColor = fg;
    }

    private static string FormatPeriod(string? from, string? to)
    {
        if (string.IsNullOrWhiteSpace(from) && string.IsNullOrWhiteSpace(to))
            return "—";
        if (string.IsNullOrWhiteSpace(from)) return to!.Trim();
        if (string.IsNullOrWhiteSpace(to)) return from!.Trim();
        return $"{from.Trim()} – {to.Trim()}";
    }

    private static string FormatList(List<string>? items, int fallbackCount)
    {
        if (items == null || items.Count == 0)
            return fallbackCount > 0 ? fallbackCount.ToString() : "—";
        if (items.Count <= 3) return string.Join(", ", items);
        return $"{string.Join(", ", items.Take(3))} (+{items.Count - 3} more)";
    }

    private static int CountLedgers(List<LedgerSummaryRowDto> rows) =>
        rows.Select(r => (r.LedgerName ?? "").Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    private static bool HasColumn(DataTable table, string name) =>
        table.Columns.Contains(name);

    private static decimal ToDecimal(object? value)
    {
        if (value == null || value == DBNull.Value) return 0m;
        return Convert.ToDecimal(value);
    }

    private static DateTime? ToDate(object? value)
    {
        if (value == null || value == DBNull.Value) return null;
        return Convert.ToDateTime(value);
    }
}
