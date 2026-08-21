using System.Data;
using System.Text;
using System.Text.Json;
using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public partial class ChatOrchestratorService
{
    private readonly SchemaRetrievalService _retrieval;
    private readonly SchemaCatalogService _schemaCatalog;
    private readonly IChatCompletionService _llm;
    private readonly SqlGuardService _sqlGuard;
    private readonly DatabaseService _database;
    private readonly AgeingReportService _ageingService;
    private readonly LedgerStatementChatService _ledgerStatementChat;
    private readonly ErpFinanceReportService _financeReportService;
    private readonly ErpInventoryReportService _inventoryReportService;
    private readonly ChatEntityResolutionService _entityResolution;
    private readonly ILogger<ChatOrchestratorService> _logger;

    private const int MaxReturnRows = 50;
    private const int MaxExportRows = 5000;
    private const int SqlTimeoutSeconds = 60;

    public ChatOrchestratorService(
        SchemaRetrievalService retrieval,
        SchemaCatalogService schemaCatalog,
        IChatCompletionService llm,
        SqlGuardService sqlGuard,
        DatabaseService database,
        AgeingReportService ageingService,
        LedgerStatementChatService ledgerStatementChat,
        ErpFinanceReportService financeReportService,
        ErpInventoryReportService inventoryReportService,
        ChatEntityResolutionService entityResolution,
        ILogger<ChatOrchestratorService> logger)
    {
        _retrieval = retrieval;
        _schemaCatalog = schemaCatalog;
        _llm = llm;
        _sqlGuard = sqlGuard;
        _database = database;
        _ageingService = ageingService;
        _ledgerStatementChat = ledgerStatementChat;
        _financeReportService = financeReportService;
        _inventoryReportService = inventoryReportService;
        _entityResolution = entityResolution;
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

        var entities = await _entityResolution.ResolveAsync(request.Message, ct);
        CurrentEntities.Value = entities;

        try
        {
        var schemaBlock = BuildSchemaBlock(chunks);
        var sqlSystem = """
            You are a T-SQL expert for Microsoft SQL Server (database MaterialProcessing).
            Generate ONE read-only query that answers the user question.
            Rules:
            - Output ONLY the SQL (no markdown fences, no explanation).
            - SELECT or WITH...SELECT only. Never modify data.
            - Use only tables/views described in the provided schema context.
            - Use ONLY exact column names listed for each table. Do not invent or borrow columns from other tables.
            - ApprovePO / ApprovePOHOD / ApproveWorkOrder key column is PoNo (NOT POCode, NOT PurchaseCode on those tables). Join PoNo = PurchasePayment.PurchaseCode.
            - ApproveWorkOrder / ApprovePO / ApprovePOHOD have NO CompanyName, CompName, or TotalAmount — those are on PurchasePayment (CompanyName, TotalAmount). Never SELECT CompName/TotalAmt FROM ApproveWorkOrder.
            - FactoryInfo is OUR company/unit master only (Oswal Extrusion Limited, Plastene Polyfilms Limited, etc.). NEVER use FactoryInfo for supplier/vendor firm names (Chemline, Bright Rubber, Lohia, etc.) — those live in Vendor.
            - FactoryInfo PAN column is PermanentAccountNo (NOT PANNo). LedgerMaster PAN column is PANNo.
            - FactoryInfo GST prefer NewGSTNo. LedgerMaster has GSTNo/NewGSTNo for parties.
            - Supplier/vendor GST/PAN/email/address/city/bank/IFSC/MSME/vendor code: ALWAYS Vendor (full profile) or vw_VendorListwithBankdtls (bank/IFSC shortcut). NEVER FactoryInfo for vendors.
            - Ledger/account groups: use SELECT DISTINCT Under FROM LedgerMaster (filter empty Under). NEVER query LedgerGroupMaster.
            - Opening/pending ledger balances: use LedgerMaster.Openingbalance and LedgerMaster.PendingBalance. NEVER query LedgerOpeningBalance (table is empty).
            - Named party/customer/vendor ledger outstanding: ALWAYS LedgerMaster WHERE LedgerName LIKE '%party%' (PendingBalance, Openingbalance, CompanyName). Never Vendor for balances. Optional CompanyName = our plant when user names it. Always TOP 50.
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
            - Top export customers / export sales ranking: ALWAYS vw_Salesvoucher (NOT despatch). Filter InvType LIKE '%Export%', Indian FY on InvDate (FY 2024-25 = 2024-04-01 to <2025-04-01), GROUP BY BuyerName, ORDER BY SUM(BillAMount) DESC, TOP N. For '<company> group' use CompanyName IN (SELECT Name FROM FactoryInfo WHERE GroupName LIKE '%hint%' OR Name LIKE '%hint%'). When user says exclude inter-company / inter unit / group companies: also NOT EXISTS buyer matching FactoryInfo.Name for that same group (do not count sister companies as export customers). BillAMount spelling. Always TOP N (default 5).
            - Country-wise sales / sales by country: ALWAYS vw_Countrywise_sales_dashboard (NOT vw_Salesvoucher). Columns: Country, Value (Amount-DebitNote), InvYear (short label 25-26), GroupName. Filter InvYear = '25-26' for FY 2025-26. Filter company via GroupName IN (SELECT DISTINCT GroupName FROM FactoryInfo WHERE Name = legal company). NEVER use vw_Salesvoucher.Destination for country — Destination is port/city/state (Gujarat, Ahmedabad), not country. GROUP BY Country, SUM(CAST(Value AS float)), ORDER BY total DESC, TOP 50.
            - Total sales / sales by product group for FY: ALWAYS vw_Sales_EBIDTA (same as Sales Dashboard, excl. InterGroup='Intergroup'). Total: SUM(Amount), SUM(netwt). By group: GROUP BY Groupname; by sub-group: GROUP BY Groupname, SubGroupName. Filter CompanyName and invdate for Indian FY (2025-04-01 to 2026-03-31 for FY 25-26). Total purchase: vw_Purchase_EBIDTA with same rules.
            - FIBC / product-line sales (per kg, monthly, last N months): governed path on vw_Sales_EBIDTA — filter Groupname/SubGroupName LIKE FIBC/Jumbo/Bulk (or tape/fabric/webbing/small bag); SUM(Amount), SUM(netwt), PerKg=Amount/netwt; monthly GROUP BY YEAR/MONTH(invdate); default period last 6 months when not FY; company optional (all companies if omitted).
            - Ledger count: COUNT(*) FROM LedgerMaster WHERE CompanyName = company; when user says under/in a group (e.g. Sundry Debtors) also filter LedgerMaster.Under LIKE '%group%'. Ledger/account groups list: SELECT DISTINCT Under FROM LedgerMaster (never LedgerGroupMaster).
            - Debtor/creditor ageing / overdue buckets: monthly/group pivot and bill-wise overdue use ERP SPs (sp_Representative_Outstanding_Pivot, sp_Overdue_Ledger). Day-bucket ageing (0-30/31-60/61-90/90+) uses governed SELECT on vw_BillWiseTransaction. Debtors use G3='Sundry Debtors'; creditors use G3='Trade Creditors' (never 'Sundry Creditors'). Sub-groups: Debtors-Overseas, Debtors-Domestic, Creditors-RM, etc.
            - Ledger voucher statement / transaction history for a named party: handled by ERP sp_ac_LedgerSummary_BankRecoDate (portal parity) — NOT vw_LedgerSummary or LedgerMaster alone. Requires company + ledger/party name + date range (defaults to current FY).
            - Despatch/packing: roll history vw_MISrolldespatch; FIBC bails FIBCDespatch; yarn MIS_YarnDespatch; small bag SmallBagBailForDespatch; rolls waiting vw_RollforDespatch. ALWAYS filter CompanyName/Companyname or InvNo/PartyName/date + TOP 50 (million-row tables). Prefer view over MISRollforDespatch table.
            - Production: factory daily vw_FactoryProduction (companyname, Particulars, TapeProduction/Fabric/SmallBag); tape plant vw_daily_tape_prod_New (bracket [Loom Dept]/[FIBC Dept]); loom rolls vw_LoomProductionENtry (MUST filter CompanyName/Sysdate/LoomNo + TOP 50 — ~716k; skip stale vw_Loom_Prod_Mtr); FIBC bags VW_FIBCBagwiseProduction (not _New); MIS qty VW_PRODUCTION_EBD_DTL; WIP vw_WIPReport; small bags SmallBagProductionEntry (Cutting/Stitching — live data mainly Plastene/HCP units, NOT Oswal; Oswal uses Tape/Fabric/WEBBING in vw_FactoryProduction). Filter EBD/WIP/loom + TOP 50. Not despatch / not ApproveWorkOrder.
            - Department wastage % vs production: governed path on vw_FactoryProduction (Particulars Tape/Fabric/WEBBING/Small Bag; WastagePct = Wastage*100/ProductionQty) using latest business Sysdate for company — NOT bare GETDATE(). Tape-plant wastage dept: vw_daily_tape_prod_New [Wastage Dept] vs [Total Production].
            - Multi-material inventory (fabric/webbing/filler cord): governed vw_itemwiseStock with OR ItemName LIKE across slash/comma-separated materials — requires company name.
            - Stitcher/sewer headcount / attendance: Loginentry.dbo.Attendancemachine (AttendanceDate, Empcode, intime) JOIN Loginentry.dbo.empinfo (EmpCode, Designation, Deptt, CompanyName). Filter Designation/Deptt LIKE stitch/sewer. Count DISTINCT Empcode where intime IS NOT NULL. NOT LoginRights for attendance.
            - Prefer TOP 50 for detail lists. COUNT aggregates need no TOP.
            - Pending filters: status = 'Pending' or Status = 'Pending' (match column casing in schema).
            - Approved counts: status LIKE 'Approved%' when statuses vary.
            - Use correct joins from the schema notes.
            - OUR company nicknames MUST use full legal CompanyName/CompName/companyname — never the nickname string:
              kp woven / k.p. woven / kpv / kpw → 'K.P. WOVEN PRIVATE LIMITED';
              oswal / oel / oel2 → 'Oswal Extrusion Limited' (or Unit-II/III/IV/V when oel2/oel3 etc.);
              polyfilms / ppl → 'Plastene Polyfilms Limited';
              hcp / bulkpack / hpbl → 'HCP Plastene Bulkpack Ltd' (hpbl2/3/4 for units);
              plastene india / pil / pil1 → 'Plastene India Limited';
              pil2 / plastene unit 2 → 'Plastene India Limited (Unit -II)';
              pil3 → Unit -III; pil4 → Unit-IV; pil5 → Unit-V; pil6 → Unit-VI; pil8 → Unit-VIII.
              Do NOT treat PIL2 in PIL2/RAW/25-26/231 as a company — that is a document prefix.
              Names ending -Purchase are vendors (PartyName/FirmName), NOT our CompanyName.
            - PurchasePayment header: PurchaseCode, TotalAmount, CompanyName, Currency, LoginName, DepttName, deliverydate — NO PurchaseDate, NO PODate. PO date is ApprovePO.PODate / ApprovePOHOD.PODate. For pending PO lists join PurchasePayment to ApprovePO and ApprovePOHOD (status = 'Pending').
            - Pending POs AT/FOR our company X → PurchasePayment.CompanyName = full legal name. Pending POs TO a vendor/supplier → Vw_PurchaseOrder.FirmName LIKE '%name%'. Always start FROM pending ApprovePO/ApprovePOHOD then join — never SELECT FROM Vw_PurchaseOrder without joining the pending set first (view is huge and times out).
            """;

        var resolvedOurCompany = ResolveCompanyForChat(request.Message);
        var companyHint = string.IsNullOrWhiteSpace(resolvedOurCompany)
            ? ""
            : $"""

            Resolved our-company for this question (use this exact literal for CompanyName/CompName/companyname filters):
            '{resolvedOurCompany}'
            """;

        var sqlUser = $"""
            Schema context:
            {schemaBlock}
            {companyHint}
            User question:
            {request.Message}
            """;

        List<Dictionary<string, object?>> rows = new();
        string? warning = null;
        string? supplementalAnswerContext = null;
        string sql = "";
        string? columnRepairHint = null;
        var usedErpAgeing = false;
        var usedErpLedgerStatement = false;
        var usedErpFinance = false;
        var usedErpInventory = false;
        int? ageingTotalCount = null;
        int? ledgerStatementTotalCount = null;
        int? financeTotalCount = null;
        int? inventoryTotalCount = null;
        AgeingReportPlan? exportAgeingPlan = null;
        ErpFinanceReportPlan? exportFinancePlan = null;
        ErpInventoryReportPlan? exportInventoryPlan = null;
        LedgerStatementPlan? exportLedgerPlan = null;

        // Day-bucket ageing (SELECT on vw_BillWiseTransaction) — before EXEC ageing
        if (TryBuildPartyAgeingBucketsSql(request.Message, out var partyBucketSql, out var partyBucketWarn))
        {
            _logger.LogInformation("Using governed party day-bucket ageing SQL");
            sql = partyBucketSql;
            warning = partyBucketWarn;
        }
        else if (TryBuildDebtorCreditorAgeingListSql(request.Message, out var listBucketSql, out var listBucketWarn))
        {
            _logger.LogInformation("Using governed debtor/creditor day-bucket ageing list SQL");
            sql = listBucketSql;
            warning = listBucketWarn;
        }
        // ERP inventory/stock ageing — before debtor ageing
        else if (TryBuildStockAgeingPlan(request.Message, out var stockAgeingPlan))
        {
            exportFinancePlan = stockAgeingPlan;
            _logger.LogInformation("Using ERP stock ageing SP {Sp}", stockAgeingPlan.StockAgeingSp);
            var stockResult = await _financeReportService.ExecuteAsync(stockAgeingPlan, ct);
            rows = stockResult.Rows;
            sql = stockResult.SqlDescription;
            warning = stockResult.Warning;
            financeTotalCount = stockResult.TotalCount;
            usedErpFinance = true;
        }
        else if (TryBuildGroupOverdueDaysPlan(request.Message, out var groupOverduePlan))
        {
            exportFinancePlan = groupOverduePlan;
            _logger.LogInformation("Using ERP group overdue days SP");
            var groupResult = await _financeReportService.ExecuteAsync(groupOverduePlan, ct);
            rows = groupResult.Rows;
            sql = groupResult.SqlDescription;
            warning = groupResult.Warning;
            financeTotalCount = groupResult.TotalCount;
            usedErpFinance = true;
        }
        else if (TryBuildOutstandingAllPlan(request.Message, out var outstandingAllPlan))
        {
            exportFinancePlan = outstandingAllPlan;
            _logger.LogInformation("Using ERP sp_OutstandingAll");
            var outstandingResult = await _financeReportService.ExecuteAsync(outstandingAllPlan, ct);
            rows = outstandingResult.Rows;
            sql = outstandingResult.SqlDescription;
            warning = outstandingResult.Warning;
            financeTotalCount = outstandingResult.TotalCount;
            usedErpFinance = true;
        }
        else if (TryBuildMsmeOverduePlan(request.Message, out var msmePlan))
        {
            exportFinancePlan = msmePlan;
            _logger.LogInformation("Using ERP MSME overdue SP");
            var msmeResult = await _financeReportService.ExecuteAsync(msmePlan, ct);
            rows = msmeResult.Rows;
            sql = msmeResult.SqlDescription;
            warning = msmeResult.Warning;
            financeTotalCount = msmeResult.TotalCount;
            usedErpFinance = true;
        }
        else if (TryBuildSalesDiscountPlan(request.Message, out var discountPlan))
        {
            exportFinancePlan = discountPlan;
            _logger.LogInformation("Using ERP sales discount SP");
            var discountResult = await _financeReportService.ExecuteAsync(discountPlan, ct);
            rows = discountResult.Rows;
            sql = discountResult.SqlDescription;
            warning = discountResult.Warning;
            financeTotalCount = discountResult.TotalCount;
            usedErpFinance = true;
        }
        else if (TryBuildExportDebtorsLast3MonthsPlan(request.Message, out var exportDebtorsPlan))
        {
            exportFinancePlan = exportDebtorsPlan;
            _logger.LogInformation("Using ERP export debtors last 3 months SP");
            var exportResult = await _financeReportService.ExecuteAsync(exportDebtorsPlan, ct);
            rows = exportResult.Rows;
            sql = exportResult.SqlDescription;
            warning = exportResult.Warning;
            financeTotalCount = exportResult.TotalCount;
            usedErpFinance = true;
        }
        else if (TryBuildInventoryReportPlan(request.Message, out var inventoryPlan))
        {
            exportInventoryPlan = inventoryPlan;
            _logger.LogInformation("Using ERP inventory/MIS SP mode={Mode}", inventoryPlan.Mode);
            var inventoryResult = await _inventoryReportService.ExecuteAsync(inventoryPlan, ct);
            rows = inventoryResult.Rows;
            sql = inventoryResult.SqlDescription;
            warning = inventoryResult.Warning;
            inventoryTotalCount = inventoryResult.TotalCount;
            usedErpInventory = true;
        }
        // ERP ageing (portal parity SPs) — before LLM / LedgerMaster-only outstanding
        else if (TryBuildAgeingReportPlan(request.Message, out var ageingPlan))
        {
            exportAgeingPlan = ageingPlan;
            _logger.LogInformation("Using ERP ageing service (mode={Mode})", ageingPlan.Mode);
            var ageingResult = await _ageingService.ExecuteAsync(ageingPlan, ct);
            rows = ageingResult.Rows;
            sql = ageingResult.SqlDescription;
            warning = ageingResult.Warning;
            ageingTotalCount = ageingResult.TotalCount;
            usedErpAgeing = true;
        }
        // ERP ledger statement (portal parity SP) — before LLM
        else if (TryBuildLedgerStatementPlan(request.Message, out var ledgerPlan))
        {
            exportLedgerPlan = ledgerPlan;
            _logger.LogInformation("Using ERP ledger statement service for {Ledger}", ledgerPlan.LedgerName);
            var ledgerResult = await _ledgerStatementChat.ExecuteAsync(ledgerPlan, ct);
            rows = ledgerResult.Rows;
            sql = ledgerResult.SqlDescription;
            warning = ledgerResult.Warning;
            ledgerStatementTotalCount = ledgerResult.TotalCount;
            usedErpLedgerStatement = true;
        }

        var usedCompositeOperational = false;
        var skipSqlExecution = usedErpAgeing || usedErpLedgerStatement || usedErpFinance || usedErpInventory;
        var useLlm = string.IsNullOrWhiteSpace(sql) && !skipSqlExecution;

        if (useLlm)
        {
            var composite = await TryExecuteCompositeOperationalQueryAsync(request.Message, ct);
            if (composite.Ok)
            {
                rows = composite.Rows;
                sql = composite.Sql;
                warning = composite.Warning;
                usedCompositeOperational = true;
                useLlm = false;
                skipSqlExecution = true;
            }
        }

        // Governed: pending PO approval (portal queue) — highest priority; skip LLM/PR confusion
        if (useLlm && TryBuildPendingPoApprovalSql(request.Message, "", out var earlyPendingPoSql, out var earlyPendingPoWarn))
        {
            _logger.LogInformation("Using governed pending PO SQL (early path)");
            sql = earlyPendingPoSql;
            warning = earlyPendingPoWarn;
        }
        // Phase 7 — MRN / vendor early paths (stores & procurement daily queries)
        else if (useLlm && TryBuildMrnPaymentEarlySql(request.Message, out var mrnPaySql, out var mrnPayWarn))
        {
            _logger.LogInformation("Using governed MRN payment SQL (early path)");
            sql = mrnPaySql;
            warning = mrnPayWarn;
        }
        else if (useLlm && TryBuildMrnByBillNoEarlySql(request.Message, out var mrnBillSql, out var mrnBillWarn))
        {
            _logger.LogInformation("Using governed MRN-by-bill SQL (early path)");
            sql = mrnBillSql;
            warning = mrnBillWarn;
        }
        else if (useLlm && TryBuildMrnByMrNoEarlySql(request.Message, out var mrnNoSql, out var mrnNoWarn))
        {
            _logger.LogInformation("Using governed MRN-by-MRNo SQL (early path)");
            sql = mrnNoSql;
            warning = mrnNoWarn;
        }
        else if (useLlm && TryBuildMrnPendingQtyEarlySql(request.Message, out var mrnPendingSql, out var mrnPendingWarn))
        {
            _logger.LogInformation("Using governed MRN pending qty SQL (early path)");
            sql = mrnPendingSql;
            warning = mrnPendingWarn;
        }
        else if (useLlm && TryBuildMrnPartyReceiptsEarlySql(request.Message, out var mrnPartySql, out var mrnPartyWarn))
        {
            _logger.LogInformation("Using governed MRN party receipts SQL (early path)");
            sql = mrnPartySql;
            warning = mrnPartyWarn;
        }
        else if (useLlm && TryBuildVendorProfileEarlySql(request.Message, out var vendorProfSql, out var vendorProfWarn))
        {
            _logger.LogInformation("Using governed vendor profile SQL (early path)");
            sql = vendorProfSql;
            warning = vendorProfWarn;
        }
        else if (useLlm && TryBuildVendorCodeEarlySql(request.Message, out var vendorCodeSql, out var vendorCodeWarn))
        {
            _logger.LogInformation("Using governed vendor code SQL (early path)");
            sql = vendorCodeSql;
            warning = vendorCodeWarn;
        }
        else if (useLlm && TryBuildVendorRateEarlySql(request.Message, out var vendorRateSql, out var vendorRateWarn))
        {
            _logger.LogInformation("Using governed vendor rate SQL (early path)");
            sql = vendorRateSql;
            warning = vendorRateWarn;
        }
        else if (useLlm && TryBuildMsmeVendorListEarlySql(request.Message, out var msmeListSql, out var msmeListWarn))
        {
            _logger.LogInformation("Using governed MSME vendor list SQL (early path)");
            sql = msmeListSql;
            warning = msmeListWarn;
        }
        else if (useLlm && TryBuildInternalVendorEarlySql(request.Message, out var intVendorSql, out var intVendorWarn))
        {
            _logger.LogInformation("Using governed internal vendor SQL (early path)");
            sql = intVendorSql;
            warning = intVendorWarn;
        }
        // Governed: country-wise sales (Sales Dashboard source) — before export-customer ranking
        else if (useLlm && TryBuildCountryWiseSalesSql(request.Message, out var countryWiseSql, out var countryWiseWarning))
        {
            _logger.LogInformation("Using governed country-wise sales SQL");
            sql = countryWiseSql;
            warning = countryWiseWarning;
        }
        else if (useLlm && TryBuildProductLineSalesSql(request.Message, out var productLineSalesSql, out var productLineSalesWarn))
        {
            _logger.LogInformation("Using governed product-line sales SQL (FIBC/monthly/per kg)");
            sql = productLineSalesSql;
            warning = productLineSalesWarn;
        }
        else if (useLlm && TryBuildSalesTotalsSql(request.Message, out var salesTotalsSql, out var salesTotalsWarning))
        {
            _logger.LogInformation("Using governed sales totals SQL");
            sql = salesTotalsSql;
            warning = salesTotalsWarning;
        }
        else if (useLlm && TryBuildSalesByGroupSql(request.Message, out var salesByGroupSql, out var salesByGroupWarning))
        {
            _logger.LogInformation("Using governed sales-by-group SQL");
            sql = salesByGroupSql;
            warning = salesByGroupWarning;
        }
        else if (useLlm && TryBuildExportSalesInvoiceListSql(request.Message, out var exportInvSql, out var exportInvWarn))
        {
            _logger.LogInformation("Using governed export sales invoice list SQL");
            sql = exportInvSql;
            warning = exportInvWarn;
        }
        else if (useLlm && TryBuildInterUnitSalesSql(request.Message, out var interUnitSql, out var interUnitWarn))
        {
            _logger.LogInformation("Using governed inter-unit sales SQL");
            sql = interUnitSql;
            warning = interUnitWarn;
        }
        else if (useLlm && TryBuildPurchaseTotalsSql(request.Message, out var purchaseTotalsSql, out var purchaseTotalsWarning))
        {
            _logger.LogInformation("Using governed purchase totals SQL");
            sql = purchaseTotalsSql;
            warning = purchaseTotalsWarning;
        }
        else if (useLlm && TryBuildLedgerCountSql(request.Message, out var ledgerCountSql, out var ledgerCountWarning))
        {
            _logger.LogInformation("Using governed ledger count SQL");
            sql = ledgerCountSql;
            warning = ledgerCountWarning;
        }
        else if (useLlm && TryBuildLedgerGroupsSql(request.Message, out var ledgerGroupsSql, out var ledgerGroupsWarning))
        {
            _logger.LogInformation("Using governed ledger groups SQL");
            sql = ledgerGroupsSql;
            warning = ledgerGroupsWarning;
        }
        else if (useLlm && TryBuildTopExportCustomersSql(request.Message, out var topExportSql, out var topExportWarning))
        {
            _logger.LogInformation("Using governed top-export-customers SQL");
            sql = topExportSql;
            warning = topExportWarning;
        }
        else if (useLlm && TryBuildLedgerOutstandingSql(request.Message, out var ledgerOstSql, out var ledgerOstWarning))
        {
            _logger.LogInformation("Using governed ledger-outstanding SQL");
            sql = ledgerOstSql;
            warning = ledgerOstWarning;
        }
        else if (useLlm && TryBuildStockInHandSql(request.Message, out var stockInHandSql, out var stockInHandWarn))
        {
            _logger.LogInformation("Using governed stock-in-hand SQL");
            sql = stockInHandSql;
            warning = stockInHandWarn;
        }
        // Governed procurement / quality early paths (before LLM SQL)
        else if (useLlm && TryBuildFinalQuotationSql(request.Message, out var finalQuoteSql, out var finalQuoteWarn))
        {
            _logger.LogInformation("Using governed FinalQuotation SQL");
            sql = finalQuoteSql;
            warning = finalQuoteWarn;
        }
        else if (useLlm && TryBuildQuotationByPoSql(request.Message, out var quotePoSql, out var quotePoWarn))
        {
            _logger.LogInformation("Using governed Vw_Quotation by PO SQL");
            sql = quotePoSql;
            warning = quotePoWarn;
        }
        else if (useLlm && TryBuildIndentQuotationSql(request.Message, out var indentQuoteSql, out var indentQuoteWarn))
        {
            _logger.LogInformation("Using governed Vw_IndentQuotation SQL");
            sql = indentQuoteSql;
            warning = indentQuoteWarn;
        }
        else if (useLlm && TryBuildSalesInvoiceItemsSql(request.Message, out var invItemsSql, out var invItemsWarn))
        {
            _logger.LogInformation("Using governed SalesVoucherItem SQL");
            sql = invItemsSql;
            warning = invItemsWarn;
        }
        else if (useLlm && TryBuildCreditNoteListSql(request.Message, out var creditListSql, out var creditListWarn))
        {
            _logger.LogInformation("Using governed credit note list SQL");
            sql = creditListSql;
            warning = creditListWarn;
        }
        else if (useLlm && TryBuildDebitNoteListSql(request.Message, out var debitListSql, out var debitListWarn))
        {
            _logger.LogInformation("Using governed debit note list SQL");
            sql = debitListSql;
            warning = debitListWarn;
        }
        else if (useLlm && TryBuildGatePassEarlySql(request.Message, out var gateEarlySql, out var gateEarlyWarn))
        {
            _logger.LogInformation("Using governed gate pass early SQL");
            sql = gateEarlySql;
            warning = gateEarlyWarn;
        }
        else if (useLlm && TryBuildIssueSlipEarlySql(request.Message, out var issueSlipEarlySql, out var issueSlipEarlyWarn))
        {
            _logger.LogInformation("Using governed issue slip early SQL");
            sql = issueSlipEarlySql;
            warning = issueSlipEarlyWarn;
        }
        else if (useLlm && TryBuildTodayOutwardEarlySql(request.Message, out var todayOutSql, out var todayOutWarn))
        {
            _logger.LogInformation("Using governed today-outward early SQL");
            sql = todayOutSql;
            warning = todayOutWarn;
        }
        else if (useLlm && TryBuildJobWorkOrderSql(request.Message, out var jwoSql, out var jwoWarn))
        {
            _logger.LogInformation("Using governed job work order SQL");
            sql = jwoSql;
            warning = jwoWarn;
        }
        else if (useLlm && TryBuildJobWorkEbdSql(request.Message, out var jwEbdSql, out var jwEbdWarn))
        {
            _logger.LogInformation("Using governed job work EBD SQL");
            sql = jwEbdSql;
            warning = jwEbdWarn;
        }
        else if (useLlm && TryBuildJobWorkReceiptSql(request.Message, out var jwRecSql, out var jwRecWarn))
        {
            _logger.LogInformation("Using governed job work receipt SQL");
            sql = jwRecSql;
            warning = jwRecWarn;
        }
        else if (useLlm && TryBuildPoPendingReceiptSql(request.Message, out var poPendingSql, out var poPendingWarn))
        {
            _logger.LogInformation("Using governed PO pending receipt SQL");
            sql = poPendingSql;
            warning = poPendingWarn;
        }
        else if (useLlm && TryBuildFibcBagProductionSql(request.Message, out var fibcProdSql, out var fibcProdWarn))
        {
            _logger.LogInformation("Using governed FIBC bag production SQL");
            sql = fibcProdSql;
            warning = fibcProdWarn;
        }
        else if (useLlm && TryBuildDepartmentWastageSql(request.Message, out var deptWastageSql, out var deptWastageWarn))
        {
            _logger.LogInformation("Using governed department wastage SQL");
            sql = deptWastageSql;
            warning = deptWastageWarn;
        }
        else if (useLlm && TryBuildStitcherAttendanceSql(request.Message, out var stitcherAttSql, out var stitcherAttWarn))
        {
            _logger.LogInformation("Using governed stitcher attendance SQL");
            sql = stitcherAttSql;
            warning = stitcherAttWarn;
        }
        else if (useLlm && TryBuildTapePlantEarlySql(request.Message, out var tapePlantSql, out var tapePlantWarn))
        {
            _logger.LogInformation("Using governed tape plant production SQL (early path)");
            sql = tapePlantSql;
            warning = tapePlantWarn;
        }
        else if (useLlm && TryBuildFactoryProductionEarlySql(request.Message, out var factoryProdSql, out var factoryProdWarn))
        {
            _logger.LogInformation("Using governed factory production SQL (early path)");
            sql = factoryProdSql;
            warning = factoryProdWarn;
        }
        else if (useLlm && TryBuildWipReportEarlySql(request.Message, out var wipSql, out var wipWarn))
        {
            _logger.LogInformation("Using governed WIP report SQL (early path)");
            sql = wipSql;
            warning = wipWarn;
        }
        else if (useLlm && TryBuildProductionEbdEarlySql(request.Message, out var prodEbdSql, out var prodEbdWarn))
        {
            _logger.LogInformation("Using governed production EBD SQL (early path)");
            sql = prodEbdSql;
            warning = prodEbdWarn;
        }
        else if (useLlm && TryBuildRollDespatchEarlySql(request.Message, out var rollDespSql, out var rollDespWarn))
        {
            _logger.LogInformation("Using governed roll despatch SQL (early path)");
            sql = rollDespSql;
            warning = rollDespWarn;
        }
        else if (useLlm && TryBuildFibcDespatchEarlySql(request.Message, out var fibcDespSql, out var fibcDespWarn))
        {
            _logger.LogInformation("Using governed FIBC despatch SQL (early path)");
            sql = fibcDespSql;
            warning = fibcDespWarn;
        }
        else if (useLlm && TryBuildYarnDespatchEarlySql(request.Message, out var yarnDespSql, out var yarnDespWarn))
        {
            _logger.LogInformation("Using governed yarn despatch SQL (early path)");
            sql = yarnDespSql;
            warning = yarnDespWarn;
        }
        else if (useLlm && TryBuildSmallBagDespatchEarlySql(request.Message, out var sbDespSql, out var sbDespWarn))
        {
            _logger.LogInformation("Using governed small-bag despatch SQL (early path)");
            sql = sbDespSql;
            warning = sbDespWarn;
        }
        else if (useLlm && TryBuildUserLookupEarlySql(request.Message, out var userLookupSql, out var userLookupWarn))
        {
            _logger.LogInformation("Using governed user lookup SQL (early path)");
            sql = userLookupSql;
            warning = userLookupWarn;
        }
        else if (useLlm && TryBuildIndentItemsEarlySql(request.Message, out var indentItemsSql, out var indentItemsWarn))
        {
            _logger.LogInformation("Using governed indent items SQL (early path)");
            sql = indentItemsSql;
            warning = indentItemsWarn;
        }
        else if (useLlm && TryBuildSalesEbdEarlySql(request.Message, out var salesEbdSql, out var salesEbdWarn))
        {
            _logger.LogInformation("Using governed sales EBD SQL (early path)");
            sql = salesEbdSql;
            warning = salesEbdWarn;
        }
        else if (useLlm && TryBuildExportDebtorsDueSql(request.Message, out var exportDueSql, out var exportDueWarn))
        {
            _logger.LogInformation("Using governed export debtors due SQL");
            sql = exportDueSql;
            warning = exportDueWarn;
        }
        else if (useLlm && TryBuildJobMrnPendingWoSql(request.Message, out var jobMrnSql, out var jobMrnWarn))
        {
            _logger.LogInformation("Using governed job MRN pending WO SQL");
            sql = jobMrnSql;
            warning = jobMrnWarn;
        }
        else if (useLlm && TryBuildPoAmendmentSql(request.Message, out var poAmendSql, out var poAmendWarn))
        {
            _logger.LogInformation("Using governed PO amendment SQL");
            sql = poAmendSql;
            warning = poAmendWarn;
        }
        else if (useLlm && TryBuildBillPaymentDraftSql(request.Message, out var billDraftSql, out var billDraftWarn))
        {
            _logger.LogInformation("Using governed bill payment draft SQL");
            sql = billDraftSql;
            warning = billDraftWarn;
        }
        else if (useLlm && TryBuildPurchaseReqSql(request.Message, out var prSql, out var prWarn))
        {
            _logger.LogInformation("Using governed purchase requisition SQL");
            sql = prSql;
            warning = prWarn;
        }
        else if (useLlm && TryBuildSmallBagProductionSql(request.Message, out var sbProdSql, out var sbProdWarn))
        {
            _logger.LogInformation("Using governed small-bag production SQL");
            sql = sbProdSql;
            warning = sbProdWarn;
        }
        else if (useLlm && TryBuildLedgerGroupingSql(request.Message, out var ledgerGrpSql, out var ledgerGrpWarn))
        {
            _logger.LogInformation("Using governed ledger grouping SQL");
            sql = ledgerGrpSql;
            warning = ledgerGrpWarn;
        }
        else if (useLlm && TryBuildAccountVoucherApprovalSql(request.Message, out var voucherApprSql, out var voucherApprWarn))
        {
            _logger.LogInformation("Using governed account voucher approval SQL");
            sql = voucherApprSql;
            warning = voucherApprWarn;
        }
        else if (useLlm && TryBuildVoucherPartySql(request.Message, out var voucherPartySql, out var voucherPartyWarn))
        {
            _logger.LogInformation("Using governed voucher party SQL");
            sql = voucherPartySql;
            warning = voucherPartyWarn;
        }
        else if (useLlm && TryBuildEditPurchaseOrderSql(request.Message, out var editPoSql, out var editPoWarn))
        {
            _logger.LogInformation("Using governed edit PO SQL");
            sql = editPoSql;
            warning = editPoWarn;
        }
        else if (useLlm && TryBuildImportPoMrnPendingSql(request.Message, out var importPoMrnSql, out var importPoMrnWarn))
        {
            _logger.LogInformation("Using governed import PO/MRN pending SQL");
            sql = importPoMrnSql;
            warning = importPoMrnWarn;
        }
        else if (useLlm && TryBuildPurchaseVoucherSql(request.Message, out var purchaseVoucherSql, out var purchaseVoucherWarn))
        {
            _logger.LogInformation("Using governed purchase voucher SQL");
            sql = purchaseVoucherSql;
            warning = purchaseVoucherWarn;
        }
        else if (useLlm && TryBuildPaymentVoucherSql(request.Message, out var paymentVoucherSql, out var paymentVoucherWarn))
        {
            _logger.LogInformation("Using governed payment voucher SQL");
            sql = paymentVoucherSql;
            warning = paymentVoucherWarn;
        }
        else if (useLlm && TryBuildPaymentReceiptSql(request.Message, out var paymentReceiptSql, out var paymentReceiptWarn))
        {
            _logger.LogInformation("Using governed payment receipt SQL");
            sql = paymentReceiptSql;
            warning = paymentReceiptWarn;
        }
        else if (useLlm && TryBuildAdvanceBillOutstandingSql(request.Message, out var advanceBillSql, out var advanceBillWarn))
        {
            _logger.LogInformation("Using governed advance bill outstanding SQL");
            sql = advanceBillSql;
            warning = advanceBillWarn;
        }
        else if (useLlm && TryBuildDueOverDueSql(request.Message, out var dueOverdueSql, out var dueOverdueWarn))
        {
            _logger.LogInformation("Using governed due/overdue summary SQL");
            sql = dueOverdueSql;
            warning = dueOverdueWarn;
        }
        else if (useLlm && TryBuildDueDateCashFlowSql(request.Message, out var cashFlowSql, out var cashFlowWarn))
        {
            _logger.LogInformation("Using governed due-date cash flow SQL");
            sql = cashFlowSql;
            warning = cashFlowWarn;
        }
        else if (useLlm && TryBuildGovernedDomainSql(request.Message, out var govSql, out var govWarning))
        {
            _logger.LogInformation("Using governed domain SQL");
            sql = govSql;
            warning = govWarning;
        }
        else if (useLlm && LooksLikeLedgerStatementIntent(request.Message)
                 && !TryBuildLedgerStatementPlan(request.Message, out _))
        {
            throw new InvalidOperationException(
                "Could not match a ledger/party name in our books for that company. " +
                "Try naming the customer and plant, e.g. Commercial Bag Company at Plastene Polyfilms.");
        }
        else if (useLlm)
        {
            var sqlRaw = await _llm.CompleteAsync(sqlSystem, sqlUser, ct);
            sql = ApplySqlPostProcess(sqlRaw, request.Message, out columnRepairHint);

            if (TryBuildCountryWiseSalesSql(request.Message, out var countryRewriteSql, out var countryRewriteWarn)
                && ShouldRewriteToCountryWiseSales(sql))
            {
                _logger.LogInformation("Rewriting LLM SQL to governed country-wise sales query");
                sql = countryRewriteSql;
                warning = countryRewriteWarn;
                columnRepairHint = null;
            }
            else if (TryBuildPendingPoApprovalSql(request.Message, sql, out var pendingPoSql, out var pendingPoWarning))
            {
                _logger.LogInformation("Rewriting pending PO query ({Mode})", pendingPoWarning);
                sql = pendingPoSql;
                warning = pendingPoWarning;
                columnRepairHint = null; // governed SQL is catalog-safe
            }
            else if (TryBuildStockInHandSql(request.Message, out var stockRewriteSql, out var stockRewriteWarn))
            {
                _logger.LogInformation("Rewriting stock-in-hand query ({Mode})", stockRewriteWarn);
                sql = stockRewriteSql;
                warning = stockRewriteWarn;
                columnRepairHint = null;
            }
            else if (TryBuildExportSalesInvoiceListSql(request.Message, out var exportInvRewriteSql, out var exportInvRewriteWarn))
            {
                _logger.LogInformation("Rewriting export sales invoice query ({Mode})", exportInvRewriteWarn);
                sql = exportInvRewriteSql;
                warning = exportInvRewriteWarn;
                columnRepairHint = null;
            }
            else if (TryBuildPendingWorkOrderSql(request.Message, sql, out var pendingWoSql, out var pendingWoWarning))
            {
                _logger.LogInformation("Rewriting pending WO query ({Mode})", pendingWoWarning);
                sql = pendingWoSql;
                warning = pendingWoWarning;
                columnRepairHint = null;
            }
            else if (TryBuildLedgerOutstandingSql(request.Message, out var pendingLedgerSql, out var pendingLedgerWarn))
            {
                _logger.LogInformation("Rewriting to governed ledger-outstanding SQL");
                sql = pendingLedgerSql;
                warning = pendingLedgerWarn;
                columnRepairHint = null;
            }
        }

        // Still-unknown columns after auto-fix → one targeted repair before execute
        if (!skipSqlExecution && !string.IsNullOrWhiteSpace(columnRepairHint))
        {
            _logger.LogWarning("Unresolved hallucinated columns; requesting catalog-aware repair");
            var colRepairUser = $"""
                The previous SQL uses invalid column names.

                Question: {request.Message}

                Schema context:
                {schemaBlock}

                Failed SQL:
                {sql}

                {columnRepairHint}

                Return ONE corrected SELECT/WITH query only. Use exact column names from the schema.
                """;
            var sqlRaw = await _llm.CompleteAsync(sqlSystem, colRepairUser, ct);
            sql = ApplySqlPostProcess(sqlRaw, request.Message, out columnRepairHint);
            if (TryBuildCountryWiseSalesSql(request.Message, out var colCountryWise, out var colCountryWiseWarn))
            {
                sql = colCountryWise;
                warning = colCountryWiseWarn;
                columnRepairHint = null;
            }
            else if (TryBuildTopExportCustomersSql(request.Message, out var colTopExport, out var colTopExportWarn))
            {
                sql = colTopExport;
                warning = colTopExportWarn;
                columnRepairHint = null;
            }
            else if (TryBuildLedgerOutstandingSql(request.Message, out var colLedgerOst, out var colLedgerOstWarn))
            {
                sql = colLedgerOst;
                warning = colLedgerOstWarn;
                columnRepairHint = null;
            }
            else if (TryBuildGovernedDomainSql(request.Message, out var colGov, out var colGovWarn))
            {
                sql = colGov;
                warning = colGovWarn;
                columnRepairHint = null;
            }
            else if (TryBuildPendingWorkOrderSql(request.Message, sql, out var colPendingWo, out var colPendingWoWarn))
            {
                sql = colPendingWo;
                warning = colPendingWoWarn;
            }
            else if (TryBuildPendingPoApprovalSql(request.Message, sql, out var colPendingSql, out var colPendingWarn))
            {
                sql = colPendingSql;
                warning = colPendingWarn;
            }
        }

        if (!skipSqlExecution)
        {
        try
        {
            rows = await ExecuteReadOnlyAsync(sql, ct);
        }
        catch (Exception ex)
        {
            var recovered = false;

            // Timeout / bad join: force governed rewrites before LLM repair
            if (TryBuildTopExportCustomersSql(request.Message, out var timeoutExportSql, out var timeoutExportWarn)
                && !string.Equals(timeoutExportSql, sql, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _logger.LogWarning(ex, "SQL failed; retrying governed top-export-customers rewrite");
                    sql = timeoutExportSql;
                    rows = await ExecuteReadOnlyAsync(sql, ct);
                    warning = timeoutExportWarn;
                    recovered = true;
                }
                catch (Exception retryEx)
                {
                    _logger.LogWarning(retryEx, "Governed top-export-customers rewrite also failed");
                }
            }

            if (!recovered
                && TryBuildLedgerOutstandingSql(request.Message, out var timeoutLedgerSql, out var timeoutLedgerWarn)
                && !string.Equals(timeoutLedgerSql, sql, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _logger.LogWarning(ex, "SQL failed; retrying governed ledger-outstanding rewrite");
                    sql = timeoutLedgerSql;
                    rows = await ExecuteReadOnlyAsync(sql, ct);
                    warning = timeoutLedgerWarn;
                    recovered = true;
                }
                catch (Exception retryEx)
                {
                    _logger.LogWarning(retryEx, "Governed ledger-outstanding rewrite also failed");
                }
            }

            if (!recovered
                && TryBuildGovernedDomainSql(request.Message, out var timeoutGovSql, out var timeoutGovWarn)
                && !string.Equals(timeoutGovSql, sql, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _logger.LogWarning(ex, "SQL failed; retrying governed domain rewrite");
                    sql = timeoutGovSql;
                    rows = await ExecuteReadOnlyAsync(sql, ct);
                    warning = timeoutGovWarn;
                    recovered = true;
                }
                catch (Exception retryEx)
                {
                    _logger.LogWarning(retryEx, "Governed domain rewrite also failed");
                }
            }

            if (!recovered
                && TryBuildPendingWorkOrderSql(request.Message, sql, out var timeoutWoSql, out var timeoutWoWarning)
                && !string.Equals(timeoutWoSql, sql, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _logger.LogWarning(ex, "SQL failed; retrying governed pending WO rewrite");
                    sql = timeoutWoSql;
                    rows = await ExecuteReadOnlyAsync(sql, ct);
                    warning = timeoutWoWarning;
                    recovered = true;
                }
                catch (Exception retryEx)
                {
                    _logger.LogWarning(retryEx, "Governed pending WO rewrite also failed");
                }
            }

            if (!recovered
                && TryBuildPendingPoApprovalSql(request.Message, sql, out var timeoutPoSql, out var timeoutPoWarning)
                && !string.Equals(timeoutPoSql, sql, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _logger.LogWarning(ex, "SQL failed; retrying governed pending PO rewrite");
                    sql = timeoutPoSql;
                    rows = await ExecuteReadOnlyAsync(sql, ct);
                    warning = timeoutPoWarning;
                    recovered = true;
                }
                catch (Exception retryEx)
                {
                    _logger.LogWarning(retryEx, "Governed pending PO rewrite also failed");
                }
            }

            if (!recovered && await TryGovernedVendorProfileRewriteAsync(request.Message, sql, ct) is { } governed)
            {
                _logger.LogWarning(ex, "SQL failed for vendor profile; using governed Vendor rewrite");
                sql = governed.Sql;
                rows = governed.Rows;
                warning = governed.Warning;
                recovered = true;
            }

            if (!recovered)
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
                Reminder: PurchasePayment has deliverydate only — NO PurchaseDate/PODate; join ApprovePO/ApprovePOHOD for PODate on pending PO lists.
                Reminder: pending POs TO a vendor use Vw_PurchaseOrder.FirmName LIKE — start FROM pending ApprovePO/ApprovePOHOD then join; never scan full Vw_PurchaseOrder unfiltered. 'at company X' = PurchasePayment.CompanyName; 'to vendor X' = FirmName.
                {(columnRepairHint is null ? "" : "\n" + columnRepairHint)}
                Return ONE corrected SELECT/WITH query only. No explanation.
                """;
            var sqlRaw = await _llm.CompleteAsync(sqlSystem, repairUser, ct);
            sql = ApplySqlPostProcess(sqlRaw, request.Message, out _);
            if (TryBuildTopExportCustomersSql(request.Message, out var repairExportSql, out var repairExportWarn))
            {
                sql = repairExportSql;
                warning = repairExportWarn;
            }
            else if (TryBuildLedgerOutstandingSql(request.Message, out var repairLedgerSql, out var repairLedgerWarn))
            {
                sql = repairLedgerSql;
                warning = repairLedgerWarn;
            }
            else if (TryBuildPendingWorkOrderSql(request.Message, sql, out var repairPendingWoSql, out var repairPendingWoWarning))
            {
                sql = repairPendingWoSql;
                warning = repairPendingWoWarning;
            }
            else if (TryBuildPendingPoApprovalSql(request.Message, sql, out var repairPendingPoSql, out var repairPendingWarning))
            {
                sql = repairPendingPoSql;
                warning = repairPendingWarning;
            }
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

        // Safety net: LLM or wrong-table SQL must not answer pending PO questions with 0 rows.
        if (LooksLikePendingPoQuestion(request.Message)
            && ShouldForcePendingPoGovernedRewrite(sql, rows.Count)
            && TryBuildPendingPoApprovalSql(request.Message, sql, out var forcedPendingPoSql, out var forcedPendingPoWarn))
        {
            _logger.LogWarning(
                "Forcing governed pending PO rewrite (prior SQL used wrong tables or returned empty)");
            sql = forcedPendingPoSql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = forcedPendingPoWarn;
        }

        // Safety net: exact ItemName match on stock queries often returns 0 — use LIKE filters.
        if (LooksLikeStockInHandQuestion(request.Message)
            && ShouldForceStockInHandGovernedRewrite(sql, rows.Count)
            && TryBuildStockInHandSql(request.Message, out var forcedStockSql, out var forcedStockWarn))
        {
            _logger.LogWarning(
                "Forcing governed stock-in-hand rewrite (exact ItemName or wrong table returned empty)");
            sql = forcedStockSql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = forcedStockWarn;
        }

        if (ShouldForceExportSalesInvoiceRewrite(request.Message, sql, rows.Count)
            && TryBuildExportSalesInvoiceListSql(request.Message, out var forcedExportInvSql, out var forcedExportInvWarn))
        {
            _logger.LogWarning(
                "Forcing governed export sales invoice rewrite (POAllocation/wrong table or empty export list)");
            sql = forcedExportInvSql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = forcedExportInvWarn;
        }

        if (LooksLikeLedgerCountQuestion(request.Message)
            && LedgerCountSqlMissingUnderFilter(request.Message, sql)
            && TryBuildLedgerCountSql(request.Message, out var forcedLedgerCountSql, out var forcedLedgerCountWarn))
        {
            _logger.LogWarning("Forcing governed ledger count with LedgerMaster.Under group filter");
            sql = forcedLedgerCountSql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = forcedLedgerCountWarn;
        }

        if (!skipSqlExecution
            && !LooksLikeAgeingQuestion(request.Message)
            && LooksLikeNamedLedgerOutstandingQuestion(request.Message)
            && IsLedgerMasterOutstandingSql(sql)
            && IsStaleLedgerMasterBalanceResult(rows)
            && await TryEnrichStaleLedgerOutstandingAsync(request.Message, rows, ct) is { } enriched)
        {
            _logger.LogWarning("LedgerMaster pending/opening zero; applying ERP outstanding enrichment");
            rows = enriched.Rows;
            sql = enriched.Sql;
            warning = enriched.Warning;
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
            if (TryBuildLedgerOutstandingSql(request.Message, out var namedOstSql, out var namedOstWarn))
            {
                _logger.LogWarning("Empty/wrong ledger balance path; using governed named-party outstanding");
                sql = namedOstSql;
                warning = namedOstWarn;
            }
            else
            {
                _logger.LogWarning("LedgerOpeningBalance is empty; rewriting to LedgerMaster balances");
                var company = TryExtractCompanyName(request.Message)
                              ?? ResolveOutwardCompanyAlias(request.Message);
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
                warning = "Rewrote LedgerOpeningBalance (empty table) to LedgerMaster Openingbalance/PendingBalance (governed).";
            }
            rows = await ExecuteReadOnlyAsync(sql, ct);
        }
        else if (rows.Count == 0
                 && TryBuildLedgerOutstandingSql(request.Message, out var emptyLedgerOstSql, out var emptyLedgerOstWarn)
                 && (!sql.Contains("LedgerMaster", StringComparison.OrdinalIgnoreCase)
                     || !sql.Contains("LedgerName", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Empty named ledger outstanding; rewriting to governed LedgerMaster LIKE");
            sql = emptyLedgerOstSql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = emptyLedgerOstWarn;
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
        else if (TryBuildDailyInwardOutwardSql(request.Message, out var dailyIoSql, out var dailyIoWarn)
                 && ShouldRewriteToDailyInwardOutward(sql, request.Message))
        {
            _logger.LogWarning(
                "Wrong singular StoreOutwards TOP 1 for plural/today inward-outward question; rewriting to vw_ItemInwardOutward");
            sql = dailyIoSql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = dailyIoWarn;
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
                 && LooksLikeCountryWiseSalesQuestion(request.Message)
                 && TryBuildCountryWiseSalesSql(request.Message, out var emptyCountrySql, out var emptyCountryWarn)
                 && !string.Equals(emptyCountrySql, sql, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Empty country-wise sales result; retrying governed SQL");
            sql = emptyCountrySql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = emptyCountryWarn;
        }
        else if (rows.Count == 0
                 && LooksLikeCountryWiseSalesQuestion(request.Message)
                 && sql.Contains("GroupName", StringComparison.OrdinalIgnoreCase)
                 && TryBuildCountryWiseSalesFallbackSql(request.Message, out var fallbackCountrySql, out var fallbackCountryWarn))
        {
            _logger.LogWarning("Empty country-wise GroupName filter; falling back to GroupName LIKE");
            sql = fallbackCountrySql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = fallbackCountryWarn;
        }
        else if (rows.Count == 0
                 && LooksLikeTopExportCustomersQuestion(request.Message)
                 && TryBuildTopExportCustomersSql(request.Message, out var emptyExportSql, out var emptyExportWarn)
                 && !string.Equals(emptyExportSql, sql, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Empty top-export-customers result; retrying governed SQL");
            sql = emptyExportSql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = emptyExportWarn;
        }
        else if (rows.Count == 0
                 && LooksLikeTopExportCustomersQuestion(request.Message)
                 && sql.Contains("FactoryInfo", StringComparison.OrdinalIgnoreCase)
                 && TryBuildTopExportCustomersFallbackSql(request.Message, out var fallbackExportSql, out var fallbackExportWarn))
        {
            // GroupName may not match — fall back to CompanyName LIKE
            _logger.LogWarning("Empty top-export group filter; falling back to CompanyName LIKE");
            sql = fallbackExportSql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = fallbackExportWarn;
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
        else if (rows.Count == 0
                 && request.Message.Contains("needle", StringComparison.OrdinalIgnoreCase)
                 && sql.Contains("vw_RollforDespatch", StringComparison.OrdinalIgnoreCase)
                 && TryBuildRollsWaitingDespatchSql(request.Message, out var rollRelaxedSql, out var rollRelaxedWarn, relaxNeedleFilter: true)
                 && !string.Equals(rollRelaxedSql, sql, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Empty needle-loom despatch result; relaxing to all rolls waiting on vw_RollforDespatch");
            sql = rollRelaxedSql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = rollRelaxedWarn;
        }
        else if (rows.Count == 0
                 && LooksLikePendingIndentQuestion(request.Message)
                 && (TryExtractDepartmentFragment(request.Message)?.Equals("Store", StringComparison.OrdinalIgnoreCase) == true
                     || request.Message.Contains("store indent", StringComparison.OrdinalIgnoreCase))
                 && TryBuildPendingIndentSql(request.Message, out var indentRelaxedSql, out var indentRelaxedWarn, relaxStoreDept: true)
                 && !string.Equals(indentRelaxedSql, sql, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Empty store-dept pending indent result; retrying with relaxed Store filter (company-only)");
            sql = indentRelaxedSql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = indentRelaxedWarn;
        }
        else if (rows.Count == 0
                 && LooksLikeAboveMaxStockQuestion(request.Message)
                 && sql.Contains("WareHouse", StringComparison.OrdinalIgnoreCase)
                 && TryBuildAboveMaxStockViaInventorySql(request.Message, out var invMaxSql, out var invMaxWarn))
        {
            _logger.LogWarning("Empty above-max on WareHouse; retrying vw_inventoryitemwarehouse_all join to WareHouse max levels");
            sql = invMaxSql;
            rows = await ExecuteReadOnlyAsync(sql, ct);
            warning = invMaxWarn;
        }
        else if (rows.Count == 0
                 && sql.Contains("SmallBagProductionEntry", StringComparison.OrdinalIgnoreCase)
                 && LooksLikeSmallBagProductionQuestion(request.Message))
        {
            var company = TryExtractSqlCompanyNameLiteral(sql) ?? TryExtractCompanyName(request.Message);
            if (!string.IsNullOrWhiteSpace(company))
            {
                _logger.LogWarning(
                    "Empty SmallBagProductionEntry for {Company}; fetching factory production summary",
                    company);
                var summarySql = $"""
                    SELECT TOP 20 Particulars, COUNT(*) AS entryCnt, MAX(Sysdate) AS lastDate, SUM(ISNULL(SmallBag, 0)) AS totalSmallBag
                    FROM vw_FactoryProduction
                    WHERE companyname = '{EscapeSqlLiteral(company)}'
                    GROUP BY Particulars
                    ORDER BY COUNT(*) DESC
                    """;
                var summaryRows = await ExecuteReadOnlyAsync(summarySql, ct);
                if (summaryRows.Count > 0)
                {
                    supplementalAnswerContext = JsonSerializer.Serialize(summaryRows);
                    warning =
                        "SmallBagProductionEntry returned no rows; included vw_FactoryProduction summary by Particulars for context (governed).";
                }
            }
        }
        }
        }

        var hitCap = rows.Count > MaxReturnRows;
        if (hitCap)
            rows = rows.Take(MaxReturnRows).ToList();

        var erpTotalCount = usedErpAgeing ? ageingTotalCount
            : usedErpLedgerStatement ? ledgerStatementTotalCount
            : usedErpFinance ? financeTotalCount
            : usedErpInventory ? inventoryTotalCount
            : null;
        var (totalCount, truncated) = erpTotalCount.HasValue
            ? (erpTotalCount, erpTotalCount.Value > rows.Count)
            : await ResolveListCardinalityAsync(sql, rows, hitCap, ct);

        var preview = JsonSerializer.Serialize(rows);
        if (preview.Length > 12000)
            preview = preview[..12000] + "...(truncated)";

        var cardinalityNote = totalCount.HasValue
            ? truncated
                ? $"Cardinality: showing {rows.Count} of {totalCount.Value} matching rows (chat caps at {MaxReturnRows}). State the full count clearly and note that Export CSV has the full set."
                : $"Cardinality: {totalCount.Value} matching row(s); all are included below."
            : truncated
                ? $"Cardinality: result capped at {MaxReturnRows} rows; full count unknown. Say you are showing a sample and suggest Export CSV for more."
                : "";

        var answerSystem = """
            You answer business questions using ONLY the SQL result data provided.
            Be concise and factual. If the result is empty, say so.
            For debtor/creditor ageing results: explain monthly bucket columns (Opening, month names, Total) or bill-wise overdue columns from the ERP ageing report; mention the as-on date from the warning if present.
            For day-bucket ageing (Bucket_0_30, Bucket_31_60, etc.): summarize each bucket and total outstanding; note age basis is BillDate (VoucherDate fallback).
            For ledger statement results: summarize opening/closing from the warning; list key vouchers (Date, Particulars, VoucherNo, Debit, Credit, Closing).
            If supplemental context is provided for an empty small-bag query, explain that SmallBagProductionEntry has no rows for that company and summarize what production types (Particulars) the company does have instead — do not invent small-bag figures.
            Do not invent numbers. Mention key figures clearly.
            For payment questions: ignore rows where PaymentNo is null; only null/empty after filtering means no payment.
            If multiple payment rows exist, list each PaymentNo with amount and give a total when useful.
            For receipt/bill questions: if multiple distinct MRNo/SrNo values appear, say how many distinct receipts and list them — do not claim a single receipt when several exist.
            When cardinality notes say the chat is capped, never imply the sample size is the full population.
            For multi-row results: summarize intelligently from the data shape — do NOT list every row.
            - If rows group by country/buyer/party/customer/vendor/ledger/item/department: give grand total of the main amount/qty column and name the top 5 by value (e.g. "Top countries: USA ₹X, UK ₹Y…").
            - If many numeric columns exist: state row count and sum the primary amount/qty totals.
            - If a simple list with no clear measure: name up to 5 distinct key values and how many total.
            Keep the answer to 2-4 sentences; put detailed rows in the table, not prose.
            """;
        var answerUser = $"""
            Question: {request.Message}

            SQL used:
            {sql}

            {cardinalityNote}

            Result rows (JSON):
            {preview}
            {(string.IsNullOrEmpty(supplementalAnswerContext)
                ? ""
                : $"""

            Supplemental context (factory production by Particulars for same company — use only when main result is empty):
            {supplementalAnswerContext}
            """)}

            Write a short natural-language answer.
            """;

        var answer = ShouldUseDeterministicAnswer(skipSqlExecution, warning, sql)
            ? BuildDeterministicAnswer(request.Message, rows, warning, totalCount, truncated)
            : await _llm.CompleteAsync(answerSystem, answerUser, ct);

        if (truncated)
        {
            var capMsg = totalCount.HasValue
                ? $"Showing {rows.Count} of {totalCount.Value} rows (chat capped at {MaxReturnRows}). Use Export CSV for the full set."
                : $"Result truncated to {MaxReturnRows} rows. Use Export CSV for more.";
            warning = string.IsNullOrEmpty(warning) ? capMsg : warning + " " + capMsg;
        }

        return new ChatResponse
        {
            Answer = answer,
            Sql = sql,
            TablesUsed = BuildTablesUsed(chunks, sql, warning),
            Rows = rows,
            RowCount = rows.Count,
            TotalCount = totalCount,
            Truncated = truncated,
            Warning = warning,
            ExportContext = BuildExportContext(
                exportAgeingPlan,
                exportFinancePlan,
                exportInventoryPlan,
                exportLedgerPlan),
        };
        }
        finally
        {
            CurrentEntities.Value = null;
        }
    }

    public async Task<ChatExportResult> ExportCsvAsync(string rawSql, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawSql))
            throw new ArgumentException("Sql is required.");

        var sql = _sqlGuard.NormalizeAndValidate(rawSql);
        var exportSql = StripLeadingTop(sql);

        int? totalCount = null;
        if (TryBuildCountWrapperSql(exportSql, out var countSql))
        {
            try
            {
                var countRows = await ExecuteReadOnlyAsync(countSql, ct, maxRows: 1);
                totalCount = TryReadFirstInt(countRows);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Export count query failed");
            }
        }

        var rows = await ExecuteReadOnlyAsync(exportSql, ct, maxRows: MaxExportRows);
        var truncated = rows.Count > MaxExportRows;
        if (truncated)
            rows = rows.Take(MaxExportRows).ToList();

        if (totalCount.HasValue)
            truncated = totalCount.Value > rows.Count;

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        return new ChatExportResult
        {
            CsvBytes = BuildCsvBytes(rows),
            FileName = $"assistant-export-{stamp}.csv",
            RowCount = rows.Count,
            TotalCount = totalCount,
            Truncated = truncated
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

    /// <summary>
    /// Named customer/vendor/party ledger outstanding → LedgerMaster.LedgerName LIKE (governed).
    /// </summary>
    private static bool TryBuildLedgerOutstandingSql(
        string message,
        out string sql,
        out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeOpeningPendingBalanceQuestion(message)
            && !LooksLikeLooseOutstandingBalanceQuestion(message))
            return false;

        if (LooksLikeAgeingQuestion(message))
            return false;

        if (LooksLikeLedgerStatementQuestion(message))
            return false;

        var party = ResolveLedgerPartyForChat(message);
        if (string.IsNullOrWhiteSpace(party)
            || ChatEntityResolutionService.IsGarbagePartyHint(party))
            return false;

        // Our-company-only questions are company-wide lists, not a named party
        var asOurCompany = CanonicalizeCompanyName(party) ?? ResolveOutwardCompanyAlias(party);
        if (!string.IsNullOrWhiteSpace(asOurCompany)
            && NamesLooselyMatch(party, asOurCompany)
            && !message.Contains("customer", StringComparison.OrdinalIgnoreCase)
            && !message.Contains("buyer", StringComparison.OrdinalIgnoreCase)
            && !message.Contains("vendor", StringComparison.OrdinalIgnoreCase)
            && !message.Contains("supplier", StringComparison.OrdinalIgnoreCase)
            && !message.Contains("party", StringComparison.OrdinalIgnoreCase))
            return false;

        var ourCompany = ResolveCompanyForChat(message);
        // If the only company mention is the party itself, don't also filter CompanyName
        if (!string.IsNullOrWhiteSpace(ourCompany) && NamesLooselyMatch(party, ourCompany))
            ourCompany = null;

        var like = EscapeSqlLiteral(party.Trim());
        var where = new List<string>
        {
            $"LedgerName LIKE '%{like}%'"
        };
        if (!string.IsNullOrWhiteSpace(ourCompany))
            where.Add($"CompanyName = '{EscapeSqlLiteral(ourCompany)}'");

        sql = $"""
            SELECT TOP 50
                CompanyName,
                LedgerName,
                PendingBalance,
                Openingbalance,
                PANNo,
                Under,
                Category
            FROM LedgerMaster
            WHERE {string.Join(" AND ", where)}
            ORDER BY ABS(ISNULL(PendingBalance, 0)) DESC, ABS(ISNULL(Openingbalance, 0)) DESC
            """;

        warning = string.IsNullOrWhiteSpace(ourCompany)
            ? $"Governed ledger outstanding: LedgerMaster.LedgerName LIKE '%{party}%' (PendingBalance/Openingbalance)."
            : $"Governed ledger outstanding: LedgerName LIKE '%{party}%' AND CompanyName = '{ourCompany}'.";
        return true;
    }

    private static bool LooksLikeLooseOutstandingBalanceQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("outstanding")
               || m.Contains("pending balance")
               || m.Contains("opening balance")
               || (m.Contains("pending") && m.Contains("balance"));
    }

    private static string? TryExtractLedgerPartyName(string message)
    {
        var alias = ResolveVendorFirmAlias(message);
        if (!string.IsNullOrWhiteSpace(alias))
            return FinalizeLedgerPartyName(alias, message);

        static string? CleanParty(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim().TrimEnd('.', ',', ';', '?', '!', ':');
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"^(?:the\s+|a\s+|an\s+)",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"^(?:customer|buyer|party|vendor|supplier|ledger)\s+",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"\s+(?:ledger|outstanding|pending|opening|balance|bill|amount|please).*$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"\s+(?:ka|ke|ki|ko|kitna|kya|hai|hain|batao|dikhao).*$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"^(?:pe|par|mein)\s+",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"\s+(?:fy|financial\s+year)\s+[\d\-/–]+.*$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"\s+(?:this|current)\s+year\s*$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            s = s.Trim().TrimEnd('.', ',', ';', '?', '!');
            if (s.Length < 3) return null;
            // Reject generic leftovers
            if (s.Equals("customer", StringComparison.OrdinalIgnoreCase)
                || s.Equals("vendor", StringComparison.OrdinalIgnoreCase)
                || s.Equals("party", StringComparison.OrdinalIgnoreCase)
                || s.Equals("company", StringComparison.OrdinalIgnoreCase)
                || s.Equals("ledger", StringComparison.OrdinalIgnoreCase))
                return null;
            return s;
        }

        // Hinglish: "Polyfilms pe Commercial Bag ka kitna pending balance hai"
        var m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:pe|par|mein)\s+(.+?)\s+ka\s+(?:kitna|kya)?\s*(?:pending|opening|outstanding|balance)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var p = CleanParty(m.Groups[1].Value);
            if (p is not null && !ChatEntityResolutionService.IsGarbagePartyHint(p))
                return FinalizeLedgerPartyName(p, message);
        }

        // Hinglish company-first: "Polyfilms pe Commercial Bag ka ..."
        m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"^(?:plastene\s+)?(?:polyfilms|ppl|oswal|oswal\s+extrusion)\s+pe\s+(.+?)\s+ka\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var p = CleanParty(m.Groups[1].Value);
            if (p is not null && !ChatEntityResolutionService.IsGarbagePartyHint(p))
                return FinalizeLedgerPartyName(p, message);
        }

        // "show vouchers for commercial bag company plastene polyfilms this year"
        m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:show|list|get|give\s+me)\s+vouchers\s+(?:for|of)\s+(?:the\s+)?(.+)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var p = CleanParty(m.Groups[1].Value);
            if (p is not null) return FinalizeLedgerPartyName(p, message);
        }

        // "ledger statement for Commercial Bag Company at Plastene Polyfilms"
        m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:ledger\s+)?(?:statement|summary|account\s+statement|transaction\s+history|voucher\s+(?:history|details|wise))\s+(?:for|of)\s+(?:the\s+)?(.+)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var p = CleanParty(m.Groups[1].Value);
            if (p is not null) return FinalizeLedgerPartyName(p, message);
        }

        // "for customer Procon Pacific LLC" / "for vendor Bright Rubber"
        m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:for|of|against)\s+(?:the\s+)?(?:customer|buyer|party|vendor|supplier|ledger)\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var p = CleanParty(m.Groups[1].Value);
            if (p is not null) return FinalizeLedgerPartyName(p, message);
        }

        // "outstanding for Procon Pacific LLC" / "pending balance of X"
        m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:ledger\s+)?(?:outstanding|pending(?:\s+balance)?|opening(?:\s+balance)?|balance)\s+(?:for|of|against)\s+(?:the\s+)?(?:customer|buyer|party|vendor|supplier)?\s*(.+)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var p = CleanParty(m.Groups[1].Value);
            if (p is not null) return FinalizeLedgerPartyName(p, message);
        }

        // "Procon Pacific LLC ledger outstanding" / "find Procon Pacific pending balance"
        // Skip Hinglish company-first questions — handled above.
        m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"^(?:find|show|get|list|what(?:'s| is)?|give\s+me)?\s*(.+?)\s+(?:ledger\s+)?(?:outstanding|pending\s+balance|opening\s+balance)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var p = CleanParty(m.Groups[1].Value);
            if (p is not null
                && !p.Contains("ledger", StringComparison.OrdinalIgnoreCase)
                && !LooksLikeOnlyOurCompanyPhrase(p)
                && !ChatEntityResolutionService.IsGarbagePartyHint(p)
                && !System.Text.RegularExpressions.Regex.IsMatch(message, @"\bpe\s+.+\s+ka\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return FinalizeLedgerPartyName(p, message);
        }

        // "ledger outstanding Procon Pacific LLC" (name at end without for/)
        m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:ledger\s+)?(?:outstanding|pending\s+balance|opening\s+balance)\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var p = CleanParty(m.Groups[1].Value);
            if (p is not null && !LooksLikeOnlyOurCompanyPhrase(p))
                return FinalizeLedgerPartyName(p, message);
        }

        return null;
    }

    /// <summary>
    /// "Commercial Bag Company at Plastene Polyfilms Limited" → "Commercial Bag Company"
    /// when company is resolved separately for @companyname / CompanyName filters.
    /// </summary>
    private static string? FinalizeLedgerPartyName(string party, string message)
    {
        if (string.IsNullOrWhiteSpace(party)) return null;

        var company = ResolveCompanyForChat(message);
        var trimmed = StripTrailingCompanyFromParty(party.Trim(), company);
        trimmed = StripEmbeddedCompanySuffix(trimmed, company);
        if (ChatEntityResolutionService.IsGarbagePartyHint(trimmed))
            return null;
        return trimmed.Length < 3 ? null : trimmed;
    }

    /// <summary>
    /// "commercial bag company plastene polyfilms" → "commercial bag company" when company is known.
    /// </summary>
    private static string StripEmbeddedCompanySuffix(string party, string? knownCompany)
    {
        var s = party.Trim();
        if (string.IsNullOrWhiteSpace(s) || string.IsNullOrWhiteSpace(knownCompany))
            return s;

        var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2) return s;

        for (var take = Math.Min(6, words.Length - 1); take >= 1; take--)
        {
            var tail = string.Join(' ', words[^take..]);
            var alias = ResolveOutwardCompanyAlias(tail);
            if (!string.IsNullOrWhiteSpace(alias)
                && alias.Equals(knownCompany, StringComparison.OrdinalIgnoreCase))
                return string.Join(' ', words[..^take]).Trim();

            if (NamesLooselyMatch(tail, knownCompany))
                return string.Join(' ', words[..^take]).Trim();
        }

        return s;
    }

    private static string StripTrailingCompanyFromParty(string party, string? knownCompany)
    {
        var s = party.Trim();
        if (string.IsNullOrWhiteSpace(s)) return s;

        if (!string.IsNullOrWhiteSpace(knownCompany))
        {
            s = System.Text.RegularExpressions.Regex.Replace(
                s,
                $@"\s+(?:at|for)\s+{System.Text.RegularExpressions.Regex.Escape(knownCompany)}\s*$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Fallback when company alias did not match but message ends with " at … Limited"
        s = System.Text.RegularExpressions.Regex.Replace(
            s,
            @"\s+(?:at|for)\s+(?:the\s+)?[A-Za-z0-9][A-Za-z0-9 .,&\-()']*?(?:Limited|Ltd|Pvt|Private)(?:\s*\([^)]+\))?\s*$",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return s.Trim();
    }

    private static bool LooksLikeOnlyOurCompanyPhrase(string phrase)
    {
        var c = CanonicalizeCompanyName(phrase) ?? ResolveOutwardCompanyAlias(phrase);
        return !string.IsNullOrWhiteSpace(c) && NamesLooselyMatch(phrase, c);
    }

    private static bool NamesLooselyMatch(string a, string b)
    {
        static string Norm(string s) =>
            System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9]+", "");
        var na = Norm(a);
        var nb = Norm(b);
        if (na.Length < 3 || nb.Length < 3) return false;
        return na == nb || na.Contains(nb) || nb.Contains(na);
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

    private string ApplySqlPostProcess(string sqlRaw, string message, out string? columnRepairHint)
    {
        var sql = _sqlGuard.NormalizeAndValidate(sqlRaw);
        sql = ApplyCanonicalOurCompanyName(ApplyKnownColumnFixes(sql), message);
        sql = _schemaCatalog.FixHallucinatedColumns(sql, out var columnFixes);
        columnRepairHint = _schemaCatalog.FormatUnknownColumnsForRepair(columnFixes);
        return sql;
    }

    /// <summary>
    /// Rewrite our-company nicknames in SQL (e.g. CompanyName = 'KP Woven')
    /// to canonical FactoryInfo / PurchasePayment names (e.g. 'K.P. WOVEN PRIVATE LIMITED').
    /// Schema RAG does not resolve company aliases — this does.
    /// </summary>
    private static string ApplyCanonicalOurCompanyName(string sql, string message)
    {
        var fromMessage = ResolveOutwardCompanyAlias(message);
        return System.Text.RegularExpressions.Regex.Replace(
            sql,
            @"\b(?<col>CompanyName|companyname|CompName)\b\s*(?<op>=|LIKE)\s*'(?<lit>[^']*)'",
            m =>
            {
                var col = m.Groups["col"].Value;
                var op = m.Groups["op"].Value;
                var lit = m.Groups["lit"].Value;
                var bare = lit.Trim().Trim('%').Trim();
                if (bare.Length == 0) return m.Value;

                var fromLit = CanonicalizeCompanyName(bare);
                string? use = null;
                if (!string.IsNullOrWhiteSpace(fromLit)
                    && !fromLit.Equals(bare, StringComparison.OrdinalIgnoreCase))
                {
                    use = fromLit;
                }
                else if (!string.IsNullOrWhiteSpace(fromMessage)
                         && IsOurCompanyNickname(bare, fromMessage))
                {
                    use = fromMessage;
                }

                if (string.IsNullOrWhiteSpace(use)
                    || use.Equals(bare, StringComparison.OrdinalIgnoreCase))
                    return m.Value;

                var escaped = EscapeSqlLiteral(use);
                if (op.Equals("LIKE", StringComparison.OrdinalIgnoreCase))
                    return $"{col} LIKE '%{escaped}%'";
                return $"{col} = '{escaped}'";
            },
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool IsOurCompanyNickname(string literal, string canonical)
    {
        if (literal.Equals(canonical, StringComparison.OrdinalIgnoreCase)) return false;
        var via = CanonicalizeCompanyName(literal);
        if (!string.IsNullOrWhiteSpace(via)
            && via.Equals(canonical, StringComparison.OrdinalIgnoreCase))
            return true;

        static string Norm(string s) =>
            System.Text.RegularExpressions.Regex.Replace(
                s.ToLowerInvariant(), @"[^a-z0-9]+", "");

        var nLit = Norm(literal);
        var nCan = Norm(canonical);
        if (nLit.Length < 4) return false;
        // "kpwoven" ⊂ "kpwovenprivatelimited"; "oswal" ⊂ "oswalextrusionlimited"
        return nCan.Contains(nLit, StringComparison.Ordinal);
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

        // PurchasePayment has deliverydate — NOT PurchaseDate or PODate (those are on ApprovePO/ApprovePOHOD)
        if (sql.Contains("PurchasePayment", StringComparison.OrdinalIgnoreCase)
            && System.Text.RegularExpressions.Regex.IsMatch(
                sql, @"\bPurchaseDate\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            sql = System.Text.RegularExpressions.Regex.Replace(
                sql, @"\bPurchaseDate\b", "deliverydate", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // ApprovePO / ApprovePOHOD / ApproveWorkOrder key is PoNo — models often invent POCode / PurchaseCode on Approve*
        if (System.Text.RegularExpressions.Regex.IsMatch(
                sql,
                @"\bApprovePO\b|\bApprovePOHOD\b|\bApproveWorkOrder\b|\bApproveIndent\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            && System.Text.RegularExpressions.Regex.IsMatch(
                sql, @"\bPOCode\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            sql = System.Text.RegularExpressions.Regex.Replace(
                sql, @"\bPOCode\b", "PoNo", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return sql;
    }

    /// <summary>
    /// Pending work orders: ApproveWorkOrder has no CompanyName/TotalAmount — join PurchasePayment.
    /// Models invent CompName/TotalAmt on ApproveWorkOrder alone.
    /// </summary>
    private static bool TryBuildPendingWorkOrderSql(
        string message,
        string sql,
        out string rewritten,
        out string warning)
    {
        rewritten = sql;
        warning = "";

        var looksWo = LooksLikePendingWorkOrderQuestion(message)
                      || sql.Contains("ApproveWorkOrder", StringComparison.OrdinalIgnoreCase);
        if (!looksWo) return false;
        if (LooksLikePendingPoQuestion(message)
            && !sql.Contains("ApproveWorkOrder", StringComparison.OrdinalIgnoreCase)
            && !message.Contains("work order", StringComparison.OrdinalIgnoreCase))
            return false;

        var badCols = System.Text.RegularExpressions.Regex.IsMatch(
                          sql, @"\bCompName\b|\bTotalAmt\b|\bCompanyName\b|\bTotalAmount\b",
                          System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                      && sql.Contains("ApproveWorkOrder", StringComparison.OrdinalIgnoreCase)
                      && !sql.Contains("PurchasePayment", StringComparison.OrdinalIgnoreCase);

        if (!LooksLikePendingWorkOrderQuestion(message) && !badCols) return false;

        var company = ResolveCompanyForChat(message)
                      ?? TryExtractSqlCompanyNameLiteral(sql);

        // CompName = '...' mistaken literal on WO — treat as company
        if (string.IsNullOrWhiteSpace(company))
        {
            var m = System.Text.RegularExpressions.Regex.Match(
                sql, @"\bCompName\b\s*=\s*'([^']+)'", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success) company = CanonicalizeCompanyName(m.Groups[1].Value);
        }

        var filters = new List<string> { "a.status = 'Pending'" };
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"pp.CompanyName = '{EscapeSqlLiteral(company)}'");
        if (TryExtractAmountThreshold(message) is { } minAmt)
            filters.Add($"ISNULL(pp.TotalAmount, 0) > {minAmt.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        rewritten = $"""
            SELECT TOP 50
                a.PoNo AS WorkOrderNo,
                pp.CompanyName,
                pp.TotalAmount,
                pp.Currency,
                MAX(a.PODate) AS PODate,
                MAX(a.ApprovalName) AS ApprovalName,
                MAX(a.status) AS status
            FROM ApproveWorkOrder a
            INNER JOIN PurchasePayment pp ON a.PoNo = pp.PurchaseCode
            WHERE {string.Join(" AND ", filters)}
            GROUP BY a.PoNo, pp.CompanyName, pp.TotalAmount, pp.Currency
            ORDER BY MAX(a.PODate) DESC
            """;
        warning = string.IsNullOrWhiteSpace(company)
            ? "Rewrote pending work-order query: ApproveWorkOrder JOIN PurchasePayment (no CompName/TotalAmt on ApproveWorkOrder)."
            : $"Rewrote pending work-order query for CompanyName = '{company}' via PurchasePayment join.";
        return true;
    }

    private static bool LooksLikePendingWorkOrderQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!m.Contains("pending")) return false;
        return m.Contains("work order")
               || m.Contains("workorder")
               || m.Contains("work-order")
               || System.Text.RegularExpressions.Regex.IsMatch(m, @"\bwos?\b");
    }

    /// <summary>
    /// Pending PO questions often hallucinate PurchaseDate, scan huge Vw_PurchaseOrder,
    /// or confuse buyer CompanyName with vendor FirmName. Always rewrite to a safe shape.
    /// </summary>
    private static bool TryBuildPendingPoApprovalSql(
        string message,
        string sql,
        out string rewritten,
        out string warning)
    {
        rewritten = sql;
        warning = "";
        if (!LooksLikePendingPoQuestion(message)) return false;

        var vendorIntent = LooksLikePendingPoVendorIntent(message, sql);
        var minAmount = TryExtractAmountThreshold(message);
        var isCount = LooksLikePendingPoCountQuestion(message);

        // Pending set first (small), then join header/lines — never scan full Vw_PurchaseOrder.
        const string pendingUnion = """
            SELECT PoNo, PODate, ApprovalName, status FROM ApprovePO WHERE status = 'Pending'
            UNION ALL
            SELECT PoNo, PODate, ApprovalName, status FROM ApprovePOHOD WHERE status = 'Pending'
            """;
        const string pendingUnionDistinct = """
            SELECT PoNo FROM ApprovePO WHERE status = 'Pending'
            UNION ALL
            SELECT PoNo FROM ApprovePOHOD WHERE status = 'Pending'
            """;

        if (vendorIntent)
        {
            var vendorFrag = TryResolvePendingPoVendorFragment(message, sql);
            if (string.IsNullOrWhiteSpace(vendorFrag))
                return false;

            var amountFilter = minAmount is { } amt
                ? $"AND ISNULL(pp.TotalAmount, 0) > {amt.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                : "";

            if (isCount)
            {
                rewritten = $"""
                    SELECT COUNT(DISTINCT a.PoNo) AS PendingPOCount
                    FROM (
                        {pendingUnionDistinct}
                    ) a
                    INNER JOIN PurchasePayment pp ON a.PoNo = pp.PurchaseCode
                    INNER JOIN Vw_PurchaseOrder v ON a.PoNo = v.PurchaseCode
                    WHERE v.FirmName LIKE '%{EscapeSqlLiteral(vendorFrag)}%'
                      {amountFilter}
                    """;
                warning =
                    $"Governed pending PO COUNT for vendor FirmName LIKE '%{vendorFrag}%' (ApprovePO + ApprovePOHOD).";
            }
            else
            {
                rewritten = $"""
                    SELECT TOP 50
                        a.PoNo AS PurchaseCode,
                        MAX(v.FirmName) AS FirmName,
                        pp.CompanyName,
                        pp.TotalAmount,
                        pp.Currency,
                        MAX(a.PODate) AS PODate,
                        MAX(a.ApprovalName) AS ApprovalName,
                        MAX(a.status) AS status
                    FROM (
                        {pendingUnion}
                    ) a
                    INNER JOIN PurchasePayment pp ON a.PoNo = pp.PurchaseCode
                    INNER JOIN Vw_PurchaseOrder v ON a.PoNo = v.PurchaseCode
                    WHERE v.FirmName LIKE '%{EscapeSqlLiteral(vendorFrag)}%'
                      {amountFilter}
                    GROUP BY a.PoNo, pp.CompanyName, pp.TotalAmount, pp.Currency
                    ORDER BY MAX(a.PODate) DESC
                    """;
                warning =
                    $"Governed pending PO list for vendor FirmName LIKE '%{vendorFrag}%' (ApprovePO + ApprovePOHOD).";
            }
            return true;
        }

        var company = ResolvePendingPoCompany(message, sql);

        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(company))
            filters.Add($"pp.CompanyName = '{EscapeSqlLiteral(company)}'");
        if (minAmount is { } minAmt)
            filters.Add($"ISNULL(pp.TotalAmount, 0) > {minAmt.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        var where = filters.Count > 0 ? "WHERE " + string.Join(" AND ", filters) : "";

        if (isCount)
        {
            rewritten = $"""
                SELECT COUNT(DISTINCT a.PoNo) AS PendingPOCount
                FROM (
                    {pendingUnionDistinct}
                ) a
                INNER JOIN PurchasePayment pp ON a.PoNo = pp.PurchaseCode
                {where}
                """;
            warning = string.IsNullOrWhiteSpace(company)
                ? "Governed pending PO COUNT via ApprovePO + ApprovePOHOD + PurchasePayment."
                : $"Governed pending PO COUNT for buyer CompanyName = '{company}' (ApprovePO + ApprovePOHOD).";
        }
        else
        {
            rewritten = $"""
                SELECT TOP 50
                    pp.PurchaseCode,
                    pp.CompanyName,
                    pp.TotalAmount,
                    pp.Currency,
                    MAX(a.PODate) AS PODate,
                    MAX(a.ApprovalName) AS ApprovalName,
                    MAX(a.status) AS status
                FROM PurchasePayment pp
                INNER JOIN (
                    {pendingUnion}
                ) a ON pp.PurchaseCode = a.PoNo
                {where}
                GROUP BY pp.PurchaseCode, pp.CompanyName, pp.TotalAmount, pp.Currency
                ORDER BY MAX(a.PODate) DESC
                """;
            warning = string.IsNullOrWhiteSpace(company)
                ? "Governed pending PO list via ApprovePO + ApprovePOHOD + PurchasePayment."
                : $"Governed pending PO list for buyer CompanyName = '{company}' (ApprovePO + ApprovePOHOD).";
        }
        return true;
    }

    private static string? ResolvePendingPoCompany(string message, string sql) =>
        ResolveCompanyForChat(message)
        ?? TryExtractSqlCompanyNameLiteral(sql);

    private static bool LooksLikePendingPoCountQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("how many")
               || m.StartsWith("number of")
               || (m.Contains("count") && (m.Contains("po") || m.Contains("purchase order")));
    }

    private static bool IsGovernedPendingPoSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return false;
        var hasPendingQueue = sql.Contains("ApprovePO", StringComparison.OrdinalIgnoreCase)
                              || sql.Contains("ApprovePOHOD", StringComparison.OrdinalIgnoreCase);
        if (!hasPendingQueue) return false;

        if (sql.Contains("PurchasePayment", StringComparison.OrdinalIgnoreCase))
            return true;

        // Vendor pending PO path joins Vw_PurchaseOrder from pending union
        return sql.Contains("Vw_PurchaseOrder", StringComparison.OrdinalIgnoreCase)
               && sql.Contains("FirmName", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldForcePendingPoGovernedRewrite(string sql, int rowCount)
    {
        if (IsGovernedPendingPoSql(sql))
            return rowCount == 0;

        if (rowCount == 0)
            return true;

        // Wrong tables that often appear when LLM confuses PR / allocation with PO approval
        if (sql.Contains("Vw_PurchaseReq", StringComparison.OrdinalIgnoreCase))
            return true;
        if (sql.Contains("POAllocation", StringComparison.OrdinalIgnoreCase)
            && !sql.Contains("ApprovePO", StringComparison.OrdinalIgnoreCase))
            return true;

        return !IsGovernedPendingPoSql(sql);
    }

    private static bool LooksLikePendingPoVendorIntent(string message, string sql)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("vendor") || m.Contains("supplier") || m.Contains("firm name") || m.Contains("firmname"))
            return true;
        if (sql.Contains("FirmName", StringComparison.OrdinalIgnoreCase)
            && !sql.Contains("CompanyName", StringComparison.OrdinalIgnoreCase))
            return true;
        // "to kp woven" / "to vendor X" — vendor/supplier direction (not "at company")
        if (System.Text.RegularExpressions.Regex.IsMatch(
                m,
                @"\bto\s+(?:the\s+)?(?:vendor\s+|supplier\s+)?(?:kp\b|k\.p|oswal|polyfilms|hcp|plastene|bright|chemline|lohia|woven)"))
            return true;
        if (System.Text.RegularExpressions.Regex.IsMatch(
                m, @"\b(?:for|against)\s+(?:vendor|supplier)\b"))
            return true;
        // Explicit "at company" / "for company" stays buyer-side
        if (m.Contains("at company") || m.Contains("for company") || m.Contains("our company"))
            return false;
        return false;
    }

    private static string? TryResolvePendingPoVendorFragment(string message, string sql)
    {
        if (ResolveVendorFirmAlias(message) is { } alias)
            return alias;

        var m = message.ToLowerInvariant();
        if ((m.Contains("woven") || m.Contains("kpv"))
            && (m.Contains("kp") || m.Contains("k.p") || m.Contains("kpv") || m.Contains("woven")))
            return "K.P. Woven"; // matches "K.P. Woven Pvt Ltd - Unit 3 -Purchase"

        var firmLit = System.Text.RegularExpressions.Regex.Match(
            sql,
            @"\bFirmName\b\s*(?:=|LIKE)\s*'%?([^'%]+)%?'",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (firmLit.Success)
        {
            var lit = firmLit.Groups[1].Value.Trim();
            if (lit.Length >= 3
                && !lit.Equals("K.P. WOVEN PRIVATE LIMITED", StringComparison.OrdinalIgnoreCase))
                return lit;
            if (lit.Contains("WOVEN", StringComparison.OrdinalIgnoreCase))
                return "K.P. Woven";
        }

        var named = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:vendor|supplier|firm\s*name)\s+([A-Za-z0-9 .,&\-()]+?)(?:\s+over|\s+above|\s+pending|\s+with|\s*$)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (named.Success)
        {
            var cand = named.Groups[1].Value.Trim().TrimEnd('.', ',', ';', '?', '!');
            if (cand.Length >= 3) return cand;
        }

        return null;
    }

    private static bool LooksLikePendingPoQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("work order") || m.Contains("workorder")) return false;
        if (m.Contains("reject")) return false;
        if (m.Contains("compare") || m.Contains(" vs ") || m.Contains("versus")) return false;
        if (m.Contains("high-value") || m.Contains("high value") || m.Contains("descending")) return false;
        if (m.Contains("currency") || m.Contains("delivery date") || m.Contains("payment term")) return false;
        if (!m.Contains("pending")) return false;
        return m.Contains("purchase order")
               || m.Contains("purchase orders")
               || System.Text.RegularExpressions.Regex.IsMatch(m, @"\bpos?\b")
               || m.Contains("po approval")
               || (m.Contains("approval") && m.Contains("purchase"));
    }

    private static decimal? TryExtractAmountThreshold(string message)
    {
        var m = message.ToLowerInvariant();

        var lakh = System.Text.RegularExpressions.Regex.Match(
            m, @"(?:over|above|greater than|>\s*)\s*(?:rs\.?|₹|inr)?\s*([\d,.]+)\s*lakh");
        if (lakh.Success
            && decimal.TryParse(lakh.Groups[1].Value.Replace(",", ""),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var lakhAmt))
            return lakhAmt * 100_000m;

        if (m.Contains("lakh")
            && (m.Contains("over") || m.Contains("above") || m.Contains("greater than") || m.Contains('>')))
        {
            var lakhAnywhere = System.Text.RegularExpressions.Regex.Match(m, @"([\d,.]+)\s*lakh");
            if (lakhAnywhere.Success
                && decimal.TryParse(lakhAnywhere.Groups[1].Value.Replace(",", ""),
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var lakhAmt2))
                return lakhAmt2 * 100_000m;
        }

        var plain = System.Text.RegularExpressions.Regex.Match(
            m, @"(?:over|above|greater than|>\s*)\s*(?:rs\.?|₹)?\s*([\d,]+)");
        if (plain.Success
            && decimal.TryParse(plain.Groups[1].Value.Replace(",", ""),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var amt))
            return amt;

        return null;
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

    private static string? ResolveOutwardCompanyAlias(string message) =>
        CompanyAliasMap.Resolve(message);

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

    private static bool LooksLikeSmallBagProductionQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("small bag")
               || m.Contains("smallbag")
               || (m.Contains("cutting") && m.Contains("stitching"));
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

    private static bool LooksLikeCountryWiseSalesQuestion(string message)
    {
        if (LooksLikeTopExportCustomersQuestion(message))
            return false;

        var m = message.ToLowerInvariant();
        if (!m.Contains("country"))
            return false;

        if (m.Contains("country wise") || m.Contains("country-wise") || m.Contains("countrywise"))
            return true;
        if (m.Contains("by country") || m.Contains("sales by country") || m.Contains("country breakdown"))
            return true;
        if (m.Contains("wise") && (m.Contains("sales") || m.Contains("sale")))
            return true;

        return false;
    }

    private static bool ShouldRewriteToCountryWiseSales(string sql)
    {
        if (sql.Contains("vw_Countrywise_sales_dashboard", StringComparison.OrdinalIgnoreCase))
            return false;

        return sql.Contains("Destination", StringComparison.OrdinalIgnoreCase)
               || (sql.Contains("vw_Salesvoucher", StringComparison.OrdinalIgnoreCase)
                   && sql.Contains("Country", StringComparison.OrdinalIgnoreCase));
    }

    private static string ToInvYearShortLabel(int fyStartYear) =>
        $"{fyStartYear % 100:D2}-{(fyStartYear + 1) % 100:D2}";

    private static bool TryBuildCountryWiseSalesSql(
        string message,
        out string sql,
        out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeCountryWiseSalesQuestion(message))
            return false;

        var topN = ParseTopN(message, defaultN: 50);
        var (fyStart, _, fyLabel) = ParseIndianFinancialYear(message);
        var invYear = ToInvYearShortLabel(fyStart.Year);
        var invYearLit = EscapeSqlLiteral(invYear);

        var groupHint = ResolveFactoryGroupHint(message);
        var companyExact = ResolveCompanyForChat(message);

        var isAll = message.Contains("all companies", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("all company", StringComparison.OrdinalIgnoreCase);

        string companyFilter;
        string companyNote;
        if (isAll)
        {
            companyFilter = "";
            companyNote = "all companies";
        }
        else if (!string.IsNullOrWhiteSpace(companyExact))
        {
            var companyLit = EscapeSqlLiteral(companyExact);
            companyFilter = $"""
                AND GroupName IN (
                    SELECT DISTINCT LTRIM(RTRIM(GroupName))
                    FROM FactoryInfo
                    WHERE Name = '{companyLit}'
                      AND ISNULL(LTRIM(RTRIM(GroupName)), '') <> ''
                )
                """;
            companyNote = companyExact;
        }
        else if (!string.IsNullOrWhiteSpace(groupHint))
        {
            var hint = EscapeSqlLiteral(groupHint);
            companyFilter = $"AND GroupName LIKE '%{hint}%'";
            companyNote = $"GroupName LIKE '%{groupHint}%'";
        }
        else
        {
            return false;
        }

        sql = $"""
            SELECT TOP {topN}
                Country,
                SUM(CAST(Value AS float)) AS SalesAmount
            FROM vw_Countrywise_sales_dashboard
            WHERE InvYear = '{invYearLit}'
              {companyFilter}
            GROUP BY Country
            ORDER BY SUM(CAST(Value AS float)) DESC
            """;

        warning =
            $"Governed country-wise sales on vw_Countrywise_sales_dashboard for FY {invYear} (InvYear={invYear}, calendar {fyLabel}), company={companyNote}, SUM(Value) excl. intercompany.";
        return true;
    }

    private static bool TryBuildCountryWiseSalesFallbackSql(
        string message,
        out string sql,
        out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeCountryWiseSalesQuestion(message))
            return false;

        var groupHint = ResolveFactoryGroupHint(message);
        var companyExact = ResolveCompanyForChat(message);
        var likeHint = groupHint
                       ?? (companyExact is not null
                           ? System.Text.RegularExpressions.Regex.Replace(
                               companyExact,
                               @"\s+(Limited|Ltd\.?|Pvt\.?|Private).*$",
                               "",
                               System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim()
                           : null);
        if (string.IsNullOrWhiteSpace(likeHint))
            return false;

        var topN = ParseTopN(message, defaultN: 50);
        var (fyStart, _, fyLabel) = ParseIndianFinancialYear(message);
        var invYear = EscapeSqlLiteral(ToInvYearShortLabel(fyStart.Year));
        var hint = EscapeSqlLiteral(likeHint);

        sql = $"""
            SELECT TOP {topN}
                Country,
                SUM(CAST(Value AS float)) AS SalesAmount
            FROM vw_Countrywise_sales_dashboard
            WHERE InvYear = '{invYear}'
              AND GroupName LIKE '%{hint}%'
            GROUP BY Country
            ORDER BY SUM(CAST(Value AS float)) DESC
            """;

        warning =
            $"Governed country-wise sales fallback: GroupName LIKE '%{likeHint}%' for FY {ToInvYearShortLabel(fyStart.Year)} ({fyLabel}).";
        return true;
    }

    private static bool LooksLikeTopExportCustomersQuestion(string message)
    {
        var m = message.ToLowerInvariant();
        if (!(m.Contains("export") && (m.Contains("customer") || m.Contains("buyer") || m.Contains("client"))))
            return false;
        // Ranking / list intent
        return m.Contains("top")
               || m.Contains("highest")
               || m.Contains("largest")
               || m.Contains("biggest")
               || m.Contains("ranking")
               || m.Contains("rank");
    }

    private static bool TryBuildTopExportCustomersSql(
        string message,
        out string sql,
        out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeTopExportCustomersQuestion(message))
            return false;

        var topN = ParseTopN(message, defaultN: 5);
        var (fyStart, fyEndExclusive, fyLabel) = ParseIndianFinancialYear(message);
        var wantsGroup = message.Contains("group", StringComparison.OrdinalIgnoreCase);
        var groupHint = ResolveFactoryGroupHint(message);
        var companyExact = ResolveCompanyForChat(message);

        string companyFilter;
        string companyNote;
        if (wantsGroup && !string.IsNullOrWhiteSpace(groupHint))
        {
            var hint = EscapeSqlLiteral(groupHint);
            companyFilter = $"""
                CompanyName IN (
                    SELECT Name FROM FactoryInfo
                    WHERE GroupName LIKE '%{hint}%'
                       OR Name LIKE '%{hint}%'
                )
                """;
            companyNote = $"FactoryInfo group/name LIKE '%{groupHint}%'";
        }
        else if (!string.IsNullOrWhiteSpace(companyExact))
        {
            companyFilter = $"CompanyName = '{EscapeSqlLiteral(companyExact)}'";
            companyNote = companyExact;
        }
        else if (!string.IsNullOrWhiteSpace(groupHint))
        {
            // Nickname without explicit "group" — still allow LIKE on company name
            var hint = EscapeSqlLiteral(groupHint);
            companyFilter = $"CompanyName LIKE '%{hint}%'";
            companyNote = $"CompanyName LIKE '%{groupHint}%'";
        }
        else
        {
            return false;
        }

        var startLit = fyStart.ToString("yyyy-MM-dd");
        var endLit = fyEndExclusive.ToString("yyyy-MM-dd");
        var excludeInter = WantsExcludeInterCompany(message);
        var interCompanyFilter = excludeInter
            ? BuildExcludeInterCompanyBuyerFilter(groupHint, companyExact)
            : "";
        var interNote = excludeInter ? ", excluding inter-company buyers (InternalVendor + all FactoryInfo)" : "";

        sql = $"""
            SELECT TOP {topN}
                BuyerName AS Customer,
                SUM(ISNULL(BillAMount, 0)) AS TotalExportAmount,
                COUNT(*) AS InvoiceCount
            FROM vw_Salesvoucher
            WHERE InvType LIKE '%Export%'
              AND InvDate >= '{startLit}'
              AND InvDate < '{endLit}'
              AND BuyerName IS NOT NULL
              AND LTRIM(RTRIM(BuyerName)) <> ''
              AND {companyFilter}
              {interCompanyFilter}
            GROUP BY BuyerName
            ORDER BY SUM(ISNULL(BillAMount, 0)) DESC
            """;

        warning =
            $"Governed top-{topN} export customers on vw_Salesvoucher for FY {fyLabel} ({startLit} to <{endLit}), company={companyNote}, InvType LIKE '%Export%'{interNote}.";
        return true;
    }

    /// <summary>
    /// When FactoryInfo.GroupName filter returns no rows, filter vw_Salesvoucher.CompanyName LIKE hint instead.
    /// </summary>
    private static bool TryBuildTopExportCustomersFallbackSql(
        string message,
        out string sql,
        out string warning)
    {
        sql = "";
        warning = "";
        if (!LooksLikeTopExportCustomersQuestion(message))
            return false;

        var groupHint = ResolveFactoryGroupHint(message);
        var companyExact = ResolveCompanyForChat(message);
        var likeHint = groupHint
                       ?? (companyExact is not null
                           ? System.Text.RegularExpressions.Regex.Replace(
                               companyExact,
                               @"\s+(Limited|Ltd\.?|Pvt\.?|Private).*$",
                               "",
                               System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim()
                           : null);
        if (string.IsNullOrWhiteSpace(likeHint))
            return false;

        var topN = ParseTopN(message, defaultN: 5);
        var (fyStart, fyEndExclusive, fyLabel) = ParseIndianFinancialYear(message);
        var startLit = fyStart.ToString("yyyy-MM-dd");
        var endLit = fyEndExclusive.ToString("yyyy-MM-dd");
        var hint = EscapeSqlLiteral(likeHint);
        var excludeInter = WantsExcludeInterCompany(message);
        var interCompanyFilter = excludeInter
            ? BuildExcludeInterCompanyBuyerFilter(groupHint, companyExact)
            : "";
        var interNote = excludeInter ? ", excluding inter-company buyers (InternalVendor + all FactoryInfo)" : "";

        sql = $"""
            SELECT TOP {topN}
                BuyerName AS Customer,
                SUM(ISNULL(BillAMount, 0)) AS TotalExportAmount,
                COUNT(*) AS InvoiceCount
            FROM vw_Salesvoucher
            WHERE InvType LIKE '%Export%'
              AND InvDate >= '{startLit}'
              AND InvDate < '{endLit}'
              AND BuyerName IS NOT NULL
              AND LTRIM(RTRIM(BuyerName)) <> ''
              AND CompanyName LIKE '%{hint}%'
              {interCompanyFilter}
            GROUP BY BuyerName
            ORDER BY SUM(ISNULL(BillAMount, 0)) DESC
            """;

        warning =
            $"Governed top-{topN} export customers fallback: CompanyName LIKE '%{likeHint}%' for FY {fyLabel} (FactoryInfo group returned no rows){interNote}.";
        return true;
    }

    private static bool WantsExcludeInterCompany(string message)
    {
        var m = message.ToLowerInvariant();
        return m.Contains("exclud")
               && (m.Contains("inter company")
                   || m.Contains("inter-company")
                   || m.Contains("intercompany")
                   || m.Contains("inter unit")
                   || m.Contains("inter-unit")
                   || m.Contains("interunit")
                   || m.Contains("group compan")
                   || m.Contains("sister compan")
                   || m.Contains("related compan"));
    }

    /// <summary>
    /// Drop buyers that are our sister/group companies.
    /// Uses InternalVendor (ERP inter-company list) + FactoryInfo rows with a real GroupName —
    /// not only the same GroupName (KP Woven is its own group but still internal).
    /// </summary>
    private static string BuildExcludeInterCompanyBuyerFilter(string? groupHint, string? companyExact)
    {
        _ = groupHint;
        _ = companyExact;

        return """
            AND NOT EXISTS (
                SELECT 1
                FROM FactoryInfo f
                WHERE ISNULL(f.Name, '') <> ''
                  AND ISNULL(f.GroupName, '') <> ''
                  AND (
                      LTRIM(RTRIM(BuyerName)) = LTRIM(RTRIM(f.Name))
                      OR BuyerName LIKE '%' + LTRIM(RTRIM(f.Name)) + '%'
                      OR LTRIM(RTRIM(f.Name)) LIKE '%' + LTRIM(RTRIM(BuyerName)) + '%'
                      OR (
                          LEN(REPLACE(REPLACE(REPLACE(REPLACE(f.Name, ' PRIVATE LIMITED', ''), ' Limited', ''), ' Ltd', ''), ' Pvt', '')) >= 6
                          AND (
                              BuyerName LIKE '%'
                                  + LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(f.Name, ' PRIVATE LIMITED', ''), ' Limited', ''), ' Ltd', ''), ' Pvt', '')))
                                  + '%'
                              OR REPLACE(REPLACE(REPLACE(BuyerName, '.', ''), ' ', ''), '-', '')
                                 LIKE '%'
                                  + REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(f.Name, ' PRIVATE LIMITED', ''), ' Limited', ''), ' Ltd', ''), ' Pvt', ''), '.', ''), ' ', '')
                                  + '%'
                          )
                      )
                  )
            )
            AND NOT EXISTS (
                SELECT 1
                FROM InternalVendor iv
                WHERE ISNULL(iv.FirmName, '') <> ''
                  AND (
                      LTRIM(RTRIM(BuyerName)) = LTRIM(RTRIM(iv.FirmName))
                      OR BuyerName LIKE '%' + LTRIM(RTRIM(iv.FirmName)) + '%'
                      OR LTRIM(RTRIM(iv.FirmName)) LIKE '%' + LTRIM(RTRIM(BuyerName)) + '%'
                      OR (
                          LEN(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(iv.FirmName, ' -Sales', ''), ' -Purchase', ''), '-Sales', ''), '-Purchase', ''), ' - Sales', ''), ' - Purchase', '')) >= 8
                          AND (
                              BuyerName LIKE '%'
                                  + LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(iv.FirmName, ' -Sales', ''), ' -Purchase', ''), '-Sales', ''), '-Purchase', ''), ' - Sales', ''), ' - Purchase', '')))
                                  + '%'
                              OR LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(iv.FirmName, ' -Sales', ''), ' -Purchase', ''), '-Sales', ''), '-Purchase', ''), ' - Sales', ''), ' - Purchase', '')))
                                 LIKE '%' + LTRIM(RTRIM(BuyerName)) + '%'
                          )
                      )
                  )
            )
            """;
    }

    private static int ParseTopN(string message, int defaultN)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\btop\s+(\d{1,2})\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n))
            return Math.Clamp(n, 1, 50);
        return Math.Clamp(defaultN, 1, 50);
    }

    /// <summary>
    /// Indian FY: Apr 1 of start year → Apr 1 of next year (exclusive end).
    /// Parses "2024-25", "2024-2025", "FY 24-25", "financial year 2024-25".
    /// </summary>
    private static (DateTime Start, DateTime EndExclusive, string Label) ParseIndianFinancialYear(string message)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:fy|f\.y\.|financial\s+year)?\s*(20\d{2})\s*[-–/]\s*(\d{2}|\d{4})\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success
            && int.TryParse(m.Groups[1].Value, out var startYear))
        {
            var endRaw = m.Groups[2].Value;
            int endYear;
            if (endRaw.Length == 4 && int.TryParse(endRaw, out var ey4))
                endYear = ey4;
            else if (endRaw.Length == 2 && int.TryParse(endRaw, out var ey2))
                endYear = (startYear / 100) * 100 + ey2;
            else
                endYear = startYear + 1;

            if (endYear <= startYear)
                endYear = startYear + 1;

            var start = new DateTime(startYear, 4, 1);
            var endEx = new DateTime(endYear, 4, 1);
            var label = $"{startYear}-{(endYear % 100):D2}";
            return (start, endEx, label);
        }

        var mShort = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:fy|f\.y\.|financial\s+year|last\s+financial\s+year)?\s*(\d{2})\s*[-–/]\s*(\d{2})\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (mShort.Success
            && int.TryParse(mShort.Groups[1].Value, out var yyStart)
            && int.TryParse(mShort.Groups[2].Value, out var yyEnd))
        {
            var shortStartYear = 2000 + yyStart;
            var shortEndYear = yyEnd > yyStart ? 2000 + yyEnd : shortStartYear + 1;
            if (shortEndYear <= shortStartYear)
                shortEndYear = shortStartYear + 1;

            var shortStart = new DateTime(shortStartYear, 4, 1);
            var shortEndEx = new DateTime(shortEndYear, 4, 1);
            var shortLabel = $"{shortStartYear}-{(shortEndYear % 100):D2}";
            return (shortStart, shortEndEx, shortLabel);
        }

        // Default: current Indian FY from local date
        var today = DateTime.Today;
        var fyStartYear = today.Month >= 4 ? today.Year : today.Year - 1;
        var defStart = new DateTime(fyStartYear, 4, 1);
        var defEnd = new DateTime(fyStartYear + 1, 4, 1);
        return (defStart, defEnd, $"{fyStartYear}-{(fyStartYear + 1) % 100:D2}");
    }

    /// <summary>
    /// Hint for FactoryInfo.GroupName / Name LIKE when user says a company or group.
    /// </summary>
    private static string? ResolveFactoryGroupHint(string message)
    {
        var m = message.ToLowerInvariant();
        if (m.Contains("plastene india") || (m.Contains("plastene") && m.Contains("india")))
            return "Plastene India";
        if (m.Contains("polyfilms") || m.Contains("ppl"))
            return "Plastene Polyfilms";
        if (m.Contains("bulkpack") || m.Contains("hcp"))
            return "HCP Plastene";
        if (m.Contains("oswal"))
            return "Oswal";
        if ((m.Contains("k.p") || m.Contains("kp ")) && m.Contains("woven"))
            return "K.P. WOVEN";
        if (System.Text.RegularExpressions.Regex.IsMatch(m, @"\bkp\b") && m.Contains("woven"))
            return "K.P. WOVEN";

        // "in <name> group" / "for <name> group"
        var g = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:in|for|at|of)\s+([A-Za-z0-9 .&'-]{3,60}?)\s+group\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (g.Success)
        {
            var name = g.Groups[1].Value.Trim();
            // strip trailing "limited" noise for broader LIKE
            name = System.Text.RegularExpressions.Regex.Replace(
                name,
                @"\s+(limited|ltd\.?|pvt\.?|private)?\s*$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
            if (name.Length >= 3)
                return name;
        }

        return null;
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

        // "... for Oswal Extrusion Limited?" / "... at Plastene Polyfilms Limited"
        var forCompany = System.Text.RegularExpressions.Regex.Match(
            message,
            @"\b(?:for|at)\s+(?:the\s+)?([A-Za-z0-9][A-Za-z0-9 .,&\-()']*?(?:Limited|Ltd|Pvt|Private)(?:\s*\([^)]+\))?)\s*[?.!]?\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (forCompany.Success)
            return forCompany.Groups[1].Value.Trim().TrimEnd('?', '.', '!');

        return null;
    }

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''");

    private static List<RetrievedTableDto> BuildTablesUsed(
        IReadOnlyList<RetrievedSchemaChunk> chunks,
        string sql,
        string? warning)
    {
        var list = chunks.Select(c => new RetrievedTableDto
        {
            ObjectName = c.ObjectName,
            Domain = c.Domain ?? "",
            Score = Math.Round(c.Score, 4)
        }).ToList();

        if (warning != null
            && warning.Contains("export sales invoice", StringComparison.OrdinalIgnoreCase))
        {
            if (!list.Any(t => t.ObjectName.Equals("vw_Salesvoucher", StringComparison.OrdinalIgnoreCase)))
            {
                list.Insert(0, new RetrievedTableDto
                {
                    ObjectName = "vw_Salesvoucher",
                    Domain = "Sales",
                    Score = 1
                });
            }
        }

        if (warning != null
            && warning.Contains("stock-in-hand", StringComparison.OrdinalIgnoreCase))
        {
            if (!list.Any(t => t.ObjectName.Equals("vw_itemwiseStock", StringComparison.OrdinalIgnoreCase)))
            {
                list.Insert(0, new RetrievedTableDto
                {
                    ObjectName = "vw_itemwiseStock",
                    Domain = "Warehouse / stock",
                    Score = 1
                });
            }
        }

        if (warning != null
            && warning.Contains("pending PO", StringComparison.OrdinalIgnoreCase))
        {
            void EnsurePendingPo(string name, string domain)
            {
                if (list.Any(t => t.ObjectName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    return;
                list.Insert(0, new RetrievedTableDto { ObjectName = name, Domain = domain, Score = 1 });
            }

            EnsurePendingPo("ApprovePO", "PO");
            EnsurePendingPo("ApprovePOHOD", "PO");
            if (sql.Contains("PurchasePayment", StringComparison.OrdinalIgnoreCase))
                EnsurePendingPo("PurchasePayment", "PO");
        }

        if (warning != null
            && warning.Contains("export customers", StringComparison.OrdinalIgnoreCase))
        {
            void Ensure(string name, string domain)
            {
                if (list.Any(t => t.ObjectName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    return;
                list.Insert(0, new RetrievedTableDto { ObjectName = name, Domain = domain, Score = 1 });
            }

            Ensure("vw_Salesvoucher", "Sales");
            if (sql.Contains("FactoryInfo", StringComparison.OrdinalIgnoreCase))
                Ensure("FactoryInfo", "Ledger / company master");
        }

        if (warning != null
            && warning.Contains("ledger outstanding", StringComparison.OrdinalIgnoreCase))
        {
            if (!list.Any(t => t.ObjectName.Equals("LedgerMaster", StringComparison.OrdinalIgnoreCase)))
            {
                list.Insert(0, new RetrievedTableDto
                {
                    ObjectName = "LedgerMaster",
                    Domain = "Ledger",
                    Score = 1
                });
            }
        }

        return list;
    }

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

    private async Task<(int? TotalCount, bool Truncated)> ResolveListCardinalityAsync(
        string sql,
        List<Dictionary<string, object?>> rows,
        bool hitCap,
        CancellationToken ct)
    {
        if (LooksLikeCountOnlyQuery(sql))
        {
            var n = TryReadFirstInt(rows);
            return (n ?? rows.Count, false);
        }

        if (!TryBuildCountWrapperSql(sql, out var countSql))
            return (null, hitCap);

        try
        {
            var countRows = await ExecuteReadOnlyAsync(countSql, ct, maxRows: 1);
            var total = TryReadFirstInt(countRows);
            if (!total.HasValue)
                return (null, hitCap);
            return (total, total.Value > rows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Companion COUNT query failed");
            return (null, hitCap);
        }
    }

    private static bool LooksLikeCountOnlyQuery(string sql)
    {
        var trimmed = sql.TrimStart();
        // SELECT [TOP n] COUNT(...) ... without other projected detail columns is a count answer
        return System.Text.RegularExpressions.Regex.IsMatch(
            trimmed,
            @"^(WITH\b[\s\S]+?\bSELECT|SELECT)\s+(?:TOP\s+\d+\s+)?COUNT\s*\(",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool TryBuildCountWrapperSql(string sql, out string countSql)
    {
        countSql = "";
        if (string.IsNullOrWhiteSpace(sql) || LooksLikeCountOnlyQuery(sql))
            return false;

        // EXEC / SP descriptions cannot be wrapped in SELECT COUNT(*) FROM (...)
        if (System.Text.RegularExpressions.Regex.IsMatch(sql, @"\bEXEC\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return false;

        var cleaned = StripLeadingTop(sql);
        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"\s+ORDER\s+BY\s+[\s\S]+$",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim().TrimEnd(';');

        if (string.IsNullOrWhiteSpace(cleaned))
            return false;

        countSql = $"SELECT COUNT(*) AS TotalCount FROM (\n{cleaned}\n) AS _cap_count";
        return true;
    }

    private static string StripLeadingTop(string sql)
    {
        var trimmed = sql.Trim();
        var m = System.Text.RegularExpressions.Regex.Match(
            trimmed,
            @"\bSELECT\s+TOP\s+\d+\s+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!m.Success)
            return trimmed;
        return string.Concat(trimmed.AsSpan(0, m.Index), "SELECT ", trimmed.AsSpan(m.Index + m.Length));
    }

    private static int? TryReadFirstInt(List<Dictionary<string, object?>> rows)
    {
        if (rows.Count == 0)
            return null;
        var first = rows[0].Values.FirstOrDefault();
        if (first == null)
            return null;
        try
        {
            return Convert.ToInt32(first);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] BuildCsvBytes(List<Dictionary<string, object?>> rows)
    {
        var sb = new StringBuilder();
        sb.Append('\uFEFF'); // Excel-friendly UTF-8 BOM
        if (rows.Count == 0)
            return Encoding.UTF8.GetBytes(sb.ToString());

        var columns = rows[0].Keys.ToList();
        static string Esc(object? v)
        {
            var s = v == null
                ? ""
                : Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "";
            if (s.IndexOfAny(new[] { '"', ',', '\n', '\r' }) >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        sb.AppendLine(string.Join(",", columns.Select(c => Esc(c))));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", columns.Select(c => Esc(row.TryGetValue(c, out var val) ? val : null))));

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private async Task<List<Dictionary<string, object?>>> ExecuteReadOnlyAsync(
        string sql,
        CancellationToken ct,
        int? maxRows = null)
    {
        var stopAfter = (maxRows ?? MaxReturnRows) + 1;
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
            if (list.Count >= stopAfter)
                break;
        }

        return list;
    }
}
