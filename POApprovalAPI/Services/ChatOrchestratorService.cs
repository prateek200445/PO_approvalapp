using System.Data;
using System.Text;
using System.Text.Json;
using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class ChatOrchestratorService
{
    private readonly SchemaRetrievalService _retrieval;
    private readonly GroqChatService _groq;
    private readonly SqlGuardService _sqlGuard;
    private readonly DatabaseService _database;
    private readonly ILogger<ChatOrchestratorService> _logger;

    private const int MaxReturnRows = 50;
    private const int SqlTimeoutSeconds = 30;

    public ChatOrchestratorService(
        SchemaRetrievalService retrieval,
        GroqChatService groq,
        SqlGuardService sqlGuard,
        DatabaseService database,
        ILogger<ChatOrchestratorService> logger)
    {
        _retrieval = retrieval;
        _groq = groq;
        _sqlGuard = sqlGuard;
        _database = database;
        _logger = logger;
    }

    public async Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Message is required.");

        var topK = request.TopK <= 0 ? 3 : Math.Clamp(request.TopK, 1, 5);
        var chunks = await _retrieval.RetrieveAsync(request.Message, topK, ct);
        if (chunks.Count == 0)
            throw new InvalidOperationException("No schema chunks retrieved.");

        var schemaBlock = BuildSchemaBlock(chunks);
        var sqlSystem = """
            You are a T-SQL expert for Microsoft SQL Server (database MaterialProcessing).
            Generate ONE read-only query that answers the user question.
            Rules:
            - Output ONLY the SQL (no markdown fences, no explanation).
            - SELECT or WITH...SELECT only. Never modify data.
            - Use only tables/views described in the provided schema context.
            - Use ONLY exact column names listed for each table. Do not invent or borrow columns from other tables.
            - FactoryInfo is OUR company/unit master only (Oswal Extrusion Limited, Plastene Polyfilms Limited, etc.). NEVER use FactoryInfo for supplier/vendor firm names (Chemline, Bright Rubber, Lohia, etc.) — those live in Vendor.
            - FactoryInfo PAN column is PermanentAccountNo (NOT PANNo). LedgerMaster PAN column is PANNo.
            - FactoryInfo GST prefer NewGSTNo. LedgerMaster has GSTNo/NewGSTNo for parties.
            - Supplier/vendor GST/PAN/email/address/city/bank/IFSC/MSME/vendor code: ALWAYS Vendor (full profile) or vw_VendorListwithBankdtls (bank/IFSC shortcut). NEVER FactoryInfo for vendors.
            - Ledger/account groups: use SELECT DISTINCT Under FROM LedgerMaster (filter empty Under). NEVER query LedgerGroupMaster.
            - Opening/pending ledger balances: use LedgerMaster.Openingbalance and LedgerMaster.PendingBalance. NEVER query LedgerOpeningBalance (table is empty).
            - MRN / material receipt / store inward: prefer Vw_StoreInwards or vw_MRNList; header table StoreInwardsPayment; lines StoreInwards. MRN number column is MRNo/MRno; payment link via vw_MRNToBillPayment or BillPaymentEntry.MRNno. Always TOP 50.
            - MRN company vs vendor: 'for company X' uses CompanyName. Vendor/supplier/party names use PartyName/Partyname. Names ending in -Purchase (e.g. Plastene Polyfilms Ltd-Purchase) are PartyName, not CompanyName.
            - NEVER invent MRDate. Vw_StoreInwards/StoreInwardsPayment: use BillDate, GateInwardDate, or SysDate. vw_MRNList/BillPaymentEntry: use MRNDate (not MRDate).
            - Payment against an MRN: prefer BillPaymentEntry WHERE MRNno = '<MRN>'. If using vw_MRNToBillPayment, require PaymentNo IS NOT NULL and DISTINCT PaymentNo/PaymentAmount. Never treat NULL PaymentNo lines as 'no payment'.
            - Receipts by supplier bill number: prefer Vw_StoreInwards WHERE BillNo = '<bill>'. Do not find store receipts via BillPaymentEntry.BillNo.
            - Users / email / full name: use loginentry.dbo.LoginRights (or loginentry..loginrights). Username column is Name. NEVER SELECT Password or SELECT *. Join PurchasePayment.LoginName / BillPaymentEntry.Loginname / ApprovePO.ApprovalName = LoginRights.Name.
            - Purchase requisition (PR): prefer Vw_PurchaseReq (Code is PR number, not IndentNo). Vendor quotations: prefer Vw_Quotation (FirmName, Rate, NegoRate, PurchaseCode). Never use empty ApproveQuotation. Vw_Quotation.StoreCode / Vw_IndentQuotation.Storecode = ApproveIndent.IndentNo.
            - Store outward / material issue: prefer StoreOutwards (company column CompName NOT CompanyName; IssueSlipNo, Qty, Deptt, IssueTo, WareHouse, sysDate). IssueSlipNo is varchar — always compare as a string literal e.g. IssueSlipNo = '215' (never IssueSlipNo = 215; unquoted ints convert every row and fail on values like 'OUT WOARD PENDING'). Item name on StoreOutwards is ItemName; on ItemInfo use itemdesc (not ItemName/ItemDesc). Phrases 'issue slip', 'what was issued', 'materials issued on slip' ALWAYS use StoreOutwards.IssueSlipNo — NEVER despatch views (vw_MISrolldespatch / Invno). Daily inward/outward by item: vw_ItemInwardOutward (companyname, Outwardqty). Monthly: vw_ItemMonthlyInwardOutward (Month, Year, OutwardQty). Join StoreOutwards.Itemcode = ItemInfo.itemcode (NOT ItemInfo.code). Skip WarehouseStoreoutwards. Always TOP 50.
            - 'Today' on store outward / vw_ItemInwardOutward / StoreOutwards: posting often lags calendar GETDATE(). Prefer the latest available business date for that company, e.g. CAST([Date] AS date) = (SELECT MAX(CAST([Date] AS date)) FROM vw_ItemInwardOutward WHERE companyname = '<company>') (or MAX(sysDate) on StoreOutwards). Select the date column so the answer can state which day was used. Only use bare GETDATE() if the user insists on calendar today and accepts empty.
            - Warehouse / stock-in-hand: prefer vw_itemwiseStock (CompanyName, ItemCode, StkInHand, Warehousename) or WareHouse (also Minlevel/Maxlevel/ReOrder). Groups/dept: vw_inventoryitemwarehouse_all. Godown list: WareHouseMaster. Company column is CompanyName (not CompName). Join ItemCode = ItemInfo.itemcode. Below reorder: StkInHand < ReOrder AND ReOrder > 0 on WareHouse. Skip broken vw_ItemStockLedger. Always TOP 50.
            - Debit notes (purchase/vendor): prefer vw_DebitNote or DebitNote (DebitNoteNumber, TotalDebitAmount, PartyName=vendor, CompanyName=ours, DebitType, BillNo, MRNo). Credit notes (sales/customer): prefer vw_creditnote or CreditNote (CreditNoteNumber, TotalCreditAmount/totalcreditamount, PartyName=customer, CompanyName=ours). Number pattern: codes with /DB/ are ALWAYS debit notes (DebitNoteNumber on DebitNote/vw_DebitNote) — e.g. OEL/DB/26-27/16. Codes with /CR/ are ALWAYS credit notes (CreditNoteNumber). NEVER query vw_creditnote for a /DB/ number. 'debit notes for company X' / 'against MRN for Oswal/polyfilms/...' → CompanyName = full legal company name for ANY plant (e.g. 'Oswal Extrusion Limited', NEVER partial 'Oswal') — NEVER MRNo = company nickname; do NOT join StoreInwardsPayment just to filter company on debit notes — filter vw_DebitNote.CompanyName directly. If user gives a real MRN number, filter MRNo = that number. CRITICAL credit-note mapping: 'polyfilms credit notes to commercial bag' → companyname = 'Plastene Polyfilms Limited' AND partyname = 'Commercial Bag Company'. NEVER put 'Plastene Polyfilms Ltd-Purchase' into credit-note filters wrongly. Names ending -Purchase are vendors. Line item tables are sparse — use headers unless user asks for items. MRNo joins StoreInwardsPayment. Do not join DebitNote.PONo to PurchasePayment. Bracket [Company Address] on vw_creditnote. Always TOP 50.
            - Vendor master: prefer Vendor (FirmName, VendorCode like Ven00171, NewGSTNo NOT GSTNo, PANNo like AACFB1249A, Email, bank IFSC, PaymentTerms, ISMSME). Bank shortcut: vw_VendorListwithBankdtls. Filter FirmName with LIKE '%name%'. LedgerName mapping: vendordata. Join Vendor.FirmName/VendorCode to Vw_PurchaseOrder and Vw_Quotation. CRITICAL pending/opening balance for a vendor: use LedgerMaster WHERE LedgerName LIKE '%FirmName%' (or PANNo = Vendor.PANNo real tax PAN). NEVER put VendorCode (Ven#####) into LedgerMaster.PANNo — Ven00171 is not a PAN. Vendor is profile/bank/MSME only, not ledger balances. Vendor-item rates: prefer VendorRate (filter FirmName or ItemCode + TOP 50); Vw_VendorItem is slim but ~14M — same mandatory filters. For a specific quotation/PO use Vw_Quotation. Always TOP 50.
            - Gate pass: returnable RGP prefer Vw_ReturnGatePass (GatePassNo format CO/yy-yy/GP/n e.g. KPV/26-27/GP/162, OEL/26-27/GP/n for Oswal — NEVER reversed like 162/KPV). Filter company via CompName LIKE '%Oswal%' OR GatePassNo LIKE 'OEL%/GP/%' (prefixes: Oswal=OEL, KP Woven=KPV, Polyfilms=PPL). Non-returnable NRGP prefer Vw_NonReturnGatePass (.../NGP/...). Inward against RGP: InwdReturnGatePass (.../IGP/...). Pending/open returns: vw_returngatepasspending WHERE PendingQty > 0. CompName NOT CompanyName. Always TOP 50.
            - Job work: formal orders prefer Vw_EditJOBWorkOrder (PurchaseCode JRO/JWO; sparse). Live qty at job work: VW_JobWork_EBD_DTL (filter companyname/ItemCode). Receipts: VW_RECJOBWORK_EBD_DTL (MRNo like JBIN-SE). Returnable job-work sends also Vw_ReturnGatePass Purpose LIKE '%Job Work%'. Do not join JOBWORKORDER to PurchasePayment. Always TOP 50.
            - Sales invoices: prefer vw_Salesvoucher (InvNo, BuyerName, BillAMount, InvType, CompanyName). Lines: SalesVoucherItem on CompanyName+InvNo (ITEMCODE, ActualQty, Rate, Amount). List: vw_SalesInvList. Taxes: SalesVoucherTax. MIS qty: VW_SALES_EBD_DTL. Bracket [Company Address]/[Company GST] on vw_Salesvoucher. Sales credit notes: CreditNote. Always TOP 50.
            - Despatch/packing: roll history vw_MISrolldespatch; FIBC bails FIBCDespatch; yarn MIS_YarnDespatch; small bag SmallBagBailForDespatch; rolls waiting vw_RollforDespatch. ALWAYS filter CompanyName/Companyname or InvNo/PartyName/date + TOP 50 (million-row tables). Prefer view over MISRollforDespatch table.
            - Production: factory daily vw_FactoryProduction (companyname, Particulars, TapeProduction/Fabric/SmallBag); tape plant vw_daily_tape_prod_New (bracket [Loom Dept]/[FIBC Dept]); loom rolls vw_LoomProductionENtry (MUST filter CompanyName/Sysdate/LoomNo + TOP 50 — ~716k; skip stale vw_Loom_Prod_Mtr); FIBC bags VW_FIBCBagwiseProduction (not _New); MIS qty VW_PRODUCTION_EBD_DTL; WIP vw_WIPReport; small bags SmallBagProductionEntry. Filter EBD/WIP/loom + TOP 50. Not despatch / not ApproveWorkOrder.
            - Prefer TOP 50 for detail lists. COUNT aggregates need no TOP.
            - Pending filters: status = 'Pending' or Status = 'Pending' (match column casing in schema).
            - Approved counts: status LIKE 'Approved%' when statuses vary.
            - Use correct joins from the schema notes.
            """;

        var sqlUser = $"""
            Schema context:
            {schemaBlock}

            User question:
            {request.Message}
            """;

        var sqlRaw = await _groq.CompleteAsync(sqlSystem, sqlUser, ct);
        var sql = _sqlGuard.NormalizeAndValidate(sqlRaw);
        sql = ApplyKnownColumnFixes(sql);

        List<Dictionary<string, object?>> rows;
        string? warning = null;
        try
        {
            rows = await ExecuteReadOnlyAsync(sql, ct);
        }
        catch (Exception ex)
        {
            if (await TryGovernedVendorProfileRewriteAsync(request.Message, sql, ct) is { } governed)
            {
                _logger.LogWarning(ex, "SQL failed for vendor profile; using governed Vendor rewrite");
                sql = governed.Sql;
                rows = governed.Rows;
                warning = governed.Warning;
            }
            else
            {
            _logger.LogWarning(ex, "SQL execution failed; asking model to repair once");
            var repairUser = $"""
                The previous SQL failed on SQL Server.

                Question: {request.Message}

                Schema context:
                {schemaBlock}

                Failed SQL:
                {sql}

                Error:
                {ex.Message}

                Fix using ONLY exact column names from the schema for the tables you query.
                Reminder: FactoryInfo uses PermanentAccountNo (not PANNo); LedgerMaster uses PANNo.
                Reminder: ledger/account groups use DISTINCT LedgerMaster.Under — never LedgerGroupMaster.
                Reminder: opening/pending balances use LedgerMaster.Openingbalance/PendingBalance — never LedgerOpeningBalance.
                Reminder: MRN 'for company X' uses CompanyName; PartyName is vendor only; *-Purchase names are PartyName.
                Reminder: NEVER use MRDate. Use BillDate/GateInwardDate on Vw_StoreInwards, or MRNDate on vw_MRNList.
                Reminder: payment-against-MRN prefer BillPaymentEntry.MRNno; if vw_MRNToBillPayment then PaymentNo IS NOT NULL.
                Reminder: receipts by bill number use Vw_StoreInwards.BillNo — not BillPaymentEntry.BillNo.
                Reminder: users/email use loginentry.dbo.LoginRights; NEVER SELECT Password; username column is Name.
                Reminder: PR uses Vw_PurchaseReq; vendor quotes use Vw_Quotation — never empty ApproveQuotation.
                Reminder: StoreOutwards uses CompName (not CompanyName); IssueSlipNo is varchar — use IssueSlipNo = '215' not = 215; 'issue slip' is StoreOutwards never despatch Invno; ItemInfo description column is itemdesc; 'today' on outward views prefer MAX(Date)/MAX(sysDate) for that company not bare GETDATE(); daily view is vw_ItemInwardOutward.companyname/Outwardqty.
                Reminder: stock-in-hand uses WareHouse/vw_itemwiseStock with CompanyName and StkInHand; reorder on WareHouse.ReOrder; not CompName.
                Reminder: debit notes = DebitNote/vw_DebitNote (CompanyName=ours full name e.g. Oswal Extrusion Limited not 'Oswal', PartyName=vendor); /DB/ numbers e.g. OEL/DB/26-27/16 are debit NEVER credit; NEVER put company names into MRNo; do not join StoreInwardsPayment to filter company on debit notes; /CR/ numbers are credit; credit notes = CreditNote/vw_creditnote (CompanyName=ours e.g. Plastene Polyfilms Limited, PartyName=customer e.g. Commercial Bag Company); never use *-Purchase as credit CompanyName; do not join DebitNote.PONo to PurchasePayment.
                Reminder: FactoryInfo is our companies only — NEVER FactoryInfo for supplier/vendor names; vendor GST column is NewGSTNo (NOT GSTNo) on Vendor/vw_VendorListwithBankdtls; vendor GST/PAN/email/city/bank/MSME use Vendor or vw_VendorListwithBankdtls with FirmName LIKE '%name%'; pending/opening balance use LedgerMaster by LedgerName or real PANNo — NEVER LedgerMaster.PANNo = VendorCode (Ven#####); vendor rates use VendorRate/Vw_VendorItem with FirmName or ItemCode filter + TOP 50 (never unfiltered).
                Reminder: gate pass uses CompName (not CompanyName); GatePassNo format CO/yy-yy/GP/n e.g. KPV/26-27/GP/162 (never 162/KPV); messy rgp 162 kpv → LIKE '%KPV%GP/162'; RGP=Vw_ReturnGatePass; NRGP=Vw_NonReturnGatePass; IGP=InwdReturnGatePass; pending=vw_returngatepasspending PendingQty>0.
                Reminder: job work live qty=VW_JobWork_EBD_DTL; receipts=VW_RECJOBWORK_EBD_DTL; formal orders=Vw_EditJOBWorkOrder (sparse); not PurchasePayment.
                Reminder: sales invoices=vw_Salesvoucher + SalesVoucherItem (CompanyName+InvNo); BuyerName is customer; BillAMount spelling; bracket spaced GST address cols.
                Reminder: despatch=vw_MISrolldespatch/FIBCDespatch/MIS_YarnDespatch/SmallBagBailForDespatch — must filter company or inv/party/date + TOP 50.
                Reminder: production=vw_FactoryProduction / vw_daily_tape_prod_New / vw_LoomProductionENtry / VW_FIBCBagwiseProduction / VW_PRODUCTION_EBD_DTL / vw_WIPReport — filter company/date/item on large views + TOP 50; skip stale loom meter views; not despatch.
                Return ONE corrected SELECT/WITH query only. No explanation.
                """;
            sqlRaw = await _groq.CompleteAsync(sqlSystem, repairUser, ct);
            sql = ApplyKnownColumnFixes(_sqlGuard.NormalizeAndValidate(sqlRaw));
            try
            {
                rows = await ExecuteReadOnlyAsync(sql, ct);
            }
            catch (Exception repairEx)
            {
                if (await TryGovernedVendorProfileRewriteAsync(request.Message, sql, ct) is { } governedRepair)
                {
                    _logger.LogWarning(repairEx, "SQL repair failed; using governed Vendor rewrite");
                    sql = governedRepair.Sql;
                    rows = governedRepair.Rows;
                    warning = governedRepair.Warning;
                }
                else
                {
                    _logger.LogError(repairEx, "SQL repair still failed");
                    throw new InvalidOperationException(
                        $"SQL failed after repair: {repairEx.Message}. Last SQL: {sql}", repairEx);
                }
            }
            }
        }

        if (warning is null)
        {
        if (rows.Count == 0 && sql.Contains("LedgerGroupMaster", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Empty LedgerGroupMaster result; rewriting to LedgerMaster.Under");
            sql = """
                SELECT DISTINCT TOP 50 Under AS GroupName
                FROM LedgerMaster
                WHERE ISNULL(Under, '') <> ''
                ORDER BY Under
                """;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = "Rewrote empty LedgerGroupMaster query to LedgerMaster.Under (governed).";
        }
        else if (rows.Count == 0 && LooksLikeLedgerGroupQuestion(request.Message)
                 && !sql.Contains("Under", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Empty result for ledger-group intent; rewriting to LedgerMaster.Under");
            sql = """
                SELECT DISTINCT TOP 50 Under AS GroupName
                FROM LedgerMaster
                WHERE ISNULL(Under, '') <> ''
                ORDER BY Under
                """;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = "Rewrote ledger-group question to DISTINCT LedgerMaster.Under (governed).";
        }
        else if (sql.Contains("LedgerOpeningBalance", StringComparison.OrdinalIgnoreCase)
                 || (rows.Count == 0 && LooksLikeOpeningPendingBalanceQuestion(request.Message)
                     && !sql.Contains("PendingBalance", StringComparison.OrdinalIgnoreCase)
                     && !sql.Contains("Openingbalance", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("LedgerOpeningBalance is empty; rewriting to LedgerMaster balances");
            var company = TryExtractCompanyName(request.Message);
            sql = string.IsNullOrWhiteSpace(company)
                ? """
                    SELECT TOP 50 CompanyName, LedgerName, Openingbalance, PendingBalance
                    FROM LedgerMaster
                    WHERE ISNULL(PendingBalance, 0) <> 0 OR ISNULL(Openingbalance, 0) <> 0
                    ORDER BY ABS(ISNULL(PendingBalance, 0)) DESC
                    """
                : $"""
                    SELECT TOP 50 CompanyName, LedgerName, Openingbalance, PendingBalance
                    FROM LedgerMaster
                    WHERE CompanyName = '{EscapeSqlLiteral(company)}'
                      AND (ISNULL(PendingBalance, 0) <> 0 OR ISNULL(Openingbalance, 0) <> 0)
                    ORDER BY ABS(ISNULL(PendingBalance, 0)) DESC
                    """;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = "Rewrote LedgerOpeningBalance (empty table) to LedgerMaster Openingbalance/PendingBalance (governed).";
        }
        else if (sql.Contains("ApproveQuotation", StringComparison.OrdinalIgnoreCase)
                 || request.Message.Contains("ApproveQuotation", StringComparison.OrdinalIgnoreCase)
                 || (rows.Count == 0 && LooksLikeVendorQuotationQuestion(request.Message)
                     && !sql.Contains("Vw_Quotation", StringComparison.OrdinalIgnoreCase)
                     && !sql.Contains("FinalQuotation", StringComparison.OrdinalIgnoreCase)
                     && !sql.Contains("Vw_IndentQuotation", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("ApproveQuotation empty / wrong path; rewriting to Vw_Quotation");
            sql = """
                SELECT TOP 50 PurchaseCode, FirmName, ItemCode, ItemDesc, Qty, Rate, NegoRate, Total, StoreCode, Sysdate
                FROM Vw_Quotation
                ORDER BY Sysdate DESC
                """;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = "Rewrote ApproveQuotation (empty table) to recent Vw_Quotation rows (governed).";
        }
        else if (rows.Count == 0
                 && LooksLikeMrnReceivingCompanyIntent(request.Message)
                 && LooksLikeMrnSql(sql)
                 && HasVendorPartyFilterWithoutCompany(sql))
        {
            _logger.LogWarning("Empty MRN result with PartyName filter; rewriting PartyName/Partyname -> CompanyName");
            sql = RewriteMrnPartyFilterToCompanyName(sql);
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = "Rewrote MRN vendor PartyName filter to receiving CompanyName (governed).";
        }
        else if (rows.Count == 0
                 && LooksLikeMrnSql(sql)
                 && TryRewriteEmptyCompanyFilterToParty(sql, out var partySql))
        {
            _logger.LogWarning("Empty MRN CompanyName filter; retrying same literal as PartyName");
            var partyRows = await ExecuteReadOnlyAsync(partySql, ct);
            if (partyRows.Count > 0)
            {
                sql = partySql;
                rows = partyRows;
                warning = "Rewrote empty CompanyName filter to PartyName (name matched a vendor/party).";
            }
        }
        else if (LooksLikeMrnPaymentQuestion(request.Message)
                 && TryResolveMrnNumber(request.Message, sql) is { } mrn
                 && ShouldRewriteMrnPaymentQuery(sql, rows))
        {
            _logger.LogWarning("Rewriting MRN payment query to BillPaymentEntry for {Mrn}", mrn);
            sql = $"""
                SELECT TOP 50 PaymentNo, MRNno, BillNo, PaymentAmount, BillAmount, UTRno, status, isPaid, IsCancel
                FROM BillPaymentEntry
                WHERE MRNno = '{EscapeSqlLiteral(mrn)}'
                ORDER BY PaymentAmount DESC
                """;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = "Rewrote MRN payment question to BillPaymentEntry by MRNno (governed; avoids null line rows on vw_MRNToBillPayment).";
        }
        else if (LooksLikeReceiptByBillQuestion(request.Message)
                 && TryExtractBillNo(request.Message) is { } billNo
                 && ShouldRewriteReceiptByBill(sql, rows))
        {
            _logger.LogWarning("Rewriting receipt-by-bill query to Vw_StoreInwards for {BillNo}", billNo);
            sql = $"""
                SELECT TOP 50 MRNo, CompanyName, PartyName, BillNo, BillDate, ItemName, ItemCode,
                       RecdQty, AcceptedQty, PendingQty, PONo, GateInwardNo, Amount
                FROM Vw_StoreInwards
                WHERE BillNo = '{EscapeSqlLiteral(billNo)}'
                ORDER BY BillDate DESC
                """;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = "Rewrote receipt-by-bill question to Vw_StoreInwards.BillNo (governed; not BillPaymentEntry.BillNo).";
        }
        else if (LooksLikeIssueSlipQuestion(request.Message)
                 && TryExtractIssueSlipNo(request.Message) is { } slipNo
                 && (ShouldRewriteIssueSlipQuery(sql)
                     || (rows.Count == 0 && ResolveOutwardCompanyAlias(request.Message) is not null)))
        {
            var slip = slipNo;
            var company = ResolveOutwardCompanyAlias(request.Message) ?? TryExtractCompanyName(request.Message);
            _logger.LogWarning("Rewriting issue-slip question to StoreOutwards slip {Slip} company {Company}", slip, company);
            sql = string.IsNullOrWhiteSpace(company)
                ? $"""
                    SELECT TOP 50 IssueSlipNo, CompName, Itemcode, ItemName, Qty, Deptt, IssueTo, WareHouse, sysDate
                    FROM StoreOutwards
                    WHERE IssueSlipNo = '{EscapeSqlLiteral(slip)}'
                    ORDER BY sysDate DESC
                    """
                : $"""
                    SELECT TOP 50 IssueSlipNo, CompName, Itemcode, ItemName, Qty, Deptt, IssueTo, WareHouse, sysDate
                    FROM StoreOutwards
                    WHERE IssueSlipNo = '{EscapeSqlLiteral(slip)}'
                      AND CompName = '{EscapeSqlLiteral(company)}'
                    ORDER BY sysDate DESC
                    """;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = "Rewrote 'issue slip' question to StoreOutwards.IssueSlipNo with canonical CompName (governed).";
        }
        else if (LooksLikeGatePassQuestion(request.Message)
                 && TryExtractGatePassRef(request.Message, sql) is { } gpRef
                 && ShouldRewriteGatePassQuery(sql, rows))
        {
            _logger.LogWarning(
                "Rewriting gate-pass query to {Prefix}/{Kind}/{Serial} LIKE match",
                gpRef.Prefix, gpRef.PassKind, gpRef.Serial);
            sql = BuildGatePassSql(gpRef);
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning =
                $"Rewrote gate-pass number to {gpRef.Prefix}/.../{gpRef.PassKind}/{gpRef.Serial} LIKE match (governed; fixed reversed/malformed GatePassNo).";
        }
        else if (rows.Count == 0
                 && LooksLikeGatePassQuestion(request.Message)
                 && TryExtractGatePassRef(request.Message, sql) is null
                 && TryResolveGatePassCompanyListRewrite(request.Message, sql) is { } gpList)
        {
            _logger.LogWarning(
                "Empty gate-pass company list; rewriting to prefix/LIKE for {Keyword}/{Prefix}",
                gpList.CompanyKeyword, gpList.Prefix);
            sql = BuildGatePassCompanyListSql(gpList);
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning =
                "Rewrote empty gate-pass company filter to CompName LIKE + GatePassNo prefix match (governed).";
        }
        else if (rows.Count == 0
                 && LooksLikeTodayOutwardQuestion(request.Message)
                 && UsesCalendarTodayOnOutwardSql(sql))
        {
            var company = ResolveOutwardCompanyAlias(request.Message)
                          ?? TryExtractCompanyName(request.Message)
                          ?? "Oswal Extrusion Limited";
            _logger.LogWarning("Empty GETDATE() outward result; rewriting to latest business date for {Company}", company);
            if (sql.Contains("StoreOutwards", StringComparison.OrdinalIgnoreCase)
                && !sql.Contains("vw_ItemInwardOutward", StringComparison.OrdinalIgnoreCase))
            {
                sql = $"""
                    SELECT TOP 50 Itemcode, ItemName, Qty, IssueTo, Deptt, IssueSlipNo, sysDate
                    FROM StoreOutwards
                    WHERE CompName = '{EscapeSqlLiteral(company)}'
                      AND CAST(sysDate AS date) = (
                          SELECT MAX(CAST(sysDate AS date)) FROM StoreOutwards
                          WHERE CompName = '{EscapeSqlLiteral(company)}')
                    ORDER BY Qty DESC
                    """;
            }
            else
            {
                sql = $"""
                    SELECT TOP 50 ItemCode, ItemName, InwardQty, Outwardqty, [Date]
                    FROM vw_ItemInwardOutward
                    WHERE companyname = '{EscapeSqlLiteral(company)}'
                      AND CAST([Date] AS date) = (
                          SELECT MAX(CAST([Date] AS date)) FROM vw_ItemInwardOutward
                          WHERE companyname = '{EscapeSqlLiteral(company)}')
                      AND ISNULL(Outwardqty, 0) <> 0
                    ORDER BY Outwardqty DESC
                    """;
            }
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = "Rewrote empty calendar-today outward filter to latest available business date for that company (governed).";
        }
        else if (TryExtractDebitOrCreditNoteNumber(request.Message) is { } noteNum
                 && ShouldRewriteDebitCreditNoteByNumber(noteNum, sql, rows))
        {
            _logger.LogWarning("Rewriting note {Note} to correct debit/credit object", noteNum);
            if (noteNum.Contains("/DB/", StringComparison.OrdinalIgnoreCase))
            {
                sql = $"""
                    SELECT TOP 50 DebitNoteNumber, CompanyName, PartyName, TotalDebitAmount, DebitType, BillNo, MRNo, sysdate
                    FROM vw_DebitNote
                    WHERE DebitNoteNumber = '{EscapeSqlLiteral(noteNum)}'
                    """;
                warning = "Rewrote /DB/ note number to vw_DebitNote.DebitNoteNumber (governed; not credit note).";
            }
            else
            {
                sql = $"""
                    SELECT TOP 50 creditnotenumber, companyname, partyname, totalcreditamount, credittype, creditnotedate, invno
                    FROM vw_creditnote
                    WHERE creditnotenumber = '{EscapeSqlLiteral(noteNum)}'
                    """;
                warning = "Rewrote /CR/ note number to vw_creditnote.creditnotenumber (governed; not debit note).";
            }
            rows = await ExecuteReadOnlyAsync(sql, ct);
        }
        else if (rows.Count == 0
                 && LooksLikeDebitNoteSql(sql)
                 && LooksLikeDebitNoteQuestion(request.Message)
                 && TryResolveDebitNoteCompany(request.Message, sql) is { } debitCompany
                 && ShouldRewriteEmptyDebitCompanyQuery(sql, debitCompany))
        {
            _logger.LogWarning(
                "Empty debit-note result with bad company/MRN filter; rewriting to CompanyName={Company}",
                debitCompany);
            sql = $"""
                SELECT TOP 50 DebitNoteNumber, CompanyName, PartyName, DebitType, TotalDebitAmount, BillNo, MRNo, sysdate
                FROM vw_DebitNote
                WHERE CompanyName = '{EscapeSqlLiteral(debitCompany)}'
                ORDER BY sysdate DESC
                """;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning =
                "Rewrote debit-note query to vw_DebitNote with full CompanyName (governed; no partial name / no StoreInwardsPayment join for company filter).";
        }
        else if (rows.Count == 0
                 && LooksLikeCreditNoteSql(sql)
                 && LooksLikeCreditNoteQuestion(request.Message)
                 && TryBuildCreditNoteCompanyPartySql(request.Message, out var creditSql))
        {
            _logger.LogWarning("Empty credit-note result; rewriting CompanyName/PartyName mapping");
            sql = creditSql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = "Rewrote credit-note filters: CompanyName=our company, PartyName=customer (governed; fixed swapped/polyfilms-Purchase mistake).";
        }
        else if (rows.Count == 0
                 && sql.Contains("LedgerMaster", StringComparison.OrdinalIgnoreCase)
                 && (LooksLikeVendorPendingBalanceQuestion(request.Message)
                     || LedgerSqlHasVendorCodeAsPan(sql))
                 && await TryResolveVendorFirmNameForBalanceAsync(request.Message, sql, ct) is { } vendorFirm)
        {
            _logger.LogWarning(
                "Empty LedgerMaster balance with VendorCode-as-PAN; rewriting to LedgerName={Firm}",
                vendorFirm);
            sql = $"""
                SELECT TOP 50 CompanyName, LedgerName, PANNo, PendingBalance, Openingbalance
                FROM LedgerMaster
                WHERE LedgerName LIKE '%{EscapeSqlLiteral(vendorFirm)}%'
                ORDER BY ABS(ISNULL(PendingBalance, 0)) DESC, ABS(ISNULL(Openingbalance, 0)) DESC
                """;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning =
                "Rewrote vendor balance query: LedgerMaster by LedgerName (governed; VendorCode Ven##### is not PANNo).";
        }
        else if (rows.Count == 0
                 && LooksLikeVendorProfileQuestion(request.Message)
                 && ShouldRewriteVendorProfileQuery(sql)
                 && await TryResolveVendorFirmNameForProfileAsync(request.Message, sql, ct) is { } vendorProfileFirm)
        {
            _logger.LogWarning(
                "Empty vendor profile query; rewriting to Vendor FirmName LIKE for {Firm}",
                vendorProfileFirm);
            sql = BuildVendorProfileSql(vendorProfileFirm, LooksLikeVendorBankOnlyQuestion(request.Message));
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = sql.Contains("FactoryInfo", StringComparison.OrdinalIgnoreCase)
                ? "Rewrote vendor profile query: FactoryInfo → Vendor (governed; suppliers are not in FactoryInfo)."
                : "Rewrote vendor profile query: exact FirmName → Vendor LIKE match (governed).";
        }
        }

        var truncated = rows.Count > MaxReturnRows;
        if (truncated)
            rows = rows.Take(MaxReturnRows).ToList();

        var preview = JsonSerializer.Serialize(rows);
        if (preview.Length > 12000)
            preview = preview[..12000] + "...(truncated)";

        var answerSystem = """
            You answer business questions using ONLY the SQL result data provided.
            Be concise and factual. If the result is empty, say so.
            Do not invent numbers. Mention key figures clearly.
            For payment questions: ignore rows where PaymentNo is null; only null/empty after filtering means no payment.
            If multiple payment rows exist, list each PaymentNo with amount and give a total when useful.
            For receipt/bill questions: if multiple distinct MRNo/SrNo values appear, say how many distinct receipts and list them — do not claim a single receipt when several exist.
            """;
        var answerUser = $"""
            Question: {request.Message}

            SQL used:
            {sql}

            Result rows (JSON):
            {preview}

            Write a short natural-language answer.
            """;

        var answer = await _groq.CompleteAsync(answerSystem, answerUser, ct);

        if (truncated)
            warning = string.IsNullOrEmpty(warning)
                ? $"Result truncated to {MaxReturnRows} rows."
                : warning + $" Result truncated to {MaxReturnRows} rows.";

        return new ChatResponse
        {
            Answer = answer,
            Sql = sql,
            TablesUsed = chunks.Select(c => new RetrievedTableDto
            {
                ObjectName = c.ObjectName,
                Domain = c.Domain ?? "",
                Score = Math.Round(c.Score, 4)
            }).ToList(),
            Rows = rows,
            RowCount = rows.Count,
            Warning = warning
        };
    }

    private static bool LooksLikeVendorQuotationQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("quotation") || m.Contains("quoted") || m.Contains("quote")
               || m.Contains("approvequotation") || m.Contains("nego rate") || m.Contains("vendor quote");
    }

    private static bool LooksLikeLedgerGroupQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("ledger group") || m.Contains("account group") || m.Contains("ledger groups")
                || m.Contains("account groups") || m.Contains("ledgergroupmaster")
                || (m.Contains("groups") && m.Contains("ledger")));
    }

    private static bool LooksLikeOpeningPendingBalanceQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("ledgeropeningbalance")) return true;
        var mentionsBalance = m.Contains("opening") || m.Contains("pending") || m.Contains("outstanding");
        var mentionsLedgerContext = m.Contains("ledger") || m.Contains("bill") || m.Contains("balance");
        return mentionsBalance && mentionsLedgerContext;
    }

    private sealed record GovernedVendorProfileResult(
        string Sql,
        List<Dictionary<string, object?>> Rows,
        string Warning);

    private async Task<GovernedVendorProfileResult?> TryGovernedVendorProfileRewriteAsync(
        string message,
        string failedSql,
        CancellationToken ct)
    {
        if (!LooksLikeVendorProfileQuestion(message)) return null;
        if (await TryResolveVendorFirmNameForProfileAsync(message, failedSql, ct) is not { } firm) return null;

        var rewritten = BuildVendorProfileSql(firm, LooksLikeVendorBankOnlyQuestion(message));
        var rows = await ExecuteReadOnlyAsync(rewritten, ct);
        var warning = failedSql.Contains("FactoryInfo", StringComparison.OrdinalIgnoreCase)
            ? "Governed vendor profile rewrite: FactoryInfo → Vendor (suppliers are not in FactoryInfo)."
            : "Governed vendor profile rewrite: Vendor FirmName LIKE match (fixed GSTNo/exact-name failures).";
        return new GovernedVendorProfileResult(rewritten, rows, warning);
    }

    private static bool ShouldRewriteVendorProfileQuery(string sql)
    {
        if (sql.Contains("FactoryInfo", StringComparison.OrdinalIgnoreCase)) return true;
        if (sql.Contains("vendordata", StringComparison.OrdinalIgnoreCase)) return true;

        if (!sql.Contains("Vendor", StringComparison.OrdinalIgnoreCase)
            && !sql.Contains("vw_VendorListwithBankdtls", StringComparison.OrdinalIgnoreCase))
            return false;

        // Exact FirmName = 'Chemline India' often misses 'Chemline India Ltd' — retry with LIKE.
        if (System.Text.RegularExpressions.Regex.IsMatch(
                sql,
                @"\bFirmName\s*=\s*'",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return true;

        return System.Text.RegularExpressions.Regex.IsMatch(
            sql,
            @"\bFirmName\s+LIKE\s+'([^']*)'",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            && !System.Text.RegularExpressions.Regex.IsMatch(
                sql,
                @"\bFirmName\s+LIKE\s+'%[^']+%'",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static string? TryExtractVendorFirmNameFilter(string sql)
    {
        var eq = System.Text.RegularExpressions.Regex.Match(
            sql,
            @"\bFirmName\s*=\s*'([^']*)'",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (eq.Success && !string.IsNullOrWhiteSpace(eq.Groups[1].Value))
            return eq.Groups[1].Value.Trim();

        var like = System.Text.RegularExpressions.Regex.Match(
            sql,
            @"\bFirmName\s+LIKE\s+'%([^']*)%'",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return like.Success && !string.IsNullOrWhiteSpace(like.Groups[1].Value)
            ? like.Groups[1].Value.Trim()
            : null;
    }

    private static bool LooksLikeVendorProfileQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        var hasProfileField = m.Contains("gst")
                              || m.Contains("pan")
                              || m.Contains("email")
                              || m.Contains("ifsc")
                              || m.Contains("bank")
                              || m.Contains("address")
                              || m.Contains(" city")
                              || m.StartsWith("city")
                              || m.Contains("msme")
                              || m.Contains("vendor code")
                              || m.Contains("payment term")
                              || m.Contains("contact")
                              || m.Contains("phone")
                              || m.Contains("tel no")
                              || m.Contains("telephone");
        if (!hasProfileField) return false;

        if (ResolveVendorFirmAlias(message) is not null) return true;
        if (m.Contains("vendor") || m.Contains("supplier") || m.Contains("firm name")) return true;
        return TryExtractFirmNameBeforeProfileFields(message) is not null;
    }

    private static bool LooksLikeVendorBankOnlyQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return (m.Contains("ifsc") || m.Contains("bank account") || m.Contains("bank details"))
               && !m.Contains("gst")
               && !m.Contains("pan")
               && !m.Contains("email")
               && !m.Contains(" city")
               && !m.Contains("address");
    }

    private static string? TryExtractFirmNameBeforeProfileFields(string message)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"^\s*(.+?)\s+(?:gst|pan|email|ifsc|bank|address|city|msme|vendor\s*code|payment\s*term|contact|phone|tel)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!m.Success) return null;

        var cand = m.Groups[1].Value.Trim();
        if (cand.Length < 3
            || cand.Equals("vendor", StringComparison.OrdinalIgnoreCase)
            || cand.Equals("supplier", StringComparison.OrdinalIgnoreCase)
            || cand.Equals("what", StringComparison.OrdinalIgnoreCase)
            || cand.Equals("show", StringComparison.OrdinalIgnoreCase)
            || cand.Equals("the", StringComparison.OrdinalIgnoreCase))
            return null;
        return cand;
    }

    private static string? TryExtractFactoryInfoNameFilter(string sql)
    {
        var eq = System.Text.RegularExpressions.Regex.Match(
            sql,
            @"\bName\s*=\s*'([^']*)'",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (eq.Success && !string.IsNullOrWhiteSpace(eq.Groups[1].Value))
            return eq.Groups[1].Value.Trim();

        var like = System.Text.RegularExpressions.Regex.Match(
            sql,
            @"\bName\s+LIKE\s+'%([^']*)%'",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return like.Success && !string.IsNullOrWhiteSpace(like.Groups[1].Value)
            ? like.Groups[1].Value.Trim()
            : null;
    }

    private static string BuildVendorProfileSql(string firmName, bool bankOnly)
    {
        var escaped = EscapeSqlLiteral(firmName);
        if (bankOnly)
        {
            return $"""
                SELECT TOP 50 FirmName, NewGSTNo, BankAccountNo, BankBranchName, IFSCCode, PaymentTerms, ModeofPayment
                FROM vw_VendorListwithBankdtls
                WHERE FirmName LIKE '%{escaped}%'
                """;
        }

        return $"""
            SELECT TOP 50 FirmName, VendorCode, NewGSTNo, PANNo, Email, ContactName, Address, City, State, PINCode,
                   BankAccountNo, BankName, BankBranchName, IFSCCode, PaymentTerms, ISMSME, MSMENumber
            FROM Vendor
            WHERE FirmName LIKE '%{escaped}%'
            """;
    }

    private async Task<string?> TryResolveVendorFirmNameForProfileAsync(
        string message,
        string sql,
        CancellationToken ct)
    {
        foreach (var cand in new[]
                 {
                     ResolveVendorFirmAlias(message),
                     TryExtractFirmNameBeforeProfileFields(message),
                     TryExtractFactoryInfoNameFilter(sql),
                     TryExtractVendorFirmNameFilter(sql)
                 })
        {
            if (string.IsNullOrWhiteSpace(cand)) continue;
            if (await VendorFirmExistsAsync(cand, ct)) return cand;
        }

        return null;
    }

    private async Task<bool> VendorFirmExistsAsync(string firmFragment, CancellationToken ct)
    {
        try
        {
            var lookup = await ExecuteReadOnlyAsync(
                $"""
                 SELECT TOP 1 FirmName
                 FROM Vendor
                 WHERE FirmName LIKE '%{EscapeSqlLiteral(firmFragment)}%'
                 """,
                ct);
            return lookup.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vendor FirmName lookup failed for {Fragment}", firmFragment);
            return false;
        }
    }

    private static bool LooksLikeVendorPendingBalanceQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        var mentionsBalance = m.Contains("pending") || m.Contains("opening") || m.Contains("outstanding")
                              || m.Contains("balance");
        if (!mentionsBalance) return false;
        return m.Contains("vendor")
               || m.Contains("bright rubber")
               || m.Contains("chemline")
               || m.Contains("firm")
               || m.Contains("supplier")
               || ResolveVendorFirmAlias(message) is not null;
    }

    private static bool LedgerSqlHasVendorCodeAsPan(string sql)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            sql,
            @"\bPANNo\b\s*=\s*'Ven\d+'",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static string? TryExtractVendorCodeFromPanFilter(string sql)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            sql,
            @"\bPANNo\b\s*=\s*'(Ven\d+)'",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? ResolveVendorFirmAlias(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("bright rubber")) return "Bright Rubber";
        if (m.Contains("chemline")) return "Chemline India Ltd";
        if (m.Contains("lohia")) return "Lohia Corp Limited";
        return null;
    }

    private async Task<string?> TryResolveVendorFirmNameForBalanceAsync(
        string message,
        string sql,
        CancellationToken ct)
    {
        var alias = ResolveVendorFirmAlias(message);
        if (!string.IsNullOrWhiteSpace(alias)) return alias;

        // "bright rubber pending balance" / firm name before balance words
        var m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"^\s*(.+?)\s+(?:pending|opening|outstanding|balance)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var cand = m.Groups[1].Value.Trim();
            if (cand.Length >= 3
                && !cand.Equals("vendor", StringComparison.OrdinalIgnoreCase)
                && !cand.Equals("the", StringComparison.OrdinalIgnoreCase))
                return cand;
        }

        var code = TryExtractVendorCodeFromPanFilter(sql);
        if (string.IsNullOrWhiteSpace(code)) return null;

        try
        {
            var lookup = await ExecuteReadOnlyAsync(
                $"""
                 SELECT TOP 1 FirmName
                 FROM Vendor
                 WHERE VendorCode = '{EscapeSqlLiteral(code)}'
                 """,
                ct);
            if (lookup.Count > 0
                && lookup[0].TryGetValue("FirmName", out var firmObj)
                && firmObj is not null
                && !string.IsNullOrWhiteSpace(firmObj.ToString()))
                return firmObj.ToString()!.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "VendorCode lookup failed for {Code}", code);
        }

        return null;
    }

    private static string ApplyKnownColumnFixes(string sql)
    {
        // Hallucinated MRDate: map by object present in the SQL
        if (System.Text.RegularExpressions.Regex.IsMatch(sql, @"\bMRDate\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            if (sql.Contains("vw_MRNList", StringComparison.OrdinalIgnoreCase)
                || sql.Contains("BillPaymentEntry", StringComparison.OrdinalIgnoreCase)
                || sql.Contains("BillPaymentHODApproval", StringComparison.OrdinalIgnoreCase))
            {
                sql = System.Text.RegularExpressions.Regex.Replace(
                    sql, @"\bMRDate\b", "MRNDate", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            else
            {
                // Vw_StoreInwards / StoreInwardsPayment have BillDate, not MRNDate
                sql = System.Text.RegularExpressions.Regex.Replace(
                    sql, @"\bMRDate\b", "BillDate", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
        }

        // StoreOutwards / gate-pass company column is CompName (not CompanyName)
        if ((sql.Contains("StoreOutwards", StringComparison.OrdinalIgnoreCase)
             || sql.Contains("ReturnGatePass", StringComparison.OrdinalIgnoreCase)
             || sql.Contains("NonReturnGatePass", StringComparison.OrdinalIgnoreCase)
             || sql.Contains("InwdReturnGatePass", StringComparison.OrdinalIgnoreCase)
             || sql.Contains("vw_returngatepasspending", StringComparison.OrdinalIgnoreCase))
            && System.Text.RegularExpressions.Regex.IsMatch(
                sql, @"\bCompanyName\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            sql = System.Text.RegularExpressions.Regex.Replace(
                sql, @"\bCompanyName\b", "CompName", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // IssueSlipNo is varchar — unquoted ints cause conversion errors on non-numeric slip values
        if (sql.Contains("IssueSlipNo", StringComparison.OrdinalIgnoreCase))
        {
            sql = System.Text.RegularExpressions.Regex.Replace(
                sql,
                @"\bIssueSlipNo\b\s*=\s*(\d+)\b",
                "IssueSlipNo = '$1'",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // ItemInfo description column is itemdesc (not ItemName / ItemDesc)
        if (sql.Contains("ItemInfo", StringComparison.OrdinalIgnoreCase))
        {
            sql = System.Text.RegularExpressions.Regex.Replace(
                sql,
                @"\bItemInfo\.(ItemName|ItemDesc)\b",
                "ItemInfo.itemdesc",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            // common aliases
            sql = System.Text.RegularExpressions.Regex.Replace(
                sql,
                @"\b(ii|i|info)\.(ItemName|ItemDesc)\b",
                "$1.itemdesc",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Daily/monthly inward-outward views use companyname
        if ((sql.Contains("vw_ItemInwardOutward", StringComparison.OrdinalIgnoreCase)
             || sql.Contains("vw_ItemMonthlyInwardOutward", StringComparison.OrdinalIgnoreCase))
            && !sql.Contains("StoreOutwards", StringComparison.OrdinalIgnoreCase)
            && System.Text.RegularExpressions.Regex.IsMatch(
                sql, @"\bCompanyName\b|\bCompName\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            sql = System.Text.RegularExpressions.Regex.Replace(
                sql, @"\bCompanyName\b|\bCompName\b", "companyname",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Warehouse/stock objects use CompanyName (not CompName from StoreOutwards)
        if ((sql.Contains("WareHouse", StringComparison.OrdinalIgnoreCase)
             || sql.Contains("vw_itemwiseStock", StringComparison.OrdinalIgnoreCase)
             || sql.Contains("vw_inventoryitemwarehouse", StringComparison.OrdinalIgnoreCase))
            && !sql.Contains("StoreOutwards", StringComparison.OrdinalIgnoreCase)
            && System.Text.RegularExpressions.Regex.IsMatch(
                sql, @"\bCompName\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            sql = System.Text.RegularExpressions.Regex.Replace(
                sql, @"\bCompName\b", "CompanyName", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // WareHouseMaster uses lowercase companyname
        if (sql.Contains("WareHouseMaster", StringComparison.OrdinalIgnoreCase)
            && !sql.Contains("StoreOutwards", StringComparison.OrdinalIgnoreCase)
            && !System.Text.RegularExpressions.Regex.IsMatch(
                sql, @"\bWareHouse\b|\bvw_itemwiseStock\b|\bvw_inventoryitemwarehouse",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            && System.Text.RegularExpressions.Regex.IsMatch(
                sql, @"\bCompanyName\b|\bCompName\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            sql = System.Text.RegularExpressions.Regex.Replace(
                sql, @"\bCompanyName\b|\bCompName\b", "companyname",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Vendor GST column is NewGSTNo (there is no GSTNo on Vendor / vw_VendorListwithBankdtls)
        if ((sql.Contains("Vendor", StringComparison.OrdinalIgnoreCase)
             || sql.Contains("vw_VendorListwithBankdtls", StringComparison.OrdinalIgnoreCase))
            && !sql.Contains("LedgerMaster", StringComparison.OrdinalIgnoreCase)
            && !sql.Contains("FactoryInfo", StringComparison.OrdinalIgnoreCase)
            && System.Text.RegularExpressions.Regex.IsMatch(
                sql, @"\bGSTNo\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            sql = System.Text.RegularExpressions.Regex.Replace(
                sql, @"\bGSTNo\b", "NewGSTNo", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // FactoryInfo PAN column is PermanentAccountNo (not PANNo)
        if (sql.Contains("FactoryInfo", StringComparison.OrdinalIgnoreCase)
            && System.Text.RegularExpressions.Regex.IsMatch(
                sql, @"\bPANNo\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            sql = System.Text.RegularExpressions.Regex.Replace(
                sql, @"\bPANNo\b", "PermanentAccountNo", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return sql;
    }

    private static bool TryRewriteEmptyCompanyFilterToParty(string sql, out string rewritten)
    {
        rewritten = sql;
        // Only when filtering CompanyName=... and not already filtering Party*=...
        var hasCompanyEq = System.Text.RegularExpressions.Regex.IsMatch(
            sql, @"\bCompanyName\b\s*=", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var hasPartyEq = System.Text.RegularExpressions.Regex.IsMatch(
            sql, @"\bPartyName\b\s*=|\bPartyname\b\s*=", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!hasCompanyEq || hasPartyEq) return false;

        // vw_MRNList uses Partyname; others use PartyName — replace only the filter column
        var partyCol = sql.Contains("vw_MRNList", StringComparison.OrdinalIgnoreCase)
            ? "Partyname"
            : "PartyName";
        rewritten = System.Text.RegularExpressions.Regex.Replace(
            sql,
            @"\bCompanyName\b(\s*=)",
            partyCol + "$1",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return !string.Equals(sql, rewritten, StringComparison.Ordinal);
    }

    private static bool LooksLikeMrnPaymentQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        var mentionsPayment = m.Contains("payment") || m.Contains("paid") || m.Contains("prq") || m.Contains("utr");
        var mentionsMrn = m.Contains("mrn") || m.Contains("material receipt") || m.Contains("goods receipt")
                          || System.Text.RegularExpressions.Regex.IsMatch(m, @"\brm\s*\d+");
        return mentionsPayment && mentionsMrn;
    }

    private static bool ShouldRewriteMrnPaymentQuery(
        string sql,
        List<Dictionary<string, object?>> rows)
    {
        // Already on the preferred path
        if (sql.Contains("BillPaymentEntry", StringComparison.OrdinalIgnoreCase)
            && sql.Contains("MRNno", StringComparison.OrdinalIgnoreCase))
            return false;

        // Line-grain view without non-null filter is unsafe for payment existence
        if (sql.Contains("vw_MRNToBillPayment", StringComparison.OrdinalIgnoreCase)
            && !sql.Contains("PaymentNo IS NOT NULL", StringComparison.OrdinalIgnoreCase))
            return true;

        if (rows.Count == 0) return true;

        var sawPaymentCol = false;
        var anyNonNull = false;
        foreach (var row in rows)
        {
            foreach (var kv in row)
            {
                if (!kv.Key.Equals("PaymentNo", StringComparison.OrdinalIgnoreCase)) continue;
                sawPaymentCol = true;
                if (kv.Value is not null && !string.IsNullOrWhiteSpace(Convert.ToString(kv.Value)))
                    anyNonNull = true;
            }
        }

        return sawPaymentCol && !anyNonNull;
    }

    private static string? TryResolveMrnNumber(string message, string sql)
    {
        var fromMsg = TryExtractMrnNumber(message);
        if (!string.IsNullOrWhiteSpace(fromMsg)) return fromMsg;
        return TryExtractMrnNumber(sql);
    }

    private static string? TryExtractMrnNumber(string text)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            text,
            @"\b(RM\s*\d+)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        // Normalize to "RM 269" style (space before digits) to match live data
        var raw = System.Text.RegularExpressions.Regex.Replace(m.Groups[1].Value, @"\s+", " ").Trim();
        var parts = raw.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 && parts[0].Length > 2)
            return parts[0][..2].ToUpperInvariant() + " " + parts[0][2..];
        if (parts.Length == 2)
            return parts[0].ToUpperInvariant() + " " + parts[1];
        return raw.ToUpperInvariant();
    }

    private static bool LooksLikeMrnReceivingCompanyIntent(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("vendor") || m.Contains("supplier") || m.Contains("party ")
            || m.Contains("-purchase") || m.Contains(" ltd-purchase"))
            return false;
        if (m.Contains("for company") || m.Contains("company name") || m.Contains("under company"))
            return true;
        // "For Oswal Extrusion Limited, material receipts..."
        if (m.StartsWith("for ") && (m.Contains("limited") || m.Contains(" ltd") || m.Contains(" pvt")))
            return true;
        // Messy: "oswal extrusion limited which mrns still have pending qty"
        if ((m.Contains("limited") || m.Contains(" ltd"))
            && (m.Contains("pending") || m.Contains("mrn") || m.Contains("receipt")))
            return true;
        return false;
    }

    private static bool LooksLikeReceiptByBillQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        // Asking bill/vendor for a known MRN is NOT "find receipts by bill"
        if (System.Text.RegularExpressions.Regex.IsMatch(m, @"\brm\s*\d+")
            && (m.Contains("who is") || m.Contains("vendor") || m.Contains("what is the bill")
                || m.Contains("bill number and") || m.Contains("bill amt")))
            return false;

        var mentionsReceipt = m.Contains("receipt") || m.Contains("mrn") || m.Contains("material")
                              || m.Contains("goods") || m.Contains("store inward") || m.Contains("reciept");
        if (!mentionsReceipt) return false;

        return m.Contains("linked to bill")
               || m.Contains("for bill")
               || m.Contains("against bill")
               || m.Contains("by bill")
               || (m.Contains("bill number") && (m.Contains("find") || m.Contains("linked") || m.Contains("receipts")))
               || (m.Contains("bill") && (m.Contains("find") || m.Contains("linked")));
    }

    private static bool ShouldRewriteReceiptByBill(string sql, List<Dictionary<string, object?>> rows)
    {
        var usesStoreBill = (sql.Contains("Vw_StoreInwards", StringComparison.OrdinalIgnoreCase)
                             || sql.Contains("StoreInwardsPayment", StringComparison.OrdinalIgnoreCase)
                             || sql.Contains("vw_MRNList", StringComparison.OrdinalIgnoreCase))
                            && System.Text.RegularExpressions.Regex.IsMatch(
                                sql, @"\bBillNo\b\s*=", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (usesStoreBill && rows.Count > 0) return false;

        // Wrong path: BillPaymentEntry.BillNo for finding receipts
        if (sql.Contains("BillPaymentEntry", StringComparison.OrdinalIgnoreCase)
            && System.Text.RegularExpressions.Regex.IsMatch(
                sql, @"\bBillNo\b\s*=", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            && !usesStoreBill)
            return true;

        return rows.Count == 0 || !usesStoreBill;
    }

    private static string? TryExtractBillNo(string message)
    {
        static bool LooksLikeBillToken(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s.Length < 3) return false;
            var t = s.Trim().TrimEnd('.', ',', ';');
            if (t.Equals("and", StringComparison.OrdinalIgnoreCase)
                || t.Equals("the", StringComparison.OrdinalIgnoreCase)
                || t.Equals("amount", StringComparison.OrdinalIgnoreCase)
                || t.Equals("number", StringComparison.OrdinalIgnoreCase))
                return false;
            // Real supplier bills usually have / or digits (e.g. PPL/D/540)
            return t.Contains('/') || t.Any(char.IsDigit);
        }

        var q = System.Text.RegularExpressions.Regex.Match(message, @"['""]([A-Za-z0-9][A-Za-z0-9/._-]{2,})['""]");
        if (q.Success && LooksLikeBillToken(q.Groups[1].Value)) return q.Groups[1].Value.Trim();

        var m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\bbill(?:\s+number|\s+no\.?)?\s+([A-Za-z0-9][A-Za-z0-9/._-]{2,})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success && LooksLikeBillToken(m.Groups[1].Value))
            return m.Groups[1].Value.Trim().TrimEnd('.', ',', ';');

        m = System.Text.RegularExpressions.Regex.Match(message, @"\b([A-Za-z]{2,}/[A-Za-z0-9/._-]+)\b");
        if (m.Success && LooksLikeBillToken(m.Groups[1].Value))
            return m.Groups[1].Value.Trim().TrimEnd('.', ',', ';');
        return null;
    }

    private static bool LooksLikeMrnSql(string sql)
    {
        return sql.Contains("Vw_StoreInwards", StringComparison.OrdinalIgnoreCase)
               || sql.Contains("StoreInwards", StringComparison.OrdinalIgnoreCase)
               || sql.Contains("vw_MRNList", StringComparison.OrdinalIgnoreCase)
               || sql.Contains("vw_MRNToBillPayment", StringComparison.OrdinalIgnoreCase)
               || sql.Contains("StoreInwardsPayment", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasVendorPartyFilterWithoutCompany(string sql)
    {
        var hasParty = System.Text.RegularExpressions.Regex.IsMatch(
            sql, @"\bPartyName\b|\bPartyname\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var hasCompany = System.Text.RegularExpressions.Regex.IsMatch(
            sql, @"\bCompanyName\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return hasParty && !hasCompany;
    }

    private static string RewriteMrnPartyFilterToCompanyName(string sql)
    {
        // PartyName / Partyname → CompanyName; leave SupplierName alone
        sql = System.Text.RegularExpressions.Regex.Replace(
            sql, @"\bPartyName\b", "CompanyName", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        sql = System.Text.RegularExpressions.Regex.Replace(
            sql, @"\bPartyname\b", "CompanyName", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return sql;
    }

    private static bool LooksLikeIssueSlipQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("issue slip")
               || (m.Contains("slip") && (m.Contains("issued") || m.Contains("issue")))
               || (m.Contains("what was issued") && m.Contains("slip"));
    }

    private sealed record GatePassRef(string Prefix, string Serial, string PassKind);

    private sealed record GatePassCompanyListRef(
        string CompanyKeyword,
        string? Prefix,
        string PassKind,
        bool PendingOnly);

    private static bool LooksLikeGatePassQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("gate pass")
               || m.Contains("gatepass")
               || m.Contains("rgp")
               || m.Contains("nrgp")
               || m.Contains("ngp")
               || m.Contains("igp")
               || System.Text.RegularExpressions.Regex.IsMatch(
                   message, @"\b(?:GP|NGP|IGP)/\d+\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
               || (m.Contains("returnable") && m.Contains("return"))
               || (m.Contains("pending") && m.Contains("return"));
    }

    private static bool ShouldRewriteGatePassQuery(string sql, List<Dictionary<string, object?>> rows)
    {
        if (GatePassSqlHasMalformedNumber(sql)) return true;

        var onGatePass = sql.Contains("Vw_ReturnGatePass", StringComparison.OrdinalIgnoreCase)
                         || sql.Contains("ReturnGatePass", StringComparison.OrdinalIgnoreCase)
                         || sql.Contains("Vw_NonReturnGatePass", StringComparison.OrdinalIgnoreCase)
                         || sql.Contains("NonReturnGatePass", StringComparison.OrdinalIgnoreCase)
                         || sql.Contains("InwdReturnGatePass", StringComparison.OrdinalIgnoreCase)
                         || sql.Contains("vw_returngatepasspending", StringComparison.OrdinalIgnoreCase);

        if (!onGatePass) return true;
        return rows.Count == 0;
    }

    private static bool GatePassSqlHasMalformedNumber(string sql)
    {
        foreach (var col in new[] { "GatePassNo", "InwGatePassNo" })
        {
            var eq = System.Text.RegularExpressions.Regex.Match(
                sql,
                $@"\b{col}\s*=\s*'([^']*)'",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (eq.Success && !IsWellFormedGatePassNo(eq.Groups[1].Value))
                return true;
        }

        return false;
    }

    private static bool IsWellFormedGatePassNo(string value)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            value,
            @"(?i)/(GP|NGP|IGP)/",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool IsLikelyGatePassCompanyPrefix(string token)
    {
        if (token.Length is < 2 or > 6) return false;
        var lower = token.ToLowerInvariant();
        return lower is not ("rgp" or "ngp" or "igp" or "nrgp" or "gp" or "the" or "for");
    }

    private static string InferGatePassKind(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("nrgp") || m.Contains("non-returnable") || m.Contains("non returnable") || m.Contains(" ngp"))
            return "NGP";
        if (m.Contains("igp") || m.Contains("inward return") || m.Contains("inward gate"))
            return "IGP";
        return "GP";
    }

    private static GatePassRef? TryExtractGatePassRef(string message, string sql)
    {
        var full = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b([A-Za-z0-9]+)/(\d{2}-\d{2})/(GP|NGP|IGP)/(\d+)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (full.Success)
        {
            return new GatePassRef(
                full.Groups[1].Value.ToUpperInvariant(),
                full.Groups[4].Value,
                full.Groups[3].Value.ToUpperInvariant());
        }

        if (LooksLikeGatePassQuestion(message))
        {
            var m1 = System.Text.RegularExpressions.Regex.Match(
                message,
                @"\b(\d{1,5})\s+([A-Za-z]{2,6})\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m1.Success && IsLikelyGatePassCompanyPrefix(m1.Groups[2].Value))
            {
                return new GatePassRef(
                    m1.Groups[2].Value.ToUpperInvariant(),
                    m1.Groups[1].Value,
                    InferGatePassKind(message));
            }

            var m2 = System.Text.RegularExpressions.Regex.Match(
                message,
                @"\b([A-Za-z]{2,6})\s+(\d{1,5})\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m2.Success && IsLikelyGatePassCompanyPrefix(m2.Groups[1].Value))
            {
                return new GatePassRef(
                    m2.Groups[1].Value.ToUpperInvariant(),
                    m2.Groups[2].Value,
                    InferGatePassKind(message));
            }
        }

        foreach (var col in new[] { "GatePassNo", "InwGatePassNo" })
        {
            var eq = System.Text.RegularExpressions.Regex.Match(
                sql,
                $@"\b{col}\s*=\s*'([^']*)'",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!eq.Success || IsWellFormedGatePassNo(eq.Groups[1].Value)) continue;

            var reversed = System.Text.RegularExpressions.Regex.Match(
                eq.Groups[1].Value,
                @"^(\d+)/([A-Za-z0-9]+)$");
            if (reversed.Success)
            {
                return new GatePassRef(
                    reversed.Groups[2].Value.ToUpperInvariant(),
                    reversed.Groups[1].Value,
                    InferGatePassKind(message));
            }
        }

        return null;
    }

    private static string BuildGatePassSql(GatePassRef gp)
    {
        var likePattern = $"%{EscapeSqlLiteral(gp.Prefix)}%{gp.PassKind}/{EscapeSqlLiteral(gp.Serial)}";

        if (gp.PassKind == "NGP")
        {
            return $"""
                SELECT TOP 50 GatePassNo, CompName, PartyName, ItemCode, ItemName, Qty, Purpose, sysdate
                FROM Vw_NonReturnGatePass
                WHERE GatePassNo LIKE '{likePattern}'
                ORDER BY sysdate DESC
                """;
        }

        if (gp.PassKind == "IGP")
        {
            return $"""
                SELECT TOP 50 InwGatePassNo, GatePassNo, CompName, ItemCode, ItemDesc, Qty, Purpose, sysdate
                FROM InwdReturnGatePass
                WHERE InwGatePassNo LIKE '{likePattern}'
                ORDER BY sysdate DESC
                """;
        }

        return $"""
            SELECT TOP 50 GatePassNo, CompName, ItemCode, ItemDesc, Qty, Purpose, ActualPartyName, PurposePartyName, sysdate
            FROM Vw_ReturnGatePass
            WHERE GatePassNo LIKE '{likePattern}'
            ORDER BY sysdate DESC
            """;
    }

    private static string? ResolveGatePassCompanyPrefix(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("oswal") || m.Contains("oel")) return "OEL";
        if ((m.Contains("k.p") || m.Contains("kp") || m.Contains("kpv")) && m.Contains("woven")) return "KPV";
        if (m.Contains("polyfilms") || m.Contains("ppl")) return "PPL";
        if (m.Contains("pil2") || (m.Contains("plastene") && m.Contains("unit"))) return "PIL2";
        if (m.Contains("plastene india")) return "PIL";
        if (m.Contains("hcp") || m.Contains("bulkpack")) return "HCP";
        return null;
    }

    private static string ExtractGatePassCompanyKeyword(string company)
    {
        if (company.Contains("Oswal", StringComparison.OrdinalIgnoreCase)) return "Oswal";
        if (company.Contains("K.P.", StringComparison.OrdinalIgnoreCase)
            || company.Contains("WOVEN", StringComparison.OrdinalIgnoreCase))
            return "K.P.";
        if (company.Contains("Polyfilms", StringComparison.OrdinalIgnoreCase)) return "Polyfilms";
        if (company.Contains("Bulkpack", StringComparison.OrdinalIgnoreCase)) return "Bulkpack";
        if (company.Contains("Plastene", StringComparison.OrdinalIgnoreCase)) return "Plastene";
        return company.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    }

    private static bool LooksLikeGatePassPendingQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("pending")
               || m.Contains("still have")
               || m.Contains("open return")
               || m.Contains("not returned")
               || m.Contains("yet to return");
    }

    private static GatePassCompanyListRef? TryResolveGatePassCompanyListRewrite(string message, string sql)
    {
        var company = ResolveOutwardCompanyAlias(message) ?? TryExtractCompanyName(message);
        var prefix = ResolveGatePassCompanyPrefix(message);
        if (string.IsNullOrWhiteSpace(company) && string.IsNullOrWhiteSpace(prefix)) return null;

        var keyword = !string.IsNullOrWhiteSpace(company)
            ? ExtractGatePassCompanyKeyword(company)
            : prefix!;
        var passKind = InferGatePassKind(message);
        var pendingOnly = LooksLikeGatePassPendingQuestion(message) && passKind == "GP";

        var onGatePass = sql.Contains("Vw_ReturnGatePass", StringComparison.OrdinalIgnoreCase)
                         || sql.Contains("ReturnGatePass", StringComparison.OrdinalIgnoreCase)
                         || sql.Contains("Vw_NonReturnGatePass", StringComparison.OrdinalIgnoreCase)
                         || sql.Contains("NonReturnGatePass", StringComparison.OrdinalIgnoreCase)
                         || sql.Contains("InwdReturnGatePass", StringComparison.OrdinalIgnoreCase)
                         || sql.Contains("vw_returngatepasspending", StringComparison.OrdinalIgnoreCase);

        if (!onGatePass) return new GatePassCompanyListRef(keyword, prefix, passKind, pendingOnly);

        var lit = TryExtractSqlCompanyNameLiteral(sql);
        if (lit is null && !sql.Contains("LIKE", StringComparison.OrdinalIgnoreCase))
            return new GatePassCompanyListRef(keyword, prefix, passKind, pendingOnly);

        if (lit is not null
            && System.Text.RegularExpressions.Regex.IsMatch(
                sql,
                @"\b(?:CompName|CompanyName|companyname)\b\s*=\s*'",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return new GatePassCompanyListRef(keyword, prefix, passKind, pendingOnly);

        return null;
    }

    private static string BuildGatePassCompanyListSql(GatePassCompanyListRef r)
    {
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(r.CompanyKeyword))
            filters.Add($"CompName LIKE '%{EscapeSqlLiteral(r.CompanyKeyword)}%'");
        if (!string.IsNullOrWhiteSpace(r.Prefix))
            filters.Add($"GatePassNo LIKE '{EscapeSqlLiteral(r.Prefix)}%/{r.PassKind}/%'");
        var companyFilter = filters.Count > 0 ? "(" + string.Join(" OR ", filters) + ")" : "1=1";

        if (r.PendingOnly)
        {
            return $"""
                SELECT TOP 50 GatePassNo, CompName, ItemCode, ItemDesc, ReturnQty, InwardQty, PendingQty, Purpose, sysdate
                FROM vw_returngatepasspending
                WHERE {companyFilter} AND PendingQty > 0
                ORDER BY PendingQty DESC
                """;
        }

        if (r.PassKind == "NGP")
        {
            return $"""
                SELECT TOP 50 GatePassNo, CompName, PartyName, ItemCode, ItemName, Qty, Purpose, sysdate
                FROM Vw_NonReturnGatePass
                WHERE {companyFilter}
                ORDER BY sysdate DESC
                """;
        }

        if (r.PassKind == "IGP")
        {
            return $"""
                SELECT TOP 50 InwGatePassNo, GatePassNo, CompName, ItemCode, ItemDesc, Qty, Purpose, sysdate
                FROM InwdReturnGatePass
                WHERE InwGatePassNo LIKE '{EscapeSqlLiteral(r.Prefix ?? r.CompanyKeyword)}%/{r.PassKind}/%'
                   OR {companyFilter}
                ORDER BY sysdate DESC
                """;
        }

        return $"""
            SELECT TOP 50 GatePassNo, CompName, ItemCode, ItemDesc, Qty, Purpose, ActualPartyName, PurposePartyName, sysdate
            FROM Vw_ReturnGatePass
            WHERE {companyFilter}
            ORDER BY sysdate DESC
            """;
    }

    private static bool ShouldRewriteIssueSlipQuery(string sql)
    {
        var usesStore = sql.Contains("StoreOutwards", StringComparison.OrdinalIgnoreCase);
        var usesDespatch = sql.Contains("vw_MISrolldespatch", StringComparison.OrdinalIgnoreCase)
                           || sql.Contains("MISRollforDespatch", StringComparison.OrdinalIgnoreCase)
                           || sql.Contains("FIBCDespatch", StringComparison.OrdinalIgnoreCase)
                           || sql.Contains("MIS_YarnDespatch", StringComparison.OrdinalIgnoreCase)
                           || sql.Contains("SmallBagBailForDespatch", StringComparison.OrdinalIgnoreCase)
                           || sql.Contains("vw_RollforDespatch", StringComparison.OrdinalIgnoreCase);
        return !usesStore || usesDespatch;
    }

    private static string? TryExtractIssueSlipNo(string message)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:issue\s+)?slip\s+(\d+)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value;
        m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:issue\s+slip|slip)\s*#?\s*(\d+)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static bool LooksLikeTodayOutwardQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!m.Contains("today")) return false;
        return m.Contains("outward")
               || m.Contains("issued")
               || m.Contains("stock issued")
               || m.Contains("inward outward")
               || m.Contains("issue");
    }

    private static bool UsesCalendarTodayOnOutwardSql(string sql)
    {
        var onOutward = sql.Contains("vw_ItemInwardOutward", StringComparison.OrdinalIgnoreCase)
                        || sql.Contains("StoreOutwards", StringComparison.OrdinalIgnoreCase)
                        || sql.Contains("vw_ItemMonthlyInwardOutward", StringComparison.OrdinalIgnoreCase);
        if (!onOutward) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(
            sql, @"\bGETDATE\s*\(", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static string? ResolveOutwardCompanyAlias(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("oswal")) return "Oswal Extrusion Limited";
        if ((m.Contains("k.p") || m.Contains("kp ") || m.Contains("kp woven") || m == "kp woven" || m.Contains("kpwoven"))
            && m.Contains("woven"))
            return "K.P. WOVEN PRIVATE LIMITED";
        // "kp woven issue slip..." — kp without trailing space
        if (System.Text.RegularExpressions.Regex.IsMatch(m, @"\bkp\b") && m.Contains("woven"))
            return "K.P. WOVEN PRIVATE LIMITED";
        if (m.Contains("polyfilms") || m.Contains("ppl"))
            return "Plastene Polyfilms Limited";
        if (m.Contains("bulkpack") || m.Contains("hcp plastene"))
            return "HCP Plastene Bulkpack Ltd";
        if (m.Contains("plastene india") && m.Contains("unit"))
            return "Plastene India Limited (Unit -II)";
        if (m.Contains("plastene india")) return "Plastene India Limited";
        return null;
    }

    /// <summary>
    /// Resolve our-company name for debit-note rewrites from message and/or mistaken MRNo literal.
    /// No hard-coded Oswal fallback — returns null if unresolved.
    /// </summary>
    private static string? TryResolveDebitNoteCompany(string message, string sql)
    {
        var fromAlias = ResolveOutwardCompanyAlias(message);
        if (!string.IsNullOrWhiteSpace(fromAlias)) return fromAlias;

        var extracted = TryExtractCompanyName(message);
        if (!string.IsNullOrWhiteSpace(extracted))
            return CanonicalizeCompanyName(extracted);

        var loose = TryExtractCompanyFromDebitPhrase(message);
        if (!string.IsNullOrWhiteSpace(loose))
            return CanonicalizeCompanyName(loose);

        var fromSqlCompany = TryExtractSqlCompanyNameLiteral(sql);
        if (!string.IsNullOrWhiteSpace(fromSqlCompany))
            return CanonicalizeCompanyName(fromSqlCompany);

        var mistaken = TryExtractMistakenMrnoLiteral(sql);
        if (!string.IsNullOrWhiteSpace(mistaken))
            return CanonicalizeCompanyName(mistaken);

        return null;
    }

    private static bool ShouldRewriteEmptyDebitCompanyQuery(string sql, string canonicalCompany)
    {
        if (DebitNoteSqlHasCompanyMistakenAsMrno(sql)) return true;

        // Don't need StoreInwardsPayment to list debit notes for a company
        if (sql.Contains("StoreInwardsPayment", StringComparison.OrdinalIgnoreCase)
            || sql.Contains("Vw_StoreInwards", StringComparison.OrdinalIgnoreCase)
            || sql.Contains("vw_MRNToBillPayment", StringComparison.OrdinalIgnoreCase))
            return true;

        var lit = TryExtractSqlCompanyNameLiteral(sql);
        if (string.IsNullOrWhiteSpace(lit))
            return true; // no company filter — add canonical

        // Partial nickname e.g. CompanyName = 'Oswal' vs 'Oswal Extrusion Limited'
        if (!lit.Equals(canonicalCompany, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static string? TryExtractSqlCompanyNameLiteral(string sql)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            sql,
            @"\b(?:CompanyName|companyname|CompName)\b\s*=\s*'([^']+)'",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string? CanonicalizeCompanyName(string raw)
    {
        var t = raw.Trim().TrimEnd('.', ',', ';', '?', '!');
        if (t.Length == 0) return null;

        var viaAlias = ResolveOutwardCompanyAlias(t);
        if (!string.IsNullOrWhiteSpace(viaAlias)) return viaAlias;

        // Already a full legal-style name — keep as-is
        if (t.Contains("Limited", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Ltd", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Pvt", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Private", StringComparison.OrdinalIgnoreCase)
            || t.Length >= 12)
            return t;

        return null;
    }

    private static string? TryExtractCompanyFromDebitPhrase(string message)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:for|at|under|of)\s+(?:company\s+)?(.+?)(?:\s+(?:with|against|provisional|debit|credit|mrn|amount|party|notes?)|\s*$)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        var cand = m.Groups[1].Value.Trim().TrimEnd('.', ',', ';', '?', '!');
        if (cand.Length < 3) return null;
        if (cand.Equals("mrn", StringComparison.OrdinalIgnoreCase)
            || cand.Equals("company", StringComparison.OrdinalIgnoreCase))
            return null;
        return cand;
    }

    private static string? TryExtractMistakenMrnoLiteral(string sql)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            sql,
            @"\bMRNo\b\s*=\s*'([^']+)'",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static bool LooksLikeCreditNoteQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("credit note")
               || m.Contains("credit notes")
               || (m.Contains("credit") && (m.Contains("cn") || m.Contains("polyfilms") || m.Contains("customer")));
    }

    private static bool LooksLikeCreditNoteSql(string sql)
    {
        return sql.Contains("vw_creditnote", StringComparison.OrdinalIgnoreCase)
               || sql.Contains("CreditNote", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeDebitNoteSql(string sql)
    {
        return sql.Contains("vw_DebitNote", StringComparison.OrdinalIgnoreCase)
               || (sql.Contains("DebitNote", StringComparison.OrdinalIgnoreCase)
                   && !sql.Contains("CreditNote", StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeDebitNoteQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("debit note")
               || m.Contains("debit notes")
               || (m.Contains("debit") && (m.Contains("dn") || m.Contains("provisional") || m.Contains("mrn")));
    }

    private static bool LooksLikeDebitForCompanyQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!LooksLikeDebitNoteQuestion(message)) return false;
        // Any debit question scoped to a company / plant name
        if (m.Contains("for company") || m.Contains("under company")) return true;
        if (System.Text.RegularExpressions.Regex.IsMatch(
                m, @"\b(?:for|at|under|of)\s+[a-z0-9]"))
            return true;
        if (m.Contains("against") && m.Contains("mrn")) return true;
        return ResolveOutwardCompanyAlias(message) is not null;
    }

    private static bool DebitNoteSqlHasCompanyMistakenAsMrno(string sql)
    {
        var literal = TryExtractMistakenMrnoLiteral(sql);
        if (string.IsNullOrWhiteSpace(literal)) return false;
        var v = literal.Trim();
        if (v.Length < 3) return false;
        // Real MRNs usually contain digits (RM 269, GNSP 480). Pure alpha/spaces ≈ company name mistake.
        if (v.Any(char.IsDigit)) return false;
        return true;
    }

    private static string? TryExtractDebitOrCreditNoteNumber(string message)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b([A-Za-z0-9]+/(?:DB|CR)/\d{2}-\d{2}/\d+)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static bool ShouldRewriteDebitCreditNoteByNumber(
        string noteNum,
        string sql,
        List<Dictionary<string, object?>> rows)
    {
        var isDebitCode = noteNum.Contains("/DB/", StringComparison.OrdinalIgnoreCase);
        var isCreditCode = noteNum.Contains("/CR/", StringComparison.OrdinalIgnoreCase);
        if (!isDebitCode && !isCreditCode) return false;

        var onDebit = sql.Contains("vw_DebitNote", StringComparison.OrdinalIgnoreCase)
                      || (sql.Contains("DebitNote", StringComparison.OrdinalIgnoreCase)
                          && !sql.Contains("CreditNote", StringComparison.OrdinalIgnoreCase));
        var onCredit = LooksLikeCreditNoteSql(sql);

        if (isDebitCode && (!onDebit || onCredit || rows.Count == 0 && onCredit))
            return true;
        if (isDebitCode && rows.Count == 0 && !onDebit)
            return true;
        if (isCreditCode && (!onCredit || onDebit || rows.Count == 0 && onDebit))
            return true;
        if (isCreditCode && rows.Count == 0 && !onCredit)
            return true;

        // Wrong object even if somehow returned rows
        if (isDebitCode && onCredit) return true;
        if (isCreditCode && onDebit && !onCredit) return true;
        return false;
    }

    private static bool TryBuildCreditNoteCompanyPartySql(string message, out string sql)
    {
        sql = "";
        var m = message.ToLowerInvariant();
        string? company = null;
        string? party = null;

        if (m.Contains("polyfilms") || m.Contains("ppl"))
            company = "Plastene Polyfilms Limited";
        else if (m.Contains("oswal"))
            company = "Oswal Extrusion Limited";
        else if (m.Contains("plastene india"))
            company = "Plastene India Limited";

        if (m.Contains("commercial bag"))
            party = "Commercial Bag Company";

        // Message like "polyfilms credit notes to X" — X is customer/party
        if (party is null)
        {
            var to = System.Text.RegularExpressions.Regex.Match(
                message,
                @"\b(?:to|for|against)\s+([A-Za-z][A-Za-z0-9 .&'-]{2,60})",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (to.Success)
            {
                var cand = to.Groups[1].Value.Trim().TrimEnd('.', ',', ';');
                if (!cand.Contains("polyfilms", StringComparison.OrdinalIgnoreCase)
                    && !cand.Contains("credit", StringComparison.OrdinalIgnoreCase)
                    && !cand.Contains("debit", StringComparison.OrdinalIgnoreCase)
                    && cand.Length >= 3)
                    party = cand;
            }
        }

        if (company is null && party is null) return false;

        var where = new List<string>();
        if (company is not null)
            where.Add($"companyname = '{EscapeSqlLiteral(company)}'");
        if (party is not null)
            where.Add($"partyname = '{EscapeSqlLiteral(party)}'");

        sql = $"""
            SELECT TOP 50 creditnotenumber, companyname, partyname, totalcreditamount, credittype, creditnotedate, invno
            FROM vw_creditnote
            WHERE {string.Join(" AND ", where)}
            ORDER BY creditnotedate DESC
            """;
        return true;
    }

    private static string? TryExtractCompanyName(string message)
    {
        // Prefer quoted company names
        var q1 = message.IndexOf('\'');
        var q2 = q1 >= 0 ? message.IndexOf('\'', q1 + 1) : -1;
        if (q1 >= 0 && q2 > q1) return message[(q1 + 1)..q2].Trim();

        foreach (var marker in new[] { " for company ", " company " })
        {
            var idx = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var rest = message[(idx + marker.Length)..].Trim();
            foreach (var stop in new[] { " where ", " with ", " top ", ",", "." })
            {
                var s = rest.IndexOf(stop, StringComparison.OrdinalIgnoreCase);
                if (s > 0) rest = rest[..s];
            }
            if (!string.IsNullOrWhiteSpace(rest)) return rest.Trim();
        }

        // "For Oswal Extrusion Limited, ..."
        if (message.StartsWith("For ", StringComparison.OrdinalIgnoreCase))
        {
            var rest = message[4..].Trim();
            foreach (var stop in new[] { ",", " with ", " material ", " mrn ", " pending " })
            {
                var s = rest.IndexOf(stop, StringComparison.OrdinalIgnoreCase);
                if (s > 0) rest = rest[..s];
            }
            if (rest.Contains("Limited", StringComparison.OrdinalIgnoreCase)
                || rest.Contains("Ltd", StringComparison.OrdinalIgnoreCase)
                || rest.Contains("Pvt", StringComparison.OrdinalIgnoreCase))
                return rest.Trim();
        }

        return null;
    }

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''");

    private static string BuildSchemaBlock(IReadOnlyList<RetrievedSchemaChunk> chunks)
    {
        var sb = new StringBuilder();
        foreach (var c in chunks)
        {
            sb.AppendLine($"### {c.ObjectName} ({c.ObjectType}, domain={c.Domain}, score={c.Score:F3})");
            sb.AppendLine(c.EmbeddingText);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private async Task<List<Dictionary<string, object?>>> ExecuteReadOnlyAsync(
        string sql,
        CancellationToken ct)
    {
        await using var connection = _database.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = SqlTimeoutSeconds;
        cmd.CommandType = CommandType.Text;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<Dictionary<string, object?>>();
        var fieldCount = reader.FieldCount;

        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < fieldCount; i++)
            {
                var name = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                if (value is DateTime dt)
                    value = dt.ToString("o");
                row[name] = value;
            }
            list.Add(row);
            if (list.Count >= MaxReturnRows + 1)
                break;
        }

        return list;
    }
}
