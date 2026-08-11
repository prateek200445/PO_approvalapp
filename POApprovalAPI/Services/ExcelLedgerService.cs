using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class ExcelLedgerService
{
    private static readonly string[] CompanyAliases = ["companyname", "company name", "company", "firm", "firm name", "entity"];
    private static readonly string[] DateAliases = ["voucher date", "voucherdate", "vch date", "txn date", "transaction date", "doc date", "date"];
    private static readonly string[] BillNoAliases = ["bill no", "bill no.", "bill number", "billno"];
    private static readonly string[] BillDateAliases = ["bill date", "billdate"];
    private static readonly string[] AmountAliases = ["amount"];
    private static readonly string[] ParticularsAliases = ["ledger name", "ledgername", "particulars", "narration", "description", "remarks", "account", "ledger"];
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
            $"{nameA} Voucher Date", $"{nameA} Bill No", $"{nameA} Bill Date", $"{nameA} Voucher No", $"{nameA} Ledger", $"{nameA} Amount", $"{nameA} Side",
            $"{nameB} Voucher Date", $"{nameB} Bill No", $"{nameB} Bill Date", $"{nameB} Voucher No", $"{nameB} Ledger", $"{nameB} Amount", $"{nameB} Side"
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
            WriteEntry(detail, row, 11, r.EntryB);
            ApplyStatusCellTone(detail.Cell(row, 1), r.Status);
        }

        var lastDataRow = Math.Max(1, exportRows.Count + 1);
        var tableRange = detail.Range(1, 1, lastDataRow, headers.Length);
        tableRange.SetAutoFilter();
        detail.SheetView.FreezeRows(1);
        detail.Columns().AdjustToContents();
        detail.Column(8).Width = Math.Min(detail.Column(8).Width, 40);
        detail.Column(15).Width = Math.Min(detail.Column(15).Width, 40);

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
        sheet.Cell(row, startCol + 1).Value = entry.BillNo;
        sheet.Cell(row, startCol + 2).Value = entry.BillDate?.ToString("dd-MM-yyyy") ?? "";
        sheet.Cell(row, startCol + 3).Value = entry.VoucherNo;
        sheet.Cell(row, startCol + 4).Value = entry.Particulars;
        sheet.Cell(row, startCol + 5).Value = entry.SignedAmount;
        sheet.Cell(row, startCol + 6).Value = entry.Side;
    }

    public List<LedgerEntryDto> ParseLedger(Stream stream, string company, LedgerColumnMapping mapping)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = ResolveSheet(workbook, mapping.SheetName, workbook.Worksheets.Select(w => w.Name).ToList());
        var headerRow = mapping.HeaderRow <= 0 ? DetectHeaderRow(sheet) : mapping.HeaderRow;
        var headers = ReadHeaders(sheet, headerRow);
        var colIndex = BuildColumnIndex(headers);

        RequireMapped(mapping.Date, "Voucher Date");
        var hasSignedAmount = !string.IsNullOrWhiteSpace(mapping.Amount);
        if (!hasSignedAmount)
        {
            RequireMapped(mapping.Debit, "Debit");
            RequireMapped(mapping.Credit, "Credit");
        }

        var dateCol = ResolveColumn(colIndex, mapping.Date!);
        var amountCol = hasSignedAmount ? ResolveColumn(colIndex, mapping.Amount!) : -1;
        var debitCol = !hasSignedAmount ? ResolveColumn(colIndex, mapping.Debit!) : -1;
        var creditCol = !hasSignedAmount ? ResolveColumn(colIndex, mapping.Credit!) : -1;
        var particularsCol = string.IsNullOrWhiteSpace(mapping.Particulars) ? -1 : ResolveColumn(colIndex, mapping.Particulars);
        var voucherCol = string.IsNullOrWhiteSpace(mapping.VoucherNo) ? -1 : ResolveColumn(colIndex, mapping.VoucherNo);
        var refCol = string.IsNullOrWhiteSpace(mapping.VoucherRef) ? -1 : ResolveColumn(colIndex, mapping.VoucherRef);
        var billNoCol = string.IsNullOrWhiteSpace(mapping.BillNo) ? -1 : ResolveColumn(colIndex, mapping.BillNo);
        var billDateCol = string.IsNullOrWhiteSpace(mapping.BillDate) ? -1 : ResolveColumn(colIndex, mapping.BillDate);
        var companyCol = string.IsNullOrWhiteSpace(mapping.Company) ? -1 : ResolveColumn(colIndex, mapping.Company);

        var entries = new List<LedgerEntryDto>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;

        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            var row = sheet.Row(r);
            if (IsEmptyRow(row, headers.Count)) continue;

            var voucherDate = ParseDate(row.Cell(dateCol));
            decimal signedAmount;
            decimal debit;
            decimal credit;

            if (hasSignedAmount)
            {
                signedAmount = ParseSignedAmount(row.Cell(amountCol));
                debit = signedAmount < 0 ? Math.Abs(signedAmount) : 0m;
                credit = signedAmount > 0 ? signedAmount : 0m;
            }
            else
            {
                debit = ParseAbsoluteAmount(row.Cell(debitCol));
                credit = ParseAbsoluteAmount(row.Cell(creditCol));
                // Legacy: debit stored as negative, credit as positive
                signedAmount = credit > 0 ? credit : debit > 0 ? -debit : 0m;
            }

            if (signedAmount == 0 && voucherDate == null) continue;

            var particulars = particularsCol > 0 ? NormalizeText(row.Cell(particularsCol).GetString()) : "";
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
                Date = voucherDate,
                BillDate = billDateCol > 0 ? ParseDate(row.Cell(billDateCol)) : null,
                Particulars = particulars,
                VoucherNo = voucherCol > 0 ? NormalizeText(row.Cell(voucherCol).GetFormattedString()) : "",
                VoucherRef = refCol > 0 ? NormalizeText(row.Cell(refCol).GetFormattedString()) : "",
                BillNo = billNoCol > 0 ? NormalizeText(row.Cell(billNoCol).GetFormattedString()) : "",
                SignedAmount = signedAmount,
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
        var usedA = new HashSet<int>();
        var usedB = new HashSet<int>();

        var groupsA = BuildBillGroups(entriesA);
        var groupsB = BuildBillGroups(entriesB);
        var usedBillKeysB = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Bill groups: many↔many / many↔one by exact BillNo + BillDate, compare summed amounts
        foreach (var ga in groupsA.Values)
        {
            if (groupsB.TryGetValue(ga.Key, out var gb))
            {
                usedBillKeysB.Add(ga.Key);
                MarkUsed(usedA, ga.Entries);
                MarkUsed(usedB, gb.Entries);
                results.Add(BuildBillGroupPair(ga, gb, options));
                continue;
            }

            var best = FindBestBillGroupCandidate(ga, groupsB, usedBillKeysB);
            if (best != null)
            {
                usedBillKeysB.Add(best.Key);
                MarkUsed(usedA, ga.Entries);
                MarkUsed(usedB, best.Entries);
                results.Add(BuildBillGroupPair(ga, best, options));
                continue;
            }

            MarkUsed(usedA, ga.Entries);
            // One-sided bill that nets to ~0 is settled internally — omit from results
            if (IsZeroNetBill(ga, options))
                continue;

            results.Add(new ComparisonPairDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = "MissingInB",
                MatchKind = "bill-group",
                Message = "No bill group with a matching Bill No found in File B",
                EntryA = ga.ToSummary(),
                EntriesA = ga.Entries.ToList(),
            });
        }

        foreach (var gb in groupsB.Where(kv => !usedBillKeysB.Contains(kv.Key)))
        {
            MarkUsed(usedB, gb.Value.Entries);
            if (IsZeroNetBill(gb.Value, options))
                continue;

            results.Add(new ComparisonPairDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = "MissingInA",
                MatchKind = "bill-group",
                Message = "No bill group with a matching Bill No found in File A",
                EntryB = gb.Value.ToSummary(),
                EntriesB = gb.Value.Entries.ToList(),
            });
        }

        // Row-level voucher-date fallback for rows without Bill No
        var remainingA = entriesA.Where(e => !usedA.Contains(e.RowIndex)).ToList();
        var remainingB = entriesB.Where(e => !usedB.Contains(e.RowIndex)).ToList();

        foreach (var a in remainingA)
        {
            var voucherMatches = remainingB
                .Where(b => !usedB.Contains(b.RowIndex))
                .Where(b => CanUseVoucherDateFallback(a, b))
                .Where(b => DatesMatch(a.Date, b.Date, options.DateToleranceDays))
                .Where(b => OppositeSignedAmounts(a, b, options.AmountTolerance))
                .ToList();

            if (voucherMatches.Count > 0)
            {
                var best = RankByOppositeAmount(a, voucherMatches).First();
                usedB.Add(best.RowIndex);
                results.Add(BuildRowPair(a, best, options, matched: true));
                continue;
            }

            var sameVoucherDate = remainingB
                .Where(b => !usedB.Contains(b.RowIndex))
                .Where(b => CanUseVoucherDateFallback(a, b))
                .Where(b => DatesMatch(a.Date, b.Date, options.DateToleranceDays))
                .Where(b => AbsAmountsClose(a, b, Math.Max(options.AmountTolerance, 0.01m)))
                .ToList();

            if (sameVoucherDate.Count > 0)
            {
                var best = RankByOppositeAmount(a, sameVoucherDate).First();
                usedB.Add(best.RowIndex);
                var reason = SameSign(a, best)
                    ? "same voucher date but amounts are not opposite signs"
                    : $"same voucher date but amounts differ by {AmountGap(a, best):N2}";
                results.Add(new ComparisonPairDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Status = "PotentialMatch",
                    MatchKind = "row",
                    Message = $"Potential mismatch: {reason}",
                    Difference = AmountGap(a, best),
                    EntryA = a,
                    EntryB = best,
                    EntriesA = [a],
                    EntriesB = [best],
                });
                continue;
            }

            results.Add(new ComparisonPairDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = "MissingInB",
                MatchKind = "row",
                Message = "No corresponding transaction found in File B",
                EntryA = a,
                EntriesA = [a],
            });
        }

        foreach (var b in remainingB.Where(x => !usedB.Contains(x.RowIndex)))
        {
            results.Add(new ComparisonPairDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = "MissingInA",
                MatchKind = "row",
                Message = "No corresponding transaction found in File A",
                EntryB = b,
                EntriesB = [b],
            });
        }

        results = PromoteMissingPairsToPotential(results, options);

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
                Duplicates = 0,
                PotentialMatches = results.Count(r => r.Status == "PotentialMatch"),
            },
            Results = results
        };
    }

    private static List<ComparisonPairDto> PromoteMissingPairsToPotential(List<ComparisonPairDto> results, LedgerMatchOptions options)
    {
        var list = results.ToList();
        var missA = list
            .Where(r => r.Status == "MissingInA" && r.EntryB != null && !string.IsNullOrWhiteSpace(r.EntryB.BillNo))
            .ToList();
        var missB = list
            .Where(r => r.Status == "MissingInB" && r.EntryA != null && !string.IsNullOrWhiteSpace(r.EntryA.BillNo))
            .ToList();

        if (missA.Count == 0 || missB.Count == 0)
            return list;

        var consumedA = new HashSet<string>();
        var consumedB = new HashSet<string>();
        var extra = new List<ComparisonPairDto>();
        var tolerance = Math.Max(options.AmountTolerance, 1m);

        foreach (var a in missB)
        {
            if (a.EntryA == null || consumedA.Contains(a.Id))
                continue;

            var candidate = missA
                .Where(b => b.EntryB != null && !consumedB.Contains(b.Id))
                .Where(b => BillNumbersLookRelated(a.EntryA!.BillNo, b.EntryB!.BillNo))
                .OrderBy(b => AmountGap(a.EntryA!, b.EntryB!))
                .ThenBy(b => DateDiff(a.EntryA!.BillDate ?? a.EntryA.Date, b.EntryB!.BillDate ?? b.EntryB.Date))
                .FirstOrDefault();

            if (candidate == null || candidate.EntryB == null)
                continue;

            consumedA.Add(a.Id);
            consumedB.Add(candidate.Id);

            var entryA = a.EntryA!;
            var entryB = candidate.EntryB!;
            var gap = AmountGap(entryA, entryB);
            var matched = OppositeSignedAmounts(entryA, entryB, tolerance);
            var billLabel = BillMatchLabel(entryA.BillNo, entryB.BillNo);

            extra.Add(new ComparisonPairDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = matched ? "Matched" : "PotentialMatch",
                MatchKind = "bill-group",
                Message = matched
                    ? $"{billLabel}: amounts reconcile"
                    : $"{billLabel}: potential amount mismatch (diff {gap:N2})",
                Difference = matched ? 0 : gap,
                EntryA = entryA,
                EntryB = entryB,
                EntriesA = a.EntriesA?.Count > 0 ? a.EntriesA : [entryA],
                EntriesB = candidate.EntriesB?.Count > 0 ? candidate.EntriesB : [entryB],
            });
        }

        list.RemoveAll(r => consumedA.Contains(r.Id) || consumedB.Contains(r.Id));
        list.AddRange(extra);
        return list;
    }

    private static Dictionary<string, BillGroup> BuildBillGroups(List<LedgerEntryDto> entries)
    {
        var map = new Dictionary<string, BillGroup>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries.Where(HasBillNo))
        {
            if (!e.BillDate.HasValue) continue;
            var key = BillGroupKey(e.BillNo, e.BillDate);
            if (!map.TryGetValue(key, out var group))
            {
                group = new BillGroup
                {
                    Key = key,
                    BillNo = e.BillNo.Trim(),
                    BillNoKey = NormalizeKey(e.BillNo),
                    BillDate = e.BillDate,
                };
                map[key] = group;
            }
            group.Entries.Add(e);
        }
        return map;
    }

    private static string BillGroupKey(string billNo, DateTime? billDate) =>
        $"{NormalizeKey(billNo)}|{(billDate?.ToString("yyyy-MM-dd") ?? "nodate")}";

    private static void MarkUsed(HashSet<int> used, IEnumerable<LedgerEntryDto> entries)
    {
        foreach (var e in entries)
            used.Add(e.RowIndex);
    }

    private static BillGroup? FindBestBillGroupCandidate(
        BillGroup ga,
        Dictionary<string, BillGroup> groupsB,
        HashSet<string> usedBillKeysB)
    {
        return groupsB
            .Where(kv => !usedBillKeysB.Contains(kv.Key))
            .Where(kv =>
                string.Equals(kv.Value.BillNoKey, ga.BillNoKey, StringComparison.OrdinalIgnoreCase) ||
                BillNumbersLookRelated(ga.BillNo, kv.Value.BillNo))
            .Select(kv => kv.Value)
            .OrderBy(g => Math.Abs(Math.Abs(ga.SignedTotal) - Math.Abs(g.SignedTotal)))
            .ThenBy(g => DateDiff(ga.BillDate, g.BillDate))
            .FirstOrDefault();
    }

    private static ComparisonPairDto BuildBillGroupPair(BillGroup a, BillGroup b, LedgerMatchOptions options)
    {
        var tolerance = BillAmountTolerance(options);
        var summaryA = a.ToSummary();
        var summaryB = b.ToSummary();
        var gap = Math.Abs(Math.Abs(a.SignedTotal) - Math.Abs(b.SignedTotal));
        var lines = $"{a.Entries.Count} ↔ {b.Entries.Count} lines";
        var billLabel = BillMatchLabel(a.BillNo, b.BillNo);

        // Both sides net to ~0 under the same Bill No → matched (settled bills)
        if (IsZeroNetBill(a, options) && IsZeroNetBill(b, options))
        {
            return new ComparisonPairDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = "Matched",
                MatchKind = "bill-group",
                Message = $"{billLabel}: bill totals both net to 0 and reconcile ({lines})",
                EntryA = summaryA,
                EntryB = summaryB,
                EntriesA = a.Entries.ToList(),
                EntriesB = b.Entries.ToList(),
            };
        }

        var opposite = OppositeSignedAmounts(summaryA, summaryB, tolerance);
        if (opposite)
        {
            return new ComparisonPairDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = "Matched",
                MatchKind = "bill-group",
                Message =
                    $"{billLabel}: {summaryA.Side} {summaryA.Amount:N2} reconciles with {summaryB.Side} {summaryB.Amount:N2} ({lines})",
                EntryA = summaryA,
                EntryB = summaryB,
                EntriesA = a.Entries.ToList(),
                EntriesB = b.Entries.ToList(),
            };
        }

        return new ComparisonPairDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Status = "PotentialMatch",
            MatchKind = "bill-group",
            Message = $"{billLabel}: potential amount mismatch ({lines}, diff {gap:N2})",
            Difference = gap,
            EntryA = summaryA,
            EntryB = summaryB,
            EntriesA = a.Entries.ToList(),
            EntriesB = b.Entries.ToList(),
        };
    }

    private static string BillMatchLabel(string? billNoA, string? billNoB)
    {
        if (string.IsNullOrWhiteSpace(billNoA) || string.IsNullOrWhiteSpace(billNoB))
            return "Bill No";
        if (string.Equals(NormalizeKey(billNoA), NormalizeKey(billNoB), StringComparison.OrdinalIgnoreCase))
            return $"Bill No {billNoA.Trim()}";
        return $"Related Bill No ({billNoA.Trim()} ↔ {billNoB.Trim()})";
    }

    private static decimal BillAmountTolerance(LedgerMatchOptions options) =>
        Math.Max(options.AmountTolerance, 1m);

    private static bool IsZeroNetBill(BillGroup group, LedgerMatchOptions options) =>
        Math.Abs(group.SignedTotal) <= BillAmountTolerance(options);

    private static ComparisonPairDto BuildRowPair(LedgerEntryDto a, LedgerEntryDto b, LedgerMatchOptions options, bool matched)
    {
        if (matched && OppositeSignedAmounts(a, b, options.AmountTolerance))
        {
            return new ComparisonPairDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = "Matched",
                MatchKind = "row",
                Message = $"{a.Side} {a.Amount:N2} reconciles with {b.Side} {b.Amount:N2} via Voucher Date",
                EntryA = a,
                EntryB = b,
                EntriesA = [a],
                EntriesB = [b],
            };
        }

        return new ComparisonPairDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Status = "PotentialMatch",
            MatchKind = "row",
            Message = $"Potential mismatch: same Voucher Date but amounts do not reconcile (diff {AmountGap(a, b):N2})",
            Difference = AmountGap(a, b),
            EntryA = a,
            EntryB = b,
            EntriesA = [a],
            EntriesB = [b],
        };
    }

    private sealed class BillGroup
    {
        public string Key { get; set; } = "";
        public string BillNo { get; set; } = "";
        public string BillNoKey { get; set; } = "";
        public DateTime? BillDate { get; set; }
        public List<LedgerEntryDto> Entries { get; } = new();
        public decimal SignedTotal => Entries.Sum(e => e.SignedAmount);

        public LedgerEntryDto ToSummary()
        {
            var total = SignedTotal;
            var first = Entries[0];
            return new LedgerEntryDto
            {
                RowIndex = first.RowIndex,
                Company = first.Company,
                BillNo = BillNo,
                BillDate = BillDate,
                Date = Entries.Select(e => e.Date).Where(d => d.HasValue).OrderBy(d => d).FirstOrDefault(),
                Particulars = Entries.Count == 1
                    ? first.Particulars
                    : $"Bill total ({Entries.Count} lines)",
                VoucherNo = Entries.Count == 1 ? first.VoucherNo : "",
                VoucherRef = "",
                SignedAmount = total,
                Debit = total < 0 ? Math.Abs(total) : 0m,
                Credit = total > 0 ? total : 0m,
            };
        }
    }

    private static IEnumerable<LedgerEntryDto> RankByOppositeAmount(LedgerEntryDto a, List<LedgerEntryDto> candidates) =>
        candidates
            .OrderByDescending(b => OppositeSignedAmounts(a, b, 0.01m))
            .ThenBy(b => AmountGap(a, b))
            .ThenBy(b => DateDiff(a.BillDate ?? a.Date, b.BillDate ?? b.Date));

    private static bool HasBillNo(LedgerEntryDto e) => !string.IsNullOrWhiteSpace(e.BillNo);

    private static bool BillNumbersLookRelated(string? billNoA, string? billNoB)
    {
        if (string.IsNullOrWhiteSpace(billNoA) || string.IsNullOrWhiteSpace(billNoB))
            return false;

        var a = NormalizeKey(billNoA);
        var b = NormalizeKey(billNoB);
        if (a == b) return true;

        var tokensA = TokenizeBillNo(billNoA);
        var tokensB = TokenizeBillNo(billNoB);
        if (tokensA.NumericTokens.Count == 0 || tokensB.NumericTokens.Count == 0)
            return false;

        if (!HasStrongNumericCoreMatch(tokensA.NumericTokens, tokensB.NumericTokens))
            return false;

        // If both sides provide alpha prefixes, require overlap to avoid loose numeric collisions.
        if (tokensA.AlphaTokens.Count > 0 && tokensB.AlphaTokens.Count > 0 &&
            !tokensA.AlphaTokens.Overlaps(tokensB.AlphaTokens))
            return false;

        return true;
    }

    private static (HashSet<string> NumericTokens, HashSet<string> AlphaTokens) TokenizeBillNo(string value)
    {
        var numeric = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var alpha = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(value.ToLowerInvariant(), @"[a-z]+|\d+"))
        {
            var token = match.Value.Trim();
            if (token.Length == 0) continue;
            if (char.IsDigit(token[0])) numeric.Add(token);
            else alpha.Add(token);
        }
        return (numeric, alpha);
    }

    private static bool HasStrongNumericCoreMatch(HashSet<string> numsA, HashSet<string> numsB)
    {
        if (numsA.Count == 0 || numsB.Count == 0)
            return false;

        var primaryA = FindPrimaryBillToken(numsA);
        var primaryB = FindPrimaryBillToken(numsB);
        if (!string.IsNullOrWhiteSpace(primaryA) && !string.IsNullOrWhiteSpace(primaryB) && primaryA != primaryB)
            return false;

        if (!numsA.Overlaps(numsB))
            return false;

        var fyA = ExtractFinancialYearTokens(numsA);
        var fyB = ExtractFinancialYearTokens(numsB);
        if (fyA.Count > 0 && fyB.Count > 0 && !fyA.Overlaps(fyB))
            return false;

        return true;
    }

    private static string FindPrimaryBillToken(HashSet<string> numericTokens)
    {
        return numericTokens
            .Select(t => int.TryParse(t, out var n) ? (Token: t, Value: n, Len: t.Length) : (Token: "", Value: -1, Len: 0))
            .Where(x => x.Value >= 100 || x.Len >= 3)
            .OrderByDescending(x => x.Value)
            .Select(x => x.Token)
            .FirstOrDefault() ?? "";
    }

    private static HashSet<string> ExtractFinancialYearTokens(HashSet<string> numericTokens)
    {
        var years = numericTokens
            .Where(t => t.Length <= 2)
            .Select(t => int.TryParse(t, out var y) ? y : -1)
            .Where(y => y >= 0 && y <= 99)
            .Distinct()
            .ToList();

        var fySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var y in years)
        {
            if (years.Contains(y + 1) || years.Contains(y - 1))
                fySet.Add(y.ToString("00"));
        }
        return fySet;
    }

    /// <summary>
    /// Voucher-date fallback is allowed only when Bill No is missing on at least one side.
    /// Two rows that both have (different) Bill Nos must never match via voucher date.
    /// </summary>
    private static bool CanUseVoucherDateFallback(LedgerEntryDto a, LedgerEntryDto b) =>
        !HasBillNo(a) || !HasBillNo(b);

    private static bool SameBillNo(LedgerEntryDto a, LedgerEntryDto b) =>
        HasBillNo(a) && HasBillNo(b) &&
        string.Equals(NormalizeKey(a.BillNo), NormalizeKey(b.BillNo), StringComparison.OrdinalIgnoreCase);

    private static bool ExactDateMatch(DateTime? a, DateTime? b) =>
        a.HasValue && b.HasValue && a.Value.Date == b.Value.Date;

    private static bool DatesMatch(DateTime? a, DateTime? b, int toleranceDays)
    {
        if (!a.HasValue || !b.HasValue) return false;
        return DateDiff(a, b) <= Math.Max(0, toleranceDays);
    }

    private static string FormatDate(DateTime? d) =>
        d?.ToString("dd-MM-yyyy") ?? "—";

    private static bool OppositeSignedAmounts(LedgerEntryDto a, LedgerEntryDto b, decimal tolerance) =>
        a.SignedAmount != 0 &&
        b.SignedAmount != 0 &&
        Math.Sign(a.SignedAmount) != Math.Sign(b.SignedAmount) &&
        Math.Abs(Math.Abs(a.SignedAmount) - Math.Abs(b.SignedAmount)) <= tolerance;

    private static bool AbsAmountsClose(LedgerEntryDto a, LedgerEntryDto b, decimal tolerance) =>
        Math.Abs(a.Amount - b.Amount) <= tolerance;

    private static bool SameSign(LedgerEntryDto a, LedgerEntryDto b) =>
        a.SignedAmount != 0 && b.SignedAmount != 0 && Math.Sign(a.SignedAmount) == Math.Sign(b.SignedAmount);

    private static decimal AmountGap(LedgerEntryDto a, LedgerEntryDto b) =>
        Math.Abs(Math.Abs(a.SignedAmount) - Math.Abs(b.SignedAmount));

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
                if (DateAliases.Any(a => n == a || n.Contains(a))) score += 3;
                if (BillNoAliases.Any(a => n == a || n.Contains(a))) score += 4;
                if (BillDateAliases.Any(a => n == a || n.Contains(a))) score += 3;
                if (AmountAliases.Any(a => n == a)) score += 4;
                if (CompanyAliases.Any(a => n == a || n.Contains(a))) score += 3;
                if (DebitAliases.Any(a => n == a || n.Contains(a))) score += 2;
                if (CreditAliases.Any(a => n == a || n.Contains(a))) score += 2;
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
        var amount = FindBestHeader(headers, AmountAliases);
        var billDate = FindBestHeader(headers, BillDateAliases);
        var voucherDate = FindBestHeader(
            headers.Where(h => !string.Equals(h, billDate, StringComparison.OrdinalIgnoreCase)).ToList(),
            DateAliases);
        return new LedgerColumnMapping
        {
            Company = FindBestHeader(headers, CompanyAliases),
            Date = voucherDate,
            Particulars = FindBestHeader(headers, ParticularsAliases),
            VoucherNo = FindBestHeader(headers, VoucherNoAliases),
            VoucherRef = FindBestHeader(headers, VoucherRefAliases),
            BillNo = FindBestHeader(headers, BillNoAliases),
            BillDate = billDate,
            Amount = amount,
            Debit = amount == null ? FindBestAmountHeader(headers, DebitAliases) : null,
            Credit = amount == null ? FindBestAmountHeader(headers, CreditAliases) : null,
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

    private static decimal ParseSignedAmount(IXLCell cell)
    {
        if (cell.TryGetValue(out double dbl)) return Math.Round((decimal)dbl, 2);
        if (cell.TryGetValue(out decimal dec)) return Math.Round(dec, 2);

        var raw = NormalizeText(cell.GetFormattedString());
        if (string.IsNullOrWhiteSpace(raw) || raw == "-" || raw == "—") return 0m;

        var negative = raw.StartsWith("(") && raw.EndsWith(")");
        raw = raw.Replace("₹", "", StringComparison.Ordinal)
                 .Replace("$", "", StringComparison.Ordinal)
                 .Replace("Rs.", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("Rs", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("INR", "", StringComparison.OrdinalIgnoreCase)
                 .Trim();

        raw = raw.Trim('(', ')');
        raw = raw.Replace(",", "");
        if (raw.StartsWith("-"))
        {
            negative = true;
            raw = raw[1..].Trim();
        }

        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            var rounded = Math.Round(Math.Abs(value), 2);
            return negative ? -rounded : rounded;
        }
        return 0m;
    }

    private static decimal ParseAbsoluteAmount(IXLCell cell) => Math.Abs(ParseSignedAmount(cell));

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
