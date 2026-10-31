using Dapper;

namespace POApprovalAPI.Services;

/// <summary>
/// One section sheet for a group and quarter. Credit lines from the section ledger,
/// rate recomputed from TDS and principal, then the liability reversal is taken off
/// until the sheet total equals the section liability. A reduced row keeps its rate
/// and the principal is resized from the new TDS.
/// </summary>
public sealed class TdsSectionSheetService
{
    private const string Section94A = "94A";
    private const string Ledger94A = "TDS -194A (Interest)";
    private const string Section94C = "94C";
    private const string Ledger94C = "TDS -194C (Contractor)";

    private readonly DatabaseService _database;

    public TdsSectionSheetService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<TdsSectionSheetDto> Get94AAsync(string group, DateTime from, DateTime to)
    {
        const string sql = """
            SET NOCOUNT ON;

            SELECT
                LTRIM(RTRIM(v.CompanyName)) AS Unit,
                CONVERT(date, v.Date) AS [Date],
                v.VoucherType,
                v.VoucherNo,
                CAST(-v.amount AS decimal(18, 2)) AS Tds,
                LTRIM(RTRIM(ISNULL(j.DrPartyName, ''))) AS Party,
                CAST(ISNULL(prin.Debit, 0) AS decimal(18, 2)) AS Principal,
                LTRIM(RTRIM(ISNULL(pan.PANNo, ''))) AS Pan
            FROM vw_LedgerSummary v WITH (NOLOCK)
            INNER JOIN FactoryInfo f WITH (NOLOCK) ON f.Name = v.CompanyName
            OUTER APPLY (
                SELECT TOP 1 j.DrPartyName
                FROM Journal j WITH (NOLOCK)
                WHERE j.CompanyName = v.CompanyName
                  AND CONVERT(varchar(40), j.JournalNo) = v.VoucherNo
                  AND CONVERT(date, j.sysdate) = CONVERT(date, v.Date)
                  AND j.PartyName = @Ledger
                  AND ISNULL(j.Credit, 0) > 0
            ) j
            OUTER APPLY (
                SELECT TOP 1 j2.Debit
                FROM Journal j2 WITH (NOLOCK)
                WHERE j2.CompanyName = v.CompanyName
                  AND CONVERT(varchar(40), j2.JournalNo) = v.VoucherNo
                  AND CONVERT(date, j2.sysdate) = CONVERT(date, v.Date)
                  AND j2.PartyName = j.DrPartyName
                  AND ISNULL(j2.Debit, 0) > 0
            ) prin
            OUTER APPLY (
                SELECT TOP 1 lm.PANNo
                FROM LedgerMaster lm WITH (NOLOCK)
                WHERE lm.LedgerName = j.DrPartyName
                  AND NULLIF(LTRIM(RTRIM(lm.PANNo)), '') IS NOT NULL
                ORDER BY CASE WHEN lm.CompanyName = v.CompanyName THEN 0 ELSE 1 END, lm.srno
            ) pan
            WHERE f.GroupName = @Group
              AND v.Date >= @From
              AND v.Date < @ToExclusive
              AND v.LedgerName = @Ledger
              AND v.amount < 0
            ORDER BY v.Date, v.VoucherNo;

            SELECT
                CONVERT(date, v.Date) AS [Date],
                v.VoucherNo,
                CAST(v.amount AS decimal(18, 2)) AS Amount,
                LTRIM(RTRIM(ISNULL(j.DrPartyName, ''))) AS DrParty,
                LTRIM(RTRIM(ISNULL(j.CrPartyName, ''))) AS CrParty
            FROM vw_LedgerSummary v WITH (NOLOCK)
            INNER JOIN FactoryInfo f WITH (NOLOCK) ON f.Name = v.CompanyName
            OUTER APPLY (
                SELECT TOP 1 j.DrPartyName, j.CrPartyName
                FROM Journal j WITH (NOLOCK)
                WHERE v.amount > 0
                  AND j.CompanyName = v.CompanyName
                  AND CONVERT(varchar(40), j.JournalNo) = v.VoucherNo
                  AND CONVERT(date, j.sysdate) = CONVERT(date, v.Date)
                  AND j.PartyName = @Ledger
                  AND ISNULL(j.Debit, 0) > 0
            ) j
            WHERE f.GroupName = @Group
              AND v.Date >= @From
              AND v.Date < @ToExclusive
              AND v.LedgerName = @Ledger
              AND v.amount > 0
              AND ISNULL(j.CrPartyName, '') NOT LIKE 'TDS Payable%'
              AND NOT (ISNULL(j.CrPartyName, '') LIKE '%K.P. Woven%' AND ISNULL(j.CrPartyName, '') LIKE '%Sales%')
              AND NOT (
                    j.CrPartyName IS NOT NULL
                    AND dbo.fn_Get_Ledger_InterUnit(v.CompanyName, j.CrPartyName, CONVERT(date, v.Date)) = 1
              )
            ORDER BY v.Date, v.VoucherNo;
            """;

        using var connection = _database.CreateConnection();
        using var grid = await connection.QueryMultipleAsync(sql, new
        {
            Group = group,
            Ledger = Ledger94A,
            From = from.Date,
            ToExclusive = to.Date.AddDays(1),
        }, commandTimeout: 180);

        var raw = (await grid.ReadAsync<SectionLineRow>()).ToList();
        var reversals = (await grid.ReadAsync<ReversalRow>()).ToList();
        return Finish(Section94A, group, from, to, raw, reversals.Sum(r => r.Amount), usePanWhenPrincipalMissing: false, reversals: reversals);
    }

