using Microsoft.Data.SqlClient;

var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
    ?? throw new InvalidOperationException("Set DB_PASSWORD");
var connStr =
    $"Server=103.240.33.122,5115;Database=MaterialProcessing;User Id=sa;Password={password};TrustServerCertificate=True;Connection Timeout=120;";

const string breakdownSql = """
WITH credit AS (
  SELECT 'Credit Note' AS DocType, cn.CreditNoteNumber AS DocNo, cn.CreditNoteDate AS DocDate,
         cn.CompanyName, cn.PartyName, CAST(cn.TotalCreditAmount AS decimal(18,2)) AS InrAmt,
         LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) AS StoredFc,
         LTRIM(RTRIM(ISNULL(cn.currency,''))) AS Currency
  FROM CreditNote cn WITH (NOLOCK)
  INNER JOIN LedgerMaster lm WITH (NOLOCK) ON lm.CompanyName = cn.CompanyName AND lm.LedgerName = cn.SalesLedger
  WHERE LOWER(LTRIM(RTRIM(CONVERT(nvarchar(20), ISNULL(lm.IsInterCompany,'no'))))) NOT IN ('yes','y','1','true')
    AND NOT EXISTS (
      SELECT 1 FROM CommonLedgerMaster cm WITH (NOLOCK)
      WHERE LTRIM(RTRIM(cm.CompanyName)) = LTRIM(RTRIM(cn.CompanyName))
        AND LTRIM(RTRIM(cm.LedgerName)) = LTRIM(RTRIM(cn.SalesLedger))
        AND LOWER(LTRIM(RTRIM(CONVERT(nvarchar(20), ISNULL(cm.IsInterCompany,'no'))))) IN ('yes','y','1','true'))
    AND NOT EXISTS (
      SELECT 1 FROM ac_interCompanyLedger icl WITH (NOLOCK)
      INNER JOIN LedgerMaster icLm WITH (NOLOCK) ON icl.LedgerId = icLm.srno
      WHERE LTRIM(RTRIM(icLm.CompanyName)) = LTRIM(RTRIM(cn.CompanyName))
        AND LTRIM(RTRIM(icLm.LedgerName)) = LTRIM(RTRIM(cn.SalesLedger)))
    AND NOT EXISTS (
      SELECT 1 FROM FactoryInfo fi WITH (NOLOCK)
      WHERE LTRIM(RTRIM(fi.Name)) = LTRIM(RTRIM(cn.SalesLedger)) AND ISNULL(fi.Name,'') <> '')
    AND lm.LedgerName LIKE '%Export%'
    AND ABS(ISNULL(cn.TotalCreditAmount,0)) >= 100
    AND LTRIM(RTRIM(ISNULL(cn.currency,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
    AND (
      LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
      OR TRY_CAST(cn.currencyValue AS float) IS NULL
      OR ABS(ISNULL(TRY_CAST(cn.currencyValue AS float),0)) < 0.01)
),
debit AS (
  SELECT 'Debit Note' AS DocType, dn.DebitNoteNumber AS DocNo, dn.sysdate AS DocDate,
         dn.CompanyName, dn.PartyName, CAST(dn.TotalDebitAmount AS decimal(18,2)) AS InrAmt,
         LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) AS StoredFc,
         LTRIM(RTRIM(ISNULL(dn.currency,''))) AS Currency
  FROM DebitNote dn WITH (NOLOCK)
  INNER JOIN LedgerMaster lm WITH (NOLOCK) ON lm.CompanyName = dn.CompanyName AND lm.LedgerName = dn.PurchaseLedger
  WHERE LOWER(LTRIM(RTRIM(CONVERT(nvarchar(20), ISNULL(lm.IsInterCompany,'no'))))) NOT IN ('yes','y','1','true')
    AND NOT EXISTS (
      SELECT 1 FROM CommonLedgerMaster cm WITH (NOLOCK)
      WHERE LTRIM(RTRIM(cm.CompanyName)) = LTRIM(RTRIM(dn.CompanyName))
        AND LTRIM(RTRIM(cm.LedgerName)) = LTRIM(RTRIM(dn.PurchaseLedger))
        AND LOWER(LTRIM(RTRIM(CONVERT(nvarchar(20), ISNULL(cm.IsInterCompany,'no'))))) IN ('yes','y','1','true'))
    AND NOT EXISTS (
      SELECT 1 FROM ac_interCompanyLedger icl WITH (NOLOCK)
      INNER JOIN LedgerMaster icLm WITH (NOLOCK) ON icl.LedgerId = icLm.srno
      WHERE LTRIM(RTRIM(icLm.CompanyName)) = LTRIM(RTRIM(dn.CompanyName))
        AND LTRIM(RTRIM(icLm.LedgerName)) = LTRIM(RTRIM(dn.PurchaseLedger)))
    AND NOT EXISTS (
      SELECT 1 FROM FactoryInfo fi WITH (NOLOCK)
      WHERE LTRIM(RTRIM(fi.Name)) = LTRIM(RTRIM(dn.PurchaseLedger)) AND ISNULL(fi.Name,'') <> '')
    AND (lm.LedgerName LIKE '%Export%' OR lm.LedgerName LIKE '%Import%')
    AND ABS(ISNULL(dn.TotalDebitAmount,0)) >= 100
    AND LTRIM(RTRIM(ISNULL(dn.currency,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
    AND (
      LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
      OR TRY_CAST(dn.currencyValue AS float) IS NULL
      OR ABS(ISNULL(TRY_CAST(dn.currencyValue AS float),0)) < 0.01)
),
all_rows AS (SELECT * FROM credit UNION ALL SELECT * FROM debit),
classified AS (
  SELECT *,
    CASE
      WHEN StoredFc IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF') THEN 'symbol_only'
      WHEN StoredFc = '' THEN 'empty'
      WHEN TRY_CAST(StoredFc AS float) IS NOT NULL AND ABS(TRY_CAST(StoredFc AS float)) < 0.01 THEN 'numeric_zero'
      WHEN TRY_CAST(StoredFc AS float) IS NULL THEN 'invalid_non_numeric'
      ELSE 'other'
    END AS IssueType
  FROM all_rows
)
SELECT IssueType, COUNT(*) AS Cnt FROM classified GROUP BY IssueType ORDER BY Cnt DESC;
""";

