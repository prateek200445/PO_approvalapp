/*
  Read-only stock as on a past date, for any company and any item.

  StockAsOn = today's WareHouse.StkInHand
             − net movement after @AsOnDate

  Net movement uses the same signs as Cl.Factory Owned in SP_STOCKANALYSIS_RPT_ALL:
    + Purchase, Warehouse Inwards, Branch Tfr Recd, Recd from JW (Own)
    + Net production (Total Production Own+JW − Prod. of JW)
    − Sales, Br Transfer Sent, Sent for JW (Own)
    − Net consumption (Total Consumption Own+JW − Consumption of JW)
    − Stock Adjustment Entry

  Does not INSERT, UPDATE, or DELETE any ERP table.
  @ItemCode NULL returns every item for the company and is slow. Pass an item code.

  Example:
    EXEC dbo.usp_StockInHandAsOn
         @CompanyName = N'HCP Plastene Bulkpack Ltd',
         @AsOnDate    = '2026-09-01',
         @ItemCode    = N'RAW06013';
*/
CREATE OR ALTER PROCEDURE dbo.usp_StockInHandAsOn
    @CompanyName varchar(150),
    @AsOnDate    date,
    @ItemCode    varchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Company varchar(150) = LTRIM(RTRIM(@CompanyName));
    DECLARE @Item varchar(50) = NULLIF(LTRIM(RTRIM(@ItemCode)), '');

    ;WITH currentStock AS (
        SELECT
            w.CompanyName,
            w.ItemCode,
            MAX(i.ItemName) AS ItemName,
            SUM(ISNULL(w.StkInHand, 0)) AS CurrentQty
        FROM WareHouse w WITH (NOLOCK)
        INNER JOIN Item i WITH (NOLOCK)
            ON i.ItemCode = w.ItemCode
           AND i.CompanyName = w.CompanyName
        WHERE w.CompanyName = @Company
          AND (@Item IS NULL OR w.ItemCode = @Item)
          AND i.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
        GROUP BY w.CompanyName, w.ItemCode
    ),
    moves AS (
        SELECT ItemCode, SUM(SignedQty) AS NetAfter
        FROM (
            /* Purchase / branch in / JW in from MRN. Recd For JW (Others) stays out of factory stock. */
            SELECT
                ItemCode,
                CASE
                    WHEN FirmGSTIn = VendorGST THEN qty
                    WHEN Categoryseries = 'JBIN-SE' THEN qty
                    WHEN Categoryseries = 'JBIN-OT' THEN 0
                    ELSE qty
                END AS SignedQty
            FROM (
                SELECT
                    ItemCode,
                    FirmGSTIn,
                    VendorGST,
                    Categoryseries,
                    CASE WHEN unit <> 'KGS' THEN ISNULL(netwt, 0) ELSE ISNULL(acceptedqty, 0) END AS qty
                FROM Vw_StoreInwards WITH (NOLOCK)
                WHERE CompanyName = @Company
                  AND (@Item IS NULL OR ItemCode = @Item)
                  AND Cancel <> 'Cancelled'
                  AND itemDeptt IN ('RM', 'SF', 'FG', 'RM Consumables')
                  AND sysdate > @AsOnDate
            ) mrn

            UNION ALL

            /* Godown post that is not the MRN itself (transid = 0 only). */
            SELECT w.ItemCode, SUM(v.qty)
            FROM WareHouseInwards v WITH (NOLOCK)
            INNER JOIN WareHouse w WITH (NOLOCK)
                ON w.ItemCode = v.ItemCode
               AND w.CompanyName = v.CompanyName
               AND w.WareHouseName = v.ToWareHouse
            WHERE v.transid = 0
              AND v.CompanyName = @Company
              AND (@Item IS NULL OR w.ItemCode = @Item)
              AND w.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
              AND v.InwardDate > @AsOnDate
            GROUP BY w.ItemCode

            UNION ALL

            /* Purchase voucher only when this company/item MRN is not already counted above. */
            SELECT
                pvi.ItemCode,
                CASE WHEN pvi.Per <> 'KGS' THEN ISNULL(SUM(pvi.netwt), 0) ELSE ISNULL(SUM(pvi.ActualQty), 0) END
            FROM PurchaseVoucherItem pvi WITH (NOLOCK)
            INNER JOIN Item WITH (NOLOCK)
                ON Item.CompanyName = pvi.CompanyName
               AND Item.ItemCode = pvi.ItemCode
            INNER JOIN PurchaseVoucher pv WITH (NOLOCK)
                ON pv.StoreInwardNo = pvi.StoreInwardNo
               AND pv.CompanyName = pvi.CompanyName
            WHERE pv.CompanyName = @Company
              AND (@Item IS NULL OR pvi.ItemCode = @Item)
              AND pv.SysDate > @AsOnDate
              AND Item.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
              AND NOT EXISTS (
                    SELECT 1
                    FROM Vw_StoreInwards s WITH (NOLOCK)
                    WHERE s.Cancel <> 'Cancelled'
                      AND s.SrNo = pv.StoreInwardNo
                      AND s.CompanyName = pv.CompanyName
                      AND s.ItemCode = pvi.ItemCode
              )
            GROUP BY pvi.ItemCode, pvi.Per

            UNION ALL

            /* Qty-difference debit note reduces purchase. */
            SELECT v.ItemCode, -SUM(v.QtyDifference)
            FROM vw_DebitNote v WITH (NOLOCK)
            INNER JOIN Item w WITH (NOLOCK)
                ON w.ItemCode = v.ItemCode
               AND w.CompanyName = v.CompanyName
            WHERE v.CompanyName = @Company
              AND (@Item IS NULL OR v.ItemCode = @Item)
              AND v.Sysdate > @AsOnDate
              AND w.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
              AND v.DebitType = 'Qty Difference'
            GROUP BY v.ItemCode

            UNION ALL

            SELECT i.ItemCode, -SUM(i.Netwt)
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
              AND (@Item IS NULL OR i.ItemCode = @Item)
              AND s.InvDate > @AsOnDate
              AND c.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
              AND s.VoucherType <> 'Job Invoice'
            GROUP BY i.ItemCode

            UNION ALL

            SELECT ItemCode, -SUM(ItemQty)
            FROM Despatch.dbo.vw_challan5a WITH (NOLOCK)
            WHERE companyname = @Company
              AND (@Item IS NULL OR ItemCode = @Item)
              AND sysdate > @AsOnDate
              AND Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
              AND (iscancel IS NULL OR iscancel = '')
            GROUP BY ItemCode

            UNION ALL

            SELECT ItemCode, SUM(qty)
            FROM vw_production_stk_FG WITH (NOLOCK)
            WHERE companyname = @Company
              AND (@Item IS NULL OR ItemCode = @Item)
              AND sysdate > @AsOnDate
              AND Deptt IN ('RM', 'SF', 'FG')
            GROUP BY ItemCode

            UNION ALL

            SELECT v.ItemCode, -SUM(v.Qty)
            FROM Despatch.dbo.vw_SubChallanListMulti v WITH (NOLOCK)
            INNER JOIN Despatch.dbo.vw_subsidiaryChallanItem o WITH (NOLOCK)
                ON o.ProcessorName = v.ProcessorName
               AND o.MainChallanDate = v.MainChallanDate
               AND o.MainChallanNo = v.MainChallanNo
            WHERE v.ProcessorName = @Company
              AND (@Item IS NULL OR v.ItemCode = @Item)
              AND v.Date > @AsOnDate
              AND o.Itemcode <> v.ItemCode
              AND ISNULL(o.isfreeze, 0) = 0
              AND (
                    (v.Deptt = 'RM' AND o.SubGroupName <> v.SubGroupName)
                 OR v.Deptt IN ('SF', 'FG', 'RM Consumables')
              )
            GROUP BY v.ItemCode

            UNION ALL

            SELECT ItemCode, -SUM(qty)
            FROM vw_consumption_stk_FG WITH (NOLOCK)
            WHERE companyname = @Company
              AND (@Item IS NULL OR ItemCode = @Item)
              AND sysdate > @AsOnDate
              AND Deptt IN ('RM', 'SF', 'FG')
            GROUP BY ItemCode

            UNION ALL

            SELECT o.ItemCode, SUM(v.Qty)
            FROM Despatch.dbo.vw_SubChallanListMulti v WITH (NOLOCK)
            INNER JOIN Despatch.dbo.vw_subsidiaryChallanItem o WITH (NOLOCK)
                ON o.ProcessorName = v.ProcessorName
               AND o.MainChallanDate = v.MainChallanDate
               AND o.MainChallanNo = v.MainChallanNo
               AND v.commodityname = o.commodityname
            WHERE v.ProcessorName = @Company
              AND (@Item IS NULL OR o.ItemCode = @Item)
              AND v.Date > @AsOnDate
              AND o.SubGroupName <> v.SubGroupName
              AND o.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
              AND ISNULL(o.isfreeze, 0) = 0
            GROUP BY o.ItemCode

            UNION ALL

            SELECT p.FGITEMCODE, -SUM(p.fPendingQty)
            FROM Prod_RMD_InOut p WITH (NOLOCK)
            INNER JOIN Item i WITH (NOLOCK)
                ON i.ItemCode = p.FGITEMCODE
               AND i.CompanyName = p.vCompanyName
            WHERE p.vCompanyName = @Company
              AND (@Item IS NULL OR p.FGITEMCODE = @Item)
              AND p.dSysdate > @AsOnDate
              AND p.vToGodown = 'Stock Adjustment Entry'
              AND i.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
            GROUP BY p.FGITEMCODE

            UNION ALL

            SELECT v.ItemCode, -SUM(v.qty)
            FROM WarehousetoWareHouse v WITH (NOLOCK)
            INNER JOIN WareHouse w WITH (NOLOCK)
                ON w.ItemCode = v.ItemCode
               AND w.CompanyName = v.CompanyName
               AND w.WareHouseName = v.ToWareHouse
            WHERE v.CompanyName = @Company
              AND (@Item IS NULL OR v.ItemCode = @Item)
              AND v.Sysdate > @AsOnDate
              AND v.ToWareHouse = 'Stock Adjustment Entry'
              AND w.Deptt IN ('RM', 'SF', 'FG', 'RM Consumables')
            GROUP BY v.ItemCode
        ) signed
        GROUP BY ItemCode
    )
    SELECT
        @Company AS CompanyName,
        COALESCE(c.ItemCode, m.ItemCode) AS ItemCode,
        c.ItemName,
        @AsOnDate AS AsOnDate,
        CAST(ISNULL(c.CurrentQty, 0) AS decimal(18, 3)) AS CurrentQty,
        CAST(ISNULL(m.NetAfter, 0) AS decimal(18, 3)) AS NetMovementAfter,
        CAST(ISNULL(c.CurrentQty, 0) - ISNULL(m.NetAfter, 0) AS decimal(18, 3)) AS StockAsOn
    FROM currentStock c
    FULL OUTER JOIN moves m
        ON m.ItemCode = c.ItemCode
    WHERE ISNULL(c.CurrentQty, 0) <> 0
       OR ISNULL(m.NetAfter, 0) <> 0
    ORDER BY COALESCE(c.ItemCode, m.ItemCode);
END
GO
