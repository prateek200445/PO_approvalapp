using Dapper;
using Microsoft.Data.SqlClient;

namespace POApprovalAPI.Services;

/// <summary>
/// Period movement between group companies: each company's own sales, purchase,
/// payment and receipt vouchers, with the other side resolved through
/// Ac_InterCompanyLedger. Same-group (inter-unit) pairs are omitted.
/// </summary>
public class IntercompanyTransactionService
{
    private const int CommandTimeoutSeconds = 180;

    private const string GroupsSql = """
        SELECT DISTINCT LTRIM(RTRIM(GroupName)) AS Name
        FROM dbo.FactoryInfo WITH (NOLOCK)
        WHERE NULLIF(LTRIM(RTRIM(GroupName)), N'') IS NOT NULL
        ORDER BY Name
        """;

    private const string MovementSql = """
        WITH map AS (
            SELECT DISTINCT
                LTRIM(RTRIM(l.CompanyName)) AS BookUnit,
                LTRIM(RTRIM(l.LedgerName)) AS LedgerName,
                LTRIM(RTRIM(fi.GroupName)) AS CounterGroup
            FROM dbo.Ac_InterCompanyLedger ac WITH (NOLOCK)
            INNER JOIN dbo.LedgerMaster l WITH (NOLOCK) ON ac.LedgerId = l.SrNo
            INNER JOIN dbo.FactoryInfo fi WITH (NOLOCK) ON fi.SrNo = ac.InterCompanyID
            WHERE NULLIF(LTRIM(RTRIM(fi.GroupName)), N'') IS NOT NULL
        ),
        books AS (
            SELECT LTRIM(RTRIM(Name)) AS Name, LTRIM(RTRIM(GroupName)) AS GroupName
            FROM dbo.FactoryInfo WITH (NOLOCK)
            WHERE NULLIF(LTRIM(RTRIM(GroupName)), N'') IS NOT NULL
        ),
        lines AS (
            SELECT b.GroupName AS BookGroup, m.CounterGroup, N'Sales' AS Kind,
                   CAST(ISNULL(v.BillAMount, 0) AS float) AS Amount
            FROM dbo.SalesVoucher v WITH (NOLOCK)
            INNER JOIN books b ON b.Name = LTRIM(RTRIM(v.CompanyName))
            INNER JOIN map m ON m.BookUnit = b.Name AND m.LedgerName = LTRIM(RTRIM(v.BuyerName))
            WHERE v.InvDate >= @From AND v.InvDate < @ToExcl
              AND b.GroupName <> m.CounterGroup
              AND b.GroupName IN @Groups
              AND m.CounterGroup IN @Groups

            UNION ALL

            SELECT b.GroupName, m.CounterGroup, N'Purchase',
                   CAST(ISNULL(v.BillAMount, 0) AS float)
            FROM dbo.PurchaseVoucher v WITH (NOLOCK)
            INNER JOIN books b ON b.Name = LTRIM(RTRIM(v.CompanyName))
            INNER JOIN map m ON m.BookUnit = b.Name AND m.LedgerName = LTRIM(RTRIM(v.SupplierName))
            WHERE v.SysDate >= @From AND v.SysDate < @ToExcl
              AND b.GroupName <> m.CounterGroup
              AND b.GroupName IN @Groups
              AND m.CounterGroup IN @Groups

            UNION ALL

            SELECT b.GroupName, m.CounterGroup, N'Payment',
                   CAST(ISNULL(p.PaymentAmt, 0) AS float)
            FROM dbo.Payment p WITH (NOLOCK)
            INNER JOIN books b ON b.Name = LTRIM(RTRIM(p.CompanyName))
            INNER JOIN map m ON m.BookUnit = b.Name AND m.LedgerName = LTRIM(RTRIM(p.LedgerTo))
            WHERE p.PaymentDate >= @From AND p.PaymentDate < @ToExcl
              AND b.GroupName <> m.CounterGroup
              AND b.GroupName IN @Groups
              AND m.CounterGroup IN @Groups

            UNION ALL

            SELECT b.GroupName, m.CounterGroup, N'Receipt',
                   CAST(ISNULL(p.PaymentAmt, 0) AS float)
            FROM dbo.PaymentReceipt p WITH (NOLOCK)
            INNER JOIN books b ON b.Name = LTRIM(RTRIM(p.companyname))
            INNER JOIN map m ON m.BookUnit = b.Name AND m.LedgerName = LTRIM(RTRIM(p.LedgerTo))
            WHERE p.PaymentDate >= @From AND p.PaymentDate < @ToExcl
              AND b.GroupName <> m.CounterGroup
              AND b.GroupName IN @Groups
              AND m.CounterGroup IN @Groups
        )
        SELECT BookGroup, CounterGroup, Kind, SUM(Amount) AS Amount
        FROM lines
        GROUP BY BookGroup, CounterGroup, Kind
        """;

