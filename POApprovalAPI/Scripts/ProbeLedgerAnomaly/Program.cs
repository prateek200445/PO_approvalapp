using Microsoft.Data.SqlClient;

var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
    ?? "PlastOswal#@123$%^&*()iop";
var connStr =
    $"Server=103.240.33.122,5115;Database=MaterialProcessing;User Id=sa;Password={password};TrustServerCertificate=True;Connection Timeout=60;";

var company = "K.P. WOVEN PRIVATE LIMITED";
var queries = new[]
{
    ("Distinct Under for creditor-ish ledgers", $"""
        SELECT DISTINCT TOP 30 Under, COUNT(*) cnt
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = '{company}'
          AND (Under LIKE '%Creditor%' OR Under LIKE 'Trade%')
        GROUP BY Under ORDER BY cnt DESC
        """),
    ("Current governed filter (>0, Creditors%)", $"""
        SELECT COUNT(*) cnt
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = '{company}'
          AND Under LIKE 'Creditors%'
          AND ISNULL(PendingBalance, 0) > 0
        """),
    ("Trade Creditors + positive PendingBalance", $"""
        SELECT TOP 10 LedgerName, Under, PendingBalance, Openingbalance
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = '{company}'
          AND Under LIKE 'Trade Creditors%'
          AND ISNULL(PendingBalance, 0) > 0
        ORDER BY PendingBalance DESC
        """),
    ("Any creditor Under + positive PendingBalance", $"""
        SELECT TOP 10 LedgerName, Under, PendingBalance, Openingbalance
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = '{company}'
          AND Under LIKE '%Creditor%'
          AND ISNULL(PendingBalance, 0) > 0
        ORDER BY PendingBalance DESC
        """),
    ("Any creditor Under + negative PendingBalance", $"""
        SELECT TOP 10 LedgerName, Under, PendingBalance, Openingbalance
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = '{company}'
          AND Under LIKE '%Creditor%'
          AND ISNULL(PendingBalance, 0) < 0
        ORDER BY PendingBalance
        """),
    ("Creditors-RM sub-group debit (positive)", $"""
        SELECT TOP 10 LedgerName, Under, PendingBalance, Openingbalance
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = '{company}'
          AND Under LIKE 'Creditors%'
          AND ISNULL(PendingBalance, 0) <> 0
        ORDER BY ABS(PendingBalance) DESC
        """),
    ("KPW creditors PendingBalance stats", $"""
        SELECT
          COUNT(*) total,
          SUM(CASE WHEN ISNULL(PendingBalance,0)>0 THEN 1 ELSE 0 END) pos,
          SUM(CASE WHEN ISNULL(PendingBalance,0)<0 THEN 1 ELSE 0 END) neg,
          SUM(CASE WHEN ISNULL(PendingBalance,0)=0 THEN 1 ELSE 0 END) zero
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = '{company}' AND Under LIKE 'Creditors%'
        """),
    ("Oswal creditors positive PendingBalance", $"""
        SELECT TOP 10 LedgerName, Under, PendingBalance, Openingbalance
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = 'Oswal Extrusion Limited'
          AND Under LIKE 'Creditors%'
          AND ISNULL(PendingBalance, 0) > 0
        ORDER BY PendingBalance DESC
        """),
    ("Oswal creditors negative PendingBalance", $"""
        SELECT TOP 10 LedgerName, Under, PendingBalance, Openingbalance
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = 'Oswal Extrusion Limited'
          AND Under LIKE 'Creditors%'
          AND ISNULL(PendingBalance, 0) < 0
        ORDER BY PendingBalance
        """),
    ("Oswal creditors non-zero either sign", $"""
        SELECT
          SUM(CASE WHEN ISNULL(PendingBalance,0)>0 THEN 1 ELSE 0 END) pos,
          SUM(CASE WHEN ISNULL(PendingBalance,0)<0 THEN 1 ELSE 0 END) neg
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = 'Oswal Extrusion Limited' AND Under LIKE 'Creditors%'
        """),
    ("KPW creditors Openingbalance stats", $"""
        SELECT
          SUM(CASE WHEN ISNULL(Openingbalance,0)>0 THEN 1 ELSE 0 END) pos,
          SUM(CASE WHEN ISNULL(Openingbalance,0)<0 THEN 1 ELSE 0 END) neg,
          SUM(CASE WHEN ISNULL(Openingbalance,0)=0 THEN 1 ELSE 0 END) zero
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = '{company}' AND Under LIKE 'Creditors%'
        """),
    ("KPW creditors Openingbalance > 0 sample", $"""
        SELECT TOP 10 LedgerName, Under, PendingBalance, Openingbalance
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = '{company}' AND Under LIKE 'Creditors%'
          AND ISNULL(Openingbalance,0) > 0
        ORDER BY Openingbalance DESC
        """),
    ("KPW creditors Openingbalance < 0 sample", $"""
        SELECT TOP 10 LedgerName, Under, PendingBalance, Openingbalance
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = '{company}' AND Under LIKE 'Creditors%'
          AND ISNULL(Openingbalance,0) < 0
        ORDER BY Openingbalance
        """),
    ("Bill-wise net for KPW creditors (sample)", $"""
        SELECT TOP 10 lm.LedgerName, lm.Under,
               SUM(bwt.Amount) netAmount, COUNT(*) bills
        FROM LedgerMaster lm WITH (NOLOCK)
        INNER JOIN vw_BillWiseTransaction bwt WITH (NOLOCK)
          ON lm.CompanyName = bwt.CompanyName AND lm.LedgerName = bwt.LedgerName
        WHERE lm.CompanyName = '{company}' AND lm.Under LIKE 'Creditors%'
        GROUP BY lm.LedgerName, lm.Under
        HAVING SUM(bwt.Amount) > 0
        ORDER BY SUM(bwt.Amount) DESC
        """),
    ("Bill-wise net negative for KPW creditors (sample)", $"""
        SELECT TOP 10 lm.LedgerName, lm.Under,
               SUM(bwt.Amount) netAmount, COUNT(*) bills
        FROM LedgerMaster lm WITH (NOLOCK)
        INNER JOIN vw_BillWiseTransaction bwt WITH (NOLOCK)
          ON lm.CompanyName = bwt.CompanyName AND lm.LedgerName = bwt.LedgerName
        WHERE lm.CompanyName = '{company}' AND lm.Under LIKE 'Creditors%'
        GROUP BY lm.LedgerName, lm.Under
        HAVING SUM(bwt.Amount) < 0
        ORDER BY SUM(bwt.Amount)
        """),
    ("KPW debtors Openingbalance stats", $"""
        SELECT
          SUM(CASE WHEN ISNULL(Openingbalance,0)>0 THEN 1 ELSE 0 END) pos,
          SUM(CASE WHEN ISNULL(Openingbalance,0)<0 THEN 1 ELSE 0 END) neg
        FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = '{company}' AND Under LIKE 'Debtors%'
        """),
    ("Count KPW creditors debit via Openingbalance > 0", $"""
        SELECT COUNT(*) cnt FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = '{company}' AND Under LIKE 'Creditors%' AND ISNULL(Openingbalance,0) > 0
        """),
    ("Count KPW creditors debit via bill-wise net > 0", $"""
        SELECT COUNT(*) cnt FROM (
          SELECT lm.LedgerName
          FROM LedgerMaster lm WITH (NOLOCK)
          INNER JOIN vw_BillWiseTransaction bwt WITH (NOLOCK)
            ON lm.CompanyName = bwt.CompanyName AND lm.LedgerName = bwt.LedgerName
          WHERE lm.CompanyName = '{company}' AND lm.Under LIKE 'Creditors%'
          GROUP BY lm.LedgerName
          HAVING SUM(bwt.Amount) > 0
        ) x
        """),
    ("Fixed formula count debtors credit", $"""
        SELECT COUNT(*) cnt FROM LedgerMaster WITH (NOLOCK)
        WHERE CompanyName = '{company}' AND Under LIKE 'Debtors%'
          AND ISNULL(NULLIF(PendingBalance, 0), Openingbalance) < 0
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
    if (n == 0) Console.WriteLine("(0 rows)");
}
