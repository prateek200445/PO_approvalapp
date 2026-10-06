/*
  Read-only roll list for one company and item, using the same rules as
  dbo.SP_Roll_ITEM_Stock, but one row per roll instead of a summed total.

  A roll is included when:
    - it is an approved Loom roll in vw_Prod_BeforeRMD
    - it was produced on or before @AsOnDate
    - pending weight is above zero
    - it sits in a roll godown
    - it was not moved out to Despatch, Stock Adjustment, Cutting,
      FIBC Bag, or Small Bag on or before @AsOnDate

  NetWt is fPendingQty, the weight left on the roll now.
  For a past date the roll is included or excluded by date, but the weight
  is still today's pending weight.

  Does not INSERT, UPDATE, or DELETE any ERP table.
  @ItemCode NULL returns every roll item for the company and is slow.

  Example:
    EXEC dbo.usp_RollStockAsOn
         @CompanyName = N'HCP Plastene Bulkpack Ltd',
         @AsOnDate    = '2026-10-06',
         @ItemCode    = N'WIP00023';
*/
CREATE OR ALTER PROCEDURE dbo.usp_RollStockAsOn
    @CompanyName varchar(150),
    @AsOnDate    date,
    @ItemCode    varchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Company varchar(150) = LTRIM(RTRIM(@CompanyName));
    DECLARE @Item varchar(50) = NULLIF(LTRIM(RTRIM(@ItemCode)), '');
    DECLARE @AsOnEnd datetime = DATEADD(day, 1, CAST(@AsOnDate AS datetime));

    ;WITH rolls AS (
        SELECT
            ISNULL(v.CuttingRollNo, v.RollNo) AS RollNo,
            v.fPendingQty AS NetWt,
            v.FGItemCode,
            v.FGItemname,
            v.Companyname,
            v.vToGodown,
            CASE WHEN v.vToGodown IS NULL THEN v.Location ELSE v.vToGodown END AS Godown,
            v.Sysdate
        FROM vw_Prod_BeforeRMD v WITH (NOLOCK)
        WHERE v.Sysdate >= '2000-01-01'
          AND v.Sysdate < @AsOnEnd
          AND v.Companyname = @Company
          AND (@Item IS NULL OR v.FGItemCode = @Item)
          AND (
                CASE WHEN v.vToGodown IS NULL THEN v.Location ELSE v.vToGodown END
              ) IN (
                'Lamination Godown',
                'Liner Bag Godown',
                'Loom Godown',
                'Needle Loom Godown',
                'Roll Material Godown'
              )
          AND v.status = 'Loom'
          AND (v.CuttingRollNo LIKE '%CUT' OR v.CuttingRollNo IS NULL)
          AND v.vStatus = 'Approved'
          AND ROUND(v.fPendingQty, 2) > 0
    )
    SELECT
        r.Companyname,
        r.vToGodown,
        r.Godown,
        r.FGItemCode,
        r.FGItemname,
        r.RollNo,
        CAST(r.NetWt AS decimal(18, 3)) AS NetWt,
        r.Sysdate,
        @AsOnDate AS AsOnDate
    FROM rolls r
    WHERE NOT EXISTS (
        SELECT 1
        FROM prod_rmd_inout x WITH (NOLOCK)
        WHERE x.vtype = 'Loom'
          AND x.vToGodown IN (
                'Despatch Godown',
                'Stock Adjustment Entry',
                'Cutting Godown',
                'FIBC Bag Godown',
                'Small Bag Godown'
              )
          AND x.vItemCode = REPLACE(r.RollNo, 'CUT', '')
          AND (x.dSysdate IS NULL OR x.dSysdate < @AsOnEnd)
    )
    ORDER BY r.Companyname, r.vToGodown, r.FGItemCode, r.RollNo;
END
GO
