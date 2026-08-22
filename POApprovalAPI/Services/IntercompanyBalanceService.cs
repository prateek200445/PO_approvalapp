using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Services;

/// <summary>
/// Intercompany balances from the data query inside
/// <c>sp_Automail_InterCompanyBalance_Limited</c> (not the email send).
/// </summary>
public class IntercompanyBalanceService
{
    private const int CommandTimeoutSeconds = 120;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(2);

    /// <summary>Same company list and sort as the automail SP.</summary>
    private static readonly string[] ReportCompanies =
    [
        "HCP Plastene Bulkpack Ltd",
        "HCP ENTERPRISE LIMITED",
        "K.P. WOVEN PRIVATE LIMITED",
        "Plastene India Limited",
        "Plastene Polyfilms Limited",
        "Oswal Extrusion Limited",
        "OSWAL COMMODITIES PRIVATE LIMITED",
    ];

    /// <summary>
    /// Verified retrieval from sp_Automail_InterCompanyBalance_Limited — do not change.
    /// CompanyType=1 (group), CompanyId unused.
    /// </summary>
    private const string BalanceSql = @"
SELECT
    @CompanyName AS CurrentCompany,
    F1.GROUPNAME AS InterCompany,
    ROUND(SUM(V.Amount), 2) AS ClosingINR
FROM Ac_InterCompanyLedger AC
INNER JOIN LedgerMaster L
    ON AC.LedgerId = L.SrNo
INNER JOIN vw_LedgerSummary V
    ON V.LedgerName = L.LedgerName
   AND V.CompanyName = L.CompanyName
INNER JOIN FactoryInfo F
    ON F.SrNo = AC.CompanyId
LEFT JOIN FactoryInfo F1
    ON F1.SrNo = AC.InterCompanyID
WHERE
    F.GroupName =
        CASE
            WHEN @CompanyType = 1
            THEN @CompanyName
            ELSE F.GroupName
        END
    AND F.SrNo =
        CASE
            WHEN @CompanyType = 2
            THEN @CompanyId
            ELSE F.SrNo
        END
    AND AC.InterCompanyID <> 0
    AND V.Date <= CONVERT(CHAR(10), @DateTo, 120)
    AND F1.GROUPNAME IN
    (
        'HCP Plastene Bulkpack Ltd',
        'HCP ENTERPRISE LIMITED',
        'K.P. WOVEN PRIVATE LIMITED',
        'Plastene India Limited',
        'Plastene Polyfilms Limited',
        'Oswal Extrusion Limited',
        'OSWAL COMMODITIES PRIVATE LIMITED'
    )
    AND F1.GROUPNAME <> @CompanyName
GROUP BY
    F1.GROUPNAME";

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;

    public IntercompanyBalanceService(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
    }

    public async Task<IntercompanyDashboardDto> GetDashboardAsync(DateTime asOf, bool refresh = false)
    {
        var asOfDate = asOf.Date;
        var key = $"intercompany-balances-sp-v1|{asOfDate:yyyy-MM-dd}";
        if (refresh)
            _cache.Remove(key);

        if (_cache.TryGetValue(key, out IntercompanyDashboardDto? cached) && cached is not null)
            return cached;

        var dto = await BuildDashboardAsync(asOfDate);
        _cache.Set(key, dto, CacheTtl);
        return dto;
    }

    private async Task<IntercompanyDashboardDto> BuildDashboardAsync(DateTime asOfDate)
    {
        using var connection = _database.CreateConnection();
        var pairs = await QuerySpBalancesAsync(connection, asOfDate);

        var lookup = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in pairs)
        {
            var company = (row.CurrentCompany ?? "").Trim();
            var counterparty = (row.InterCompany ?? "").Trim();
            if (company.Length == 0 || counterparty.Length == 0)
                continue;

            if (!lookup.TryGetValue(company, out var map))
            {
                map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                lookup[company] = map;
            }

            map[counterparty] = Math.Round(row.ClosingINR, 2, MidpointRounding.AwayFromZero);
        }

        var lines = new List<IntercompanyLineDto>();
        var matrices = new List<IntercompanyMatrixDto>();

        foreach (var company in ReportCompanies)
        {
            lookup.TryGetValue(company, out var map);
            map ??= new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            var amounts = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var total = 0d;
            foreach (var other in ReportCompanies)
            {
                if (other.Equals(company, StringComparison.OrdinalIgnoreCase))
                    continue;
                var value = map.TryGetValue(other, out var v) ? v : 0d;
                amounts[other] = value;
                total += value;
            }

            matrices.Add(new IntercompanyMatrixDto
            {
                Company = company,
                Amounts = amounts,
                Total = Math.Round(total, 2, MidpointRounding.AwayFromZero),
            });

            foreach (var other in ReportCompanies)
            {
                if (other.Equals(company, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!map.TryGetValue(other, out var value) || Math.Round(Math.Abs(value), 0) == 0)
                    continue;

                lines.Add(new IntercompanyLineDto
                {
                    Company = company,
                    Counterparty = other,
                    LedgerName = other,
                    Balance = value,
                    BalanceCr = Math.Round(value / 10_000_000d, 2, MidpointRounding.AwayFromZero),
                });
            }
        }

        return new IntercompanyDashboardDto
        {
            AsOf = asOfDate.ToString("yyyy-MM-dd"),
            Counterparties = ReportCompanies.ToList(),
            Matrices = matrices,
            Lines = lines,
        };
    }

    private static async Task<List<SpBalanceRow>> QuerySpBalancesAsync(SqlConnection connection, DateTime asOf)
    {
        var rows = new List<SpBalanceRow>();
        foreach (var company in ReportCompanies)
        {
            var part = await connection.QueryAsync<SpBalanceRow>(
                BalanceSql,
                new
                {
                    CompanyName = company,
                    CompanyType = 1,
                    CompanyId = 0,
                    DateTo = asOf,
                },
                commandTimeout: CommandTimeoutSeconds);
            rows.AddRange(part);
        }

        return rows;
    }

    private sealed class SpBalanceRow
    {
        public string? CurrentCompany { get; set; }
        public string? InterCompany { get; set; }
        public double ClosingINR { get; set; }
    }
}

public class IntercompanyDashboardDto
{
    public string AsOf { get; set; } = "";
    public List<string> Counterparties { get; set; } = [];
    public List<IntercompanyMatrixDto> Matrices { get; set; } = [];
    public List<IntercompanyLineDto> Lines { get; set; } = [];
}

public class IntercompanyMatrixDto
{
    public string Company { get; set; } = "";
    public Dictionary<string, double> Amounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public double Total { get; set; }
}

public class IntercompanyLineDto
{
    public string Company { get; set; } = "";
    public string Counterparty { get; set; } = "";
    public string LedgerName { get; set; } = "";
    public double Balance { get; set; }
    public double BalanceCr { get; set; }
}