    public async Task<TdsSectionSheetDto> Get94CAsync(string group, DateTime from, DateTime to)
    {
        const string sql = """
            SET NOCOUNT ON;

            SELECT
                LTRIM(RTRIM(v.CompanyName)) AS Unit,
                CONVERT(date, v.Date) AS [Date],
                v.VoucherType,
                v.VoucherNo,
                CAST(-v.amount AS decimal(18, 2)) AS Tds,
                calc.Party,
                calc.Principal,
                LTRIM(RTRIM(ISNULL(pan.PANNo, ''))) AS Pan
            FROM vw_LedgerSummary v WITH (NOLOCK)
            INNER JOIN FactoryInfo f WITH (NOLOCK) ON f.Name = v.CompanyName
            OUTER APPLY (
                SELECT TOP 1 LTRIM(RTRIM(ISNULL(j.DrPartyName, ''))) AS DrParty, LTRIM(RTRIM(ISNULL(j.CrPartyName, ''))) AS CrParty
                FROM Journal j WITH (NOLOCK)
                WHERE v.VoucherType = 'Journal'
                  AND j.CompanyName = v.CompanyName
                  AND CONVERT(varchar(40), j.JournalNo) = v.VoucherNo
                  AND CONVERT(date, j.sysdate) = CONVERT(date, v.Date)
                  AND j.PartyName = @Ledger
                  AND ISNULL(j.Credit, 0) > 0
            ) j
            OUTER APPLY (
                SELECT TOP 1 LTRIM(RTRIM(pv.SupplierName)) AS Supplier, CAST(pv.AccessableValue AS decimal(18, 2)) AS Assessable
                FROM PurchaseVoucher pv WITH (NOLOCK)
                INNER JOIN PurchaseVoucherTax pvt WITH (NOLOCK)
                    ON pvt.StoreInwardNo = pv.StoreInwardNo
                   AND pvt.yr = pv.yr
                   AND pvt.CompanyName = pv.CompanyName
                WHERE v.VoucherType <> 'Journal'
                  AND pv.CompanyName = v.CompanyName
                  AND pv.GeneralPurchaseNo = v.VoucherNo
                  AND pv.VoucherType = v.VoucherType
                  AND CONVERT(date, pv.SysDate) = CONVERT(date, v.Date)
                  AND pvt.TaxledgerName = @Ledger
                  AND ABS(pvt.Amount - v.amount) < 0.01
            ) pur
            CROSS APPLY (
                SELECT
                    CASE
                        WHEN v.VoucherType = 'Journal' AND (ISNULL(j.CrParty, '') = '' OR j.CrParty LIKE 'TDS%') THEN ISNULL(j.DrParty, '')
                        WHEN v.VoucherType = 'Journal' THEN ISNULL(j.CrParty, '')
                        ELSE ISNULL(pur.Supplier, '')
                    END AS Party,
                    CASE
                        WHEN v.VoucherType = 'Journal' THEN CAST(0 AS decimal(18, 2))
                        ELSE CAST(ISNULL(pur.Assessable, 0) AS decimal(18, 2))
                    END AS Principal
            ) named
            OUTER APPLY (
                SELECT CAST(ISNULL(SUM(j2.Debit), 0) AS decimal(18, 2)) AS BaseAmt
                FROM Journal j2 WITH (NOLOCK)
                WHERE v.VoucherType = 'Journal'
                  AND j2.CompanyName = v.CompanyName
                  AND CONVERT(varchar(40), j2.JournalNo) = v.VoucherNo
                  AND CONVERT(date, j2.sysdate) = CONVERT(date, v.Date)
                  AND ISNULL(j2.Debit, 0) > 0
                  AND j2.PartyName NOT LIKE 'TDS%'
                  AND j2.PartyName NOT LIKE '%GST Input%'
                  AND j2.PartyName NOT LIKE 'Round Off%'
                  AND LTRIM(RTRIM(j2.PartyName)) <> named.Party
            ) expn
            CROSS APPLY (
                SELECT
                    named.Party AS Party,
                    CASE
                        WHEN v.VoucherType = 'Journal' THEN expn.BaseAmt
                        ELSE named.Principal
                    END AS Principal
            ) calc
            OUTER APPLY (
                SELECT TOP 1 lm.PANNo
                FROM LedgerMaster lm WITH (NOLOCK)
                WHERE lm.LedgerName = calc.Party
                  AND NULLIF(LTRIM(RTRIM(lm.PANNo)), '') IS NOT NULL
                ORDER BY CASE WHEN lm.CompanyName = v.CompanyName THEN 0 ELSE 1 END, lm.srno
            ) pan
            WHERE f.GroupName = @Group
              AND v.Date >= @From
              AND v.Date < @ToExclusive
              AND v.LedgerName = @Ledger
              AND v.amount < 0
            ORDER BY v.Date, v.VoucherNo;

            SELECT
                CONVERT(date, v.Date) AS [Date],
                v.VoucherNo,
                CAST(v.amount AS decimal(18, 2)) AS Amount,
                LTRIM(RTRIM(ISNULL(j.DrPartyName, ''))) AS DrParty,
                LTRIM(RTRIM(ISNULL(j.CrPartyName, ''))) AS CrParty
            FROM vw_LedgerSummary v WITH (NOLOCK)
            INNER JOIN FactoryInfo f WITH (NOLOCK) ON f.Name = v.CompanyName
            OUTER APPLY (
                SELECT TOP 1 j.DrPartyName, j.CrPartyName
                FROM Journal j WITH (NOLOCK)
                WHERE v.amount > 0
                  AND j.CompanyName = v.CompanyName
                  AND CONVERT(varchar(40), j.JournalNo) = v.VoucherNo
                  AND CONVERT(date, j.sysdate) = CONVERT(date, v.Date)
                  AND j.PartyName = @Ledger
                  AND ISNULL(j.Debit, 0) > 0
            ) j
            WHERE f.GroupName = @Group
              AND v.Date >= @From
              AND v.Date < @ToExclusive
              AND v.LedgerName = @Ledger
              AND v.amount > 0
              AND ISNULL(j.CrPartyName, '') NOT LIKE 'TDS Payable%'
              AND NOT (ISNULL(j.CrPartyName, '') LIKE '%K.P. Woven%' AND ISNULL(j.CrPartyName, '') LIKE '%Sales%')
              AND NOT (
                    j.CrPartyName IS NOT NULL
                    AND dbo.fn_Get_Ledger_InterUnit(v.CompanyName, j.CrPartyName, CONVERT(date, v.Date)) = 1
              )
            ORDER BY v.Date, v.VoucherNo;
            """;

        using var connection = _database.CreateConnection();
        using var grid = await connection.QueryMultipleAsync(sql, new
        {
            Group = group,
            Ledger = Ledger94C,
            From = from.Date,
            ToExclusive = to.Date.AddDays(1),
        }, commandTimeout: 180);

        var raw = (await grid.ReadAsync<SectionLineRow>()).ToList();
        var reversals = (await grid.ReadAsync<ReversalRow>()).ToList();
        return Finish(Section94C, group, from, to, raw, reversals.Sum(r => r.Amount), usePanWhenPrincipalMissing: true, reversals: reversals);
    }

