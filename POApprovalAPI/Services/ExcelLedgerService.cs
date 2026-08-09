using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class ExcelLedgerService
{
    private static readonly string[] CompanyAliases = ["company", "company name", "firm", "firm name", "entity"];
    private static readonly string[] DateAliases = ["date", "txn date", "transaction date", "vch date", "voucher date", "doc date"];
    private static readonly string[] ParticularsAliases = ["particulars", "narration", "description", "remarks", "account", "ledger"];
    private static readonly string[] VoucherNoAliases = ["voucher no", "voucher no.", "vch no", "vch no.", "voucher number", "doc no", "doc no."];
    private static readonly string[] VoucherRefAliases = ["voucher ref", "vch ref", "ref", "reference", "bank ref", "utr", "cheque no", "chq no"];
    private static readonly string[] DebitAliases =
    [
        "debit inr", "debit (inr)", "debit rs", "dr inr", "dr (inr)",
        "debit", "dr", "debit amount", "withdrawal", "dr amount"
    ];
    private static readonly string[] CreditAliases =
    [
        "credit inr", "credit (inr)", "credit rs", "cr inr", "cr (inr)",
        "credit", "cr", "credit amount", "deposit", "cr amount"
    ];
    private static readonly string[] ForeignAmountMarkers = ["fc", "foreign", "usd", "eur", "$"];
    private static readonly string[] InrAmountMarkers = ["inr", "rs", "rupee", "₹"];

    public ExcelPreviewResponse Preview(Stream stream, string fileName, string? sheetName = null, int? headerRow = null)
    {
        using var workbook = new XLWorkbook(stream);
        var sheetNames = workbook.Worksheets.Select(w => w.Name).ToList();
        if (sheetNames.Count == 0)
            throw new InvalidOperationException("Workbook has no worksheets.");

        var sheet = ResolveSheet(workbook, sheetName, sheetNames);
        var detectedHeaderRow = headerRow ?? DetectHeaderRow(sheet);
        var headers = ReadHeaders(sheet, detectedHeaderRow);
        if (headers.Count == 0)
            throw new InvalidOperationException("Could not detect column headers. Check the selected sheet and header row.");

        var mapping = SuggestMapping(headers);
        mapping.SheetName = sheet.Name;
        mapping.HeaderRow = detectedHeaderRow;

        var sampleRows = ReadSampleRows(sheet, detectedHeaderRow, headers, 5);
        var dataRowCount = CountDataRows(sheet, detectedHeaderRow, headers);

        return new ExcelPreviewResponse
        {
            FileName = fileName,
            SheetNames = sheetNames,
            SelectedSheet = sheet.Name,
            HeaderRow = detectedHeaderRow,
            Headers = headers,
            SuggestedMapping = mapping,
            DataRowCount = dataRowCount,
            SampleRows = sampleRows
        };
    }

    public ComparisonResultDto Compare(
        Stream streamA,
        string fileNameA,
        Stream streamB,
        string fileNameB,
        LedgerColumnMapping mappingA,
        LedgerColumnMapping mappingB,
        LedgerMatchOptions? options = null)
    {
        options ??= new LedgerMatchOptions();

        var entriesA = ParseLedger(streamA, "Company A", mappingA);
        var entriesB = ParseLedger(streamB, "Company B", mappingB);

        var result = Reconcile(entriesA, entriesB, options);
        result.CompanyNameA = ResolveLedgerName(entriesA, "Company A");
        result.CompanyNameB = ResolveLedgerName(entriesB, "Company B");
        return result;
    }

    public byte[] BuildExport(ComparisonResultDto result)
    {
        var nameA = string.IsNullOrWhiteSpace(result.CompanyNameA) ? "Company A" : result.CompanyNameA.Trim();
        var nameB = string.IsNullOrWhiteSpace(result.CompanyNameB) ? "Company B" : result.CompanyNameB.Trim();

        using var workbook = new XLWorkbook();
        var summary = workbook.Worksheets.Add("Summary");

        summary.Cell(1, 1).Value = "Ledger A";
        summary.Cell(1, 2).Value = nameA;
        summary.Cell(2, 1).Value = "Ledger B";
        summary.Cell(2, 2).Value = nameB;

        StyleSummaryLabel(summary.Cell(1, 1), XLColor.FromHtml("#1F4E79"), XLColor.White);
        StyleSummaryLabel(summary.Cell(2, 1), XLColor.FromHtml("#1F4E79"), XLColor.White);
        summary.Cell(1, 2).Style.Font.Bold = true;
        summary.Cell(2, 2).Style.Font.Bold = true;
        summary.Range(1, 1, 2, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        summary.Range(1, 1, 2, 2).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        summary.Cell(4, 1).Value = "Metric";
        summary.Cell(4, 2).Value = "Count";
        var metricHeader = summary.Range(4, 1, 4, 2);
        metricHeader.Style.Font.Bold = true;
        metricHeader.Style.Font.FontColor = XLColor.White;
        metricHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#2F5496");
        metricHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var rows = new (string Label, int Value, string Tone)[]
        {
            ($"Total — {nameA}", result.Summary.TotalA, "total"),
            ($"Total — {nameB}", result.Summary.TotalB, "total"),
            ("Matched", result.Summary.Matched, "matched"),
            ("Amount Mismatch", result.Summary.AmountMismatch, "mismatch"),
            ($"Missing in {nameA}", result.Summary.MissingInA, "missing"),
            ($"Missing in {nameB}", result.Summary.MissingInB, "missing"),
            ("Duplicates", result.Summary.Duplicates, "other"),
            ("Potential Matches", result.Summary.PotentialMatches, "other"),
        };
        for (var i = 0; i < rows.Length; i++)
        {
            var excelRow = i + 5;
            summary.Cell(excelRow, 1).Value = rows[i].Label;
            summary.Cell(excelRow, 2).Value = rows[i].Value;
            ApplySummaryRowTone(summary.Range(excelRow, 1, excelRow, 2), rows[i].Tone);
            summary.Cell(excelRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            summary.Cell(excelRow, 2).Style.Font.Bold = true;
        }
        summary.Range(4, 1, 4 + rows.Length, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        summary.Range(4, 1, 4 + rows.Length, 2).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        summary.Columns().AdjustToContents();

        var hasMatched = result.Results.Any(r => r.Status == "Matched");
        var detail = workbook.Worksheets.Add(hasMatched ? "Results" : "Discrepancies");
        var headers = new[]
        {
            "Status", "Message", "Difference",
            $"{nameA} Date", $"{nameA} Voucher", $"{nameA} Ref", $"{nameA} Particulars", $"{nameA} Debit", $"{nameA} Credit",
            $"{nameB} Date", $"{nameB} Voucher", $"{nameB} Ref", $"{nameB} Particulars", $"{nameB} Debit", $"{nameB} Credit"
        };
        for (var c = 0; c < headers.Length; c++)
            detail.Cell(1, c + 1).Value = headers[c];

        var headerRange = detail.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2F5496");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.WrapText = true;

        var exportRows = result.Results
            .Where(r => r.Status is not "Matched")
            .Concat(result.Results.Where(r => r.Status == "Matched"))
            .ToList();

        for (var i = 0; i < exportRows.Count; i++)
        {
            var r = exportRows[i];
            var row = i + 2;
            detail.Cell(row, 1).Value = FormatExportStatus(r.Status, nameA, nameB);
            detail.Cell(row, 2).Value = r.Message;
            if (r.Difference.HasValue) detail.Cell(row, 3).Value = r.Difference.Value;
            WriteEntry(detail, row, 4, r.EntryA);
            WriteEntry(detail, row, 10, r.EntryB);
            ApplyStatusCellTone(detail.Cell(row, 1), r.Status);
        }

        var lastDataRow = Math.Max(1, exportRows.Count + 1);
        var tableRange = detail.Range(1, 1, lastDataRow, headers.Length);
        tableRange.SetAutoFilter();
        detail.SheetView.FreezeRows(1);
        detail.Columns().AdjustToContents();
        // Cap very wide particulars columns for readability
        detail.Column(7).Width = Math.Min(detail.Column(7).Width, 40);
        detail.Column(13).Width = Math.Min(detail.Column(13).Width, 40);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
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
            "mismatch" => (XLColor.FromHtml("#FFEB9C"), XLColor.FromHtml("#9C5700")),
            "missing" => (XLColor.FromHtml("#FFC7CE"), XLColor.FromHtml("#9C0006")),
            "other" => (XLColor.FromHtml("#DDEBF7"), XLColor.FromHtml("#1F4E79")),
            _ => (XLColor.FromHtml("#E7E6E6"), XLColor.FromHtml("#333333")),
        };
        range.Style.Fill.BackgroundColor = bg;
        range.Style.Font.FontColor = fg;
    }

    private static void ApplyStatusCellTone(IXLCell cell, string status)
    {
        var (bg, fg) = status switch
        {
            "Matched" => (XLColor.FromHtml("#C6EFCE"), XLColor.FromHtml("#006100")),
            "AmountMismatch" => (XLColor.FromHtml("#FFEB9C"), XLColor.FromHtml("#9C5700")),
            "MissingInA" or "MissingInB" => (XLColor.FromHtml("#FFC7CE"), XLColor.FromHtml("#9C0006")),
            "Duplicate" => (XLColor.FromHtml("#DDEBF7"), XLColor.FromHtml("#1F4E79")),
            "PotentialMatch" => (XLColor.FromHtml("#E2D5F1"), XLColor.FromHtml("#5B2C6F")),
            _ => (XLColor.FromHtml("#E7E6E6"), XLColor.FromHtml("#333333")),
        };
        cell.Style.Fill.BackgroundColor = bg;
        cell.Style.Font.FontColor = fg;
        cell.Style.Font.Bold = true;
    }

    private static string FormatExportStatus(string status, string nameA, string nameB) => status switch
    {
        "MissingInA" => $"Missing in {nameA}",
        "MissingInB" => $"Missing in {nameB}",
        "AmountMismatch" => "Amount Mismatch",
        "PotentialMatch" => "Potential Match",
        _ => status
    };

    private static void WriteEntry(IXLWorksheet sheet, int row, int startCol, LedgerEntryDto? entry)
    {
        if (entry == null) return;
        sheet.Cell(row, startCol).Value = entry.Date?.ToString("dd-MM-yyyy") ?? "";
        sheet.Cell(row, startCol + 1).Value = entry.VoucherNo;
        sheet.Cell(row, startCol + 2).Value = entry.VoucherRef;
        sheet.Cell(row, startCol + 3).Value = entry.Particulars;
        sheet.Cell(row, startCol + 4).Value = entry.Debit;
        sheet.Cell(row, startCol + 5).Value = entry.Credit;
    }

    public List<LedgerEntryDto> ParseLedger(Stream stream, string company, LedgerColumnMapping mapping)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = ResolveSheet(workbook, mapping.SheetName, workbook.Worksheets.Select(w => w.Name).ToList());
        var headerRow = mapping.HeaderRow <= 0 ? DetectHeaderRow(sheet) : mapping.HeaderRow;
        var headers = ReadHeaders(sheet, headerRow);
        var colIndex = BuildColumnIndex(headers);

        RequireMapped(mapping.Date, "Date");
        RequireMapped(mapping.Debit, "Debit");
        RequireMapped(mapping.Credit, "Credit");

        var dateCol = ResolveColumn(colIndex, mapping.Date!);
        var debitCol = ResolveColumn(colIndex, mapping.Debit!);
        var creditCol = ResolveColumn(colIndex, mapping.Credit!);
        var particularsCol = string.IsNullOrWhiteSpace(mapping.Particulars) ? -1 : ResolveColumn(colIndex, mapping.Particulars);
        var voucherCol = string.IsNullOrWhiteSpace(mapping.VoucherNo) ? -1 : ResolveColumn(colIndex, mapping.VoucherNo);
        var refCol = string.IsNullOrWhiteSpace(mapping.VoucherRef) ? -1 : ResolveColumn(colIndex, mapping.VoucherRef);
        var companyCol = string.IsNullOrWhiteSpace(mapping.Company) ? -1 : ResolveColumn(colIndex, mapping.Company);

        var entries = new List<LedgerEntryDto>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;

        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            var row = sheet.Row(r);
            if (IsEmptyRow(row, headers.Count)) continue;

            var date = ParseDate(row.Cell(dateCol));
            var debit = ParseAmount(row.Cell(debitCol));
            var credit = ParseAmount(row.Cell(creditCol));
            if (debit == 0 && credit == 0 && date == null) continue;

            var particulars = particularsCol > 0 ? NormalizeText(row.Cell(particularsCol).GetString()) : "";
            // Skip opening/closing balance style rows
            if (IsBalanceRow(particulars)) continue;

            var companyName = companyCol > 0
                ? NormalizeText(row.Cell(companyCol).GetFormattedString())
                : "";
            if (string.IsNullOrWhiteSpace(companyName))
                companyName = company;

            entries.Add(new LedgerEntryDto
            {
                RowIndex = r,
                Company = companyName,
                Date = date,
                Particulars = particulars,
                VoucherNo = voucherCol > 0 ? NormalizeText(row.Cell(voucherCol).GetFormattedString()) : "",
                VoucherRef = refCol > 0 ? NormalizeText(row.Cell(refCol).GetFormattedString()) : "",
                Debit = debit,
                Credit = credit
            });
        }

        return entries;
    }

    private static string ResolveLedgerName(List<LedgerEntryDto> entries, string fallback)
    {
        var name = entries
            .Select(e => e.Company?.Trim() ?? "")
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Where(c => !string.Equals(c, "A", StringComparison.OrdinalIgnoreCase))
            .Where(c => !string.Equals(c, "B", StringComparison.OrdinalIgnoreCase))
            .Where(c => !string.Equals(c, "Company A", StringComparison.OrdinalIgnoreCase))
            .Where(c => !string.Equals(c, "Company B", StringComparison.OrdinalIgnoreCase))
            .GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.First())
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(name) ? fallback : name;
    }

    private ComparisonResultDto Reconcile(List<LedgerEntryDto> entriesA, List<LedgerEntryDto> entriesB, LedgerMatchOptions options)
    {
        var results = new List<ComparisonPairDto>();
        var usedB = new HashSet<int>();
        var duplicateKeysA = FindDuplicateKeys(entriesA);
        var duplicateKeysB = FindDuplicateKeys(entriesB);

        // Index B by amount bucket for fast candidate lookup
        var indexB = new Dictionary<string, List<LedgerEntryDto>>();
        foreach (var b in entriesB)
        {
            foreach (var key in BuildLookupKeys(b, options))
            {
                if (!indexB.TryGetValue(key, out var list))
                {
                    list = new List<LedgerEntryDto>();
                    indexB[key] = list;
                }
                list.Add(b);
            }
        }

        foreach (var a in entriesA)
        {
            var candidates = new List<LedgerEntryDto>();
            foreach (var key in BuildLookupKeys(a, options))
            {
                if (indexB.TryGetValue(key, out var list))
                    candidates.AddRange(list);
            }

            candidates = candidates
                .Where(b => !usedB.Contains(b.RowIndex))
                .DistinctBy(b => b.RowIndex)
                .ToList();

            if (candidates.Count == 0)
            {
                // Try date-flexible / amount-only potential matches
                var potential = FindPotential(a, entriesB, usedB, options);
                if (potential != null)
                {
                    usedB.Add(potential.RowIndex);
                    // Identity hits (ref/vno) get full classify; loose near-amount stays PotentialMatch.
                    var fallbackStatus = HasSharedIdentity(a, potential, options)
                        ? ClassifyPair(a, potential, options)
                        : Math.Abs(a.Amount - potential.Amount) > options.AmountTolerance
                            ? "AmountMismatch"
                            : "PotentialMatch";

                    results.Add(new ComparisonPairDto
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Status = fallbackStatus,
                        Message = StatusMessage(fallbackStatus, a, potential, options),
                        Difference = fallbackStatus == "AmountMismatch" ? Math.Abs(a.Amount - potential.Amount) : null,
                        EntryA = a,
                        EntryB = potential
                    });
                }
                else
                {
                    results.Add(new ComparisonPairDto
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Status = "MissingInB",
                        Message = "No corresponding transaction found in File B",
                        EntryA = a
                    });
                }
                continue;
            }

            var best = RankCandidates(a, candidates, options).First();
            usedB.Add(best.RowIndex);

            var isDuplicate = duplicateKeysA.Contains(DuplicateKey(a)) || duplicateKeysB.Contains(DuplicateKey(best));
            var pairStatus = ClassifyPair(a, best, options);
            // Shared voucher ref / voucher no uniquely identifies the pair — keep Matched.
            if (isDuplicate && pairStatus == "Matched" && !HasSharedIdentity(a, best, options))
                pairStatus = "Duplicate";

            results.Add(new ComparisonPairDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = pairStatus,
                Message = StatusMessage(pairStatus, a, best, options),
                Difference = pairStatus == "AmountMismatch" ? Math.Abs(a.Amount - best.Amount) : null,
                EntryA = a,
                EntryB = best
            });
        }

        foreach (var b in entriesB.Where(x => !usedB.Contains(x.RowIndex)))
        {
            results.Add(new ComparisonPairDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = "MissingInA",
                Message = "No corresponding transaction found in File A",
                EntryB = b
            });
        }

        return new ComparisonResultDto
        {
            Summary = new ComparisonSummary
            {
                TotalA = entriesA.Count,
                TotalB = entriesB.Count,
                Matched = results.Count(r => r.Status == "Matched"),
                AmountMismatch = results.Count(r => r.Status == "AmountMismatch"),
                MissingInA = results.Count(r => r.Status == "MissingInA"),
                MissingInB = results.Count(r => r.Status == "MissingInB"),
                Duplicates = results.Count(r => r.Status == "Duplicate"),
                PotentialMatches = results.Count(r => r.Status == "PotentialMatch"),
            },
            Results = results
        };
    }

    private static string ClassifyPair(LedgerEntryDto a, LedgerEntryDto b, LedgerMatchOptions options)
    {
        var oppositeOk =
            (a.Debit > 0 && b.Credit > 0) ||
            (a.Credit > 0 && b.Debit > 0);

        var amountDiff = Math.Abs(a.Amount - b.Amount);
        if (amountDiff > options.AmountTolerance)
            return "AmountMismatch";

        if (!oppositeOk)
            return "PotentialMatch";

        // Strong match when opposite sides + amount reconcile
        return "Matched";
    }

    private static string StatusMessage(string status, LedgerEntryDto a, LedgerEntryDto b, LedgerMatchOptions? options = null) => status switch
    {
        "Matched" when HasSharedIdentity(a, b, options ?? new LedgerMatchOptions())
            => $"{a.Side} {a.Amount:N2} reconciles with {b.Side} {b.Amount:N2} (shared voucher identity)",
        "Matched" => $"{a.Side} {a.Amount:N2} reconciles with {b.Side} {b.Amount:N2}",
        "AmountMismatch" when SameVoucherRef(a, b)
            => $"Same voucher ref but amounts differ by {Math.Abs(a.Amount - b.Amount):N2}",
        "AmountMismatch" when SameVoucherNo(a, b)
            => $"Same voucher no but amounts differ by {Math.Abs(a.Amount - b.Amount):N2}",
        "AmountMismatch" => $"Amounts differ by {Math.Abs(a.Amount - b.Amount):N2}",
        "Duplicate" => "Matched, but duplicate date/amount keys exist and no shared voucher ref/no",
        "PotentialMatch" => "Candidate found but debit/credit sides are not opposite",
        _ => status
    };

    private static IEnumerable<LedgerEntryDto> RankCandidates(LedgerEntryDto a, List<LedgerEntryDto> candidates, LedgerMatchOptions options)
    {
        // Prefer shared voucher ref / voucher no before generic date+amount ties.
        return candidates
            .OrderByDescending(b => HasSharedIdentity(a, b, options))
            .ThenByDescending(b => Score(a, b, options))
            .ThenBy(b => Math.Abs(a.Amount - b.Amount))
            .ThenBy(b => DateDiff(a.Date, b.Date));
    }

    private static int Score(LedgerEntryDto a, LedgerEntryDto b, LedgerMatchOptions options)
    {
        var score = 0;
        var opposite =
            (a.Debit > 0 && b.Credit > 0) ||
            (a.Credit > 0 && b.Debit > 0);
        if (opposite) score += 50;
        if (Math.Abs(a.Amount - b.Amount) <= options.AmountTolerance) score += 40;
        if (a.Date.HasValue && b.Date.HasValue && a.Date.Value.Date == b.Date.Value.Date) score += 20;

        // Strong identity keys
        if (options.PreferVoucherRef && SameVoucherRef(a, b))
            score += 100;
        if (options.MatchOnVoucherNo && SameVoucherNo(a, b))
            score += 80;

        if (!string.IsNullOrWhiteSpace(a.Particulars) && !string.IsNullOrWhiteSpace(b.Particulars))
        {
            var ta = NormalizeKey(a.Particulars);
            var tb = NormalizeKey(b.Particulars);
            if (ta.Contains(tb) || tb.Contains(ta)) score += 10;
        }
        return score;
    }

    private static LedgerEntryDto? FindPotential(
        LedgerEntryDto a,
        List<LedgerEntryDto> entriesB,
        HashSet<int> usedB,
        LedgerMatchOptions options)
    {
        var pool = entriesB.Where(b => !usedB.Contains(b.RowIndex)).ToList();
        if (pool.Count == 0) return null;

        // Prefer exact voucher identity even when amount/date buckets missed.
        if (options.PreferVoucherRef && !string.IsNullOrWhiteSpace(a.VoucherRef))
        {
            var refHit = pool
                .Where(b => SameVoucherRef(a, b))
                .OrderByDescending(b => Score(a, b, options))
                .FirstOrDefault();
            if (refHit != null) return refHit;
        }

        if (options.MatchOnVoucherNo && !string.IsNullOrWhiteSpace(a.VoucherNo))
        {
            var vnoHit = pool
                .Where(b => SameVoucherNo(a, b))
                .OrderByDescending(b => Score(a, b, options))
                .FirstOrDefault();
            if (vnoHit != null) return vnoHit;
        }

        // Same amount, date within wider window
        var amountHits = pool
            .Where(b => Math.Abs(a.Amount - b.Amount) <= Math.Max(options.AmountTolerance, 0.01m))
            .Where(b => !a.Date.HasValue || !b.Date.HasValue || DateDiff(a.Date, b.Date) <= Math.Max(options.DateToleranceDays, 3))
            .OrderByDescending(b => Score(a, b, options))
            .ToList();

        return amountHits.FirstOrDefault();
    }

    private static IEnumerable<string> BuildLookupKeys(LedgerEntryDto entry, LedgerMatchOptions options)
    {
        // Strongest identity keys first conceptually (dictionary merge order does not matter).
        if (options.PreferVoucherRef && !string.IsNullOrWhiteSpace(entry.VoucherRef))
            yield return $"ref|{NormalizeKey(entry.VoucherRef)}";

        if (options.MatchOnVoucherNo && !string.IsNullOrWhiteSpace(entry.VoucherNo))
            yield return $"vno|{NormalizeKey(entry.VoucherNo)}";

        var amountKey = entry.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        if (!options.MatchOnAmount)
            amountKey = "*";

        if (options.MatchOnDate && entry.Date.HasValue)
        {
            for (var d = -options.DateToleranceDays; d <= options.DateToleranceDays; d++)
            {
                var date = entry.Date.Value.Date.AddDays(d).ToString("yyyy-MM-dd");
                yield return $"{date}|{amountKey}";
            }
        }
        else
        {
            yield return $"*|{amountKey}";
        }
    }

    private static bool HasSharedIdentity(LedgerEntryDto a, LedgerEntryDto b, LedgerMatchOptions options)
    {
        if (options.PreferVoucherRef && SameVoucherRef(a, b)) return true;
        if (options.MatchOnVoucherNo && SameVoucherNo(a, b)) return true;
        return false;
    }

    private static bool SameVoucherRef(LedgerEntryDto a, LedgerEntryDto b) =>
        !string.IsNullOrWhiteSpace(a.VoucherRef) &&
        !string.IsNullOrWhiteSpace(b.VoucherRef) &&
        string.Equals(NormalizeKey(a.VoucherRef), NormalizeKey(b.VoucherRef), StringComparison.OrdinalIgnoreCase);

    private static bool SameVoucherNo(LedgerEntryDto a, LedgerEntryDto b) =>
        !string.IsNullOrWhiteSpace(a.VoucherNo) &&
        !string.IsNullOrWhiteSpace(b.VoucherNo) &&
        string.Equals(NormalizeKey(a.VoucherNo), NormalizeKey(b.VoucherNo), StringComparison.OrdinalIgnoreCase);

    private static HashSet<string> FindDuplicateKeys(List<LedgerEntryDto> entries)
    {
        return entries
            .GroupBy(DuplicateKey)
            .Where(g => g.Count() > 1 && g.Key != "|0.00")
            .Select(g => g.Key)
            .ToHashSet();
    }

    private static string DuplicateKey(LedgerEntryDto e) =>
        $"{e.Date?.ToString("yyyy-MM-dd") ?? ""}|{e.Amount.ToString("0.00", CultureInfo.InvariantCulture)}|{e.Side}";

    private static int DateDiff(DateTime? a, DateTime? b)
    {
        if (!a.HasValue || !b.HasValue) return 999;
        return Math.Abs((a.Value.Date - b.Value.Date).Days);
    }

    private static IXLWorksheet ResolveSheet(XLWorkbook workbook, string? sheetName, List<string> sheetNames)
    {
        if (!string.IsNullOrWhiteSpace(sheetName))
        {
            var match = workbook.Worksheets.FirstOrDefault(w =>
                string.Equals(w.Name, sheetName, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }
        return workbook.Worksheet(1);
    }

    private static int DetectHeaderRow(IXLWorksheet sheet)
    {
        var lastRow = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 1, 30);
        var bestRow = 1;
        var bestScore = -1;
        for (var r = 1; r <= lastRow; r++)
        {
            var values = sheet.Row(r).CellsUsed().Select(c => NormalizeText(c.GetString())).Where(v => v.Length > 0).ToList();
            if (values.Count < 2) continue;
            var score = 0;
            foreach (var v in values)
            {
                var n = NormalizeKey(v);
                if (DateAliases.Any(a => n.Contains(a))) score += 3;
                if (DebitAliases.Any(a => n == a || n.Contains(a))) score += 3;
                if (CreditAliases.Any(a => n == a || n.Contains(a))) score += 3;
                if (ParticularsAliases.Any(a => n.Contains(a))) score += 2;
                if (VoucherNoAliases.Any(a => n.Contains(a))) score += 2;
            }
            if (score > bestScore)
            {
                bestScore = score;
                bestRow = r;
            }
        }
        return bestRow;
    }

    private static List<string> ReadHeaders(IXLWorksheet sheet, int headerRow)
    {
        var row = sheet.Row(headerRow);
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var headers = new List<string>();
        for (var c = 1; c <= lastCol; c++)
        {
            var text = NormalizeText(row.Cell(c).GetString());
            if (string.IsNullOrWhiteSpace(text))
                text = $"Column{c}";
            // Ensure unique header names
            var unique = text;
            var i = 2;
            while (headers.Contains(unique, StringComparer.OrdinalIgnoreCase))
            {
                unique = $"{text}_{i++}";
            }
            headers.Add(unique);
        }
        return headers;
    }

    private static LedgerColumnMapping SuggestMapping(List<string> headers)
    {
        return new LedgerColumnMapping
        {
            Company = FindBestHeader(headers, CompanyAliases),
            Date = FindBestHeader(headers, DateAliases),
            Particulars = FindBestHeader(headers, ParticularsAliases),
            VoucherNo = FindBestHeader(headers, VoucherNoAliases),
            VoucherRef = FindBestHeader(headers, VoucherRefAliases),
            // Always prefer Debit/Credit (INR); never auto-pick FC amount columns.
            Debit = FindBestAmountHeader(headers, DebitAliases),
            Credit = FindBestAmountHeader(headers, CreditAliases),
        };
    }

    private static string? FindBestHeader(List<string> headers, string[] aliases)
    {
        string? best = null;
        var bestScore = 0;
        foreach (var h in headers)
        {
            var n = NormalizeKey(h);
            foreach (var alias in aliases)
            {
                var score = n == alias ? 100 : n.Contains(alias) ? 50 + alias.Length : 0;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = h;
                }
            }
        }
        return best;
    }

    /// <summary>
    /// Picks Debit/Credit columns preferring INR and excluding foreign-currency columns.
    /// </summary>
    private static string? FindBestAmountHeader(List<string> headers, string[] aliases)
    {
        string? best = null;
        var bestScore = int.MinValue;

        foreach (var h in headers)
        {
            var n = NormalizeKey(h);
            if (ForeignAmountMarkers.Any(m => n.Contains(m)))
                continue; // skip Debit (FC), Credit (FC), etc.

            var score = 0;
            foreach (var alias in aliases)
            {
                if (n == alias) score = Math.Max(score, 200 + alias.Length);
                else if (n.Contains(alias)) score = Math.Max(score, 100 + alias.Length);
            }
            if (score == 0) continue;

            // Strong boost when header explicitly says INR / Rs.
            if (InrAmountMarkers.Any(m => n.Contains(m)))
                score += 80;

            // Prefer plain "Debit"/"Credit" over unrelated matches when no INR marker exists,
            // but still below explicit INR columns.
            if (n is "debit" or "credit" or "dr" or "cr")
                score += 20;

            if (score > bestScore)
            {
                bestScore = score;
                best = h;
            }
        }

        // Fallback: if every debit/credit header was FC-only, allow previous generic behaviour.
        return best ?? FindBestHeader(headers, aliases);
    }

    private static List<Dictionary<string, string>> ReadSampleRows(IXLWorksheet sheet, int headerRow, List<string> headers, int take)
    {
        var samples = new List<Dictionary<string, string>>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        for (var r = headerRow + 1; r <= lastRow && samples.Count < take; r++)
        {
            var row = sheet.Row(r);
            if (IsEmptyRow(row, headers.Count)) continue;
            var dict = new Dictionary<string, string>();
            for (var c = 0; c < headers.Count; c++)
                dict[headers[c]] = NormalizeText(row.Cell(c + 1).GetFormattedString());
            samples.Add(dict);
        }
        return samples;
    }

    private static int CountDataRows(IXLWorksheet sheet, int headerRow, List<string> headers)
    {
        var count = 0;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            if (!IsEmptyRow(sheet.Row(r), headers.Count)) count++;
        }
        return count;
    }

    private static Dictionary<string, int> BuildColumnIndex(List<string> headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
            map[headers[i]] = i + 1;
        return map;
    }

    private static int ResolveColumn(Dictionary<string, int> index, string header)
    {
        if (index.TryGetValue(header, out var col)) return col;
        throw new InvalidOperationException($"Column '{header}' was not found in the worksheet headers.");
    }

    private static void RequireMapped(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{field} column mapping is required.");
    }

    private static bool IsEmptyRow(IXLRow row, int colCount)
    {
        for (var c = 1; c <= colCount; c++)
        {
            if (!string.IsNullOrWhiteSpace(row.Cell(c).GetFormattedString()))
                return false;
        }
        return true;
    }

    private static bool IsBalanceRow(string particulars)
    {
        var n = NormalizeKey(particulars);
        return n.Contains("opening balance") || n.Contains("closing balance") || n == "total" || n.StartsWith("grand total");
    }

    private static DateTime? ParseDate(IXLCell cell)
    {
        if (cell.TryGetValue(out DateTime dt)) return dt.Date;
        var raw = NormalizeText(cell.GetFormattedString());
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var formats = new[]
        {
            "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy",
            "dd-MM-yy", "d-M-yy", "dd/MM/yy", "d/M/yy",
            "yyyy-MM-dd", "MM/dd/yyyy", "dd.MM.yyyy"
        };
        if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact.Date;
        if (DateTime.TryParse(raw, CultureInfo.GetCultureInfo("en-IN"), DateTimeStyles.None, out var enIn))
            return enIn.Date;
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var inv))
            return inv.Date;
        return null;
    }

    private static decimal ParseAmount(IXLCell cell)
    {
        if (cell.TryGetValue(out double dbl)) return Math.Round((decimal)dbl, 2);
        if (cell.TryGetValue(out decimal dec)) return Math.Round(dec, 2);

        var raw = NormalizeText(cell.GetFormattedString());
        if (string.IsNullOrWhiteSpace(raw) || raw == "-" || raw == "—") return 0m;

        raw = raw.Replace("₹", "", StringComparison.Ordinal)
                 .Replace("$", "", StringComparison.Ordinal)
                 .Replace("Rs.", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("Rs", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("INR", "", StringComparison.OrdinalIgnoreCase)
                 .Trim();

        // Handle (1,234.00) as negative / just strip
        raw = raw.Trim('(', ')');
        raw = raw.Replace(",", "");

        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return Math.Round(Math.Abs(value), 2);
        return 0m;
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return Regex.Replace(value.Trim(), @"\s+", " ");
    }

    private static string NormalizeKey(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) || ch == ' ')
                sb.Append(ch);
        }
        return Regex.Replace(sb.ToString().Trim(), @"\s+", " ");
    }
}
