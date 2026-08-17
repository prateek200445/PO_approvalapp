using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services.FinancialStatements;

public class TrialBalanceExcelService
{
    private static readonly string[] ParticularsAliases =
        ["particulars", "ledger name", "ledger", "account", "account name", "description"];

    private static readonly string[] OpeningAliases = ["opening", "opening balance", "op balance", "op. balance"];
    private static readonly string[] DebitAliases = ["debit", "dr", "debit amount"];
    private static readonly string[] CreditAliases = ["credit", "cr", "credit amount"];
    private static readonly string[] ClosingAliases = ["closing", "closing balance", "cl balance", "balance"];
    private static readonly string[] AdjustedClosingAliases = ["adjusted closing", "closing (adjusted)", "final closing"];
    private static readonly string[] GroupAliases = ["group", "schedule", "fs group", "note group", "classification"];

    public TrialBalancePreviewResponse Preview(Stream stream, string fileName, string? sheetName = null, int? headerRow = null)
    {
        using var workbook = new XLWorkbook(stream);
        var sheetNames = workbook.Worksheets.Select(w => w.Name).ToList();
        if (sheetNames.Count == 0)
            throw new InvalidOperationException("Workbook has no worksheets.");

        var sheet = ResolveSheet(workbook, sheetName, sheetNames);
        var detectedHeaderRow = headerRow ?? DetectHeaderRow(sheet);
        var headers = ReadHeaders(sheet, detectedHeaderRow);
        if (headers.Count == 0)
            throw new InvalidOperationException("Could not detect column headers on the trial balance sheet.");

        var mapping = SuggestMapping(headers, sheet.Name, detectedHeaderRow);
        var sampleRows = ReadSampleRows(sheet, detectedHeaderRow, headers, 8);
        var dataRowCount = CountDataRows(sheet, detectedHeaderRow, headers);

        return new TrialBalancePreviewResponse
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

    public List<TrialBalanceRowDto> Parse(Stream stream, TrialBalanceColumnMapping mapping)
    {
        using var workbook = new XLWorkbook(stream);
        var sheetNames = workbook.Worksheets.Select(w => w.Name).ToList();
        var sheet = ResolveSheet(workbook, mapping.SheetName, sheetNames);
        var headerRow = mapping.HeaderRow <= 0 ? DetectHeaderRow(sheet) : mapping.HeaderRow;
        var headers = ReadHeaders(sheet, headerRow);
        var colIndex = BuildColumnIndex(headers);

        var particularsCol = ResolveColumn(colIndex, mapping.Particulars, ParticularsAliases);
        var openingCol = ResolveOptionalColumn(colIndex, mapping.Opening, OpeningAliases);
        var debitCol = ResolveOptionalColumn(colIndex, mapping.Debit, DebitAliases);
        var creditCol = ResolveOptionalColumn(colIndex, mapping.Credit, CreditAliases);
        var closingCol = ResolveOptionalColumn(colIndex, mapping.Closing, ClosingAliases);
        var adjustedClosingCol = ResolveOptionalColumn(colIndex, mapping.AdjustedClosing, AdjustedClosingAliases);
        var groupCol = ResolveOptionalColumn(colIndex, mapping.Group, GroupAliases);

        var rows = new List<TrialBalanceRowDto>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;

        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            var ledger = ReadCellString(sheet, r, particularsCol);
            if (string.IsNullOrWhiteSpace(ledger))
                continue;

            var normalized = NormalizeLabel(ledger);
            if (normalized is "totals" or "total" or "grand total")
                continue;

            var opening = openingCol > 0 ? ReadDecimal(sheet, r, openingCol) : 0m;
            var debit = debitCol > 0 ? ReadDecimal(sheet, r, debitCol) : 0m;
            var credit = creditCol > 0 ? ReadDecimal(sheet, r, creditCol) : 0m;
            var closing = adjustedClosingCol > 0
                ? ReadDecimal(sheet, r, adjustedClosingCol)
                : closingCol > 0
                    ? ReadDecimal(sheet, r, closingCol)
                    : opening + debit - credit;

            var group = groupCol > 0 ? ReadCellString(sheet, r, groupCol) : "";
            if (group == "0")
                group = "";

            rows.Add(new TrialBalanceRowDto
            {
                Ledger = ledger.Trim(),
                Opening = opening,
                Debit = debit,
                Credit = credit,
                Closing = closing,
                Group = group.Trim()
            });
        }

        return rows;
    }

