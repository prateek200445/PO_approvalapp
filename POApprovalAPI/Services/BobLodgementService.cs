using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Dapper;
using iTextSharp.text.pdf;

namespace POApprovalAPI.Services;

/// <summary>
/// BOB export-bill lodgement: upload Container Dispatch / finance Excel, map rows into
/// the PIL or KPW fillable PDF, allow manual edits, download.
/// </summary>
public sealed class BobLodgementService
{
    private const int Timeout = 120;
    private readonly DatabaseService _database;
    private readonly IWebHostEnvironment _env;

    public BobLodgementService(DatabaseService database, IWebHostEnvironment env)
    {
        _database = database;
        _env = env;
    }

    public IReadOnlyList<BobLodgementCompanyInfo> ListCompanies() =>
        Profiles.Select(p => new BobLodgementCompanyInfo(p.Id, p.Title, p.ShortLabel)).ToList();

    public BobLodgementExcelParseResult ParseExcel(Stream stream, string? fileName = null)
    {
        using var book = new XLWorkbook(stream);
        var sheet = book.Worksheets.FirstOrDefault(w => w.RowsUsed().Any())
            ?? throw new ArgumentException("Excel has no used worksheets.");

        var used = sheet.RangeUsed()
            ?? throw new ArgumentException("Excel sheet is empty.");
        var firstRow = used.FirstRow().RowNumber();
        var lastRow = used.LastRow().RowNumber();
        var firstCol = used.FirstColumn().ColumnNumber();
        var lastCol = used.LastColumn().ColumnNumber();

        var headerRow = firstRow;
        Dictionary<string, int>? map = null;
        for (var r = firstRow; r <= Math.Min(firstRow + 20, lastRow); r++)
        {
            var trial = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var c = firstCol; c <= lastCol; c++)
            {
                var header = CellText(sheet.Cell(r, c));
                if (string.IsNullOrWhiteSpace(header)) continue;
                var key = MatchHeader(header);
                if (key != null && !trial.ContainsKey(key))
                    trial[key] = c;
            }
            // Need at least a few lodgement-useful columns
            var score = new[] { "invoiceNo", "buyerName", "shippingBillNo", "amount", "currency", "blNo", "companyName" }
                .Count(k => trial.ContainsKey(k));
            if (score >= 2)
            {
                headerRow = r;
                map = trial;
                break;
            }
        }

        if (map == null || map.Count == 0)
            throw new ArgumentException(
                "Could not detect header row. Include columns like Invoice No, Buyer, Shipping Bill, Amount, Currency, B/L, Port.");

        var rows = new List<BobLodgementExcelRow>();
        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            string Get(string key) => map.TryGetValue(key, out var col) ? CellText(sheet.Cell(r, col)) : "";
            var invoiceNo = Get("invoiceNo");
            var buyer = Get("buyerName");
            var sb = Get("shippingBillNo");
            var amountRaw = Get("amount");
            if (string.IsNullOrWhiteSpace(invoiceNo) && string.IsNullOrWhiteSpace(buyer) &&
                string.IsNullOrWhiteSpace(sb) && string.IsNullOrWhiteSpace(amountRaw))
                continue;

            var amount = ParseAmount(amountRaw);

