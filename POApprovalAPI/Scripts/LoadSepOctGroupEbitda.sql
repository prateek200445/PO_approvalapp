/* Sep-Oct EBITDA stock and provisions, plus July-August stock that the movement
   and EBITDA sheets can support. Does not delete Apr-Jun. Does not replace
   Jul-Aug provisions. Missing stock categories are copied from the prior month.
   Not loaded: HPBL-4 October provisions (template stops at September);
   HPBL 1/2/3/Baroda stock other than September; KPW-1 movement sheet (negative RM,
   bible stock sheet used instead); PIL-2 / KPW-1 stores tabs that are prior-year.
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

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 2348.461246, UploadedAt = GETDATE(), Remarks = N'KPW-3 Summary factory value + at JW own'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-07-01', N'FinishedGoods', 2348.461246, N'KPW-3 Summary factory value + at JW own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 387.796722, UploadedAt = GETDATE(), Remarks = N'KPW-3 Summary factory value + at JW own'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-07-01', N'RawMaterial', 387.796722, N'KPW-3 Summary factory value + at JW own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1106.429109, UploadedAt = GETDATE(), Remarks = N'KPW-3 Summary factory value + at JW own'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-07-01', N'WIP', 1106.429109, N'KPW-3 Summary factory value + at JW own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 164.364829, UploadedAt = GETDATE(), Remarks = N'OEL Summary Value Factory Own'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-07-01', N'FinishedGoods', 164.364829, N'OEL Summary Value Factory Own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 151.110904, UploadedAt = GETDATE(), Remarks = N'OEL Summary Value Factory Own'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-07-01', N'RawMaterial', 151.110904, N'OEL Summary Value Factory Own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 35.118585, UploadedAt = GETDATE(), Remarks = N'OEL Summary Value Factory Own'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-07-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-07-01', N'WIP', 35.118585, N'OEL Summary Value Factory Own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1649.788893, UploadedAt = GETDATE(), Remarks = N'PIL-2 EBITDA stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-07-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-07-01', N'FinishedGoods', 1649.788893, N'PIL-2 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 942.578953, UploadedAt = GETDATE(), Remarks = N'PIL-2 EBITDA stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-07-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-07-01', N'RawMaterial', 942.578953, N'PIL-2 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 119.999594, UploadedAt = GETDATE(), Remarks = N'PIL-2 EBITDA stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-07-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-07-01', N'Stores', 119.999594, N'PIL-2 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 2876.258867, UploadedAt = GETDATE(), Remarks = N'KPW-3 Summary factory value + at JW own'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-08-01', N'FinishedGoods', 2876.258867, N'KPW-3 Summary factory value + at JW own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 388.863363, UploadedAt = GETDATE(), Remarks = N'KPW-3 Summary factory value + at JW own'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-08-01', N'RawMaterial', 388.863363, N'KPW-3 Summary factory value + at JW own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1476.511024, UploadedAt = GETDATE(), Remarks = N'KPW-3 Summary factory value + at JW own'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-08-01', N'WIP', 1476.511024, N'KPW-3 Summary factory value + at JW own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 222.192078, UploadedAt = GETDATE(), Remarks = N'OEL Summary Value Factory Own'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-08-01', N'FinishedGoods', 222.192078, N'OEL Summary Value Factory Own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 323.292053, UploadedAt = GETDATE(), Remarks = N'OEL Summary Value Factory Own'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-08-01', N'RawMaterial', 323.292053, N'OEL Summary Value Factory Own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 36.753964, UploadedAt = GETDATE(), Remarks = N'OEL Summary Value Factory Own'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-08-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-08-01', N'WIP', 36.753964, N'OEL Summary Value Factory Own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1682.778956, UploadedAt = GETDATE(), Remarks = N'PIL-2 EBITDA stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-08-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-08-01', N'FinishedGoods', 1682.778956, N'PIL-2 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 743.015950, UploadedAt = GETDATE(), Remarks = N'PIL-2 EBITDA stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-08-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-08-01', N'RawMaterial', 743.015950, N'PIL-2 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 123.019505, UploadedAt = GETDATE(), Remarks = N'PIL-2 EBITDA stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-08-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-08-01', N'Stores', 123.019505, N'PIL-2 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 182.516690, UploadedAt = GETDATE(), Remarks = N'HPBL Sep stock summary'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-09-01', N'FinishedGoods', 182.516690, N'HPBL Sep stock summary', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 146.249500, UploadedAt = GETDATE(), Remarks = N'HPBL Sep stock summary'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-09-01', N'RawMaterial', 146.249500, N'HPBL Sep stock summary', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 122.604880, UploadedAt = GETDATE(), Remarks = N'HPBL Sep stock summary'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-09-01', N'Stores', 122.604880, N'HPBL Sep stock summary', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 645.645310, UploadedAt = GETDATE(), Remarks = N'HPBL Sep stock summary'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', NULL, '2026-09-01', N'WIP', 645.645310, N'HPBL Sep stock summary', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 8.471450, UploadedAt = GETDATE(), Remarks = N'HPBL Sep stock summary'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-09-01', N'FinishedGoods', 8.471450, N'HPBL Sep stock summary', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 24.436080, UploadedAt = GETDATE(), Remarks = N'HPBL Sep stock summary'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-09-01', N'RawMaterial', 24.436080, N'HPBL Sep stock summary', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 20.622130, UploadedAt = GETDATE(), Remarks = N'HPBL Sep stock summary'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-09-01', N'Stores', 20.622130, N'HPBL Sep stock summary', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 3.516960, UploadedAt = GETDATE(), Remarks = N'HPBL Sep stock summary'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', NULL, '2026-09-01', N'WIP', 3.516960, N'HPBL Sep stock summary', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 22.118540, UploadedAt = GETDATE(), Remarks = N'HPBL Sep stock summary'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-09-01', N'RawMaterial', 22.118540, N'HPBL Sep stock summary', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 413.930000, UploadedAt = GETDATE(), Remarks = N'HPBL4 SEPT-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-09-01', N'FinishedGoods', 413.930000, N'HPBL4 SEPT-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-09-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 5.100760, UploadedAt = GETDATE(), Remarks = N'HPBL4 SEPT-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-09-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-09-01', N'Packing', 5.100760, N'HPBL4 SEPT-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 367.920000, UploadedAt = GETDATE(), Remarks = N'HPBL4 SEPT-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-09-01', N'RawMaterial', 367.920000, N'HPBL4 SEPT-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 62.495170, UploadedAt = GETDATE(), Remarks = N'HPBL4 SEPT-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-09-01', N'Stores', 62.495170, N'HPBL4 SEPT-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-09-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'HPBL4 SEPT-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-09-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-09-01', N'Traded', 0.000000, N'HPBL4 SEPT-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.458280, UploadedAt = GETDATE(), Remarks = N'HPBL4 SEPT-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-09-01', N'WIP', 0.458280, N'HPBL4 SEPT-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 105.035100, UploadedAt = GETDATE(), Remarks = N'HPBL Sep stock summary'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-09-01', N'FinishedGoods', 105.035100, N'HPBL Sep stock summary', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 60.197380, UploadedAt = GETDATE(), Remarks = N'HPBL Sep stock summary'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-09-01', N'Stores', 60.197380, N'HPBL Sep stock summary', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 300.836030, UploadedAt = GETDATE(), Remarks = N'HPBL Sep stock summary'
    WHERE CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack ltd (Vadodara)', NULL, '2026-09-01', N'WIP', 300.836030, N'HPBL Sep stock summary', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1788.222565, UploadedAt = GETDATE(), Remarks = N'KPW-1 EBITDA stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-09-01', N'FinishedGoods', 1788.222565, N'KPW-1 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW-1 stock sheet has no packing/WIP/traded'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-09-01', N'Packing', 0.000000, N'KPW-1 stock sheet has no packing/WIP/traded', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1271.252640, UploadedAt = GETDATE(), Remarks = N'KPW-1 EBITDA stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-09-01', N'RawMaterial', 1271.252640, N'KPW-1 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 152.995806, UploadedAt = GETDATE(), Remarks = N'KPW-1 EBITDA stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-09-01', N'Stores', 152.995806, N'KPW-1 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW-1 stock sheet has no packing/WIP/traded'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-09-01', N'Traded', 0.000000, N'KPW-1 stock sheet has no packing/WIP/traded', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW-1 stock sheet has no packing/WIP/traded'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-09-01', N'WIP', 0.000000, N'KPW-1 stock sheet has no packing/WIP/traded', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 164.096289, UploadedAt = GETDATE(), Remarks = N'KPW-3 Sep stock valuation value+job'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-09-01', N'FinishedGoods', 164.096289, N'KPW-3 Sep stock valuation value+job', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1774.958322, UploadedAt = GETDATE(), Remarks = N'KPW-3 Sep stock valuation value+job'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-09-01', N'RawMaterial', 1774.958322, N'KPW-3 Sep stock valuation value+job', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1049.604302, UploadedAt = GETDATE(), Remarks = N'KPW-3 Sep stock valuation value+job'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-09-01', N'WIP', 1049.604302, N'KPW-3 Sep stock valuation value+job', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 302.535976, UploadedAt = GETDATE(), Remarks = N'OEL Summary Value Factory Own'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-09-01', N'FinishedGoods', 302.535976, N'OEL Summary Value Factory Own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 365.714084, UploadedAt = GETDATE(), Remarks = N'OEL Summary Value Factory Own'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-09-01', N'RawMaterial', 365.714084, N'OEL Summary Value Factory Own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 58.246164, UploadedAt = GETDATE(), Remarks = N'OEL Summary Value Factory Own'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-09-01', N'WIP', 58.246164, N'OEL Summary Value Factory Own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1134.310977, UploadedAt = GETDATE(), Remarks = N'sept 26/PIL1/pnl-stock-template-plastene-india-limited-2026-09 (1).xlsx'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-09-01', N'FinishedGoods', 1134.310977, N'sept 26/PIL1/pnl-stock-template-plastene-india-limited-2026-09 (1).xlsx', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-09-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 10.900596, UploadedAt = GETDATE(), Remarks = N'sept 26/PIL1/pnl-stock-template-plastene-india-limited-2026-09 (1).xlsx'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-09-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-09-01', N'Packing', 10.900596, N'sept 26/PIL1/pnl-stock-template-plastene-india-limited-2026-09 (1).xlsx', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1776.340366, UploadedAt = GETDATE(), Remarks = N'sept 26/PIL1/pnl-stock-template-plastene-india-limited-2026-09 (1).xlsx'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-09-01', N'RawMaterial', 1776.340366, N'sept 26/PIL1/pnl-stock-template-plastene-india-limited-2026-09 (1).xlsx', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 153.293694, UploadedAt = GETDATE(), Remarks = N'sept 26/PIL1/pnl-stock-template-plastene-india-limited-2026-09 (1).xlsx'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-09-01', N'Stores', 153.293694, N'sept 26/PIL1/pnl-stock-template-plastene-india-limited-2026-09 (1).xlsx', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-09-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'sept 26/PIL1/pnl-stock-template-plastene-india-limited-2026-09 (1).xlsx'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-09-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-09-01', N'Traded', 0.000000, N'sept 26/PIL1/pnl-stock-template-plastene-india-limited-2026-09 (1).xlsx', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 78.007571, UploadedAt = GETDATE(), Remarks = N'sept 26/PIL1/pnl-stock-template-plastene-india-limited-2026-09 (1).xlsx'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-09-01', N'WIP', 78.007571, N'sept 26/PIL1/pnl-stock-template-plastene-india-limited-2026-09 (1).xlsx', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1828.608427, UploadedAt = GETDATE(), Remarks = N'PIL-2 EBITDA stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-09-01', N'FinishedGoods', 1828.608427, N'PIL-2 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1027.985036, UploadedAt = GETDATE(), Remarks = N'PIL-2 EBITDA stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-09-01', N'RawMaterial', 1027.985036, N'PIL-2 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 117.323807, UploadedAt = GETDATE(), Remarks = N'PIL-2 EBITDA stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-09-01', N'Stores', 117.323807, N'PIL-2 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 701.198419, UploadedAt = GETDATE(), Remarks = N'PPL Sep valuation FG+SF'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-09-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-09-01', N'FinishedGoods', 701.198419, N'PPL Sep valuation FG+SF', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-09-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 15.875220, UploadedAt = GETDATE(), Remarks = N'PPL Sep valuation packing'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-09-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-09-01', N'Packing', 15.875220, N'PPL Sep valuation packing', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 181.826822, UploadedAt = GETDATE(), Remarks = N'PPL Sep valuation'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-09-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-09-01', N'RawMaterial', 181.826822, N'PPL Sep valuation', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 103.062536, UploadedAt = GETDATE(), Remarks = N'PPL Sep valuation stores'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-09-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-09-01', N'Stores', 103.062536, N'PPL Sep valuation stores', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 139.252679, UploadedAt = GETDATE(), Remarks = N'PPL Sep valuation #N/A'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-09-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-09-01', N'WIP', 139.252679, N'PPL Sep valuation #N/A', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 435.680000, UploadedAt = GETDATE(), Remarks = N'HPBL4 OCT-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-10-01', N'FinishedGoods', 435.680000, N'HPBL4 OCT-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-10-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 11.420000, UploadedAt = GETDATE(), Remarks = N'HPBL4 OCT-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-10-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-10-01', N'Packing', 11.420000, N'HPBL4 OCT-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 468.900000, UploadedAt = GETDATE(), Remarks = N'HPBL4 OCT-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-10-01', N'RawMaterial', 468.900000, N'HPBL4 OCT-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 69.150000, UploadedAt = GETDATE(), Remarks = N'HPBL4 OCT-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-10-01', N'Stores', 69.150000, N'HPBL4 OCT-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-10-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'HPBL4 OCT-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-10-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-10-01', N'Traded', 0.000000, N'HPBL4 OCT-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.521990, UploadedAt = GETDATE(), Remarks = N'HPBL4 OCT-26'
    WHERE CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', NULL, '2026-10-01', N'WIP', 0.521990, N'HPBL4 OCT-26', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1151.074723, UploadedAt = GETDATE(), Remarks = N'KPW-1 EBITDA stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-10-01', N'FinishedGoods', 1151.074723, N'KPW-1 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW-1 stock sheet has no packing/WIP/traded'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-10-01', N'Packing', 0.000000, N'KPW-1 stock sheet has no packing/WIP/traded', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1888.649360, UploadedAt = GETDATE(), Remarks = N'KPW-1 EBITDA stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-10-01', N'RawMaterial', 1888.649360, N'KPW-1 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 174.375110, UploadedAt = GETDATE(), Remarks = N'KPW-1 EBITDA stock sheet'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-10-01', N'Stores', 174.375110, N'KPW-1 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW-1 stock sheet has no packing/WIP/traded'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-10-01', N'Traded', 0.000000, N'KPW-1 stock sheet has no packing/WIP/traded', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'KPW-1 stock sheet has no packing/WIP/traded'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', NULL, '2026-10-01', N'WIP', 0.000000, N'KPW-1 stock sheet has no packing/WIP/traded', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 3870.112666, UploadedAt = GETDATE(), Remarks = N'KPW-3 Summary factory value + at JW own'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-10-01', N'FinishedGoods', 3870.112666, N'KPW-3 Summary factory value + at JW own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 453.295457, UploadedAt = GETDATE(), Remarks = N'KPW-3 Summary factory value + at JW own'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-10-01', N'RawMaterial', 453.295457, N'KPW-3 Summary factory value + at JW own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1893.301015, UploadedAt = GETDATE(), Remarks = N'KPW-3 Summary factory value + at JW own'
    WHERE CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', NULL, '2026-10-01', N'WIP', 1893.301015, N'KPW-3 Summary factory value + at JW own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 371.643960, UploadedAt = GETDATE(), Remarks = N'OEL Summary Value Factory Own'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-10-01', N'FinishedGoods', 371.643960, N'OEL Summary Value Factory Own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 372.388969, UploadedAt = GETDATE(), Remarks = N'OEL Summary Value Factory Own'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-10-01', N'RawMaterial', 372.388969, N'OEL Summary Value Factory Own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 77.267764, UploadedAt = GETDATE(), Remarks = N'OEL Summary Value Factory Own'
    WHERE CompanyName = N'Oswal Extrusion Limited' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', NULL, '2026-10-01', N'WIP', 77.267764, N'OEL Summary Value Factory Own', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1221.423309, UploadedAt = GETDATE(), Remarks = N'oct 26/PIL1/pnl-stock-template-plastene-india-limited-2026-10 (1).xlsx'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-10-01', N'FinishedGoods', 1221.423309, N'oct 26/PIL1/pnl-stock-template-plastene-india-limited-2026-10 (1).xlsx', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-10-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 12.378464, UploadedAt = GETDATE(), Remarks = N'oct 26/PIL1/pnl-stock-template-plastene-india-limited-2026-10 (1).xlsx'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-10-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-10-01', N'Packing', 12.378464, N'oct 26/PIL1/pnl-stock-template-plastene-india-limited-2026-10 (1).xlsx', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1936.844118, UploadedAt = GETDATE(), Remarks = N'oct 26/PIL1/pnl-stock-template-plastene-india-limited-2026-10 (1).xlsx'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-10-01', N'RawMaterial', 1936.844118, N'oct 26/PIL1/pnl-stock-template-plastene-india-limited-2026-10 (1).xlsx', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 158.451684, UploadedAt = GETDATE(), Remarks = N'oct 26/PIL1/pnl-stock-template-plastene-india-limited-2026-10 (1).xlsx'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-10-01', N'Stores', 158.451684, N'oct 26/PIL1/pnl-stock-template-plastene-india-limited-2026-10 (1).xlsx', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-10-01' AND Category = N'Traded' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 0.000000, UploadedAt = GETDATE(), Remarks = N'oct 26/PIL1/pnl-stock-template-plastene-india-limited-2026-10 (1).xlsx'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-10-01' AND Category = N'Traded' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-10-01', N'Traded', 0.000000, N'oct 26/PIL1/pnl-stock-template-plastene-india-limited-2026-10 (1).xlsx', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 74.978514, UploadedAt = GETDATE(), Remarks = N'oct 26/PIL1/pnl-stock-template-plastene-india-limited-2026-10 (1).xlsx'
    WHERE CompanyName = N'Plastene India Limited' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited', NULL, '2026-10-01', N'WIP', 74.978514, N'oct 26/PIL1/pnl-stock-template-plastene-india-limited-2026-10 (1).xlsx', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1907.488250, UploadedAt = GETDATE(), Remarks = N'PIL-2 EBITDA stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-10-01', N'FinishedGoods', 1907.488250, N'PIL-2 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 1168.951688, UploadedAt = GETDATE(), Remarks = N'PIL-2 EBITDA stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-10-01', N'RawMaterial', 1168.951688, N'PIL-2 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 125.219234, UploadedAt = GETDATE(), Remarks = N'PIL-2 EBITDA stock sheet'
    WHERE CompanyName = N'Plastene India Limited (Unit -II)' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', NULL, '2026-10-01', N'Stores', 125.219234, N'PIL-2 EBITDA stock sheet', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 851.986818, UploadedAt = GETDATE(), Remarks = N'PPL Oct valuation FG+SF'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-10-01' AND Category = N'FinishedGoods' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-10-01', N'FinishedGoods', 851.986818, N'PPL Oct valuation FG+SF', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-10-01' AND Category = N'Packing' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 16.673320, UploadedAt = GETDATE(), Remarks = N'PPL Oct valuation packing'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-10-01' AND Category = N'Packing' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-10-01', N'Packing', 16.673320, N'PPL Oct valuation packing', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 454.562321, UploadedAt = GETDATE(), Remarks = N'PPL Oct valuation'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-10-01' AND Category = N'RawMaterial' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-10-01', N'RawMaterial', 454.562321, N'PPL Oct valuation', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 110.281539, UploadedAt = GETDATE(), Remarks = N'PPL Oct valuation stores'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-10-01' AND Category = N'Stores' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-10-01', N'Stores', 110.281539, N'PPL Oct valuation stores', GETDATE());

IF EXISTS (SELECT 1 FROM dbo.PnlStockValue WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL)
    UPDATE dbo.PnlStockValue
    SET AmountLacs = 170.444335, UploadedAt = GETDATE(), Remarks = N'PPL Oct valuation #N/A'
    WHERE CompanyName = N'Plastene Polyfilms Limited' AND StockMonth = '2026-10-01' AND Category = N'WIP' AND PlantName IS NULL;
ELSE
    INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', NULL, '2026-10-01', N'WIP', 170.444335, N'PPL Oct valuation #N/A', GETDATE());

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-07-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND p.StockMonth = '2026-06-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-07-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-07-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'Oswal Extrusion Limited' AND p.StockMonth = '2026-06-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-07-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-07-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'Plastene India Limited (Unit -II)' AND p.StockMonth = '2026-06-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-07-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-08-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND p.StockMonth = '2026-07-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-08-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-08-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'Oswal Extrusion Limited' AND p.StockMonth = '2026-07-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-08-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-08-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'Plastene India Limited (Unit -II)' AND p.StockMonth = '2026-07-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-08-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-09-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'HCP Plastene Bulkpack Ltd' AND p.StockMonth = '2026-08-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-09-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-09-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND p.StockMonth = '2026-08-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-09-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-09-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND p.StockMonth = '2026-08-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-09-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-09-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND p.StockMonth = '2026-08-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-09-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-09-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'HCP Plastene Bulkpack ltd (Vadodara)' AND p.StockMonth = '2026-08-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-09-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-09-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND p.StockMonth = '2026-08-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-09-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-09-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND p.StockMonth = '2026-08-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-09-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-09-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'Oswal Extrusion Limited' AND p.StockMonth = '2026-08-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-09-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-09-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'Plastene India Limited' AND p.StockMonth = '2026-08-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-09-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-09-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'Plastene India Limited (Unit -II)' AND p.StockMonth = '2026-08-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-09-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-09-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'Plastene Polyfilms Limited' AND p.StockMonth = '2026-08-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-09-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-10-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND p.StockMonth = '2026-09-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-10-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-10-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'K.P. WOVEN PRIVATE LIMITED' AND p.StockMonth = '2026-09-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-10-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-10-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND p.StockMonth = '2026-09-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-10-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-10-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'Oswal Extrusion Limited' AND p.StockMonth = '2026-09-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-10-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-10-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'Plastene India Limited' AND p.StockMonth = '2026-09-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-10-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-10-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'Plastene India Limited (Unit -II)' AND p.StockMonth = '2026-09-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-10-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT p.CompanyName, NULL, '2026-10-01', p.Category, p.AmountLacs, N'carried forward from prior month', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'Plastene Polyfilms Limited' AND p.StockMonth = '2026-09-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-10-01' AND c.Category = p.Category AND c.PlantName IS NULL);

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd' AND sysdate >= '2026-09-01' AND sysdate < DATEADD(month, 1, '2026-09-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Canteen Expense - Factory', 410000.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Conveyance Expenses', 684.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Employers Contribution to ESI', 17244.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Freight Inward Expense (RCM)', 118000.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Freight Outward Expense (RCM)', 669850.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Freight Outward Expense (With GST)', 5916222.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Internal Transportation Expense / Reimbursement', 11265.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Job Charges', 22850526.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Labour Charges', 3866608.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Loading & Unloading Expense', 8640.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Medical Expense', 19362.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'P.F. Contribution - Employer', 200297.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Postage & Courier Expense', 1680.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Power Expense', 4229374.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Printing & Stationery Expense', 6000.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Professional & Consultancy Fees', 682255.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Repairs & Maintenance Exp - Plant & Machinery', 328352.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Salaries Expense', 3808372.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Security Expenses', 124000.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Staff Welfare Expense', 24656.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-09-01', N'Testing Charges', 4500.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-1', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND sysdate >= '2026-09-01' AND sysdate < DATEADD(month, 1, '2026-09-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-09-01', N'Conveyance Expenses', 2000.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-09-01', N'Employers Contribution to ESI', 841.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-09-01', N'Internal Transportation Expense / Reimbursement', 21690.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-09-01', N'Labour Charges', 52553.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-09-01', N'P.F. Contribution - Employer', 5471.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-09-01', N'Postage & Courier Expense', 13310.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-09-01', N'Professional & Consultancy Fees', 45000.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-09-01', N'Repairs & Maintenance Exp - Plant & Machinery', 6710.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-2', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-09-01', N'Salaries Expense', 389683.0000, N'sept 26/hpbl123/hpbl123 prov sept.xlsx unit-2', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND sysdate >= '2026-09-01' AND sysdate < DATEADD(month, 1, '2026-09-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'Advocate Fees Expense', 7500.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'C & F  Charges - Export', 517000.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'Factory Expense', 4120.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'Freight Inward Expense (RCM)', 15000.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'Freight Outward Expense (With GST)', 1699000.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'Fumigation Charges', 42320.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'House Keeping Expense', 36000.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'Incentive Expense - Production', 11000.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'Labour Charges', 7300656.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'Medical Expense', 870.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'Mobile & Telephone Expense', 10497.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'P.F. Admin. Charge', 3500.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'P.F. Contribution - Employer', 42243.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'Repairs & Maintenance Exp - Computer & Printers', 7250.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'Repairs & Maintenance Exp - Electric', 38855.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'Testing Charges', 81200.0000, N'HPBL4 SEPT-26', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-09-01', N'Water Charges', 112700.0000, N'HPBL4 SEPT-26', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED' AND sysdate >= '2026-09-01' AND sysdate < DATEADD(month, 1, '2026-09-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'C & F  Charges - Export', 796015.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Canteen Expense - Factory', 18331.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Corporate social responsibility', 408256.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Exchange Rate Difference - Debtors', -25161872.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Factory Expense', 25700.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Freight Outward Expense  - Export', -10733989.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Freight Outward Expense (RCM)', 2603500.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Freight Outward Expense (With GST)', 2001692.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Fumigation Charges', 26550.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'House Keeping Expense', 607773.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Insurance Expenses - Other', 200000.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Job Charges', 518048.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Job Work Income', 25418172.0000, N'KPW-1 Sep TB provision', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Labour Charges', 6836133.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Loading & Unloading Expense', 5000.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Medical Expense', 48270.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Power Expense', 5226033.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Professional & Consultancy Fees', 463319.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Rent Expense - Colony', 1368800.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Repairs & Maintenance Exp - Plant & Machinery', 145626.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Salaries Expense', 21239669.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Security Expenses', 327220.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Vehicle Hire Charges - RCM', 75500.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Vehicle Hire Charges - With Gst', 160000.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Water Charges', 190955.0000, N'KPW-1 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-09-01', N'Xerox Expense', 4500.0000, N'KPW-1 Sep TB provision', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND sysdate >= '2026-09-01' AND sysdate < DATEADD(month, 1, '2026-09-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-09-01', N'C & F  Charges  - Import', 280000.0000, N'KPW-3 Sep provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-09-01', N'C & F  Charges - Export', 100000.0000, N'KPW-3 Sep provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-09-01', N'C & F Charges - Detention Outward', 15000.0000, N'KPW-3 Sep provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-09-01', N'Freight Outward Expense  - Export', 651226.0000, N'KPW-3 Sep provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-09-01', N'Fumigation Charges', 20090.0000, N'KPW-3 Sep provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-09-01', N'Insurance Exp - Vehicles', -28178.0000, N'KPW-3 Sep provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-09-01', N'Mobile & Telephone Expense', 707.0000, N'KPW-3 Sep provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-09-01', N'Rent Expense - Factory', -52800.0000, N'KPW-3 Sep provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-09-01', N'Salaries Expense', 95000.0000, N'KPW-3 Sep provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-09-01', N'Security Expenses', 38000.0000, N'KPW-3 Sep provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-09-01', N'Water Charges', 350.0000, N'KPW-3 Sep provision sheet', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Oswal Extrusion Limited' AND sysdate >= '2026-09-01' AND sysdate < DATEADD(month, 1, '2026-09-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Advocate Fees Expense', 7500.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Bonus Expense', 1450000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'C & F  Charges  - Import', 280000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Canteen Expense - Factory', 45000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Factory Expense', 7500.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Freight Outward Expense (RCM)', 35000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Garden Mainteance Exp', 5000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Incentive Expense - Production', 125000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Insurance Expenses - Marine', 42000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Internal Transportation Expense / Reimbursement', 69000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Job Work Income', -23215675.3928, N'OEL Sep provisions', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Labour Charges', 12573391.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Loading & Unloading Expense', 83000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Mobile & Telephone Expense', 22000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'P.F. Contribution - Employer', 157051.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Postage & Courier Expense', 11000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Power Expense', 2400000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Printing & Stationery Expense', 9000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Professional & Consultancy Fees', 63600.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Repairs & Maintenance Exp - Computer & Printers', 39650.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Salaries Expense', 4080517.5500, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Security Expenses', 170488.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Vehicle Hire Charges - RCM', 380000.0000, N'OEL Sep provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-09-01', N'Water Charges', 200000.0000, N'OEL Sep provisions', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited' AND sysdate >= '2026-09-01' AND sysdate < DATEADD(month, 1, '2026-09-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'C & F  Charges - Export', 1828642.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Canteen Expense - Factory', 157782.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Exchange Rate Difference - Creditor', -1497836.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Exchange Rate Difference - Debtors', -5137440.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Freight outward Expenses (RCM_Withinstate)', 500000.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Income From Wind Mill', -1656100.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Labour Charges', 10143497.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Power Expense', 11247583.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Professional & Consultancy Fees', 128000.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Salaries Expense', 6837717.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Security Expenses', 167817.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Staff Welfare Expense', 82400.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Vehicle Hire Charges - RCM', 23000.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Vehicle Hire Charges - With Gst', 90000.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-09-01', N'Water Charges', 1218300.0000, N'sept 26/PIL1/pnl-provision-template-plastene-india-limited-2026-09 (1).xlsx', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited (Unit -II)' AND sysdate >= '2026-09-01' AND sysdate < DATEADD(month, 1, '2026-09-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-09-01', N'Grand Total', 22846805.0000, N'PIL-2 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-09-01', N'House Keeping Expense', 310000.0000, N'PIL-2 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-09-01', N'Labour Charges', 11550805.0000, N'PIL-2 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-09-01', N'Loading & Unloading Expense', 123000.0000, N'PIL-2 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-09-01', N'Salaries Expense', 10637000.0000, N'PIL-2 Sep TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-09-01', N'Security Expenses', 226000.0000, N'PIL-2 Sep TB provision', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene Polyfilms Limited' AND sysdate >= '2026-09-01' AND sysdate < DATEADD(month, 1, '2026-09-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-09-01', N'Freight Outward Expense (With GST)', 190000.0000, N'PPL Sep PROVISION DATA', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-09-01', N'Job Work Income', -10661475.5200, N'PPL Sep PROVISION DATA', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-09-01', N'Labour Charges', 11244188.0000, N'PPL Sep PROVISION DATA', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-09-01', N'Power Expense', 2200000.0000, N'PPL Sep PROVISION DATA', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED' AND sysdate >= '2026-10-01' AND sysdate < DATEADD(month, 1, '2026-10-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'C & F  Charges - Export', 123505.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Canteen Expense - Factory', 33650.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Corporate social responsibility', 418136.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Equipment Certification Charges', 58174.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Factory Expense', 25700.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Freight Outward Expense (RCM)', 155500.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Freight Outward Expense (With GST)', 3613072.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Fumigation Charges', 26550.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'House Keeping Expense', 530000.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Job Charges', 63007.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Labour Charges', 7038211.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Loading & Unloading Expense', 5000.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Medical Expense', 5000.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Power Expense', 4619638.7800, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Professional & Consultancy Fees', 324080.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Rent Expense - Colony', 1368800.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Repairs & Maintenance Exp - Electric', 92192.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Salaries Expense', 20437640.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Security Expenses', 382161.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Vehicle Hire Charges - RCM', 75500.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Vehicle Hire Charges - With Gst', 160000.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Water Charges', 112820.0000, N'KPW-1 Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-10-01', N'Xerox Expense', 24930.0000, N'KPW-1 Oct provisions', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND sysdate >= '2026-10-01' AND sysdate < DATEADD(month, 1, '2026-10-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-10-01', N'Bonus Expense', 12612.0000, N'KPW-3 Oct provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-10-01', N'C & F  Charges  - Import', 50650.0000, N'KPW-3 Oct provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-10-01', N'C & F  Charges - Export', 287871.0000, N'KPW-3 Oct provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-10-01', N'C & F Charges - Detention Outward', 14000.0000, N'KPW-3 Oct provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-10-01', N'Freight Outward Expense  - Export', 320000.0000, N'KPW-3 Oct provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-10-01', N'Fumigation Charges', 19000.0000, N'KPW-3 Oct provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-10-01', N'Insurance Exp - Vehicles', -24000.0000, N'KPW-3 Oct provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-10-01', N'Mobile & Telephone Expense', 707.0000, N'KPW-3 Oct provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-10-01', N'Rent Expense - Factory', -44000.0000, N'KPW-3 Oct provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-10-01', N'Salaries Expense', 77183.0000, N'KPW-3 Oct provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-10-01', N'Security Expenses', 33451.0000, N'KPW-3 Oct provision sheet', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-10-01', N'Water Charges', 275.0000, N'KPW-3 Oct provision sheet', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Oswal Extrusion Limited' AND sysdate >= '2026-10-01' AND sysdate < DATEADD(month, 1, '2026-10-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Advocate Fees Expense', 7500.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Bonus Expense', 135000.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Canteen Expense - Factory', 98800.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Equipment Certification Charges', -4125.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Fees & Taxes', -11501.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Freight Outward Expense (RCM)', 35000.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Garden Mainteance Exp', 5000.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Insurance Exp - Vehicles', 85300.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Internal Transportation Expense / Reimbursement', 50000.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Job Work Income', -11358966.2067, N'OEL Oct provisions', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Labour Charges', 12503140.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Loading & Unloading Expense', 76254.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Membership & Subscription Fees', -4542.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Mobile & Telephone Expense', 25000.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'P.F. Contribution - Employer', 153351.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Postage & Courier Expense', 15000.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Power Expense', 2500000.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Printing & Stationery Expense', 9000.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Professional & Consultancy Fees', -286883.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Rent Expense - Factory', -360866.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Repairs & Maintenance Exp - Computer & Printers', 39950.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Repairs & Maintenance Exp - Others', 125000.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Salaries Expense', 4180378.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Security Expenses', 171290.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Staff Welfare Expense', -17160.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Vehicle Hire Charges - RCM', 378720.0000, N'OEL Oct provisions', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Oswal Extrusion Limited', '2026-10-01', N'Water Charges', 230420.0000, N'OEL Oct provisions', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited' AND sysdate >= '2026-10-01' AND sysdate < DATEADD(month, 1, '2026-10-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'C & F  Charges - Export', 1538582.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Canteen Expense - Factory', 168284.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Exchange Rate Difference - Creditor', -1497836.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Exchange Rate Difference - Debtors', -5137440.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Freight outward Expenses (RCM_Withinstate)', 500000.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Income From Wind Mill', -779098.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Income');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Labour Charges', 9469516.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Power Expense', 10636757.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Professional & Consultancy Fees', 128000.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Salaries Expense', 6629041.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Security Expenses', 181388.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Staff Welfare Expense', 80510.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Vehicle Hire Charges - RCM', 25000.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Vehicle Hire Charges - With Gst', 90000.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited', '2026-10-01', N'Water Charges', 1177500.0000, N'oct 26/PIL1/pnl-provision-template-plastene-india-limited-2026-10 (1).xlsx', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene India Limited (Unit -II)' AND sysdate >= '2026-10-01' AND sysdate < DATEADD(month, 1, '2026-10-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-10-01', N'C & F  Charges - Export', 1000000.0000, N'PIL-2 Oct TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-10-01', N'Grand Total', 24368111.0000, N'PIL-2 Oct TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-10-01', N'House Keeping Expense', 310000.0000, N'PIL-2 Oct TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-10-01', N'Labour Charges', 11859487.0000, N'PIL-2 Oct TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-10-01', N'Loading & Unloading Expense', 145783.0000, N'PIL-2 Oct TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-10-01', N'Salaries Expense', 10796551.0000, N'PIL-2 Oct TB provision', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene India Limited (Unit -II)', '2026-10-01', N'Security Expenses', 256290.0000, N'PIL-2 Oct TB provision', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'Plastene Polyfilms Limited' AND sysdate >= '2026-10-01' AND sysdate < DATEADD(month, 1, '2026-10-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-10-01', N'Bonus Exp.', 2155313.0000, N'PPL Oct PROVISION DATA', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-10-01', N'Freight Outward Expense (With GST)', 175000.0000, N'PPL Oct PROVISION DATA', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-10-01', N'Labour Charges', 11827646.0000, N'PPL Oct PROVISION DATA', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'Plastene Polyfilms Limited', '2026-10-01', N'Power Expense', 1672000.0000, N'PPL Oct PROVISION DATA', N'Expense');

COMMIT;
-- ROLLBACK;
