/* July stock for HPBL 1, HPBL 2 and Vadodara.
   August stock for those three. October stock for HPBL 1 from Valuation.xlsx.
   HPBL 3 July, August and October stay equal to September. No carry-forward. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 240.987170, UploadedAt = GETDATE(), Remarks = N'HPBL Stock.xlsx July'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-07-01', N'Stores', 240.987170, N'HPBL Stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 20.622130, UploadedAt = GETDATE(), Remarks = N'HPBL Stock.xlsx July'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-07-01', N'Stores', 20.622130, N'HPBL Stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 51.972090, UploadedAt = GETDATE(), Remarks = N'HPBL Stock.xlsx July'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-07-01', N'Stores', 51.972090, N'HPBL Stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 434.783090, UploadedAt = GETDATE(), Remarks = N'HPBL Stock.xlsx July'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-07-01', N'RawMaterial', 434.783090, N'HPBL Stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 24.436080, UploadedAt = GETDATE(), Remarks = N'HPBL Stock.xlsx July'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-07-01', N'RawMaterial', 24.436080, N'HPBL Stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 461.254960, UploadedAt = GETDATE(), Remarks = N'HPBL Stock.xlsx July'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-07-01', N'WIP', 461.254960, N'HPBL Stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 3.516960, UploadedAt = GETDATE(), Remarks = N'HPBL Stock.xlsx July'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-07-01', N'WIP', 3.516960, N'HPBL Stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 189.897700, UploadedAt = GETDATE(), Remarks = N'HPBL Stock.xlsx July'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-07-01', N'WIP', 189.897700, N'HPBL Stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 114.185950, UploadedAt = GETDATE(), Remarks = N'HPBL Stock.xlsx July'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-07-01', N'FinishedGoods', 114.185950, N'HPBL Stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 8.471450, UploadedAt = GETDATE(), Remarks = N'HPBL Stock.xlsx July'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-07-01', N'FinishedGoods', 8.471450, N'HPBL Stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 112.845180, UploadedAt = GETDATE(), Remarks = N'HPBL Stock.xlsx July'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-07-01', N'FinishedGoods', 112.845180, N'HPBL Stock.xlsx July', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 178.016470, UploadedAt = GETDATE(), Remarks = N'HPBL Stock aug.xlsx August'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-08-01', N'Stores', 178.016470, N'HPBL Stock aug.xlsx August', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 31.553440, UploadedAt = GETDATE(), Remarks = N'HPBL Stock aug.xlsx August'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-08-01', N'Stores', 31.553440, N'HPBL Stock aug.xlsx August', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 77.328370, UploadedAt = GETDATE(), Remarks = N'HPBL Stock aug.xlsx August'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-08-01', N'Stores', 77.328370, N'HPBL Stock aug.xlsx August', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 5.134390, UploadedAt = GETDATE(), Remarks = N'HPBL Stock aug.xlsx August'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-08-01', N'RawMaterial', 5.134390, N'HPBL Stock aug.xlsx August', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 41.277710, UploadedAt = GETDATE(), Remarks = N'HPBL Stock aug.xlsx August'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-08-01', N'RawMaterial', 41.277710, N'HPBL Stock aug.xlsx August', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 613.320490, UploadedAt = GETDATE(), Remarks = N'HPBL Stock aug.xlsx August'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-08-01', N'WIP', 613.320490, N'HPBL Stock aug.xlsx August', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 3.105300, UploadedAt = GETDATE(), Remarks = N'HPBL Stock aug.xlsx August'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-08-01', N'WIP', 3.105300, N'HPBL Stock aug.xlsx August', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 189.589830, UploadedAt = GETDATE(), Remarks = N'HPBL Stock aug.xlsx August'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-08-01', N'WIP', 189.589830, N'HPBL Stock aug.xlsx August', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 108.885620, UploadedAt = GETDATE(), Remarks = N'HPBL Stock aug.xlsx August'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-08-01', N'FinishedGoods', 108.885620, N'HPBL Stock aug.xlsx August', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 4.571120, UploadedAt = GETDATE(), Remarks = N'HPBL Stock aug.xlsx August'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-08-01', N'FinishedGoods', 4.571120, N'HPBL Stock aug.xlsx August', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 121.389560, UploadedAt = GETDATE(), Remarks = N'HPBL Stock aug.xlsx August'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-08-01', N'FinishedGoods', 121.389560, N'HPBL Stock aug.xlsx August', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 763.551550, UploadedAt = GETDATE(), Remarks = N'Valuation.xlsx October HPBL 1'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-10-01', N'RawMaterial', 763.551550, N'Valuation.xlsx October HPBL 1', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 446.226130, UploadedAt = GETDATE(), Remarks = N'Valuation.xlsx October HPBL 1'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-10-01', N'WIP', 446.226130, N'Valuation.xlsx October HPBL 1', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 171.591320, UploadedAt = GETDATE(), Remarks = N'Valuation.xlsx October HPBL 1'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-10-01', N'FinishedGoods', 171.591320, N'Valuation.xlsx October HPBL 1', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 133.913970, UploadedAt = GETDATE(), Remarks = N'Valuation.xlsx October HPBL 1'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-10-01', N'Stores', 133.913970, N'Valuation.xlsx October HPBL 1', GETDATE());

COMMIT;