const string zeroSamplesSql = """
WITH credit AS (
  SELECT 'Credit Note' AS DocType, cn.CreditNoteNumber AS DocNo, cn.CreditNoteDate AS DocDate,
         cn.CompanyName, cn.PartyName, CAST(cn.TotalCreditAmount AS decimal(18,2)) AS InrAmt,
         LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) AS StoredFc,
         LTRIM(RTRIM(ISNULL(cn.currency,''))) AS Currency
  FROM CreditNote cn WITH (NOLOCK)
  INNER JOIN LedgerMaster lm WITH (NOLOCK) ON lm.CompanyName = cn.CompanyName AND lm.LedgerName = cn.SalesLedger
  WHERE LOWER(LTRIM(RTRIM(CONVERT(nvarchar(20), ISNULL(lm.IsInterCompany,'no'))))) NOT IN ('yes','y','1','true')
    AND lm.LedgerName LIKE '%Export%'
    AND ABS(ISNULL(cn.TotalCreditAmount,0)) >= 100
    AND LTRIM(RTRIM(ISNULL(cn.currency,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
    AND LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) NOT IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
    AND (LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) = ''
         OR (TRY_CAST(cn.currencyValue AS float) IS NOT NULL AND ABS(TRY_CAST(cn.currencyValue AS float)) < 0.01))
),
debit AS (
  SELECT 'Debit Note' AS DocType, dn.DebitNoteNumber AS DocNo, dn.sysdate AS DocDate,
         dn.CompanyName, dn.PartyName, CAST(dn.TotalDebitAmount AS decimal(18,2)) AS InrAmt,
         LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) AS StoredFc,
         LTRIM(RTRIM(ISNULL(dn.currency,''))) AS Currency
  FROM DebitNote dn WITH (NOLOCK)
  INNER JOIN LedgerMaster lm WITH (NOLOCK) ON lm.CompanyName = dn.CompanyName AND lm.LedgerName = dn.PurchaseLedger
  WHERE LOWER(LTRIM(RTRIM(CONVERT(nvarchar(20), ISNULL(lm.IsInterCompany,'no'))))) NOT IN ('yes','y','1','true')
    AND (lm.LedgerName LIKE '%Export%' OR lm.LedgerName LIKE '%Import%')
    AND ABS(ISNULL(dn.TotalDebitAmount,0)) >= 100
    AND LTRIM(RTRIM(ISNULL(dn.currency,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
    AND LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) NOT IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
    AND (LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) = ''
         OR (TRY_CAST(dn.currencyValue AS float) IS NOT NULL AND ABS(TRY_CAST(dn.currencyValue AS float)) < 0.01))
)
SELECT TOP 10 * FROM (SELECT * FROM credit UNION ALL SELECT * FROM debit) x ORDER BY InrAmt DESC;
""";