            rows.Add(new BobLodgementExcelRow
            {
                RowNumber = r,
                CompanyName = Get("companyName"),
                InvoiceNo = invoiceNo,
                InvoiceDate = NormalizeDateText(Get("invoiceDate")),
                BuyerName = buyer,
                BuyerAddress = Get("buyerAddress"),
                BuyerCountry = Get("buyerCountry"),
                Currency = NormalizeCurrency(Get("currency")),
                Amount = amount,
                AmountWords = Get("amountWords"),
                ShippingBillNo = sb,
                ShippingBillDate = NormalizeDateText(Get("shippingBillDate")),
                BlNo = Get("blNo"),
                BlDate = NormalizeDateText(Get("blDate")),
                PortOfLoading = Prefer(Get("portOfLoading"), PortName(Get("portCode"))),
                PortOfDestination = Get("portOfDestination"),
                PortCode = Get("portCode"),
                HsCode = Get("hsCode"),
                Goods = Get("goods"),
                TermsOfPayment = Get("termsOfPayment"),
                UsanceDays = Get("usanceDays"),
            });
        }

        return new BobLodgementExcelParseResult
        {
            FileName = fileName ?? "",
            SheetName = sheet.Name,
            HeaderRow = headerRow,
            MappedColumns = map.ToDictionary(kv => kv.Key, kv => sheet.Cell(headerRow, kv.Value).GetString().Trim()),
            Rows = rows,
        };
    }

    public BobLodgementDraft BuildDraftFromExcel(string companyId, BobLodgementExcelRow row)
    {
        var profile = RequireProfile(companyId);
        var terms = row.TermsOfPayment ?? "";
        var (usanceDays, termsFrom) = string.IsNullOrWhiteSpace(row.UsanceDays)
            ? ParseUsance(terms)
            : (row.UsanceDays.Trim(), string.IsNullOrWhiteSpace(terms) ? "BILL OF LADING" : ParseUsance(terms).From);
        var currency = NormalizeCurrency(row.Currency);
        var amount = row.Amount;
        var amountWords = string.IsNullOrWhiteSpace(row.AmountWords)
            ? AmountInWords(amount, currency)
            : row.AmountWords.Trim();
        var today = DateTime.Today;
        var buyerBlock = Prefer(
            string.Join("\r", new[] { row.BuyerName, row.BuyerAddress, row.BuyerCountry }
                .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim())),
            row.BuyerName);

        var fields = new List<BobLodgementField>
        {
            F("branchName", "Branch name", profile.BranchName, sort: 1),
            F("applicationDate", "Application date", today.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture), sort: 2),
            F("requestType", "Request type", profile.DefaultRequestType, sort: 3),
            F("underLc", "Under letter of credit", "No", sort: 4),
            F("currency", "Bill currency", currency, sort: 10),
            F("amountFigure", "Bill amount (figure)", FormatAmount(amount), sort: 11),
            F("amountWords", "Bill amount (words)", amountWords, sort: 12),
            F("usanceDays", "Usance days", usanceDays, sort: 13),
            F("termsFrom", "Usance from", termsFrom, sort: 14),
            F("termsOfPayment", "Terms of payment", terms, sort: 15),
            F("goods", "Exported goods", Prefer(row.Goods, "BAGS"), sort: 20),
            F("hsCode", "HS code", Prefer(row.HsCode, profile.DefaultHsCode), sort: 21),
            F("portOfLoading", "Port of loading", Prefer(row.PortOfLoading, "MUNDRA"), sort: 22),
            F("portOfDestination", "Port of destination", Prefer(row.PortOfDestination), sort: 23),
            F("blNo", "B/L / FCR number", Prefer(row.BlNo), sort: 24),
            F("shippingBillNo", "Shipping bill / EDF no.", Prefer(row.ShippingBillNo), sort: 25),
            F("shippingBillDate", "Shipping bill date", FormatDisplayDate(row.ShippingBillDate), sort: 26),
            F("countryOfOrigin", "Country of origin", "INDIA", sort: 27),
            F("portCode", "Port code", Prefer(row.PortCode), sort: 28),
            F("exporterNameAddress", "Exporter name & address", profile.ExporterAddress, multiline: true, sort: 30),
            F("exporterPerson", "Exporter contact person", profile.ContactPerson, sort: 31),
            F("exporterEmail", "Exporter email", profile.Email, sort: 32),
            F("exporterPhone", "Exporter phone", profile.Phone, sort: 33),
            F("buyerNameAddress", "Buyer name & address", buyerBlock, multiline: true, sort: 40),
            F("buyerCountry", "Buyer country", Prefer(row.BuyerCountry, GuessCountry(row.PortOfDestination)), sort: 41),
            F("buyerBankNameAddress", "Buyer bank name & address", "", multiline: true, sort: 42),
            F("buyerBankSwift", "Buyer bank SWIFT", "", sort: 43),
            F("creditAccountNo", "Credit proceeds A/c no.", profile.AccountNo, sort: 50),
            F("chargesAccountNo", "Debit charges A/c no.", profile.AccountNo, sort: 51),
            F("invoiceNo", "Export invoice no.", Prefer(row.InvoiceNo), sort: 60),
            F("invoiceDate", "Export invoice date", FormatDisplayDate(row.InvoiceDate), sort: 61),
            F("companyName", "Company (from sheet)", Prefer(row.CompanyName, profile.Title), sort: 62),
            F("specialInstructions", "Special instructions", "", multiline: true, sort: 70),
        };

        return new BobLodgementDraft
        {
            CompanyId = profile.Id,
            CompanyTitle = profile.Title,
            InvoiceNo = row.InvoiceNo ?? "",
            ShippingBillNo = row.ShippingBillNo ?? "",
            SourceSummary = $"{Prefer(row.BuyerName, "—")} · {currency} {FormatAmount(amount)} · SB {Prefer(row.ShippingBillNo, "—")} · row {row.RowNumber}",
            Fields = fields.OrderBy(f => f.Sort).ToList(),
        };
    }

    public async Task<IReadOnlyList<BobLodgementShipmentHit>> SearchAsync(
        string companyId, string? q, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var profile = RequireProfile(companyId);
        var like = string.IsNullOrWhiteSpace(q) ? null : "%" + q.Trim() + "%";
        const string sql = """
            SELECT TOP 80
                LTRIM(RTRIM(ISNULL(v.Invoice_no, ''))) AS InvoiceNo,
                CONVERT(varchar(10), v.Invoice_dt, 120) AS InvoiceDate,
                LTRIM(RTRIM(ISNULL(v.Buyers, ''))) AS BuyerName,
                LTRIM(RTRIM(ISNULL(v.CompanyName, ''))) AS CompanyName,
                LTRIM(RTRIM(ISNULL(v.currency, ''))) AS Currency,
                CAST(ISNULL(v.CommercialAmount, ISNULL(v.TotalAmt, 0)) AS decimal(18, 2)) AS Amount,
                LTRIM(RTRIM(ISNULL(v.ShippingBillNo, ''))) AS ShippingBillNo,
                CONVERT(varchar(10), v.ShippingBDTE, 120) AS ShippingBillDate,
                LTRIM(RTRIM(ISNULL(v.PORTCode, ''))) AS PortCode,
                LTRIM(RTRIM(ISNULL(v.port_discharge, ''))) AS PortOfDischarge,
                LTRIM(RTRIM(ISNULL(v.Bill_leading_no, ''))) AS BlNo,
                CONVERT(varchar(10), v.Bill_leading_dt, 120) AS BlDate
            FROM Despatch.dbo.VW_EXPORT_SHIPINGBILLDETAIL v WITH (NOLOCK)
            WHERE LTRIM(RTRIM(ISNULL(v.ShippingBillNo, ''))) <> ''
              AND LTRIM(RTRIM(ISNULL(v.ShippingBillNo, ''))) <> '.'
              AND (@From IS NULL OR v.Invoice_dt >= @From)
              AND (@To IS NULL OR v.Invoice_dt < DATEADD(day, 1, @To))
              AND (
                    LTRIM(RTRIM(v.CompanyName)) LIKE @CompanyLike
                 OR LTRIM(RTRIM(v.CompanyName)) LIKE @CompanyLike2
                  )
              AND (
                    @Like IS NULL
                 OR v.Invoice_no LIKE @Like
                 OR v.ShippingBillNo LIKE @Like
                 OR v.Buyers LIKE @Like
                 OR v.Bill_leading_no LIKE @Like
                  )
            ORDER BY v.Invoice_dt DESC, v.ShippingBillNo DESC
            """;
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<BobLodgementShipmentHit>(
            new CommandDefinition(sql, new
            {
                From = from,
                To = to,
                Like = like,
                CompanyLike = profile.CompanyLike,
                CompanyLike2 = profile.CompanyLike2,
            }, cancellationToken: ct, commandTimeout: Timeout));
        return rows.ToList();
    }

    public async Task<BobLodgementDraft> BuildDraftAsync(
        string companyId, string? invoiceNo, string? shippingBillNo, CancellationToken ct = default)
    {
        var profile = RequireProfile(companyId);
        var erp = await LoadShipmentAsync(profile, invoiceNo, shippingBillNo, ct)
            ?? throw new ArgumentException("No shipping-bill / invoice found in ERP for the selected company.");

        var buyer = await LoadBuyerAsync(erp.BuyerName, ct);
        var packing = await LoadPackingAsync(erp.InvoiceNo, erp.ShippingBillNo, ct);
        var terms = buyer?.TermsOfPayment ?? "";
        var (usanceDays, termsFrom) = ParseUsance(terms);

        var today = DateTime.Today;
        var fields = new List<BobLodgementField>
        {
            F("branchName", "Branch name", profile.BranchName, sort: 1),
            F("applicationDate", "Application date", today.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture), sort: 2),
            F("requestType", "Request type", profile.DefaultRequestType, sort: 3),
            F("underLc", "Under letter of credit", "No", sort: 4),
            F("currency", "Bill currency", NormalizeCurrency(erp.Currency), sort: 10),
            F("amountFigure", "Bill amount (figure)", FormatAmount(erp.Amount), sort: 11),
            F("amountWords", "Bill amount (words)", AmountInWords(erp.Amount, NormalizeCurrency(erp.Currency)), sort: 12),
            F("usanceDays", "Usance days", usanceDays, sort: 13),
            F("termsFrom", "Usance from", termsFrom, sort: 14),
            F("termsOfPayment", "Terms of payment (ERP)", terms, sort: 15),
            F("goods", "Exported goods", "BAGS", sort: 20),
            F("hsCode", "HS code", profile.DefaultHsCode, sort: 21),
            F("portOfLoading", "Port of loading", Prefer(packing?.PortLoading, PortName(erp.PortCode), "MUNDRA"), sort: 22),
            F("portOfDestination", "Port of destination", Prefer(erp.PortOfDischarge, packing?.PortDischarge), sort: 23),
            F("blNo", "B/L / FCR number", Prefer(packing?.BlNo, erp.BlNo), sort: 24),
            F("shippingBillNo", "Shipping bill / EDF no.", erp.ShippingBillNo, sort: 25),
            F("shippingBillDate", "Shipping bill date", FormatDisplayDate(erp.ShippingBillDate), sort: 26),
            F("countryOfOrigin", "Country of origin", "INDIA", sort: 27),
            F("portCode", "Port code", erp.PortCode, sort: 28),
            F("exporterNameAddress", "Exporter name & address", profile.ExporterAddress, multiline: true, sort: 30),
            F("exporterPerson", "Exporter contact person", profile.ContactPerson, sort: 31),
            F("exporterEmail", "Exporter email", profile.Email, sort: 32),
            F("exporterPhone", "Exporter phone", profile.Phone, sort: 33),
            F("buyerNameAddress", "Buyer name & address", Prefer(FormatBuyerAddress(buyer, erp.BuyerName), erp.BuyerName), multiline: true, sort: 40),
            F("buyerCountry", "Buyer country", Prefer(buyer?.Country, GuessCountry(erp.PortOfDischarge)), sort: 41),
            F("buyerBankNameAddress", "Buyer bank name & address", "", multiline: true, sort: 42),
            F("buyerBankSwift", "Buyer bank SWIFT", "", sort: 43),
            F("creditAccountNo", "Credit proceeds A/c no.", profile.AccountNo, sort: 50),
            F("chargesAccountNo", "Debit charges A/c no.", profile.AccountNo, sort: 51),
            F("invoiceNo", "Export invoice no.", erp.InvoiceNo, sort: 60),
            F("invoiceDate", "Export invoice date", FormatDisplayDate(erp.InvoiceDate), sort: 61),
            F("companyName", "ERP company", erp.CompanyName, sort: 62),
            F("specialInstructions", "Special instructions", "", multiline: true, sort: 70),
        };

        return new BobLodgementDraft
        {
            CompanyId = profile.Id,
            CompanyTitle = profile.Title,
            InvoiceNo = erp.InvoiceNo,
            ShippingBillNo = erp.ShippingBillNo,
            SourceSummary = $"{erp.BuyerName} · {NormalizeCurrency(erp.Currency)} {FormatAmount(erp.Amount)} · SB {erp.ShippingBillNo} · {erp.PortOfDischarge}",
            Fields = fields.OrderBy(f => f.Sort).ToList(),
        };
    }

    public byte[] BuildPdf(BobLodgementDownloadRequest request)
    {
        var profile = RequireProfile(request.CompanyId);
        var map = (request.Fields ?? [])
            .Where(f => !string.IsNullOrWhiteSpace(f.Key))
            .ToDictionary(f => f.Key, f => f.Value ?? "", StringComparer.OrdinalIgnoreCase);

        string G(string key) => map.TryGetValue(key, out var v) ? v.Trim() : "";

        var template = Path.Combine(_env.ContentRootPath, "Assets", "BobLodgement", profile.TemplateFile);
        if (!File.Exists(template))
            throw new FileNotFoundException($"Lodgement template missing: {profile.TemplateFile}");

        using var reader = new PdfReader(template);
        using var ms = new MemoryStream();
        using (var stamper = new PdfStamper(reader, ms))
        {
            var form = stamper.AcroFields;
            ClearTickDefaults(form);

            // Request type
            SetTick(form, RequestTypeField(G("requestType"), profile), true);
            SetTick(form, "untitled20", !IsYes(G("underLc"))); // sample PDFs tick "No"
            if (IsYes(G("underLc"))) SetTick(form, "untitled19", true);

            form.SetField("untitled39", G("branchName"));
            form.SetField("untitled40", G("applicationDate"));
            form.SetField("untitled47", G("currency"));
            form.SetField("untitled48", G("amountWords"));
            form.SetField("untitled49", G("amountFigure"));
            form.SetField("untitled50", G("usanceDays"));
            form.SetField("untitled51", G("termsFrom"));
            form.SetField("untitled52", G("goods"));
            form.SetField("untitled53", G("hsCode"));
            form.SetField("untitled54", G("portOfLoading"));
            form.SetField("untitled55", G("portOfDestination"));
            form.SetField("untitled56", G("blNo"));
            form.SetField("untitled57", G("shippingBillNo"));
            form.SetField("untitled58", G("shippingBillDate"));
            form.SetField("untitled61", G("exporterNameAddress"));
            form.SetField("untitled62", G("buyerNameAddress"));
            form.SetField("untitled63", G("exporterPerson"));
            form.SetField("untitled64", G("buyerCountry"));
            form.SetField("untitled65", G("exporterPhone"));
            form.SetField("untitled67", G("exporterEmail"));
            form.SetField("untitled41", G("buyerBankNameAddress"));
            form.SetField("untitled42", G("buyerBankSwift"));
            form.SetField("untitled96", G("creditAccountNo"));
            form.SetField("untitled97", G("chargesAccountNo"));
            form.SetField("untitled95", G("specialInstructions"));

            // Document counts — invoice + transport original (matches filled samples)
            form.SetField("untitled74", "01");
            form.SetField("untitled75", "01");
            form.SetField("untitled78", "01");
            form.SetField("untitled86", "01");

            var compact = CompactDate(G("applicationDate"));
            form.SetField("untitled88", compact);
            form.SetField("untitled114", compact);
            form.SetField("untitled117", compact);

            // Pre-fill authorised signatory image for PIL / KPW in the seal boxes.
            StampCompanySignature(stamper, profile);

            stamper.FormFlattening = false;
            stamper.Close();
        }

        return ms.ToArray();
    }

    private void StampCompanySignature(PdfStamper stamper, CompanyProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.SignatureFile)) return;
        var path = Path.Combine(_env.ContentRootPath, "Assets", "BobLodgement", profile.SignatureFile);
        if (!File.Exists(path)) return;

        iTextSharp.text.Image img;
        try
        {
            img = iTextSharp.text.Image.GetInstance(path);
        }
        catch
        {
            return;
        }

        // PDF coords are bottom-left origin (A4 ≈ 595 x 842).
        // Blank "Authorised Signatory with Company/Firm seal" box sits bottom-right,
        // right of the Date field and above the label text.
        // Page 1 — date untitled88 ~ [45,150]–[211,166]; box to the right.
        StampSignatureOnPage(stamper, img, page: 1, x: 355f, y: 150f, maxWidth: 200f, maxHeight: 45f);
        // Page 2 — date untitled114 ~ [48,132]–[203,154].
        StampSignatureOnPage(stamper, img, page: 2, x: 355f, y: 150f, maxWidth: 200f, maxHeight: 45f);
        // Page 3 — FEMA date untitled117 ~ [48,257]–[208,278].
        StampSignatureOnPage(stamper, img, page: 3, x: 355f, y: 252f, maxWidth: 200f, maxHeight: 50f);
    }

    private static void StampSignatureOnPage(
        PdfStamper stamper, iTextSharp.text.Image source, int page, float x, float y, float maxWidth, float maxHeight)
    {
        if (page < 1 || page > stamper.Reader.NumberOfPages) return;
        var img = iTextSharp.text.Image.GetInstance(source);
        img.ScaleToFit(maxWidth, maxHeight);
        img.SetAbsolutePosition(x, y);
        stamper.GetOverContent(page).AddImage(img);
    }

    private async Task<ErpShipment?> LoadShipmentAsync(
        CompanyProfile profile, string? invoiceNo, string? shippingBillNo, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1
                LTRIM(RTRIM(ISNULL(v.Invoice_no, ''))) AS InvoiceNo,
                CONVERT(varchar(10), v.Invoice_dt, 120) AS InvoiceDate,
                LTRIM(RTRIM(ISNULL(v.Buyers, ''))) AS BuyerName,
                LTRIM(RTRIM(ISNULL(v.CompanyName, ''))) AS CompanyName,
                LTRIM(RTRIM(ISNULL(v.currency, ''))) AS Currency,
                CAST(ISNULL(v.CommercialAmount, ISNULL(v.TotalAmt, 0)) AS decimal(18, 2)) AS Amount,
                LTRIM(RTRIM(ISNULL(v.ShippingBillNo, ''))) AS ShippingBillNo,
                CONVERT(varchar(10), v.ShippingBDTE, 120) AS ShippingBillDate,
                LTRIM(RTRIM(ISNULL(v.PORTCode, ''))) AS PortCode,
                LTRIM(RTRIM(ISNULL(v.port_discharge, ''))) AS PortOfDischarge,
                LTRIM(RTRIM(ISNULL(v.Bill_leading_no, ''))) AS BlNo,
                CONVERT(varchar(10), v.Bill_leading_dt, 120) AS BlDate
            FROM Despatch.dbo.VW_EXPORT_SHIPINGBILLDETAIL v WITH (NOLOCK)
            WHERE (
                    LTRIM(RTRIM(v.CompanyName)) LIKE @CompanyLike
                 OR LTRIM(RTRIM(v.CompanyName)) LIKE @CompanyLike2
                  )
              AND (
                    (@InvoiceNo IS NOT NULL AND LTRIM(RTRIM(v.Invoice_no)) = @InvoiceNo)
                 OR (@ShippingBillNo IS NOT NULL AND LTRIM(RTRIM(v.ShippingBillNo)) = @ShippingBillNo)
                  )
            ORDER BY v.Invoice_dt DESC
            """;
        using var connection = _database.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ErpShipment>(
            new CommandDefinition(sql, new
            {
                InvoiceNo = NullIfEmpty(invoiceNo),
                ShippingBillNo = NullIfEmpty(shippingBillNo),
                CompanyLike = profile.CompanyLike,
                CompanyLike2 = profile.CompanyLike2,
            }, cancellationToken: ct, commandTimeout: Timeout));
    }

    private async Task<BuyerInfo?> LoadBuyerAsync(string buyerName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(buyerName)) return null;
        const string sql = """
            SELECT TOP 1
                LTRIM(RTRIM(ISNULL(BuyerName, ''))) AS BuyerName,
                LTRIM(RTRIM(ISNULL(Address, ''))) AS Address,
                LTRIM(RTRIM(ISNULL(Country, ''))) AS Country,
                LTRIM(RTRIM(ISNULL(TermsOfPayment, ''))) AS TermsOfPayment
            FROM Despatch.dbo.Vw_MarketingInvoice WITH (NOLOCK)
            WHERE ISNULL(IsCancel, '') = ''
              AND BuyerName LIKE @Like
            ORDER BY InvDate DESC
            """;
        using var connection = _database.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<BuyerInfo>(
            new CommandDefinition(sql, new { Like = buyerName.Trim() + "%" },
                cancellationToken: ct, commandTimeout: Timeout));
    }

    private async Task<PackingInfo?> LoadPackingAsync(string invoiceNo, string shippingBillNo, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP 1
                LTRIM(RTRIM(ISNULL(Bill_leading_no, ''))) AS BlNo,
                LTRIM(RTRIM(ISNULL(port_loading, ''))) AS PortLoading,
                LTRIM(RTRIM(ISNULL(port_discharge, ''))) AS PortDischarge
            FROM Despatch.dbo.Packinglist WITH (NOLOCK)
            WHERE (@InvoiceNo IS NOT NULL AND LTRIM(RTRIM(Invoice_no)) = @InvoiceNo)
               OR (@ShippingBillNo IS NOT NULL AND LTRIM(RTRIM(ShippingBillNo)) = @ShippingBillNo)
            ORDER BY Invoice_dt DESC
            """;
        using var connection = _database.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<PackingInfo>(
            new CommandDefinition(sql, new
            {
                InvoiceNo = NullIfEmpty(invoiceNo),
                ShippingBillNo = NullIfEmpty(shippingBillNo),
            }, cancellationToken: ct, commandTimeout: Timeout));
    }

    private static CompanyProfile RequireProfile(string? id) =>
        Profiles.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException("Select PIL or KPW.");

    private static readonly CompanyProfile[] Profiles =
    [
        new(
            "PIL",
            "Plastene India Limited (PIL)",
            "PIL",
            "bob-lodgement-pil.pdf",
            "signature-pil.png",
            "Plastene India Limited%",
            "PLASTENE INDIA%",
            "SME",
            "Bill Purchase – Non LC (FPO)",
            "63053200",
            "PLASTENE INDIA LIMITED UNIT1,\rS.NO.- 315/1, 315/2, 316,317,\rNANI CHAIRAI , TALUKA BHACHAU, KUTCH",
            "FORAM MEHTA",
            "FINANCE5@CHAMPALALGROUP.COM",
            "",
            "26160500000026"),
        new(
            "KPW",
            "KP Woven Private Limited (KPW)",
            "KPW",
            "bob-lodgement-kpw.pdf",
            "signature-kpw.png",
            "K.P. WOVEN%",
            "KP WOVEN%",
            "SME",
            "Collection Bills (FBC)",
            "63053200",
            "KP WOVEN PRIVATE LIMITED \rPLOT NO SHED NO .PE/82\rROAD NO .19 GIDC SANAND II\rINDUSTRIAL ESTATE SANAND \rAHMEDABAD GUJARAT \rINDIA",
            "FORAM MEHTA",
            "FINANCE5@CHAMPALALGROUP.COM",
            "7069100022",
            "25380500000217"),
    ];

    private static string RequestTypeField(string requestType, CompanyProfile profile)
    {
        var t = (requestType ?? "").Trim().ToUpperInvariant();
        if (t.Contains("FBC") || t.Contains("COLLECTION BILLS")) return "untitled1";
        if (t.Contains("FBM") || t.Contains("100% ADVANCE") || t.Contains("AGAINST 100")) return "untitled3";
        if (t.Contains("FBCA") || t.Contains("DIRECT BILLS")) return "untitled5";
        if (t.Contains("FPO") || (t.Contains("NON LC") && t.Contains("PURCHASE"))) return "untitled7";
        if (t.Contains("FPL") || (t.Contains("PURCHASE") && t.Contains("LC") && !t.Contains("NON"))) return "untitled9";
        if (t.Contains("FCP")) return "untitled11";
        if (t.Contains("FDO")) return "untitled13";
        if (t.Contains("FDL")) return "untitled15";
        if (t.Contains("FCD")) return "untitled17";
        if (t.Contains("FBN") || t.Contains("NEGOTIATION")) return "untitled22";
        if (t.Contains("PSDL")) return "untitled24";
        // fall back to company habit from sample PDFs
        return profile.Id == "KPW" ? "untitled1" : "untitled7";
    }

    private static void ClearTickDefaults(AcroFields form)
    {
        foreach (var name in new[]
                 {
                     "untitled1", "untitled3", "untitled5", "untitled7", "untitled9", "untitled11",
                     "untitled13", "untitled15", "untitled17", "untitled19", "untitled20",
                     "untitled22", "untitled24", "untitled25", "untitled26",
                 })
        {
            try { form.SetField(name, "Off"); } catch { /* field may be absent */ }
        }
    }

    private static void SetTick(AcroFields form, string field, bool on)
    {
        try { form.SetField(field, on ? "Yes" : "Off"); } catch { /* ignore */ }
    }

    private static bool IsYes(string? v) =>
        string.Equals(v?.Trim(), "Yes", StringComparison.OrdinalIgnoreCase)
        || string.Equals(v?.Trim(), "Y", StringComparison.OrdinalIgnoreCase)
        || string.Equals(v?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

    private static (string Days, string From) ParseUsance(string terms)
    {
        if (string.IsNullOrWhiteSpace(terms)) return ("", "BILL OF LADING");
        var m = Regex.Match(terms, @"(\d+)\s*days?", RegexOptions.IgnoreCase);
        var days = m.Success ? m.Groups[1].Value : "";
        var from = terms.Contains("BL", StringComparison.OrdinalIgnoreCase)
                   || terms.Contains("BILL OF LADING", StringComparison.OrdinalIgnoreCase)
            ? "BL AFTER DATE"
            : terms.Contains("SIGHT", StringComparison.OrdinalIgnoreCase)
                ? "SIGHT"
                : "BILL OF LADING";
        if (terms.Contains("ADVANCE", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(days))
            return ("0", "ADVANCE");
        return (days, from);
    }

    private static string PortName(string? portCode) =>
        (portCode ?? "").Trim().ToUpperInvariant() switch
        {
            "INMUN1" or "INMUN" => "MUNDRA",
            "INAMD4" or "INAMD" => "AHMEDABAD",
            "INNSA1" or "INNSA" => "NHAVA SHEVA",
            _ => portCode?.Trim() ?? "",
        };

    private static string FormatBuyerAddress(BuyerInfo? buyer, string fallbackName)
    {
        if (buyer == null) return fallbackName;
        var parts = new[] { buyer.BuyerName, buyer.Address, buyer.Country }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().Trim(','));
        return string.Join("\r", parts);
    }

    private static string GuessCountry(string? discharge)
    {
        var d = (discharge ?? "").ToUpperInvariant();
        if (d.Contains("SPAIN") || d.Contains("VALENCIA")) return "SPAIN";
        if (d.Contains("FELIXSTOWE") || d.Contains("UK") || d.Contains("ENGLAND")) return "ENGLAND";
        if (d.Contains("GERMANY")) return "GERMANY";
        if (d.Contains("FRANCE")) return "FRANCE";
        if (d.Contains("USA") || d.Contains("NEW YORK")) return "USA";
        return "";
    }

    private static string NormalizeCurrency(string? raw)
    {
        var c = (raw ?? "").Trim().ToUpperInvariant();
        return c switch
        {
            "$" or "USD$" or "US$" => "USD",
            "€" or "EURO" or "EUR" => "EURO",
            "£" or "GBP£" => "GBP",
            "" => "USD",
            _ => c,
        };
    }

    private static string FormatAmount(decimal amount) =>
        amount == decimal.Truncate(amount)
            ? amount.ToString("0", CultureInfo.InvariantCulture)
            : amount.ToString("0.00", CultureInfo.InvariantCulture);

    private static string FormatDisplayDate(string? iso)
    {
        if (DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
        return iso ?? "";
    }

    private static string CompactDate(string? display)
    {
        if (DateTime.TryParse(display, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d.ToString("ddMMyyyy", CultureInfo.InvariantCulture);
        var digits = new string((display ?? "").Where(char.IsDigit).ToArray());
        return digits.Length >= 8 ? digits[..8] : digits;
    }

    private static string Prefer(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? "";

    private static string? NullIfEmpty(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static string CellText(IXLCell cell)
    {
        // ClosedXML's TryGetValue<DateTime> succeeds for ordinary amounts (Excel serials),
        // e.g. 40119.1 → 2009-11-02 — so only treat real date-typed / date-formatted cells as dates.
        if (IsDateCell(cell) && cell.TryGetValue(out DateTime dt))
            return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (cell.TryGetValue(out double num))
            return num.ToString(CultureInfo.InvariantCulture);
        return cell.GetFormattedString()?.Trim() ?? cell.GetString()?.Trim() ?? "";
    }

    private static bool IsDateCell(IXLCell cell)
    {
        if (cell.DataType == XLDataType.DateTime) return true;
        var fmt = cell.Style.NumberFormat.Format ?? "";
        if (string.IsNullOrWhiteSpace(fmt) || fmt.Equals("General", StringComparison.OrdinalIgnoreCase))
            return false;
        // Date formats use y/d/M; skip plain number / accounting formats (0, #, , .).
        if (fmt.IndexOfAny(['y', 'Y', 'd', 'D']) >= 0) return true;
        // Month token in Excel is M/MM (not mm = minutes); avoid matching #,##0.00
        return fmt.Contains('M') && !fmt.Contains('0') && !fmt.Contains('#');
    }

    private static decimal ParseAmount(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0m;
        // Strip currency symbols / letters; keep digits, separators, sign.
        var cleaned = new string(raw.Trim()
            .Where(ch => char.IsDigit(ch) || ch is '.' or ',' or '-' or '(' or ')')
            .ToArray());
        if (cleaned.StartsWith('(') && cleaned.EndsWith(')'))
            cleaned = "-" + cleaned.Trim('(', ')');
        cleaned = cleaned.Replace(",", "");
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            return amount;
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.GetCultureInfo("en-IN"), out amount))
            return amount;
        return 0m;
    }

    private static string NormalizeDateText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ||
            DateTime.TryParse(raw, CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.None, out d) ||
            DateTime.TryParse(raw, CultureInfo.GetCultureInfo("en-IN"), DateTimeStyles.None, out d))
            return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return raw.Trim();
    }

    private static string? MatchHeader(string header)
    {
        var n = NormHeader(header);
        if (n is "") return null;

        bool Has(params string[] parts) => parts.All(p => n.Contains(p, StringComparison.Ordinal));

        if (Has("company") || n is "unit" or "entity") return "companyName";
        // Currency before invoice — "Invoice Currency" must not become invoiceNo.
        if (Has("currency") || n is "ccy" or "curr") return "currency";
        if ((Has("invoice") && Has("date")) || n is "invdate" or "invdt") return "invoiceDate";
        if (Has("amount") && Has("word")) return "amountWords";
        // Skip INR / receipt amount columns so FC invoice amount wins.
        if (Has("amount") && (Has("inr") || Has("rupee") || Has("receipt") || Has("recpt") || Has("rcpt")))
            return null;
        if (Has("amount") || Has("invoicevalue") || Has("invamount") || Has("fob") || Has("billamount")
            || Has("commercialamount") || n is "value" or "invvalue" or "amt" or "invoiceamt")
            return "amount";
        if (Has("invoice") || n is "invno" or "inv" or "commercialinvoice") return "invoiceNo";
        if (Has("buyer") && (Has("address") || Has("addr"))) return "buyerAddress";
        if (Has("buyer") && Has("country")) return "buyerCountry";
        if (Has("buyer") || Has("customer") || Has("consignee") || n is "drawee") return "buyerName";
        if ((Has("shipping") && Has("bill") && Has("date")) || Has("sbdate") || Has("shippingbdte")) return "shippingBillDate";
        if ((Has("shipping") && Has("bill")) || n is "sbno" or "sb" or "edf" or "shippingbillno") return "shippingBillNo";
        // Avoid "Payment Due Days from BL Date" matching as blDate.
        if (n is "bldate" or "billofladingdate" || (Has("bl") && Has("date") && !Has("due") && !Has("payment") && !Has("day")))
            return "blDate";
        if (n is "bl" or "blno" || Has("billoflading") || Has("fcr") || (Has("bl") && Has("no") && !Has("due")))
            return "blNo";
        if (Has("loading") || Has("pol") || n.Contains("portofloading", StringComparison.Ordinal)) return "portOfLoading";
        if (Has("discharge") || Has("destination") || Has("pod") || Has("finaldestination")) return "portOfDestination";
        if (Has("port") && Has("code")) return "portCode";
        if (Has("hsn") || Has("hscode") || (Has("hs") && Has("code"))) return "hsCode";
        if (Has("goods") || Has("commodity") || Has("description") || Has("product")) return "goods";
        if ((Has("usance") && Has("day")) || (Has("due") && Has("day"))) return "usanceDays";
        if (Has("terms") || Has("tenor") || (Has("payment") && !Has("due"))) return "termsOfPayment";
        if (Has("country") && !Has("origin")) return "buyerCountry";
        return null;
    }

    private static string NormHeader(string header)
    {
        var chars = header.ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch)).ToArray();
        return new string(chars);
    }

    private static BobLodgementField F(string key, string label, string value, bool multiline = false, int sort = 0) =>
        new() { Key = key, Label = label, Value = value, Multiline = multiline, Sort = sort };

    private static string AmountInWords(decimal amount, string currency)
    {
        var whole = (long)Math.Floor(Math.Abs(amount));
        var frac = (int)Math.Round((Math.Abs(amount) - whole) * 100, MidpointRounding.AwayFromZero);
        var cur = NormalizeCurrency(currency);
        var prefix = cur switch
        {
            "USD" => "USD ",
            "EURO" or "EUR" => "EURO ",
            "GBP" => "GBP ",
            _ => cur + " ",
        };
        var words = NumberToWords(whole).ToUpperInvariant();
        if (frac > 0 && cur is "GBP")
            return prefix + words + " AND PENCE " + NumberToWords(frac).ToUpperInvariant() + " ONLY";
        if (frac > 0)
            return prefix + words + " AND CENTS " + NumberToWords(frac).ToUpperInvariant() + " ONLY";
        return prefix + words + " ONLY";
    }

    private static string NumberToWords(long n)
    {
        if (n == 0) return "Zero";
        string[] units =
        [
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
            "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen",
        ];
        string[] tens = ["", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"];
        string Scale(long v, long div, string name)
        {
            if (v < div) return "";
            var q = v / div;
            return NumberToWords(q) + " " + name + (v % div > 0 ? " " + NumberToWords(v % div) : "");
        }
        if (n >= 10000000) return Scale(n, 10000000, "Crore");
        if (n >= 100000) return Scale(n, 100000, "Lakh");
        if (n >= 1000) return Scale(n, 1000, "Thousand");
        if (n >= 100) return Scale(n, 100, "Hundred");
        if (n >= 20) return tens[n / 10] + (n % 10 > 0 ? " " + units[n % 10] : "");
        return units[n];
    }

    private sealed record CompanyProfile(
        string Id, string Title, string ShortLabel, string TemplateFile, string SignatureFile,
        string CompanyLike, string CompanyLike2, string BranchName, string DefaultRequestType,
        string DefaultHsCode, string ExporterAddress, string ContactPerson, string Email, string Phone, string AccountNo);

    private sealed class ErpShipment
    {
        public string InvoiceNo { get; set; } = "";
        public string InvoiceDate { get; set; } = "";
        public string BuyerName { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string Currency { get; set; } = "";
        public decimal Amount { get; set; }
        public string ShippingBillNo { get; set; } = "";
        public string ShippingBillDate { get; set; } = "";
        public string PortCode { get; set; } = "";
        public string PortOfDischarge { get; set; } = "";
        public string BlNo { get; set; } = "";
        public string BlDate { get; set; } = "";
    }

    private sealed class BuyerInfo
    {
        public string BuyerName { get; set; } = "";
        public string Address { get; set; } = "";
        public string Country { get; set; } = "";
        public string TermsOfPayment { get; set; } = "";
    }

    private sealed class PackingInfo
    {
        public string BlNo { get; set; } = "";
        public string PortLoading { get; set; } = "";
        public string PortDischarge { get; set; } = "";
    }
}

public record BobLodgementCompanyInfo(string Id, string Title, string ShortLabel);

public sealed class BobLodgementShipmentHit
{
    public string InvoiceNo { get; set; } = "";
    public string InvoiceDate { get; set; } = "";
    public string BuyerName { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string Currency { get; set; } = "";
    public decimal Amount { get; set; }
    public string ShippingBillNo { get; set; } = "";
    public string ShippingBillDate { get; set; } = "";
    public string PortCode { get; set; } = "";
    public string PortOfDischarge { get; set; } = "";
    public string BlNo { get; set; } = "";
    public string BlDate { get; set; } = "";
}

public sealed class BobLodgementField
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public bool Multiline { get; set; }
    public int Sort { get; set; }
}

public sealed class BobLodgementDraft
{
    public string CompanyId { get; set; } = "";
    public string CompanyTitle { get; set; } = "";
    public string InvoiceNo { get; set; } = "";
    public string ShippingBillNo { get; set; } = "";
    public string SourceSummary { get; set; } = "";
    public List<BobLodgementField> Fields { get; set; } = [];
}

public sealed class BobLodgementDownloadRequest
{
    public string CompanyId { get; set; } = "";
    public List<BobLodgementField>? Fields { get; set; }
}

public sealed class BobLodgementExcelParseResult
{
    public string FileName { get; set; } = "";
    public string SheetName { get; set; } = "";
    public int HeaderRow { get; set; }
    public Dictionary<string, string> MappedColumns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<BobLodgementExcelRow> Rows { get; set; } = [];
}

public sealed class BobLodgementExcelRow
{
    public int RowNumber { get; set; }
    public string CompanyName { get; set; } = "";
    public string InvoiceNo { get; set; } = "";
    public string InvoiceDate { get; set; } = "";
    public string BuyerName { get; set; } = "";
    public string BuyerAddress { get; set; } = "";
    public string BuyerCountry { get; set; } = "";
    public string Currency { get; set; } = "";
    public decimal Amount { get; set; }
    public string AmountWords { get; set; } = "";
    public string ShippingBillNo { get; set; } = "";
    public string ShippingBillDate { get; set; } = "";
    public string BlNo { get; set; } = "";
    public string BlDate { get; set; } = "";
    public string PortOfLoading { get; set; } = "";
    public string PortOfDestination { get; set; } = "";
    public string PortCode { get; set; } = "";
    public string HsCode { get; set; } = "";
    public string Goods { get; set; } = "";
    public string TermsOfPayment { get; set; } = "";
    public string UsanceDays { get; set; } = "";
}

public sealed class BobLodgementExcelDraftRequest
{
    public string CompanyId { get; set; } = "";
    public BobLodgementExcelRow? Row { get; set; }
}