    public Task<TdsSectionSheetDto> Get94HAsync(string group, DateTime from, DateTime to) =>
        GetExpenseSectionAsync("94H", "TDS -194H (Commission)", [2m], group, from, to);

    public Task<TdsSectionSheetDto> Get94IAsync(string group, DateTime from, DateTime to) =>
        GetExpenseSectionAsync("94I", "TDS -194I (Rent)", [2m, 10m], group, from, to);

    public Task<TdsSectionSheetDto> Get94JAsync(string group, DateTime from, DateTime to) =>
        GetExpenseSectionAsync("94J", "TDS -194J (Professional)", [2m, 10m], group, from, to);

    /// <summary>
    /// Ledger credits are the sheet, same as the other sections, so the total stays on the liability.
    /// Domestic goods bills past ₹50 lakh with no 194Q line are listed beside it and are not added in.
    /// </summary>
    public async Task<TdsSectionSheetDto> Get94QAsync(string group, DateTime from, DateTime to)
    {
        var sheet = await Load94QAsync(group, from, to);
        sheet.Unbooked = await GetUnbooked94QAsync(group, from, to);
        var journalTds = sheet.Lines
            .Where(line => line.VoucherType == "Journal")
            .GroupBy(line => line.Party.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(line => line.Tds), StringComparer.OrdinalIgnoreCase);
        foreach (var party in sheet.Unbooked.GroupBy(line => line.Party.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            journalTds.TryGetValue(party.Key, out var already);
            if (already + 1m >= party.Sum(line => line.Tds))
            {
                foreach (var line in party) line.Covered = true;
            }
        }
        sheet.UnbookedTds = sheet.Unbooked.Where(line => !line.Covered).Sum(line => line.Tds);
        return sheet;
    }

    /// <summary>
    /// Booked 94Q comes from the purchase-tax rows and the journal credits, not the full ledger view.
    /// </summary>
    private async Task<TdsSectionSheetDto> Load94QAsync(string group, DateTime from, DateTime to)
    {
        const string ledger = "TDS -194Q (Goods)";
        const string sql = """
            SET NOCOUNT ON;

            SELECT
                LTRIM(RTRIM(pv.CompanyName)) AS Unit,
                CONVERT(date, pv.SysDate) AS [Date],
                pv.VoucherType,
                pv.GeneralPurchaseNo AS VoucherNo,
                CAST(ABS(pvt.Amount) AS decimal(18, 2)) AS Tds,
                LTRIM(RTRIM(pv.SupplierName)) AS Party,
                CAST(ISNULL(pv.AccessableValue, 0) AS decimal(18, 2)) AS Principal,
                CAST('' AS varchar(20)) AS Pan
            FROM PurchaseVoucherTax pvt WITH (NOLOCK)
            INNER JOIN PurchaseVoucher pv WITH (NOLOCK)
                ON pvt.StoreInwardNo = pv.StoreInwardNo AND pvt.yr = pv.yr AND pvt.CompanyName = pv.CompanyName
            INNER JOIN FactoryInfo f WITH (NOLOCK) ON f.Name = pv.CompanyName
            WHERE f.GroupName = @Group
              AND pv.SysDate >= @From
              AND pv.SysDate < @ToExclusive
              AND pvt.TaxledgerName = @Ledger
              AND ABS(pvt.Amount) > 0;

            SELECT
                LTRIM(RTRIM(j.CompanyName)) AS Unit,
                CONVERT(date, j.sysdate) AS [Date],
                'Journal' AS VoucherType,
                CONVERT(varchar(40), j.JournalNo) AS VoucherNo,
                CAST(j.Credit AS decimal(18, 2)) AS Tds,
                calc.Party,
                CAST(0 AS decimal(18, 2)) AS Principal,
                CAST('' AS varchar(20)) AS Pan
            FROM Journal j WITH (NOLOCK)
            INNER JOIN FactoryInfo f WITH (NOLOCK) ON f.Name = j.CompanyName
            CROSS APPLY (
                SELECT CASE
                    WHEN ISNULL(LTRIM(RTRIM(j.CrPartyName)), '') = '' OR j.CrPartyName LIKE 'TDS%'
                        THEN LTRIM(RTRIM(ISNULL(j.DrPartyName, '')))
                    ELSE LTRIM(RTRIM(j.CrPartyName))
                END AS Party
            ) calc
            WHERE f.GroupName = @Group
              AND j.sysdate >= @From
              AND j.sysdate < @ToExclusive
              AND j.PartyName = @Ledger
              AND ISNULL(j.Credit, 0) > 0;

            SELECT
                LTRIM(RTRIM(j.CompanyName)) AS Unit,
                CONVERT(date, j.sysdate) AS [Date],
                CONVERT(varchar(40), j.JournalNo) AS VoucherNo,
                LTRIM(RTRIM(j2.PartyName)) AS Expense,
                CAST(j2.Debit AS decimal(18, 2)) AS Amount
            FROM Journal j WITH (NOLOCK)
            INNER JOIN FactoryInfo f WITH (NOLOCK) ON f.Name = j.CompanyName
            INNER JOIN Journal j2 WITH (NOLOCK)
                ON j2.CompanyName = j.CompanyName
               AND j2.JournalNo = j.JournalNo
            WHERE f.GroupName = @Group
              AND j.sysdate >= @From
              AND j.sysdate < @ToExclusive
              AND j.PartyName = @Ledger
              AND ISNULL(j.Credit, 0) > 0
              AND ISNULL(j2.Debit, 0) > 0
              AND j2.PartyName NOT LIKE 'TDS%'
              AND j2.PartyName NOT LIKE '%GST Input%'
              AND j2.PartyName NOT LIKE 'Round Off%';

            SELECT
                j.CompanyName AS RawCompany,
                CONVERT(date, j.sysdate) AS [Date],
                CAST(j.Debit AS decimal(18, 2)) AS Debit,
                LTRIM(RTRIM(ISNULL(j.CrPartyName, ''))) AS Contra
            FROM Journal j WITH (NOLOCK)
            INNER JOIN FactoryInfo f WITH (NOLOCK) ON f.Name = j.CompanyName
            WHERE f.GroupName = @Group
              AND j.sysdate >= @From
              AND j.sysdate < @ToExclusive
              AND j.PartyName = @Ledger
              AND ISNULL(j.Debit, 0) > 0;
            """;

        using var connection = _database.CreateConnection();
        List<SectionLineRow> purchases;
        List<SectionLineRow> journals;
        List<ExpenseDebitRow> debits;
        List<LedgerDebitRow> debitRows;
        using (var grid = await connection.QueryMultipleAsync(sql, new
        {
            Group = group,
            Ledger = ledger,
            From = from.Date,
            ToExclusive = to.Date.AddDays(1),
        }, commandTimeout: 60))
        {
            purchases = (await grid.ReadAsync<SectionLineRow>()).ToList();
            journals = (await grid.ReadAsync<SectionLineRow>()).ToList();
            debits = (await grid.ReadAsync<ExpenseDebitRow>()).ToList();
            debitRows = (await grid.ReadAsync<LedgerDebitRow>()).ToList();
        }

        var byVoucher = debits.GroupBy(d => VoucherKey(d.Unit, d.Date, d.VoucherNo))
            .ToDictionary(g => g.Key, g => g.ToList());
        foreach (var row in journals)
        {
            byVoucher.TryGetValue(VoucherKey(row.Unit, row.Date, row.VoucherNo), out var lines);
            var amounts = (lines ?? [])
                .Where(d => !string.Equals(d.Expense, row.Party, StringComparison.OrdinalIgnoreCase))
                .Select(d => d.Amount)
                .ToList();
            row.Principal = ChoosePrincipal(row.Tds, amounts, [0.1m]);
        }

        decimal reversal = 0;
        foreach (var debit in debitRows)
        {
            if (debit.Contra.StartsWith("TDS Payable", StringComparison.OrdinalIgnoreCase)) continue;
            if (debit.Contra.Contains("K.P. Woven", StringComparison.OrdinalIgnoreCase)
                && debit.Contra.Contains("Sales", StringComparison.OrdinalIgnoreCase)) continue;
            if (debit.Contra.Length > 0)
            {
                var inter = await connection.ExecuteScalarAsync<int?>(
                    "SELECT dbo.fn_Get_Ledger_InterUnit(@Company, @Ledger, @Date)",
                    new { Company = debit.RawCompany, Ledger = debit.Contra, Date = debit.Date });
                if (inter == 1) continue;
            }
            reversal += debit.Debit;
        }

        var raw = purchases.Concat(journals).OrderBy(r => r.Date).ThenBy(r => r.VoucherNo).ToList();
        await FillPanAsync(connection, raw);
        return Finish("94Q", group, from, to, raw, reversal, usePanWhenPrincipalMissing: false, snapRates: [0.1m], snapWithinRupee: true);
    }

    /// <summary>
    /// Principal is the one expense debit that matches TDS at a section rate, within a rupee.
    /// If none does, the expense debits are added together. A voucher with no bill keeps a blank rate.
    /// A calculated rate within 0.05 of a section rate is shown as that rate. The bill principal stays.
    /// </summary>
    private async Task<TdsSectionSheetDto> GetExpenseSectionAsync(
        string section,
        string ledger,
        decimal[] rates,
        string group,
        DateTime from,
        DateTime to,
        bool snapWithinRupee = false)
    {
        const string sql = """
            SET NOCOUNT ON;

            SELECT
                LTRIM(RTRIM(v.CompanyName)) AS Unit,
                CONVERT(date, v.Date) AS [Date],
                v.VoucherType,
                v.VoucherNo,
                CAST(-v.amount AS decimal(18, 2)) AS Tds,
                calc.Party,
                CASE WHEN v.VoucherType = 'Journal' THEN CAST(0 AS decimal(18, 2)) ELSE ISNULL(pur.Assessable, 0) END AS Principal,
                LTRIM(RTRIM(ISNULL(pan.PANNo, ''))) AS Pan
            FROM vw_LedgerSummary v WITH (NOLOCK)
            INNER JOIN FactoryInfo f WITH (NOLOCK) ON f.Name = v.CompanyName
            OUTER APPLY (
                SELECT TOP 1 LTRIM(RTRIM(ISNULL(j.DrPartyName, ''))) AS DrParty, LTRIM(RTRIM(ISNULL(j.CrPartyName, ''))) AS CrParty
                FROM Journal j WITH (NOLOCK)
                WHERE v.VoucherType = 'Journal'
                  AND j.CompanyName = v.CompanyName
                  AND CONVERT(varchar(40), j.JournalNo) = v.VoucherNo
                  AND CONVERT(date, j.sysdate) = CONVERT(date, v.Date)
                  AND j.PartyName = @Ledger
                  AND ISNULL(j.Credit, 0) > 0
            ) j
            OUTER APPLY (
                SELECT TOP 1 CAST(pv.AccessableValue AS decimal(18, 2)) AS Assessable, LTRIM(RTRIM(pv.SupplierName)) AS Supplier
                FROM PurchaseVoucher pv WITH (NOLOCK)
                INNER JOIN PurchaseVoucherTax pvt WITH (NOLOCK)
                    ON pvt.StoreInwardNo = pv.StoreInwardNo AND pvt.yr = pv.yr AND pvt.CompanyName = pv.CompanyName
                WHERE v.VoucherType <> 'Journal'
                  AND pv.CompanyName = v.CompanyName
                  AND pv.GeneralPurchaseNo = v.VoucherNo
                  AND pv.VoucherType = v.VoucherType
                  AND CONVERT(date, pv.SysDate) = CONVERT(date, v.Date)
                  AND pvt.TaxledgerName = @Ledger
                  AND ABS(pvt.Amount - v.amount) < 0.01
            ) pur
            CROSS APPLY (
                SELECT CASE
                    WHEN v.VoucherType = 'Journal' AND (ISNULL(j.CrParty, '') = '' OR j.CrParty LIKE 'TDS%') THEN ISNULL(j.DrParty, '')
                    WHEN v.VoucherType = 'Journal' THEN ISNULL(j.CrParty, '')
                    ELSE ISNULL(pur.Supplier, '')
                END AS Party
            ) calc
            OUTER APPLY (
                SELECT TOP 1 lm.PANNo
                FROM LedgerMaster lm WITH (NOLOCK)
                WHERE lm.LedgerName = calc.Party
                  AND NULLIF(LTRIM(RTRIM(lm.PANNo)), '') IS NOT NULL
                ORDER BY CASE WHEN lm.CompanyName = v.CompanyName THEN 0 ELSE 1 END, lm.srno
            ) pan
            WHERE f.GroupName = @Group
              AND v.Date >= @From
              AND v.Date < @ToExclusive
              AND v.LedgerName = @Ledger
              AND v.amount < 0
            ORDER BY v.Date, v.VoucherNo;

            SELECT
                LTRIM(RTRIM(v.CompanyName)) AS Unit,
                CONVERT(date, v.Date) AS [Date],
                v.VoucherNo,
                LTRIM(RTRIM(j2.PartyName)) AS Expense,
                CAST(j2.Debit AS decimal(18, 2)) AS Amount
            FROM vw_LedgerSummary v WITH (NOLOCK)
            INNER JOIN FactoryInfo f WITH (NOLOCK) ON f.Name = v.CompanyName
            INNER JOIN Journal j2 WITH (NOLOCK)
                ON j2.CompanyName = v.CompanyName
               AND CONVERT(varchar(40), j2.JournalNo) = v.VoucherNo
               AND CONVERT(date, j2.sysdate) = CONVERT(date, v.Date)
            WHERE f.GroupName = @Group
              AND v.Date >= @From
              AND v.Date < @ToExclusive
              AND v.LedgerName = @Ledger
              AND v.amount < 0
              AND v.VoucherType = 'Journal'
              AND ISNULL(j2.Debit, 0) > 0
              AND j2.PartyName NOT LIKE 'TDS%'
              AND j2.PartyName NOT LIKE '%GST Input%'
              AND j2.PartyName NOT LIKE 'Round Off%';

            SELECT CAST(ISNULL(SUM(CASE WHEN mark.Treatment = 'reversal' THEN mark.Debit ELSE 0 END), 0) AS decimal(18, 2)) AS Reversal
            FROM (
                SELECT
                    CAST(CASE WHEN v.amount > 0 THEN v.amount ELSE 0 END AS decimal(18, 2)) AS Debit,
                    CASE
                        WHEN v.amount <= 0 THEN 'credit'
                        WHEN j.CrPartyName LIKE 'TDS Payable%' THEN 'transfer'
                        WHEN j.CrPartyName LIKE '%K.P. Woven%' AND j.CrPartyName LIKE '%Sales%' THEN 'group-unit'
                        WHEN j.CrPartyName IS NOT NULL
                             AND dbo.fn_Get_Ledger_InterUnit(v.CompanyName, j.CrPartyName, CONVERT(date, v.Date)) = 1 THEN 'group-unit'
                        ELSE 'reversal'
                    END AS Treatment
                FROM vw_LedgerSummary v WITH (NOLOCK)
                INNER JOIN FactoryInfo f WITH (NOLOCK) ON f.Name = v.CompanyName
                OUTER APPLY (
                    SELECT TOP 1 j.CrPartyName
                    FROM Journal j WITH (NOLOCK)
                    WHERE v.amount > 0
                      AND j.CompanyName = v.CompanyName
                      AND CONVERT(varchar(40), j.JournalNo) = v.VoucherNo
                      AND CONVERT(date, j.sysdate) = CONVERT(date, v.Date)
                      AND j.PartyName = @Ledger
                      AND ISNULL(j.Debit, 0) > 0
                ) j
                WHERE f.GroupName = @Group
                  AND v.Date >= @From
                  AND v.Date < @ToExclusive
                  AND v.LedgerName = @Ledger
            ) mark;
            """;

        using var connection = _database.CreateConnection();
        using var grid = await connection.QueryMultipleAsync(sql, new
        {
            Group = group,
            Ledger = ledger,
            From = from.Date,
            ToExclusive = to.Date.AddDays(1),
        }, commandTimeout: 180);

        var raw = (await grid.ReadAsync<SectionLineRow>()).ToList();
        var debits = (await grid.ReadAsync<ExpenseDebitRow>()).ToList();
        var reversal = await grid.ReadFirstAsync<decimal>();
        var byVoucher = debits.GroupBy(d => VoucherKey(d.Unit, d.Date, d.VoucherNo))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var row in raw.Where(r => r.VoucherType == "Journal"))
        {
            byVoucher.TryGetValue(VoucherKey(row.Unit, row.Date, row.VoucherNo), out var lines);
            var amounts = (lines ?? [])
                .Where(d => !string.Equals(d.Expense, row.Party, StringComparison.OrdinalIgnoreCase))
                .Select(d => d.Amount)
                .ToList();
            row.Principal = ChoosePrincipal(row.Tds, amounts, rates);
        }

        return Finish(section, group, from, to, raw, reversal, usePanWhenPrincipalMissing: false, snapRates: rates, snapWithinRupee: snapWithinRupee);
    }