const string invalidSamplesSql = """
SELECT TOP 15 StoredFc, COUNT(*) cnt
FROM (
  SELECT LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) AS StoredFc
  FROM CreditNote cn WITH (NOLOCK)
  INNER JOIN LedgerMaster lm WITH (NOLOCK) ON lm.CompanyName = cn.CompanyName AND lm.LedgerName = cn.SalesLedger
  WHERE lm.LedgerName LIKE '%Export%'
    AND ABS(ISNULL(cn.TotalCreditAmount,0)) >= 100
    AND LTRIM(RTRIM(ISNULL(cn.currency,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
    AND LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) NOT IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
    AND TRY_CAST(cn.currencyValue AS float) IS NULL
    AND LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) <> ''
  UNION ALL
  SELECT LTRIM(RTRIM(ISNULL(dn.currencyValue,'')))
  FROM DebitNote dn WITH (NOLOCK)
  INNER JOIN LedgerMaster lm WITH (NOLOCK) ON lm.CompanyName = dn.CompanyName AND lm.LedgerName = dn.PurchaseLedger
  WHERE (lm.LedgerName LIKE '%Export%' OR lm.LedgerName LIKE '%Import%')
    AND ABS(ISNULL(dn.TotalDebitAmount,0)) >= 100
    AND LTRIM(RTRIM(ISNULL(dn.currency,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
    AND LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) NOT IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
    AND TRY_CAST(dn.currencyValue AS float) IS NULL
    AND LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) <> ''
) t
GROUP BY StoredFc ORDER BY cnt DESC;
""";

await using var conn = new SqlConnection(connStr);
await conn.OpenAsync();
await RunQuery(conn, "Issue breakdown (audit rules + IC exclusion)", breakdownSql);
await RunQuery(conn, "Samples: empty or numeric-zero stored FC (basic, no full IC)", zeroSamplesSql);
await RunQuery(conn, "Top invalid_non_numeric StoredFc values", invalidSamplesSql);

const string broadZeroSql = """
SELECT 'CN_all_export' AS Bucket, COUNT(*) AS Cnt
FROM CreditNote cn WITH (NOLOCK)
INNER JOIN LedgerMaster lm WITH (NOLOCK) ON lm.CompanyName = cn.CompanyName AND lm.LedgerName = cn.SalesLedger
WHERE lm.LedgerName LIKE '%Export%'
  AND ABS(ISNULL(cn.TotalCreditAmount,0)) >= 100
  AND LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) NOT IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
  AND (LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) = ''
       OR (TRY_CAST(cn.currencyValue AS float) IS NOT NULL AND ABS(TRY_CAST(cn.currencyValue AS float)) < 0.01))
UNION ALL
SELECT 'DN_all_export_import', COUNT(*)
FROM DebitNote dn WITH (NOLOCK)
INNER JOIN LedgerMaster lm WITH (NOLOCK) ON lm.CompanyName = dn.CompanyName AND lm.LedgerName = dn.PurchaseLedger
WHERE (lm.LedgerName LIKE '%Export%' OR lm.LedgerName LIKE '%Import%')
  AND ABS(ISNULL(dn.TotalDebitAmount,0)) >= 100
  AND LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) NOT IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
  AND (LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) = ''
       OR (TRY_CAST(dn.currencyValue AS float) IS NOT NULL AND ABS(TRY_CAST(dn.currencyValue AS float)) < 0.01))
UNION ALL
SELECT 'CN_foreign_cur_zero_fc', COUNT(*)
FROM CreditNote cn WITH (NOLOCK)
INNER JOIN LedgerMaster lm WITH (NOLOCK) ON lm.CompanyName = cn.CompanyName AND lm.LedgerName = cn.SalesLedger
WHERE lm.LedgerName LIKE '%Export%'
  AND ABS(ISNULL(cn.TotalCreditAmount,0)) >= 100
  AND LTRIM(RTRIM(ISNULL(cn.currency,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
  AND LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) NOT IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
  AND (LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) = ''
       OR (TRY_CAST(cn.currencyValue AS float) IS NOT NULL AND ABS(TRY_CAST(cn.currencyValue AS float)) < 0.01))
UNION ALL
SELECT 'CN_any_ledger_foreign_zero', COUNT(*)
FROM CreditNote cn WITH (NOLOCK)
WHERE ABS(ISNULL(cn.TotalCreditAmount,0)) >= 100
  AND LTRIM(RTRIM(ISNULL(cn.currency,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
  AND LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) NOT IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
  AND (LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) = ''
       OR (TRY_CAST(cn.currencyValue AS float) IS NOT NULL AND ABS(TRY_CAST(cn.currencyValue AS float)) < 0.01));
""";

