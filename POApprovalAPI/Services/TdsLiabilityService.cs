using Dapper;

namespace POApprovalAPI.Services;

/// <summary>
/// 26Q liability for one group and date range.
/// Period credit on each section ledger, minus reversal debits.
/// A debit whose other side is a group unit, or TDS Payable, is left out.
/// </summary>
public sealed class TdsLiabilityService
{
    public static readonly string[] Sections = ["94A", "94C", "94H", "94I", "94J", "94Q"];

    private static readonly Dictionary<string, string> LedgerBySection = new(StringComparer.OrdinalIgnoreCase)
    {
        ["94A"] = "TDS -194A (Interest)",
        ["94C"] = "TDS -194C (Contractor)",
        ["94H"] = "TDS -194H (Commission)",
        ["94I"] = "TDS -194I (Rent)",
        ["94J"] = "TDS -194J (Professional)",
        ["94Q"] = "TDS -194Q (Goods)",
    };

    private readonly DatabaseService _database;

    public TdsLiabilityService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<TdsGroupOption>> GetGroupsAsync()
    {
        const string sql = """
            SELECT f.GroupName AS Name, COUNT(DISTINCT f.SrNo) AS Units
            FROM FactoryInfo f WITH (NOLOCK)
            WHERE ISNULL(f.GroupName, '') <> '' AND LEN(f.GroupName) > 5
            GROUP BY f.GroupName
            ORDER BY f.GroupName
            """;

        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<TdsGroupOption>(sql);
        return rows.ToList();
    }

    public async Task<TdsLiabilityDto> GetLiabilityAsync(string group, DateTime from, DateTime to)
    {
        const string sql = """
            SET NOCOUNT ON;
            IF OBJECT_ID('tempdb..#tds_marked') IS NOT NULL DROP TABLE #tds_marked;

            SELECT
                l.CompanyName,
                l.Section,
                l.Credit,
                l.Debit,
                l.VoucherType,
                l.VoucherNo,
                l.Dt,
                j.CrPartyName AS Contra,
                CASE
                    WHEN l.Debit = 0 THEN 'credit'
                    WHEN j.CrPartyName LIKE 'TDS Payable%' THEN 'transfer'
                    WHEN j.CrPartyName IS NOT NULL
                         AND dbo.fn_Get_Ledger_InterUnit(l.CompanyName, j.CrPartyName, l.Dt) = 1 THEN 'group-unit'
                    ELSE 'reversal'
                END AS Treatment
            INTO #tds_marked
            FROM (
                SELECT
                    v.CompanyName,
                    CASE v.LedgerName
                        WHEN 'TDS -194A (Interest)' THEN '94A'
                        WHEN 'TDS -194C (Contractor)' THEN '94C'
                        WHEN 'TDS -194H (Commission)' THEN '94H'
                        WHEN 'TDS -194I (Rent)' THEN '94I'
                        WHEN 'TDS -194J (Professional)' THEN '94J'
                        WHEN 'TDS -194Q (Goods)' THEN '94Q'
                    END AS Section,
                    CAST(CASE WHEN v.amount < 0 THEN -v.amount ELSE 0 END AS decimal(18, 2)) AS Credit,
                    CAST(CASE WHEN v.amount > 0 THEN v.amount ELSE 0 END AS decimal(18, 2)) AS Debit,
                    v.VoucherType,
                    v.VoucherNo,
                    CONVERT(date, v.Date) AS Dt,
                    v.LedgerName
                FROM vw_LedgerSummary v WITH (NOLOCK)
                INNER JOIN FactoryInfo f WITH (NOLOCK) ON f.Name = v.CompanyName
                WHERE f.GroupName = @Group
                  AND v.Date >= @From
                  AND v.Date < @ToExclusive
                  AND v.LedgerName IN (
                      'TDS -194A (Interest)', 'TDS -194C (Contractor)', 'TDS -194H (Commission)',
                      'TDS -194I (Rent)', 'TDS -194J (Professional)', 'TDS -194Q (Goods)')
            ) l
            OUTER APPLY (
                SELECT TOP 1 j.CrPartyName
                FROM Journal j WITH (NOLOCK)
                WHERE l.Debit > 0
                  AND j.CompanyName = l.CompanyName
                  AND CONVERT(varchar(40), j.JournalNo) = l.VoucherNo
                  AND CONVERT(date, j.sysdate) = l.Dt
                  AND j.PartyName = l.LedgerName
                  AND ISNULL(j.Debit, 0) > 0
            ) j;

            SELECT LTRIM(RTRIM(CompanyName)) AS Unit, Section,
                   SUM(Credit) AS Credit,
                   SUM(CASE WHEN Treatment = 'reversal' THEN Debit ELSE 0 END) AS Reversal
            FROM #tds_marked
            GROUP BY LTRIM(RTRIM(CompanyName)), Section
            HAVING SUM(Credit) <> 0
                OR SUM(CASE WHEN Treatment = 'reversal' THEN Debit ELSE 0 END) <> 0;

            SELECT LTRIM(RTRIM(CompanyName)) AS Unit, Section, Dt AS [Date], VoucherType, VoucherNo,
                   ISNULL(Contra, '') AS Particulars, Debit AS Amount
            FROM #tds_marked
            WHERE Treatment = 'reversal'
            ORDER BY Dt, Section, CompanyName, VoucherNo;

            SELECT
                ISNULL(SUM(CASE WHEN Treatment = 'group-unit' THEN 1 ELSE 0 END), 0) AS GroupUnitLines,
                ISNULL(SUM(CASE WHEN Treatment = 'group-unit' THEN Debit ELSE 0 END), 0) AS GroupUnitAmount,
                ISNULL(SUM(CASE WHEN Treatment = 'transfer' THEN 1 ELSE 0 END), 0) AS TransferLines,
                ISNULL(SUM(CASE WHEN Treatment = 'transfer' THEN Debit ELSE 0 END), 0) AS TransferAmount
            FROM #tds_marked;
            """;

        using var connection = _database.CreateConnection();
        using var grid = await connection.QueryMultipleAsync(sql, new
        {
            Group = group,
            From = from.Date,
            ToExclusive = to.Date.AddDays(1),
        }, commandTimeout: 180);

        var cells = (await grid.ReadAsync<SectionCell>()).ToList();
        var reversals = (await grid.ReadAsync<TdsReversalRow>()).ToList();
        var omitted = await grid.ReadFirstOrDefaultAsync<TdsOmittedDto>() ?? new TdsOmittedDto();

        var byUnit = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in cells)
        {
            if (string.IsNullOrEmpty(cell.Section) || !LedgerBySection.ContainsKey(cell.Section))
                continue;
            if (!byUnit.TryGetValue(cell.Unit, out var sections))
            {
                sections = Sections.ToDictionary(s => s, _ => 0m);
                byUnit[cell.Unit] = sections;
            }
            sections[cell.Section] = cell.Credit - cell.Reversal;
        }