    /// <summary>
    /// Purchase bills in the quarter for a resident seller whose bills in that quarter are past ₹50 lakh,
    /// where the voucher was saved with no 194Q line. Group units and imports stay out.
    /// The 0.1% figure is only a note. It is not part of the sheet total.
    /// </summary>
    private async Task<List<TdsUnbookedLine>> GetUnbooked94QAsync(string group, DateTime from, DateTime to)
    {
        const string sql = """
            SET NOCOUNT ON;

            SELECT f.Name
            INTO #units
            FROM FactoryInfo f WITH (NOLOCK)
            WHERE f.GroupName = @Group;

            SELECT LTRIM(RTRIM(pv.SupplierName)) AS Party
            INTO #past
            FROM PurchaseVoucher pv WITH (NOLOCK)
            INNER JOIN #units u ON u.Name = pv.CompanyName
            WHERE pv.SysDate >= @From
              AND pv.SysDate < @ToExclusive
              AND pv.VoucherType = 'R.M.P.'
              AND ISNULL(pv.AccessableValue, 0) > 0
              AND LTRIM(RTRIM(pv.SupplierName)) NOT LIKE '%- Purchase'
              AND LTRIM(RTRIM(pv.SupplierName)) NOT LIKE '%-Purchase'
              AND NOT (LTRIM(RTRIM(pv.SupplierName)) LIKE '%K.P. Woven%' AND LTRIM(RTRIM(pv.SupplierName)) LIKE '%Sales%')
            GROUP BY LTRIM(RTRIM(pv.SupplierName))
            HAVING SUM(CAST(pv.AccessableValue AS decimal(18, 2))) >= 5000000;

            SELECT
                LTRIM(RTRIM(pv.CompanyName)) AS Unit,
                CONVERT(date, pv.SysDate) AS [Date],
                pv.VoucherType,
                pv.GeneralPurchaseNo AS VoucherNo,
                LTRIM(RTRIM(pv.SupplierName)) AS Party,
                CAST('' AS varchar(20)) AS Pan,
                CAST(pv.AccessableValue AS decimal(18, 2)) AS Principal
            FROM PurchaseVoucher pv WITH (NOLOCK)
            INNER JOIN #units u ON u.Name = pv.CompanyName
            INNER JOIN #past p ON p.Party = LTRIM(RTRIM(pv.SupplierName))
            WHERE pv.SysDate >= @From
              AND pv.SysDate < @ToExclusive
              AND pv.VoucherType = 'R.M.P.'
              AND ISNULL(pv.AccessableValue, 0) > 0
              AND NOT EXISTS (
                    SELECT 1
                    FROM PurchaseVoucherTax pvt WITH (NOLOCK)
                    WHERE pvt.StoreInwardNo = pv.StoreInwardNo
                      AND pvt.yr = pv.yr
                      AND pvt.CompanyName = pv.CompanyName
                      AND pvt.TaxledgerName = 'TDS -194Q (Goods)'
              )
            ORDER BY pv.SysDate, pv.GeneralPurchaseNo;
            """;

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<TdsUnbookedLine>(sql, new
        {
            Group = group,
            From = from.Date,
            ToExclusive = to.Date.AddDays(1),
        }, commandTimeout: 30);

        var list = rows.ToList();
        var pans = await LoadPansAsync(connection, list.Select(line => line.Party));
        foreach (var line in list)
        {
            line.Rate = 0.1m;
            line.Tds = Math.Round(line.Principal * 0.001m, 2, MidpointRounding.AwayFromZero);
            if (pans.TryGetValue(line.Party ?? "", out var pan)) line.Pan = pan;
        }
        return list;
    }