const string broadZeroSamplesSql = """
SELECT TOP 10 'CN' AS Src, cn.CreditNoteNumber AS DocNo, cn.CreditNoteDate AS DocDate, cn.CompanyName,
       cn.SalesLedger AS LedgerName, cn.TotalCreditAmount AS InrAmt,
       LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) AS StoredFc,
       LTRIM(RTRIM(ISNULL(cn.currency,''))) AS Currency
FROM CreditNote cn WITH (NOLOCK)
INNER JOIN LedgerMaster lm WITH (NOLOCK) ON lm.CompanyName = cn.CompanyName AND lm.LedgerName = cn.SalesLedger
WHERE lm.LedgerName LIKE '%Export%'
  AND ABS(ISNULL(cn.TotalCreditAmount,0)) >= 100
  AND LTRIM(RTRIM(ISNULL(cn.currency,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
  AND LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) NOT IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
  AND (LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) = ''
       OR (TRY_CAST(cn.currencyValue AS float) IS NOT NULL AND ABS(TRY_CAST(cn.currencyValue AS float)) < 0.01))
ORDER BY cn.TotalCreditAmount DESC;
""";

await RunQuery(conn, "Broader zero/empty counts", broadZeroSql);
await RunQuery(conn, "Samples foreign currency + zero FC on export CN", broadZeroSamplesSql);

const string literalZeroSql = """
SELECT 'CN_stored_0' AS Bucket, COUNT(*) cnt
FROM CreditNote cn WITH (NOLOCK)
WHERE ABS(ISNULL(cn.TotalCreditAmount,0)) >= 100
  AND LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) IN ('0','0.0','0.00','0.000')
UNION ALL
SELECT 'DN_stored_0', COUNT(*)
FROM DebitNote dn WITH (NOLOCK)
WHERE ABS(ISNULL(dn.TotalDebitAmount,0)) >= 100
  AND LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) IN ('0','0.0','0.00','0.000')
UNION ALL
SELECT 'CN_stored_empty', COUNT(*)
FROM CreditNote cn WITH (NOLOCK)
WHERE ABS(ISNULL(cn.TotalCreditAmount,0)) >= 100
  AND LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) = ''
UNION ALL
SELECT 'CN_stored_symbol_export', COUNT(*)
FROM CreditNote cn WITH (NOLOCK)
INNER JOIN LedgerMaster lm ON lm.CompanyName=cn.CompanyName AND lm.LedgerName=cn.SalesLedger
WHERE lm.LedgerName LIKE '%Export%'
  AND ABS(ISNULL(cn.TotalCreditAmount,0)) >= 100
  AND LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF');
""";

await RunQuery(conn, "Literal zero/empty/symbol counts (raw CN/DN)", literalZeroSql);

const string noIcSql = """
SELECT IssueType, COUNT(*) cnt FROM (
  SELECT CASE
    WHEN LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF') THEN 'symbol'
    WHEN LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) = '' THEN 'empty'
    WHEN TRY_CAST(cn.currencyValue AS float) IS NOT NULL AND ABS(TRY_CAST(cn.currencyValue AS float)) < 0.01 THEN 'numeric_zero'
    ELSE 'other_bad'
  END AS IssueType
  FROM CreditNote cn WITH (NOLOCK)
  INNER JOIN LedgerMaster lm ON lm.CompanyName=cn.CompanyName AND lm.LedgerName=cn.SalesLedger
  WHERE lm.LedgerName LIKE '%Export%'
    AND ABS(ISNULL(cn.TotalCreditAmount,0)) >= 100
    AND LTRIM(RTRIM(ISNULL(cn.currency,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
    AND (
      LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
      OR TRY_CAST(cn.currencyValue AS float) IS NULL
      OR ABS(ISNULL(TRY_CAST(cn.currencyValue AS float),0)) < 0.01)
  UNION ALL
  SELECT CASE
    WHEN LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF') THEN 'symbol'
    WHEN LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) = '' THEN 'empty'
    WHEN TRY_CAST(dn.currencyValue AS float) IS NOT NULL AND ABS(TRY_CAST(dn.currencyValue AS float)) < 0.01 THEN 'numeric_zero'
    ELSE 'other_bad'
  END
  FROM DebitNote dn WITH (NOLOCK)
  INNER JOIN LedgerMaster lm ON lm.CompanyName=dn.CompanyName AND lm.LedgerName=dn.PurchaseLedger
  WHERE (lm.LedgerName LIKE '%Export%' OR lm.LedgerName LIKE '%Import%')
    AND ABS(ISNULL(dn.TotalDebitAmount,0)) >= 100
    AND LTRIM(RTRIM(ISNULL(dn.currency,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
    AND (
      LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
      OR TRY_CAST(dn.currencyValue AS float) IS NULL
      OR ABS(ISNULL(TRY_CAST(dn.currencyValue AS float),0)) < 0.01)
) x GROUP BY IssueType;
""";

