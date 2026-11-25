/* Packing was inside the stores closing on the movement Stores sheet.
   Split Packing Material out of Stores for PIL-2 and KPW-1, Apr-Oct 2026.
   Stores + Packing stays equal to the sheet total. RM and FG already match the summary box.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

UPDATE dbo.PnlStockValue SET AmountLacs = 8.428800, Remarks = N'PIL-2 stors ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 109.829100, Remarks = N'PIL-2 stors ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;

UPDATE dbo.PnlStockValue SET AmountLacs = 5.660000, Remarks = N'PIL-2 stors ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 113.833400, Remarks = N'PIL-2 stors ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;

UPDATE dbo.PnlStockValue SET AmountLacs = 5.040300, Remarks = N'PIL-2 stors ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 110.910500, Remarks = N'PIL-2 stors ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;

UPDATE dbo.PnlStockValue SET AmountLacs = 6.674300, Remarks = N'PIL-2 stors ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-07-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 113.325200, Remarks = N'PIL-2 stors ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL;

UPDATE dbo.PnlStockValue SET AmountLacs = 8.704900, Remarks = N'PIL-2 stors ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-08-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 114.314600, Remarks = N'PIL-2 stors ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL;

UPDATE dbo.PnlStockValue SET AmountLacs = 7.213900, Remarks = N'PIL-2 stors ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-09-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 110.109800, Remarks = N'PIL-2 stors ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL;

UPDATE dbo.PnlStockValue SET AmountLacs = 8.505000, Remarks = N'PIL-2 stors ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-10-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 116.714200, Remarks = N'PIL-2 stors ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL;

UPDATE dbo.PnlStockValue SET AmountLacs = 3.539800, Remarks = N'KPW-1 Store ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 155.817800, Remarks = N'KPW-1 Store ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;

UPDATE dbo.PnlStockValue SET AmountLacs = 10.037800, Remarks = N'KPW-1 Store ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 161.711100, Remarks = N'KPW-1 Store ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;

UPDATE dbo.PnlStockValue SET AmountLacs = 3.332300, Remarks = N'KPW-1 Store ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 155.916200, Remarks = N'KPW-1 Store ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;

UPDATE dbo.PnlStockValue SET AmountLacs = 5.176800, Remarks = N'KPW-1 Store ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 144.022100, Remarks = N'KPW-1 Store ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL;

UPDATE dbo.PnlStockValue SET AmountLacs = 5.168600, Remarks = N'KPW-1 Store ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 151.622200, Remarks = N'KPW-1 Store ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL;

UPDATE dbo.PnlStockValue SET AmountLacs = 6.691400, Remarks = N'KPW-1 Store ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 146.304400, Remarks = N'KPW-1 Store ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL;

UPDATE dbo.PnlStockValue SET AmountLacs = 9.125200, Remarks = N'KPW-1 Store ClosingAmount Packing Material', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'Packing' AND PlantName IS NULL;
UPDATE dbo.PnlStockValue SET AmountLacs = 165.249900, Remarks = N'KPW-1 Store ClosingAmount excluding packing', UploadedAt = GETDATE()
WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL;

COMMIT;
