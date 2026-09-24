/* Generated from july 26 / aug 26 factory files + public templates already in stored form.
   Stock: PnlStockValue AmountLacs, PlantName NULL, months 2026-07-01 and 2026-08-01 only.
   Provisions: rupees + EntryType. DELETE/INSERT those two months only. Does not touch Apr-Jun.
   Gaps not loaded: KPW-2 / VAD / HO / HPBL-3 stock; KPW/OEL/HPBL123 stock (qty movement only);
   PIL-2 stock skipped (movement .xls is qty; stors sheet is FY 23-24). PPL Aug packing from STORE REPORT.
   Review CompanyName against FactoryInfo before COMMIT.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

IF OBJECT_ID(N'dbo.PnlStockValue', N'U') IS NULL
BEGIN
    RAISERROR(N'PnlStockValue is missing.', 16, 1);
    RETURN;
END
IF OBJECT_ID(N'dbo.Provisioning', N'U') IS NULL
BEGIN
    RAISERROR(N'Provisioning is missing.', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 612.580000, UploadedAt = GETDATE(), Remarks = N'public/hplb_4_stock.xlsx JUL-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-07-01', N'FinishedGoods', 612.580000, N'public/hplb_4_stock.xlsx JUL-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-07-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 16.490000, UploadedAt = GETDATE(), Remarks = N'public/hplb_4_stock.xlsx JUL-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-07-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-07-01', N'Packing', 16.490000, N'public/hplb_4_stock.xlsx JUL-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 345.610000, UploadedAt = GETDATE(), Remarks = N'public/hplb_4_stock.xlsx JUL-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-07-01', N'RawMaterial', 345.610000, N'public/hplb_4_stock.xlsx JUL-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 58.230000, UploadedAt = GETDATE(), Remarks = N'public/hplb_4_stock.xlsx JUL-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-07-01', N'Stores', 58.230000, N'public/hplb_4_stock.xlsx JUL-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.608600, UploadedAt = GETDATE(), Remarks = N'public/hplb_4_stock.xlsx JUL-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-07-01', N'WIP', 0.608600, N'public/hplb_4_stock.xlsx JUL-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1334.863362, UploadedAt = GETDATE(), Remarks = N'public/pil1 stock.xlsx July / 1e5'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-07-01', N'FinishedGoods', 1334.863362, N'public/pil1 stock.xlsx July / 1e5', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-07-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 10.232912, UploadedAt = GETDATE(), Remarks = N'public/pil1 stock.xlsx July / 1e5'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-07-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-07-01', N'Packing', 10.232912, N'public/pil1 stock.xlsx July / 1e5', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 2455.582573, UploadedAt = GETDATE(), Remarks = N'public/pil1 stock.xlsx July / 1e5'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-07-01', N'RawMaterial', 2455.582573, N'public/pil1 stock.xlsx July / 1e5', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 136.566420, UploadedAt = GETDATE(), Remarks = N'public/pil1 stock.xlsx July / 1e5'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-07-01', N'Stores', 136.566420, N'public/pil1 stock.xlsx July / 1e5', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 55.978576, UploadedAt = GETDATE(), Remarks = N'public/pil1 stock.xlsx July / 1e5'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-07-01', N'WIP', 55.978576, N'public/pil1 stock.xlsx July / 1e5', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 936.845147, UploadedAt = GETDATE(), Remarks = N'public/ppl_stock.xlsx July'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-07-01', N'FinishedGoods', 936.845147, N'public/ppl_stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-07-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 12.854954, UploadedAt = GETDATE(), Remarks = N'public/ppl_stock.xlsx July'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-07-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-07-01', N'Packing', 12.854954, N'public/ppl_stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 145.388855, UploadedAt = GETDATE(), Remarks = N'public/ppl_stock.xlsx July'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-07-01', N'RawMaterial', 145.388855, N'public/ppl_stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 90.040914, UploadedAt = GETDATE(), Remarks = N'public/ppl_stock.xlsx July'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-07-01', N'Stores', 90.040914, N'public/ppl_stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 204.148277, UploadedAt = GETDATE(), Remarks = N'public/ppl_stock.xlsx July'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-07-01', N'WIP', 204.148277, N'public/ppl_stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 802.120000, UploadedAt = GETDATE(), Remarks = N'public/hplb_4_stock.xlsx AUG-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-08-01', N'FinishedGoods', 802.120000, N'public/hplb_4_stock.xlsx AUG-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-08-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 7.310000, UploadedAt = GETDATE(), Remarks = N'public/hplb_4_stock.xlsx AUG-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-08-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-08-01', N'Packing', 7.310000, N'public/hplb_4_stock.xlsx AUG-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 364.230000, UploadedAt = GETDATE(), Remarks = N'public/hplb_4_stock.xlsx AUG-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-08-01', N'RawMaterial', 364.230000, N'public/hplb_4_stock.xlsx AUG-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 61.750000, UploadedAt = GETDATE(), Remarks = N'public/hplb_4_stock.xlsx AUG-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-08-01', N'Stores', 61.750000, N'public/hplb_4_stock.xlsx AUG-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.557390, UploadedAt = GETDATE(), Remarks = N'public/hplb_4_stock.xlsx AUG-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-08-01', N'WIP', 0.557390, N'public/hplb_4_stock.xlsx AUG-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1424.746236, UploadedAt = GETDATE(), Remarks = N'aug 26 PIL-1 stock template / 1e5'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-08-01', N'FinishedGoods', 1424.746236, N'aug 26 PIL-1 stock template / 1e5', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-08-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 6.069953, UploadedAt = GETDATE(), Remarks = N'aug 26 PIL-1 stock template / 1e5'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-08-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-08-01', N'Packing', 6.069953, N'aug 26 PIL-1 stock template / 1e5', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1345.530298, UploadedAt = GETDATE(), Remarks = N'aug 26 PIL-1 stock template / 1e5'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-08-01', N'RawMaterial', 1345.530298, N'aug 26 PIL-1 stock template / 1e5', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 143.119076, UploadedAt = GETDATE(), Remarks = N'aug 26 PIL-1 stock template / 1e5'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-08-01', N'Stores', 143.119076, N'aug 26 PIL-1 stock template / 1e5', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 62.889359, UploadedAt = GETDATE(), Remarks = N'aug 26 PIL-1 stock template / 1e5'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-08-01', N'WIP', 62.889359, N'aug 26 PIL-1 stock template / 1e5', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1029.895675, UploadedAt = GETDATE(), Remarks = N'PPL Aug valuation FG+SF'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-08-01', N'FinishedGoods', 1029.895675, N'PPL Aug valuation FG+SF', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-08-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 23.461916, UploadedAt = GETDATE(), Remarks = N'PPL Aug valuation STORE REPORT Packing Material'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-08-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-08-01', N'Packing', 23.461916, N'PPL Aug valuation STORE REPORT Packing Material', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 129.476145, UploadedAt = GETDATE(), Remarks = N'PPL Aug valuation'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-08-01', N'RawMaterial', 129.476145, N'PPL Aug valuation', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 88.896822, UploadedAt = GETDATE(), Remarks = N'PPL Aug valuation STORE & SPARE'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-08-01', N'Stores', 88.896822, N'PPL Aug valuation STORE & SPARE', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 153.647645, UploadedAt = GETDATE(), Remarks = N'PPL Aug valuation #N/A'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-08-01', N'WIP', 153.647645, N'PPL Aug valuation #N/A', GETDATE());

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd' AND sysdate >= '2026-07-01' AND sysdate < DATEADD(month, 1, '2026-07-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Canteen Expense - Factory', 190000.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Conveyance Expenses', 6409.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Employers Contribution to ESI', 11000.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Freight Inward Expense (RCM)', 96478.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Freight Outward Expense (RCM)', 423700.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Freight Outward Expense (With GST)', 2265000.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Internal Transportation Expense / Reimbursement', 26042.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Job Charges', 29441113.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Labour Charges', 3362356.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Loading & Unloading Expense', 5970.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Medical Expense', 20820.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'P.F. Contribution - Employer', 140000.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Postage & Courier Expense', 638.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Power Expense', 2394917.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Printing & Stationery Expense', 759630.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Professional & Consultancy Fees', 831485.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Repairs & Maintenance Exp - Plant & Machinery', 350822.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Salaries Expense', 300000.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Security Expenses', 123500.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-07-01', N'Staff Welfare Expense', 25670.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-1', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND sysdate >= '2026-07-01' AND sysdate < DATEADD(month, 1, '2026-07-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-07-01', N'Conveyance Expenses', 2000.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-07-01', N'Employers Contribution to ESI', 1000.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-07-01', N'Internal Transportation Expense / Reimbursement', 8057.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-07-01', N'Labour Charges', 45365.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-07-01', N'P.F. Contribution - Employer', 6000.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-07-01', N'Postage & Courier Expense', 9175.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-07-01', N'Professional & Consultancy Fees', 50000.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-07-01', N'Salaries Expense', 50000.0000, N'july 26/hpbl123/prov hpbl 123 jul 26.xlsx unit-2', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND sysdate >= '2026-07-01' AND sysdate < DATEADD(month, 1, '2026-07-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-07-01', N'Advocate Fees Expense', 7500.0000, N'public/hplb_4_prov.xlsx JUL-26 * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-07-01', N'C & F  Charges - Export', 595000.0000, N'public/hplb_4_prov.xlsx JUL-26 * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-07-01', N'Freight Outward Expense (With GST)', 7514000.0000, N'public/hplb_4_prov.xlsx JUL-26 * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-07-01', N'Freight outward Expenses (RCM_Withinstate)', 21600.0000, N'public/hplb_4_prov.xlsx JUL-26 * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-07-01', N'Incentive Expense - Production', 11000.0000, N'public/hplb_4_prov.xlsx JUL-26 * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-07-01', N'Labour Charges', 8568000.0000, N'public/hplb_4_prov.xlsx JUL-26 * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-07-01', N'Medical Expense', 15892.0000, N'public/hplb_4_prov.xlsx JUL-26 * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-07-01', N'Mobile & Telephone Expense', 3499.0000, N'public/hplb_4_prov.xlsx JUL-26 * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-07-01', N'P.F. Contribution - Employer', 43337.0000, N'public/hplb_4_prov.xlsx JUL-26 * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-07-01', N'Repairs & Maintenance Exp - Computer & Printers', 7050.0000, N'public/hplb_4_prov.xlsx JUL-26 * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-07-01', N'Testing Charges', 81200.0000, N'public/hplb_4_prov.xlsx JUL-26 * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-07-01', N'Water Charges', 131000.0000, N'public/hplb_4_prov.xlsx JUL-26 * 1e5', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED' AND sysdate >= '2026-07-01' AND sysdate < DATEADD(month, 1, '2026-07-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'C & F  Charges - Export', 1178682.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Canteen Expense - Factory', 320910.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Equipment Certification Charges', 31748.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Factory Expense', 6200.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Freight Outward Expense (RCM)', 336000.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Freight Outward Expense (With GST)', 345940.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'House Keeping Expense', 467310.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Job Charges', 228490.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Labour Charges', 7315182.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Medical Expense', 176812.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Power Expense', 4130997.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Professional & Consultancy Fees', 639867.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Rent Expense - Colony', 1313300.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Repairs & Maintenance Exp - Electric', 645623.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Salaries Expense', 17687654.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Security Expenses', 324466.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Vehicle Hire Charges - RCM', 75500.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Vehicle Hire Charges - With Gst', 160000.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Water Charges', 184784.0000, N'july 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Xerox Expense', 4500.0000, N'july 26 kpw1 provisions', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND sysdate >= '2026-07-01' AND sysdate < DATEADD(month, 1, '2026-07-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-07-01', N'C & F  Charges - Export', 550000.0000, N'july 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-07-01', N'C & F Charges - Detention Outward', 15000.0000, N'july 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-07-01', N'Fumigation Charges', 9500.0000, N'july 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-07-01', N'Insurance Exp - Vehicles', -37828.0000, N'july 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-07-01', N'Mobile & Telephone Expense', 707.0000, N'july 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-07-01', N'Rent Expense - Factory', -352971.0000, N'july 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-07-01', N'Salaries Expense', 93000.0000, N'july 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-07-01', N'Security Expenses', 34000.0000, N'july 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-07-01', N'Water Charges', 3350.0000, N'july 26 kpw3 provisions', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Oswal Extrusion Limited' AND sysdate >= '2026-07-01' AND sysdate < DATEADD(month, 1, '2026-07-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Advocate Fees Expense', 7500.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Bonus Expense', 1100000.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Canteen Expense - Factory', 60000.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Equipment Certification Charges', -6600.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Fees & Taxes', -5310.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Freight Outward Expense (RCM)', 150000.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Garden Mainteance Exp', 5000.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Insurance Expenses - Other', -45650.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Internal Transportation Expense / Reimbursement', 248000.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Job Charges', 35000.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Job Work Income', -15234502.4672, N'july 26 OEL provisions', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Loading & Unloading Expense', 35000.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Membership & Subscription Fees', -9001.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Mobile & Telephone Expense', 25000.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'P.F. Contribution - Employer', 154220.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Postage & Courier Expense', 15000.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Power Expense', 3976999.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Printing & Stationery Expense', 15000.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Professional & Consultancy Fees', 287694.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Rent Expense - Factory', -385188.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Repairs & Maintenance Exp - Computer & Printers', 38000.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Salaries Expense', 4095228.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Security Expenses', 200000.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Staff Welfare Expense', -19140.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Vehicle Hire Charges - RCM', 368030.0000, N'july 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-07-01', N'Water Charges', 235966.0000, N'july 26 OEL provisions', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited' AND sysdate >= '2026-07-01' AND sysdate < DATEADD(month, 1, '2026-07-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-07-01', N'C & F  Charges - Export', 918254.4600, N'july 26/pil1/Provision July-26.xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-07-01', N'Canteen Expense - Factory', 167694.0000, N'july 26/pil1/Provision July-26.xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-07-01', N'Freight outward Expenses (RCM_Withinstate)', 500000.0000, N'july 26/pil1/Provision July-26.xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-07-01', N'Income From Wind Mill', -2785184.0000, N'july 26/pil1/Provision July-26.xlsx', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-07-01', N'Labour Charges', 10162081.0000, N'july 26/pil1/Provision July-26.xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-07-01', N'Power Expense', 11453336.0000, N'july 26/pil1/Provision July-26.xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-07-01', N'Professional & Consultancy Fees', 125000.0000, N'july 26/pil1/Provision July-26.xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-07-01', N'Salaries Expense', 6678605.0000, N'july 26/pil1/Provision July-26.xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-07-01', N'Security Expenses', 169177.0000, N'july 26/pil1/Provision July-26.xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-07-01', N'Staff Welfare Expense', 80000.0000, N'july 26/pil1/Provision July-26.xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-07-01', N'Vehicle Hire Charges - RCM', 23000.0000, N'july 26/pil1/Provision July-26.xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-07-01', N'Vehicle Hire Charges - With Gst', 90000.0000, N'july 26/pil1/Provision July-26.xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-07-01', N'Water Charges', 1265935.0000, N'july 26/pil1/Provision July-26.xlsx', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited (Unit -II)' AND sysdate >= '2026-07-01' AND sysdate < DATEADD(month, 1, '2026-07-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-07-01', N'Freight Outward Expense  - Export', 2000000.0000, N'PIL-2 TB JULY Provisin col', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-07-01', N'Freight Outward Expense (RCM)', 500000.0000, N'PIL-2 TB JULY Provisin col', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-07-01', N'Freight Outward Expense (With GST)', 500000.0000, N'PIL-2 TB JULY Provisin col', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-07-01', N'House Keeping Expense', 310000.0000, N'PIL-2 TB JULY Provisin col', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-07-01', N'Labour Charges', 11297259.0000, N'PIL-2 TB JULY Provisin col', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-07-01', N'Loading & Unloading Expense', 90972.0000, N'PIL-2 TB JULY Provisin col', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-07-01', N'Salaries Expense', 11158291.0000, N'PIL-2 TB JULY Provisin col', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-07-01', N'Security Expenses', 229451.0000, N'PIL-2 TB JULY Provisin col', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene Polyfilms Limited' AND sysdate >= '2026-07-01' AND sysdate < DATEADD(month, 1, '2026-07-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-07-01', N'Freight Outward Expense (With GST)', 225000.0000, N'public/ppl_prov.xlsx July * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-07-01', N'Job Work Income', -2488246.0000, N'public/ppl_prov.xlsx July * 1e5', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-07-01', N'Labour Charges', 11043677.0000, N'public/ppl_prov.xlsx July * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-07-01', N'Power Expense', 2506637.1300, N'public/ppl_prov.xlsx July * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-07-01', N'Salaries Expense', 2900000.0000, N'public/ppl_prov.xlsx July * 1e5', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd' AND sysdate >= '2026-08-01' AND sysdate < DATEADD(month, 1, '2026-08-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Canteen Expense - Factory', 223104.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Conveyance Expenses', 3334.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Employers Contribution to ESI', 18296.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Freight Inward Expense (RCM)', 63783.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Freight Outward Expense (RCM)', 415300.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Freight Outward Expense (With GST)', 3886324.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Internal Transportation Expense / Reimbursement', 22889.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Job Charges', 20156616.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Labour Charges', 3297476.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Loading & Unloading Expense', 3940.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Medical Expense', 22800.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'P.F. Contribution - Employer', 207488.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Postage & Courier Expense', 5373.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Power Expense', 9235435.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Printing & Stationery Expense', 9018.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Professional & Consultancy Fees', 642735.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Repairs & Maintenance Exp - Plant & Machinery', 121726.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Salaries Expense', 4148544.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Security Expenses', 124000.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-08-01', N'Staff Welfare Expense', 9855.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-1', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND sysdate >= '2026-08-01' AND sysdate < DATEADD(month, 1, '2026-08-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-08-01', N'Conveyance Expenses', 2000.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-08-01', N'Employers Contribution to ESI', 923.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-08-01', N'Internal Transportation Expense / Reimbursement', 5000.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-08-01', N'Job Charges', 28800.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-08-01', N'Labour Charges', 50506.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-08-01', N'P.F. Contribution - Employer', 6087.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-08-01', N'Postage & Courier Expense', 4665.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-08-01', N'Professional & Consultancy Fees', 50000.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-08-01', N'Salaries Expense', 394919.0000, N'aug 26/HPBL123/PROVISION AUG 26 H 123.xlsx unit-2', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND sysdate >= '2026-08-01' AND sysdate < DATEADD(month, 1, '2026-08-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'Advocate Fees Expense', 7500.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'C & F  Charges - Export', 364000.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'Freight Outward Expense (With GST)', 588000.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'Fumigation Charges', 13000.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'House Keeping Expense', 63200.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'Incentive Expense - Production', 11000.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'Labour Charges', 9262000.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'Medical Expense', 3680.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'Mobile & Telephone Expense', 3500.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'P.F. Admin. Charge', 3400.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'P.F. Contribution - Employer', 41000.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'Postage & Courier Expense', 81000.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'Repairs & Maintenance Exp - Plant & Machinery', 29000.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'Testing Charges', 81000.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'Vehicle Hire Charges - RCM', 180000.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'Water Charges', 117000.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-08-01', N'Weighment Expense', 7000.0000, N'aug 26 HPBL4 provision template AUG * 1e5', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED' AND sysdate >= '2026-08-01' AND sysdate < DATEADD(month, 1, '2026-08-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'C & F  Charges - Export', 419308.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Canteen Expense - Factory', 376290.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Factory Expense', 5000.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Freight Outward Expense (RCM)', 500000.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Freight Outward Expense (With GST)', 3236167.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Fumigation Charges', 26550.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'House Keeping Expense', 554014.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Job Charges', 207763.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Labour Charges', 8112264.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Medical Expense', 34992.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Power Expense', 4320121.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Professional & Consultancy Fees', 310000.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Rent Expense - Colony', 1321300.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Repairs & Maintenance Exp - Electric', 164138.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Salaries Expense', 21239669.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Security Expenses', 415483.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Vehicle Hire Charges - RCM', 75500.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Vehicle Hire Charges - With Gst', 160000.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Water Charges', 94105.0000, N'aug 26 kpw1 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Xerox Expense', 4500.0000, N'aug 26 kpw1 provisions', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND sysdate >= '2026-08-01' AND sysdate < DATEADD(month, 1, '2026-08-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-08-01', N'C & F  Charges - Export', 450000.0000, N'aug 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-08-01', N'C & F Charges - Detention Outward', 25000.0000, N'aug 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-08-01', N'Freight Outward Expense  - Export', 156000.0000, N'aug 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-08-01', N'Fumigation Charges', 8000.0000, N'aug 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-08-01', N'Insurance Exp - Vehicles', -33003.0000, N'aug 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-08-01', N'Mobile & Telephone Expense', 707.0000, N'aug 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-08-01', N'Rent Expense - Factory', -202885.0000, N'aug 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-08-01', N'Salaries Expense', 95000.0000, N'aug 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-08-01', N'Security Expenses', 38000.0000, N'aug 26 kpw3 provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-08-01', N'Water Charges', 250.0000, N'aug 26 kpw3 provisions', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Oswal Extrusion Limited' AND sysdate >= '2026-08-01' AND sysdate < DATEADD(month, 1, '2026-08-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Bonus Expense', 1400000.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'C & F  Charges  - Import', 326394.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Canteen Expense - Factory', 45000.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Equipment Certification Charges', -5775.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Factory Expense', 35000.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Fees & Taxes', -4130.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Freight Inward Expense (With GST)', 30150.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Garden Mainteance Exp', 5000.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Internal Transportation Expense / Reimbursement', 201000.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Job Work Income', -13750714.7985, N'aug 26 OEL provisions', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Labour Charges', 14759734.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Loading & Unloading Expense', 135000.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Membership & Subscription Fees', -7191.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Mobile & Telephone Expense', 28000.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'P.F. Contribution - Employer', 149468.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Postage & Courier Expense', 35000.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Power Expense', 2500000.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Printing & Stationery Expense', 9000.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Professional & Consultancy Fees', 30000.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Rent Expense - Factory', -285314.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Repairs & Maintenance Exp - Computer & Printers', 39800.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Salaries Expense', 4143931.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Security Expenses', 200000.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Staff Welfare Expense', -18480.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Vehicle Hire Charges - RCM', 330000.0000, N'aug 26 OEL provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-08-01', N'Water Charges', 240000.0000, N'aug 26 OEL provisions', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited' AND sysdate >= '2026-08-01' AND sysdate < DATEADD(month, 1, '2026-08-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-08-01', N'C & F  Charges - Export', 2244723.0000, N'aug 26 PIL-1 provision template (rupees)', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-08-01', N'Canteen Expense - Factory', 168000.0000, N'aug 26 PIL-1 provision template (rupees)', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-08-01', N'Freight outward Expenses (RCM_Withinstate)', 500000.0000, N'aug 26 PIL-1 provision template (rupees)', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-08-01', N'Income From Wind Mill', -1785696.0000, N'aug 26 PIL-1 provision template (rupees)', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-08-01', N'Labour Charges', 10881573.0000, N'aug 26 PIL-1 provision template (rupees)', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-08-01', N'Power Expense', 11080418.0000, N'aug 26 PIL-1 provision template (rupees)', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-08-01', N'Professional & Consultancy Fees', 128000.0000, N'aug 26 PIL-1 provision template (rupees)', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-08-01', N'Salaries Expense', 7087849.0000, N'aug 26 PIL-1 provision template (rupees)', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-08-01', N'Security Expenses', 175919.0000, N'aug 26 PIL-1 provision template (rupees)', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-08-01', N'Staff Welfare Expense', 84005.0000, N'aug 26 PIL-1 provision template (rupees)', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-08-01', N'Vehicle Hire Charges - RCM', 23000.0000, N'aug 26 PIL-1 provision template (rupees)', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-08-01', N'Vehicle Hire Charges - With Gst', 90000.0000, N'aug 26 PIL-1 provision template (rupees)', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-08-01', N'Water Charges', 1200025.0000, N'aug 26 PIL-1 provision template (rupees)', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited (Unit -II)' AND sysdate >= '2026-08-01' AND sysdate < DATEADD(month, 1, '2026-08-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-08-01', N'Freight Outward Expense (With GST)', 1000000.0000, N'PIL-2 TB AUG Provisin col', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-08-01', N'House Keeping Expense', 310000.0000, N'PIL-2 TB AUG Provisin col', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-08-01', N'Labour Charges', 12681078.0000, N'PIL-2 TB AUG Provisin col', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-08-01', N'Loading & Unloading Expense', 104888.0000, N'PIL-2 TB AUG Provisin col', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-08-01', N'Salaries Expense', 11769965.0000, N'PIL-2 TB AUG Provisin col', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-08-01', N'Security Expenses', 241548.0000, N'PIL-2 TB AUG Provisin col', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene Polyfilms Limited' AND sysdate >= '2026-08-01' AND sysdate < DATEADD(month, 1, '2026-08-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-08-01', N'Freight Outward Expense (With GST)', 210000.0000, N'PPL Aug PROVISION DATA col D', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-08-01', N'Job Work Income', -7149760.2000, N'PPL Aug PROVISION DATA col D', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-08-01', N'Labour Charges', 11902898.0000, N'PPL Aug PROVISION DATA col D', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-08-01', N'Power Expense', 2300000.0000, N'PPL Aug PROVISION DATA col D', N'Expense');

COMMIT;
-- ROLLBACK;
