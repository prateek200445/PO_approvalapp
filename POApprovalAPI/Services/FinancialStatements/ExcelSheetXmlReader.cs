using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace POApprovalAPI.Services.FinancialStatements;

/// <summary>
/// Reads cell values directly from xlsx XML (handles pivot-table sheets where ClosedXML misses labels).
/// </summary>
public static class ExcelSheetXmlReader
{
    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    public static Dictionary<(int Row, int Col), string> ReadSheet(byte[] xlsxBytes, string sheetName)
    {
        using var stream = new MemoryStream(xlsxBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var sharedStrings = ReadSharedStrings(archive);
        var sheetPath = ResolveSheetPath(archive, sheetName)
            ?? throw new InvalidOperationException($"Sheet '{sheetName}' not found in workbook.");

        var sheetEntry = archive.GetEntry(sheetPath)
            ?? archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("/" + sheetPath.Split('/').Last(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Sheet file '{sheetPath}' not found.");

        using var sheetStream = sheetEntry.Open();
        var doc = XDocument.Load(sheetStream);
        var cells = new Dictionary<(int Row, int Col), string>();

        foreach (var rowEl in doc.Descendants(Ns + "row"))
        {
            if (!int.TryParse(rowEl.Attribute("r")?.Value, out var rowNum))
                continue;

            foreach (var cell in rowEl.Elements(Ns + "c"))
            {
                var cellRef = cell.Attribute("r")?.Value;
                if (string.IsNullOrWhiteSpace(cellRef))
                    continue;

                var match = Regex.Match(cellRef, @"^([A-Z]+)(\d+)$");
                if (!match.Success)
                    continue;

                var colNum = ColumnLettersToNumber(match.Groups[1].Value);
                var value = ReadCellValue(cell, sharedStrings);
                cells[(rowNum, colNum)] = value;
            }
        }

        return cells;
    }

    public static string GetCell(Dictionary<(int Row, int Col), string> cells, int row, int col, string defaultValue = "")
        => cells.TryGetValue((row, col), out var value) ? value : defaultValue;

    public static decimal? GetDecimal(Dictionary<(int Row, int Col), string> cells, int row, int col)
    {
        var text = GetCell(cells, row, col);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Replace(",", "").Trim();
        return decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return [];

        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        var list = new List<string>();

        foreach (var si in doc.Descendants(Ns + "si"))
        {
            var texts = si.Descendants(Ns + "t").Select(t => t.Value);
            list.Add(string.Concat(texts));
        }

        return list;
    }

    private static string? ResolveSheetPath(ZipArchive archive, string sheetName)
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml");
        var relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
        if (workbookEntry == null || relsEntry == null)
            return null;

        using var wbStream = workbookEntry.Open();
        using var relStream = relsEntry.Open();
        var wbDoc = XDocument.Load(wbStream);
        var relDoc = XDocument.Load(relStream);

        XNamespace relNs = "http://schemas.openxmlformats.org/package/2006/relationships";
        var relMap = relDoc.Root?.Elements(relNs + "Relationship")
            .ToDictionary(r => r.Attribute("Id")?.Value ?? "", r => r.Attribute("Target")?.Value ?? "")
            ?? new Dictionary<string, string>();

        foreach (var sheet in wbDoc.Descendants(Ns + "sheet"))
        {
            if (!string.Equals(sheet.Attribute("name")?.Value, sheetName, StringComparison.OrdinalIgnoreCase))
                continue;

            XNamespace relAttrNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            var relId = sheet.Attribute(relAttrNs + "id")?.Value;
            if (relId == null || !relMap.TryGetValue(relId, out var target))
                return null;

            if (!target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
                target = "xl/" + target.TrimStart('/');

            if (archive.GetEntry(target) != null)
                return target;

            var fileName = target.Split('/').Last();
            return archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase))?.FullName;
        }

        return null;
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var valueEl = cell.Element(Ns + "v");
        if (valueEl == null)
            return "";

        var type = cell.Attribute("t")?.Value;
        if (type == "s" && int.TryParse(valueEl.Value, out var index) && index >= 0 && index < sharedStrings.Count)
            return sharedStrings[index];

        return valueEl.Value;
    }

    private static int ColumnLettersToNumber(string letters)
    {
        var n = 0;
        foreach (var c in letters)
            n = n * 26 + (c - 'A' + 1);
        return n;
    }
}
