using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class DailyProductionPriceService
{
    private const int CommandTimeoutSeconds = 90;
    private const string ExportCachePrefix = "daily-production:export:";
    private static readonly TimeSpan ExportTtl = TimeSpan.FromMinutes(30);

    private static readonly (string Label, double MinInclusive, double MaxExclusive, int Sort)[] WeightBands =
    {
        ("Below 0.6", 0, 0.6, 0),
        ("0.6-1", 0.6, 1.0, 1),
        ("1-1.4", 1.0, 1.4, 2),
        ("1.4-1.8", 1.4, 1.8, 3),
        ("1.8-2.2", 1.8, 2.2, 4),
        ("2.2-2.6", 2.2, 2.6, 5),
        ("2.6-3", 2.6, 3.0, 6),
        ("3-3.4", 3.0, 3.4, 7),
        ("3.4-3.8", 3.4, 3.8, 8),
        ("3.8-4.2", 3.8, 4.2, 9),
        ("4.2+", 4.2, double.MaxValue, 10),
    };

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;

    public DailyProductionPriceService(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
    }

    public bool TryGetExport(string token, out byte[]? bytes, out string fileName)
    {
        bytes = null;
        fileName = "daily-production.xlsx";
        if (string.IsNullOrWhiteSpace(token))
            return false;
        if (!_cache.TryGetValue(ExportCachePrefix + token.Trim(), out CachedExport? cached) || cached is null)
            return false;
        bytes = cached.Bytes;
        fileName = cached.FileName;
        return true;
    }

    public async Task<DailyProductionProcessResponse> ProcessAsync(
        Stream excelStream,
        string originalFileName,
        CancellationToken ct)
    {
        using var workbook = new XLWorkbook(excelStream);
        var sheet = FindProductionSheet(workbook);
        var headerRow = FindHeaderRow(sheet);
        var map = MapColumns(sheet, headerRow);

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        var parsed = new List<ParsedRow>();
        var icos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            ct.ThrowIfCancellationRequested();
            var row = sheet.Row(r);
            var ico = CellText(row, map.Ico);
            var po = CellText(row, map.Po);
            var consignee = CellText(row, map.Consignee);
            if (string.IsNullOrWhiteSpace(ico)
                && string.IsNullOrWhiteSpace(po)
                && string.IsNullOrWhiteSpace(consignee)
                && !HasAnyValue(row, map))
            {
                continue;
            }

            var item = new ParsedRow
            {
                ExcelRow = r,
                Ico = ico,
                IcoKey = NormalizeIco(ico),
                PoNo = po,
                PoKey = NormalizePo(po),
                Consignee = consignee,
                BagType = CellText(row, map.BagType),
                Size = CellText(row, map.Size),
                LineNo = CellText(row, map.Line),
                Pcs = CellNumber(row, map.Pcs),
                Kgs = CellNumber(row, map.Kgs),
                WeightPerPc = CellNumber(row, map.WeightPerPc),
            };
            parsed.Add(item);
            if (item.IcoKey.Length > 0)
                icos.Add(item.Ico.Trim());
        }

        if (parsed.Count == 0)
            throw new InvalidOperationException("No production rows found under the header row.");

        var asOf = DateTime.Today;
        var fxTask = LoadFxRatesAsync(asOf, ct);
        var pricesTask = LoadPricesAsync(icos.ToList(), ct);
        await Task.WhenAll(fxTask, pricesTask);
        var fx = await fxTask;
        var prices = await pricesTask;

        var priceCol = EnsureColumn(sheet, headerRow, map.LastHeaderCol, "Sales Price", map.SalesPrice);
        var currencyCol = EnsureColumn(sheet, headerRow, Math.Max(map.LastHeaderCol, priceCol), "Currency", map.Currency);
        var valueCol = EnsureColumn(sheet, headerRow, Math.Max(Math.Max(map.LastHeaderCol, priceCol), currencyCol), "Sales Value", map.SalesValue);

        foreach (var item in parsed)
        {
            var match = ResolvePrice(item, prices);
            if (match is not null && match.Rate > 0)
            {
                var originalCurrency = string.IsNullOrWhiteSpace(match.Currency) ? "INR" : match.Currency.Trim();
                double inrPrice;
                try
                {
                    inrPrice = ToInr(match.Rate, originalCurrency, fx);
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException($"ICO {item.Ico}: {ex.Message}");
                }
                item.OriginalPrice = match.Rate;
                item.OriginalCurrency = originalCurrency;
                item.SalesPrice = Math.Round(inrPrice, 4);
                item.Currency = "INR";
                item.SalesValue = Math.Round(inrPrice * item.Pcs, 2);
                item.Priced = true;
            }

            var excelRow = sheet.Row(item.ExcelRow);
            if (item.Priced)
            {
                excelRow.Cell(priceCol).Value = item.SalesPrice!.Value;
                excelRow.Cell(priceCol).Style.NumberFormat.Format = "0.00";
                excelRow.Cell(currencyCol).Value = "INR";
                excelRow.Cell(valueCol).Value = item.SalesValue!.Value;
                excelRow.Cell(valueCol).Style.NumberFormat.Format = "#,##0.00";
            }
            else
            {
                excelRow.Cell(priceCol).Clear();
                excelRow.Cell(currencyCol).Clear();
                excelRow.Cell(valueCol).Clear();
            }
        }

        sheet.Columns(priceCol, valueCol).AdjustToContents(headerRow, Math.Min(headerRow + 40, lastRow));

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        var bytes = output.ToArray();

        var token = Guid.NewGuid().ToString("N");
        var fileName = BuildFileName(originalFileName);
        _cache.Set(ExportCachePrefix + token, new CachedExport { Bytes = bytes, FileName = fileName }, ExportTtl);

        return new DailyProductionProcessResponse
        {
            DownloadToken = token,
            FileName = fileName,
            SheetName = sheet.Name,
            Summary = BuildSummary(parsed, fx),
        };
    }

    private async Task<FxSnapshot> LoadFxRatesAsync(DateTime asOf, CancellationToken ct)
    {
        using var connection = _database.CreateConnection();
        const string select = @"
SELECT TOP 1
    CAST(ISNULL(Dollar, 0) AS float) AS Dollar,
    CAST(ISNULL(Euro, 0) AS float) AS Euro,
    CAST(ISNULL(Pound, 0) AS float) AS Pound,
    CAST(ISNULL(CHF, 0) AS float) AS CHF,
    sysdate AS RateFrom,
    todate AS RateTo
FROM currency_rbi WITH (NOLOCK)";

        var window = await connection.QueryFirstOrDefaultAsync<FxSnapshot>(
            select + @"
WHERE CAST(@AsOf AS date) BETWEEN CAST(sysdate AS date) AND CAST(ISNULL(todate, DATEADD(YEAR, 5, GETDATE())) AS date)
ORDER BY sysdate DESC",
            new { AsOf = asOf.Date },
            commandTimeout: CommandTimeoutSeconds);

        if (window is not null && HasUsableFx(window))
        {
            window.AsOf = asOf.Date;
            window.UsedFallback = false;
            return window;
        }

        ct.ThrowIfCancellationRequested();
        var fallback = await connection.QueryFirstOrDefaultAsync<FxSnapshot>(
            select + @"
WHERE CAST(sysdate AS date) <= CAST(@AsOf AS date)
ORDER BY sysdate DESC",
            new { AsOf = asOf.Date },
            commandTimeout: CommandTimeoutSeconds);

        if (fallback is null || !HasUsableFx(fallback))
            throw new InvalidOperationException(
                $"No RBI forex found in currency_rbi for {asOf:yyyy-MM-dd} (or earlier). Cannot convert to INR.");

        fallback.AsOf = asOf.Date;
        fallback.UsedFallback = true;
        return fallback;
    }

    private static bool HasUsableFx(FxSnapshot fx) =>
        fx.Dollar > 1 || fx.Euro > 1 || fx.Pound > 1 || fx.CHF > 1;

    private static double ToInr(double amount, string currency, FxSnapshot fx)
    {
        var key = NormalizeFxCurrency(currency);
        var rate = key switch
        {
            "INR" => 1d,
            "USD" => fx.Dollar,
            "EUR" => fx.Euro,
            "GBP" => fx.Pound,
            "CHF" => fx.CHF,
            _ => throw new InvalidOperationException(
                $"Cannot convert currency '{currency}' to INR. Supported: INR, USD, EUR, GBP, CHF."),
        };
        if (key != "INR" && rate <= 1)
            throw new InvalidOperationException(
                $"RBI {key} rate is missing or invalid ({rate}) for {fx.AsOf:yyyy-MM-dd}.");
        return amount * rate;
    }

    private static string NormalizeFxCurrency(string? currency)
    {
        var c = (currency ?? "").Trim();
        if (string.IsNullOrEmpty(c)) return "INR";
        if (c.StartsWith("Rs", StringComparison.OrdinalIgnoreCase)
            || c.Equals("INR", StringComparison.OrdinalIgnoreCase)
            || c == "₹")
            return "INR";
        if (c is "$" or "USD" or "US$" or "DOLLAR" or "DOLLARS")
            return "USD";
        if (c is "€"
            || c.Equals("EUR", StringComparison.OrdinalIgnoreCase)
            || c.Equals("EURO", StringComparison.OrdinalIgnoreCase)
            || c.Equals("EUROS", StringComparison.OrdinalIgnoreCase))
            return "EUR";
        if (c.Equals("GBP", StringComparison.OrdinalIgnoreCase)
            || c.Equals("POUND", StringComparison.OrdinalIgnoreCase)
            || c == "£")
            return "GBP";
        if (c.Equals("CHF", StringComparison.OrdinalIgnoreCase))
            return "CHF";
        return c.ToUpperInvariant();
    }

    private async Task<Dictionary<string, List<MarketingPriceRow>>> LoadPricesAsync(
        IReadOnlyList<string> icos,
        CancellationToken ct)
    {
        var result = new Dictionary<string, List<MarketingPriceRow>>(StringComparer.OrdinalIgnoreCase);
        if (icos.Count == 0)
            return result;

        using var connection = _database.CreateProductionConnection();
        for (var i = 0; i < icos.Count; i += 400)
        {
            ct.ThrowIfCancellationRequested();
            var chunk = icos.Skip(i).Take(400).ToList();
            var rows = await connection.QueryAsync<MarketingPriceRow>(@"
SELECT
    LTRIM(RTRIM(MarketingInvNo)) AS MarketingInvNo,
    LTRIM(RTRIM(BuyerOrderNo)) AS BuyerOrderNo,
    Rate,
    ProductionRate,
    Currency,
    Qty,
    TypeofBag,
    ItemDesc
FROM Despatch.dbo.Vw_MarketingInvoice WITH (NOLOCK)
WHERE LTRIM(RTRIM(MarketingInvNo)) IN @Icos",
                new { Icos = chunk },
                commandTimeout: CommandTimeoutSeconds);

            foreach (var row in rows)
            {
                var key = NormalizeIco(row.MarketingInvNo);
                if (key.Length == 0)
                    continue;
                row.Rate = CoalesceRate(row.Rate, row.ProductionRate);
                row.Currency = string.IsNullOrWhiteSpace(row.Currency) ? "" : row.Currency.Trim();
                if (!result.TryGetValue(key, out var list))
                {
                    list = new List<MarketingPriceRow>();
                    result[key] = list;
                }
                list.Add(row);
            }
        }

        return result;
    }

    private static MarketingPriceRow? ResolvePrice(
        ParsedRow item,
        IReadOnlyDictionary<string, List<MarketingPriceRow>> prices)
    {
        if (item.IcoKey.Length == 0)
            return null;
        if (!prices.TryGetValue(item.IcoKey, out var list) || list.Count == 0)
            return null;

        MarketingPriceRow? best = null;
        var bestScore = int.MinValue;
        foreach (var row in list)
        {
            var score = 0;
            if (item.PoKey.Length > 0 && NormalizePo(row.BuyerOrderNo) == item.PoKey)
                score += 100;
            else if (item.PoKey.Length > 0 && LoosePoOverlap(item.PoKey, NormalizePo(row.BuyerOrderNo)))
                score += 40;

            if (LooksLikeBag(row.TypeofBag) || LooksLikeBag(row.ItemDesc))
                score += 10;
            if (LooksLikeLabel(row.TypeofBag) || LooksLikeLabel(row.ItemDesc))
                score -= 30;

            if (row.Qty > 0)
                score += Math.Min(20, (int)Math.Log10(row.Qty + 1) * 5);

            if (score > bestScore)
            {
                bestScore = score;
                best = row;
            }
        }

        return best;
    }

    private static DailyProductionSummary BuildSummary(List<ParsedRow> parsed, FxSnapshot fx)
    {
        var pricedRows = parsed.Count(p => p.Priced);
        var uniqueIco = parsed
            .Where(p => p.IcoKey.Length > 0)
            .Select(p => p.IcoKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var pricedIco = parsed
            .Where(p => p.Priced && p.IcoKey.Length > 0)
            .Select(p => p.IcoKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var summary = new DailyProductionSummary
        {
            ProductionRows = parsed.Count,
            UniqueIcoCount = uniqueIco.Count,
            PricedIcoCount = pricedIco,
            PricedRowCount = pricedRows,
            UnpricedRowCount = parsed.Count - pricedRows,
            TotalPcs = parsed.Sum(p => p.Pcs),
            TotalKgs = parsed.Sum(p => p.Kgs),
            TotalSalesValueInr = parsed.Where(p => p.Priced).Sum(p => p.SalesValue ?? 0),
            Fx = new DailyProductionFxInfo
            {
                AsOf = fx.AsOf.ToString("yyyy-MM-dd"),
                RateFrom = fx.RateFrom?.ToString("yyyy-MM-dd") ?? "",
                RateTo = fx.RateTo?.ToString("yyyy-MM-dd") ?? "",
                UsedFallback = fx.UsedFallback,
                Dollar = fx.Dollar,
                Euro = fx.Euro,
                Pound = fx.Pound,
                Chf = fx.CHF,
            },
            CurrencyTotals = RollupCurrency(parsed.Where(p => p.Priced)),
            ByLine = parsed
                .GroupBy(p => string.IsNullOrWhiteSpace(p.LineNo) ? "(blank)" : p.LineNo)
                .Select(g => new DailyProductionLineRow
                {
                    LineNo = g.Key,
                    Pcs = g.Sum(x => x.Pcs),
                    Kgs = g.Sum(x => x.Kgs),
                    PricedRows = g.Count(x => x.Priced),
                    UnpricedRows = g.Count(x => !x.Priced),
                    Currencies = RollupCurrency(g.Where(x => x.Priced)),
                })
                .OrderBy(x => LineSort(x.LineNo))
                .ThenBy(x => x.LineNo, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ByWeightBand = parsed
                .GroupBy(p => BandFor(p.WeightPerPc))
                .Select(g => new DailyProductionBandRow
                {
                    Band = g.Key.Label,
                    SortOrder = g.Key.Sort,
                    Pcs = g.Sum(x => x.Pcs),
                    Kgs = g.Sum(x => x.Kgs),
                    Currencies = RollupCurrency(g.Where(x => x.Priced)),
                })
                .OrderBy(x => x.SortOrder)
                .ToList(),
        };

        var icoGroups = parsed
            .Where(p => p.IcoKey.Length > 0)
            .GroupBy(p => p.IcoKey, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                var priced = g.FirstOrDefault(x => x.Priced);
                var pcs = g.Sum(x => x.Pcs);
                var kgs = g.Sum(x => x.Kgs);
                var value = priced?.SalesPrice is double rate ? rate * pcs : (double?)null;
                var valuePerKg = value is > 0 && kgs > 0 ? value / kgs : null;
                var lines = string.Join(", ", g.Select(x => x.LineNo).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(LineSort));
                return new DailyProductionIcoRow
                {
                    Ico = priced?.Ico ?? first.Ico,
                    Consignee = first.Consignee,
                    PoNo = first.PoNo,
                    BagType = first.BagType,
                    Size = first.Size,
                    LineNos = lines,
                    Pcs = pcs,
                    Kgs = kgs,
                    WeightPerPc = first.WeightPerPc,
                    SalesPrice = priced?.SalesPrice,
                    Currency = priced?.Currency ?? "",
                    OriginalPrice = priced?.OriginalPrice,
                    OriginalCurrency = priced?.OriginalCurrency ?? "",
                    SalesValue = value,
                    ValuePerKg = valuePerKg,
                    Priced = priced is not null,
                };
            })
            .OrderByDescending(x => x.Priced)
            .ThenByDescending(x => x.ValuePerKg ?? -1)
            .ToList();

        summary.ByIco = icoGroups;
        summary.Unpriced = icoGroups
            .Where(x => !x.Priced)
            .Select(x => new DailyProductionUnpricedRow
            {
                Ico = x.Ico,
                PoNo = x.PoNo,
                Consignee = x.Consignee,
                Pcs = x.Pcs,
            })
            .ToList();
        summary.Hints = BuildHints(summary);
        return summary;
    }

    private static List<string> BuildHints(DailyProductionSummary summary)
    {
        var hints = new List<string>
        {
            "Sales price and value are ERP Rate converted to INR using RBI forex for the upload date — still selling price, not profit.",
        };

        foreach (var line in summary.ByLine)
        {
            var ranked = summary.ByIco
                .Where(i => i.Priced
                            && i.ValuePerKg is > 0
                            && i.LineNos.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                                .Contains(line.LineNo, StringComparer.OrdinalIgnoreCase)
                            && i.Pcs >= 50)
                .OrderBy(i => i.ValuePerKg)
                .ToList();
            if (ranked.Count < 2)
                continue;
            var low = ranked.First();
            var high = ranked.Last();
            if (high.ValuePerKg is not double hv || low.ValuePerKg is not double lv || hv < lv * 1.4)
                continue;
            hints.Add(
                $"Line {line.LineNo}: {high.Ico} is ₹{hv:0.00}/kg vs {low.Ico} at ₹{lv:0.00}/kg. If that line can run the higher-value bag, consider shifting mix toward {high.Ico}.");
        }

        var weakBands = summary.ByWeightBand
            .Where(b => b.Pcs > 0)
            .Select(b =>
            {
                var inr = b.Currencies.FirstOrDefault();
                var valuePerKg = inr is { Kgs: > 0 } ? inr.SalesValue / inr.Kgs : 0;
                return (b.Band, b.Pcs, valuePerKg, Share: b.Pcs / Math.Max(summary.TotalPcs, 1));
            })
            .Where(x => x.Share >= 0.15 && x.valuePerKg > 0)
            .OrderBy(x => x.valuePerKg)
            .ToList();

        if (weakBands.Count >= 1)
        {
            var weak = weakBands.First();
            var strong = summary.ByWeightBand
                .Select(b =>
                {
                    var inr = b.Currencies.FirstOrDefault();
                    var vpk = inr is { Kgs: > 0 } ? inr.SalesValue / inr.Kgs : 0;
                    return (b.Band, vpk, b.Pcs);
                })
                .Where(x => x.vpk > weak.valuePerKg * 1.3 && x.Pcs > 0)
                .OrderByDescending(x => x.vpk)
                .FirstOrDefault();
            if (strong.Band is not null)
            {
                hints.Add(
                    $"Weight band {weak.Band} is {weak.Share:P0} of pcs with lower INR value/kg than {strong.Band}. Consider whether some of that volume can move to a higher-value bag.");
            }
        }

        return hints.Take(8).ToList();
    }

    private static List<DailyProductionCurrencyTotal> RollupCurrency(IEnumerable<ParsedRow> rows)
    {
        return rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Currency) ? "—" : r.Currency, StringComparer.OrdinalIgnoreCase)
            .Select(g => new DailyProductionCurrencyTotal
            {
                Currency = g.Key,
                SalesValue = g.Sum(x => x.SalesValue ?? 0),
                Pcs = g.Sum(x => x.Pcs),
                Kgs = g.Sum(x => x.Kgs),
            })
            .OrderByDescending(x => x.SalesValue)
            .ToList();
    }

    private static (string Label, int Sort) BandFor(double weight)
    {
        if (weight <= 0)
            return ("Unknown wt/pc", 99);
        foreach (var band in WeightBands)
        {
            if (weight >= band.MinInclusive && weight < band.MaxExclusive)
                return (band.Label, band.Sort);
        }
        return ("4.2+", 10);
    }

    private static IXLWorksheet FindProductionSheet(XLWorkbook workbook)
    {
        var named = workbook.Worksheets.FirstOrDefault(s =>
            s.Name.Trim().Contains("production", StringComparison.OrdinalIgnoreCase));
        if (named is not null)
            return named;

        foreach (var sheet in workbook.Worksheets)
        {
            if (FindHeaderRowOrZero(sheet) > 0)
                return sheet;
        }

        throw new InvalidOperationException(
            "Could not find a Production sheet. Expected a tab named like 'production report' with ICO# and P.O.No columns.");
    }

    private static int FindHeaderRow(IXLWorksheet sheet)
    {
        var row = FindHeaderRowOrZero(sheet);
        if (row <= 0)
            throw new InvalidOperationException("Could not find a header row with ICO / P.O. columns.");
        return row;
    }

    private static int FindHeaderRowOrZero(IXLWorksheet sheet)
    {
        var last = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 1, 20);
        for (var r = 1; r <= last; r++)
        {
            var texts = sheet.Row(r).CellsUsed().Select(c => NormalizeHeader(c.GetString())).ToList();
            var hasIco = texts.Any(t => t is "ico" or "icono" or "marketinginvno" or "mktinv");
            var hasPo = texts.Any(t => t.Contains("po") || t.Contains("buyerorder"));
            if (hasIco && (hasPo || texts.Any(t => t.Contains("pcs"))))
                return r;
        }
        return 0;
    }

    private static ColumnMap MapColumns(IXLWorksheet sheet, int headerRow)
    {
        var map = new ColumnMap();
        var last = sheet.Row(headerRow).LastCellUsed()?.Address.ColumnNumber ?? 1;
        map.LastHeaderCol = last;

        for (var c = 1; c <= last; c++)
        {
            var key = NormalizeHeader(sheet.Cell(headerRow, c).GetString());
            if (key.Length == 0)
                continue;
            if (map.Ico == 0 && (key is "ico" or "icono" or "marketinginvno" || key.StartsWith("ico")))
                map.Ico = c;
            else if (map.Po == 0 && (key.Contains("pono") || key is "po" || key.Contains("buyerorder") || key.Contains("orderno")))
                map.Po = c;
            else if (map.Line == 0 && (key.Contains("lineno") || key is "line"))
                map.Line = c;
            else if (map.Consignee == 0 && key.Contains("consignee"))
                map.Consignee = c;
            else if (map.BagType == 0 && (key.Contains("typeofbag") || key.Contains("bagtype")))
                map.BagType = c;
            else if (map.Size == 0 && (key.Contains("sizein") || key == "size" || key.Contains("sizeincm")))
                map.Size = c;
            else if (map.Pcs == 0 && key is "pcs" or "pc" or "qty")
                map.Pcs = c;
            else if (map.Kgs == 0 && (key is "kgs" or "kg" or "kilos"))
                map.Kgs = c;
            else if (map.WeightPerPc == 0 && (key.Contains("weightper") || key.Contains("wtper") || key is "wtpc"))
                map.WeightPerPc = c;
            else if (map.SalesPrice == 0 && (key is "salesprice" or "rate" || key.Contains("salesprice")))
                map.SalesPrice = c;
            else if (map.Currency == 0 && key is "currency")
                map.Currency = c;
            else if (map.SalesValue == 0 && (key is "salesvalue" or "amount"))
                map.SalesValue = c;
        }

        if (map.Ico == 0)
            throw new InvalidOperationException("Production sheet is missing an ICO# column.");
        if (map.Pcs == 0)
            throw new InvalidOperationException("Production sheet is missing a pcs column.");

        return map;
    }

    private static int EnsureColumn(IXLWorksheet sheet, int headerRow, int afterCol, string title, int existing)
    {
        if (existing > 0)
        {
            sheet.Cell(headerRow, existing).Value = title;
            return existing;
        }

        var col = afterCol + 1;
        var header = sheet.Cell(headerRow, col);
        var template = sheet.Cell(headerRow, Math.Max(afterCol, 1));
        header.Value = title;
        header.Style = template.Style;
        header.Style.Font.Bold = true;
        return col;
    }

    private static bool HasAnyValue(IXLRow row, ColumnMap map)
    {
        foreach (var col in new[] { map.Pcs, map.Kgs, map.Line, map.WeightPerPc })
        {
            if (col > 0 && !row.Cell(col).IsEmpty())
                return true;
        }
        return false;
    }

    private static string CellText(IXLRow row, int col)
    {
        if (col <= 0)
            return "";
        var cell = row.Cell(col);
        var text = cell.GetFormattedString()?.Trim() ?? "";
        if (text.Length > 0)
            return text.Replace('\u00a0', ' ').Trim();
        return cell.IsEmpty() ? "" : cell.GetString().Replace('\u00a0', ' ').Trim();
    }

    private static double CellNumber(IXLRow row, int col)
    {
        if (col <= 0)
            return 0;
        var cell = row.Cell(col);
        if (cell.TryGetValue(out double number))
            return number;
        var text = cell.GetString().Trim();
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
            return number;
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out number))
            return number;
        return 0;
    }

    private static string NormalizeHeader(string raw)
    {
        var s = (raw ?? "").Replace('\u00a0', ' ').Trim().ToLowerInvariant();
        return Regex.Replace(s, @"[^a-z0-9]+", "");
    }

    private static string NormalizeIco(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";
        return Regex.Replace(raw.Replace('\u00a0', ' ').Trim(), @"\s+", "").ToUpperInvariant();
    }

    private static string NormalizePo(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";
        return Regex.Replace(raw.Replace('\u00a0', ' ').ToUpperInvariant(), @"[^A-Z0-9]+", "");
    }

    private static bool LoosePoOverlap(string a, string b)
    {
        if (a.Length < 6 || b.Length < 6)
            return false;
        return a.Contains(b) || b.Contains(a);
    }

    private static bool LooksLikeBag(string? text)
    {
        var t = (text ?? "").ToLowerInvariant();
        return t.Contains("panel") || t.Contains("circular") || t.Contains("baffle") || t.Contains("fibc") || t.Contains("bag");
    }

    private static bool LooksLikeLabel(string? text)
    {
        var t = (text ?? "").ToLowerInvariant();
        return t.Contains("label") || t.Contains("woven label");
    }

    private static double CoalesceRate(double rate, double productionRate)
    {
        if (rate > 0)
            return rate;
        if (productionRate > 0)
            return productionRate;
        return rate;
    }

    private static int LineSort(string line)
    {
        return int.TryParse(line, out var n) ? n : 999;
    }

    private static string BuildFileName(string original)
    {
        var name = Path.GetFileNameWithoutExtension(original);
        if (string.IsNullOrWhiteSpace(name))
            name = "Daily Production Report";
        if (name.Contains("sales price", StringComparison.OrdinalIgnoreCase))
            return name + ".xlsx";
        return name + " with sales price.xlsx";
    }

    private sealed class ColumnMap
    {
        public int LastHeaderCol { get; set; }
        public int Ico { get; set; }
        public int Po { get; set; }
        public int Line { get; set; }
        public int Consignee { get; set; }
        public int BagType { get; set; }
        public int Size { get; set; }
        public int Pcs { get; set; }
        public int Kgs { get; set; }
        public int WeightPerPc { get; set; }
        public int SalesPrice { get; set; }
        public int Currency { get; set; }
        public int SalesValue { get; set; }
    }

    private sealed class ParsedRow
    {
        public int ExcelRow { get; set; }
        public string Ico { get; set; } = "";
        public string IcoKey { get; set; } = "";
        public string PoNo { get; set; } = "";
        public string PoKey { get; set; } = "";
        public string Consignee { get; set; } = "";
        public string BagType { get; set; } = "";
        public string Size { get; set; } = "";
        public string LineNo { get; set; } = "";
        public double Pcs { get; set; }
        public double Kgs { get; set; }
        public double WeightPerPc { get; set; }
        public double? SalesPrice { get; set; }
        public string Currency { get; set; } = "";
        public double? OriginalPrice { get; set; }
        public string OriginalCurrency { get; set; } = "";
        public double? SalesValue { get; set; }
        public bool Priced { get; set; }
    }

    private sealed class FxSnapshot
    {
        public DateTime AsOf { get; set; }
        public DateTime? RateFrom { get; set; }
        public DateTime? RateTo { get; set; }
        public bool UsedFallback { get; set; }
        public double Dollar { get; set; }
        public double Euro { get; set; }
        public double Pound { get; set; }
        public double CHF { get; set; }
    }

    private sealed class MarketingPriceRow
    {
        public string MarketingInvNo { get; set; } = "";
        public string BuyerOrderNo { get; set; } = "";
        public double Rate { get; set; }
        public double ProductionRate { get; set; }
        public string Currency { get; set; } = "";
        public double Qty { get; set; }
        public string TypeofBag { get; set; } = "";
        public string ItemDesc { get; set; } = "";
    }

    private sealed class CachedExport
    {
        public required byte[] Bytes { get; init; }
        public required string FileName { get; init; }
    }
}