await RunQuery(conn, "Breakdown WITHOUT intercompany exclusion", noIcSql);

const string noForeignCurFilterSql = """
SELECT IssueType, COUNT(*) cnt FROM (
  SELECT CASE
    WHEN LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF') THEN 'symbol'
    WHEN LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) = '' THEN 'empty'
    WHEN TRY_CAST(cn.currencyValue AS float) IS NOT NULL AND ABS(TRY_CAST(cn.currencyValue AS float)) < 0.01 THEN 'numeric_zero'
    ELSE 'other_bad'
  END AS IssueType
  FROM CreditNote cn WITH (NOLOCK)
  INNER JOIN LedgerMaster lm ON lm.CompanyName=cn.CompanyName AND lm.LedgerName=cn.SalesLedger
  WHERE lm.LedgerName LIKE '%Export%'
    AND ABS(ISNULL(cn.TotalCreditAmount,0)) >= 100
    AND (
      LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
      OR TRY_CAST(cn.currencyValue AS float) IS NULL
      OR ABS(ISNULL(TRY_CAST(cn.currencyValue AS float),0)) < 0.01)
  UNION ALL
  SELECT CASE
    WHEN LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF') THEN 'symbol'
    WHEN LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) = '' THEN 'empty'
    WHEN TRY_CAST(dn.currencyValue AS float) IS NOT NULL AND ABS(TRY_CAST(dn.currencyValue AS float)) < 0.01 THEN 'numeric_zero'
    ELSE 'other_bad'
  END
  FROM DebitNote dn WITH (NOLOCK)
  INNER JOIN LedgerMaster lm ON lm.CompanyName=dn.CompanyName AND lm.LedgerName=dn.PurchaseLedger
  WHERE (lm.LedgerName LIKE '%Export%' OR lm.LedgerName LIKE '%Import%')
    AND ABS(ISNULL(dn.TotalDebitAmount,0)) >= 100
    AND (
      LTRIM(RTRIM(ISNULL(dn.currencyValue,''))) IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
      OR TRY_CAST(dn.currencyValue AS float) IS NULL
      OR ABS(ISNULL(TRY_CAST(dn.currencyValue AS float),0)) < 0.01)
) x GROUP BY IssueType;
""";

await RunQuery(conn, "Breakdown without foreign-currency filter on currency col", noForeignCurFilterSql);

const string otherBadSamplesSql = """
SELECT TOP 15 'CN' AS Src, cn.CreditNoteNumber AS DocNo, cn.TotalCreditAmount AS Inr,
       LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) AS StoredFc,
       LTRIM(RTRIM(ISNULL(cn.currency,''))) AS Currency,
       CASE
         WHEN LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) = '' THEN 'empty'
         WHEN TRY_CAST(cn.currencyValue AS float) IS NOT NULL AND ABS(TRY_CAST(cn.currencyValue AS float)) < 0.01 THEN 'numeric_zero'
         ELSE 'other'
       END AS ZeroType
FROM CreditNote cn WITH (NOLOCK)
INNER JOIN LedgerMaster lm ON lm.CompanyName=cn.CompanyName AND lm.LedgerName=cn.SalesLedger
WHERE lm.LedgerName LIKE '%Export%'
  AND ABS(ISNULL(cn.TotalCreditAmount,0)) >= 100
  AND LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) NOT IN ('$','USD','US$',N'€','EUR','Euro','GBP','CHF')
  AND (LTRIM(RTRIM(ISNULL(cn.currencyValue,''))) = ''
       OR (TRY_CAST(cn.currencyValue AS float) IS NOT NULL AND ABS(TRY_CAST(cn.currencyValue AS float)) < 0.01))
ORDER BY cn.TotalCreditAmount DESC;
""";

await RunQuery(conn, "Samples of empty/zero FC (no foreign currency filter)", otherBadSamplesSql);

static async Task RunQuery(SqlConnection conn, string title, string sql)
{
    Console.WriteLine($"\n========== {title} ==========");
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    cmd.CommandTimeout = 180;
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