        var units = byUnit
            .OrderBy(u => u.Key, StringComparer.OrdinalIgnoreCase)
            .Select(u => new TdsLiabilityUnitRow
            {
                Unit = u.Key,
                Sections = u.Value,
                Total = u.Value.Values.Sum(),
            })
            .ToList();

        var groupSections = Sections.ToDictionary(
            s => s,
            s => units.Sum(u => u.Sections.GetValueOrDefault(s)));

        return new TdsLiabilityDto
        {
            Group = group,
            From = from.ToString("yyyy-MM-dd"),
            To = to.ToString("yyyy-MM-dd"),
            Units = units,
            GroupTotal = new TdsLiabilityUnitRow
            {
                Unit = group,
                Sections = groupSections,
                Total = groupSections.Values.Sum(),
            },
            Reversals = reversals,
            Omitted = omitted,
        };
    }

    private sealed class SectionCell
    {
        public string Unit { get; set; } = "";
        public string Section { get; set; } = "";
        public decimal Credit { get; set; }
        public decimal Reversal { get; set; }
    }
}

public sealed class TdsGroupOption
{
    public string Name { get; set; } = "";
    public int Units { get; set; }
}

public sealed class TdsLiabilityDto
{
    public string Group { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public List<TdsLiabilityUnitRow> Units { get; set; } = [];
    public TdsLiabilityUnitRow GroupTotal { get; set; } = new();
    public List<TdsReversalRow> Reversals { get; set; } = [];
    public TdsOmittedDto Omitted { get; set; } = new();
}

public sealed class TdsLiabilityUnitRow
{
    public string Unit { get; set; } = "";
    public Dictionary<string, decimal> Sections { get; set; } = new();
    public decimal Total { get; set; }
}

public sealed class TdsReversalRow
{
    public string Unit { get; set; } = "";
    public string Section { get; set; } = "";
    public DateTime Date { get; set; }
    public string VoucherType { get; set; } = "";
    public string VoucherNo { get; set; } = "";
    public string Particulars { get; set; } = "";
    public decimal Amount { get; set; }
}

public sealed class TdsOmittedDto
{
    public int GroupUnitLines { get; set; }
    public decimal GroupUnitAmount { get; set; }
    public int TransferLines { get; set; }
    public decimal TransferAmount { get; set; }
}
