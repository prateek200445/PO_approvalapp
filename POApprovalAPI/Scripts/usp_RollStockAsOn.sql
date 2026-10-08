/*
  Read-only roll list for one company and item on one date.

  A roll is included when it was produced on or before @AsOnDate and it had
  not yet moved from a roll godown to despatch, cutting, or a bag godown.
  The current godown is not used, because a later despatch changes it and
  would hide the roll on an earlier date.

  NetWt is fPendingQty. Despatch does not reduce that weight.
  The total matches the closing of dbo.usp_ItemStockSummary for a roll item
  when @AsOnDate is that summary's end date.

  Does not INSERT, UPDATE, or DELETE any ERP table.

  Example:
    EXEC dbo.usp_RollStockAsOn
         @CompanyName = N'HCP Plastene Bulkpack Ltd',
         @AsOnDate    = '2026-09-30',
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
      AND ROUND(v.fPendingQty, 2) > 0;

    CREATE TABLE #out (
        RollKey    varchar(100) COLLATE DATABASE_DEFAULT NOT NULL,
        FirstOut   datetime,
        FromGodown varchar(150)
    );

    INSERT #out (RollKey, FirstOut, FromGodown)
    SELECT d.RollKey, d.dSysdate, d.vFromGodown
    FROM (
        SELECT
            x.vItemCode COLLATE DATABASE_DEFAULT AS RollKey,
            x.dSysdate,
            x.vFromGodown,
            ROW_NUMBER() OVER (
                PARTITION BY x.vItemCode
                ORDER BY x.dSysdate, x.iSrNo
            ) AS rn
        FROM prod_rmd_inout x WITH (NOLOCK)
        INNER JOIN #rolls r
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
    ) d
    WHERE d.rn = 1;

    SELECT
        r.Companyname,
        CASE
            WHEN o.FirstOut >= @AsOnEnd THEN o.FromGodown
            ELSE r.Godown
        END AS vToGodown,
        CASE
            WHEN o.FirstOut >= @AsOnEnd THEN o.FromGodown
            ELSE r.Godown
        END AS Godown,
        r.FGItemCode,
        r.FGItemname,
        r.RollNo,
        r.NetWt,
        r.Sysdate,
        @AsOnDate AS AsOnDate
    FROM #rolls r
    LEFT JOIN #out o ON o.RollKey = r.RollKey
    WHERE o.FirstOut IS NULL
       OR o.FirstOut >= @AsOnEnd
    ORDER BY r.Companyname, r.vToGodown, r.FGItemCode, r.RollNo;
END
GO
