/*
  Read-only roll list for one company and item, using the same rules as
  dbo.SP_Roll_ITEM_Stock, but one row per roll instead of a summed total.

  Rolls for the item are collected first. Moved-out roll numbers are then
  matched in one set, not once per row of the view.

  NetWt is fPendingQty, the weight left on the roll now.
  Does not INSERT, UPDATE, or DELETE any ERP table.

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

    IF @Item IS NULL
    BEGIN
        RAISERROR('Pass an item code.', 16, 1);
        RETURN;
    END;

    CREATE TABLE #rolls (
        Companyname varchar(150),
        vToGodown   varchar(150),
        Godown      varchar(150),
        FGItemCode  varchar(50),
        FGItemname  varchar(200),
        RollNo      varchar(100),
        RollKey     varchar(100) COLLATE DATABASE_DEFAULT,
        NetWt       decimal(18, 3),
        Sysdate     datetime
    );

    INSERT #rolls (
        Companyname, vToGodown, Godown, FGItemCode, FGItemname,
        RollNo, RollKey, NetWt, Sysdate
    )
    SELECT
        v.Companyname,
        v.vToGodown,
        CASE WHEN v.vToGodown IS NULL THEN v.Location ELSE v.vToGodown END,
        v.FGItemCode,
        v.FGItemname,
        ISNULL(v.CuttingRollNo, v.RollNo),
        REPLACE(ISNULL(v.CuttingRollNo, v.RollNo), 'CUT', '') COLLATE DATABASE_DEFAULT,
        CAST(v.fPendingQty AS decimal(18, 3)),
        v.Sysdate
    FROM vw_Prod_BeforeRMD v WITH (NOLOCK)
    WHERE v.Companyname = @Company
      AND v.FGItemCode = @Item
      AND v.Sysdate >= '2000-01-01'
      AND v.Sysdate < @AsOnEnd
      AND v.status = 'Loom'
      AND (v.CuttingRollNo LIKE '%CUT' OR v.CuttingRollNo IS NULL)
      AND v.vStatus = 'Approved'
      AND ROUND(v.fPendingQty, 2) > 0
      AND (
            CASE WHEN v.vToGodown IS NULL THEN v.Location ELSE v.vToGodown END
          ) IN (
            'Lamination Godown',
            'Liner Bag Godown',
            'Loom Godown',
            'Needle Loom Godown',
            'Roll Material Godown'
          );

    CREATE TABLE #moved (
        vItemCode varchar(100) COLLATE DATABASE_DEFAULT NOT NULL
    );

    INSERT #moved (vItemCode)
    SELECT DISTINCT x.vItemCode COLLATE DATABASE_DEFAULT
    FROM prod_rmd_inout x WITH (NOLOCK)
    INNER JOIN #rolls r
        ON r.RollKey = x.vItemCode COLLATE DATABASE_DEFAULT
    WHERE x.vtype = 'Loom'
      AND x.vToGodown IN (
            'Despatch Godown',
            'Stock Adjustment Entry',
            'Cutting Godown',
            'FIBC Bag Godown',
            'Small Bag Godown'
          )
      AND (x.dSysdate IS NULL OR x.dSysdate < @AsOnEnd);

    SELECT
        r.Companyname,
        r.vToGodown,
        r.Godown,
        r.FGItemCode,
        r.FGItemname,
        r.RollNo,
        r.NetWt,
        r.Sysdate,
        @AsOnDate AS AsOnDate
    FROM #rolls r
    WHERE NOT EXISTS (
        SELECT 1 FROM #moved m WHERE m.vItemCode = r.RollKey
    )
    ORDER BY r.Companyname, r.vToGodown, r.FGItemCode, r.RollNo;
END
GO
