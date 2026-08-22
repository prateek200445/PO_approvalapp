using Microsoft.Data.SqlClient;

var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
    ?? "PlastOswal#@123$%^&*()iop";
var connStr =
    $"Server=103.240.33.122,5115;Database=MaterialProcessing;User Id=sa;Password={password};TrustServerCertificate=True;Connection Timeout=60;";

var company = "Plastene Polyfilms Limited";
var start = "2025-04-01";
var end = "2026-03-31";

var queries = new (string Title, string Sql)[]
{
    ("TOP expense ledgers (governed)", $"""
        SELECT TOP 15
            av.LedgerName,
            ISNULL(g.expensegrouphead, 'Unmapped') AS ExpenseGroupHead,
            COUNT(*) AS VoucherCount,
            ROUND(SUM(ISNULL(av.Debit, 0)), 2) AS TotalExpense
        FROM AccountVoucherApproval av WITH (NOLOCK)
        LEFT JOIN vw_Commonledgergrouping g WITH (NOLOCK)
            ON av.LedgerName = g.ledgername AND g.expensehead = 'Expenses'
        WHERE av.CompanyName = '{company}'
          AND (av.LedgerName LIKE '%Expense%'
               OR av.LedgerName IN (SELECT DISTINCT ledgername FROM vw_Commonledgergrouping WHERE expensehead = 'Expenses'))
          AND av.Status IN ('Approved', 'Pending')
          AND av.VoucherDate >= '{start}' AND av.VoucherDate <= '{end}'
        GROUP BY av.LedgerName, g.expensegrouphead
        HAVING SUM(ISNULL(av.Debit, 0)) > 0
        ORDER BY TotalExpense DESC
        """),
    ("All expense heads month-wise (governed)", $"""
        SELECT TOP 50
            YEAR(av.VoucherDate) AS ExpenseYear,
            MONTH(av.VoucherDate) AS ExpenseMonth,
            LEFT(DATENAME(month, av.VoucherDate), 3) AS MonthName,
            ISNULL(g.expensegrouphead, 'Unmapped') AS ExpenseGroupHead,
            ROUND(SUM(ISNULL(av.Debit, 0)), 2) AS ExpenseAmount
        FROM AccountVoucherApproval av WITH (NOLOCK)
        LEFT JOIN vw_Commonledgergrouping g WITH (NOLOCK)
            ON av.LedgerName = g.ledgername AND g.expensehead = 'Expenses'
        WHERE av.CompanyName = '{company}'
          AND (av.LedgerName LIKE '%Expense%'
               OR av.LedgerName IN (SELECT DISTINCT ledgername FROM vw_Commonledgergrouping WHERE expensehead = 'Expenses'))
          AND av.Status IN ('Approved', 'Pending')
          AND av.VoucherDate >= '{start}' AND av.VoucherDate <= '{end}'
        GROUP BY YEAR(av.VoucherDate), MONTH(av.VoucherDate), DATENAME(month, av.VoucherDate), g.expensegrouphead
        ORDER BY ExpenseYear, ExpenseMonth, ExpenseAmount DESC
        """)
};

await using var conn = new SqlConnection(connStr);
await conn.OpenAsync();
foreach (var (title, sql) in queries)
{
    Console.WriteLine($"\n========== {title} ==========");
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    cmd.CommandTimeout = 120;
    await using var reader = await cmd.ExecuteReaderAsync();
    var n = 0;
    while (await reader.ReadAsync())
    {
        if (n == 0)
        {
            for (var c = 0; c < reader.FieldCount; c++) Console.Write($"{reader.GetName(c)}\t");
            Console.WriteLine();
        }
        for (var c = 0; c < reader.FieldCount; c++) Console.Write($"{reader.GetValue(c)}\t");
        Console.WriteLine();
        n++;
    }
    Console.WriteLine(n == 0 ? "(0 rows)" : $"({n} rows)");
}