    private static async Task FillPanAsync(System.Data.IDbConnection connection, List<SectionLineRow> rows)
    {
        var pans = await LoadPansAsync(connection, rows.Select(row => row.Party));
        foreach (var row in rows)
        {
            if (pans.TryGetValue(row.Party ?? "", out var pan)) row.Pan = pan;
        }
    }

    private static async Task<Dictionary<string, string>> LoadPansAsync(System.Data.IDbConnection connection, IEnumerable<string> parties)
    {
        var names = parties.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (names.Length == 0) return map;
        var found = await connection.QueryAsync<PanRow>(
            """
            SELECT LTRIM(RTRIM(LedgerName)) AS Name, LTRIM(RTRIM(PANNo)) AS Pan
            FROM LedgerMaster WITH (NOLOCK)
            WHERE LedgerName IN @Names
              AND NULLIF(LTRIM(RTRIM(PANNo)), '') IS NOT NULL
            """,
            new { Names = names });
        foreach (var row in found)
        {
            if (!map.ContainsKey(row.Name)) map[row.Name] = row.Pan ?? "";
        }
        return map;
    }

    private sealed class PanRow
    {
        public string Name { get; set; } = "";
        public string Pan { get; set; } = "";
    }

    private static string VoucherKey(string unit, DateTime date, string voucherNo) =>
        $"{unit.Trim()}|{date:yyyy-MM-dd}|{voucherNo}";

