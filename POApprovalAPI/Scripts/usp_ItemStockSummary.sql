/*
  Read-only item stock summary for a date range.

  Same shape as a ledger:
    opening
    + each movement in the range
    = closing

  An item stored as rolls (approved loom rolls in vw_Prod_BeforeRMD) uses the
  rolls themselves. Opening and closing are the roll weight on those dates.
  A roll stays until it moves from a roll godown to despatch, cutting, or a
  bag godown. Inward is rolls produced in the range. Outward is rolls that
  left in the range. Weight is fPendingQty, which despatch does not reduce.

  Any other item uses today's WareHouse.StkInHand minus every movement after
  @DateFrom, the same figure as dbo.usp_StockInHandAsOn. Each source is read
  once. Signs match Cl.Factory Owned in SP_STOCKANALYSIS_RPT_ALL.
  Recd For JW (Others) / JBIN-OT is left out.

  Closing = Opening + Inward − Outward.
  Does not INSERT, UPDATE, or DELETE any ERP table.

  Result 1: opening, one total per movement type, closing.
  Result 2: one row per document, with a running balance.

  Example:
    EXEC dbo.usp_ItemStockSummary
         @CompanyName = N'HCP Plastene Bulkpack Ltd',
         @ItemCode    = N'RAW06013',
         @DateFrom    = '2026-09-01',
         @DateTo      = '2026-10-06';
*/
CREATE OR ALTER PROCEDURE dbo.usp_ItemStockSummary
    @CompanyName varchar(150),
    @ItemCode    varchar(50),
    @DateFrom    date,
    @DateTo      date
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Company varchar(150) = LTRIM(RTRIM(@CompanyName));
    DECLARE @Item varchar(50) = NULLIF(LTRIM(RTRIM(@ItemCode)), '');
    DECLARE @From datetime = CAST(@DateFrom AS datetime);

    IF @Item IS NULL
    BEGIN
        RAISERROR('Pass an item code.', 16, 1);
        RETURN;
    END;

    IF @DateTo < @DateFrom
    BEGIN
        RAISERROR('Date to is before date from.', 16, 1);
        RETURN;
    END;

    DECLARE @ItemName varchar(200) = (
        SELECT MAX(ItemName)
        FROM Item WITH (NOLOCK)
        WHERE CompanyName = @Company
          AND ItemCode = @Item
    );

    CREATE TABLE #txn (
        TxnDate       date,
        MovementType  varchar(50),
        DocNo         varchar(100),
        InwardQty     decimal(18, 3),
        OutwardQty    decimal(18, 3)
    );

    DECLARE @ToEnd datetime = DATEADD(day, 1, CAST(@DateTo AS datetime));

    /* Roll items: opening, produced, sent out, and closing are the rolls. */
    IF EXISTS (
        SELECT 1
        FROM vw_Prod_BeforeRMD WITH (NOLOCK)
        WHERE Companyname = @Company
          AND FGItemCode = @Item
          AND status = 'Loom'
          AND vStatus = 'Approved'
          AND ROUND(fPendingQty, 2) > 0
    )
    BEGIN
        CREATE TABLE #roll (
            RollKey varchar(100) COLLATE DATABASE_DEFAULT,
            RollNo  varchar(100),
            Sysdate datetime,
            Wt      decimal(18, 3)
        );

        INSERT #roll (RollKey, RollNo, Sysdate, Wt)
        SELECT
            REPLACE(ISNULL(v.CuttingRollNo, v.RollNo), 'CUT', '') COLLATE DATABASE_DEFAULT,
            ISNULL(v.CuttingRollNo, v.RollNo),
            v.Sysdate,
            CAST(v.fPendingQty AS decimal(18, 3))
        FROM vw_Prod_BeforeRMD v WITH (NOLOCK)
        WHERE v.Companyname = @Company
          AND v.FGItemCode = @Item
          AND v.Sysdate >= '2000-01-01'
          AND v.Sysdate < @ToEnd
          AND v.status = 'Loom'
          AND v.vStatus = 'Approved'
          AND (v.CuttingRollNo LIKE '%CUT' OR v.CuttingRollNo IS NULL)
          AND ROUND(v.fPendingQty, 2) > 0;

        CREATE TABLE #out (
            RollKey  varchar(100) COLLATE DATABASE_DEFAULT NOT NULL,
            FirstOut datetime
        );

        INSERT #out (RollKey, FirstOut)
        SELECT x.vItemCode COLLATE DATABASE_DEFAULT, MIN(x.dSysdate)
        FROM prod_rmd_inout x WITH (NOLOCK)
        INNER JOIN #roll r
            ON r.RollKey = x.vItemCode COLLATE DATABASE_DEFAULT
        WHERE x.vCompanyName = @Company
          AND x.vtype = 'Loom'
          AND x.vFromGodown IN (
                'Lamination Godown',
                'Liner Bag Godown',
                'Loom Godown',
                'Needle Loom Godown',
                'Roll Material Godown'
              )
          AND x.vToGodown IN (
                'Despatch Godown',
                'Stock Adjustment Entry',
                'Cutting Godown',
                'FIBC Bag Godown',
                'Small Bag Godown'
              )
        GROUP BY x.vItemCode COLLATE DATABASE_DEFAULT;

        DECLARE @RollOpening decimal(18, 3) = ISNULL((
            SELECT SUM(r.Wt)
            FROM #roll r
            LEFT JOIN #out o ON o.RollKey = r.RollKey
            WHERE r.Sysdate < @From
              AND (o.FirstOut IS NULL OR o.FirstOut >= @From)
        ), 0);

        INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
        SELECT CAST(r.Sysdate AS date), 'Rolls produced', ISNULL(r.RollNo, ''), r.Wt, 0
        FROM #roll r
        WHERE r.Sysdate >= @From
          AND r.Sysdate < @ToEnd;

        INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
        SELECT CAST(o.FirstOut AS date), 'Rolls sent out', ISNULL(r.RollNo, ''), 0, r.Wt
        FROM #roll r
        INNER JOIN #out o ON o.RollKey = r.RollKey
        WHERE o.FirstOut >= @From
          AND o.FirstOut < @ToEnd;

        DECLARE @RollInward decimal(18, 3) = ISNULL((SELECT SUM(InwardQty) FROM #txn), 0);
        DECLARE @RollOutward decimal(18, 3) = ISNULL((SELECT SUM(OutwardQty) FROM #txn), 0);
        DECLARE @RollClosing decimal(18, 3) = @RollOpening + @RollInward - @RollOutward;

        SELECT
            @Company AS CompanyName,
            @Item AS ItemCode,
            @ItemName AS ItemName,
            @DateFrom AS DateFrom,
            @DateTo AS DateTo,
            LineType,
            MovementType,
            InwardQty,
            OutwardQty,
            Balance
        FROM (
            SELECT
                0 AS SortNo,
                'Opening' AS LineType,
                '' AS MovementType,
                CAST(0 AS decimal(18, 3)) AS InwardQty,
                CAST(0 AS decimal(18, 3)) AS OutwardQty,
                @RollOpening AS Balance
            UNION ALL
            SELECT
                1,
                'Movement',
                MovementType,
                SUM(InwardQty),
                SUM(OutwardQty),
                SUM(InwardQty) - SUM(OutwardQty)
            FROM #txn
            GROUP BY MovementType
            UNION ALL
            SELECT
                2,
                'Closing',
                '',
                @RollInward,
                @RollOutward,
                @RollClosing
        ) summary
        ORDER BY SortNo, MovementType;

        SELECT
            @Company AS CompanyName,
            @Item AS ItemCode,
            @ItemName AS ItemName,
            TxnDate,
            MovementType,
            DocNo,
            InwardQty,
            OutwardQty,
            @RollOpening + SUM(InwardQty - OutwardQty) OVER (
                ORDER BY TxnDate, MovementType, DocNo
                ROWS UNBOUNDED PRECEDING
            ) AS Balance
        FROM #txn
        ORDER BY TxnDate, MovementType, DocNo;

        RETURN;
    END;

    /* Purchase, branch receipt, and job-work receipt from the MRN. JBIN-OT is excluded. */
    INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
    SELECT
        CAST(sysdate AS date),
        CASE
            WHEN FirmGSTIn = VendorGST THEN 'Branch Tfr Recd'
            WHEN Categoryseries = 'JBIN-SE' THEN 'Recd from JW (Own)'
            ELSE 'Purchase'
        END,
        ISNULL(MRNo, ''),
        SUM(CASE WHEN unit <> 'KGS' THEN ISNULL(netwt, 0) ELSE ISNULL(acceptedqty, 0) END),
        0
    FROM Vw_StoreInwards WITH (NOLOCK)
    WHERE CompanyName = @Company
      AND ItemCode = @Item
      AND Cancel <> 'Cancelled'
      AND itemDeptt IN ('RM', 'SF', 'FG', 'RM Consumables')
      AND sysdate > @From
      AND ISNULL(Categoryseries, '') <> 'JBIN-OT'
    GROUP BY
        CAST(sysdate AS date),
        CASE
            WHEN FirmGSTIn = VendorGST THEN 'Branch Tfr Recd'
            WHEN Categoryseries = 'JBIN-SE' THEN 'Recd from JW (Own)'
            ELSE 'Purchase'
        END,
        ISNULL(MRNo, '');

    /* Godown post that is not the MRN itself. */
    INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
    SELECT
        CAST(v.InwardDate AS date),
        'Warehouse Inwards',
        '',
        SUM(v.qty),
        0
    FROM WareHouseInwards v WITH (NOLOCK)
    INNER JOIN WareHouse w WITH (NOLOCK)
        ON w.ItemCode = v.ItemCode
       AND w.CompanyName = v.CompanyName
       AND w.WareHouseName = v.ToWareHouse
    WHERE v.transid = 0
      AND v.CompanyName = @Company
      AND w.ItemCode = @Item
      AND w.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
      AND v.InwardDate > @From
    GROUP BY CAST(v.InwardDate AS date);

    /* Purchase voucher only when that MRN is not already in Vw_StoreInwards. */
    INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
    SELECT
        CAST(pv.SysDate AS date),
        'Purchase',
        ISNULL(pv.StoreInwardNo, ''),
        SUM(CASE WHEN pvi.Per <> 'KGS' THEN ISNULL(pvi.netwt, 0) ELSE ISNULL(pvi.ActualQty, 0) END),
        0
    FROM PurchaseVoucherItem pvi WITH (NOLOCK)
    INNER JOIN Item WITH (NOLOCK)
        ON Item.CompanyName = pvi.CompanyName
       AND Item.ItemCode = pvi.ItemCode
    INNER JOIN PurchaseVoucher pv WITH (NOLOCK)
        ON pv.StoreInwardNo = pvi.StoreInwardNo
       AND pv.CompanyName = pvi.CompanyName
    WHERE pv.CompanyName = @Company
      AND pvi.ItemCode = @Item
      AND pv.SysDate > @From
      AND Item.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
      AND NOT EXISTS (
            SELECT 1
            FROM Vw_StoreInwards s WITH (NOLOCK)
            WHERE s.Cancel <> 'Cancelled'
              AND s.SrNo = pv.StoreInwardNo
              AND s.CompanyName = pv.CompanyName
              AND s.ItemCode = pvi.ItemCode
      )
    GROUP BY CAST(pv.SysDate AS date), ISNULL(pv.StoreInwardNo, '');

    /* Qty-difference debit note reduces purchase. */
    INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
    SELECT
        CAST(v.Sysdate AS date),
        'Purchase',
        ISNULL(v.DebitNoteNumber, ''),
        0,
        SUM(v.QtyDifference)
    FROM vw_DebitNote v WITH (NOLOCK)
    INNER JOIN Item w WITH (NOLOCK)
        ON w.ItemCode = v.ItemCode
       AND w.CompanyName = v.CompanyName
    WHERE v.CompanyName = @Company
      AND v.ItemCode = @Item
      AND v.Sysdate > @From
      AND w.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
      AND v.DebitType = 'Qty Difference'
    GROUP BY CAST(v.Sysdate AS date), ISNULL(v.DebitNoteNumber, '');

    INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
    SELECT
        CAST(s.InvDate AS date),
        'Sales',
        ISNULL(s.InvNo, ''),
        0,
        SUM(i.Netwt)
    FROM SalesVoucher s WITH (NOLOCK)
    INNER JOIN SalesVoucherItem i WITH (NOLOCK)
        ON s.companyId = i.companyId
       AND s.CompanyName = i.CompanyName
       AND s.InvNo = i.InvNo
       AND s.InvDate = i.InvDate
       AND s.InvYear = i.Invyear
    INNER JOIN Item c WITH (NOLOCK)
        ON c.CompanyName = i.CompanyName
       AND c.ItemCode = i.ItemCode
    WHERE s.CompanyName = @Company
      AND i.ItemCode = @Item
      AND s.InvDate > @From
      AND c.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
      AND s.VoucherType <> 'Job Invoice'
    GROUP BY CAST(s.InvDate AS date), ISNULL(s.InvNo, '');

    INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
    SELECT
        CAST(sysdate AS date),
        CASE
            WHEN NewGSTNo = factoryGSTNo THEN 'Br Transfer Sent'
            ELSE 'Sent for JW (Own)'
        END,
        ISNULL(ChallanNo, ''),
        0,
        SUM(ItemQty)
    FROM Despatch.dbo.vw_challan5a WITH (NOLOCK)
    WHERE companyname = @Company
      AND ItemCode = @Item
      AND sysdate > @From
      AND Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
      AND (iscancel IS NULL OR iscancel = '')
    GROUP BY
        CAST(sysdate AS date),
        CASE
            WHEN NewGSTNo = factoryGSTNo THEN 'Br Transfer Sent'
            ELSE 'Sent for JW (Own)'
        END,
        ISNULL(ChallanNo, '');

    INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
    SELECT
        CAST(sysdate AS date),
        'Total Production Own+JW',
        '',
        SUM(qty),
        0
    FROM vw_production_stk_FG WITH (NOLOCK)
    WHERE companyname = @Company
      AND ItemCode = @Item
      AND sysdate > @From
      AND Deptt IN ('RM', 'SF', 'FG')
    GROUP BY CAST(sysdate AS date);

    INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
    SELECT
        CAST(v.Date AS date),
        'Production Of JW',
        ISNULL(v.MainChallanNo, ''),
        0,
        SUM(v.Qty)
    FROM Despatch.dbo.vw_SubChallanListMulti v WITH (NOLOCK)
    INNER JOIN Despatch.dbo.vw_subsidiaryChallanItem o WITH (NOLOCK)
        ON o.ProcessorName = v.ProcessorName
       AND o.MainChallanDate = v.MainChallanDate
       AND o.MainChallanNo = v.MainChallanNo
    WHERE v.ProcessorName = @Company
      AND v.ItemCode = @Item
      AND v.Date > @From
      AND o.Itemcode <> v.ItemCode
      AND ISNULL(o.isfreeze, 0) = 0
      AND (
            (v.Deptt = 'RM' AND o.SubGroupName <> v.SubGroupName)
         OR v.Deptt IN ('SF', 'FG', 'RM Consumables')
      )
    GROUP BY CAST(v.Date AS date), ISNULL(v.MainChallanNo, '');

    INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
    SELECT
        CAST(sysdate AS date),
        'Total Consumption Own+JW',
        '',
        0,
        SUM(qty)
    FROM vw_consumption_stk_FG WITH (NOLOCK)
    WHERE companyname = @Company
      AND ItemCode = @Item
      AND sysdate > @From
      AND Deptt IN ('RM', 'SF', 'FG')
    GROUP BY CAST(sysdate AS date);

    /* Added back so net consumption = total consumption − consumption of JW. */
    INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
    SELECT
        CAST(v.Date AS date),
        'Consumption of JW',
        ISNULL(v.MainChallanNo, ''),
        SUM(v.Qty),
        0
    FROM Despatch.dbo.vw_SubChallanListMulti v WITH (NOLOCK)
    INNER JOIN Despatch.dbo.vw_subsidiaryChallanItem o WITH (NOLOCK)
        ON o.ProcessorName = v.ProcessorName
       AND o.MainChallanDate = v.MainChallanDate
       AND o.MainChallanNo = v.MainChallanNo
       AND v.commodityname = o.commodityname
    WHERE v.ProcessorName = @Company
      AND o.ItemCode = @Item
      AND v.Date > @From
      AND o.SubGroupName <> v.SubGroupName
      AND o.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
      AND ISNULL(o.isfreeze, 0) = 0
    GROUP BY CAST(v.Date AS date), ISNULL(v.MainChallanNo, '');

    INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
    SELECT
        CAST(p.dSysdate AS date),
        'Stock Adjustment Entry',
        '',
        0,
        SUM(p.fPendingQty)
    FROM Prod_RMD_InOut p WITH (NOLOCK)
    INNER JOIN Item i WITH (NOLOCK)
        ON i.ItemCode = p.FGITEMCODE
       AND i.CompanyName = p.vCompanyName
    WHERE p.vCompanyName = @Company
      AND p.FGITEMCODE = @Item
      AND p.dSysdate > @From
      AND p.vToGodown = 'Stock Adjustment Entry'
      AND i.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
    GROUP BY CAST(p.dSysdate AS date);

    INSERT #txn (TxnDate, MovementType, DocNo, InwardQty, OutwardQty)
    SELECT
        CAST(v.Sysdate AS date),
        'Stock Adjustment Entry',
        '',
        0,
        SUM(v.qty)
    FROM WarehousetoWareHouse v WITH (NOLOCK)
    INNER JOIN WareHouse w WITH (NOLOCK)
        ON w.ItemCode = v.ItemCode
       AND w.CompanyName = v.CompanyName
       AND w.WareHouseName = v.ToWareHouse
    WHERE v.CompanyName = @Company
      AND v.ItemCode = @Item
      AND v.Sysdate > @From
      AND v.ToWareHouse = 'Stock Adjustment Entry'
      AND w.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
    GROUP BY CAST(v.Sysdate AS date);

    DECLARE @Current decimal(18, 3) = ISNULL((
        SELECT SUM(ISNULL(w.StkInHand, 0))
        FROM WareHouse w WITH (NOLOCK)
        INNER JOIN Item i WITH (NOLOCK)
            ON i.ItemCode = w.ItemCode
           AND i.CompanyName = w.CompanyName
        WHERE w.CompanyName = @Company
          AND w.ItemCode = @Item
          AND i.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
    ), 0);
    DECLARE @Opening decimal(18, 3) = @Current - ISNULL((SELECT SUM(InwardQty - OutwardQty) FROM #txn), 0);

    DELETE FROM #txn WHERE TxnDate > @DateTo;

    DECLARE @Inward decimal(18, 3) = ISNULL((SELECT SUM(InwardQty) FROM #txn), 0);
    DECLARE @Outward decimal(18, 3) = ISNULL((SELECT SUM(OutwardQty) FROM #txn), 0);
    DECLARE @Closing decimal(18, 3) = @Opening + @Inward - @Outward;

    SELECT
        @Company AS CompanyName,
        @Item AS ItemCode,
        @ItemName AS ItemName,
        @DateFrom AS DateFrom,
        @DateTo AS DateTo,
        LineType,
        MovementType,
        InwardQty,
        OutwardQty,
        Balance
    FROM (
        SELECT
            0 AS SortNo,
            'Opening' AS LineType,
            '' AS MovementType,
            CAST(0 AS decimal(18, 3)) AS InwardQty,
            CAST(0 AS decimal(18, 3)) AS OutwardQty,
            @Opening AS Balance
        UNION ALL
        SELECT
            1,
            'Movement',
            MovementType,
            SUM(InwardQty),
            SUM(OutwardQty),
            SUM(InwardQty) - SUM(OutwardQty)
        FROM #txn
        GROUP BY MovementType
        UNION ALL
        SELECT
            2,
            'Closing',
            '',
            @Inward,
            @Outward,
            @Closing
    ) summary
    ORDER BY SortNo, MovementType;

    SELECT
        @Company AS CompanyName,
        @Item AS ItemCode,
        @ItemName AS ItemName,
        TxnDate,
        MovementType,
        DocNo,
        InwardQty,
        OutwardQty,
        @Opening + SUM(InwardQty - OutwardQty) OVER (
            ORDER BY TxnDate, MovementType, DocNo
            ROWS UNBOUNDED PRECEDING
        ) AS Balance
    FROM #txn
    ORDER BY TxnDate, MovementType, DocNo;
END
GO
