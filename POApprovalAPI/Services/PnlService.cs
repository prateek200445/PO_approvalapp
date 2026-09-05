using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class PnlService
{
    private const int TimeoutSeconds = 180;
    private const double Lacs = 100_000d;

    private static readonly string[] StockCategories =
    {
        "RawMaterial", "Packing", "WIP", "FinishedGoods", "Traded", "Stores",
    };

    private static readonly Dictionary<string, string> StockLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RawMaterial"] = "Raw materials",
        ["Packing"] = "Packing materials",
        ["WIP"] = "WIP",
        ["FinishedGoods"] = "Finished goods",
        ["Traded"] = "Stock in trade",
        ["Stores"] = "Stores, spares & consumables",
    };

    private static readonly Dictionary<string, string> HeadAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sale of finished goods"] = "Sale of finished goods",
        ["sale of traded goods"] = "Sale of traded goods",
        ["income from jobwork charges"] = "Income from Jobwork charges",
        ["export incentives"] = "Export incentives",
        ["commission income"] = "Commission income",
        ["scrap sales"] = "Scrap sales",
        ["sales of scrap & wastage"] = "Scrap sales",
        ["wind mill income"] = "Wind mill income",
        ["- on fixed deposits with banks"] = "- on fixed deposits with Banks",
        ["interest received from bank"] = "- on fixed deposits with Banks",
        ["interest recd from others"] = "- from others",
        ["interest received from others"] = "- from others",
        ["rent income"] = "Rent Income",
        ["interest received from customers"] = "Interest Received From Customers",
        ["profit on sale of fixed assets"] = "Profit on sale of fixed assets",
        ["profit on sale of investments"] = "Profit on sale of investments",
        ["sundry balance written off"] = "Sundry balance written off",
        ["miscellaneous income"] = "Miscellaneous income",
        ["salaries, wages and bonus"] = "Salaries, wages and  bonus",
        ["wages and salary"] = "Salaries, wages and  bonus",
        ["contribution to provident and other funds"] = "Contribution to provident and other funds",
        ["gratuity expenses"] = "Gratuity Expenses",
        ["staff welfare expenses"] = "Staff welfare expenses",
        ["labour charges"] = "Labour charges",
        ["power and fuel"] = "Power and fuel",
        ["jobwork charges"] = "Jobwork charges",
        ["other manufacturing expenses"] = "Other Manufacturing Expenses",
        ["advertising and sales promotion expense"] = "Advertising and sales promotion expense",
        ["advertisement & sales promotion expense"] = "Advertising and sales promotion expense",
        ["travelling expense"] = "Travelling expense",
        ["travelling & conveyance"] = "Travelling expense",
        ["brokerage & commission"] = "Brokerage & commission",
        ["conveyance, vehicle running and traveling expenses"] = "Conveyance, vehicle running and traveling expenses",
        ["freight outward and c & f charges"] = "Freight Outward and C & F Charges",
        ["freight outward expense"] = "Freight Outward and C & F Charges",
        ["add: purchase of traded goods"] = "Add: Purchase of traded goods",
        ["purchase of traded goods"] = "Add: Purchase of traded goods",
        ["insurance"] = "Insurance",
        ["insurance expenses"] = "Insurance",
        ["miscellaneous expense"] = "Miscellaneous Expense",
        ["miscellaneous expenses"] = "Miscellaneous Expense",
        ["other administrative expenses"] = "Other Administrative Expenses",
        ["exchange rate difference"] = "Exchange Rate Difference",
        ["depreciation expenses"] = "Depreciation",
        ["bank charges & commission"] = "Bank Charges & Commission",
        ["interest expense - other"] = "Interest Expense - Other",
        ["interest on working capital & term loan"] = "Interest on Working Capital & Term Loan",
        ["income tax"] = "Income Tax",
    };

    // Same grouping as the group EBITDA TB (PCFC FX sits under bank charges, not admin FX).
    private static readonly Dictionary<string, string> LedgerHeadOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Exchange Rate Difference - Pcfc"] = "Bank Charges & Commission",
        ["Exchange Rate Difference - PCFC"] = "Bank Charges & Commission",
        ["Exchange Rate Difference - Finance"] = "Bank Charges & Commission",
    };

    private readonly DatabaseService _database;

    public PnlService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<List<PnlCompanyOption>> GetCompaniesAsync()
    {
        using var connection = _database.CreateConnection();
        var factories = (await connection.QueryAsync<(string Name, string? GroupName)>(@"
SELECT Name, GroupName
FROM FactoryInfo WITH (NOLOCK)
WHERE ISNULL(Name, '') <> ''
ORDER BY Name")).ToList();

        var options = new List<PnlCompanyOption>
        {
            new() { Value = "All Companies", Label = "All Companies", Kind = "all" },
        };

        foreach (var group in factories
                     .Select(f => (f.GroupName ?? "").Trim())
                     .Where(g => g.Length > 0)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(g => g, StringComparer.OrdinalIgnoreCase))
        {
            options.Add(new PnlCompanyOption
            {
                Value = $"G-{group}",
                Label = $"{group} (Group)",
                Kind = "group",
            });
        }

        foreach (var factory in factories)
        {
            options.Add(new PnlCompanyOption
            {
                Value = factory.Name.Trim(),
                Label = factory.Name.Trim(),
                Kind = "company",
            });
        }

        return options;
    }

    public async Task<PnlIncomeExpenseResult> GetIncomeExpenseAsync(
        string company,
        DateTime dateFrom,
        DateTime dateTo)
    {
        var companies = await ResolveCompaniesAsync(company);
        var rows = await LoadBooksPlusProvisionAsync(companies, dateFrom, dateTo);

        var heads = rows
            .GroupBy(r => (Category: r.Category, Head: CanonHead(r.Head, r.LedgerName)))
            .Select(g => new PnlHeadGroup
            {
                Category = g.Key.Category,
                Head = g.Key.Head,
                Amount = g.Sum(x => x.Amount),
                AmountLacs = Round2(g.Sum(x => x.Amount) / Lacs),
                Ledgers = g
                    .GroupBy(x => x.LedgerName, StringComparer.OrdinalIgnoreCase)
                    .Select(lg => new PnlLedgerLine
                    {
                        LedgerName = lg.Key,
                        Amount = lg.Sum(x => x.Amount),
                        AmountLacs = Round2(lg.Sum(x => x.Amount) / Lacs),
                    })
                    .OrderByDescending(x => Math.Abs(x.Amount))
                    .ToList(),
            })
            .OrderBy(h => h.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(h => h.Head, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PnlIncomeExpenseResult
        {
            Company = company,
            DateFrom = dateFrom.ToString("yyyy-MM-dd"),
            DateTo = dateTo.ToString("yyyy-MM-dd"),
            IncomeLacs = Round2(heads.Where(h => h.Category.Equals("Income", StringComparison.OrdinalIgnoreCase)).Sum(h => h.Amount) / Lacs),
            ExpenseLacs = Round2(heads.Where(h => h.Category.Equals("Expense", StringComparison.OrdinalIgnoreCase)).Sum(h => h.Amount) / Lacs),
            Heads = heads,
        };
    }

    public async Task<PnlProvisionState> GetProvisionsAsync(string company, DateTime monthStart)
    {
        if (IsAggregate(company))
            throw new ArgumentException("Pick a single company to enter provision.");

        var names = await ResolveCompaniesAsync(company);
        var companyName = names.First();
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        using var connection = _database.CreateConnection();
        var rows = (await connection.QueryAsync<PnlProvisionRow>(@"
SELECT LTRIM(RTRIM(Ledgername)) AS LedgerName,
       SUM(ISNULL(Amount, 0)) AS Amount
FROM Provisioning WITH (NOLOCK)
WHERE LTRIM(RTRIM(companyname)) = @Company
  AND sysdate BETWEEN @From AND @To
GROUP BY LTRIM(RTRIM(Ledgername))
ORDER BY LTRIM(RTRIM(Ledgername))",
            new { Company = companyName, From = monthStart, To = monthEnd },
            commandTimeout: TimeoutSeconds)).ToList();

        foreach (var row in rows)
            row.AmountLacs = Round4(row.Amount / Lacs);

        var ledgers = (await connection.QueryAsync<string>(@"
SELECT DISTINCT LTRIM(RTRIM(LedgerName))
FROM LedgerMaster WITH (NOLOCK)
WHERE CompanyName = @Company
  AND Category IN ('Expense','Income')
  AND ISNULL(LedgerName, '') <> ''
ORDER BY 1",
            new { Company = companyName })).ToList();

        return new PnlProvisionState
        {
            Company = companyName,
            Month = monthStart.ToString("yyyy-MM"),
            Rows = rows,
            LedgerOptions = ledgers,
        };
    }

    public async Task SaveProvisionsAsync(PnlProvisionSaveRequest request)
    {
        var monthStart = ParseMonth(request.Month);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        if (IsAggregate(request.Company))
            throw new ArgumentException("Pick a single company to save provision.");

        var companyName = (await ResolveCompaniesAsync(request.Company)).First();
        var rows = (request.Rows ?? new List<PnlProvisionRow>())
            .Where(r => !string.IsNullOrWhiteSpace(r.LedgerName))
            .Select(r => new
            {
                Ledger = r.LedgerName.Trim(),
                Amount = r.Amount != 0 ? r.Amount : r.AmountLacs * Lacs,
            })
            .Where(r => Math.Abs(r.Amount) > 0.0001)
            .ToList();

        using var connection = _database.CreateConnection();
        using var tx = connection.BeginTransaction();
        await connection.ExecuteAsync(@"
DELETE FROM Provisioning
WHERE LTRIM(RTRIM(companyname)) = @Company
  AND sysdate BETWEEN @From AND @To",
            new { Company = companyName, From = monthStart, To = monthEnd },
            tx);

        var entryTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in await connection.QueryAsync<(string Ledger, string Category)>(@"
SELECT LTRIM(RTRIM(LedgerName)) AS Ledger, LTRIM(RTRIM(ISNULL(Category, N'Expense'))) AS Category
FROM LedgerMaster WITH (NOLOCK)
WHERE CompanyName = @Company
  AND ISNULL(LedgerName, N'') <> N''",
            new { Company = companyName },
            tx))
        {
            if (!entryTypes.ContainsKey(item.Ledger))
                entryTypes[item.Ledger] = item.Category;
        }

        foreach (var row in rows)
        {
            var entryType = entryTypes.TryGetValue(row.Ledger, out var category) &&
                            category.Equals("Income", StringComparison.OrdinalIgnoreCase)
                ? "Income"
                : "Expense";

            await connection.ExecuteAsync(@"
INSERT INTO Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType)
VALUES (@Company, @Date, @Ledger, @Amount, @Remarks, @EntryType)",
                new
                {
                    Company = companyName,
                    Date = monthStart,
                    Ledger = row.Ledger.Length > 100 ? row.Ledger[..100] : row.Ledger,
                    Amount = row.Amount,
                    Remarks = "App P&L provision",
                    EntryType = entryType,
                },
                tx);
        }

        tx.Commit();
    }

    public async Task<PnlStockState> GetStockAsync(string company, DateTime monthStart)
    {
        if (IsAggregate(company))
            throw new ArgumentException("Pick a single company to enter stock value.");

        var companyName = (await ResolveCompaniesAsync(company)).First();
        await EnsureStockTableAsync();
        var prev = monthStart.AddMonths(-1);

        using var connection = _database.CreateConnection();
        var stored = (await connection.QueryAsync<(string Category, DateTime StockMonth, double AmountLacs)>(@"
SELECT Category, StockMonth, AmountLacs
FROM PnlStockValue WITH (NOLOCK)
WHERE LTRIM(RTRIM(CompanyName)) = @Company
  AND StockMonth IN (@Prev, @Month)",
            new { Company = companyName, Prev = prev, Month = monthStart })).ToList();

        var rows = StockCategories.Select(cat =>
        {
            var opening = stored.FirstOrDefault(s =>
                s.Category.Equals(cat, StringComparison.OrdinalIgnoreCase) && s.StockMonth == prev);
            var closing = stored.FirstOrDefault(s =>
                s.Category.Equals(cat, StringComparison.OrdinalIgnoreCase) && s.StockMonth == monthStart);
            return new PnlStockRow
            {
                Category = cat,
                Label = StockLabels[cat],
                OpeningLacs = opening.Category == null ? 0 : opening.AmountLacs,
                ClosingLacs = closing.Category == null ? 0 : closing.AmountLacs,
            };
        }).ToList();

        return new PnlStockState
        {
            Company = companyName,
            Month = monthStart.ToString("yyyy-MM"),
            Rows = rows,
        };
    }

    public async Task<PnlStockYearState> GetStockYearAsync(string company, DateTime monthInFy)
    {
        if (IsAggregate(company))
            throw new ArgumentException("Pick a single company to enter stock value.");

        var companyName = (await ResolveCompaniesAsync(company)).First();
        await EnsureStockTableAsync();
        var fyStart = FyStartYear(monthInFy);
        var openingMonth = new DateTime(fyStart, 3, 1);
        var fyMonths = FyMonths(fyStart);
        var loadMonths = fyMonths.Concat(new[] { openingMonth }).Distinct().ToList();

        using var connection = _database.CreateConnection();
        var stored = (await connection.QueryAsync<(string Category, DateTime StockMonth, double AmountLacs)>(@"
SELECT Category, StockMonth, AmountLacs
FROM PnlStockValue WITH (NOLOCK)
WHERE LTRIM(RTRIM(CompanyName)) = @Company
  AND StockMonth IN @Months",
            new { Company = companyName, Months = loadMonths })).ToList();

        double Cell(string cat, DateTime month)
        {
            var hit = stored.FirstOrDefault(s =>
                s.Category.Equals(cat, StringComparison.OrdinalIgnoreCase) && s.StockMonth.Date == month.Date);
            return hit.Category == null ? 0 : hit.AmountLacs;
        }

        var columns = fyMonths.Select(m => new PnlStockYearColumn
        {
            Key = m.ToString("yyyy-MM"),
            Label = m.ToString("MMM-yy"),
        }).ToList();

        var rows = StockCategories.Select(cat => new PnlStockYearRow
        {
            Category = cat,
            Label = StockLabels[cat],
            OpeningLacs = Cell(cat, openingMonth),
            Months = fyMonths.ToDictionary(m => m.ToString("yyyy-MM"), m => Cell(cat, m)),
        }).ToList();

        return new PnlStockYearState
        {
            Company = companyName,
            FyStart = fyStart,
            FyLabel = $"Apr-{fyStart % 100:00} to Mar-{(fyStart + 1) % 100:00}",
            Columns = columns,
            Rows = rows,
        };
    }

    public async Task SaveStockYearAsync(PnlStockYearSaveRequest request)
    {
        if (request.FyStart < 2000 || request.FyStart > 2100)
            throw new ArgumentException("Invalid financial year.");
        if (IsAggregate(request.Company))
            throw new ArgumentException("Pick a single company to save stock value.");

        var companyName = (await ResolveCompaniesAsync(request.Company)).First();
        await EnsureStockTableAsync();
        var openingMonth = new DateTime(request.FyStart, 3, 1);
        var fyMonths = FyMonths(request.FyStart);

        using var connection = _database.CreateConnection();
        foreach (var row in request.Rows ?? new List<PnlStockYearRow>())
        {
            if (string.IsNullOrWhiteSpace(row.Category) || row.Category.Equals("Total", StringComparison.OrdinalIgnoreCase))
                continue;
            await UpsertStockAsync(connection, companyName, openingMonth, row.Category, row.OpeningLacs);
            foreach (var month in fyMonths)
            {
                var key = month.ToString("yyyy-MM");
                var amount = 0d;
                if (row.Months != null)
                    row.Months.TryGetValue(key, out amount);
                await UpsertStockAsync(connection, companyName, month, row.Category, amount);
            }
        }
    }

    public async Task<PnlOverheadState> GetOverheadAsync(string company, DateTime monthStart)
    {
        if (IsAggregate(company))
            throw new ArgumentException("Pick a single company to enter Common / HO expenses.");

        var companyName = (await ResolveCompaniesAsync(company)).First();
        await EnsureOverheadTableAsync();
        using var connection = _database.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<(double CommonLacs, double HoLacs)>(@"
SELECT CommonLacs, HoLacs
FROM PnlOverhead WITH (NOLOCK)
WHERE LTRIM(RTRIM(CompanyName)) = @Company AND OverheadMonth = @Month",
            new { Company = companyName, Month = monthStart });

        return new PnlOverheadState
        {
            Company = companyName,
            Month = monthStart.ToString("yyyy-MM"),
            CommonLacs = row.CommonLacs,
            HoLacs = row.HoLacs,
        };
    }

    public async Task SaveOverheadAsync(PnlOverheadSaveRequest request)
    {
        if (IsAggregate(request.Company))
            throw new ArgumentException("Pick a single company to save Common / HO expenses.");

        var monthStart = ParseMonth(request.Month);
        var companyName = (await ResolveCompaniesAsync(request.Company)).First();
        await EnsureOverheadTableAsync();
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(@"
IF EXISTS (
    SELECT 1 FROM PnlOverhead
    WHERE CompanyName = @Company AND OverheadMonth = @Month
)
    UPDATE PnlOverhead
    SET CommonLacs = @Common, HoLacs = @Ho, UploadedAt = GETDATE()
    WHERE CompanyName = @Company AND OverheadMonth = @Month
ELSE
    INSERT INTO PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (@Company, @Month, @Common, @Ho, GETDATE())",
            new
            {
                Company = companyName,
                Month = monthStart,
                Common = request.CommonLacs,
                Ho = request.HoLacs,
            });
    }

    public async Task<List<PnlUploadItem>> GetUploadsAsync(string company, DateTime monthStart)
    {
        if (IsAggregate(company))
            throw new ArgumentException("Pick a single company to view uploads.");

        var companyName = (await ResolveCompaniesAsync(company)).First();
        await EnsureUploadTableAsync();

        using var connection = _database.CreateConnection();
        var rows = (await connection.QueryAsync<PnlUploadItem>(@"
SELECT
    Id,
    LTRIM(RTRIM(CompanyName)) AS Company,
    CONVERT(varchar(7), MonthStart, 120) AS Month,
    UploadType,
    OriginalFileName,
    ContentType,
    DATALENGTH(FileBytes) AS FileSizeBytes,
    Remarks,
    UploadedAt
FROM PnlUploadInbox WITH (NOLOCK)
WHERE LTRIM(RTRIM(CompanyName)) = @Company
  AND MonthStart = @Month
ORDER BY UploadedAt DESC, Id DESC",
            new { Company = companyName, Month = monthStart.Date })).ToList();
        return rows;
    }

    public async Task SaveUploadAsync(
        string company,
        DateTime monthStart,
        string uploadType,
        string fileName,
        string contentType,
        byte[] fileBytes,
        string? remarks = null)
    {
        if (IsAggregate(company))
            throw new ArgumentException("Pick a single company to upload a file.");

        var companyName = (await ResolveCompaniesAsync(company)).First();
        var type = NormalizeUploadType(uploadType);
        await EnsureUploadTableAsync();
        await ImportUploadAsync(companyName, monthStart.Date, type, fileBytes);
        await PersistUploadInboxAsync(companyName, monthStart.Date, type, fileName, contentType, fileBytes, remarks);
    }

    public async Task<PnlWorkbookImportResult> SaveWorkbookUploadAsync(
        string company,
        string fileName,
        string contentType,
        byte[] fileBytes,
        bool zeroCommonHo)
    {
        if (IsAggregate(company))
            throw new ArgumentException("Pick a single company to upload a workbook.");

        var companyName = (await ResolveCompaniesAsync(company)).First();
        await EnsureUploadTableAsync();

        using var workbook = new XLWorkbook(new MemoryStream(fileBytes));
        var result = new PnlWorkbookImportResult { Company = companyName };
        var months = new SortedSet<DateTime>();

        foreach (var sheet in workbook.Worksheets)
        {
            var sheetResult = new PnlWorkbookSheetResult { Sheet = sheet.Name };
            try
            {
                var type = DetectSheetUploadType(sheet);
                var monthStart = ReadSheetMonth(sheet);
                if (type is null || monthStart is null)
                {
                    sheetResult.Status = "skipped";
                    sheetResult.Detail = "Could not read upload type (B4) or month (B3).";
                    result.Sheets.Add(sheetResult);
                    continue;
                }

                sheetResult.UploadType = type;
                sheetResult.Month = monthStart.Value.ToString("yyyy-MM");
                var imported = await ImportSheetAsync(companyName, monthStart.Value, type, sheet);
                if (!imported)
                {
                    sheetResult.Status = "skipped";
                    sheetResult.Detail = "No data rows in this sheet.";
                    result.Sheets.Add(sheetResult);
                    continue;
                }

                await PersistUploadInboxAsync(
                    companyName,
                    monthStart.Value,
                    type,
                    fileName,
                    contentType,
                    fileBytes,
                    $"Dev workbook sheet '{sheet.Name}'");
                months.Add(monthStart.Value);
                sheetResult.Status = "imported";
                result.Sheets.Add(sheetResult);
            }
            catch (Exception ex)
            {
                sheetResult.Status = "error";
                sheetResult.Detail = ex.Message;
                result.Sheets.Add(sheetResult);
            }
        }

        if (zeroCommonHo && months.Count > 0)
        {
            foreach (var monthStart in months)
            {
                await SaveOverheadAsync(new PnlOverheadSaveRequest
                {
                    Company = companyName,
                    Month = monthStart.ToString("yyyy-MM"),
                    CommonLacs = 0,
                    HoLacs = 0,
                });
            }

            result.ZeroedCommonHo = true;
        }

        result.Months = months.Select(m => m.ToString("yyyy-MM")).ToList();
        if (result.Sheets.All(s => s.Status != "imported"))
        {
            var details = string.Join(" ", result.Sheets.Select(s =>
                $"{s.Sheet}: {s.Status}{(string.IsNullOrWhiteSpace(s.Detail) ? "" : $" ({s.Detail})")}."));
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(details)
                    ? "No stock or provision sheets were imported from the workbook."
                    : $"No stock or provision sheets were imported. {details}");
        }

        return result;
    }

    private async Task PersistUploadInboxAsync(
        string companyName,
        DateTime monthStart,
        string type,
        string fileName,
        string contentType,
        byte[] fileBytes,
        string? remarks)
    {
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(@"
MERGE PnlUploadInbox AS tgt
USING (
    SELECT
        @Company AS CompanyName,
        @Month AS MonthStart,
        @UploadType AS UploadType
) AS src
ON LTRIM(RTRIM(tgt.CompanyName)) = LTRIM(RTRIM(src.CompanyName))
   AND tgt.MonthStart = src.MonthStart
   AND LTRIM(RTRIM(tgt.UploadType)) = LTRIM(RTRIM(src.UploadType))
WHEN MATCHED THEN
    UPDATE SET
        OriginalFileName = @FileName,
        ContentType = @ContentType,
        FileBytes = @FileBytes,
        Remarks = @Remarks,
        UploadedAt = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (CompanyName, MonthStart, UploadType, OriginalFileName, ContentType, FileBytes, Remarks, UploadedAt)
    VALUES (@Company, @Month, @UploadType, @FileName, @ContentType, @FileBytes, @Remarks, GETDATE());",
            new
            {
                Company = companyName,
                Month = monthStart.Date,
                UploadType = type,
                FileName = fileName,
                ContentType = contentType,
                FileBytes = fileBytes,
                Remarks = remarks,
            });
    }

    public async Task<(byte[] Bytes, string FileName)> GetUploadTemplateAsync(
        string company,
        DateTime monthStart,
        string uploadType)
    {
        if (IsAggregate(company))
            throw new ArgumentException("Pick a single company to download a template.");

        var companyName = (await ResolveCompaniesAsync(company)).First();
        var type = NormalizeUploadType(uploadType);

        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(type switch
        {
            "Stock" => "Stock Upload",
            "Provision" => "Provision Upload",
            _ => "Common HO Upload",
        });

        BuildTemplateHeader(sheet, companyName, monthStart, type);

        switch (type)
        {
            case "Stock":
                BuildStockTemplate(sheet);
                break;
            case "Provision":
                var ledgers = await GetProvisionLedgerOptionsAsync(companyName);
                BuildProvisionTemplate(sheet, ledgers);
                break;
            case "CommonExpense":
                BuildCommonHoTemplate(sheet);
                break;
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        var fileName = type switch
        {
            "Stock" => $"pnl-stock-template-{SafeFilePart(companyName)}-{monthStart:yyyy-MM}.xlsx",
            "Provision" => $"pnl-provision-template-{SafeFilePart(companyName)}-{monthStart:yyyy-MM}.xlsx",
            _ => $"pnl-common-ho-template-{SafeFilePart(companyName)}-{monthStart:yyyy-MM}.xlsx",
        };

        return (ms.ToArray(), fileName);
    }

    private async Task ImportUploadAsync(string companyName, DateTime monthStart, string type, byte[] fileBytes)
    {
        using var workbook = new XLWorkbook(new MemoryStream(fileBytes));
        var sheet = type switch
        {
            "Stock" => FindWorksheet(workbook, "Stock Upload", "Stock"),
            "Provision" => FindWorksheet(workbook, "Provision Upload", "Provision"),
            "CommonExpense" => FindWorksheet(workbook, "Common HO Upload", "Common HO", "Common & HO"),
            _ => workbook.Worksheets.First(),
        };
        var imported = await ImportSheetAsync(companyName, monthStart, type, sheet);
        if (!imported)
            throw new InvalidOperationException($"No {type.ToLowerInvariant()} rows were found in the template.");
    }

    private async Task<bool> ImportSheetAsync(string companyName, DateTime monthStart, string type, IXLWorksheet sheet) =>
        type switch
        {
            "Stock" => await ImportStockTemplateAsync(companyName, monthStart, sheet),
            "Provision" => await ImportProvisionTemplateAsync(companyName, monthStart, sheet),
            "CommonExpense" => await ImportCommonHoTemplateAsync(companyName, monthStart, sheet),
            _ => throw new ArgumentException("Unsupported upload type."),
        };

    private async Task<bool> ImportStockTemplateAsync(string companyName, DateTime monthStart, IXLWorksheet sheet)
    {
        var headerRow = FindHeaderRow(sheet, "Category", "Amount Lacs");
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        var rows = new List<(string Category, double AmountLacs)>();

        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            var row = sheet.Row(r);
            var categoryText = row.Cell(1).GetString().Trim();
            var amountCell = row.Cell(2);
            if (string.IsNullOrWhiteSpace(categoryText) && amountCell.IsEmpty())
                continue;

            var category = NormalizeStockCategory(categoryText);
            var amount = ReadNumber(amountCell);
            rows.Add((category, amount));
        }

        if (rows.Count == 0)
            return false;

        await SaveStockMonthAsync(companyName, monthStart, rows);
        return true;
    }

    private async Task<bool> ImportProvisionTemplateAsync(string companyName, DateTime monthStart, IXLWorksheet sheet)
    {
        var headerRow = FindHeaderRow(sheet, "Ledger Name", "Amount Lacs");
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        var rows = new List<PnlProvisionRow>();

        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            var row = sheet.Row(r);
            var ledgerName = row.Cell(1).GetString().Trim();
            var amountCell = row.Cell(2);
            if (string.IsNullOrWhiteSpace(ledgerName) && amountCell.IsEmpty())
                continue;

            var amount = ReadNumber(amountCell);
            if (Math.Abs(amount) < 0.0001)
                continue;

            rows.Add(new PnlProvisionRow
            {
                LedgerName = ledgerName,
                Amount = amount * Lacs,
                AmountLacs = amount,
            });
        }

        if (rows.Count == 0)
            return false;

        await SaveProvisionsAsync(new PnlProvisionSaveRequest
        {
            Company = companyName,
            Month = monthStart.ToString("yyyy-MM"),
            Rows = rows,
        });
        return true;
    }

    private async Task<bool> ImportCommonHoTemplateAsync(string companyName, DateTime monthStart, IXLWorksheet sheet)
    {
        var headerRow = FindHeaderRow(sheet, "Particular", "Amount Lacs");
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        double common = 0;
        double ho = 0;
        var seen = 0;

        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            var row = sheet.Row(r);
            var label = row.Cell(1).GetString().Trim();
            var amountCell = row.Cell(2);
            if (string.IsNullOrWhiteSpace(label) && amountCell.IsEmpty())
                continue;

            var amount = ReadNumber(amountCell);
            if (label.Equals("Common expenses", StringComparison.OrdinalIgnoreCase) ||
                label.Equals("Common expense", StringComparison.OrdinalIgnoreCase) ||
                label.Equals("Common", StringComparison.OrdinalIgnoreCase))
            {
                common = amount;
                seen++;
                continue;
            }

            if (label.Equals("HO expenses", StringComparison.OrdinalIgnoreCase) ||
                label.Equals("HO expense", StringComparison.OrdinalIgnoreCase) ||
                label.Equals("HO", StringComparison.OrdinalIgnoreCase))
            {
                ho = amount;
                seen++;
            }
        }

        if (seen == 0)
            return false;

        await SaveOverheadAsync(new PnlOverheadSaveRequest
        {
            Company = companyName,
            Month = monthStart.ToString("yyyy-MM"),
            CommonLacs = common,
            HoLacs = ho,
        });
        return true;
    }

    private async Task SaveStockMonthAsync(string companyName, DateTime monthStart, IReadOnlyList<(string Category, double AmountLacs)> rows)
    {
        await EnsureStockTableAsync();
        using var connection = _database.CreateConnection();
        foreach (var row in rows)
            await UpsertStockAsync(connection, companyName, monthStart, row.Category, row.AmountLacs);
    }

    private static void BuildTemplateHeader(IXLWorksheet sheet, string companyName, DateTime monthStart, string type)
    {
        sheet.Cell("A1").Value = "P&L Upload Template";
        sheet.Range("A1:C1").Merge();
        sheet.Cell("A2").Value = "Company";
        sheet.Cell("B2").Value = companyName;
        sheet.Cell("A3").Value = "Month";
        sheet.Cell("B3").Value = monthStart.ToString("yyyy-MM");
        sheet.Cell("A4").Value = "Upload Type";
        sheet.Cell("B4").Value = type;
        sheet.Cell("A5").Value = "Instructions";
        sheet.Cell("B5").Value = "Fill the amounts in lacs and keep the sheet layout unchanged.";

        sheet.Range("A1:C1").Style.Font.Bold = true;
        sheet.Range("A1:C1").Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EEF9");
        sheet.Range("A2:A5").Style.Font.Bold = true;
        sheet.Column("A").Width = 28;
        sheet.Column("B").Width = 24;
        sheet.Column("C").Width = 40;
    }

    private static void BuildStockTemplate(IXLWorksheet sheet)
    {
        sheet.Cell("A7").Value = "Category";
        sheet.Cell("B7").Value = "Amount Lacs";
        sheet.Cell("C7").Value = "Remarks";
        sheet.Range("A7:C7").Style.Font.Bold = true;
        sheet.Range("A7:C7").Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");

        var row = 8;
        foreach (var category in StockCategories)
        {
            sheet.Cell(row, 1).Value = StockLabels[category];
            row++;
        }

        sheet.SheetView.FreezeRows(7);
    }

    private async Task<List<string>> GetProvisionLedgerOptionsAsync(string companyName)
    {
        using var connection = _database.CreateConnection();
        return (await connection.QueryAsync<string>(@"
SELECT DISTINCT LTRIM(RTRIM(LedgerName))
FROM LedgerMaster WITH (NOLOCK)
WHERE CompanyName = @Company
  AND Category IN ('Expense','Income')
  AND ISNULL(LedgerName, '') <> ''
ORDER BY 1",
            new { Company = companyName })).ToList();
    }

    private static void BuildProvisionTemplate(IXLWorksheet sheet, IReadOnlyList<string> ledgers)
    {
        sheet.Cell("A7").Value = "Ledger Name";
        sheet.Cell("B7").Value = "Amount Lacs";
        sheet.Cell("C7").Value = "Remarks";
        sheet.Range("A7:C7").Style.Font.Bold = true;
        sheet.Range("A7:C7").Style.Fill.BackgroundColor = XLColor.FromHtml("#E2F0D9");

        var row = 8;
        foreach (var ledger in ledgers)
        {
            sheet.Cell(row, 1).Value = ledger;
            row++;
        }

        if (row == 8)
        {
            sheet.Cell("A8").Value = "Sample ledger";
            sheet.Cell("B8").Value = 0;
            sheet.Cell("C8").Value = "No ledgers found for this company";
        }

        sheet.SheetView.FreezeRows(7);
    }

    private static void BuildCommonHoTemplate(IXLWorksheet sheet)
    {
        sheet.Cell("A7").Value = "Particular";
        sheet.Cell("B7").Value = "Amount Lacs";
        sheet.Cell("C7").Value = "Remarks";
        sheet.Range("A7:C7").Style.Font.Bold = true;
        sheet.Range("A7:C7").Style.Fill.BackgroundColor = XLColor.FromHtml("#FCE4D6");
        sheet.Cell("A8").Value = "Common expenses";
        sheet.Cell("A9").Value = "HO expenses";
        sheet.SheetView.FreezeRows(7);
    }

    private static IXLWorksheet FindWorksheet(XLWorkbook workbook, params string[] names)
    {
        foreach (var name in names)
        {
            var sheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (sheet is not null)
                return sheet;
        }

        return workbook.Worksheets.First();
    }

    private static string? DetectSheetUploadType(IXLWorksheet sheet)
    {
        var marked = sheet.Cell("B4").GetString().Trim();
        if (!string.IsNullOrWhiteSpace(marked))
        {
            try
            {
                return NormalizeUploadType(marked);
            }
            catch (ArgumentException)
            {
                // fall through to the sheet name
            }
        }

        var name = sheet.Name;
        if (name.Contains("stock", StringComparison.OrdinalIgnoreCase))
            return "Stock";
        if (name.Contains("provision", StringComparison.OrdinalIgnoreCase))
            return "Provision";
        if (name.Contains("common", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("ho", StringComparison.OrdinalIgnoreCase))
            return "CommonExpense";
        return null;
    }

    private static DateTime? ReadSheetMonth(IXLWorksheet sheet)
    {
        var raw = sheet.Cell("B3").GetFormattedString().Trim();
        if (string.IsNullOrWhiteSpace(raw))
            raw = sheet.Cell("B3").GetString().Trim();

        if (!string.IsNullOrWhiteSpace(raw))
        {
            if (DateTime.TryParseExact(raw, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ym))
                return new DateTime(ym.Year, ym.Month, 1);
            if (DateTime.TryParseExact(raw + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return new DateTime(parsed.Year, parsed.Month, 1);
        }

        var match = Regex.Match(sheet.Name, @"(January|February|March|April|May|June|July|August|September|October|November|December)[^\d]*(\d{2})", RegexOptions.IgnoreCase);
        if (!match.Success)
            return null;
        if (!DateTime.TryParseExact(match.Groups[1].Value, "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
            return null;
        var year = 2000 + int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        return new DateTime(year, month.Month, 1);
    }

    private static int FindHeaderRow(IXLWorksheet sheet, params string[] expectedHeaders)
    {
        var lastRow = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 1, 20);
        for (var row = 1; row <= lastRow; row++)
        {
            var values = sheet.Row(row).CellsUsed()
                .Select(c => Canon(c.GetString()))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (expectedHeaders.All(h => values.Contains(Canon(h))))
                return row;
        }

        throw new InvalidOperationException($"Could not find the header row in sheet '{sheet.Name}'.");
    }

    private static string NormalizeStockCategory(string value)
    {
        var text = Canon(value);
        foreach (var category in StockCategories)
        {
            if (Canon(category) == text)
                return category;

            if (StockLabels.TryGetValue(category, out var label) && Canon(label) == text)
                return category;
        }

        throw new InvalidOperationException($"Unknown stock category '{value}'.");
    }

    private static double ReadNumber(IXLCell cell)
    {
        if (cell.TryGetValue<double>(out var value))
            return value;

        var text = cell.GetFormattedString().Trim();
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        text = text.Replace(",", "");
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        if (double.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("en-IN"), out parsed))
            return parsed;

        return 0;
    }

    private static string SafeFilePart(string value)
    {
        var safe = Regex.Replace((value ?? "").Trim(), @"[^a-zA-Z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(safe) ? "company" : safe.ToLowerInvariant();
    }

    private static string Canon(string value)
    {
        var text = (value ?? "").Trim().ToLowerInvariant();
        return Regex.Replace(text, @"\s+", " ");
    }

    private async Task<OverheadBits> SumOverheadAsync(
        IReadOnlyList<string> companies,
        DateTime fromMonth,
        DateTime toMonth)
    {
        await EnsureOverheadTableAsync();
        var allCompanies = companies.Count == 0;
        var companyFilter = allCompanies ? new List<string> { "__none__" } : companies.ToList();
        using var connection = _database.CreateConnection();
        var row = await connection.QuerySingleAsync<(double CommonLacs, double HoLacs)>(@"
SELECT
    ISNULL(SUM(CommonLacs), 0) AS CommonLacs,
    ISNULL(SUM(HoLacs), 0) AS HoLacs
FROM PnlOverhead WITH (NOLOCK)
WHERE OverheadMonth >= @FromMonth AND OverheadMonth <= @ToMonth
  AND (@AllCompanies = 1 OR LTRIM(RTRIM(CompanyName)) IN @Companies)",
            new
            {
                FromMonth = fromMonth.Date,
                ToMonth = toMonth.Date,
                AllCompanies = allCompanies ? 1 : 0,
                Companies = companyFilter,
            });
        return new OverheadBits
        {
            Common = row.CommonLacs * Lacs,
            Ho = row.HoLacs * Lacs,
        };
    }

    private async Task EnsureOverheadTableAsync()
    {
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(@"
IF OBJECT_ID(N'dbo.PnlOverhead', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PnlOverhead (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyName VARCHAR(150) NOT NULL,
        OverheadMonth DATE NOT NULL,
        CommonLacs FLOAT NOT NULL CONSTRAINT DF_PnlOverhead_Common DEFAULT (0),
        HoLacs FLOAT NOT NULL CONSTRAINT DF_PnlOverhead_Ho DEFAULT (0),
        UploadedBy VARCHAR(100) NULL,
        UploadedAt DATETIME NOT NULL CONSTRAINT DF_PnlOverhead_Uploaded DEFAULT (GETDATE()),
        Remarks VARCHAR(500) NULL,
        CONSTRAINT UQ_PnlOverhead UNIQUE (CompanyName, OverheadMonth)
    );
END");
    }

    private async Task EnsureUploadTableAsync()
    {
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(@"
IF OBJECT_ID(N'dbo.PnlUploadInbox', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PnlUploadInbox (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyName VARCHAR(150) NOT NULL,
        MonthStart DATE NOT NULL,
        UploadType VARCHAR(30) NOT NULL,
        OriginalFileName VARCHAR(260) NOT NULL,
        ContentType VARCHAR(100) NOT NULL,
        FileBytes VARBINARY(MAX) NOT NULL,
        Remarks VARCHAR(500) NULL,
        UploadedAt DATETIME NOT NULL CONSTRAINT DF_PnlUploadInbox_UploadedAt DEFAULT (GETDATE())
    );

    CREATE UNIQUE INDEX UX_PnlUploadInbox_CompanyMonthType
    ON dbo.PnlUploadInbox (CompanyName, MonthStart, UploadType);
END");
    }

    private static int FyStartYear(DateTime month) => month.Month >= 4 ? month.Year : month.Year - 1;

    private static List<DateTime> FyMonths(int fyStart) =>
        Enumerable.Range(0, 12).Select(i => new DateTime(fyStart, 4, 1).AddMonths(i)).ToList();

    public async Task SaveStockAsync(PnlStockSaveRequest request)
    {
        var monthStart = ParseMonth(request.Month);
        if (IsAggregate(request.Company))
            throw new ArgumentException("Pick a single company to save stock value.");

        var companyName = (await ResolveCompaniesAsync(request.Company)).First();
        await EnsureStockTableAsync();
        var prev = monthStart.AddMonths(-1);

        using var connection = _database.CreateConnection();
        foreach (var row in request.Rows ?? new List<PnlStockRow>())
        {
            if (string.IsNullOrWhiteSpace(row.Category))
                continue;
            await UpsertStockAsync(connection, companyName, prev, row.Category, row.OpeningLacs);
            await UpsertStockAsync(connection, companyName, monthStart, row.Category, row.ClosingLacs);
        }
    }

    public async Task<PnlStatementResult> GetStatementAsync(string company, DateTime monthStart)
    {
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var ytdFrom = monthStart.Month >= 4
            ? new DateTime(monthStart.Year, 4, 1)
            : new DateTime(monthStart.Year - 1, 4, 1);

        var companies = await ResolveCompaniesAsync(company);
        var monthBooks = await LoadBooksPlusProvisionAsync(companies, monthStart, monthEnd);
        var ytdBooks = await LoadBooksPlusProvisionAsync(companies, ytdFrom, monthEnd);

        var monthHeads = SumHeads(monthBooks);
        var ytdHeads = SumHeads(ytdBooks);

        var stock = IsAggregate(company)
            ? new PnlStockState { Rows = StockCategories.Select(c => new PnlStockRow { Category = c, Label = StockLabels[c] }).ToList() }
            : await GetStockAsync(company, monthStart);

        var ytdStockOpening = IsAggregate(company)
            ? stock
            : await GetStockAsync(company, ytdFrom);

        var monthComputed = ComputeInventory(monthHeads, stock, useYtdOpening: false);
        var ytdComputed = ComputeInventory(ytdHeads, new PnlStockState
        {
            Rows = StockCategories.Select(cat => new PnlStockRow
            {
                Category = cat,
                OpeningLacs = ytdStockOpening.Rows.First(r => r.Category == cat).OpeningLacs,
                ClosingLacs = stock.Rows.First(r => r.Category == cat).ClosingLacs,
            }).ToList(),
        }, useYtdOpening: true);

        var monthOh = await SumOverheadAsync(companies, monthStart, monthStart);
        var ytdOh = await SumOverheadAsync(companies, ytdFrom, monthStart);

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = BuildTemplate(monthHeads, ytdHeads, monthComputed, ytdComputed, monthOh, ytdOh, used);

        var unmapped = monthHeads
            .Where(kv => !used.Contains(kv.Key) && !IsPurchaseFeed(kv.Key) && Math.Abs(kv.Value) > 0.5)
            .Select(kv => new PnlHeadGroup
            {
                Head = kv.Key,
                Amount = kv.Value,
                AmountLacs = Round2(kv.Value / Lacs),
            })
            .OrderBy(h => h.Head)
            .ToList();

        var ebitda = rows.FirstOrDefault(r => r.Id == "ebitda")?.MonthLacs ?? 0;
        var pbt = rows.FirstOrDefault(r => r.Id == "pbt")?.MonthLacs ?? 0;
        var stockIncomplete = stock.Rows.All(r => r.OpeningLacs == 0 && r.ClosingLacs == 0);

        return new PnlStatementResult
        {
            Company = company,
            Month = monthStart.ToString("yyyy-MM"),
            DateFrom = monthStart.ToString("yyyy-MM-dd"),
            DateTo = monthEnd.ToString("yyyy-MM-dd"),
            YtdFrom = ytdFrom.ToString("yyyy-MM-dd"),
            EbitdaLacs = ebitda,
            PbtLacs = pbt,
            StockIncomplete = stockIncomplete,
            Rows = rows,
            Unmapped = unmapped,
        };
    }

    public async Task<(byte[] Bytes, string FileName)> ExportStatementExcelAsync(string company, DateTime monthStart)
    {
        var statement = await GetStatementAsync(company, monthStart);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("P&L Result");
        sheet.Style.Font.FontName = "Calibri";
        sheet.Style.Font.FontSize = 11;
        sheet.ShowGridLines = false;

        var navy = XLColor.FromHtml("#1B4F72");
        var header = XLColor.FromHtml("#1F4E79");
        var teal = XLColor.FromHtml("#0E7C66");
        var gold = XLColor.FromHtml("#B7950B");
        var lineBg = XLColor.FromHtml("#F8FBFD");
        var totalBg = XLColor.FromHtml("#D6EAF8");
        var resultBg = XLColor.FromHtml("#D5F5E3");
        var kpiEbitda = XLColor.FromHtml("#E8F8F5");
        var kpiPbt = XLColor.FromHtml("#FEF9E7");

        sheet.Range(1, 1, 1, 5).Merge();
        sheet.Cell(1, 1).Value = "P&L / EBITDA Result";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 18;
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        sheet.Range(1, 1, 1, 5).Style.Fill.BackgroundColor = navy;
        sheet.Range(1, 1, 1, 5).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Row(1).Height = 28;

        sheet.Range(2, 1, 2, 5).Merge();
        sheet.Cell(2, 1).Value =
            $"{statement.Company}  •  {monthStart:MMM yyyy}  •  Month {statement.DateFrom} to {statement.DateTo}  •  YTD from {statement.YtdFrom}  •  Amounts in lacs";
        sheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#1B4F72");
        sheet.Range(2, 1, 2, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#D4E6F1");
        sheet.Row(2).Height = 20;

        var ytdEbitda = statement.Rows.FirstOrDefault(r => r.Id == "ebitda")?.YtdLacs ?? 0;
        var ytdPbt = statement.Rows.FirstOrDefault(r => r.Id == "pbt")?.YtdLacs ?? 0;
        WriteKpi(sheet, 4, 1, "Month EBITDA", statement.EbitdaLacs, kpiEbitda, teal);
        WriteKpi(sheet, 4, 2, "YTD EBITDA", ytdEbitda, kpiEbitda, teal);
        WriteKpi(sheet, 4, 4, "Month PBT", statement.PbtLacs, kpiPbt, gold);
        WriteKpi(sheet, 4, 5, "YTD PBT", ytdPbt, kpiPbt, gold);

        const int tableHeader = 7;
        string[] cols = ["Particulars", "Month", "% of turnover", "YTD", "YTD % of turnover"];
        for (var c = 0; c < cols.Length; c++)
        {
            var cell = sheet.Cell(tableHeader, c + 1);
            cell.Value = cols[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = header;
            cell.Style.Alignment.Horizontal = c == 0
                ? XLAlignmentHorizontalValues.Left
                : XLAlignmentHorizontalValues.Right;
        }

        var row = tableHeader + 1;
        foreach (var item in statement.Rows)
        {
            var isHeader = item.Kind == "header";
            sheet.Cell(row, 1).Value = item.Label;
            if (!isHeader)
            {
                SetLacs(sheet.Cell(row, 2), item.MonthLacs);
                SetPct(sheet.Cell(row, 3), item.PctToSales);
                SetLacs(sheet.Cell(row, 4), item.YtdLacs);
                SetPct(sheet.Cell(row, 5), item.YtdPctToSales);
            }

            var fill = item.Kind switch
            {
                "header" => header,
                "total" => totalBg,
                "result" => resultBg,
                _ => row % 2 == 0 ? lineBg : XLColor.White,
            };
            sheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = fill;
            if (item.Kind is "header" or "total" or "result")
                sheet.Range(row, 1, row, 5).Style.Font.Bold = true;
            if (item.Kind == "header")
                sheet.Range(row, 1, row, 5).Style.Font.FontColor = XLColor.White;
            if (item.Kind == "line")
                sheet.Cell(row, 1).Style.Alignment.Indent = 1;
            if (item.Kind == "result")
                sheet.Range(row, 1, row, 5).Style.Font.FontColor = teal;
            sheet.Range(row, 1, row, 5).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
            sheet.Range(row, 1, row, 5).Style.Border.BottomBorderColor = XLColor.FromHtml("#D5D8DC");
            row++;
        }

        var lastRow = Math.Max(tableHeader, row - 1);
        var table = sheet.Range(tableHeader, 1, lastRow, 5);
        table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.OutsideBorderColor = navy;
        sheet.Range(tableHeader, 1, lastRow, 5).SetAutoFilter();
        sheet.SheetView.FreezeRows(tableHeader);
        sheet.Column(1).Width = 58;
        sheet.Column(2).Width = 16;
        sheet.Column(3).Width = 16;
        sheet.Column(4).Width = 16;
        sheet.Column(5).Width = 20;
        sheet.SheetView.FreezeColumns(1);

        if (statement.Unmapped.Count > 0)
        {
            var unmapped = workbook.Worksheets.Add("Unmapped TB heads");
            unmapped.Style.Font.FontName = "Calibri";
            unmapped.Cell(1, 1).Value = "Head";
            unmapped.Cell(1, 2).Value = "Month amount (lacs)";
            unmapped.Range(1, 1, 1, 2).Style.Font.Bold = true;
            unmapped.Range(1, 1, 1, 2).Style.Font.FontColor = XLColor.White;
            unmapped.Range(1, 1, 1, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#922B21");
            var u = 2;
            foreach (var head in statement.Unmapped)
            {
                unmapped.Cell(u, 1).Value = head.Head;
                SetLacs(unmapped.Cell(u, 2), head.AmountLacs);
                u++;
            }

            unmapped.Range(1, 1, Math.Max(1, u - 1), 2).SetAutoFilter();
            unmapped.SheetView.FreezeRows(1);
            unmapped.Column(1).Width = 48;
            unmapped.Column(2).Width = 22;
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var file = $"pnl-{SafeFilePart(statement.Company)}-{monthStart:yyyy-MM}.xlsx";
        return (ms.ToArray(), file);
    }

    public async Task<(byte[] Bytes, string FileName)> ExportSummaryExcelAsync(DateTime monthStart)
    {
        var companies = await ResolveExportCompaniesAsync();
        var fyStart = monthStart.Month >= 4
            ? new DateTime(monthStart.Year, 4, 1)
            : new DateTime(monthStart.Year - 1, 4, 1);
        var months = new List<DateTime>();
        for (var m = fyStart; m <= monthStart; m = m.AddMonths(1))
            months.Add(m);

        var grid = new Dictionary<string, Dictionary<DateTime, (double Ebitda, double EbitdaYtd, double Pbt, double PbtYtd)>>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var company in companies)
        {
            var byMonth = new Dictionary<DateTime, (double, double, double, double)>();
            foreach (var month in months)
            {
                var stmt = await GetStatementAsync(company, month);
                var ebitdaYtd = stmt.Rows.FirstOrDefault(r => r.Id == "ebitda")?.YtdLacs ?? 0;
                var pbtYtd = stmt.Rows.FirstOrDefault(r => r.Id == "pbt")?.YtdLacs ?? 0;
                byMonth[month] = (stmt.EbitdaLacs, ebitdaYtd, stmt.PbtLacs, pbtYtd);
            }

            grid[company] = byMonth;
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("EBITDA Summary");
        sheet.Style.Font.FontName = "Calibri";
        sheet.Style.Font.FontSize = 11;
        sheet.ShowGridLines = false;

        var navy = XLColor.FromHtml("#1B4F72");
        var ebitdaHdr = XLColor.FromHtml("#0E7C66");
        var pbtHdr = XLColor.FromHtml("#B7950B");
        var ebitdaBand = XLColor.FromHtml("#E8F8F5");
        var pbtBand = XLColor.FromHtml("#FEF9E7");
        var alt = XLColor.FromHtml("#F4F6F7");

        var lastCol = 1 + months.Count * 4;
        sheet.Range(1, 1, 1, lastCol).Merge();
        sheet.Cell(1, 1).Value = "EBITDA – Summary";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 18;
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        sheet.Range(1, 1, 1, lastCol).Style.Fill.BackgroundColor = navy;
        sheet.Row(1).Height = 28;

        sheet.Range(2, 1, 2, lastCol).Merge();
        sheet.Cell(2, 1).Value =
            $"Live P&L (TB + stock + provision + Common/HO)  •  FY {fyStart:MMM yyyy} – {monthStart:MMM yyyy}  •  Amounts in lacs";
        sheet.Range(2, 1, 2, lastCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#D4E6F1");
        sheet.Cell(2, 1).Style.Font.FontColor = navy;

        sheet.Cell(4, 1).Value = "Plant";
        sheet.Range(3, 1, 4, 1).Merge();
        sheet.Range(3, 1, 4, 1).Style.Fill.BackgroundColor = navy;
        sheet.Range(3, 1, 4, 1).Style.Font.FontColor = XLColor.White;
        sheet.Range(3, 1, 4, 1).Style.Font.Bold = true;
        sheet.Range(3, 1, 4, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        for (var i = 0; i < months.Count; i++)
        {
            var start = 2 + i * 4;
            sheet.Range(3, start, 3, start + 3).Merge();
            sheet.Cell(3, start).Value = months[i].ToString("MMM yyyy");
            sheet.Cell(3, start).Style.Font.Bold = true;
            sheet.Cell(3, start).Style.Font.FontColor = XLColor.White;
            sheet.Range(3, start, 3, start + 3).Style.Fill.BackgroundColor = i % 2 == 0 ? ebitdaHdr : XLColor.FromHtml("#1A5276");
            sheet.Range(3, start, 3, start + 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] sub = ["EBITDA", "YTD", "PBT", "YTD"];
            for (var s = 0; s < 4; s++)
            {
                var cell = sheet.Cell(4, start + s);
                cell.Value = sub[s];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = s < 2 ? ebitdaHdr : pbtHdr;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        var dataRow = 5;
        foreach (var company in companies)
        {
            sheet.Cell(dataRow, 1).Value = SummaryPlantLabel(company);
            sheet.Cell(dataRow, 1).Style.Font.Bold = true;
            for (var i = 0; i < months.Count; i++)
            {
                var start = 2 + i * 4;
                var vals = grid[company][months[i]];
                SetLacs(sheet.Cell(dataRow, start), vals.Ebitda);
                SetLacs(sheet.Cell(dataRow, start + 1), vals.EbitdaYtd);
                SetLacs(sheet.Cell(dataRow, start + 2), vals.Pbt);
                SetLacs(sheet.Cell(dataRow, start + 3), vals.PbtYtd);
                sheet.Range(dataRow, start, dataRow, start + 1).Style.Fill.BackgroundColor = ebitdaBand;
                sheet.Range(dataRow, start + 2, dataRow, start + 3).Style.Fill.BackgroundColor = pbtBand;
            }

            if ((dataRow - 5) % 2 == 1)
                sheet.Cell(dataRow, 1).Style.Fill.BackgroundColor = alt;
            dataRow++;
        }

        sheet.Cell(dataRow, 1).Value = "Total Group";
        sheet.Range(dataRow, 1, dataRow, lastCol).Style.Font.Bold = true;
        sheet.Range(dataRow, 1, dataRow, lastCol).Style.Fill.BackgroundColor = navy;
        sheet.Range(dataRow, 1, dataRow, lastCol).Style.Font.FontColor = XLColor.White;
        for (var i = 0; i < months.Count; i++)
        {
            var start = 2 + i * 4;
            var month = months[i];
            SetLacs(sheet.Cell(dataRow, start), companies.Sum(c => grid[c][month].Ebitda));
            SetLacs(sheet.Cell(dataRow, start + 1), companies.Sum(c => grid[c][month].EbitdaYtd));
            SetLacs(sheet.Cell(dataRow, start + 2), companies.Sum(c => grid[c][month].Pbt));
            SetLacs(sheet.Cell(dataRow, start + 3), companies.Sum(c => grid[c][month].PbtYtd));
        }

        var lastData = dataRow;
        sheet.Range(4, 1, lastData, lastCol).SetAutoFilter();
        sheet.SheetView.FreezeRows(4);
        sheet.SheetView.FreezeColumns(1);
        sheet.Column(1).Width = 38;
        for (var c = 2; c <= lastCol; c++)
            sheet.Column(c).Width = 13;
        sheet.Range(3, 1, lastData, lastCol).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        sheet.Range(3, 1, lastData, lastCol).Style.Border.InsideBorderColor = XLColor.FromHtml("#D5D8DC");
        sheet.Range(3, 1, lastData, lastCol).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.Range(3, 1, lastData, lastCol).Style.Border.OutsideBorderColor = navy;
        sheet.SheetView.ZoomScale = 90;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return (ms.ToArray(), $"pnl-summary-{monthStart:yyyy-MM}.xlsx");
    }

    public async Task<(byte[] Bytes, string FileName)> ExportMonthGridExcelAsync(DateTime monthStart)
    {
        var companies = await ResolveExportCompaniesAsync();
        var statements = new List<(string Company, PnlStatementResult Statement)>();
        foreach (var company in companies)
            statements.Add((company, await GetStatementAsync(company, monthStart)));

        var template = statements.FirstOrDefault().Statement?.Rows
            ?? throw new InvalidOperationException("No companies found to export.");
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var ytdFrom = monthStart.Month >= 4
            ? new DateTime(monthStart.Year, 4, 1)
            : new DateTime(monthStart.Year - 1, 4, 1);

        using var workbook = new XLWorkbook();
        var sheetName = monthStart.ToString("MMM-yy", CultureInfo.InvariantCulture);
        var sheet = workbook.Worksheets.Add(sheetName);
        sheet.Style.Font.FontName = "Calibri";
        sheet.Style.Font.FontSize = 10;
        sheet.ShowGridLines = false;

        var navy = XLColor.FromHtml("#1B4F72");
        var header = XLColor.FromHtml("#1F4E79");
        var teal = XLColor.FromHtml("#0E7C66");
        var gold = XLColor.FromHtml("#B7950B");
        var totalBg = XLColor.FromHtml("#D6EAF8");
        var resultBg = XLColor.FromHtml("#D5F5E3");
        var lineBg = XLColor.FromHtml("#F8FBFD");
        var bandA = XLColor.FromHtml("#EAF2F8");
        var bandB = XLColor.FromHtml("#E8F8F5");

        var lastCol = 1 + statements.Count * 4 + 4;
        sheet.Range(1, 1, 1, lastCol).Merge();
        sheet.Cell(1, 1).Value = $"GROUP EBITDA & PBT FOR THE MONTH OF {monthStart:MMM-yy}";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 18;
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        sheet.Range(1, 1, 1, lastCol).Style.Fill.BackgroundColor = navy;
        sheet.Row(1).Height = 28;

        sheet.Range(2, 1, 2, lastCol).Merge();
        sheet.Cell(2, 1).Value =
            $"Live P&L  •  Month {monthStart:yyyy-MM-dd} to {monthEnd:yyyy-MM-dd}  •  YTD from {ytdFrom:yyyy-MM-dd}  •  Amounts in lacs";
        sheet.Range(2, 1, 2, lastCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#D4E6F1");
        sheet.Cell(2, 1).Style.Font.FontColor = navy;

        sheet.Range(3, 1, 4, 1).Merge();
        sheet.Cell(3, 1).Value = "Particulars";
        sheet.Range(3, 1, 4, 1).Style.Fill.BackgroundColor = navy;
        sheet.Range(3, 1, 4, 1).Style.Font.FontColor = XLColor.White;
        sheet.Range(3, 1, 4, 1).Style.Font.Bold = true;
        sheet.Range(3, 1, 4, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        string[] sub = [$"{monthStart:MMM-yy}", "% To Sales", "YTD", "% To Sales"];
        for (var i = 0; i < statements.Count; i++)
        {
            var start = 2 + i * 4;
            var band = i % 2 == 0 ? teal : XLColor.FromHtml("#1A5276");
            sheet.Range(3, start, 3, start + 3).Merge();
            sheet.Cell(3, start).Value = SummaryPlantNick(statements[i].Company);
            sheet.Cell(3, start).Style.Font.Bold = true;
            sheet.Cell(3, start).Style.Font.FontColor = XLColor.White;
            sheet.Range(3, start, 3, start + 3).Style.Fill.BackgroundColor = band;
            sheet.Range(3, start, 3, start + 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            sheet.Range(3, start, 3, start + 3).Style.Alignment.WrapText = true;
            for (var s = 0; s < 4; s++)
            {
                var cell = sheet.Cell(4, start + s);
                cell.Value = sub[s];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = s is 0 or 2 ? teal : gold;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        var groupStart = 2 + statements.Count * 4;
        sheet.Range(3, groupStart, 3, groupStart + 3).Merge();
        sheet.Cell(3, groupStart).Value = "Total Group";
        sheet.Cell(3, groupStart).Style.Font.Bold = true;
        sheet.Cell(3, groupStart).Style.Font.FontColor = XLColor.White;
        sheet.Range(3, groupStart, 3, groupStart + 3).Style.Fill.BackgroundColor = navy;
        sheet.Range(3, groupStart, 3, groupStart + 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        for (var s = 0; s < 4; s++)
        {
            var cell = sheet.Cell(4, groupStart + s);
            cell.Value = sub[s];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = s is 0 or 2 ? teal : gold;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        sheet.Row(3).Height = 32;

        var dataRow = 5;
        foreach (var item in template)
        {
            sheet.Cell(dataRow, 1).Value = item.Label;
            var isHeader = item.Kind == "header";
            double monthSum = 0;
            double ytdSum = 0;
            double turnM = 0;
            double turnY = 0;

            for (var i = 0; i < statements.Count; i++)
            {
                var start = 2 + i * 4;
                var match = statements[i].Statement.Rows.FirstOrDefault(r => r.Id == item.Id);
                var fill = i % 2 == 0 ? bandA : bandB;
                sheet.Range(dataRow, start, dataRow, start + 3).Style.Fill.BackgroundColor = fill;
                if (!isHeader && match is not null)
                {
                    SetLacs(sheet.Cell(dataRow, start), match.MonthLacs);
                    SetPct(sheet.Cell(dataRow, start + 1), match.PctToSales);
                    SetLacs(sheet.Cell(dataRow, start + 2), match.YtdLacs);
                    SetPct(sheet.Cell(dataRow, start + 3), match.YtdPctToSales);
                    monthSum += match.MonthLacs ?? 0;
                    ytdSum += match.YtdLacs ?? 0;
                }

                var turn = statements[i].Statement.Rows.FirstOrDefault(r => r.Id == "turnover");
                turnM += turn?.MonthLacs ?? 0;
                turnY += turn?.YtdLacs ?? 0;
            }

            if (!isHeader)
            {
                SetLacs(sheet.Cell(dataRow, groupStart), monthSum);
                SetPct(sheet.Cell(dataRow, groupStart + 1), turnM == 0 ? 0 : monthSum / turnM);
                SetLacs(sheet.Cell(dataRow, groupStart + 2), ytdSum);
                SetPct(sheet.Cell(dataRow, groupStart + 3), turnY == 0 ? 0 : ytdSum / turnY);
            }

            var rowFill = item.Kind switch
            {
                "header" => header,
                "total" => totalBg,
                "result" => resultBg,
                _ => dataRow % 2 == 0 ? lineBg : XLColor.White,
            };
            sheet.Cell(dataRow, 1).Style.Fill.BackgroundColor = rowFill;
            if (item.Kind is "header" or "total" or "result")
                sheet.Range(dataRow, 1, dataRow, lastCol).Style.Font.Bold = true;
            if (item.Kind == "header")
            {
                sheet.Range(dataRow, 1, dataRow, lastCol).Style.Fill.BackgroundColor = header;
                sheet.Range(dataRow, 1, dataRow, lastCol).Style.Font.FontColor = XLColor.White;
            }

            if (item.Kind == "result")
            {
                sheet.Range(dataRow, 1, dataRow, lastCol).Style.Fill.BackgroundColor = resultBg;
                sheet.Range(dataRow, 1, dataRow, lastCol).Style.Font.FontColor = teal;
            }
            else if (item.Kind == "total")
            {
                sheet.Range(dataRow, 1, dataRow, lastCol).Style.Fill.BackgroundColor = totalBg;
            }
            else if (item.Kind == "line")
            {
                sheet.Cell(dataRow, 1).Style.Alignment.Indent = 1;
                sheet.Cell(dataRow, 1).Style.Fill.BackgroundColor = rowFill;
            }

            sheet.Range(dataRow, groupStart, dataRow, groupStart + 3).Style.Fill.BackgroundColor =
                item.Kind == "header" ? header : XLColor.FromHtml("#F4F6F7");
            if (item.Kind == "result")
                sheet.Range(dataRow, groupStart, dataRow, groupStart + 3).Style.Fill.BackgroundColor = resultBg;
            if (item.Kind == "total")
                sheet.Range(dataRow, groupStart, dataRow, groupStart + 3).Style.Fill.BackgroundColor = totalBg;

            dataRow++;
        }

        var lastData = Math.Max(4, dataRow - 1);
        sheet.Range(4, 1, lastData, lastCol).SetAutoFilter();
        sheet.SheetView.FreezeRows(4);
        sheet.SheetView.FreezeColumns(1);
        sheet.Column(1).Width = 48;
        for (var c = 2; c <= lastCol; c++)
            sheet.Column(c).Width = 12;
        sheet.Range(3, 1, lastData, lastCol).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        sheet.Range(3, 1, lastData, lastCol).Style.Border.InsideBorderColor = XLColor.FromHtml("#D5D8DC");
        sheet.Range(3, 1, lastData, lastCol).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.Range(3, 1, lastData, lastCol).Style.Border.OutsideBorderColor = navy;
        sheet.SheetView.ZoomScale = 80;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return (ms.ToArray(), $"pnl-month-{monthStart:yyyy-MM}.xlsx");
    }

    private async Task<List<string>> ResolveExportCompaniesAsync()
    {
        var summaryNames = new HashSet<string>(SummaryPlantNames(), StringComparer.OrdinalIgnoreCase);
        var companies = (await GetCompaniesAsync())
            .Where(c => c.Kind == "company")
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(name => summaryNames.Contains(name))
            .ToList();
        if (companies.Count > 0)
            return companies;

        return (await GetCompaniesAsync())
            .Where(c => c.Kind == "company")
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void WriteKpi(
        IXLWorksheet sheet,
        int row,
        int col,
        string label,
        double value,
        XLColor fill,
        XLColor accent)
    {
        sheet.Cell(row, col).Value = label;
        sheet.Cell(row, col).Style.Font.FontSize = 9;
        sheet.Cell(row, col).Style.Font.FontColor = accent;
        sheet.Cell(row, col).Style.Fill.BackgroundColor = fill;
        sheet.Cell(row + 1, col).Value = value;
        sheet.Cell(row + 1, col).Style.NumberFormat.Format = "#,##0.00;[Red](#,##0.00);-";
        sheet.Cell(row + 1, col).Style.Font.Bold = true;
        sheet.Cell(row + 1, col).Style.Font.FontSize = 14;
        sheet.Cell(row + 1, col).Style.Font.FontColor = accent;
        sheet.Cell(row + 1, col).Style.Fill.BackgroundColor = fill;
        sheet.Range(row, col, row + 1, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.Range(row, col, row + 1, col).Style.Border.OutsideBorderColor = accent;
    }

    private static void SetLacs(IXLCell cell, double? value)
    {
        cell.Value = value ?? 0;
        cell.Style.NumberFormat.Format = "#,##0.00;[Red](#,##0.00);-";
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static void SetPct(IXLCell cell, double? value)
    {
        cell.Value = value ?? 0;
        cell.Style.NumberFormat.Format = "0.0%;[Red](0.0%);-";
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static IEnumerable<string> SummaryPlantNames() =>
        new[]
        {
            "Plastene India Limited",
            "Plastene India Limited (Unit -II)",
            "Plastene India Limited - HO",
            "HCP Plastene Bulkpack Ltd (Unit - IV)",
            "HCP Plastene Bulkpack Ltd",
            "HCP Plastene Bulkpack Ltd (Unit - II)",
            "HCP Plastene Bulkpack Ltd (Unit - III)",
            "HCP Plastene Bulkpack ltd (Vadodara)",
            "Plastene Polyfilms Limited",
            "Oswal Extrusion Limited",
            "K.P. WOVEN PRIVATE LIMITED",
            "K.P. WOVEN PRIVATE LIMITED (UNIT-II)",
            "K.P. WOVEN PRIVATE LIMITED (UNIT-III)",
        };

    private static string SummaryPlantLabel(string company)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Plastene India Limited"] = "PIL I",
            ["Plastene India Limited (Unit -II)"] = "PIL II",
            ["Plastene India Limited - HO"] = "PIL HO",
            ["HCP Plastene Bulkpack Ltd (Unit - IV)"] = "HPBL4",
            ["HCP Plastene Bulkpack Ltd"] = "HPBL I",
            ["HCP Plastene Bulkpack Ltd (Unit - II)"] = "HPBL II",
            ["HCP Plastene Bulkpack Ltd (Unit - III)"] = "HPBL III",
            ["HCP Plastene Bulkpack ltd (Vadodara)"] = "HPBL VAD",
            ["Plastene Polyfilms Limited"] = "PPL",
            ["Oswal Extrusion Limited"] = "OEL I",
            ["K.P. WOVEN PRIVATE LIMITED"] = "KP I",
            ["K.P. WOVEN PRIVATE LIMITED (UNIT-II)"] = "KP II",
            ["K.P. WOVEN PRIVATE LIMITED (UNIT-III)"] = "KP III",
        };
        return map.TryGetValue(company.Trim(), out var nick) ? $"{nick}  —  {company.Trim()}" : company.Trim();
    }

    private static string SummaryPlantNick(string company)
    {
        var label = SummaryPlantLabel(company);
        var sep = label.IndexOf("  —  ", StringComparison.Ordinal);
        return sep > 0 ? label[..sep] : label;
    }

    private async Task<List<BookRow>> LoadBooksPlusProvisionAsync(
        IReadOnlyList<string> companies,
        DateTime dateFrom,
        DateTime dateTo)
    {
        // TB for the requested range, plus closing-month provision, minus the month before the range
        // when that month is still in the same FY. FY starts in April, so March is not reversed.
        // Month Jun = TB Jun + Jun prov − May prov. YTD Apr–Jul = TB Apr–Jul + Jul prov only.
        var closingMonth = new DateTime(dateTo.Year, dateTo.Month, 1);
        var closingEnd = closingMonth.AddMonths(1).AddDays(-1);
        var reverseMonth = new DateTime(dateFrom.Year, dateFrom.Month, 1).AddMonths(-1);
        var reverseEnd = reverseMonth.AddMonths(1).AddDays(-1);
        var fyStart = dateFrom.Month >= 4
            ? new DateTime(dateFrom.Year, 4, 1)
            : new DateTime(dateFrom.Year - 1, 4, 1);
        var reverseInFy = reverseMonth >= fyStart;
        var allCompanies = companies.Count == 0;
        var companyFilter = allCompanies ? new List<string> { "__none__" } : companies.ToList();

        using var connection = _database.CreateConnection();
        var books = (await connection.QueryAsync<BookRow>(@"
SELECT
    LTRIM(RTRIM(ISNULL(L.Category, N''))) AS Category,
    LTRIM(RTRIM(ISNULL(L.Underschedule6, N''))) AS Head,
    LTRIM(RTRIM(ISNULL(VL.LedgerName, N''))) AS LedgerName,
    SUM(ISNULL(VL.Amount, 0)) AS Amount
FROM vw_LedgerSummary VL WITH (NOLOCK)
INNER JOIN LedgerMaster L WITH (NOLOCK)
    ON L.LedgerName = VL.LedgerName
   AND L.CompanyName = VL.CompanyName
WHERE ISNULL(L.Underschedule6, N'') <> N''
  AND VL.Date BETWEEN @DateFrom AND @DateTo
  AND L.Category IN (N'Expense', N'Income')
  AND (@AllCompanies = 1 OR VL.CompanyName IN @Companies)
GROUP BY LTRIM(RTRIM(ISNULL(L.Category, N''))),
         LTRIM(RTRIM(ISNULL(L.Underschedule6, N''))),
         LTRIM(RTRIM(ISNULL(VL.LedgerName, N'')))",
            new
            {
                DateFrom = dateFrom.Date,
                DateTo = dateTo.Date,
                AllCompanies = allCompanies ? 1 : 0,
                Companies = companyFilter,
            },
            commandTimeout: TimeoutSeconds)).ToList();

        var provisionNow = (await connection.QueryAsync<BookRow>(@"
SELECT
    LTRIM(RTRIM(ISNULL(L.Category, N'Expense'))) AS Category,
    LTRIM(RTRIM(ISNULL(L.Underschedule6, N''))) AS Head,
    LTRIM(RTRIM(ISNULL(VL.Ledgername, N''))) AS LedgerName,
    SUM(ISNULL(VL.Amount, 0)) AS Amount
FROM Provisioning VL WITH (NOLOCK)
INNER JOIN LedgerMaster L WITH (NOLOCK)
    ON L.LedgerName = VL.Ledgername
   AND L.CompanyName = VL.companyname
WHERE ISNULL(L.Underschedule6, N'') <> N''
  AND VL.sysdate BETWEEN @DateFrom AND @DateTo
  AND (@AllCompanies = 1 OR VL.companyname IN @Companies)
GROUP BY LTRIM(RTRIM(ISNULL(L.Category, N'Expense'))),
         LTRIM(RTRIM(ISNULL(L.Underschedule6, N''))),
         LTRIM(RTRIM(ISNULL(VL.Ledgername, N'')))",
            new
            {
                DateFrom = closingMonth,
                DateTo = closingEnd,
                AllCompanies = allCompanies ? 1 : 0,
                Companies = companyFilter,
            },
            commandTimeout: TimeoutSeconds)).ToList();

        List<BookRow> provisionPrev = new();
        if (reverseInFy)
        {
            provisionPrev = (await connection.QueryAsync<BookRow>(@"
SELECT
    LTRIM(RTRIM(ISNULL(L.Category, N'Expense'))) AS Category,
    LTRIM(RTRIM(ISNULL(L.Underschedule6, N''))) AS Head,
    LTRIM(RTRIM(ISNULL(VL.Ledgername, N''))) AS LedgerName,
    -SUM(ISNULL(VL.Amount, 0)) AS Amount
FROM Provisioning VL WITH (NOLOCK)
INNER JOIN LedgerMaster L WITH (NOLOCK)
    ON L.LedgerName = VL.Ledgername
   AND L.CompanyName = VL.companyname
WHERE ISNULL(L.Underschedule6, N'') <> N''
  AND VL.sysdate BETWEEN @DateFrom AND @DateTo
  AND (@AllCompanies = 1 OR VL.companyname IN @Companies)
GROUP BY LTRIM(RTRIM(ISNULL(L.Category, N'Expense'))),
         LTRIM(RTRIM(ISNULL(L.Underschedule6, N''))),
         LTRIM(RTRIM(ISNULL(VL.Ledgername, N'')))",
                new
                {
                    DateFrom = reverseMonth,
                    DateTo = reverseEnd,
                    AllCompanies = allCompanies ? 1 : 0,
                    Companies = companyFilter,
                },
                commandTimeout: TimeoutSeconds)).ToList();
        }

        books.AddRange(provisionNow);
        books.AddRange(provisionPrev);
        return books;
    }

    private async Task<List<string>> ResolveCompaniesAsync(string company)
    {
        if (string.IsNullOrWhiteSpace(company) || IsAll(company))
            return new List<string>();

        using var connection = _database.CreateConnection();
        if (company.StartsWith("G-", StringComparison.OrdinalIgnoreCase))
        {
            var group = company[2..].Trim();
            var names = (await connection.QueryAsync<string>(@"
SELECT Name FROM FactoryInfo WITH (NOLOCK)
WHERE LTRIM(RTRIM(ISNULL(GroupName, N''))) = @Group",
                new { Group = group })).ToList();
            return names.Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
        }

        return new List<string> { company.Trim() };
    }

    private async Task EnsureStockTableAsync()
    {
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(@"
IF OBJECT_ID(N'dbo.PnlStockValue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PnlStockValue (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyName VARCHAR(150) NOT NULL,
        PlantName VARCHAR(100) NULL,
        StockMonth DATE NOT NULL,
        Category VARCHAR(40) NOT NULL,
        AmountLacs FLOAT NOT NULL CONSTRAINT DF_PnlStockValue_Amount DEFAULT (0),
        UploadedBy VARCHAR(100) NULL,
        UploadedAt DATETIME NOT NULL CONSTRAINT DF_PnlStockValue_UploadedAt DEFAULT (GETDATE()),
        Remarks VARCHAR(500) NULL,
        CONSTRAINT UQ_PnlStockValue UNIQUE (CompanyName, PlantName, StockMonth, Category)
    );
END");
    }

    private static async Task UpsertStockAsync(
        System.Data.IDbConnection connection,
        string company,
        DateTime month,
        string category,
        double amountLacs)
    {
        await connection.ExecuteAsync(@"
IF EXISTS (
    SELECT 1 FROM PnlStockValue
    WHERE CompanyName = @Company AND StockMonth = @Month AND Category = @Category
)
    UPDATE PnlStockValue
    SET AmountLacs = @Amount, UploadedAt = GETDATE()
    WHERE CompanyName = @Company AND StockMonth = @Month AND Category = @Category
ELSE
    INSERT INTO PnlStockValue (CompanyName, StockMonth, Category, AmountLacs, UploadedAt)
    VALUES (@Company, @Month, @Category, @Amount, GETDATE())",
            new
            {
                Company = company,
                Month = month,
                Category = category,
                Amount = amountLacs,
            });
    }

    private static string NormalizeUploadType(string uploadType)
    {
        var type = (uploadType ?? "").Trim().ToLowerInvariant();
        return type switch
        {
            "stock" => "Stock",
            "provision" => "Provision",
            "common" => "CommonExpense",
            "commonexpense" => "CommonExpense",
            "common-expense" => "CommonExpense",
            "common expense" => "CommonExpense",
            _ => throw new ArgumentException("Upload type must be Stock, Provision, or CommonExpense."),
        };
    }

    private static Dictionary<string, double> SumHeads(IEnumerable<BookRow> rows)
    {
        return rows
            .GroupBy(r => CanonHead(r.Head, r.LedgerName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(PnlSignedAmount), StringComparer.OrdinalIgnoreCase);
    }

    // TB credits are negative; P&L shows income as positive (Excel style). Expense sign is unchanged.
    private static double PnlSignedAmount(BookRow row)
    {
        if (row.Category.Equals("Income", StringComparison.OrdinalIgnoreCase))
            return -row.Amount;
        return row.Amount;
    }

    private static InventoryBits ComputeInventory(
        Dictionary<string, double> heads,
        PnlStockState stock,
        bool useYtdOpening)
    {
        _ = useYtdOpening;
        double HeadSum(params string[] keys) =>
            heads.Where(kv => keys.Any(k => kv.Key.Equals(k, StringComparison.OrdinalIgnoreCase) || ContainsPurchase(kv.Key, k)))
                .Sum(kv => kv.Value);

        var rmPurch = HeadSum("Add: Purchase of raw materials", "Add : Purchase of Raw Material", "Purchase of raw materials");
        var pmPurch = HeadSum("Add: Purchase of packing materials", "Add : Purchase of Packing Material", "Purchase of packing materials");
        var purchExp = HeadSum("Add: Purchase expenses", "Add : Purchase Expenses", "Purchase expenses");
        var tradedPurch = HeadSum("Add: Purchase of traded goods", "Purchase of traded goods");
        var storesPurch = HeadSum("Add: Purchase of Stores, Spares & Consumables", "Purchase of Stores, Spares & Consumables");

        double Stock(string cat, bool opening)
        {
            var row = stock.Rows.FirstOrDefault(r => r.Category.Equals(cat, StringComparison.OrdinalIgnoreCase));
            return (opening ? row?.OpeningLacs : row?.ClosingLacs) ?? 0;
        }

        return new InventoryBits
        {
            Materials = rmPurch + pmPurch + purchExp
                        + Stock("RawMaterial", true) * Lacs + Stock("Packing", true) * Lacs
                        - Stock("RawMaterial", false) * Lacs - Stock("Packing", false) * Lacs,
            Traded = tradedPurch + Stock("Traded", true) * Lacs - Stock("Traded", false) * Lacs,
            InventoryChange = Stock("WIP", true) * Lacs + Stock("FinishedGoods", true) * Lacs
                              - Stock("WIP", false) * Lacs - Stock("FinishedGoods", false) * Lacs,
            Stores = storesPurch + Stock("Stores", true) * Lacs - Stock("Stores", false) * Lacs,
        };
    }

    private static bool ContainsPurchase(string head, string key)
    {
        var h = head.ToLowerInvariant();
        var k = key.ToLowerInvariant();
        if (k.Contains("raw") && h.Contains("purchase") && h.Contains("raw"))
            return true;
        if (k.Contains("packing") && h.Contains("purchase") && h.Contains("packing"))
            return true;
        if (k.Contains("purchase expenses") && h.Contains("purchase") && h.Contains("expense"))
            return true;
        if (k.Contains("traded") && h.Contains("purchase") && h.Contains("traded"))
            return true;
        if (k.Contains("stores") && h.Contains("purchase") && (h.Contains("store") || h.Contains("spare")))
            return true;
        return false;
    }

    private static bool IsPurchaseFeed(string head)
    {
        var h = head.ToLowerInvariant();
        return h.Contains("purchase of raw")
            || h.Contains("purchase of packing")
            || h.Contains("purchase expenses")
            || h.Contains("purchase of traded")
            || h.Contains("purchase of stores")
            || h.Contains("purchase of stock");
    }

    private static List<PnlStatementRow> BuildTemplate(
        Dictionary<string, double> month,
        Dictionary<string, double> ytd,
        InventoryBits monthInv,
        InventoryBits ytdInv,
        OverheadBits ohMonth,
        OverheadBits ohYtd,
        HashSet<string> used)
    {
        double Line(string name)
        {
            used.Add(name);
            month.TryGetValue(name, out var m);
            return m;
        }

        double Ytd(string name)
        {
            ytd.TryGetValue(name, out var v);
            return v;
        }

        var built = new List<PnlStatementRow>();
        void H(string id, string label) =>
            built.Add(new PnlStatementRow { Id = id, Label = label, Kind = "header" });

        void L(string id, string label)
        {
            var m = Line(label);
            var y = Ytd(label);
            built.Add(Make(id, label, "line", m, y));
        }

        void C(string id, string label, double m, double y) =>
            built.Add(Make(id, label, "computed", m, y));

        void T(string id, string label, double m, double y) =>
            built.Add(Make(id, label, "total", m, y));

        H("rev", "Revenue from operations");
        L("fg", "Sale of finished goods");
        L("traded", "Sale of traded goods");
        var revM = Val(built, "fg") + Val(built, "traded");
        var revY = YtdVal(built, "fg") + YtdVal(built, "traded");
        T("revTotal", "Total Revenue from operations", revM, revY);

        H("svc", "Sale of services");
        L("job", "Income from Jobwork charges");
        T("svcTotal", "Total Sales of services", Val(built, "job"), YtdVal(built, "job"));

        H("oop", "Other operating revenue");
        L("export", "Export incentives");
        L("commission", "Commission income");
        L("scrap", "Scrap sales");
        T("oopTotal", "Total Other operating revenue",
            Val(built, "export") + Val(built, "commission") + Val(built, "scrap"),
            YtdVal(built, "export") + YtdVal(built, "commission") + YtdVal(built, "scrap"));

        H("oi", "Other income");
        L("wind", "Wind mill income");
        L("intFd", "- on fixed deposits with Banks");
        L("intOth", "- from others");
        L("rent", "Rent Income");
        L("intCust", "Interest Received From Customers");
        L("pfa", "Profit on sale of fixed assets");
        L("pinv", "Profit on sale of investments");
        L("sundry", "Sundry balance written off");
        L("misc", "Miscellaneous income");
        T("oiTotal", "Total Other income",
            Val(built, "wind") + Val(built, "intFd") + Val(built, "intOth") + Val(built, "rent")
            + Val(built, "intCust") + Val(built, "pfa") + Val(built, "pinv") + Val(built, "sundry") + Val(built, "misc"),
            YtdVal(built, "wind") + YtdVal(built, "intFd") + YtdVal(built, "intOth") + YtdVal(built, "rent")
            + YtdVal(built, "intCust") + YtdVal(built, "pfa") + YtdVal(built, "pinv") + YtdVal(built, "sundry") + YtdVal(built, "misc"));

        var turnM = Val(built, "revTotal") + Val(built, "svcTotal") + Val(built, "oopTotal") + Val(built, "oiTotal");
        var turnY = YtdVal(built, "revTotal") + YtdVal(built, "svcTotal") + YtdVal(built, "oopTotal") + YtdVal(built, "oiTotal");
        T("turnover", "Total Turnover", turnM, turnY);

        H("cogs", "Cost of Goods");
        C("materials", "Cost of materials consumed", monthInv.Materials, ytdInv.Materials);
        C("sit", "Purchase of stock-in-trade", monthInv.Traded, ytdInv.Traded);
        C("invchg", "Changes in inventory of finished goods and work-in-progress", monthInv.InventoryChange, ytdInv.InventoryChange);
        C("stores", "Stores, Spares & Consumables", monthInv.Stores, ytdInv.Stores);
        T("cogsTotal", "Total Cost of goods",
            Val(built, "materials") + Val(built, "sit") + Val(built, "invchg") + Val(built, "stores"),
            YtdVal(built, "materials") + YtdVal(built, "sit") + YtdVal(built, "invchg") + YtdVal(built, "stores"));

        H("emp", "Employee benefit expenses");
        L("sal", "Salaries, wages and  bonus");
        L("pf", "Contribution to provident and other funds");
        L("grat", "Gratuity Expenses");
        L("welfare", "Staff welfare expenses");
        L("labour", "Labour charges");
        T("empTotal", "Total Employee benefit expenses",
            Val(built, "sal") + Val(built, "pf") + Val(built, "grat") + Val(built, "welfare") + Val(built, "labour"),
            YtdVal(built, "sal") + YtdVal(built, "pf") + YtdVal(built, "grat") + YtdVal(built, "welfare") + YtdVal(built, "labour"));

        H("mfg", "Manufacturing Expenses");
        L("power", "Power and fuel");
        L("jw", "Jobwork charges");
        L("omfg", "Other Manufacturing Expenses");
        T("mfgTotal", "Total Manufacturing Expenses",
            Val(built, "power") + Val(built, "jw") + Val(built, "omfg"),
            YtdVal(built, "power") + YtdVal(built, "jw") + YtdVal(built, "omfg"));

        H("sell", "Selling and Distribution Expenses");
        L("ads", "Advertising and sales promotion expense");
        L("travel", "Travelling expense");
        L("broker", "Brokerage & commission");
        L("conv", "Conveyance, vehicle running and traveling expenses");
        L("freight", "Freight Outward and C & F Charges");
        T("sellTotal", "Total Selling and Distribution Expenses",
            Val(built, "ads") + Val(built, "travel") + Val(built, "broker") + Val(built, "conv") + Val(built, "freight"),
            YtdVal(built, "ads") + YtdVal(built, "travel") + YtdVal(built, "broker") + YtdVal(built, "conv") + YtdVal(built, "freight"));

        H("admin", "Administrative Expenses");
        L("ins", "Insurance");
        L("miscExp", "Miscellaneous Expense");
        L("oadm", "Other Administrative Expenses");
        L("fx", "Exchange Rate Difference");
        C("common", "Common Expenses", ohMonth.Common, ohYtd.Common);
        C("ho", "HO Expenses", ohMonth.Ho, ohYtd.Ho);
        T("adminTotal", "Total Administrative Expenses",
            Val(built, "ins") + Val(built, "miscExp") + Val(built, "oadm") + Val(built, "fx")
            + Val(built, "common") + Val(built, "ho"),
            YtdVal(built, "ins") + YtdVal(built, "miscExp") + YtdVal(built, "oadm") + YtdVal(built, "fx")
            + YtdVal(built, "common") + YtdVal(built, "ho"));

        var soldM = Val(built, "cogsTotal") + Val(built, "empTotal") + Val(built, "mfgTotal")
                    + Val(built, "sellTotal") + Val(built, "adminTotal");
        var soldY = YtdVal(built, "cogsTotal") + YtdVal(built, "empTotal") + YtdVal(built, "mfgTotal")
                    + YtdVal(built, "sellTotal") + YtdVal(built, "adminTotal");
        T("sold", "Total Cost of goods sold", soldM, soldY);

        var ebitdaM = turnM - soldM;
        var ebitdaY = turnY - soldY;
        built.Add(Make("ebitda", "EBITDA", "result", ebitdaM, ebitdaY));

        L("dep", "Depreciation");
        L("bank", "Bank Charges & Commission");
        L("intO", "Interest Expense - Other");
        L("intTl", "Interest on Term Loan");
        L("intWc", "Interest on Working Capital & Term Loan");

        var pbtM = ebitdaM - Val(built, "dep") - Val(built, "bank") - Val(built, "intO")
                   - Val(built, "intTl") - Val(built, "intWc");
        var pbtY = ebitdaY - YtdVal(built, "dep") - YtdVal(built, "bank") - YtdVal(built, "intO")
                   - YtdVal(built, "intTl") - YtdVal(built, "intWc");
        built.Add(Make("pbt", "PBT", "result", pbtM, pbtY));
        built.Add(Make("cashPbt", "Cash PBT", "result", pbtM + Val(built, "dep"), pbtY + YtdVal(built, "dep")));

        ApplyPct(built, turnM, turnY);
        return built;
    }

    private static PnlStatementRow Make(string id, string label, string kind, double monthRupees, double ytdRupees) =>
        new()
        {
            Id = id,
            Label = label,
            Kind = kind,
            MonthLacs = Round2(monthRupees / Lacs),
            YtdLacs = Round2(ytdRupees / Lacs),
        };

    private static double Val(List<PnlStatementRow> rows, string id) =>
        (rows.FirstOrDefault(r => r.Id == id)?.MonthLacs ?? 0) * Lacs;

    private static double YtdVal(List<PnlStatementRow> rows, string id) =>
        (rows.FirstOrDefault(r => r.Id == id)?.YtdLacs ?? 0) * Lacs;

    private static void ApplyPct(List<PnlStatementRow> rows, double turnM, double turnY)
    {
        foreach (var row in rows)
        {
            if (row.MonthLacs == null)
                continue;
            row.PctToSales = turnM == 0 ? 0 : Round4((row.MonthLacs.Value * Lacs) / turnM);
            row.YtdPctToSales = turnY == 0 ? 0 : Round4((row.YtdLacs!.Value * Lacs) / turnY);
        }
    }

    private static string CanonHead(string? raw, string? ledgerName = null)
    {
        if (!string.IsNullOrWhiteSpace(ledgerName) &&
            LedgerHeadOverrides.TryGetValue(ledgerName.Trim(), out var ledgerHead))
            return ledgerHead;

        var s = Regex.Replace((raw ?? "").Trim(), @"\s+", " ");
        if (HeadAliases.TryGetValue(s, out var mapped))
            return mapped;
        var key = s.ToLowerInvariant();
        if (HeadAliases.TryGetValue(key, out mapped))
            return mapped;
        return s;
    }

    public static DateTime ParseMonth(string? month)
    {
        if (string.IsNullOrWhiteSpace(month))
            throw new ArgumentException("Month is required (yyyy-MM).");
        if (DateTime.TryParseExact(month.Trim() + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d))
            return d;
        throw new ArgumentException("Month must be yyyy-MM.");
    }

    private static bool IsAll(string company) =>
        company.Equals("All Companies", StringComparison.OrdinalIgnoreCase)
        || company == "*"
        || company.Equals("all", StringComparison.OrdinalIgnoreCase);

    private static bool IsAggregate(string company) =>
        IsAll(company) || company.StartsWith("G-", StringComparison.OrdinalIgnoreCase);

    private static double Round2(double v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    private static double Round4(double v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

    private sealed class BookRow
    {
        public string Category { get; set; } = "";
        public string Head { get; set; } = "";
        public string LedgerName { get; set; } = "";
        public double Amount { get; set; }
    }

    private sealed class InventoryBits
    {
        public double Materials { get; set; }
        public double Traded { get; set; }
        public double InventoryChange { get; set; }
        public double Stores { get; set; }
    }

    private sealed class OverheadBits
    {
        public double Common { get; set; }
        public double Ho { get; set; }
    }
}
