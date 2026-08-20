-- Plastene Unit-II demo data assessment
DECLARE @Co NVARCHAR(200) = N'Plastene India Limited (Unit -II)';

PRINT '=== FACTORY / SETUP (MaterialProcessing) ===';
IF OBJECT_ID('dbo.PlanningFactoryConfig') IS NOT NULL
    SELECT * FROM PlanningFactoryConfig WHERE CompanyName = @Co;
ELSE PRINT 'PlanningFactoryConfig: missing';

IF OBJECT_ID('dbo.PlanningLineConfig') IS NOT NULL
    SELECT COUNT(*) AS LineConfigRows FROM PlanningLineConfig WHERE CompanyName = @Co;
ELSE PRINT 'PlanningLineConfig: missing';

IF OBJECT_ID('dbo.PlanningLoomPool') IS NOT NULL
    SELECT PoolPurpose, IncludeInPlanning, COUNT(*) AS Cnt
    FROM PlanningLoomPool WHERE CompanyName = @Co
    GROUP BY PoolPurpose, IncludeInPlanning;
ELSE PRINT 'PlanningLoomPool: missing';

IF OBJECT_ID('dbo.PlanningBacklog') IS NOT NULL
    SELECT TOP 10 * FROM PlanningBacklog WHERE CompanyName = @Co AND Status = 'Open' ORDER BY CreatedAt DESC;
ELSE PRINT 'PlanningBacklog: missing';

IF OBJECT_ID('dbo.FibcQuotationHold') IS NOT NULL
    SELECT Status, COUNT(*) AS Cnt FROM FibcQuotationHold GROUP BY Status;
ELSE PRINT 'FibcQuotationHold: missing';

PRINT '=== FIBC LINES (ERP) ===';
SELECT TOP 15 LNo, BagType, Bagcapacity, SOrderno, NoOfDaysChk
FROM NewLineMaster WITH (NOLOCK)
WHERE CompanyName = @Co ORDER BY LNo;

PRINT '=== RECENT FIBC ALLOCATIONS ===';
SELECT TOP 15 PONO, COUNT(*) AS Rows, MIN(AllocationDate) AS FromDt, MAX(AllocationDate) AS ToDt
FROM prod_fibcallocationMaster WITH (NOLOCK)
WHERE CompanyName = @Co AND (isActive IS NULL OR UPPER(LTRIM(RTRIM(isActive))) <> 'N')
GROUP BY PONO ORDER BY MAX(AllocationDate) DESC;

PRINT '=== LOOM MASTER ===';
SELECT COUNT(*) AS ActiveLooms FROM NewMISLoomMaster WITH (NOLOCK)
WHERE CompanyName = @Co AND (isFreeze IS NULL OR isFreeze = 0 OR UPPER(LTRIM(RTRIM(isFreeze))) = 'N');

PRINT '=== RECENT LOOM ALLOCATIONS ===';
SELECT TOP 15 PONO, COUNT(*) AS Rows, MIN(AllocationDate) AS FromDt, MAX(ToDate) AS ToDt
FROM Prod_LoomAlocationMaster WITH (NOLOCK)
WHERE PONO IS NOT NULL AND LTRIM(PONO) <> ''
  AND (isActive IS NULL OR UPPER(LTRIM(RTRIM(isActive))) <> 'N')
GROUP BY PONO ORDER BY MAX(AllocationDate) DESC;

PRINT '=== MARKETING ORDERS (Despatch) ===';
SELECT TOP 15 BuyerOrderNo, BuyerName, DespatchDate, TotalQty, TypeofBag
FROM Despatch.dbo.MarketingInvoice WITH (NOLOCK)
WHERE BuyerOrderNo IS NOT NULL AND LTRIM(BuyerOrderNo) <> ''
ORDER BY DespatchDate DESC;

PRINT '=== BOM FABRIC (production) sample orders ===';
SELECT TOP 10 FilePONo, Customer, Heading, GSM, FabricSize, TotalMtr, Targetdate
FROM production.dbo.Vw_Bom_PPC WITH (NOLOCK)
WHERE FilePONo IS NOT NULL AND LTRIM(FilePONo) <> '' AND TotalMtr > 0
ORDER BY Targetdate DESC;