    private static decimal ChoosePrincipal(decimal tds, List<decimal> amounts, decimal[] rates)
    {
        decimal bestAmount = 0;
        var bestDiff = decimal.MaxValue;
        var found = false;
        foreach (var amount in amounts)
        {
            foreach (var rate in rates)
            {
                var diff = Math.Abs(Math.Round(amount * rate / 100m, 2, MidpointRounding.AwayFromZero) - tds);
                if (diff <= 1m && diff < bestDiff)
                {
                    bestDiff = diff;
                    bestAmount = amount;
                    found = true;
                }
            }
        }
        if (found) return bestAmount;
        return amounts.Sum();
    }

    private static TdsSectionSheetDto Finish(
        string section,
        string group,
        DateTime from,
        DateTime to,
        List<SectionLineRow> raw,
        decimal reversal,
        bool usePanWhenPrincipalMissing,
        decimal[]? snapRates = null,
        bool snapWithinRupee = false,
        List<ReversalRow>? reversals = null)
    {
        var roundOff = raw.Where(r => r.Party.Contains("round off", StringComparison.OrdinalIgnoreCase)).ToList();
        var lines = raw.Except(roundOff).Select(row => ToLine(row, usePanWhenPrincipalMissing)).ToList();
        if (snapRates is { Length: > 0 })
        {
            foreach (var line in lines)
            {
                if (line.Principal <= 0) continue;
                if (snapWithinRupee && snapRates.Length == 1)
                {
                    var expected = Math.Round(line.Principal * snapRates[0] / 100m, 2, MidpointRounding.AwayFromZero);
                    if (Math.Abs(expected - line.Tds) <= 1m) line.Rate = snapRates[0];
                    continue;
                }
                decimal? nearest = null;
                var nearestGap = 0.05m;
                foreach (var rate in snapRates)
                {
                    var gap = Math.Abs(line.Rate - rate);
                    if (gap < nearestGap)
                    {
                        nearestGap = gap;
                        nearest = rate;
                    }
                }
                if (nearest != null) line.Rate = nearest.Value;
            }
        }
        var duplicatesRemoved = CollapseBlankPanDuplicates(lines);
        if (reversals != null)
            ApplyPartyReversals(lines, reversals);
        else
        {
            var debit = reversal;
            ApplyDebit(lines, ref debit);
        }
        return new TdsSectionSheetDto
        {
            Section = section,
            Group = group,
            From = from.ToString("yyyy-MM-dd"),
            To = to.ToString("yyyy-MM-dd"),
            ReversalDebit = reversal,
            Liability = Math.Max(0, raw.Sum(r => r.Tds) - reversal),
            RoundOffRemoved = roundOff.Count,
            DuplicatesRemoved = duplicatesRemoved,
            Lines = lines,
            TotalTds = lines.Sum(l => l.Tds),
            TotalPrincipal = lines.Sum(l => l.Principal),
        };
    }

