CREATE OR ALTER PROCEDURE dbo.AppGstRecoBills
(
    @CompanyName varchar(100),
    @FromDate datetime,
    @ToDate datetime,
    @BillFrom datetime = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sv.sysdate,
        sv.StoreInwardNo,
        sv.yr,
        sv.SupplierName,
        LTRIM(RTRIM(ISNULL(l.NewGSTNo, ''))) AS Gstin,
        sv.VoucherType,
        CONVERT(varchar(40), sv.GeneralPurchaseNo) AS VoucherNo,
        LTRIM(RTRIM(ISNULL(sv.SupplierInvNo, ''))) AS BillNo,
        CONVERT(date, sv.SupplierInvDate) AS BillDate,
        sv.PurchaseLedger,
        CONVERT(decimal(18, 2), ISNULL(sv.BillAMount, 0)) AS BillAmount,
        CONVERT(decimal(18, 2), SUM(ISNULL(svi.Amount, 0) * ISNULL(sv.ExchangeRate, 1))) AS Basic
    INTO #voucher
    FROM purchasevoucher sv WITH (NOLOCK)
    INNER JOIN LedgerMaster l WITH (NOLOCK)
        ON l.CompanyName = sv.CompanyName AND l.LedgerName = sv.SupplierName
    LEFT JOIN purchasevoucherItem svi WITH (NOLOCK)
        ON sv.StoreInwardNo = svi.StoreInwardNo AND sv.yr = svi.yr AND sv.CompanyName = svi.CompanyName
    WHERE sv.CompanyName = @CompanyName
      AND sv.SysDate BETWEEN @FromDate AND @ToDate
      AND sv.SupplierInvDate IS NOT NULL
      AND CONVERT(date, sv.SupplierInvDate) >= CONVERT(date, ISNULL(@BillFrom, @FromDate))
      AND CONVERT(date, sv.SupplierInvDate) <= CONVERT(date, @ToDate)
    GROUP BY
        sv.sysdate, sv.StoreInwardNo, sv.yr, sv.SupplierName, l.NewGSTNo, sv.VoucherType,
        sv.GeneralPurchaseNo, sv.SupplierInvNo, sv.SupplierInvDate, sv.PurchaseLedger, sv.BillAMount;

    SELECT
        t.yr,
        t.StoreInwardNo,
        t.sysDate,
        CONVERT(decimal(18, 2), SUM(CASE WHEN t.TaxledgerName LIKE '%CGST%' AND t.TypeTransaction = 'Store Inwards' THEN ISNULL(t.Amount, 0) ELSE 0 END)) AS Cgst,
        CONVERT(decimal(18, 2), SUM(CASE WHEN t.TaxledgerName LIKE '%SGST%' AND t.TypeTransaction = 'Store Inwards' THEN ISNULL(t.Amount, 0) ELSE 0 END)) AS Sgst,
        CONVERT(decimal(18, 2), SUM(CASE WHEN t.TaxledgerName LIKE '%IGST%' AND t.TypeTransaction = 'Store Inwards' THEN ISNULL(t.Amount, 0) ELSE 0 END)) AS Igst,
        CONVERT(decimal(18, 2), SUM(CASE WHEN t.TaxledgerName LIKE '%GST%INPUT%' THEN 0 WHEN t.TaxledgerName LIKE '%Freight%' THEN ISNULL(t.Amount, 0) ELSE 0 END)) AS Freight,
        CONVERT(decimal(18, 2), SUM(CASE WHEN t.TaxledgerName LIKE '%Loading%' THEN ISNULL(t.Amount, 0) ELSE 0 END)) AS Loading,
        CONVERT(decimal(18, 2), SUM(CASE WHEN t.TaxledgerName LIKE '%Packing%Forward%' THEN ISNULL(t.Amount, 0) ELSE 0 END)) AS Packing,
        CONVERT(decimal(18, 2), SUM(CASE
            WHEN t.TaxledgerName LIKE '%GST%INPUT%' THEN 0
            WHEN t.TaxledgerName LIKE '%Freight%' THEN 0
            WHEN t.TaxledgerName LIKE '%Loading%' THEN 0
            WHEN t.TaxledgerName LIKE '%Packing%Forward%' THEN 0
            WHEN t.TaxledgerName LIKE '%Custom%Duty%' OR t.TaxledgerName LIKE '%Cenvatable%Part%' THEN 0
            WHEN t.TaxledgerName LIKE '%TCS%' THEN 0
            WHEN t.TaxledgerName LIKE '%CST%' AND ISNULL(t.RATE, 0) <> 0 THEN 0
            WHEN t.TaxledgerName LIKE '%Vat%' OR t.TaxledgerName LIKE '%Input Additional Tax%' THEN 0
            WHEN t.TaxledgerName LIKE '%Excise%' OR t.TaxledgerName LIKE '%cenvat%' OR t.TaxledgerName LIKE '%S&H%'
                 OR t.TaxledgerName LIKE '%Edu%Cess%' OR t.TaxledgerName LIKE '%Sp%Add%' THEN 0
            ELSE ISNULL(t.Amount, 0)
        END)) AS OtherCharge
    INTO #tax
    FROM purchaseVoucherTax t WITH (NOLOCK)
    WHERE t.CompanyName = @CompanyName
      AND t.sysDate BETWEEN @FromDate AND @ToDate
    GROUP BY t.yr, t.StoreInwardNo, t.sysDate;

    SELECT
        v.sysdate,
        v.StoreInwardNo,
        v.SupplierName,
        v.Gstin,
        v.VoucherType,
        v.VoucherNo,
        v.BillNo,
        v.BillDate,
        v.PurchaseLedger,
        v.BillAmount,
        v.Basic,
        ISNULL(tax.Cgst, 0) AS Cgst,
        ISNULL(tax.Sgst, 0) AS Sgst,
        ISNULL(tax.Igst, 0) AS Igst,
        ISNULL(tax.Freight, 0) AS Freight,
        ISNULL(tax.OtherCharge, 0) AS OtherCharge,
        ISNULL(tax.Loading, 0) AS Loading,
        ISNULL(tax.Packing, 0) AS Packing
    INTO #line
    FROM #voucher v
    LEFT JOIN #tax tax
        ON tax.yr = v.yr AND tax.StoreInwardNo = v.StoreInwardNo AND tax.sysDate = v.sysdate
    WHERE v.BillNo <> '' AND v.Gstin <> '';

    SELECT
        BillNo,
        BillDate,
        Gstin,
        SUM(Basic) AS Basic,
        SUM(Cgst) AS Cgst,
        SUM(Sgst) AS Sgst,
        SUM(Igst) AS Igst,
        SUM(Freight) AS Freight,
        SUM(OtherCharge) AS OtherCharge,
        SUM(Loading) AS Loading,
        SUM(Packing) AS Packing
    INTO #bill
    FROM #line
    GROUP BY BillNo, BillDate, Gstin;

    SELECT
        VoucherType,
        VoucherNo,
        sysdate AS VoucherDate,
        BillNo,
        BillDate,
        SupplierName AS Party,
        Gstin,
        CONVERT(varchar(40), StoreInwardNo) AS RefNo,
        PurchaseLedger AS Ledger,
        Basic,
        Cgst,
        Sgst,
        Igst,
        Freight,
        OtherCharge,
        Loading,
        Packing,
        BillAmount AS GrossAmount
    FROM (
        SELECT
            b.Basic,
            b.Cgst,
            b.Sgst,
            b.Igst,
            b.Freight,
            b.OtherCharge,
            b.Loading,
            b.Packing,
            l.VoucherType,
            l.VoucherNo,
            l.sysdate,
            l.BillNo,
            l.BillDate,
            l.SupplierName,
            l.Gstin,
            l.StoreInwardNo,
            l.PurchaseLedger,
            l.BillAmount,
            ROW_NUMBER() OVER (
                PARTITION BY l.BillNo, l.BillDate, l.Gstin
                ORDER BY ABS(l.Cgst) + ABS(l.Sgst) + ABS(l.Igst) DESC, ABS(l.Basic) DESC, l.VoucherNo
            ) AS rn
        FROM #bill b
        INNER JOIN #line l
            ON l.BillNo = b.BillNo AND l.BillDate = b.BillDate AND l.Gstin = b.Gstin
    ) ranked
    WHERE rn = 1;
END
