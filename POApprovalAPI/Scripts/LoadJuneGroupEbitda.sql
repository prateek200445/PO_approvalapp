/* Generated from public/june 26 Group EBIDTA.xlsx
   Stock sheet: op -> 2026-03-01, then Apr/May/Jun closings.
   Provisions: 04/05/06.conso TB section A='Provisions'. Amounts in rupees.
   Review CompanyName against FactoryInfo before COMMIT.
   Does not load July+.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

IF OBJECT_ID(N'dbo.PnlStockValue', N'U') IS NULL
BEGIN
    RAISERROR(N'PnlStockValue is missing.', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1183.630759, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-03-01', N'RawMaterial', 1183.630759, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1596.097255, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-04-01', N'RawMaterial', 1596.097255, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1978.285243, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-05-01', N'RawMaterial', 1978.285243, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 3073.726310, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-06-01', N'RawMaterial', 3073.726310, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 777.074404, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-03-01', N'RawMaterial', 777.074404, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 785.184809, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-04-01', N'RawMaterial', 785.184809, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 861.269450, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-05-01', N'RawMaterial', 861.269450, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1136.653589, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-06-01', N'RawMaterial', 1136.653589, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 311.597702, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-03-01', N'RawMaterial', 311.597702, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 391.802787, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-04-01', N'RawMaterial', 391.802787, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 233.139468, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-05-01', N'RawMaterial', 233.139468, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 312.383596, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-06-01', N'RawMaterial', 312.383596, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 784.091060, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-03-01', N'RawMaterial', 784.091060, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 742.482353, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-04-01', N'RawMaterial', 742.482353, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 533.047663, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-05-01', N'RawMaterial', 533.047663, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 123.059340, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-06-01', N'RawMaterial', 123.059340, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-03-01', N'RawMaterial', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-04-01', N'RawMaterial', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-05-01', N'RawMaterial', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-06-01', N'RawMaterial', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 238.546514, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-03-01', N'RawMaterial', 238.546514, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 390.229328, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-04-01', N'RawMaterial', 390.229328, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 461.249774, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-05-01', N'RawMaterial', 461.249774, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 413.333898, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-06-01', N'RawMaterial', 413.333898, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 168.895890, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-03-01', N'RawMaterial', 168.895890, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 802.992420, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-04-01', N'RawMaterial', 802.992420, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1375.636960, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-05-01', N'RawMaterial', 1375.636960, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 756.211460, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-06-01', N'RawMaterial', 756.211460, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 26.899460, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-03-01', N'RawMaterial', 26.899460, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 24.652910, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-04-01', N'RawMaterial', 24.652910, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 21.918970, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-05-01', N'RawMaterial', 21.918970, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 21.918970, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-06-01', N'RawMaterial', 21.918970, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 22.118540, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-03-01', N'RawMaterial', 22.118540, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 22.118540, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-04-01', N'RawMaterial', 22.118540, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 22.118540, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-05-01', N'RawMaterial', 22.118540, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 22.118540, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-06-01', N'RawMaterial', 22.118540, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-03-01', N'RawMaterial', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-04-01', N'RawMaterial', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-05-01', N'RawMaterial', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-06-01', N'RawMaterial', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 78.787352, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-03-01', N'RawMaterial', 78.787352, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 163.548899, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-04-01', N'RawMaterial', 163.548899, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 209.311035, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-05-01', N'RawMaterial', 209.311035, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 363.796780, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-06-01', N'RawMaterial', 363.796780, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 21.170826, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-03-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-03-01', N'RawMaterial', 21.170826, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 83.560661, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-04-01', N'RawMaterial', 83.560661, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 25.641962, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-05-01', N'RawMaterial', 25.641962, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 3.155298, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-06-01', N'RawMaterial', 3.155298, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 7.828483, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-03-01', N'Packing', 7.828483, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 6.116738, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-04-01', N'Packing', 6.116738, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 7.900431, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-05-01', N'Packing', 7.900431, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 7.850989, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-06-01', N'Packing', 7.850989, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-03-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-04-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-05-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-06-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 12.590000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-03-01', N'Packing', 12.590000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 12.870000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-04-01', N'Packing', 12.870000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 10.490000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-05-01', N'Packing', 10.490000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 19.860000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-06-01', N'Packing', 19.860000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-03-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-04-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-05-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-06-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-03-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-04-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-05-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-06-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-03-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-04-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-05-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-06-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-03-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-04-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-05-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-06-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-03-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-04-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-05-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-06-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-03-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-04-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-05-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-06-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-03-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-04-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-05-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-06-01', N'Packing', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 13.241029, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-03-01', N'Packing', 13.241029, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 11.216303, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-04-01', N'Packing', 11.216303, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 10.169866, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-05-01', N'Packing', 10.169866, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 11.950915, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-06-01', N'Packing', 11.950915, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 17.464140, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-03-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-03-01', N'Packing', 17.464140, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 18.662770, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-04-01', N'Packing', 18.662770, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 20.156050, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-05-01', N'Packing', 20.156050, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 22.219970, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-06-01', N'Packing', 22.219970, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 45.182088, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-03-01', N'WIP', 45.182088, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 46.625737, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-04-01', N'WIP', 46.625737, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 72.624571, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-05-01', N'WIP', 72.624571, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 40.046965, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-06-01', N'WIP', 40.046965, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-03-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-04-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-05-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-06-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-03-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-04-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-05-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-06-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-03-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-04-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-05-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-06-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-03-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-04-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-05-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-06-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-03-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-04-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-05-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-06-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 424.600860, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-03-01', N'WIP', 424.600860, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 343.528280, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-04-01', N'WIP', 343.528280, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 409.091120, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-05-01', N'WIP', 409.091120, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 393.966040, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-06-01', N'WIP', 393.966040, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 7.628510, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-03-01', N'WIP', 7.628510, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 3.028690, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-04-01', N'WIP', 3.028690, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 3.126160, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-05-01', N'WIP', 3.126160, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 3.126160, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-06-01', N'WIP', 3.126160, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-03-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-04-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-05-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-06-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-03-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-04-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-05-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-06-01', N'WIP', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 126.951067, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-03-01', N'WIP', 126.951067, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 122.891592, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-04-01', N'WIP', 122.891592, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 150.875806, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-05-01', N'WIP', 150.875806, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 174.448977, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-06-01', N'WIP', 174.448977, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 54.616950, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-03-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-03-01', N'WIP', 54.616950, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 47.735998, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-04-01', N'WIP', 47.735998, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 32.399917, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-05-01', N'WIP', 32.399917, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 27.499215, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-06-01', N'WIP', 27.499215, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 822.786593, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-03-01', N'FinishedGoods', 822.786593, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 754.657252, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-04-01', N'FinishedGoods', 754.657252, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 894.488661, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-05-01', N'FinishedGoods', 894.488661, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1122.652399, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-06-01', N'FinishedGoods', 1122.652399, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1483.275653, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-03-01', N'FinishedGoods', 1483.275653, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1640.735491, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-04-01', N'FinishedGoods', 1640.735491, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1485.898968, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-05-01', N'FinishedGoods', 1485.898968, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1263.486353, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-06-01', N'FinishedGoods', 1263.486353, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 382.843017, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-03-01', N'FinishedGoods', 382.843017, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 538.070255, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-04-01', N'FinishedGoods', 538.070255, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 532.429335, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-05-01', N'FinishedGoods', 532.429335, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 830.260251, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-06-01', N'FinishedGoods', 830.260251, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 2064.470000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-03-01', N'FinishedGoods', 2064.470000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 2563.130377, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-04-01', N'FinishedGoods', 2563.130377, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 3062.033943, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-05-01', N'FinishedGoods', 3062.033943, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 3163.756699, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-06-01', N'FinishedGoods', 3163.756699, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-03-01', N'FinishedGoods', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-04-01', N'FinishedGoods', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-05-01', N'FinishedGoods', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-06-01', N'FinishedGoods', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 448.863241, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-03-01', N'FinishedGoods', 448.863241, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 522.619626, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-04-01', N'FinishedGoods', 522.619626, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 663.217999, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-05-01', N'FinishedGoods', 663.217999, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 530.026194, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-06-01', N'FinishedGoods', 530.026194, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 69.965830, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-03-01', N'FinishedGoods', 69.965830, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 42.715590, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-04-01', N'FinishedGoods', 42.715590, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 59.015250, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-05-01', N'FinishedGoods', 59.015250, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 187.595960, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-06-01', N'FinishedGoods', 187.595960, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 7.514720, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-03-01', N'FinishedGoods', 7.514720, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 2.244330, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-04-01', N'FinishedGoods', 2.244330, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 4.406040, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-05-01', N'FinishedGoods', 4.406040, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 4.406040, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-06-01', N'FinishedGoods', 4.406040, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-03-01', N'FinishedGoods', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-04-01', N'FinishedGoods', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-05-01', N'FinishedGoods', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-06-01', N'FinishedGoods', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-03-01', N'FinishedGoods', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-04-01', N'FinishedGoods', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-05-01', N'FinishedGoods', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-06-01', N'FinishedGoods', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 286.249725, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-03-01', N'FinishedGoods', 286.249725, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 387.571457, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-04-01', N'FinishedGoods', 387.571457, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 357.337178, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-05-01', N'FinishedGoods', 357.337178, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 805.855305, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-06-01', N'FinishedGoods', 805.855305, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 35.190206, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-03-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-03-01', N'FinishedGoods', 35.190206, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 17.895876, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-04-01', N'FinishedGoods', 17.895876, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 9.152000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-05-01', N'FinishedGoods', 9.152000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-06-01', N'FinishedGoods', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 122.379334, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-03-01', N'Stores', 122.379334, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 130.209286, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-04-01', N'Stores', 130.209286, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 131.362286, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-05-01', N'Stores', 131.362286, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 143.045145, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-06-01', N'Stores', 143.045145, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 120.167178, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-03-01', N'Stores', 120.167178, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 118.257907, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-04-01', N'Stores', 118.257907, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 119.493449, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-05-01', N'Stores', 119.493449, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 115.950857, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-06-01', N'Stores', 115.950857, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 46.180000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-03-01', N'Stores', 46.180000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 49.380000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-04-01', N'Stores', 49.380000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 54.810000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-05-01', N'Stores', 54.810000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 57.380000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-06-01', N'Stores', 57.380000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 128.982800, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-03-01', N'Stores', 128.982800, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 159.357524, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-04-01', N'Stores', 159.357524, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 171.748900, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-05-01', N'Stores', 171.748900, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 159.248466, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-06-01', N'Stores', 159.248466, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-03-01', N'Stores', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-04-01', N'Stores', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-05-01', N'Stores', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-06-01', N'Stores', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-03-01', N'Stores', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-04-01', N'Stores', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-05-01', N'Stores', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-06-01', N'Stores', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 84.486270, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-03-01', N'Stores', 84.486270, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 100.598400, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-04-01', N'Stores', 100.598400, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 100.124260, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-05-01', N'Stores', 100.124260, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 284.716870, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-06-01', N'Stores', 284.716870, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 19.112570, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-03-01', N'Stores', 19.112570, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 19.414030, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-04-01', N'Stores', 19.414030, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 20.018079, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-05-01', N'Stores', 20.018079, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 20.018079, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-06-01', N'Stores', 20.018079, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-03-01', N'Stores', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-04-01', N'Stores', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-05-01', N'Stores', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-06-01', N'Stores', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-03-01', N'Stores', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 18.349120, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-04-01', N'Stores', 18.349120, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 44.773560, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-05-01', N'Stores', 44.773560, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 44.773560, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-06-01', N'Stores', 44.773560, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 84.510000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-03-01', N'Stores', 84.510000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 88.771631, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-04-01', N'Stores', 88.771631, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 89.832260, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-05-01', N'Stores', 89.832260, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 91.763318, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-06-01', N'Stores', 91.763318, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 130.790000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-03-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-03-01', N'Stores', 130.790000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 160.276700, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-04-01', N'Stores', 160.276700, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 151.976760, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-05-01', N'Stores', 151.976760, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 151.079500, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-06-01', N'Stores', 151.079500, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-03-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-04-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-05-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-06-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-03-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-04-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-05-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-06-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-03-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-04-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-05-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-06-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-03-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-04-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-05-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-06-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-03-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-04-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-05-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-II)', NULL, '2026-06-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-03-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-04-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-05-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-06-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-03-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-04-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-05-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-06-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-03-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-04-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-05-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-06-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-03-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-04-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-05-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-06-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-03-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-04-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-05-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-06-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-03-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-04-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-05-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-06-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-03-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-03-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-04-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-05-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'June Group EBITDA Stock sheet'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-06-01', N'Traded', 0.000000, N'June Group EBITDA Stock sheet', GETDATE());

-- 04.conso TB -> 2026-04-01
DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd' AND sysdate >= '2026-04-01' AND sysdate < DATEADD(month, 1, '2026-04-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Freight Inward Expense (RCM)', 20440.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Salaries Expense', 2500000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Staff Welfare Expense', 19845.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Canteen Expense - Factory', 140000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Power Expense', 3919720.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Labour Charges', 2671000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Security Expenses', 124000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Freight Outward Expense (With GST)', 78380.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Professional & Consultancy Fees', 555000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'P.F. Contribution - Employer', 95000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Loading & Unloading Expense', 7080.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Conveyance Expenses', 4000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Postage & Courier Expense', 149760.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Printing & Stationery Expense', 10190.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Freight Outward Expense (RCM)', 570000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Job Charges', 1235889.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Repairs & Maintenance Exp - Plant & Machinery', 249812.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Employers Contribution to ESI', 10000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Medical Expense', 5490.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', N'Internal Transportation Expense / Reimbursement', 16570.0000, N'June Group EBITDA 04.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND sysdate >= '2026-04-01' AND sysdate < DATEADD(month, 1, '2026-04-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-04-01', N'Salaries Expense', 450000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-04-01', N'Labour Charges', 54540.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-04-01', N'Professional & Consultancy Fees', 50000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-04-01', N'P.F. Contribution - Employer', 7500.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-04-01', N'Conveyance Expenses', 2000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-04-01', N'Postage & Courier Expense', 19855.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-04-01', N'Repairs & Maintenance Exp - Plant & Machinery', 28300.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-04-01', N'Employers Contribution to ESI', 1000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-04-01', N'Internal Transportation Expense / Reimbursement', 7079.0000, N'June Group EBITDA 04.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED' AND sysdate >= '2026-04-01' AND sysdate < DATEADD(month, 1, '2026-04-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Salaries Expense', 14933704.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Canteen Expense - Factory', 235000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Power Expense', 3576447.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Labour Charges', 4554417.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Water Charges', 104358.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Security Expenses', 270400.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Vehicle Hire Charges - RCM', 35000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Vehicle Hire Charges - With Gst', 160000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Freight Outward Expense (With GST)', 1000000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Professional & Consultancy Fees', 609145.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Loading & Unloading Expense', 14280.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Repairs & Maintenance Exp - Others', 170042.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Freight Outward Expense (RCM)', 279000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Job Charges', 4954774.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'C & F  Charges - Export', 160059.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Fumigation Charges', 20060.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Factory Expense', 600.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'House Keeping Expense', 434328.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', N'Rent Expense - Colony', 1265900.0000, N'June Group EBITDA 04.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND sysdate >= '2026-04-01' AND sysdate < DATEADD(month, 1, '2026-04-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-04-01', N'Salaries Expense', 112692.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-04-01', N'Water Charges', 500.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-04-01', N'Security Expenses', 29000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-04-01', N'Mobile & Telephone Expense', 707.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-04-01', N'C & F  Charges - Export', 269520.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-04-01', N'Rent Expense - Factory', 352971.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-04-01', N'Factory Expense', 8550.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-04-01', N'Freight Outward Expense  - Export', 450000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-04-01', N'Insurance Expenses - Other', -100000.0000, N'June Group EBITDA 04.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Oswal Extrusion Limited' AND sysdate >= '2026-04-01' AND sysdate < DATEADD(month, 1, '2026-04-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Salaries Expense', 3840000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Staff Welfare Expense', -21780.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Canteen Expense - Factory', 45000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Labour Charges', 7900000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Water Charges', 250000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Security Expenses', 152000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Vehicle Hire Charges - RCM', 350000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Professional & Consultancy Fees', 380000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Advocate Fees Expense', 7500.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'P.F. Contribution - Employer', 140366.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Mobile & Telephone Expense', 25000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Loading & Unloading Expense', 45000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Postage & Courier Expense', 12000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Printing & Stationery Expense', 15000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Garden Mainteance Exp', 5000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Job Charges', 35000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Repairs & Maintenance Exp - Computer & Printers', 68000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Rent Expense - Factory', -477908.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Factory Expense', 5000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Repairs & Maintenance Exp - Plant & Machinery', 150000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Job Work Income', -28176333.2951, N'June Group EBITDA 04.conso TB', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Bonus Expense', 250000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Membership & Subscription Fees', -14908.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Internal Transportation Expense / Reimbursement', 33000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-04-01', N'Professional Tax - Company', -131528.0000, N'June Group EBITDA 04.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited' AND sysdate >= '2026-04-01' AND sysdate < DATEADD(month, 1, '2026-04-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-04-01', N'Salaries Expense', 5905866.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-04-01', N'Staff Welfare Expense', 81405.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-04-01', N'Canteen Expense - Factory', 133130.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-04-01', N'Power Expense', 8715453.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-04-01', N'Labour Charges', 6286205.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-04-01', N'Water Charges', 863400.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-04-01', N'Security Expenses', 136000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-04-01', N'Vehicle Hire Charges - RCM', 23000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-04-01', N'Vehicle Hire Charges - With Gst', 90000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-04-01', N'Income From Wind Mill', -600297.0000, N'June Group EBITDA 04.conso TB', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-04-01', N'Professional & Consultancy Fees', 125000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-04-01', N'C & F  Charges - Export', 843210.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-04-01', N'Freight outward Expenses (RCM_Withinstate)', 500000.0000, N'June Group EBITDA 04.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited (Unit -II)' AND sysdate >= '2026-04-01' AND sysdate < DATEADD(month, 1, '2026-04-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-04-01', N'Salaries Expense', 17399872.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-04-01', N'Loading & Unloading Expense', 69895.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-04-01', N'Freight Outward Expense (RCM)', 700000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-04-01', N'C & F  Charges - Export', 700000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-04-01', N'House Keeping Expense', 310000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-04-01', N'Freight Outward Expense  - Export', 700000.0000, N'June Group EBITDA 04.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND sysdate >= '2026-04-01' AND sysdate < DATEADD(month, 1, '2026-04-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-04-01', N'Labour Charges', 7206230.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-04-01', N'Water Charges', 103500.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-04-01', N'Freight Outward Expense (With GST)', 287000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-04-01', N'Professional & Consultancy Fees', 3600.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-04-01', N'Advocate Fees Expense', 7500.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-04-01', N'P.F. Contribution - Employer', 35405.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-04-01', N'Repairs & Maintenance Exp - Computer & Printers', 4400.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-04-01', N'C & F  Charges - Export', 155000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-04-01', N'Repairs & Maintenance Exp - Plant & Machinery', 38422.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-04-01', N'Medical Expense', 27800.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-04-01', N'Incentive Expense - Production', 11000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-04-01', N'Freight outward Expenses (RCM_Withinstate)', 20000.0000, N'June Group EBITDA 04.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited - HO' AND sysdate >= '2026-04-01' AND sysdate < DATEADD(month, 1, '2026-04-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited - HO', '2026-04-01', N'Salaries Expense', 2500000.0000, N'June Group EBITDA 04.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene Polyfilms Limited' AND sysdate >= '2026-04-01' AND sysdate < DATEADD(month, 1, '2026-04-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-04-01', N'Power Expense', 2000000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-04-01', N'Labour Charges', 7660768.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-04-01', N'Freight Outward Expense (With GST)', 185000.0000, N'June Group EBITDA 04.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-04-01', N'Job Work Income', -8947680.0000, N'June Group EBITDA 04.conso TB', N'Income');

-- 05.conso TB -> 2026-05-01
DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd' AND sysdate >= '2026-05-01' AND sysdate < DATEADD(month, 1, '2026-05-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Salaries Expense', 2600000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Staff Welfare Expense', 11046.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Canteen Expense - Factory', 170000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Power Expense', 2336144.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Labour Charges', 2639832.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Security Expenses', 196240.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Freight Outward Expense (With GST)', 2642466.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Professional & Consultancy Fees', 828910.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'P.F. Contribution - Employer', 98000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Loading & Unloading Expense', 23660.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Conveyance Expenses', 11864.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Postage & Courier Expense', 1700.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Printing & Stationery Expense', 6000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Freight Outward Expense (RCM)', 342750.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Job Charges', 9662240.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Repairs & Maintenance Exp - Plant & Machinery', 162590.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Employers Contribution to ESI', 12000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Medical Expense', 5500.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', N'Internal Transportation Expense / Reimbursement', 14704.0000, N'June Group EBITDA 05.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND sysdate >= '2026-05-01' AND sysdate < DATEADD(month, 1, '2026-05-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-05-01', N'Salaries Expense', 450000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-05-01', N'Labour Charges', 71584.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-05-01', N'Professional & Consultancy Fees', 50000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-05-01', N'P.F. Contribution - Employer', 8000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-05-01', N'Conveyance Expenses', 2000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-05-01', N'Job Charges', 932.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-05-01', N'Repairs & Maintenance Exp - Plant & Machinery', 16300.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-05-01', N'Employers Contribution to ESI', 15000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-05-01', N'Internal Transportation Expense / Reimbursement', 3805.0000, N'June Group EBITDA 05.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED' AND sysdate >= '2026-05-01' AND sysdate < DATEADD(month, 1, '2026-05-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Salaries Expense', 14561624.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Canteen Expense - Factory', 297440.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Power Expense', 3994704.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Labour Charges', 5335438.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Water Charges', 196417.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Security Expenses', 254855.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Vehicle Hire Charges - RCM', 37000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Vehicle Hire Charges - With Gst', 160000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Freight Outward Expense (With GST)', 2109578.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Professional & Consultancy Fees', 401900.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Loading & Unloading Expense', 20000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Repairs & Maintenance Exp - Others', 881909.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Freight Outward Expense (RCM)', 505500.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Job Charges', 9884517.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'C & F  Charges - Export', 329800.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Fumigation Charges', 123206.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Factory Expense', 54600.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'House Keeping Expense', 494052.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Medical Expense', 42875.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Rent Expense - Colony', 1307900.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', N'Equipment Certification Charges', 398859.0000, N'June Group EBITDA 05.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND sysdate >= '2026-05-01' AND sysdate < DATEADD(month, 1, '2026-05-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-05-01', N'Salaries Expense', 150000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-05-01', N'Water Charges', 250.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-05-01', N'Security Expenses', 34000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-05-01', N'Mobile & Telephone Expense', 707.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-05-01', N'C & F  Charges - Export', 406500.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-05-01', N'Rent Expense - Factory', -229285.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-05-01', N'Insurance Exp - Vehicles', -100000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-05-01', N'Fumigation Charges', 9500.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-05-01', N'C & F Charges - Detention Outward', 15000.0000, N'June Group EBITDA 05.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Oswal Extrusion Limited' AND sysdate >= '2026-05-01' AND sysdate < DATEADD(month, 1, '2026-05-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'C & F Charges - Import', 297000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Salaries Expense', 4500000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Canteen Expense - Factory', 35000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Labour Charges', 10000000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Water Charges', 298000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Security Expenses', 150000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Vehicle Hire Charges - RCM', 400000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Professional & Consultancy Fees', 395000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Advocate Fees Expense', 7500.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'P.F. Contribution - Employer', 145000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Mobile & Telephone Expense', 22000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Loading & Unloading Expense', 100000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Postage & Courier Expense', 27716.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Printing & Stationery Expense', 9000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Repairs & Maintenance Exp - Others', 167730.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Freight Outward Expense (RCM)', 45000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Job Charges', 35000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Repairs & Maintenance Exp - Computer & Printers', 39500.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Job Work Income', -25185263.4103, N'June Group EBITDA 05.conso TB', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Bonus Expense', 500000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Internal Transportation Expense / Reimbursement', 195000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-05-01', N'Insurance Expenses - Other', 6900.0000, N'June Group EBITDA 05.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited' AND sysdate >= '2026-05-01' AND sysdate < DATEADD(month, 1, '2026-05-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-05-01', N'Salaries Expense', 5794851.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-05-01', N'Staff Welfare Expense', 65741.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-05-01', N'Canteen Expense - Factory', 109133.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-05-01', N'Power Expense', 9449522.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-05-01', N'Labour Charges', 6821460.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-05-01', N'Water Charges', 952504.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-05-01', N'Security Expenses', 150258.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-05-01', N'Vehicle Hire Charges - RCM', 23000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-05-01', N'Vehicle Hire Charges - With Gst', 90000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-05-01', N'Income From Wind Mill', -1260319.0000, N'June Group EBITDA 05.conso TB', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-05-01', N'Professional & Consultancy Fees', 125000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-05-01', N'C & F  Charges - Export', 258636.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-05-01', N'Freight outward Expenses (RCM_Withinstate)', 500000.0000, N'June Group EBITDA 05.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited (Unit -II)' AND sysdate >= '2026-05-01' AND sysdate < DATEADD(month, 1, '2026-05-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-05-01', N'Salaries Expense', 9372319.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-05-01', N'Labour Charges', 6926989.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-05-01', N'Security Expenses', 235000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-05-01', N'Freight Outward Expense (With GST)', 1000000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-05-01', N'Loading & Unloading Expense', 91523.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-05-01', N'Freight Outward Expense (RCM)', 1000000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-05-01', N'House Keeping Expense', 310000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-05-01', N'Freight Outward Expense  - Export', 1000000.0000, N'June Group EBITDA 05.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND sysdate >= '2026-05-01' AND sysdate < DATEADD(month, 1, '2026-05-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-05-01', N'Labour Charges', 7142306.1000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-05-01', N'Water Charges', 127490.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-05-01', N'Freight Outward Expense (With GST)', 908700.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-05-01', N'Advocate Fees Expense', 7500.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-05-01', N'P.F. Contribution - Employer', 33466.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-05-01', N'Repairs & Maintenance Exp - Computer & Printers', 3560.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-05-01', N'C & F  Charges - Export', 497315.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-05-01', N'House Keeping Expense', 17600.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-05-01', N'Incentive Expense - Production', 11000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-05-01', N'Freight outward Expenses (RCM_Withinstate)', 3800.0000, N'June Group EBITDA 05.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited - HO' AND sysdate >= '2026-05-01' AND sysdate < DATEADD(month, 1, '2026-05-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited - HO', '2026-05-01', N'Salaries Expense', 2500000.0000, N'June Group EBITDA 05.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene Polyfilms Limited' AND sysdate >= '2026-05-01' AND sysdate < DATEADD(month, 1, '2026-05-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-05-01', N'Power Expense', 1900000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-05-01', N'Labour Charges', 6744231.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-05-01', N'Freight Outward Expense (With GST)', 225000.0000, N'June Group EBITDA 05.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-05-01', N'Job Work Income', -9419906.3400, N'June Group EBITDA 05.conso TB', N'Income');

-- 06.conso TB -> 2026-06-01
DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd' AND sysdate >= '2026-06-01' AND sysdate < DATEADD(month, 1, '2026-06-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Freight Inward Expense (RCM)', 27504.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Salaries Expense', 3000000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Staff Welfare Expense', 20734.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Canteen Expense - Factory', 200000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Power Expense', 2848022.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Labour Charges', 2665731.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Security Expenses', 123030.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Freight Outward Expense (With GST)', 1740000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Professional & Consultancy Fees', 962810.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'P.F. Contribution - Employer', 135000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Loading & Unloading Expense', 5120.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Conveyance Expenses', 7354.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Postage & Courier Expense', 24146.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Printing & Stationery Expense', 71370.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Freight Outward Expense (RCM)', 319000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Job Charges', 33397359.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Repairs & Maintenance Exp - Plant & Machinery', 176878.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Employers Contribution to ESI', 11500.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Medical Expense', 17000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', N'Internal Transportation Expense / Reimbursement', 94440.0000, N'June Group EBITDA 06.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND sysdate >= '2026-06-01' AND sysdate < DATEADD(month, 1, '2026-06-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-06-01', N'Salaries Expense', 350000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-06-01', N'Labour Charges', 43860.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-06-01', N'Professional & Consultancy Fees', 50000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-06-01', N'P.F. Contribution - Employer', 5000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-06-01', N'Postage & Courier Expense', 16440.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-06-01', N'Repairs & Maintenance Exp - Plant & Machinery', 3200.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-06-01', N'Employers Contribution to ESI', 800.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-06-01', N'Medical Expense', 2000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-06-01', N'Internal Transportation Expense / Reimbursement', 7362.0000, N'June Group EBITDA 06.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED' AND sysdate >= '2026-06-01' AND sysdate < DATEADD(month, 1, '2026-06-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Salaries Expense', 14394285.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Canteen Expense - Factory', 305645.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Power Expense', 3474593.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Labour Charges', 5707555.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Water Charges', 104358.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Security Expenses', 312750.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Vehicle Hire Charges - RCM', 75500.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Vehicle Hire Charges - With Gst', 160000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Freight Outward Expense (With GST)', 1536432.5000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Professional & Consultancy Fees', 255000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Loading & Unloading Expense', 20000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Freight Outward Expense (RCM)', 450000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Job Charges', 5289754.9000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'C & F  Charges - Export', 609549.7700, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Fumigation Charges', 20060.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Factory Expense', 5000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'House Keeping Expense', 467310.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Repairs & Maintenance Exp - Electric', 779709.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Medical Expense', 79148.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Rent Expense - Colony', 1313300.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Xerox Expense', 5000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', N'Equipment Certification Charges', 86840.0000, N'June Group EBITDA 06.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND sysdate >= '2026-06-01' AND sysdate < DATEADD(month, 1, '2026-06-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-06-01', N'Salaries Expense', 150000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-06-01', N'Water Charges', 350.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-06-01', N'Security Expenses', 34000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-06-01', N'Mobile & Telephone Expense', 707.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-06-01', N'C & F  Charges - Export', 450000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-06-01', N'Rent Expense - Factory', -79200.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-06-01', N'Insurance Exp - Vehicles', -38690.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-06-01', N'Fumigation Charges', 9560.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-06-01', N'C & F Charges - Detention Outward', 10000.0000, N'June Group EBITDA 06.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Oswal Extrusion Limited' AND sysdate >= '2026-06-01' AND sysdate < DATEADD(month, 1, '2026-06-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'C & F Charges - Import', 297000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Salaries Expense', 4000000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Staff Welfare Expense', -19800.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Canteen Expense - Factory', 54742.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Labour Charges', 11600000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Water Charges', 258000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Security Expenses', 150000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Vehicle Hire Charges - RCM', 345000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Professional & Consultancy Fees', 315389.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Advocate Fees Expense', 7500.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'P.F. Contribution - Employer', 140000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Mobile & Telephone Expense', 21000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Loading & Unloading Expense', 100000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Postage & Courier Expense', 27000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Printing & Stationery Expense', 9000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Repairs & Maintenance Exp - Others', 167730.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Freight Outward Expense (RCM)', 71000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Garden Mainteance Exp', 5000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Job Charges', 103407.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Repairs & Maintenance Exp - Computer & Printers', 45000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Rent Expense - Factory', -278158.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Job Work Income', -17832135.1047, N'June Group EBITDA 06.conso TB', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Bonus Expense', 750000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Membership & Subscription Fees', -10675.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Internal Transportation Expense / Reimbursement', 195000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Equipment Certification Charges', -7424.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Professional Tax - Company', -79611.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Insurance Expenses - Other', 12000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-06-01', N'Fees & Taxes', -6317.0000, N'June Group EBITDA 06.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited' AND sysdate >= '2026-06-01' AND sysdate < DATEADD(month, 1, '2026-06-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-06-01', N'Salaries Expense', 6097045.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-06-01', N'Staff Welfare Expense', 83405.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-06-01', N'Canteen Expense - Factory', 149766.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-06-01', N'Power Expense', 9799099.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-06-01', N'Labour Charges', 7581387.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-06-01', N'Water Charges', 1102876.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-06-01', N'Security Expenses', 147050.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-06-01', N'Vehicle Hire Charges - RCM', 23000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-06-01', N'Vehicle Hire Charges - With Gst', 90000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-06-01', N'Income From Wind Mill', -2499182.0000, N'June Group EBITDA 06.conso TB', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-06-01', N'Professional & Consultancy Fees', 125000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-06-01', N'C & F  Charges - Export', 875958.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-06-01', N'Freight outward Expenses (RCM_Withinstate)', 500000.0000, N'June Group EBITDA 06.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited (Unit -II)' AND sysdate >= '2026-06-01' AND sysdate < DATEADD(month, 1, '2026-06-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-06-01', N'Salaries Expense', 9869685.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-06-01', N'Labour Charges', 8584167.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-06-01', N'Security Expenses', 205000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-06-01', N'Freight Outward Expense (With GST)', 500000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-06-01', N'Loading & Unloading Expense', 89664.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-06-01', N'Freight Outward Expense (RCM)', 1000000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-06-01', N'House Keeping Expense', 310000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-06-01', N'Freight Outward Expense  - Export', 1000000.0000, N'June Group EBITDA 06.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND sysdate >= '2026-06-01' AND sysdate < DATEADD(month, 1, '2026-06-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', N'Labour Charges', 7560906.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', N'Water Charges', 147200.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', N'Freight Outward Expense (With GST)', 625000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', N'Advocate Fees Expense', 7500.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', N'P.F. Contribution - Employer', 36015.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', N'Mobile & Telephone Expense', 10497.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', N'Repairs & Maintenance Exp - Computer & Printers', 6000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', N'C & F  Charges - Export', 225000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', N'House Keeping Expense', 35200.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', N'Medical Expense', 1120.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', N'Incentive Expense - Production', 11000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', N'Testing Charges', 29000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', N'Freight outward Expenses (RCM_Withinstate)', 10400.0000, N'June Group EBITDA 06.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited - HO' AND sysdate >= '2026-06-01' AND sysdate < DATEADD(month, 1, '2026-06-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited - HO', '2026-06-01', N'Salaries Expense', 2500000.0000, N'June Group EBITDA 06.conso TB', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene Polyfilms Limited' AND sysdate >= '2026-06-01' AND sysdate < DATEADD(month, 1, '2026-06-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-06-01', N'Power Expense', 1738234.2300, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-06-01', N'Labour Charges', 7952292.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-06-01', N'Freight Outward Expense (With GST)', 185000.0000, N'June Group EBITDA 06.conso TB', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-06-01', N'Job Work Income', -2399515.2000, N'June Group EBITDA 06.conso TB', N'Income');

COMMIT;
-- ROLLBACK;