    private readonly DatabaseService _database;

    public IntercompanyTransactionService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<string>> GetGroupsAsync()
    {
        using var connection = _database.CreateConnection();
        var names = await connection.QueryAsync<string>(GroupsSql, commandTimeout: CommandTimeoutSeconds);
        return names.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).ToList();
    }

    public async Task<IntercompanyTransactionDto> GetTransactionsAsync(
        DateTime from,
        DateTime to,
        IEnumerable<string> groups)
    {
        var fromDate = from.Date;
        var toDate = to.Date;
        if (toDate < fromDate)
            throw new ArgumentException("The end date must be on or after the start date.");
        if (toDate > fromDate.AddYears(5))
            throw new ArgumentException("Choose a period of five years or less.");

        var requested = groups
            .Select(g => (g ?? "").Trim())
            .Where(g => g.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (requested.Count < 2)
            throw new ArgumentException("Select at least two group companies.");

        using var connection = _database.CreateConnection();
        var known = (await connection.QueryAsync<string>(GroupsSql, commandTimeout: CommandTimeoutSeconds))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .ToList();

        var selected = known
            .Where(name => requested.Any(r => string.Equals(r, name, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (selected.Count < 2)
            throw new ArgumentException("Select at least two group companies from the list.");

        var rows = await connection.QueryAsync<MovementRow>(
            MovementSql,
            new { From = fromDate, ToExcl = toDate.AddDays(1), Groups = selected },
            commandTimeout: CommandTimeoutSeconds);

        var cells = new Dictionary<string, IntercompanyTransactionCellDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var book = Canonical(selected, row.BookGroup);
            var counter = Canonical(selected, row.CounterGroup);
            if (book.Length == 0 || counter.Length == 0 || string.Equals(book, counter, StringComparison.OrdinalIgnoreCase))
                continue;

            var key = book + "\u001f" + counter;
            if (!cells.TryGetValue(key, out var cell))
            {
                cell = new IntercompanyTransactionCellDto { BookGroup = book, CounterGroup = counter };
                cells[key] = cell;
            }

            var amount = Math.Round(row.Amount, 2, MidpointRounding.AwayFromZero);
            switch ((row.Kind ?? "").Trim())
            {
                case "Sales": cell.Sales += amount; break;
                case "Purchase": cell.Purchase += amount; break;
                case "Payment": cell.Payment += amount; break;
                case "Receipt": cell.Receipt += amount; break;
            }
        }

        return new IntercompanyTransactionDto
        {
            From = fromDate.ToString("yyyy-MM-dd"),
            To = toDate.ToString("yyyy-MM-dd"),
            Groups = selected,
            Cells = cells.Values
                .OrderBy(c => c.BookGroup, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.CounterGroup, StringComparer.OrdinalIgnoreCase)
                .ToList(),
        };
    }

    private static string Canonical(IReadOnlyList<string> selected, string? name)
    {
        var value = (name ?? "").Trim();
        if (value.Length == 0) return "";
        return selected.FirstOrDefault(s => string.Equals(s, value, StringComparison.OrdinalIgnoreCase)) ?? "";
    }

    private sealed class MovementRow
    {
        public string? BookGroup { get; set; }
        public string? CounterGroup { get; set; }
        public string? Kind { get; set; }
        public double Amount { get; set; }
    }
}

public class IntercompanyTransactionDto
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public List<string> Groups { get; set; } = [];
    public List<IntercompanyTransactionCellDto> Cells { get; set; } = [];
}

public class IntercompanyTransactionCellDto
{
    public string BookGroup { get; set; } = "";
    public string CounterGroup { get; set; } = "";
    public double Sales { get; set; }
    public double Purchase { get; set; }
    public double Payment { get; set; }
    public double Receipt { get; set; }
}
