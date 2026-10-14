using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class DomesticInvoiceService
{
    private readonly DatabaseService _db;

    public DomesticInvoiceService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<DomesticInvoiceLookups> LookupsAsync(string company, string asOn, CancellationToken ct)
    {
        var date = ParseDate(asOn).ToString("yyyy-MM-dd");
        await using var con = _db.CreateConnection();
        var result = new DomesticInvoiceLookups
        {
            Companies = await Strings(con, "SELECT name FROM Despatch.dbo.FactoryInfo WITH (NOLOCK) ORDER BY OrderPosition", ct),
            Years = await Strings(con, "SELECT yr FROM MaterialProcessing.dbo.yearmaster WITH (NOLOCK) ORDER BY srno DESC", ct),
            Departments = await Strings(con, "SELECT DepttName FROM Despatch.dbo.itemdeptt WITH (NOLOCK) ORDER BY depttname", ct),
            Transporters = await Strings(con, "SELECT compname FROM Despatch.dbo.TransportMaster WITH (NOLOCK) ORDER BY compname", ct),
            Currencies = await Strings(con, "SELECT currency FROM Despatch.dbo.currencytype WITH (NOLOCK)", ct),
            States = await Strings(con, "SELECT DISTINCT StateName FROM Despatch.dbo.StateMaster WITH (NOLOCK) ORDER BY StateName", ct),
            PaymentTerms = await Strings(con, "SELECT DISTINCT paymenttermname FROM MaterialProcessing.dbo.PaymentTermsMaster WITH (NOLOCK) WHERE Type='Sales' AND isdelflag=0 ORDER BY paymenttermname", ct),
            Vehicles = await Strings(con, "SELECT DISTINCT vehicalNo FROM Despatch.dbo.DomesticInvoice WITH (NOLOCK) WHERE ISNULL(vehicalNo,'')<>'' ORDER BY vehicalNo", ct),
            Destinations = await Strings(con, "SELECT DISTINCT Destination FROM Despatch.dbo.DomesticInvoice WITH (NOLOCK) WHERE ISNULL(Destination,'')<>'' ORDER BY Destination", ct),
            Units = await Strings(con, "SELECT DISTINCT UnitName FROM MaterialProcessing.dbo.Unit WITH (NOLOCK) ORDER BY UnitName", ct),
            MainGroups = await Strings(con, "SELECT DISTINCT DepttName FROM MaterialProcessing.dbo.ItemDeptt WITH (NOLOCK) ORDER BY DepttName", ct),
            Vendors = await Strings(con, "SELECT DISTINCT FirmName FROM MaterialProcessing.dbo.Vendor WITH (NOLOCK) ORDER BY FirmName", ct),
        };

        if (!string.IsNullOrWhiteSpace(company))
        {
            result.Buyers = await Strings(con, """
                SELECT companyname FROM Despatch.dbo.companymaster WITH (NOLOCK)
                WHERE ISNULL(IsFreeze,'no')='no'
                  AND ((@d BETWEEN fromdate AND todate) OR (fromdate <= @d AND todate IS NULL))
                  AND CompanyCode NOT IN (
                    SELECT LCompanyCode FROM MaterialProcessing.dbo.LedgerMaster WITH (NOLOCK)
                    WHERE (Todate IS NOT NULL OR IsAccClose='yes') AND companyname=@c AND ISNULL(LCompanyCode,'')<>'')
                ORDER BY companyname
                """, ct, P("@d", date), P("@c", company));

            result.AccountHeads = await Strings(con, """
                SELECT LedgerName FROM MaterialProcessing.dbo.ledgermaster WITH (NOLOCK)
                WHERE isinvoice='yes' AND companyname=@c ORDER BY LedgerName
                """, ct, P("@c", company));

            result.Commodities = await Rows(con, """
                SELECT DISTINCT CommodityName, ISNULL(deptt,'') deptt, ISNULL(THeading,'') THeading
                FROM Despatch.dbo.Commodity WITH (NOLOCK)
                WHERE companyname=@c
                  AND ((@d BETWEEN nFromDate AND nToDate) OR (nFromDate <= @d AND nToDate IS NULL))
                ORDER BY CommodityName
                """, ct, r => new DomesticCommodity
            {
                Name = S(r, "CommodityName"),
                Deptt = S(r, "deptt"),
                TariffHeading = S(r, "THeading"),
            }, P("@c", company), P("@d", date));

            result.Items = await Rows(con, """
                SELECT DISTINCT itemcode, Itemname, ISNULL(Deptt,'') Deptt, ISNULL(GroupName,'') GroupName
                FROM MaterialProcessing.dbo.ItemCodes WITH (NOLOCK)
                WHERE todate IS NULL ORDER BY ItemName
                """, ct, r => new DomesticItemOption
            {
                Code = S(r, "itemcode"),
                Name = S(r, "Itemname"),
                Deptt = S(r, "Deptt"),
                GroupName = S(r, "GroupName"),
            });

            result.Groups = await Rows(con, """
                SELECT DISTINCT Depttname, GroupName FROM MaterialProcessing.dbo.ItemGroup WITH (NOLOCK)
                """, ct, r => new DomesticItemGroup { Deptt = S(r, "Depttname"), GroupName = S(r, "GroupName") });

            result.SubGroups = await Rows(con, """
                SELECT DISTINCT DepartmentName, GroupName, SubGroupName FROM MaterialProcessing.dbo.SubGroupMaster WITH (NOLOCK)
                """, ct, r => new DomesticItemSubGroup
            {
                Deptt = S(r, "DepartmentName"),
                GroupName = S(r, "GroupName"),
                SubGroupName = S(r, "SubGroupName"),
            });

            result.UtBonds = await Strings(con, """
                SELECT BondNo FROM Despatch.dbo.UTBond WITH (NOLOCK) WHERE companyname=@c ORDER BY BondNo
                """, ct, P("@c", company));

            await using (var cmd = new SqlCommand("""
                SELECT TOP 1 ISNULL(NState,'') NState, ISNULL(NewGSTNo,'') NewGSTNo,
                       ISNULL(dispatchcode,'') dispatchcode, ISNULL(IsMappPackingList,0) IsMappPackingList
                FROM Despatch.dbo.FactoryInfo WITH (NOLOCK) WHERE name=@c
                """, con))
            {
                cmd.Parameters.Add(P("@c", company));
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    result.FactoryState = S(reader, "NState");
                    result.CompanyGstNo = S(reader, "NewGSTNo");
                    result.DispatchCode = S(reader, "dispatchcode");
                    result.MapPackingList = Convert.ToInt32(reader["IsMappPackingList"]) > 0;
                }
            }
        }

        await using (var excise = new SqlCommand("""
            SELECT TOP 1 * FROM MaterialProcessing.dbo.excisemaster WITH (NOLOCK) ORDER BY FROMDATE DESC
            """, con))
        await using (var reader = await excise.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct) && reader.FieldCount >= 3)
            {
                result.ExciseDuty = Dec(reader, 0);
                result.EduCess = Dec(reader, 1);
                result.SheCess = Dec(reader, 2);
            }
        }

        return result;
    }

    public async Task<List<string>> InvoicesAsync(string company, string suffix, bool merchant, CancellationToken ct)
    {
        await using var con = _db.CreateConnection();
        var merchantSql = merchant
            ? " AND MerchantExport='yes'"
            : " AND ISNULL(MerchantExport,'no')='no'";
        return await Strings(con, $"""
            SELECT InvNo FROM Despatch.dbo.DomesticInvoice WITH (NOLOCK)
            WHERE companyname=@c AND suffix=@y {merchantSql}
            ORDER BY InvNo DESC
            """, ct, P("@c", company), P("@y", suffix));
    }

    public async Task<List<DomesticConsigneeOption>> ConsigneesAsync(string buyer, CancellationToken ct)
    {
        await using var con = _db.CreateConnection();
        var code = await Scalar(con, "SELECT TOP 1 CompanyCode FROM Despatch.dbo.companymaster WITH (NOLOCK) WHERE companyname=@b", ct, P("@b", buyer));
        if (string.IsNullOrWhiteSpace(code)) return [];
        return await Rows(con, """
            SELECT ConsigneeCode, Consigneename, ISNULL(City,'') City, ISNULL(ConsigneeAdd,'') ConsigneeAdd
            FROM Despatch.dbo.Consignee WITH (NOLOCK)
            WHERE companyCode=@c ORDER BY Consigneename
            """, ct, r => new DomesticConsigneeOption
        {
            Code = S(r, "ConsigneeCode"),
            Name = S(r, "Consigneename"),
            City = S(r, "City"),
            Address = S(r, "ConsigneeAdd"),
        }, P("@c", code));
    }

    public async Task<DomesticTaxRate> CommodityTaxAsync(string company, string commodity, string asOn, CancellationToken ct)
    {
        var date = ParseDate(asOn).ToString("yyyy-MM-dd");
        await using var con = _db.CreateConnection();
        await using var cmd = new SqlCommand("""
            SELECT TOP 1 THeading, NCGSTRate, NSGSTRate, NIGSTRate
            FROM Despatch.dbo.CommodityHSNGST WITH (NOLOCK)
            WHERE companyname=@c AND CommodityName=@n
              AND ((@d BETWEEN nFromDate AND nToDate) OR (nFromDate <= @d AND nToDate IS NULL))
            """, con);
        cmd.Parameters.Add(P("@c", company));
        cmd.Parameters.Add(P("@n", commodity));
        cmd.Parameters.Add(P("@d", date));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return new DomesticTaxRate();
        var heading = S(reader, "THeading");
        if (ParseDate(asOn) < new DateTime(2021, 4, 1) && heading.Length > 4)
            heading = heading[..4];
        return new DomesticTaxRate
        {
            TariffHeading = heading,
            Cgst = Dec(reader, "NCGSTRate"),
            Sgst = Dec(reader, "NSGSTRate"),
            Igst = Dec(reader, "NIGSTRate"),
        };
    }

    public async Task<string> VendorAddressAsync(string firm, CancellationToken ct)
    {
        await using var con = _db.CreateConnection();
        return await Scalar(con, "SELECT TOP 1 ISNULL(Address,'') FROM MaterialProcessing.dbo.Vendor WITH (NOLOCK) WHERE FirmName=@f", ct, P("@f", firm)) ?? "";
    }

    public async Task<string?> InsurancePolicyAsync(string company, CancellationToken ct)
    {
        await using var con = _db.CreateConnection();
        return await Scalar(con, """
            SELECT TOP 1 policyno FROM Despatch.dbo.InsurancePolicy WITH (NOLOCK)
            WHERE companyname=@c ORDER BY policyno DESC
            """, ct, P("@c", company));
    }

    public async Task<UserCompanyDto> UserCompanyAsync(string login, CancellationToken ct)
    {
        var user = (login ?? "").Trim();
        if (user.Length == 0) return new UserCompanyDto { Message = "Login required." };

        await using var con = _db.CreateConnection();
        var emp = await Scalar(con, """
            SELECT TOP 1 NULLIF(LTRIM(RTRIM(EmpCode)), '')
            FROM Loginentry.dbo.LoginRights WITH (NOLOCK)
            WHERE LTRIM(RTRIM(Name))=@u
            """, ct, P("@u", user));
        if (string.IsNullOrWhiteSpace(emp))
            return new UserCompanyDto { Message = "This login has no employee code, so the company cannot be locked." };

        var payrollCompany = "";
        var payrollFailed = false;
        try
        {
            using var payroll = _db.CreatePayrollLoginEntryConnection();
            payrollCompany = await Scalar(payroll, """
                SELECT TOP 1 LTRIM(RTRIM(CompanyName))
                FROM empinfo WITH (NOLOCK)
                WHERE LTRIM(RTRIM(EmpCode))=@e
                """, ct, P("@e", emp)) ?? "";
        }
        catch
        {
            payrollFailed = true;
        }

        if (payrollFailed)
            return new UserCompanyDto { Message = "Could not read the company on this employee." };
        if (payrollCompany.Length == 0)
            return new UserCompanyDto { Message = "No company is stored on this employee." };

        var factories = await Strings(con, "SELECT name FROM Despatch.dbo.FactoryInfo WITH (NOLOCK) ORDER BY OrderPosition", ct);
        var exact = factories.FirstOrDefault(f => f.Equals(payrollCompany, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return new UserCompanyDto { CompanyName = exact };

        var matches = factories.Where(f =>
            f.Contains(payrollCompany, StringComparison.OrdinalIgnoreCase)
            || payrollCompany.Contains(f, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count >= 1)
        {
            var chosen = matches.FirstOrDefault(f => f.Equals("Plastene India Limited (Unit -II)", StringComparison.OrdinalIgnoreCase))
                ?? matches.FirstOrDefault(f => f.Contains("Unit -II", StringComparison.OrdinalIgnoreCase))
                ?? matches.FirstOrDefault(f => f.Contains("Unit-II", StringComparison.OrdinalIgnoreCase))
                ?? matches[0];
            return new UserCompanyDto { CompanyName = chosen };
        }

        return new UserCompanyDto { Message = $"Employee company '{payrollCompany}' is not a domestic invoice company." };
    }

    public async Task<BuyerTaxDto> BuyerTaxAsync(string buyer, CancellationToken ct)
    {
        await using var con = _db.CreateConnection();
        await using var cmd = new SqlCommand("""
            SELECT TOP 1 ISNULL(Vatno,'') Vatno, ISNULL(GSTNo,'') GSTNo, ISNULL(isaftinno,'') isaftinno, ISNULL(NewGSTNo,'') NewGSTNo
            FROM Despatch.dbo.companymaster WITH (NOLOCK) WHERE companyname=@b
            """, con);
        cmd.Parameters.Add(P("@b", buyer));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return new BuyerTaxDto();
        return new BuyerTaxDto
        {
            VatNo = S(reader, "Vatno"),
            GstNo = S(reader, "GSTNo"),
            IsAfTin = S(reader, "isaftinno").Equals("yes", StringComparison.OrdinalIgnoreCase),
            NewGstNo = S(reader, "NewGSTNo"),
        };
    }

    public async Task<decimal> CurrencyValueAsync(string currency, CancellationToken ct)
    {
        await using var con = _db.CreateConnection();
        var raw = await Scalar(con, "SELECT TOP 1 currencyvalue FROM Despatch.dbo.currencytype WITH (NOLOCK) WHERE currency=@c", ct, P("@c", currency));
        return decimal.TryParse(raw, out var n) ? n : 0;
    }

    public async Task<decimal> TcsRateAsync(string company, string ledger, string commodity, string asOn, CancellationToken ct)
    {
        var date = ParseDate(asOn);
        await using var con = _db.CreateConnection();
        var raw = await Scalar(con, """
            SELECT TOP 1 TotalTCS FROM Despatch.dbo.tcsfill WITH (NOLOCK)
            WHERE Commodity=@commodity AND companyname=@c
              AND ((@d BETWEEN fromdate AND todate) OR (@d >= fromdate AND todate IS NULL))
            """, ct, P("@commodity", commodity), P("@c", company), P("@d", date.ToString("yyyy-MM-dd")));
        if (decimal.TryParse(raw, out var direct)) return direct;
        if (date < new DateTime(2020, 10, 1)) return 0;

        var pan = await Scalar(con, """
            SELECT ISNULL(PANNo,'') FROM MaterialProcessing.dbo.vw_TCS_Sales_Appliacble WITH (NOLOCK)
            WHERE companyname=@c AND LedgerName=@l
            """, ct, P("@c", company), P("@l", ledger)) ?? "";
        var tcsApp = await IntScalar(con, "SELECT MaterialProcessing.dbo.fn_Get_IsTCSApp(@c,@l,@d)", ct,
            P("@c", company), P("@l", ledger), P("@d", date.ToString("yyyy-MM-dd")));
        if (pan.Length < 10) return tcsApp == 0 ? 0 : 1;

        raw = await Scalar(con, """
            SELECT TOP 1 TotalTCS FROM Despatch.dbo.tcsfill WITH (NOLOCK)
            WHERE companyname=@c AND ISNULL(Commodity,'')=''
              AND ((@d BETWEEN fromdate AND todate) OR (@d >= fromdate AND todate IS NULL))
            ORDER BY FromDate DESC
            """, ct, P("@c", company), P("@d", date.ToString("yyyy-MM-dd")));
        var rate = decimal.TryParse(raw, out var n) ? n : 0;
        var inter = await IntScalar(con, "SELECT MaterialProcessing.dbo.fn_Get_Ledger_InterCompany(@c,@l,@d)", ct,
            P("@c", company), P("@l", ledger), P("@d", date.ToString("yyyy-MM-dd")));
        var unit = await IntScalar(con, "SELECT MaterialProcessing.dbo.fn_Get_Ledger_InterUnit(@c,@l,@d)", ct,
            P("@c", company), P("@l", ledger), P("@d", date.ToString("yyyy-MM-dd")));
        if (inter == 1 || unit == 1 || tcsApp == 0) return 0;
        return rate;
    }

    public async Task<List<DomesticPackingListRow>> PendingPackingListsAsync(string company, CancellationToken ct)
    {
        await using var con = _db.CreateConnection();
        return await Rows(con, """
            SELECT * FROM Despatch.dbo.vw_PackingListAll_Pending WITH (NOLOCK)
            WHERE companyname=@c ORDER BY despatchdate, packinglistno, invno
            """, ct, r => new DomesticPackingListRow
        {
            PackingListNo = S(r, "PACKINGLISTNO"),
            InvNo = S(r, "invno"),
            PartyName = S(r, "PartyName"),
            DespatchDate = ReadDate(r, "despatchdate"),
            NetWt = Dec(r, "Netwt"),
        }, P("@c", company));
    }

    public async Task ValidatePackingAsync(DomesticPackingCheck check, CancellationToken ct)
    {
        if (check.PackingListNos.Count == 0)
            throw new ArgumentException("Select at least one packing list.");
        var selectedWt = 0m;
        await using var con = _db.CreateConnection();
        var rows = await PendingPackingListsAsync(check.CompanyName, ct);
        foreach (var no in check.PackingListNos)
        {
            var row = rows.FirstOrDefault(r => r.PackingListNo == no || r.InvNo == no);
            if (row == null) throw new ArgumentException($"Packing list {no} is not pending.");
            selectedWt += row.NetWt;
        }
        if (selectedWt != check.InvoiceNetWt)
            throw new ArgumentException("Packing list quantity and invoice quantity do not match.");

        var invoiceByItem = check.Lines
            .Where(l => !string.IsNullOrWhiteSpace(l.ItemCode))
            .GroupBy(l => l.ItemCode)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.NetWt));
        var packed = await ItemWeights(con, check.PackingListNos, ct);
        if (packed.Count != invoiceByItem.Count)
            throw new ArgumentException("Packing list items do not match the invoice lines.");
        foreach (var pair in invoiceByItem)
        {
            if (!packed.TryGetValue(pair.Key, out var wt) || wt != pair.Value)
                throw new ArgumentException($"Net weight for item {pair.Key} does not match the packing list.");
        }
    }

    public async Task<DomesticInvoiceDto?> GetAsync(string company, string suffix, int invNo, bool merchant, CancellationToken ct)
    {
        await using var con = _db.CreateConnection();
        var merchantSql = merchant ? " AND MerchantExport='yes'" : " AND ISNULL(MerchantExport,'no')='no'";
        await using var cmd = new SqlCommand($"""
            SELECT * FROM Despatch.dbo.DomesticInvoice WITH (NOLOCK)
            WHERE companyname=@c AND suffix=@y AND InvNo=@n {merchantSql}
            """, con);
        cmd.Parameters.Add(P("@c", company));
        cmd.Parameters.Add(P("@y", suffix));
        cmd.Parameters.Add(P("@n", invNo));
        DomesticInvoiceDto? invoice = null;
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct)) return null;
            invoice = MapHeader(reader);
        }

        invoice.Marketing = (await Scalar(con, """
            SELECT TOP 1 IsMarketing FROM Despatch.dbo.DomesticInvoice WITH (NOLOCK)
            WHERE companyname=@c AND suffix=@y AND InvNo=@n AND IsMarketing='yes'
            """, ct, P("@c", company), P("@y", suffix), P("@n", invNo))) != null;
        invoice.Printed = (await Scalar(con, """
            SELECT TOP 1 InvNo FROM Despatch.dbo.printdomesticinvoice WITH (NOLOCK)
            WHERE InvNo=@n
            """, ct, P("@n", invNo))) != null;

        await using (var oel = new SqlCommand("""
            SELECT * FROM Despatch.dbo.DOMESTICINVOICEOEL2 WITH (NOLOCK)
            WHERE InvoiceNumber=@n AND sufix=@y AND companyname=@c
            """, con))
        {
            oel.Parameters.Add(P("@n", invNo));
            oel.Parameters.Add(P("@y", suffix));
            oel.Parameters.Add(P("@c", company));
            await using var reader = await oel.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                invoice.OelBasic = Dec(reader, "Basic");
                invoice.OelCvd = Dec(reader, "cvd");
                invoice.OelCess1 = Dec(reader, "EduCess1");
                invoice.OelCess2 = Dec(reader, "EduCess2");
                invoice.OelCess3 = Dec(reader, "EduCess3");
                invoice.OelShe1 = Dec(reader, "HiEduCess1");
                invoice.OelShe2 = Dec(reader, "HiEduCess2");
                invoice.OelShe3 = Dec(reader, "HiEduCess3");
                invoice.OelAddDuty = Dec(reader, "AddDuty");
            }
        }

        if (!string.IsNullOrWhiteSpace(invoice.ConsigneeCode))
        {
            await using var cons = new SqlCommand("""
                SELECT TOP 1 consigneename, City, ConsigneeAdd FROM Despatch.dbo.consignee WITH (NOLOCK)
                WHERE Consigneecode=@code
                """, con);
            cons.Parameters.Add(P("@code", invoice.ConsigneeCode));
            await using var reader = await cons.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                invoice.ConsigneeName = S(reader, "consigneename");
                invoice.ConsigneeCity = S(reader, "City");
                invoice.ConsigneeAddress = S(reader, "ConsigneeAdd");
            }
        }

        invoice.Lines = await Rows(con, $"""
            SELECT * FROM Despatch.dbo.DomesticInvItem WITH (NOLOCK)
            WHERE InvNo=@n AND yr=@y AND companyname=@c {merchantSql}
            ORDER BY SrNo
            """, ct, MapLine, P("@n", invNo), P("@y", suffix), P("@c", company));

        var oelView = invoice.CompanyName == DomesticInvoiceMath.OelCompany;
        var view = oelView
            ? "Despatch.dbo.Vw_DomesticInvoiceOEL2"
            : "Despatch.dbo.Vw_DomesticInvoice";
        invoice.GstInvNo = await Scalar(con, $"""
            SELECT TOP 1 GSTInvNo FROM {view} WITH (NOLOCK)
            WHERE {ViewFilter(oelView)}
            """, ct, P("@n", invNo), P("@c", company), P("@y", suffix)) ?? invoice.InvType;

        invoice.EwayNo = await Scalar(con, """
            SELECT TOP 1 EWayNo FROM Despatch.dbo.EWayBillMaster WITH (NOLOCK)
            WHERE companyname=@c AND Yr=@y AND InvNo=@gst
            """, ct, P("@c", company), P("@y", suffix), P("@gst", invoice.GstInvNo)) ?? "";

        invoice.PackingListNo = await Scalar(con, """
            SELECT TOP 1 PackingListNo FROM Despatch.dbo.MappPackingList WITH (NOLOCK)
            WHERE CompanyName=@c AND Yr=@y AND InvNo=@gst
            """, ct, P("@c", company), P("@y", suffix), P("@gst", invoice.GstInvNo)) ?? "";

        return invoice;
    }

    public async Task<DomesticInvoiceSaveResult> SaveAsync(DomesticInvoiceDto invoice, string login, bool update, CancellationToken ct)
    {
        Validate(invoice);
        await EnsureDatesOpen(invoice, ct);
        await using var con = _db.CreateConnection();
        await using var tx = (SqlTransaction)await con.BeginTransactionAsync(ct);
        try
        {
            if (update)
            {
                if (await Exists(con, tx, """
                    SELECT TOP 1 InvNo FROM Despatch.dbo.printdomesticinvoice
                    WHERE InvNo=@n
                    """, ct, P("@n", invoice.InvNo)))
                    throw new ArgumentException("This invoice is already printed and cannot be changed.");
                if (await Exists(con, tx, """
                    SELECT TOP 1 InvNo FROM Despatch.dbo.DomesticInvoice
                    WHERE InvNo=@n AND suffix=@y AND companyname=@c AND IsMarketing='yes'
                    """, ct, P("@n", invoice.InvNo), P("@y", invoice.Suffix), P("@c", invoice.CompanyName)))
                    throw new ArgumentException("Update this invoice against the order. It is a marketing invoice.");
            }
            else
            {
                var (number, printed) = await NextNumber(con, tx, invoice, ct);
                invoice.InvNo = number;
                invoice.InvType = printed;
                invoice.DespatchNo = number.ToString();
            }

            DomesticInvoiceMath.Calculate(invoice);
            var words = DomesticInvoiceMath.RupeesInWords(invoice.GrandTotal);
            var consigneeCode = await ConsigneeCode(con, tx, invoice, ct);

            if (update)
            {
                await Exec(con, tx, "DELETE FROM Despatch.dbo.DomesticInvItem WHERE InvNo=@n AND Yr=@y AND Companyname=@c", ct,
                    P("@n", invoice.InvNo), P("@y", invoice.Suffix), P("@c", invoice.CompanyName));
                await Exec(con, tx, "DELETE FROM Despatch.dbo.DOMESTICINVOICEOEL2 WHERE InvoiceNumber=@n AND sufix=@y AND companyname=@c", ct,
                    P("@n", invoice.InvNo), P("@y", invoice.Suffix), P("@c", invoice.CompanyName));
                await UpdateHeader(con, tx, invoice, words, consigneeCode, ct);
            }
            else
            {
                await InsertHeader(con, tx, invoice, words, consigneeCode, login, ct);
            }

            if (invoice.CompanyName == DomesticInvoiceMath.OelCompany)
                await InsertOel(con, tx, invoice, ct);
            var sr = 1;
            foreach (var line in invoice.Lines.Where(l => !string.IsNullOrWhiteSpace(l.Commodity)))
                await InsertLine(con, tx, invoice, line, sr++, login, ct);

            if (invoice.DirectExport)
            {
                await Exec(con, tx, """
                    INSERT INTO Despatch.dbo.domesticARE1 (InvNo, CompanyName, Year, AdvNo, UT1BondNo)
                    VALUES (@n, @c, @y, @adv, @bond)
                    """, ct,
                    P("@n", invoice.InvNo), P("@c", invoice.CompanyName), P("@y", invoice.Suffix),
                    P("@adv", invoice.AdvanceLicNo), P("@bond", invoice.UtBondNo));
            }

            if (invoice.Insurance)
            {
                var cover = DomesticInvoiceMath.RoundAway(invoice.GrandTotal * 110m / 100m, 2);
                await Exec(con, tx, """
                    UPDATE Despatch.dbo.insurancepolicy
                    SET pendinglimit = Pendinglimit - @amt
                    WHERE policyno=@p AND companyname=@c
                    """, ct, P("@amt", cover), P("@p", invoice.InsurancePolicyNo), P("@c", invoice.CompanyName));
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(invoice.PackingListNo))
        {
            var gst = await GstNumber(invoice, ct);
            await using var map = _db.CreateConnection();
            await Exec(map, null, "DELETE FROM Despatch.dbo.MappPackingList WHERE CompanyName=@c AND Yr=@y AND InvNo=@gst", ct,
                P("@c", invoice.CompanyName), P("@y", invoice.Suffix), P("@gst", gst));
            await Exec(map, null, """
                INSERT INTO Despatch.dbo.MappPackingList (CompanyName, InvNo, Yr, PackingListNo)
                VALUES (@c, @gst, @y, @p)
                """, ct, P("@c", invoice.CompanyName), P("@y", invoice.Suffix), P("@gst", gst), P("@p", invoice.PackingListNo));
            invoice.GstInvNo = gst;
        }
        else
        {
            invoice.GstInvNo = await GstNumber(invoice, ct);
        }

        return new DomesticInvoiceSaveResult
        {
            InvNo = invoice.InvNo,
            InvType = invoice.InvType,
            GstInvNo = invoice.GstInvNo,
            Message = update
                ? $"Domestic invoice {invoice.InvNo} updated."
                : $"Domestic invoice {invoice.InvNo} saved.",
        };
    }

    public async Task UpdateEwayAsync(string company, string suffix, int invNo, string eway, CancellationToken ct)
    {
        var invoice = await GetAsync(company, suffix, invNo, false, ct)
            ?? await GetAsync(company, suffix, invNo, true, ct)
            ?? throw new ArgumentException("Invoice not found.");
        var gst = string.IsNullOrWhiteSpace(invoice.GstInvNo) ? invoice.InvType : invoice.GstInvNo;
        await using var con = _db.CreateConnection();
        var existing = await Scalar(con, """
            SELECT TOP 1 EWayNo FROM Despatch.dbo.EWayBillMaster WHERE companyname=@c AND Yr=@y AND InvNo=@n
            """, ct, P("@c", company), P("@y", suffix), P("@n", gst));
        if (existing == null)
        {
            await Exec(con, null, """
                INSERT INTO Despatch.dbo.EWayBillMaster (CompanyName, InvNo, Yr, EWayNo)
                VALUES (@c, @n, @y, @e)
                """, ct, P("@c", company), P("@n", gst), P("@y", suffix), P("@e", eway));
        }
        else
        {
            await Exec(con, null, """
                UPDATE Despatch.dbo.EWayBillMaster SET EWayNo=@e
                WHERE companyname=@c AND Yr=@y AND InvNo=@n
                """, ct, P("@e", eway), P("@c", company), P("@y", suffix), P("@n", gst));
        }
    }

    public async Task<DomesticPrintDocument> PrintAsync(string company, string suffix, int invNo, bool merchant, bool record, string login, CancellationToken ct)
    {
        var invoice = await GetAsync(company, suffix, invNo, merchant, ct)
            ?? throw new ArgumentException("Invoice not found.");
        var gstDate = ParseDate(invoice.InvDate) >= new DateTime(2017, 7, 1);
        var copies = gstDate
            ? new List<string> { "ORIGINAL FOR RECIPIENT", "DUPLICATE FOR TRANSPORTER", "TRIPLICATE FOR SUPPLIER" }
            : ["ORIGINAL FOR BUYER", "DUPLICATE FOR TRANSPORTER", "TRIPLICATE FOR ASSESSES"];
        var supplementary = invoice.Header.Contains("Supplementary", StringComparison.OrdinalIgnoreCase);
        var report = invoice.CompanyName == DomesticInvoiceMath.OelCompany
            ? (gstDate ? "CRDomesticPriceOEL2_GST" : "CRDomesticPriceOEL2")
            : supplementary
                ? (gstDate ? "CRSuppDomesticPrice_GST" : "CRSuppDomesticPrice")
                : (gstDate ? "CRDomesticPrice_GST" : "CRDomesticPrice");
        if (invoice.DirectExport) report = "CRARE1_UTBOND_NoImport";

        var ewayRequired = false;
        var ewayGenerated = !string.IsNullOrWhiteSpace(invoice.EwayNo);
        if (gstDate && ParseDate(invoice.InvDate) >= new DateTime(2018, 2, 1))
        {
            var yarn = invoice.Lines.Any(l =>
                l.Commodity.Contains("YARN", StringComparison.OrdinalIgnoreCase) ||
                l.ItemDesc.Contains("YARN", StringComparison.OrdinalIgnoreCase));
            ewayRequired = yarn || !invoice.State.Equals("Gujarat", StringComparison.OrdinalIgnoreCase);
            if (ewayRequired)
            {
                await using var con = _db.CreateConnection();
                var count = await IntScalar(con, """
                    SELECT COUNT(*) FROM Despatch.dbo.EWayBillMaster WITH (NOLOCK)
                    WHERE companyname=@c AND yr=@y AND InvNo=@n
                    """, ct, P("@c", company), P("@y", suffix), P("@n", invoice.GstInvNo));
                ewayGenerated = count > 0;
            }
        }

        if (record)
        {
            if (ewayRequired && !ewayGenerated)
                throw new ArgumentException("E-Way Bill is not generated.");
            await using var con = _db.CreateConnection();
            await Exec(con, null, """
                UPDATE Despatch.dbo.DomesticInvoice SET Header='EXTRA COPY'
                WHERE InvNo=@n AND suffix=@y AND companyname=@c
                """, ct, P("@n", invNo), P("@y", suffix), P("@c", company));
            if (!invoice.Printed)
            {
                await Exec(con, null, """
                    INSERT INTO Despatch.dbo.printdomesticinvoice
                    VALUES (@n, @y, @d, @c, @login)
                    """, ct, P("@n", invNo), P("@y", suffix), P("@d", DateTime.Now), P("@c", company), P("@login", login));
                invoice.Printed = true;
            }
            invoice.Header = "EXTRA COPY";
        }

        var oel = invoice.CompanyName == DomesticInvoiceMath.OelCompany;
        var view = oel
            ? "Despatch.dbo.Vw_DomesticInvoiceOEL2"
            : "Despatch.dbo.Vw_DomesticInvoice";
        await using var read = _db.CreateConnection();
        var rows = await DictionaryRows(read, $"""
            SELECT * FROM {view} WITH (NOLOCK)
            WHERE {ViewFilter(oel)}
            ORDER BY commodity DESC, groupcommodity
            """, ct, P("@n", invNo), P("@c", company), P("@y", suffix));

        return new DomesticPrintDocument
        {
            CopyTitle = invoice.Printed && !record ? "EXTRA COPY" : copies[0],
            Copies = copies,
            AlreadyPrinted = invoice.Printed,
            EwayRequired = ewayRequired,
            EwayGenerated = ewayGenerated,
            ReportName = report,
            Invoice = invoice,
            Rows = rows,
        };
    }

    private static void Validate(DomesticInvoiceDto invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.CompanyName)) throw new ArgumentException("Company is required.");
        if (string.IsNullOrWhiteSpace(invoice.Suffix)) throw new ArgumentException("Suffix is required.");
        if (string.IsNullOrWhiteSpace(invoice.TypeOfInvoice)) throw new ArgumentException("Type of invoice cannot be blank. Select Tax or Retail invoice.");
        if (string.IsNullOrWhiteSpace(invoice.BuyerName)) throw new ArgumentException("Buyer name is required.");
        if (string.IsNullOrWhiteSpace(invoice.Department)) throw new ArgumentException("Department is required.");
        if (string.IsNullOrWhiteSpace(invoice.LrNo)) throw new ArgumentException("LR number is required.");
        if (string.IsNullOrWhiteSpace(invoice.State)) throw new ArgumentException("State is required.");
        if (string.IsNullOrWhiteSpace(invoice.BuyerOrderNo)) throw new ArgumentException("Buyer order number is required.");
        if (string.IsNullOrWhiteSpace(invoice.TermsOfPayment)) throw new ArgumentException("Terms of payment must be chosen from the list.");
        var lines = invoice.Lines.Where(l => !string.IsNullOrWhiteSpace(l.Commodity)).ToList();
        if (lines.Count == 0) throw new ArgumentException("Enter at least one item.");
        var gst = ParseDate(invoice.InvDate) >= new DateTime(2017, 7, 1);
        var hsnFull = ParseDate(invoice.InvDate) >= new DateTime(2021, 4, 1);
        string? heading = null;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.Uom)) throw new ArgumentException("UOM is required on every line.");
            if (line.Qty == 0) throw new ArgumentException("Order quantity is required.");
            if (line.Rate == 0) throw new ArgumentException("Unit price is required.");
            if (line.NetWt == 0) throw new ArgumentException("Net weight is required.");
            if (line.GrossWt == 0) throw new ArgumentException("Gross weight is required.");
            if (line.NetWt > line.GrossWt) throw new ArgumentException("Net weight should be less than gross weight.");
            if (line.Uom.Equals("kgs", StringComparison.OrdinalIgnoreCase) && line.Qty != line.NetWt)
                throw new ArgumentException("When UOM is KGS, quantity must equal net weight.");
            if (gst && string.IsNullOrWhiteSpace(line.Hsn)) throw new ArgumentException("HSN is required.");
            if (hsnFull && line.Hsn.Trim().Length < 6) throw new ArgumentException("HSN must be at least 6 characters.");
            if (gst && string.IsNullOrWhiteSpace(line.ItemCode)) throw new ArgumentException("Item code is required.");
            if (heading == null) heading = line.Hsn;
            else if (!string.Equals(heading, line.Hsn, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Every commodity on the invoice must share the same tariff heading.");
        }
    }

    private async Task EnsureDatesOpen(DomesticInvoiceDto invoice, CancellationToken ct)
    {
        foreach (var raw in new[] { invoice.InvDate, invoice.InvoiceDateTime, invoice.DespatchDate, invoice.RemovalDateTime })
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var date = ParseDate(raw);
            var start = new DateTime(date.Year, date.Month, 1).ToString("yyyy-MM-dd");
            var end = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)).ToString("yyyy-MM-dd");
            await using var con = _db.CreateConnection();
            var locked = await Scalar(con, """
                SELECT TOP 1 Companyname FROM MaterialProcessing.dbo.VoucherTypePrdlock WITH (NOLOCK)
                WHERE Companyname=@c AND groupname='sales' AND LockDate BETWEEN @a AND @b
                """, ct, P("@c", invoice.CompanyName), P("@a", start), P("@b", end));
            if (locked != null)
                throw new ArgumentException($"Sales entries are locked for {date:MMMM yyyy}.");
        }

        await using var check = _db.CreateConnection();
        var term = await Scalar(check, """
            SELECT TOP 1 paymenttermname FROM MaterialProcessing.dbo.PaymentTermsMaster WITH (NOLOCK)
            WHERE Type='Sales' AND isdelflag=0 AND paymenttermname=@t
            """, ct, P("@t", invoice.TermsOfPayment));
        if (term == null) throw new ArgumentException("Terms of payment must be chosen from the sales payment-term list.");
        var buyerGst = await Scalar(check, """
            SELECT ISNULL(NewGSTNo,'') FROM Despatch.dbo.CompanyMaster WITH (NOLOCK) WHERE CompanyName=@b
            """, ct, P("@b", invoice.BuyerName));
        if (buyerGst == null) throw new ArgumentException("Invalid buyer name.");
        if (ParseDate(invoice.InvDate) >= new DateTime(2017, 7, 1))
        {
            var sellerGst = await Scalar(check, """
                SELECT ISNULL(NewGSTNo,'') FROM Despatch.dbo.FactoryInfo WITH (NOLOCK) WHERE name=@c
                """, ct, P("@c", invoice.CompanyName)) ?? "";
            if (buyerGst.Length > 0 && buyerGst == sellerGst)
                throw new ArgumentException("A domestic invoice is not generated for the same GSTIN. Create a delivery challan for a branch transfer.");
        }
    }

    private static async Task<(int Number, string Printed)> NextNumber(SqlConnection con, SqlTransaction tx, DomesticInvoiceDto invoice, CancellationToken ct)
    {
        if (ParseDate(invoice.InvDate) < new DateTime(2017, 7, 1))
        {
            var max = await IntScalar(con, """
                SELECT ISNULL(MAX(InvNo),0) FROM Despatch.dbo.DomesticInvoice
                WHERE suffix=@y AND companyname=@c
                """, ct, tx, P("@y", invoice.Suffix), P("@c", invoice.CompanyName));
            return (max + 1, "");
        }

        var code = await Scalar(con, "SELECT ISNULL(dispatchcode,'') FROM Despatch.dbo.FactoryInfo WHERE name=@c", ct, tx, P("@c", invoice.CompanyName)) ?? "";
        if (invoice.MerchantExport)
        {
            var max = await IntScalar(con, """
                SELECT ISNULL(MAX(CAST(invno AS int)),0) FROM Despatch.dbo.DomesticInvoice
                WHERE companyname=@c AND Suffix=@y AND MerchantExport='yes'
                """, ct, tx, P("@c", invoice.CompanyName), P("@y", invoice.Suffix));
            return (max + 1, $"{code}/ME/{max + 1}");
        }

        if (invoice.PurchaseReturn)
        {
            var max = await IntScalar(con, """
                SELECT ISNULL(MAX(CAST(invno AS int)),0) FROM Despatch.dbo.PurReturnInvoice
                WHERE companyname=@c AND Suffix=@y
                """, ct, tx, P("@c", invoice.CompanyName), P("@y", invoice.Suffix));
            return (max + 1, $"{code}/PR/{max + 1}");
        }

        var series = await IntScalar(con, """
            SELECT ISNULL(MAX(invno),0) FROM (
                SELECT MAX(CAST(invno AS int)) invno FROM Despatch.dbo.DomesticInvoice WHERE companyname=@c AND Suffix=@y
                UNION ALL
                SELECT MAX(CAST(invno AS int)) FROM Despatch.dbo.JobWorkInvoice WHERE companyname=@c AND Suffix=@y
            ) A
            """, ct, tx, P("@c", invoice.CompanyName), P("@y", invoice.Suffix));
        return (series + 1, $"{code}/D/{series + 1}");
    }

    private static async Task InsertHeader(SqlConnection con, SqlTransaction tx, DomesticInvoiceDto invoice, string words, string consigneeCode, string login, CancellationToken ct)
    {
        await Exec(con, tx, """
            INSERT INTO Despatch.dbo.DomesticInvoice (
              SalesOrderNo,InvNo,InvDate,TermsOfPayment,BuyerOrderNo,BuyerDate,DespatchNo,DespatchDate,VehicalNo,Destination,
              TermsOfDelivery,BuyerName,ExDuty,Educess,SecHiEduCess,vat,Addvat,ExciseAmt,EduCessAmt,SecHiEduCessAmt,VatAmt,AddVatAmt,
              TotalQty,TotalAmt,TotalAmtWord,InvDateTime,RemDateTime,TypeOfInvoice,DelChallanNo,DelChallanDate,LRNo,LRDate,LoginName,
              ExciseAmtWord,TransportName,Consignee,Heading,Insurance,Typevat,Remarks,CheckInsurance,Excisetype,VatType,Frieght,TCSPer,TCSAmt,
              Suffix,Deptt,Header,CompanyName,UTBond,Currency,Currencyvalue,PLA,RG23,ConsigneeCode,SupplementaryInvoice,AgainstInvNo,IsCancel,
              SpDuty1,SpDuty1Amt,SpDuty2,SpDuty2Amt,SpDuty3,SpDuty3Amt,AgainstInvDate,AccountHead,notifier,freightremarks,IsMArketing,
              ISFREIGHTINCLUDE,ExciseNilRemarks,VatNIlRemarks,State,ispurchasereturn,MobileNo,InvType,IsGSTAllowDuty1,IsGSTAllowDuty2,
              IsGSTAllowDuty3,IsNIllRated,MerchantExport,Notification,AdvanceLicense,isdispfrm,dispfrm,dispfrm_add)
            VALUES (
              '',@InvNo,@InvDate,@Terms,@BuyerOrder,@BuyerDate,@InvNo,@DespatchDate,@Vehicle,@Destination,
              @Delivery,@Buyer,@ExDuty,@Edu,@She,@Vat,@AddVat,0,0,0,0,0,
              0,@Total,@Words,@InvDt,@RemDt,@Type,@Challan,@ChallanDate,@Lr,@LrDate,@Login,
              @ExciseWord,@Transport,@Consignee,@Heading,@Insurance,@TaxType,@Remarks,@InsureFlag,@ExciseApp,@VatApp,@Freight,@TcsPer,0,
              @Suffix,@Deptt,'',@Company,@UtDetails,@Currency,@CurrencyValue,@Pla,@Rg23,@ConsigneeCode,NULL,'','',
              @Sp1,@Sp1Amt,@Sp2,@Sp2Amt,@Sp3,@Sp3Amt,'',@Account,@Notifier,@FreightRemarks,'no',
              @FreightInclude,@ExciseNil,@VatNil,@State,@PurchaseReturn,@Mobile,@InvType,@Gst1,@Gst2,
              @Gst3,@NilRated,@Merchant,@Notification,@License,@DispatchFlag,@DispatchName,@DispatchAdd)
            """, ct, HeaderParameters(invoice, words, consigneeCode, login));
    }

    private static async Task UpdateHeader(SqlConnection con, SqlTransaction tx, DomesticInvoiceDto invoice, string words, string consigneeCode, CancellationToken ct)
    {
        await Exec(con, tx, """
            UPDATE Despatch.dbo.DomesticInvoice SET
              BuyerName=@Buyer, InvDate=@InvDate, TermsOfPayment=@Terms, BuyerOrderNo=@BuyerOrder, BuyerDate=@BuyerDate,
              DespatchDate=@DespatchDate, VehicalNo=@Vehicle, Destination=@Destination, TermsOfDelivery=@Delivery,
              ExDuty=@ExDuty, Educess=@Edu, SecHiEduCess=@She, vat=@Vat, Addvat=@AddVat, TotalAmt=@Total, TotalAmtWord=@Words,
              InvDateTime=@InvDt, RemDateTime=@RemDt, TypeOfInvoice=@Type, DelChallanNo=@Challan, DelChallanDate=@ChallanDate,
              LRNo=@Lr, LRDate=@LrDate, ExciseAmtWord=@ExciseWord, TransportName=@Transport, Consignee=@Consignee, Heading=@Heading,
              Insurance=@Insurance, Typevat=@TaxType, Remarks=@Remarks, CheckInsurance=@InsureFlag, Excisetype=@ExciseApp, VatType=@VatApp,
              Frieght=@Freight, TCSPer=@TcsPer, Deptt=@Deptt, UTBond=@UtDetails, Currency=@Currency, Currencyvalue=@CurrencyValue,
              PLA=@Pla, RG23=@Rg23, ConsigneeCode=@ConsigneeCode, SpDuty1=@Sp1, SpDuty1Amt=@Sp1Amt, SpDuty2=@Sp2, SpDuty2Amt=@Sp2Amt,
              SpDuty3=@Sp3, SpDuty3Amt=@Sp3Amt, AccountHead=@Account, notifier=@Notifier, freightremarks=@FreightRemarks,
              ISFREIGHTINCLUDE=@FreightInclude, ExciseNilRemarks=@ExciseNil, VatNIlRemarks=@VatNil, State=@State,
              ispurchasereturn=@PurchaseReturn, MobileNo=@Mobile, IsGSTAllowDuty1=@Gst1, IsGSTAllowDuty2=@Gst2, IsGSTAllowDuty3=@Gst3,
              IsNIllRated=@NilRated, MerchantExport=@Merchant, Notification=@Notification, AdvanceLicense=@License,
              isdispfrm=@DispatchFlag, dispfrm=@DispatchName, dispfrm_add=@DispatchAdd
            WHERE InvNo=@InvNo AND suffix=@Suffix AND CompanyName=@Company
            """, ct, HeaderParameters(invoice, words, consigneeCode, ""));
    }

    private static SqlParameter[] HeaderParameters(DomesticInvoiceDto invoice, string words, string consigneeCode, string login) =>
    [
        P("@InvNo", invoice.InvNo),
        P("@InvDate", ParseDate(invoice.InvDate)),
        P("@Terms", invoice.TermsOfPayment),
        P("@BuyerOrder", invoice.BuyerOrderNo),
        P("@BuyerDate", string.IsNullOrWhiteSpace(invoice.BuyerOrderDate) ? DBNull.Value : ParseDate(invoice.BuyerOrderDate)),
        P("@DespatchDate", ParseDate(string.IsNullOrWhiteSpace(invoice.DespatchDate) ? invoice.InvDate : invoice.DespatchDate)),
        P("@Vehicle", invoice.VehicleNo.ToUpperInvariant()),
        P("@Destination", invoice.Destination.ToUpperInvariant()),
        P("@Delivery", string.IsNullOrWhiteSpace(invoice.TermsOfDelivery) ? "FOR" : invoice.TermsOfDelivery),
        P("@Buyer", invoice.BuyerName),
        P("@ExDuty", invoice.ExDuty), P("@Edu", invoice.EduCess), P("@She", invoice.SheCess),
        P("@Vat", invoice.Vat), P("@AddVat", invoice.AddVat),
        P("@Total", invoice.GrandTotal), P("@Words", words),
        P("@InvDt", ParseDate(string.IsNullOrWhiteSpace(invoice.InvoiceDateTime) ? invoice.InvDate : invoice.InvoiceDateTime)),
        P("@RemDt", ParseDate(string.IsNullOrWhiteSpace(invoice.RemovalDateTime) ? invoice.InvDate : invoice.RemovalDateTime)),
        P("@Type", invoice.TypeOfInvoice),
        P("@Challan", invoice.DelChallanNo),
        P("@ChallanDate", ParseDate(string.IsNullOrWhiteSpace(invoice.DelChallanDate) ? invoice.InvDate : invoice.DelChallanDate)),
        P("@Lr", invoice.LrNo),
        P("@LrDate", ParseDate(string.IsNullOrWhiteSpace(invoice.LrDate) ? invoice.InvDate : invoice.LrDate)),
        P("@Login", login),
        P("@ExciseWord", invoice.ExciseApplicable == "NIL" ? "NIL" : ""),
        P("@Transport", invoice.TransportName),
        P("@Consignee", invoice.ConsigneeAddress),
        P("@Heading", invoice.TariffHeading),
        P("@Insurance", invoice.Insurance ? invoice.InsurancePolicyNo : ""),
        P("@TaxType", invoice.TaxType),
        P("@Remarks", invoice.Remarks),
        P("@InsureFlag", invoice.Insurance ? "yes" : "no"),
        P("@ExciseApp", invoice.ExciseApplicable),
        P("@VatApp", invoice.VatApplicable),
        P("@Freight", invoice.Freight),
        P("@TcsPer", invoice.TcsPercent),
        P("@Suffix", invoice.Suffix),
        P("@Deptt", invoice.Department),
        P("@Company", invoice.CompanyName),
        P("@UtDetails", invoice.AvailBenefit ? invoice.UtBondDetails : ""),
        P("@Currency", invoice.Currency),
        P("@CurrencyValue", invoice.CurrencyValue),
        P("@Pla", invoice.DirectExport ? "yes" : "no"),
        P("@Rg23", invoice.Rg23),
        P("@ConsigneeCode", consigneeCode),
        P("@Sp1", invoice.SpDutyRemarks1), P("@Sp1Amt", invoice.SpDutyAmt1),
        P("@Sp2", invoice.SpDutyRemarks2), P("@Sp2Amt", invoice.SpDutyAmt2),
        P("@Sp3", invoice.SpDutyRemarks3), P("@Sp3Amt", invoice.SpDutyAmt3),
        P("@Account", invoice.AccountHead),
        P("@Notifier", invoice.Notifier),
        P("@FreightRemarks", invoice.FreightRemarks),
        P("@FreightInclude", invoice.GstAfterFreight ? "yes" : "no"),
        P("@ExciseNil", invoice.ExciseNilRemarks),
        P("@VatNil", invoice.VatNilRemarks),
        P("@State", invoice.State),
        P("@PurchaseReturn", invoice.PurchaseReturn ? "yes" : "no"),
        P("@Mobile", invoice.MobileNo),
        P("@InvType", invoice.InvType),
        P("@Gst1", invoice.GstOnSpDuty1 ? "yes" : "no"),
        P("@Gst2", invoice.GstOnSpDuty2 ? "yes" : "no"),
        P("@Gst3", invoice.GstOnSpDuty3 ? "yes" : "no"),
        P("@NilRated", invoice.IgstNilNotification ? "yes" : "no"),
        P("@Merchant", invoice.MerchantExport ? "yes" : "no"),
        P("@Notification", invoice.Notification),
        P("@License", invoice.AdvanceLicense),
        P("@DispatchFlag", invoice.DispatchFrom ? 1 : 0),
        P("@DispatchName", invoice.DispatchFromName),
        P("@DispatchAdd", invoice.DispatchFromAddress),
    ];

    private static Task InsertOel(SqlConnection con, SqlTransaction tx, DomesticInvoiceDto invoice, CancellationToken ct) =>
        Exec(con, tx, """
            INSERT INTO Despatch.dbo.DomesticInvoiceoel2
            VALUES (@n,@y,@c,@basic,@cvd,@c1,@c2,@c3,@s1,@s2,@s3,0,0,0,0,0,0,0,0,@addPct,0)
            """, ct,
            P("@n", invoice.InvNo.ToString()), P("@y", invoice.Suffix), P("@c", invoice.CompanyName),
            P("@basic", invoice.OelBasic), P("@cvd", invoice.OelCvd),
            P("@c1", invoice.OelCess1), P("@c2", invoice.OelCess2), P("@c3", invoice.OelCess3),
            P("@s1", invoice.OelShe1), P("@s2", invoice.OelShe2), P("@s3", invoice.OelShe3),
            P("@addPct", invoice.OelAddDuty));

    private static Task InsertLine(SqlConnection con, SqlTransaction tx, DomesticInvoiceDto invoice, DomesticInvoiceLineDto line, int sr, string login, CancellationToken ct) =>
        Exec(con, tx, """
            INSERT INTO Despatch.dbo.DomesticInvItem (
              InvNo,NoofPkg,ItemDesc,Qty,Rate,Unit,Amount,NetWt,GrossWt,SalesOrderNo,LoginName,Commodity,SrNo,GroupCommodity,
              NonDisplayUnit,NonDisplayQty,QtyRemarks,Yr,Companyname,CommodityRemarks,Desc1,Desc2,BagsMRP,size,weekno,LotNo,Colour,
              Meter,GPM,GSM,WBWeight,ReqWtperBag,ReqWtTotal,DispatchWtperBagGr,WtTolperBagGr,WtExtrapershortKg,WtDiffbetPLWB,
              AssesableValue,OrderNO,itemDeptt,HSNCode,CGSTrate,CGSTamt,SGSTRate,SGSTAmt,IGSTRate,IGSTAmt,Itemcode,MerchantExport)
            VALUES (
              @n,@pkg,@desc,@qty,@rate,@uom,@amt,@net,@gross,'',@login,@commodity,@sr,@group,
              @ndu,@ndq,@qrem,@y,@c,@crem,NULL,NULL,NULL,NULL,NULL,NULL,NULL,
              NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,
              NULL,'',@deptt,@hsn,@cgstr,@cgsta,@sgstr,@sgsta,@igstr,@igsta,@item,@merchant)
            """, ct,
            P("@n", invoice.InvNo), P("@pkg", line.NoOfPkg), P("@desc", line.ItemDesc),
            P("@qty", line.Qty), P("@rate", line.Rate), P("@uom", line.Uom), P("@amt", line.Amount),
            P("@net", line.NetWt), P("@gross", line.GrossWt), P("@login", login), P("@commodity", line.Commodity),
            P("@sr", sr), P("@group", line.GroupName), P("@ndu", string.IsNullOrWhiteSpace(line.NonDisplayUnit) ? "KGS" : line.NonDisplayUnit),
            P("@ndq", line.NonDisplayQty), P("@qrem", line.QtyRemarks), P("@y", invoice.Suffix), P("@c", invoice.CompanyName),
            P("@crem", line.CommodityRemarks), P("@deptt", line.Deptt), P("@hsn", line.Hsn),
            P("@cgstr", line.CgstRate), P("@cgsta", line.CgstAmt), P("@sgstr", line.SgstRate), P("@sgsta", line.SgstAmt),
            P("@igstr", line.IgstRate), P("@igsta", line.IgstAmt), P("@item", line.ItemCode),
            P("@merchant", invoice.MerchantExport ? "yes" : "no"));

    private static async Task<string> ConsigneeCode(SqlConnection con, SqlTransaction tx, DomesticInvoiceDto invoice, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(invoice.ConsigneeCode)) return invoice.ConsigneeCode;
        if (string.IsNullOrWhiteSpace(invoice.ConsigneeName)) return "";
        var companyCode = await Scalar(con, "SELECT TOP 1 CompanyCode FROM Despatch.dbo.companymaster WHERE companyname=@b", ct, tx, P("@b", invoice.BuyerName)) ?? "";
        return await Scalar(con, """
            SELECT TOP 1 ConsigneeCode FROM Despatch.dbo.Consignee
            WHERE consigneename=@n AND companycode=@c AND city=@city
            """, ct, tx, P("@n", invoice.ConsigneeName), P("@c", companyCode), P("@city", invoice.ConsigneeCity)) ?? "";
    }

    private async Task<string> GstNumber(DomesticInvoiceDto invoice, CancellationToken ct)
    {
        await using var con = _db.CreateConnection();
        var oel = invoice.CompanyName == DomesticInvoiceMath.OelCompany;
        var view = oel
            ? "Despatch.dbo.Vw_DomesticInvoiceOEL2"
            : "Despatch.dbo.Vw_DomesticInvoice";
        return await Scalar(con, $"""
            SELECT TOP 1 GSTInvNo FROM {view} WITH (NOLOCK)
            WHERE {ViewFilter(oel)}
            """, ct, P("@n", invoice.InvNo), P("@c", invoice.CompanyName), P("@y", invoice.Suffix))
            ?? invoice.InvType;
    }

    private static async Task<Dictionary<string, decimal>> ItemWeights(SqlConnection con, List<string> numbers, CancellationToken ct)
    {
        var names = numbers.Select((_, i) => "@p" + i).ToArray();
        await using var cmd = new SqlCommand($"""
            SELECT Itemcode, SUM(netwt) Netwt
            FROM Despatch.dbo.vw_PackingListAll_Pending_dtl WITH (NOLOCK)
            WHERE PACKINGLISTNO IN ({string.Join(",", names)})
            GROUP BY Itemcode
            """, con);
        for (var i = 0; i < numbers.Count; i++)
            cmd.Parameters.Add(P(names[i], numbers[i]));
        var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            map[S(reader, "Itemcode")] = Dec(reader, "Netwt");
        return map;
    }

    private static DomesticInvoiceDto MapHeader(SqlDataReader r) => new()
    {
        InvNo = Convert.ToInt32(r["InvNo"]),
        InvType = S(r, "InvType"),
        CompanyName = S(r, "CompanyName"),
        Suffix = S(r, "Suffix"),
        TypeOfInvoice = S(r, "TypeOfInvoice"),
        BuyerName = S(r, "BuyerName"),
        InvDate = ReadDate(r, "InvDate"),
        DelChallanNo = S(r, "DelChallanNo"),
        DelChallanDate = ReadDate(r, "DelChallanDate"),
        LrNo = S(r, "LRNo"),
        LrDate = ReadDate(r, "LRDate"),
        DespatchNo = S(r, "DespatchNo"),
        DespatchDate = ReadDate(r, "DespatchDate"),
        TransportName = S(r, "TransportName"),
        VehicleNo = S(r, "VehicalNo"),
        Destination = S(r, "Destination"),
        TermsOfDelivery = S(r, "TermsOfDelivery"),
        State = S(r, "State"),
        MobileNo = S(r, "MobileNo"),
        InvoiceDateTime = ReadDate(r, "InvDateTime"),
        RemovalDateTime = ReadDate(r, "RemDateTime"),
        BuyerOrderNo = S(r, "BuyerOrderNo"),
        BuyerOrderDate = ReadDate(r, "BuyerDate"),
        TermsOfPayment = S(r, "TermsOfPayment"),
        Department = S(r, "Deptt"),
        AccountHead = S(r, "AccountHead"),
        TariffHeading = S(r, "Heading"),
        Insurance = Yes(r, "CheckInsurance"),
        InsurancePolicyNo = S(r, "Insurance"),
        PurchaseReturn = Yes(r, "ispurchasereturn"),
        DirectExport = Yes(r, "PLA"),
        ExDuty = Dec(r, "ExDuty"),
        EduCess = Dec(r, "Educess"),
        SheCess = Dec(r, "SecHiEduCess"),
        TaxType = S(r, "Typevat"),
        Vat = Dec(r, "vat"),
        AddVat = Dec(r, "Addvat"),
        ExciseApplicable = S(r, "Excisetype"),
        VatApplicable = S(r, "VatType"),
        ExciseNilRemarks = S(r, "ExciseNilRemarks"),
        VatNilRemarks = S(r, "VatNIlRemarks"),
        Freight = Dec(r, "Frieght"),
        TcsPercent = Dec(r, "TCSPer"),
        GstAfterFreight = Yes(r, "ISFREIGHTINCLUDE"),
        Remarks = S(r, "Remarks"),
        FreightRemarks = S(r, "freightremarks"),
        Rg23 = S(r, "RG23"),
        IgstNilNotification = Yes(r, "IsNIllRated"),
        Currency = S(r, "Currency"),
        CurrencyValue = S(r, "Currencyvalue"),
        AvailBenefit = !string.IsNullOrWhiteSpace(S(r, "UTBond")),
        UtBondDetails = S(r, "UTBond"),
        ConsigneeCode = S(r, "ConsigneeCode"),
        Notifier = S(r, "notifier"),
        DispatchFrom = S(r, "isdispfrm") == "1",
        DispatchFromName = S(r, "dispfrm"),
        DispatchFromAddress = S(r, "dispfrm_add"),
        SpDutyRemarks1 = S(r, "SpDuty1"),
        SpDutyAmt1 = Dec(r, "SpDuty1Amt"),
        SpDutyRemarks2 = S(r, "SpDuty2"),
        SpDutyAmt2 = Dec(r, "SpDuty2Amt"),
        SpDutyRemarks3 = S(r, "SpDuty3"),
        SpDutyAmt3 = Dec(r, "SpDuty3Amt"),
        GstOnSpDuty1 = Yes(r, "IsGSTAllowDuty1"),
        GstOnSpDuty2 = Yes(r, "IsGSTAllowDuty2"),
        GstOnSpDuty3 = Yes(r, "IsGSTAllowDuty3"),
        MerchantExport = Yes(r, "MerchantExport"),
        Notification = S(r, "Notification"),
        AdvanceLicense = S(r, "AdvanceLicense"),
        GrandTotal = Dec(r, "TotalAmt"),
        Header = S(r, "Header"),
    };

    private static DomesticInvoiceLineDto MapLine(SqlDataReader r) => new()
    {
        NoOfPkg = S(r, "NoofPkg"),
        ItemCode = S(r, "Itemcode"),
        Commodity = S(r, "Commodity"),
        ItemDesc = S(r, "ItemDesc"),
        Deptt = S(r, "itemDeptt"),
        Uom = S(r, "Unit"),
        Hsn = S(r, "HSNCode"),
        Qty = Dec(r, "Qty"),
        Rate = Dec(r, "Rate"),
        Amount = Dec(r, "Amount"),
        CgstRate = Dec(r, "CGSTrate"),
        CgstAmt = Dec(r, "CGSTamt"),
        SgstRate = Dec(r, "SGSTRate"),
        SgstAmt = Dec(r, "SGSTAmt"),
        IgstRate = Dec(r, "IGSTRate"),
        IgstAmt = Dec(r, "IGSTAmt"),
        NetWt = Dec(r, "NetWt"),
        GrossWt = Dec(r, "GrossWt"),
        NonDisplayQty = Dec(r, "NonDisplayQty"),
        NonDisplayUnit = S(r, "NonDisplayUnit"),
        QtyRemarks = S(r, "QtyRemarks"),
        CommodityRemarks = S(r, "CommodityRemarks"),
        GroupName = S(r, "GroupCommodity"),
    };

    private static async Task<List<Dictionary<string, string?>>> DictionaryRows(SqlConnection con, string sql, CancellationToken ct, params SqlParameter[] ps)
    {
        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddRange(ps);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var rows = new List<Dictionary<string, string?>>();
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, string?>();
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i));
            rows.Add(row);
        }
        return rows;
    }

    private static async Task<List<string>> Strings(SqlConnection con, string sql, CancellationToken ct, params SqlParameter[] ps)
    {
        await using var cmd = new SqlCommand(sql, con);
        if (ps.Length > 0) cmd.Parameters.AddRange(ps);
        var list = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            if (!reader.IsDBNull(0)) list.Add(Convert.ToString(reader.GetValue(0)) ?? "");
        }
        return list;
    }

    private static async Task<List<T>> Rows<T>(SqlConnection con, string sql, CancellationToken ct, Func<SqlDataReader, T> map, params SqlParameter[] ps)
    {
        await using var cmd = new SqlCommand(sql, con);
        if (ps.Length > 0) cmd.Parameters.AddRange(ps);
        var list = new List<T>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) list.Add(map(reader));
        return list;
    }

    private static string ViewFilter(bool oel) =>
        oel
            ? "InvNo=@n AND companyname=@c AND suffix=@y"
            : "InvNo=@n AND companyname=@c AND (suffix=@y OR ARESuffix=@y)";

    private static async Task<string?> Scalar(SqlConnection con, string sql, CancellationToken ct, params SqlParameter[] ps) =>
        await Scalar(con, sql, ct, null, ps);

    private static async Task<string?> Scalar(SqlConnection con, string sql, CancellationToken ct, SqlTransaction? tx, params SqlParameter[] ps)
    {
        await using var cmd = new SqlCommand(sql, con, tx);
        if (ps.Length > 0) cmd.Parameters.AddRange(ps);
        var value = await cmd.ExecuteScalarAsync(ct);
        return value == null || value is DBNull ? null : Convert.ToString(value);
    }

    private static async Task<int> IntScalar(SqlConnection con, string sql, CancellationToken ct, params SqlParameter[] ps) =>
        await IntScalar(con, sql, ct, null, ps);

    private static async Task<int> IntScalar(SqlConnection con, string sql, CancellationToken ct, SqlTransaction? tx, params SqlParameter[] ps)
    {
        var raw = await Scalar(con, sql, ct, tx, ps);
        return int.TryParse(raw, out var n) ? n : 0;
    }

    private static async Task<bool> Exists(SqlConnection con, SqlTransaction tx, string sql, CancellationToken ct, params SqlParameter[] ps) =>
        await Scalar(con, sql, ct, tx, ps) != null;

    private static async Task Exec(SqlConnection con, SqlTransaction? tx, string sql, CancellationToken ct, params SqlParameter[] ps)
    {
        await using var cmd = new SqlCommand(sql, con, tx);
        cmd.Parameters.AddRange(ps);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static SqlParameter P(string name, object? value) =>
        new(name, value ?? DBNull.Value);

    private static string S(SqlDataReader r, string name)
    {
        var i = r.GetOrdinal(name);
        return r.IsDBNull(i) ? "" : Convert.ToString(r.GetValue(i)) ?? "";
    }

    private static decimal Dec(SqlDataReader r, string name)
    {
        var i = r.GetOrdinal(name);
        return r.IsDBNull(i) ? 0 : Convert.ToDecimal(r.GetValue(i));
    }

    private static decimal Dec(SqlDataReader r, int ordinal) =>
        r.IsDBNull(ordinal) ? 0 : Convert.ToDecimal(r.GetValue(ordinal));

    private static bool Yes(SqlDataReader r, string name) =>
        S(r, name).Equals("yes", StringComparison.OrdinalIgnoreCase);

    private static string ReadDate(SqlDataReader r, string name)
    {
        var i = r.GetOrdinal(name);
        if (r.IsDBNull(i)) return "";
        var value = r.GetValue(i);
        return value is DateTime dt ? dt.ToString("yyyy-MM-ddTHH:mm") : Convert.ToString(value) ?? "";
    }

    private static DateTime ParseDate(string raw)
    {
        if (DateTime.TryParse(raw, out var dt)) return dt;
        return DateTime.Today;
    }
}