    private static TdsSectionLine ToLine(SectionLineRow row) => ToLine(row, usePanWhenPrincipalMissing: false);

    private static TdsSectionLine ToLine(SectionLineRow row, bool usePanWhenPrincipalMissing)
    {
        var principal = row.Principal;
        var rate = principal == 0 ? 0 : Math.Round(row.Tds * 100m / principal, 2, MidpointRounding.AwayFromZero);
        if (principal == 0 && usePanWhenPrincipalMissing)
        {
            var pan = row.Pan ?? "";
            var digit = pan.Length >= 4 ? char.ToUpperInvariant(pan[3]) : '\0';
            var statutory = digit is 'P' or 'H' ? 1m : digit is 'C' or 'F' ? 2m : 0m;
            if (statutory != 0)
            {
                rate = statutory;
                principal = Math.Round(row.Tds * 100m / statutory, 2, MidpointRounding.AwayFromZero);
            }
        }

        return new TdsSectionLine
        {
            Unit = row.Unit,
            Date = row.Date,
            VoucherType = row.VoucherType ?? "",
            VoucherNo = row.VoucherNo ?? "",
            Party = row.Party,
            Pan = row.Pan ?? "",
            Principal = principal,
            Rate = rate,
            Tds = row.Tds,
            OriginalTds = row.Tds,
        };
    }

    /// <summary>
    /// Rows still without a PAN that share a TDS amount are the same pasted line twice. Keep the earliest.
    /// </summary>
    private static int CollapseBlankPanDuplicates(List<TdsSectionLine> lines)
    {
        var blank = lines.Where(l => string.IsNullOrWhiteSpace(l.Pan)).ToList();
        var remove = new List<TdsSectionLine>();
        foreach (var group in blank.GroupBy(l => l.Tds))
        {
            if (group.Count() < 2) continue;
            remove.AddRange(group.OrderBy(l => l.Date).ThenBy(l => l.VoucherNo).Skip(1));
        }
        foreach (var line in remove) lines.Remove(line);
        return remove.Count;
    }

