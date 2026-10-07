using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace POApprovalAPI.Services;

/// <summary>
/// Stores shipping-bill data uploaded from the ICEGATE "export status" workbook and matches it to ERP export invoices.
/// ICEGATE data is per exporter IEC, so its "Company Name" is only a label; matching uses the shipping bill number
/// and, failing that, the invoice number.
/// </summary>
public sealed class IcegateService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string[] EmptyMarkers = ["N.A.", "NA", "N/A", "-", ".", "NULL"];
    private static readonly string[] DateFormats =
    [
        "dd/MM/yyyy", "d/M/yyyy", "dd-MMM-yyyy HH:mm:ss", "dd-MMM-yyyy", "d-MMM-yyyy", "dd-MM-yyyy", "yyyy-MM-dd",
        "dd/MM/yyyy HH:mm:ss", "yyyy-MM-ddTHH:mm:ss",
    ];

    private readonly object _gate = new();
    private readonly string _storePath;
    private IcegateStore? _store;

    public IcegateService(IConfiguration config, IHostEnvironment env)
    {
        var configured = config["Icegate:DataDirectory"];
        var root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(env.ContentRootPath, "Data", "Icegate")
            : configured;
        Directory.CreateDirectory(root);
        var ignore = Path.Combine(root, ".gitignore");
        if (!File.Exists(ignore))
            File.WriteAllText(ignore, "*\n");
        _storePath = Path.Combine(root, "store.json");
    }

    // ---------------------------------------------------------------- store

    public IcegateStatusDto GetStatus()
    {
        lock (_gate)
        {
            var store = Load();
            return new IcegateStatusDto(store.ShippingBills.Count, store.ShippingBills.Sum(b => b.Invoices.Count),
                store.Uploads.OrderByDescending(u => u.UploadedAtUtc).ToList());
        }
    }

    public IReadOnlyList<IcegateShippingBill> Snapshot()
    {
        lock (_gate)
            return Load().ShippingBills.ToList();
    }

    public IcegateUpload Import(Stream stream, string fileName)
    {
        var parsed = Parse(stream);
        if (parsed.Count == 0)
            throw new InvalidOperationException("No shipping bills found. Upload the ICEGATE export workbook (Shipping_Bill_Details, LEGM_Status, IGST_Invoice_Details sheets).");

        var upload = new IcegateUpload
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            FileName = Path.GetFileName(fileName),
            UploadedAtUtc = DateTime.UtcNow,
            ShippingBills = parsed.Count,
            Invoices = parsed.Sum(b => b.Invoices.Count),
            FirstSbDate = parsed.Min(b => b.SbDate),
            LastSbDate = parsed.Max(b => b.SbDate),
            CompanyLabels = parsed.Select(b => b.CompanyLabel).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
        };
        foreach (var b in parsed)
            b.UploadId = upload.Id;

        lock (_gate)
        {
            var store = Load();
            var keys = parsed.Select(b => b.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            upload.Replaced = store.ShippingBills.RemoveAll(b => keys.Contains(b.Key));
            store.ShippingBills.AddRange(parsed);
            store.Uploads.Add(upload);
            var live = store.ShippingBills.Select(b => b.UploadId).ToHashSet();
            store.Uploads.RemoveAll(u => !live.Contains(u.Id));
            Save(store);
        }
        return upload;
    }

    public bool DeleteUpload(string id)
    {
        lock (_gate)
        {
            var store = Load();
            var removed = store.ShippingBills.RemoveAll(b => b.UploadId == id) + store.Uploads.RemoveAll(u => u.Id == id);
            if (removed > 0)
                Save(store);
            return removed > 0;
        }
    }

    private IcegateStore Load()
    {
        if (_store is not null)
            return _store;
        _store = File.Exists(_storePath)
            ? JsonSerializer.Deserialize<IcegateStore>(File.ReadAllText(_storePath)) ?? new IcegateStore()
            : new IcegateStore();
        return _store;
    }

    private void Save(IcegateStore store)
    {
        var tmp = _storePath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(store, JsonOptions));
        File.Move(tmp, _storePath, overwrite: true);
        _store = store;
    }

    // ---------------------------------------------------------------- parsing

    public static List<IcegateShippingBill> Parse(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var bills = new Dictionary<string, IcegateShippingBill>(StringComparer.OrdinalIgnoreCase);

        IcegateShippingBill Bill(Row row)
        {
            var port = row.Text("portcode").ToUpperInvariant();
            var sb = NormSb(row.Text("sbnumber", "sbno", "shippingbillno", "shippingbillnumber"));
            var date = row.Date("sbdate", "shippingbilldate");
            var key = BillKey(port, sb, date);
            if (!bills.TryGetValue(key, out var bill))
            {
                bill = new IcegateShippingBill { PortCode = port, SbNo = sb, SbDate = date, CompanyLabel = row.Text("companyname") };
                bills[key] = bill;
            }
            return bill;
        }

        foreach (var row in Rows(FindSheet(wb, "shippingbilldetails", 1)))
        {
            if (row.Text("sbnumber", "sbno").Length == 0) continue;
            var bill = Bill(row);
            bill.Fob = row.Number("fobinr", "fob");
        }

        foreach (var row in Rows(FindSheet(wb, "legmstatus", 4)))
        {
            if (row.Text("sbnumber", "sbno").Length == 0) continue;
            var bill = Bill(row);
            var egm = row.Text("egmno", "egmnumber");
            if (egm.Length > 0)
            {
                bill.EgmNo = egm;
                bill.EgmDate = row.Date("egmdate");
            }
        }

        foreach (var row in Rows(FindSheet(wb, "igstinvoicedetails", 7)))
        {
            if (row.Text("sbnumber", "sbno").Length == 0) continue;
            var bill = Bill(row);
            var no = row.Text("invoiceno", "invoicenumber");
            if (no.Length == 0) continue;
            bill.Invoices.RemoveAll(i => string.Equals(NormInvoice(i.No), NormInvoice(no), StringComparison.OrdinalIgnoreCase));
            bill.Invoices.Add(new IcegateInvoice { No = no, Date = row.Date("invoicedate"), IgstPaid = row.Number("igstpaidinr", "igstpaid", "igstamount") });
        }

        return bills.Values.Where(b => b.SbNo.Length > 0).ToList();
    }

    private static IXLWorksheet? FindSheet(XLWorkbook wb, string normalisedName, int position)
    {
        var byName = wb.Worksheets.FirstOrDefault(w => NormHeader(w.Name).Contains(normalisedName, StringComparison.Ordinal));
        if (byName is not null) return byName;
        return wb.Worksheets.Count >= position ? wb.Worksheet(position) : null;
    }

    private static IEnumerable<Row> Rows(IXLWorksheet? ws)
    {
        var used = ws?.RangeUsed();
        if (ws is null || used is null) yield break;
        var headerRow = used.FirstRow().RowNumber();
        var lastCol = used.LastColumn().ColumnNumber();
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var c = 1; c <= lastCol; c++)
        {
            var h = NormHeader(ws.Cell(headerRow, c).GetString());
            if (h.Length > 0) map.TryAdd(h, c);
        }
        for (var r = headerRow + 1; r <= used.LastRow().RowNumber(); r++)
            yield return new Row(ws.Row(r), map);
    }

    private sealed class Row(IXLRow row, Dictionary<string, int> map)
    {
        private IXLCell? Cell(string[] names)
        {
            foreach (var n in names)
                if (map.TryGetValue(n, out var c)) return row.Cell(c);
            foreach (var n in names)
            {
                var hit = map.FirstOrDefault(kv => kv.Key.StartsWith(n, StringComparison.Ordinal));
                if (hit.Key is not null) return row.Cell(hit.Value);
            }
            return null;
        }

        public string Text(params string[] names)
        {
            var cell = Cell(names);
            if (cell is null || cell.IsEmpty()) return "";
            var v = cell.Value;
            var s = v.IsNumber ? v.GetNumber().ToString("0.##", CultureInfo.InvariantCulture)
                : v.IsDateTime ? v.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : cell.GetString();
            s = Regex.Replace(s, @"\s+", " ").Trim();
            return EmptyMarkers.Contains(s, StringComparer.OrdinalIgnoreCase) ? "" : s;
        }

        public decimal? Number(params string[] names)
        {
            var cell = Cell(names);
            if (cell is null || cell.IsEmpty()) return null;
            if (cell.Value.IsNumber) return (decimal)cell.Value.GetNumber();
            var s = Text(names).Replace(",", "");
            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        public DateTime? Date(params string[] names)
        {
            var cell = Cell(names);
            if (cell is null || cell.IsEmpty()) return null;
            if (cell.Value.IsDateTime) return cell.Value.GetDateTime().Date;
            var s = Text(names);
            if (s.Length == 0) return null;
            return DateTime.TryParseExact(s, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var d)
                ? d.Date
                : DateTime.TryParse(s, CultureInfo.GetCultureInfo("en-IN"), DateTimeStyles.AllowWhiteSpaces, out d) ? d.Date : null;
        }
    }

    // ---------------------------------------------------------------- normalisation

    private static string NormHeader(string s) => Regex.Replace(s.ToLowerInvariant(), "[^a-z0-9]", "");

    public static string NormSb(string? s)
    {
        var digits = Regex.Replace(s ?? "", @"\D", "");
        return digits.TrimStart('0');
    }

    /// <summary>Upper-cases, drops spaces and the trailing financial-year suffix, and strips leading zeros from numeric parts.</summary>
    public static string NormInvoice(string? s)
    {
        var v = Regex.Replace((s ?? "").ToUpperInvariant(), @"\s+", "");
        v = Regex.Replace(v, @"/(\d{2}|\d{4})-(\d{2}|\d{4})$", "");
        return Regex.Replace(v, @"\d+", m => m.Value.TrimStart('0') is { Length: > 0 } t ? t : "0");
    }

    public static string InvoicePrefix(string? s)
    {
        var m = Regex.Match(NormInvoice(s), @"^(.*?)\d+$");
        return m.Success ? m.Groups[1].Value : "";
    }

    private static string BillKey(string port, string sb, DateTime? date) => $"{port}|{sb}|{date:yyyy-MM-dd}";

    // ---------------------------------------------------------------- matching

    /// <summary>Matches ERP export invoices (all companies) to the stored shipping bills.</summary>
    public static IcegateMatchResult Match(IReadOnlyList<IcegateErpInvoice> erp, IReadOnlyList<IcegateShippingBill> bills)
    {
        var bySb = bills.Where(b => b.SbNo.Length > 0).ToLookup(b => b.SbNo);
        var byInvoice = bills.SelectMany(b => b.Invoices.Select(i => (Bill: b, Invoice: i)))
            .ToLookup(x => NormInvoice(x.Invoice.No), StringComparer.OrdinalIgnoreCase);

        var matches = new Dictionary<IcegateErpInvoice, IcegateMatch>();
        var used = new HashSet<(IcegateShippingBill, IcegateInvoice?)>();

        foreach (var inv in erp)
        {
            var norm = NormInvoice(inv.PrintedNo);
            var normErp = NormInvoice(inv.ErpNo);
            (IcegateShippingBill Bill, IcegateInvoice Invoice)? byNo = byInvoice[norm].Concat(byInvoice[normErp])
                .OrderBy(x => x.Invoice.Date is { } d ? Math.Abs((d - inv.Date).TotalDays) : 999)
                .Select(x => ((IcegateShippingBill, IcegateInvoice)?)x)
                .FirstOrDefault();

            IcegateShippingBill? bill = null;
            IcegateInvoice? line = null;
            var by = "";
            var sb = NormSb(inv.ShippingBillNo);
            if (sb.Length > 0)
            {
                bill = bySb[sb]
                    .OrderBy(b => string.Equals(b.PortCode, inv.PortCode, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(b => b.SbDate is { } d && inv.ShippingBillDate is { } e ? Math.Abs((d - e).TotalDays) : 999)
                    .FirstOrDefault();
                if (bill is not null)
                {
                    by = "shipping bill";
                    line = bill.Invoices.FirstOrDefault(i => NormInvoice(i.No) == norm || NormInvoice(i.No) == normErp)
                           ?? (bill.Invoices.Count == 1 ? bill.Invoices[0] : null);
                }
            }
            var invoiceAgrees = line is not null && (NormInvoice(line.No) == norm || NormInvoice(line.No) == normErp);
            if (byNo is { } hit && !invoiceAgrees && !(bill is not null && bill.Invoices.Count == 0))
            {
                bill = hit.Bill;
                line = hit.Invoice;
                by = sb.Length > 0 ? "invoice no (ERP shipping bill differs)" : "invoice no";
            }
            if (bill is null) continue;
            used.Add((bill, line));
            matches[inv] = new IcegateMatch(bill, line, by);
        }

        // Taxable value of every ERP invoice on the same shipping bill, for the FOB comparison.
        var billTaxable = matches.GroupBy(kv => kv.Value.Bill)
            .ToDictionary(g => g.Key, g => (Taxable: g.Sum(kv => kv.Key.Taxable), Count: g.Count()));

        var prefixes = erp.GroupBy(e => InvoicePrefix(e.PrintedNo), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Company).Distinct(StringComparer.OrdinalIgnoreCase).ToList(), StringComparer.OrdinalIgnoreCase);

        var unmatched = new List<IcegateUnmatched>();
        foreach (var b in bills)
        {
            var lines = b.Invoices.Count == 0 ? [null] : b.Invoices.Cast<IcegateInvoice?>().ToList();
            foreach (var l in lines)
            {
                if (used.Contains((b, l)) || (l is null && used.Any(u => u.Item1 == b))) continue;
                if (l is not null && b.Invoices.Count == 1 && used.Contains((b, null))) continue;
                var company = l is not null && prefixes.TryGetValue(InvoicePrefix(l.No), out var cs) && cs.Count == 1 ? cs[0] : "";
                unmatched.Add(new IcegateUnmatched(b, l, company));
            }
        }

        return new IcegateMatchResult(matches, billTaxable, unmatched);
    }
}

// ---------------------------------------------------------------- models

public sealed class IcegateStore
{
    public List<IcegateShippingBill> ShippingBills { get; set; } = new();
    public List<IcegateUpload> Uploads { get; set; } = new();
}

public sealed class IcegateShippingBill
{
    public string CompanyLabel { get; set; } = "";
    public string PortCode { get; set; } = "";
    public string SbNo { get; set; } = "";
    public DateTime? SbDate { get; set; }
    public decimal? Fob { get; set; }
    public string EgmNo { get; set; } = "";
    public DateTime? EgmDate { get; set; }
    public List<IcegateInvoice> Invoices { get; set; } = new();
    public string UploadId { get; set; } = "";

    [System.Text.Json.Serialization.JsonIgnore]
    public string Key => $"{PortCode}|{SbNo}|{SbDate:yyyy-MM-dd}";
}

public sealed class IcegateInvoice
{
    public string No { get; set; } = "";
    public DateTime? Date { get; set; }
    public decimal? IgstPaid { get; set; }
}

public sealed class IcegateUpload
{
    public string Id { get; set; } = "";
    public string FileName { get; set; } = "";
    public DateTime UploadedAtUtc { get; set; }
    public int ShippingBills { get; set; }
    public int Invoices { get; set; }
    public int Replaced { get; set; }
    public DateTime? FirstSbDate { get; set; }
    public DateTime? LastSbDate { get; set; }
    public List<string> CompanyLabels { get; set; } = new();
}

public sealed record IcegateStatusDto(int ShippingBills, int Invoices, IReadOnlyList<IcegateUpload> Uploads);

public sealed record IcegateErpInvoice(string Company, string PrintedNo, string ErpNo, DateTime Date, string PortCode,
    string ShippingBillNo, DateTime? ShippingBillDate, decimal Taxable, decimal Igst, string ExportType);

public sealed record IcegateMatch(IcegateShippingBill Bill, IcegateInvoice? Invoice, string MatchedBy);

public sealed record IcegateUnmatched(IcegateShippingBill Bill, IcegateInvoice? Invoice, string Company);

public sealed record IcegateMatchResult(
    IReadOnlyDictionary<IcegateErpInvoice, IcegateMatch> Matches,
    IReadOnlyDictionary<IcegateShippingBill, (decimal Taxable, int Count)> BillTaxable,
    IReadOnlyList<IcegateUnmatched> Unmatched);
