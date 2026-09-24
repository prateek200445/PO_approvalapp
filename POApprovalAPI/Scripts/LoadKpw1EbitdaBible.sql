/* KPW I bible: EBIDTA 26-27 - April to Aug 26--.xlsx
   Stock Sheet - KPW cols 16-20 (rupees / 1e5). Restates Apr-Jun RM/FG/Stores; loads Jul-Aug.
   Mar opening already matches. Packing/WIP/Traded set to 0.
   Provisions: JULY-TB / AUGUST - TB Provision column. Replaces Jul and Aug only.
   Does not touch other companies or Apr-Jun Provisioning.
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

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1166.990168, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-04-01', N'RawMaterial', 1166.990168, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1714.473477, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-04-01', N'FinishedGoods', 1714.473477, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 159.357524, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-04-01', N'Stores', 159.357524, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-04-01', N'Packing', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-04-01', N'WIP', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-04-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-04-01', N'Traded', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 785.483842, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-05-01', N'RawMaterial', 785.483842, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1722.863520, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-05-01', N'FinishedGoods', 1722.863520, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 171.748900, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-05-01', N'Stores', 171.748900, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-05-01', N'Packing', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-05-01', N'WIP', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-05-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-05-01', N'Traded', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 638.829982, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-06-01', N'RawMaterial', 638.829982, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1860.988601, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-06-01', N'FinishedGoods', 1860.988601, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 159.248466, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-06-01', N'Stores', 159.248466, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-06-01', N'Packing', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-06-01', N'WIP', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-06-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-06-01', N'Traded', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 848.928888, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-07-01', N'RawMaterial', 848.928888, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1750.329559, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-07-01', N'FinishedGoods', 1750.329559, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 149.198985, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-07-01', N'Stores', 149.198985, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-07-01', N'Packing', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-07-01', N'WIP', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-07-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-07-01', N'Traded', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1116.864220, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-08-01', N'RawMaterial', 1116.864220, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1739.427597, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-08-01', N'FinishedGoods', 1739.427597, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 156.790801, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-08-01', N'Stores', 156.790801, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-08-01', N'Packing', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-08-01', N'WIP', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW I EBITDA bible Stock Sheet - KPW'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-08-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-08-01', N'Traded', 0.000000, N'KPW I EBITDA bible Stock Sheet - KPW', GETDATE());

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED' AND sysdate >= '2026-07-01' AND sysdate < DATEADD(month, 1, '2026-07-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Equipment Certification Charges', 31748.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Factory Expense', 6200.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Labour Charges', 7315182.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Power Expense', 4130997.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Rent Expense - Colony', 1313300.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Job Charges', 228490.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Water Charges', 184784.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'House Keeping Expense', 467310.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Security Expenses', 324466.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Xerox Expense', 4500.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Repairs & Maintenance Exp - Building', 645623.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Vehicle Hire Charges - RCM', 75500.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Vehicle Hire Charges - With Gst', 160000.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'P.F. Admin. Charge', 31714.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'P.F. Contribution - Employer', 684273.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Allowance - Canteen', 320910.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Employers Contribution to ESI', 344324.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Salaries Expense', 14975707.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Medical Expense', 176812.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Exchange Rate Difference - Debtors', -25161872.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Professional & Consultancy Fees', 639867.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'C & F  Charges - Export', 1178682.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Freight Outward Expense  - Export', -10733989.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Freight Outward Expense (RCM)', 336000.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-07-01', N'Freight Outward Expense (With GST)', 345940.0000, N'KPW I EBITDA bible JULY-TB Provision', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED' AND sysdate >= '2026-08-01' AND sysdate < DATEADD(month, 1, '2026-08-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Factory Expense', 5000.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Labour Charges', 8112264.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Power Expense', 4320121.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Rent Expense - Colony', 1321300.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Job Charges', 207763.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Water Charges', 94105.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Repairs & Maintenance Exp - Plant & Machinery', 164138.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'House Keeping Expense', 554014.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Security Expenses', 415483.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Xerox Expense', 4500.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Vehicle Hire Charges - RCM', 75500.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Vehicle Hire Charges - With Gst', 160000.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'P.F. Admin. Charge', 30689.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'P.F. Contribution - Employer', 766732.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Salaries Expense', 18707589.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Canteen Expense - Factory', 376290.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Medical Expense', 34992.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Exchange Rate Difference - Debtors', -25161872.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Professional & Consultancy Fees', 310000.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'C & F  Charges - Export', 419308.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Freight Outward Expense  - Export', -10733989.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Freight Outward Expense (RCM)', 500000.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-08-01', N'Freight Outward Expense (With GST)', 3236167.0000, N'KPW I EBITDA bible AUGUST-TB Provision', N'Expense');

COMMIT;
-- ROLLBACK;