    /// <summary>
    /// Each reversal is taken off that party's own credit lines, in date order.
    /// The party is whichever side of the reversal journal already appears on the sheet.
    /// </summary>
    private static void ApplyPartyReversals(List<TdsSectionLine> lines, List<ReversalRow> reversals)
    {
        foreach (var reversal in reversals.OrderBy(r => r.Date).ThenBy(r => r.VoucherNo))
        {
            var party = PartyOnSheet(lines, reversal.DrParty, reversal.CrParty);
            if (party == null) continue;
            var debit = reversal.Amount;
            ApplyDebit(lines, ref debit, party);
        }
    }

    private static string? PartyOnSheet(List<TdsSectionLine> lines, string drParty, string crParty)
    {
        var dr = (drParty ?? "").Trim();
        var cr = (crParty ?? "").Trim();
        var drHit = dr.Length > 0 && lines.Any(line => string.Equals(line.Party.Trim(), dr, StringComparison.OrdinalIgnoreCase));
        var crHit = cr.Length > 0 && lines.Any(line => string.Equals(line.Party.Trim(), cr, StringComparison.OrdinalIgnoreCase));
        if (drHit && !crHit) return dr;
        if (crHit && !drHit) return cr;
        if (drHit && crHit) return cr;
        return null;
    }

    /// <summary>
    /// Take one reversal off the credit lines of one party.
    /// Exact row match is deleted. Otherwise the smallest line still larger than the debit is reduced.
    /// If none is larger, the largest line is removed and the remainder is applied again.
    /// A line that survives keeps its rate; principal is resized from the new TDS.
    /// </summary>
    private static void ApplyDebit(List<TdsSectionLine> lines, ref decimal debit, string? party = null)
    {
        bool InParty(TdsSectionLine line) =>
            party == null || string.Equals(line.Party.Trim(), party.Trim(), StringComparison.OrdinalIgnoreCase);

        if (debit <= 0.005m || !lines.Any(InParty)) return;

        var remaining = debit;
        var exact = lines.FirstOrDefault(l => InParty(l) && Math.Abs(l.Tds - remaining) < 0.005m);
        if (exact != null)
        {
            lines.Remove(exact);
            debit = 0;
            return;
        }

        var nextHighest = lines.Where(l => InParty(l) && l.Tds > remaining).OrderBy(l => l.Tds).FirstOrDefault();
        if (nextHighest != null)
        {
            nextHighest.Tds = Math.Round(nextHighest.Tds - remaining, 2, MidpointRounding.AwayFromZero);
            if (nextHighest.Rate != 0)
                nextHighest.Principal = Math.Round(nextHighest.Tds * 100m / nextHighest.Rate, 2, MidpointRounding.AwayFromZero);
            debit = 0;
            return;
        }

        var highest = lines.Where(InParty).OrderByDescending(l => l.Tds).First();
        var consumed = highest.Tds;
        lines.Remove(highest);
        debit = Math.Round(debit - consumed, 2, MidpointRounding.AwayFromZero);
        ApplyDebit(lines, ref debit, party);
    }

    private sealed class ReversalRow
    {
        public DateTime Date { get; set; }
        public string VoucherNo { get; set; } = "";
        public decimal Amount { get; set; }
        public string DrParty { get; set; } = "";
        public string CrParty { get; set; } = "";
    }

    private sealed class LedgerDebitRow
    {
        public string RawCompany { get; set; } = "";
        public DateTime Date { get; set; }
        public decimal Debit { get; set; }
        public string Contra { get; set; } = "";
    }

    private sealed class ExpenseDebitRow
    {
        public string Unit { get; set; } = "";
        public DateTime Date { get; set; }
        public string VoucherNo { get; set; } = "";
        public string Expense { get; set; } = "";
        public decimal Amount { get; set; }
    }

    private sealed class SectionLineRow
    {
        public string Unit { get; set; } = "";
        public DateTime Date { get; set; }
        public string VoucherType { get; set; } = "";
        public string VoucherNo { get; set; } = "";
        public decimal Tds { get; set; }
        public string Party { get; set; } = "";
        public decimal Principal { get; set; }
        public string Pan { get; set; } = "";
    }
}

public sealed class TdsSectionSheetDto
{
    public string Section { get; set; } = "";
    public string Group { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public decimal ReversalDebit { get; set; }
    public decimal Liability { get; set; }
    public int RoundOffRemoved { get; set; }
    public int DuplicatesRemoved { get; set; }
    public List<TdsSectionLine> Lines { get; set; } = [];
    public decimal TotalTds { get; set; }
    public decimal TotalPrincipal { get; set; }
    public List<TdsUnbookedLine> Unbooked { get; set; } = [];
    public decimal UnbookedTds { get; set; }
}

/// <summary>A purchase bill that should have carried 194Q. Kept off the sheet total.</summary>
public sealed class TdsUnbookedLine
{
    public string Unit { get; set; } = "";
    public DateTime Date { get; set; }
    public string VoucherType { get; set; } = "";
    public string VoucherNo { get; set; } = "";
    public string Party { get; set; } = "";
    public string Pan { get; set; } = "";
    public decimal Principal { get; set; }
    public decimal Rate { get; set; }
    public decimal Tds { get; set; }
    public bool Covered { get; set; }
}

public sealed class TdsSectionLine
{
    public string Unit { get; set; } = "";
    public DateTime Date { get; set; }
    public string VoucherType { get; set; } = "";
    public string VoucherNo { get; set; } = "";
    public string Party { get; set; } = "";
    public string Pan { get; set; } = "";
    public decimal Principal { get; set; }
    public decimal Rate { get; set; }
    public decimal Tds { get; set; }
    public decimal OriginalTds { get; set; }
}
