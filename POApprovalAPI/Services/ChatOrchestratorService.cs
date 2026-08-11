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
            - FactoryInfo PAN column is PermanentAccountNo (NOT PANNo). LedgerMaster PAN column is PANNo.
            - FactoryInfo GST prefer NewGSTNo. LedgerMaster has GSTNo/NewGSTNo for parties.
            - Ledger/account groups: use SELECT DISTINCT Under FROM LedgerMaster (filter empty Under). NEVER query LedgerGroupMaster.
            - Opening/pending ledger balances: use LedgerMaster.Openingbalance and LedgerMaster.PendingBalance. NEVER query LedgerOpeningBalance (table is empty).
            - MRN / material receipt / store inward: prefer Vw_StoreInwards or vw_MRNList; header table StoreInwardsPayment; lines StoreInwards. MRN number column is MRNo/MRno; payment link via vw_MRNToBillPayment or BillPaymentEntry.MRNno. Always TOP 50.
            - MRN company vs vendor: 'for company X' uses CompanyName. Vendor/supplier/party names use PartyName/Partyname. Names ending in -Purchase (e.g. Plastene Polyfilms Ltd-Purchase) are PartyName, not CompanyName.
            - NEVER invent MRDate. Vw_StoreInwards/StoreInwardsPayment: use BillDate, GateInwardDate, or SysDate. vw_MRNList/BillPaymentEntry: use MRNDate (not MRDate).
            - Payment against an MRN: prefer BillPaymentEntry WHERE MRNno = '<MRN>'. If using vw_MRNToBillPayment, require PaymentNo IS NOT NULL and DISTINCT PaymentNo/PaymentAmount. Never treat NULL PaymentNo lines as 'no payment'.
            - Receipts by supplier bill number: prefer Vw_StoreInwards WHERE BillNo = '<bill>'. Do not find store receipts via BillPaymentEntry.BillNo.
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
        try
        {
            rows = await ExecuteReadOnlyAsync(sql, ct);
        }
        catch (Exception ex)
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
                _logger.LogError(repairEx, "SQL repair still failed");
                throw new InvalidOperationException(
                    $"SQL failed after repair: {repairEx.Message}. Last SQL: {sql}", repairEx);
            }
        }

        string? warning = null;
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