    private static TrialBalanceColumnMapping SuggestMapping(IReadOnlyList<string> headers, string sheetName, int headerRow)
    {
        var closingHeaders = headers
            .Select((h, i) => (Header: h, Index: i))
            .Where(x => NormalizeLabel(x.Header).StartsWith("closing"))
            .ToList();

        var preferredClosing = closingHeaders.Count > 1
            ? closingHeaders[^1].Header
            : closingHeaders.FirstOrDefault().Header ?? "Closing";

        var otbHeader = headers.FirstOrDefault(h => NormalizeLabel(h) == "otb");
        var adjustedClosing = closingHeaders.Count > 1
            ? closingHeaders[^1].Header
            : !string.IsNullOrWhiteSpace(otbHeader)
                ? headers.SkipWhile(h => !h.Equals(otbHeader, StringComparison.OrdinalIgnoreCase)).Skip(1)
                    .FirstOrDefault(h => NormalizeLabel(h).StartsWith("closing"))
                : null;

        return new TrialBalanceColumnMapping
        {
            SheetName = sheetName,
            HeaderRow = headerRow,
            Particulars = FindHeader(headers, ParticularsAliases) ?? headers.FirstOrDefault(h => !string.IsNullOrWhiteSpace(h)) ?? "Particulars",
            Opening = FindHeader(headers, OpeningAliases) ?? "Opening",
            Debit = FindHeader(headers, DebitAliases) ?? "Debit",
            Credit = FindHeader(headers, CreditAliases) ?? "Credit",
            Closing = preferredClosing,
            AdjustedClosing = adjustedClosing,
            Group = FindHeader(headers, GroupAliases) ?? "Group"
        };
    }

    private static int DetectHeaderRow(IXLWorksheet sheet)
    {
        var lastRow = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 20, 30);
        var bestRow = 1;
        var bestScore = -1;

        for (var r = 1; r <= lastRow; r++)
        {
            var headers = ReadHeaders(sheet, r);
            if (headers.Count == 0)
                continue;

            var score = 0;
            if (FindHeader(headers, ParticularsAliases) != null) score += 4;
            if (FindHeader(headers, ClosingAliases) != null) score += 3;
            if (FindHeader(headers, DebitAliases) != null) score += 2;
            if (FindHeader(headers, CreditAliases) != null) score += 2;
            if (FindHeader(headers, OpeningAliases) != null) score += 1;
            if (FindHeader(headers, GroupAliases) != null) score += 1;

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
        var headers = new List<string>();
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 1;
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var c = 1; c <= lastCol; c++)
        {
            var text = sheet.Cell(headerRow, c).GetFormattedString().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                headers.Add("");
                continue;
            }

