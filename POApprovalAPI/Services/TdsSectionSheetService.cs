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

            SELECT CAST(ISNULL(SUM(CASE WHEN mark.Treatment = 'reversal' THEN mark.Debit ELSE 0 END), 0) AS decimal(18, 2)) AS Reversal
            FROM (
                SELECT
                    CAST(CASE WHEN v.amount > 0 THEN v.amount ELSE 0 END AS decimal(18, 2)) AS Debit,
                    CASE
                        WHEN v.amount <= 0 THEN 'credit'
                        WHEN j.CrPartyName LIKE 'TDS Payable%' THEN 'transfer'
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
            Ledger = Ledger94A,
            From = from.Date,
            ToExclusive = to.Date.AddDays(1),
        }, commandTimeout: 180);

        var raw = (await grid.ReadAsync<SectionLineRow>()).ToList();
        var reversal = await grid.ReadFirstAsync<decimal>();

        var roundOff = raw.Where(r => r.Party.Contains("round off", StringComparison.OrdinalIgnoreCase)).ToList();
        var lines = raw.Except(roundOff).Select(ToLine).ToList();

        var duplicatesRemoved = CollapseBlankPanDuplicates(lines);

        var debit = reversal;
        ApplyDebit(lines, ref debit);

        var total = lines.Sum(l => l.Tds);
        return new TdsSectionSheetDto
        {
            Section = Section94A,
            Group = group,
            From = from.ToString("yyyy-MM-dd"),
            To = to.ToString("yyyy-MM-dd"),
            ReversalDebit = reversal,
            Liability = raw.Sum(r => r.Tds) - reversal,
            RoundOffRemoved = roundOff.Count,
            DuplicatesRemoved = duplicatesRemoved,
            Lines = lines,
            TotalTds = total,
            TotalPrincipal = lines.Sum(l => l.Principal),
        };
    }

    private static TdsSectionLine ToLine(SectionLineRow row)
    {
        var rate = row.Principal == 0 ? 0 : Math.Round(row.Tds * 100m / row.Principal, 2, MidpointRounding.AwayFromZero);
        return new TdsSectionLine
        {
            Unit = row.Unit,
            Date = row.Date,
            VoucherType = row.VoucherType ?? "",
            VoucherNo = row.VoucherNo ?? "",
            Party = row.Party,
            Pan = row.Pan ?? "",
            Principal = row.Principal,
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
    /// Take the reversal debit off the credit lines.
    /// Exact row match is deleted. Otherwise the smallest line still larger than the debit is reduced.
    /// If none is larger, the largest line is removed and the remainder is applied again.
    /// A line that survives keeps its rate; principal is resized from the new TDS.
    /// </summary>
    private static void ApplyDebit(List<TdsSectionLine> lines, ref decimal debit)
    {
        if (debit <= 0.005m || lines.Count == 0) return;

        var remaining = debit;
        var exact = lines.FirstOrDefault(l => Math.Abs(l.Tds - remaining) < 0.005m);
        if (exact != null)
        {
            lines.Remove(exact);
            debit = 0;
            return;
        }

        var nextHighest = lines.Where(l => l.Tds > remaining).OrderBy(l => l.Tds).FirstOrDefault();
        if (nextHighest != null)
        {
            nextHighest.Tds = Math.Round(nextHighest.Tds - remaining, 2, MidpointRounding.AwayFromZero);
            if (nextHighest.Rate != 0)
                nextHighest.Principal = Math.Round(nextHighest.Tds * 100m / nextHighest.Rate, 2, MidpointRounding.AwayFromZero);
            debit = 0;
            return;
        }

        var highest = lines.OrderByDescending(l => l.Tds).First();
        var consumed = highest.Tds;
        lines.Remove(highest);
        debit = Math.Round(debit - consumed, 2, MidpointRounding.AwayFromZero);
        ApplyDebit(lines, ref debit);
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
