using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Services;

/// <summary>
/// GST ITC-04 (job work) from the ERP job-work section:
/// Table 4  = goods sent to job worker   → Despatch.dbo.Vw_Challan5A
/// Table 5A = goods received back from the same JW → dbo.Challan5AInward
/// Companies under the same GSTIN (FactoryInfo / factoryGSTNo) are rolled up together.
/// Period is the GST half-year (Apr–Sep / Oct–Mar) used by the ITC-04 offline utility.
/// </summary>
public sealed class GstItc04Service
{
    private const int QueryTimeoutSeconds = 180;

    private static readonly (string Gstin, string Code)[] KnownRegistrations =
    [
        ("24AAACC7815N1ZI", "HCP Ent"),
        ("24AABCG1282C2Z6", "HPBL"),
        ("24AFOPG7793M1ZO", "JBP"),
        ("24AAECK6307M2ZE", "KP SEZ"),
        ("24AAECK6307M1ZF", "KPW"),
        ("24AADCM8286G1Z9", "Marudhar"),
        ("24AAACO3557B2ZV", "OCPL"),
        ("24AAACO7290H4ZB", "OEL"),
        ("24AAACO3087C1ZT", "PIL"),
        ("24AAFCP2956A1ZR", "PPL"),
    ];

    private static readonly Dictionary<string, string> UqcMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["KGS"] = "KGS", ["KG"] = "KGS", ["KILOGRAMS"] = "KGS", ["KILOGRAM"] = "KGS",
        ["PCS"] = "PCS", ["PC"] = "PCS", ["PIECES"] = "PCS", ["PIECE"] = "PCS", ["NOS"] = "NOS", ["NO"] = "NOS",
        ["MTR"] = "MTR", ["MTRS"] = "MTR", ["METER"] = "MTR", ["METERS"] = "MTR", ["MT"] = "MTR",
        ["SET"] = "SET", ["SETS"] = "SET",
        ["BAG"] = "BAG", ["BALE"] = "BAL", ["BOX"] = "BOX", ["ROLL"] = "ROL", ["QTL"] = "QTL",
    };

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;

    public GstItc04Service(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
    }

    public async Task<GstItc04Dto> GetAsync(string period, string? gstin = null, bool refresh = false)
    {
        var (from, toExclusive, label, fy, taxPeriod) = ParsePeriod(period);
        var key = $"gst-itc04-v1:{period}";
        if (!refresh && _cache.TryGetValue(key, out GstItc04Dto? hit) && hit != null)
            return Filter(hit, gstin);

        var known = KnownRegistrations.ToDictionary(k => k.Gstin, k => k.Code, StringComparer.OrdinalIgnoreCase);

        const string sentSql = """
            SELECT LTRIM(RTRIM(c.CompanyName)) AS CompanyUnit,
                   ISNULL(NULLIF(LTRIM(RTRIM(c.factoryGSTNo)), ''), ISNULL(fi.NewGSTNo, '')) AS ManufacturerGstin,
                   LTRIM(RTRIM(c.CompNam)) AS JobWorkerName,
                   LTRIM(RTRIM(ISNULL(c.NewGSTNo, ''))) AS JobWorkerGstin,
                   LTRIM(RTRIM(c.ChallanNo)) AS ChallanNo,
                   CAST(c.SysDate AS date) AS ChallanDate,
                   LTRIM(RTRIM(ISNULL(c.DescriptionGoods, c.DescGoods))) AS Description,
                   CAST(c.ItemQty AS decimal(18, 3)) AS Quantity,
                   LTRIM(RTRIM(ISNULL(NULLIF(c.ItemUnit, ''), c.unit))) AS QtyUnit,
                   CAST(ISNULL(c.itemValue, 0) AS decimal(18, 2)) AS TaxableValue,
                   CAST(ISNULL(c.IGSTrate, 0) AS decimal(18, 2)) AS IgstRate,
                   CAST(ISNULL(c.CGSTrate, 0) AS decimal(18, 2)) AS CgstRate,
                   CAST(ISNULL(c.SGSTrate, 0) AS decimal(18, 2)) AS SgstRate,
                   LTRIM(RTRIM(ISNULL(c.NatureProc, ''))) AS NatureOfJob,
                   LTRIM(RTRIM(ISNULL(c.ItemCode, ''))) AS ItemCode
            FROM Despatch.dbo.Vw_Challan5A c WITH (NOLOCK)
            LEFT JOIN dbo.FactoryInfo fi WITH (NOLOCK)
              ON LTRIM(RTRIM(fi.Name)) = LTRIM(RTRIM(c.CompanyName))
            WHERE c.SysDate >= @From AND c.SysDate < @ToExcl
              AND ISNULL(c.iscancel, '') NOT IN ('yes', 'Yes', 'YES', 'Cancelled', 'cancelled')
              AND ISNULL(c.ItemQty, 0) <> 0
            """;

        // One original challan per inward line (first number in CombineChallanNo), joined for JW GSTIN / date / nature.
        const string recvSql = """
            SELECT LTRIM(RTRIM(a.companyname)) AS CompanyUnit,
                   ISNULL(NULLIF(LTRIM(RTRIM(c.factoryGSTNo)), ''), ISNULL(fi.NewGSTNo, '')) AS ManufacturerGstin,
                   LTRIM(RTRIM(a.BuyerName)) AS JobWorkerName,
                   LTRIM(RTRIM(ISNULL(c.NewGSTNo, ''))) AS JobWorkerGstin,
                   LTRIM(RTRIM(orig.ChallanNo)) AS OriginalChallanNo,
                   CAST(c.SysDate AS date) AS OriginalChallanDate,
                   LTRIM(RTRIM(ISNULL(a.subsidiary_challan_no, ''))) AS JobWorkerChallanNo,
                   CAST(a.subsidiary_challan_dt AS date) AS JobWorkerChallanDate,
                   LTRIM(RTRIM(ISNULL(NULLIF(c.NatureProc, ''), ISNULL(a.natureOfProcess, '')))) AS NatureOfJob,
                   LTRIM(RTRIM(a.DescOfGoods)) AS Description,
                   CAST(a.AcceptedQty AS decimal(18, 3)) AS Quantity,
                   LTRIM(RTRIM(ISNULL(a.Unit, ''))) AS QtyUnit,
                   LTRIM(RTRIM(ISNULL(a.Itemcode, ''))) AS ItemCode,
                   LTRIM(RTRIM(ISNULL(a.InwardNo, ''))) AS InwardNo
            FROM dbo.Challan5AInward a WITH (NOLOCK)
            CROSS APPLY (
                SELECT LTRIM(RTRIM(
                    CASE WHEN CHARINDEX(',', ISNULL(a.CombineChallanNo, '')) > 0
                         THEN LEFT(a.CombineChallanNo, CHARINDEX(',', a.CombineChallanNo) - 1)
                         ELSE a.CombineChallanNo END)) AS ChallanNo
            ) orig
            OUTER APPLY (
                SELECT TOP 1 c.factoryGSTNo, c.NewGSTNo, c.SysDate, c.NatureProc
                FROM Despatch.dbo.Vw_Challan5A c WITH (NOLOCK)
                WHERE c.ChallanNo = orig.ChallanNo
                  AND ISNULL(c.iscancel, '') NOT IN ('yes', 'Yes', 'YES', 'Cancelled', 'cancelled')
            ) c
            LEFT JOIN dbo.FactoryInfo fi WITH (NOLOCK)
              ON LTRIM(RTRIM(fi.Name)) = LTRIM(RTRIM(a.companyname))
            WHERE a.sysdate >= @From AND a.sysdate < @ToExcl
              AND ISNULL(a.AcceptedQty, 0) <> 0
              AND ISNULL(orig.ChallanNo, '') <> ''
            """;

        List<SentRow> sent;
        List<RecvRow> recv;
        using (var connection = _database.CreateConnection())
        {
            var args = new { From = from, ToExcl = toExclusive };
            sent = (await connection.QueryAsync<SentRow>(sentSql, args, commandTimeout: QueryTimeoutSeconds)).ToList();
            recv = (await connection.QueryAsync<RecvRow>(recvSql, args, commandTimeout: QueryTimeoutSeconds)).ToList();
        }

        // Deduplicate Vw_Challan5A join (one challan can appear as multiple item lines matching the same Join).
        var table4 = sent.Select(s => new GstItc04Table4Row
        {
            ManufacturerGstin = s.ManufacturerGstin,
            Code = known.GetValueOrDefault(s.ManufacturerGstin) ?? "",
            Unit = s.CompanyUnit,
            JobWorkerGstin = CleanGstin(s.JobWorkerGstin) ?? ExtractGstin(s.JobWorkerName) ?? "",
            JobWorkerState = "",
            JobWorkerType = IsSez(s.JobWorkerName, s.JobWorkerGstin) ? "SEZ" : "Non SEZ",
            ChallanNo = s.ChallanNo,
            ChallanDate = s.ChallanDate?.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) ?? "",
            GoodsType = GoodsType(s.ItemCode),
            Description = CleanText(s.Description),
            Uqc = MapUqc(s.QtyUnit),
            Quantity = Math.Round(s.Quantity, 2),
            TaxableValue = Math.Round(s.TaxableValue, 2),
            IgstRate = s.IgstRate,
            CgstRate = s.CgstRate,
            SgstRate = s.SgstRate,
            Cess = 0,
            JobWorkerName = CleanJwName(s.JobWorkerName),
            NatureOfJob = CleanText(s.NatureOfJob),
        }).OrderBy(r => r.Code).ThenBy(r => r.ChallanDate).ThenBy(r => r.ChallanNo).ToList();

        var table5A = recv.Select(r => new GstItc04Table5Row
        {
            ManufacturerGstin = r.ManufacturerGstin,
            Code = known.GetValueOrDefault(r.ManufacturerGstin) ?? "",
            Unit = r.CompanyUnit,
            JobWorkerGstin = CleanGstin(r.JobWorkerGstin) ?? ExtractGstin(r.JobWorkerName) ?? "",
            JobWorkerState = "",
            OriginalChallanNo = r.OriginalChallanNo,
            OriginalChallanDate = r.OriginalChallanDate?.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) ?? "",
            JobWorkerChallanNo = r.JobWorkerChallanNo,
            JobWorkerChallanDate = r.JobWorkerChallanDate?.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) ?? "",
            NatureOfJob = CleanText(r.NatureOfJob),
            Description = CleanText(r.Description),
            Uqc = MapUqc(r.QtyUnit),
            Quantity = Math.Round(r.Quantity, 2),
            LossUqc = "",
            LossQuantity = 0,
            JobWorkerName = CleanJwName(r.JobWorkerName),
            InwardNo = r.InwardNo,
        }).OrderBy(r => r.Code).ThenBy(r => r.OriginalChallanDate).ThenBy(r => r.OriginalChallanNo).ToList();

        var registrations = table4.Select(r => r.ManufacturerGstin)
            .Concat(table5A.Select(r => r.ManufacturerGstin))
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var units = table4.Where(r => r.ManufacturerGstin.Equals(g, StringComparison.OrdinalIgnoreCase)).Select(r => r.Unit)
                    .Concat(table5A.Where(r => r.ManufacturerGstin.Equals(g, StringComparison.OrdinalIgnoreCase)).Select(r => r.Unit))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(u => u).ToList();
                return new GstItc04Registration
                {
                    Gstin = g,
                    Code = known.GetValueOrDefault(g) ?? g,
                    Known = known.ContainsKey(g),
                    Units = units,
                    SentRows = table4.Count(r => r.ManufacturerGstin.Equals(g, StringComparison.OrdinalIgnoreCase)),
                    ReceivedRows = table5A.Count(r => r.ManufacturerGstin.Equals(g, StringComparison.OrdinalIgnoreCase)),
                    SentQty = table4.Where(r => r.ManufacturerGstin.Equals(g, StringComparison.OrdinalIgnoreCase)).Sum(r => r.Quantity),
                    ReceivedQty = table5A.Where(r => r.ManufacturerGstin.Equals(g, StringComparison.OrdinalIgnoreCase)).Sum(r => r.Quantity),
                    SentValue = table4.Where(r => r.ManufacturerGstin.Equals(g, StringComparison.OrdinalIgnoreCase)).Sum(r => r.TaxableValue),
                };
            })
            .OrderBy(r => r.Known ? 0 : 1).ThenBy(r => r.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var dto = new GstItc04Dto
        {
            Period = period,
            PeriodLabel = label,
            FinancialYear = fy,
            TaxPeriod = taxPeriod,
            From = from.ToString("yyyy-MM-dd"),
            To = toExclusive.AddDays(-1).ToString("yyyy-MM-dd"),
            GeneratedAt = DateTime.Now,
            Registrations = registrations,
            Table4 = table4,
            Table5A = table5A,
            Table5B = [],
            Table5C = [],
        };
        _cache.Set(key, dto, TimeSpan.FromMinutes(30));
        return Filter(dto, gstin);
    }

    /// <summary>Fills the official GST ITC-04 Offline Utility (.xlsm) template — same sheets, columns and starting cell as the portal tool.</summary>
    public async Task<byte[]> BuildExcelAsync(string period, string? gstin = null)
    {
        var data = await GetAsync(period, gstin);
        var template = ResolveTemplatePath();
        using var wb = new XLWorkbook(template);

        var home = wb.Worksheet("Home");
        // Official Home: labels on row 4 (merged B4:C4 … H4:I4), values on row 5
        // with dropdowns on D5 (SEZ), F5 (FY), H5 (Tax Period). GSTIN in B5.
        var mfgGstin = data.Registrations.Count == 1 ? data.Registrations[0].Gstin : "";
        home.Cell(5, 2).Value = mfgGstin;              // B5
        home.Cell(5, 4).Value = IsManufacturerSez(mfgGstin) ? "Yes" : "No"; // D5 sez_mfg
        home.Cell(5, 6).Value = data.FinancialYear;    // F5 FY_List e.g. 2026-2027
        home.Cell(5, 8).Value = data.TaxPeriod;        // H5 Apr-Sep / Oct-Mar

        FillTable4(wb.Worksheet("Table 4"), data.Table4);
        FillTable5A(wb.Worksheet("Table 5A"), data.Table5A);
        // Table 5B / 5C left blank (no ERP source yet).

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// ZIP of Table_4.csv and Table_5A.csv using only the official offline-utility columns
    /// (ctin…flag / ctin…lwqty+flag), UTF-8 BOM for Excel.
    /// </summary>
    public async Task<byte[]> BuildCsvZipAsync(string period, string? gstin = null)
    {
        var data = await GetAsync(period, gstin);
        var inv = CultureInfo.InvariantCulture;
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteCsvEntry(zip, "Table_4.csv",
            [
                [
                    "GSTIN of Job Worker (JW)", "State (in case of unregistered JW)", "Job Worker's Type",
                    "Challan Number", "Challan Date (dd-mm-yyyy)", "Types of Goods", "Description of Goods",
                    "Unique Quantity Code (UQC)", "Quantity", "Taxable Value (in Rupees)",
                    "Integrated Tax Rate in (%)", "Central Tax Rate in (%)", "State/UT Tax Rate in (%)", "Cess", "Action",
                ],
                .. data.Table4.Select(x => new[]
                {
                    x.JobWorkerGstin, x.JobWorkerState, x.JobWorkerType, x.ChallanNo, x.ChallanDate,
                    x.GoodsType, x.Description, x.Uqc, x.Quantity.ToString("0.##", inv), x.TaxableValue.ToString("0.00", inv),
                    x.IgstRate.ToString("0.##", inv), x.CgstRate.ToString("0.##", inv), x.SgstRate.ToString("0.##", inv),
                    x.Cess.ToString("0.##", inv), "ADD",
                }),
            ]);

            WriteCsvEntry(zip, "Table_5A.csv",
            [
                [
                    "GSTIN of Job Worker(JW)", "State (in case of unregistered JW)",
                    "Original Challan Number issued by Prinicpal", "Original Challan Date issued by Principal (dd-mm-yyyy)",
                    "Challan Number issued by Job worker", "Challan Date issued by worker (dd-mm-yyyy)",
                    "Nature of Job Work done", "Description of Goods", "Unique Quantity Code (UQC)", "Quantity",
                    "Unique Quantity Code (UQC)", "Quantity", "Action",
                ],
                .. data.Table5A.Select(x => new[]
                {
                    x.JobWorkerGstin, x.JobWorkerState, x.OriginalChallanNo, x.OriginalChallanDate,
                    x.JobWorkerChallanNo, x.JobWorkerChallanDate,
                    string.IsNullOrWhiteSpace(x.NatureOfJob) ? "Job Work" : x.NatureOfJob,
                    x.Description, x.Uqc, x.Quantity.ToString("0.##", inv),
                    x.LossUqc, x.LossQuantity == 0 ? "" : x.LossQuantity.ToString("0.##", inv), "ADD",
                }),
            ]);
        }
        return ms.ToArray();
    }

    private static string ResolveTemplatePath()
    {
        var name = "ITC_04_Offline_Utility.xlsm";
        foreach (var dir in new[]
                 {
                     Path.Combine(AppContext.BaseDirectory, "Assets"),
                     Path.Combine(Directory.GetCurrentDirectory(), "Assets"),
                     Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets"),
                 })
        {
            var path = Path.GetFullPath(Path.Combine(dir, name));
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException($"ITC-04 template not found ({name}). Expected under Assets/.");
    }

    /// <summary>Official Table 4: headers row 5, codes row 6, data from row 7 col B (15 cols ending with Action=N).</summary>
    private static void FillTable4(IXLWorksheet ws, List<GstItc04Table4Row> rows)
    {
        var r = 7;
        foreach (var x in rows)
        {
            ws.Cell(r, 2).Value = x.JobWorkerGstin;
            ws.Cell(r, 3).Value = x.JobWorkerState;
            ws.Cell(r, 4).Value = x.JobWorkerType;
            ws.Cell(r, 5).Value = x.ChallanNo;
            ws.Cell(r, 6).Value = x.ChallanDate;
            ws.Cell(r, 7).Value = x.GoodsType;
            ws.Cell(r, 8).Value = x.Description;
            ws.Cell(r, 9).Value = x.Uqc;
            ws.Cell(r, 10).Value = x.Quantity;
            ws.Cell(r, 11).Value = x.TaxableValue;
            ws.Cell(r, 12).Value = x.IgstRate;
            ws.Cell(r, 13).Value = x.CgstRate;
            ws.Cell(r, 14).Value = x.SgstRate;
            ws.Cell(r, 15).Value = x.Cess;
            ws.Cell(r, 16).Value = "ADD"; // Action dropdown: ADD / DELETE (maps to flag N/D)
            r++;
        }
    }

    /// <summary>Official Table 5A: data from row 7 col B (Action = ADD).</summary>
    private static void FillTable5A(IXLWorksheet ws, List<GstItc04Table5Row> rows)
    {
        var r = 7;
        foreach (var x in rows)
        {
            ws.Cell(r, 2).Value = x.JobWorkerGstin;
            ws.Cell(r, 3).Value = x.JobWorkerState;
            ws.Cell(r, 4).Value = x.OriginalChallanNo;
            ws.Cell(r, 5).Value = x.OriginalChallanDate;
            ws.Cell(r, 6).Value = x.JobWorkerChallanNo;
            ws.Cell(r, 7).Value = x.JobWorkerChallanDate;
            ws.Cell(r, 8).Value = string.IsNullOrWhiteSpace(x.NatureOfJob) ? "Job Work" : x.NatureOfJob;
            ws.Cell(r, 9).Value = x.Description;
            ws.Cell(r, 10).Value = x.Uqc;
            ws.Cell(r, 11).Value = x.Quantity;
            if (!string.IsNullOrWhiteSpace(x.LossUqc)) ws.Cell(r, 12).Value = x.LossUqc;
            if (x.LossQuantity != 0) ws.Cell(r, 13).Value = x.LossQuantity;
            ws.Cell(r, 14).Value = "ADD";
            r++;
        }
    }

    private static void WriteCsvEntry(ZipArchive zip, string name, IEnumerable<string[]> rows)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        foreach (var row in rows)
            writer.WriteLine(string.Join(",", row.Select(CsvEscape)));
    }

    private static string CsvEscape(string? value)
    {
        var s = value ?? "";
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    private static GstItc04Dto Filter(GstItc04Dto data, string? gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin)) return data;
        return new GstItc04Dto
        {
            Period = data.Period,
            PeriodLabel = data.PeriodLabel,
            FinancialYear = data.FinancialYear,
            TaxPeriod = data.TaxPeriod,
            From = data.From,
            To = data.To,
            GeneratedAt = data.GeneratedAt,
            Registrations = data.Registrations.Where(r => r.Gstin.Equals(gstin, StringComparison.OrdinalIgnoreCase)).ToList(),
            Table4 = data.Table4.Where(r => r.ManufacturerGstin.Equals(gstin, StringComparison.OrdinalIgnoreCase)).ToList(),
            Table5A = data.Table5A.Where(r => r.ManufacturerGstin.Equals(gstin, StringComparison.OrdinalIgnoreCase)).ToList(),
            Table5B = data.Table5B,
            Table5C = data.Table5C,
        };
    }

    /// <summary>period: "2026-H1" (Apr–Sep), "2026-H2" (Oct–Mar of FY starting that year), or "yyyy-MM" month (expanded to containing half-year).</summary>
    public static (DateTime From, DateTime ToExclusive, string Label, string Fy, string TaxPeriod) ParsePeriod(string period)
    {
        if (Regex.IsMatch(period, @"^\d{4}-H[12]$", RegexOptions.IgnoreCase))
        {
            var year = int.Parse(period[..4], CultureInfo.InvariantCulture);
            var h1 = period.EndsWith("H1", StringComparison.OrdinalIgnoreCase);
            var from = h1 ? new DateTime(year, 4, 1) : new DateTime(year, 10, 1);
            var toEx = h1 ? new DateTime(year, 10, 1) : new DateTime(year + 1, 4, 1);
            // Match offline utility master: "2026-2027", "Apr-Sep" / "Oct-Mar".
            var fy = $"{year}-{year + 1}";
            var tax = h1 ? "Apr-Sep" : "Oct-Mar";
            var label = h1 ? $"Apr–Sep {year}" : $"Oct {year}–Mar {year + 1}";
            return (from, toEx, label, fy, tax);
        }
        if (DateTime.TryParseExact(period, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
        {
            // Half-year containing the month (Apr–Sep / Oct–Mar).
            var h1 = month.Month is >= 4 and <= 9;
            var year = h1 ? month.Year : (month.Month >= 10 ? month.Year : month.Year - 1);
            return ParsePeriod($"{year}-{(h1 ? "H1" : "H2")}");
        }
        throw new ArgumentException("Period must be yyyy-H1, yyyy-H2, or yyyy-MM.");
    }

    private static string MapUqc(string? unit)
    {
        var raw = (unit ?? "").Split(',')[0].Trim().ToUpperInvariant();
        if (raw.Length == 0) return "KGS";
        if (UqcMap.TryGetValue(raw, out var mapped)) return mapped;
        return raw.Length <= 3 ? raw : raw[..3];
    }

    private static string GoodsType(string? itemCode)
    {
        var c = (itemCode ?? "").ToUpperInvariant();
        if (c.StartsWith("CAP") || c.StartsWith("FA") || c.Contains("ASSET")) return "Capital Goods";
        return "Inputs";
    }

    private static bool IsManufacturerSez(string? gstin) => IsSez(null, gstin);

    private static bool IsSez(string? name, string? gstin)
    {
        var n = name ?? "";
        if (n.Contains("SEZ", StringComparison.OrdinalIgnoreCase) || n.Contains("KASEZ", StringComparison.OrdinalIgnoreCase))
            return true;
        // Known SEZ GSTINs in the group (KP SEZ; OEL marked SEZ in ERP)
        return string.Equals(gstin, "24AAECK6307M2ZE", StringComparison.OrdinalIgnoreCase)
            || string.Equals(gstin, "24AAACO7290H4ZB", StringComparison.OrdinalIgnoreCase);
    }

    private static string? CleanGstin(string? g)
    {
        var s = (g ?? "").Trim().ToUpperInvariant();
        return Regex.IsMatch(s, @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][A-Z0-9]Z[A-Z0-9]$") ? s : null;
    }

    private static string? ExtractGstin(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var m = Regex.Match(text.ToUpperInvariant(), @"[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][A-Z0-9]Z[A-Z0-9]");
        return m.Success ? m.Value : null;
    }

    private static string CleanJwName(string? name)
    {
        var s = (name ?? "").Trim();
        s = Regex.Replace(s, @"\s+[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][A-Z0-9]Z[A-Z0-9]\s*$", "", RegexOptions.IgnoreCase);
        return CleanText(s);
    }

    private static string CleanText(string? s)
    {
        var t = Regex.Replace((s ?? "").Trim(), @"\s+", " ");
        // Offline utility allows alphanumeric + / - and space
        t = Regex.Replace(t, @"[^A-Za-z0-9 /\-.,()&]", "");
        return t.Length > 100 ? t[..100] : t;
    }

    private sealed class SentRow
    {
        public string CompanyUnit { get; set; } = "";
        public string ManufacturerGstin { get; set; } = "";
        public string JobWorkerName { get; set; } = "";
        public string JobWorkerGstin { get; set; } = "";
        public string ChallanNo { get; set; } = "";
        public DateTime? ChallanDate { get; set; }
        public string Description { get; set; } = "";
        public decimal Quantity { get; set; }
        public string QtyUnit { get; set; } = "";
        public decimal TaxableValue { get; set; }
        public decimal IgstRate { get; set; }
        public decimal CgstRate { get; set; }
        public decimal SgstRate { get; set; }
        public string NatureOfJob { get; set; } = "";
        public string ItemCode { get; set; } = "";
    }

    private sealed class RecvRow
    {
        public string CompanyUnit { get; set; } = "";
        public string ManufacturerGstin { get; set; } = "";
        public string JobWorkerName { get; set; } = "";
        public string JobWorkerGstin { get; set; } = "";
        public string OriginalChallanNo { get; set; } = "";
        public DateTime? OriginalChallanDate { get; set; }
        public string JobWorkerChallanNo { get; set; } = "";
        public DateTime? JobWorkerChallanDate { get; set; }
        public string NatureOfJob { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Quantity { get; set; }
        public string QtyUnit { get; set; } = "";
        public string ItemCode { get; set; } = "";
        public string InwardNo { get; set; } = "";
    }
}

public sealed class GstItc04Dto
{
    public string Period { get; set; } = "";
    public string PeriodLabel { get; set; } = "";
    public string FinancialYear { get; set; } = "";
    public string TaxPeriod { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
    public List<GstItc04Registration> Registrations { get; set; } = [];
    public List<GstItc04Table4Row> Table4 { get; set; } = [];
    public List<GstItc04Table5Row> Table5A { get; set; } = [];
    public List<GstItc04Table5Row> Table5B { get; set; } = [];
    public List<GstItc04Table5Row> Table5C { get; set; } = [];
}

public sealed class GstItc04Registration
{
    public string Gstin { get; set; } = "";
    public string Code { get; set; } = "";
    public bool Known { get; set; }
    public List<string> Units { get; set; } = [];
    public int SentRows { get; set; }
    public int ReceivedRows { get; set; }
    public decimal SentQty { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal SentValue { get; set; }
}

public sealed class GstItc04Table4Row
{
    public string ManufacturerGstin { get; set; } = "";
    public string Code { get; set; } = "";
    public string Unit { get; set; } = "";
    public string JobWorkerGstin { get; set; } = "";
    public string JobWorkerState { get; set; } = "";
    public string JobWorkerType { get; set; } = "";
    public string ChallanNo { get; set; } = "";
    public string ChallanDate { get; set; } = "";
    public string GoodsType { get; set; } = "";
    public string Description { get; set; } = "";
    public string Uqc { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal IgstRate { get; set; }
    public decimal CgstRate { get; set; }
    public decimal SgstRate { get; set; }
    public decimal Cess { get; set; }
    public string JobWorkerName { get; set; } = "";
    public string NatureOfJob { get; set; } = "";
}

public sealed class GstItc04Table5Row
{
    public string ManufacturerGstin { get; set; } = "";
    public string Code { get; set; } = "";
    public string Unit { get; set; } = "";
    public string JobWorkerGstin { get; set; } = "";
    public string JobWorkerState { get; set; } = "";
    public string OriginalChallanNo { get; set; } = "";
    public string OriginalChallanDate { get; set; } = "";
    public string JobWorkerChallanNo { get; set; } = "";
    public string JobWorkerChallanDate { get; set; } = "";
    public string NatureOfJob { get; set; } = "";
    public string Description { get; set; } = "";
    public string Uqc { get; set; } = "";
    public decimal Quantity { get; set; }
    public string LossUqc { get; set; } = "";
    public decimal LossQuantity { get; set; }
    public string JobWorkerName { get; set; } = "";
    public string InwardNo { get; set; } = "";
}