            if (seen.TryGetValue(text, out var count))
            {
                seen[text] = count + 1;
                headers.Add($"{text} ({count + 1})");
            }
            else
            {
                seen[text] = 1;
                headers.Add(text);
            }
        }

        while (headers.Count > 0 && string.IsNullOrWhiteSpace(headers[^1]))
            headers.RemoveAt(headers.Count - 1);

        return headers;
    }

    private static List<Dictionary<string, string?>> ReadSampleRows(
        IXLWorksheet sheet,
        int headerRow,
        IReadOnlyList<string> headers,
        int maxRows)
    {
        var rows = new List<Dictionary<string, string?>>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        var count = 0;

        for (var r = headerRow + 1; r <= lastRow && count < maxRows; r++)
        {
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var hasValue = false;
            for (var c = 0; c < headers.Count; c++)
            {
                var val = sheet.Cell(r, c + 1).GetFormattedString().Trim();
                if (!string.IsNullOrWhiteSpace(val))
                    hasValue = true;
                row[headers[c]] = val;
            }

            if (hasValue)
            {
                rows.Add(row);
                count++;
            }
        }

        return rows;
    }

    private static int CountDataRows(IXLWorksheet sheet, int headerRow, IReadOnlyList<string> headers)
    {
        var colIndex = BuildColumnIndex(headers);
        var particularsCol = ResolveColumn(colIndex, FindHeader(headers, ParticularsAliases) ?? headers.FirstOrDefault() ?? "", ParticularsAliases);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        var count = 0;

        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            var ledger = ReadCellString(sheet, r, particularsCol);
            if (!string.IsNullOrWhiteSpace(ledger) && NormalizeLabel(ledger) is not ("totals" or "total"))
                count++;
        }

        return count;
    }

    private static Dictionary<string, int> BuildColumnIndex(IReadOnlyList<string> headers)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i]?.Trim();
            if (!string.IsNullOrWhiteSpace(header) && !index.ContainsKey(header))
                index[header] = i + 1;
        }
        return index;
    }

    private static int ResolveColumn(Dictionary<string, int> colIndex, string mappedHeader, IEnumerable<string> aliases)
    {
        if (!string.IsNullOrWhiteSpace(mappedHeader) && colIndex.TryGetValue(mappedHeader.Trim(), out var col))
            return col;

        var fallback = FindHeader(colIndex.Keys.ToList(), aliases);
        if (fallback != null && colIndex.TryGetValue(fallback, out col))
            return col;

        throw new InvalidOperationException($"Required column not found: {mappedHeader}");
    }

    private static int ResolveOptionalColumn(Dictionary<string, int> colIndex, string? mappedHeader, IEnumerable<string> aliases)
    {
        if (!string.IsNullOrWhiteSpace(mappedHeader) && colIndex.TryGetValue(mappedHeader.Trim(), out var col))
            return col;

        var fallback = FindHeader(colIndex.Keys.ToList(), aliases);
        if (fallback != null && colIndex.TryGetValue(fallback, out col))
            return col;

        return 0;
    }

    private static string? FindHeader(IReadOnlyList<string> headers, IEnumerable<string> aliases)
    {
        foreach (var alias in aliases)
        {
            var match = headers.FirstOrDefault(h => NormalizeLabel(h) == NormalizeLabel(alias));
            if (match != null)
                return match;
        }

        return headers.FirstOrDefault(h => aliases.Any(a => NormalizeLabel(h).Contains(NormalizeLabel(a), StringComparison.Ordinal)));
    }

    private static string NormalizeLabel(string? value)
        => Regex.Replace((value ?? "").Trim().ToLowerInvariant(), @"\s+", " ");

    private static string ReadCellString(IXLWorksheet sheet, int row, int col)
    {
        if (col <= 0)
            return "";
        return sheet.Cell(row, col).GetFormattedString().Trim();
    }

    private static decimal ReadDecimal(IXLWorksheet sheet, int row, int col)
    {
        if (col <= 0)
            return 0m;

        var cell = sheet.Cell(row, col);
        if (cell.TryGetValue(out double d))
            return Convert.ToDecimal(d);

        var text = cell.GetFormattedString().Trim();
        if (string.IsNullOrWhiteSpace(text))
            return 0m;

        text = text.Replace(",", "").Replace("₹", "").Trim();
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return 0m;
    }

    private static IXLWorksheet ResolveSheet(XLWorkbook workbook, string? sheetName, IReadOnlyList<string> sheetNames)
    {
        if (!string.IsNullOrWhiteSpace(sheetName))
        {
            var ws = workbook.Worksheets.FirstOrDefault(w =>
                w.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));
            if (ws != null)
                return ws;
        }

        var tbSheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Contains("TB", StringComparison.OrdinalIgnoreCase) ||
            w.Name.Contains("Trial", StringComparison.OrdinalIgnoreCase));
        return tbSheet ?? workbook.Worksheet(sheetNames[0]);
    }
}
