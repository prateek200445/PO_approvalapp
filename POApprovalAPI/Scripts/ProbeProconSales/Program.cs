using Microsoft.Data.SqlClient;

var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
    ?? "PlastOswal#@123$%^&*()iop";
var connStr =
    $"Server=103.240.33.122,5115;Database=MaterialProcessing;User Id=sa;Password={password};TrustServerCertificate=True;Connection Timeout=60;";

var queries = new[]
{
    """
    SELECT CompanyName, COUNT(*) cnt, ROUND(SUM(BillAMount),0) amt, MIN(InvDate) firstInv, MAX(InvDate) lastInv
    FROM vw_Salesvoucher WITH (NOLOCK)
    WHERE BuyerName LIKE '%Procon%Pacific%'
    GROUP BY CompanyName ORDER BY amt DESC
    """,
    """
    SELECT TOP 10 CompanyName, BuyerName, currency, InvType, InvDate, BillAMount, ExchangeRate
    FROM vw_Salesvoucher WITH (NOLOCK)
    WHERE CompanyName LIKE '%Polyfilms%'
      AND BuyerName LIKE '%Procon%'
    ORDER BY InvDate DESC
    """,
    """
    SELECT CompanyName, BuyerName, currency, InvType,
           COUNT(*) InvoiceCount, ROUND(SUM(BillAMount),0) TotalSalesInr,
           ROUND(SUM(CASE WHEN ISNULL(ExchangeRate,0)>1.0001 THEN BillAMount/NULLIF(ExchangeRate,0)
                          WHEN ISNULL(NULLIF(LTRIM(RTRIM(currency)),''),'INR') NOT IN ('INR','Rs.','RS','Rs') THEN BillAMount ELSE 0 END),0) TotalSalesFc
    FROM vw_Salesvoucher WITH (NOLOCK)
    WHERE CompanyName = 'Plastene Polyfilms Limited'
      AND BuyerName LIKE '%Procon%' AND BuyerName LIKE '%Pacific%'
      AND InvDate >= '2025-04-01' AND InvDate <= '2026-03-31'
      AND InvType NOT LIKE '%InterUnit%'
      AND (ISNULL(NULLIF(LTRIM(RTRIM(currency)), ''), 'INR') NOT IN ('INR', 'Rs.', 'RS', 'Rs')
           OR InvType LIKE '%Export%' OR ISNULL(ExchangeRate, 0) > 1.0001)
    GROUP BY CompanyName, BuyerName, currency, InvType
    ORDER BY TotalSalesFc DESC
    """,
    """
    SELECT TOP 50 CompanyName, BuyerName, currency, InvType,
           COUNT(*) InvoiceCount, ROUND(SUM(BillAMount),0) TotalSalesInr,
           ROUND(SUM(CASE WHEN ISNULL(ExchangeRate,0)>1.0001 THEN BillAMount/NULLIF(ExchangeRate,0)
                          WHEN ISNULL(NULLIF(LTRIM(RTRIM(currency)),''),'INR') NOT IN ('INR','Rs.','RS','Rs') THEN BillAMount ELSE 0 END),0) TotalSalesFc
    FROM vw_Salesvoucher WITH (NOLOCK)
    WHERE BuyerName LIKE '%Procon%' AND BuyerName LIKE '%Pacific%'
      AND InvDate >= '2025-04-01' AND InvDate <= '2026-03-31'
      AND InvType NOT LIKE '%InterUnit%'
      AND CompanyName <> 'Plastene Polyfilms Limited'
      AND (ISNULL(NULLIF(LTRIM(RTRIM(currency)), ''), 'INR') NOT IN ('INR', 'Rs.', 'RS', 'Rs')
           OR InvType LIKE '%Export%' OR ISNULL(ExchangeRate, 0) > 1.0001)
    GROUP BY CompanyName, BuyerName, currency, InvType
    ORDER BY TotalSalesFc DESC, TotalSalesInr DESC
    """,
};

await using var conn = new SqlConnection(connStr);
await conn.OpenAsync();
for (var i = 0; i < queries.Length; i++)
{
    Console.WriteLine($"\n========== Query {i + 1} ==========");
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = queries[i];
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
