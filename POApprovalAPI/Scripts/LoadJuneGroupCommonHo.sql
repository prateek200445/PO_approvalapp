/* Generated from public/june 26 Group EBIDTA.xlsx
   Apr-26 / May-26 / Jun-26 rows 78-79 (Common Expenses, HO Expenses), MONTH column.
   Amounts are already in lacs. Upserts dbo.PnlOverhead.
   June Common/HO on Jun-26 is blank (0) for every unit — month June EBITDA will not move.
   April and May allocations are loaded and affect YTD only when viewing Jun 2026.
   Does not load July+ or consolidation columns.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

IF OBJECT_ID(N'dbo.PnlOverhead', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PnlOverhead (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyName VARCHAR(150) NOT NULL,
        OverheadMonth DATE NOT NULL,
        CommonLacs FLOAT NOT NULL CONSTRAINT DF_PnlOverhead_Common DEFAULT (0),
        HoLacs FLOAT NOT NULL CONSTRAINT DF_PnlOverhead_Ho DEFAULT (0),
        UploadedBy VARCHAR(100) NULL,
        UploadedAt DATETIME NOT NULL CONSTRAINT DF_PnlOverhead_Uploaded DEFAULT (GETDATE()),
        Remarks VARCHAR(500) NULL,
        CONSTRAINT UQ_PnlOverhead UNIQUE (CompanyName, OverheadMonth)
    );
END

-- Apr-26 PIL I
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited' AND OverheadMonth = '2026-04-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 2.615662, HoLacs = 4.008249, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited' AND OverheadMonth = '2026-04-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Plastene India Limited', '2026-04-01', 2.615662, 4.008249, GETDATE());

-- Apr-26 PIL II
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited (Unit -II)' AND OverheadMonth = '2026-04-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 4.359436, HoLacs = 6.680415, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited (Unit -II)' AND OverheadMonth = '2026-04-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', '2026-04-01', 4.359436, 6.680415, GETDATE());

-- Apr-26 HPBL4
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND OverheadMonth = '2026-04-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 2.615662, HoLacs = 4.008249, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND OverheadMonth = '2026-04-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-04-01', 2.615662, 4.008249, GETDATE());

-- Apr-26 OSWAL - KASEZ
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Oswal Extrusion Limited' AND OverheadMonth = '2026-04-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = -18.745575, HoLacs = 4.676291, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Oswal Extrusion Limited' AND OverheadMonth = '2026-04-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', '2026-04-01', -18.745575, 4.676291, GETDATE());

-- Apr-26 PLASTENE POLYFILM LTD.
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene Polyfilms Limited' AND OverheadMonth = '2026-04-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 4.359436, HoLacs = 6.680415, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene Polyfilms Limited' AND OverheadMonth = '2026-04-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', '2026-04-01', 4.359436, 6.680415, GETDATE());

-- Apr-26 K P WOVEN PVT LTD - UNIT - I
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'K.P. WOVEN PRIVATE LIMITED' AND OverheadMonth = '2026-04-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 4.795380, HoLacs = 7.348457, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'K.P. WOVEN PRIVATE LIMITED' AND OverheadMonth = '2026-04-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-04-01', 4.795380, 7.348457, GETDATE());

-- Apr-26 K P WOVEN PVT LTD - UNIT -III
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND OverheadMonth = '2026-04-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND OverheadMonth = '2026-04-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-04-01', 0.000000, 0.000000, GETDATE());

-- Apr-26 HCP PLASTENE BULKPACK LTD. - UNIT - I
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd' AND OverheadMonth = '2026-04-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd' AND OverheadMonth = '2026-04-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', '2026-04-01', 0.000000, 0.000000, GETDATE());

-- Apr-26 HCP PLASTENE BULKPACK LTD. - UNIT - II
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND OverheadMonth = '2026-04-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND OverheadMonth = '2026-04-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-04-01', 0.000000, 0.000000, GETDATE());

-- Apr-26 HCP PLASTENE BULKPACK LTD. - UNIT - III
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND OverheadMonth = '2026-04-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND OverheadMonth = '2026-04-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', '2026-04-01', 0.000000, 0.000000, GETDATE());

-- Apr-26 PIL HO
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited - HO' AND OverheadMonth = '2026-04-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = -33.402077, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited - HO' AND OverheadMonth = '2026-04-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Plastene India Limited - HO', '2026-04-01', 0.000000, -33.402077, GETDATE());

-- May-26 PIL I
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited' AND OverheadMonth = '2026-05-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 2.647823, HoLacs = 6.322791, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited' AND OverheadMonth = '2026-05-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Plastene India Limited', '2026-05-01', 2.647823, 6.322791, GETDATE());

-- May-26 PIL II
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited (Unit -II)' AND OverheadMonth = '2026-05-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 4.413038, HoLacs = 10.537985, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited (Unit -II)' AND OverheadMonth = '2026-05-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', '2026-05-01', 4.413038, 10.537985, GETDATE());

-- May-26 HPBL4
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND OverheadMonth = '2026-05-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 2.647823, HoLacs = 6.322791, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND OverheadMonth = '2026-05-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-05-01', 2.647823, 6.322791, GETDATE());

-- May-26 OSWAL - KASEZ
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Oswal Extrusion Limited' AND OverheadMonth = '2026-05-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = -18.976063, HoLacs = 7.376590, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Oswal Extrusion Limited' AND OverheadMonth = '2026-05-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', '2026-05-01', -18.976063, 7.376590, GETDATE());

-- May-26 PLASTENE POLYFILM LTD.
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene Polyfilms Limited' AND OverheadMonth = '2026-05-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 4.413038, HoLacs = 10.537985, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene Polyfilms Limited' AND OverheadMonth = '2026-05-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', '2026-05-01', 4.413038, 10.537985, GETDATE());

-- May-26 K P WOVEN PVT LTD - UNIT - I
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'K.P. WOVEN PRIVATE LIMITED' AND OverheadMonth = '2026-05-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 4.854342, HoLacs = 11.591784, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'K.P. WOVEN PRIVATE LIMITED' AND OverheadMonth = '2026-05-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-05-01', 4.854342, 11.591784, GETDATE());

-- May-26 K P WOVEN PVT LTD - UNIT -III
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND OverheadMonth = '2026-05-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND OverheadMonth = '2026-05-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-05-01', 0.000000, 0.000000, GETDATE());

-- May-26 HCP PLASTENE BULKPACK LTD. - UNIT - I
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd' AND OverheadMonth = '2026-05-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd' AND OverheadMonth = '2026-05-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', '2026-05-01', 0.000000, 0.000000, GETDATE());

-- May-26 HCP PLASTENE BULKPACK LTD. - UNIT - II
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND OverheadMonth = '2026-05-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND OverheadMonth = '2026-05-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-05-01', 0.000000, 0.000000, GETDATE());

-- May-26 HCP PLASTENE BULKPACK LTD. - UNIT - III
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND OverheadMonth = '2026-05-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND OverheadMonth = '2026-05-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', '2026-05-01', 0.000000, 0.000000, GETDATE());

-- May-26 PIL HO
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited - HO' AND OverheadMonth = '2026-05-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = -52.689926, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited - HO' AND OverheadMonth = '2026-05-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Plastene India Limited - HO', '2026-05-01', 0.000000, -52.689926, GETDATE());

-- Jun-26 PIL I
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited' AND OverheadMonth = '2026-06-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited' AND OverheadMonth = '2026-06-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Plastene India Limited', '2026-06-01', 0.000000, 0.000000, GETDATE());

-- Jun-26 PIL II
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited (Unit -II)' AND OverheadMonth = '2026-06-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited (Unit -II)' AND OverheadMonth = '2026-06-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Plastene India Limited (Unit -II)', '2026-06-01', 0.000000, 0.000000, GETDATE());

-- Jun-26 HPBL4
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND OverheadMonth = '2026-06-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND OverheadMonth = '2026-06-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-06-01', 0.000000, 0.000000, GETDATE());

-- Jun-26 OSWAL - KASEZ
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Oswal Extrusion Limited' AND OverheadMonth = '2026-06-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Oswal Extrusion Limited' AND OverheadMonth = '2026-06-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Oswal Extrusion Limited', '2026-06-01', 0.000000, 0.000000, GETDATE());

-- Jun-26 PLASTENE POLYFILM LTD.
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene Polyfilms Limited' AND OverheadMonth = '2026-06-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene Polyfilms Limited' AND OverheadMonth = '2026-06-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Plastene Polyfilms Limited', '2026-06-01', 0.000000, 0.000000, GETDATE());

-- Jun-26 K P WOVEN PVT LTD - UNIT - I
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'K.P. WOVEN PRIVATE LIMITED' AND OverheadMonth = '2026-06-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'K.P. WOVEN PRIVATE LIMITED' AND OverheadMonth = '2026-06-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED', '2026-06-01', 0.000000, 0.000000, GETDATE());

-- Jun-26 K P WOVEN PVT LTD - UNIT -III
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND OverheadMonth = '2026-06-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)' AND OverheadMonth = '2026-06-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'K.P. WOVEN PRIVATE LIMITED (UNIT-III)', '2026-06-01', 0.000000, 0.000000, GETDATE());

-- Jun-26 HCP PLASTENE BULKPACK LTD. - UNIT - I
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd' AND OverheadMonth = '2026-06-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd' AND OverheadMonth = '2026-06-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd', '2026-06-01', 0.000000, 0.000000, GETDATE());

-- Jun-26 HCP PLASTENE BULKPACK LTD. - UNIT - II
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND OverheadMonth = '2026-06-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND OverheadMonth = '2026-06-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-06-01', 0.000000, 0.000000, GETDATE());

-- Jun-26 HCP PLASTENE BULKPACK LTD. - UNIT - III
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND OverheadMonth = '2026-06-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND OverheadMonth = '2026-06-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'HCP Plastene Bulkpack Ltd (Unit - III)', '2026-06-01', 0.000000, 0.000000, GETDATE());

-- Jun-26 PIL HO
IF EXISTS (SELECT 1 FROM dbo.PnlOverhead WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited - HO' AND OverheadMonth = '2026-06-01')
    UPDATE dbo.PnlOverhead
    SET CommonLacs = 0.000000, HoLacs = 0.000000, UploadedAt = GETDATE()
    WHERE LTRIM(RTRIM(CompanyName)) = N'Plastene India Limited - HO' AND OverheadMonth = '2026-06-01';
ELSE
    INSERT INTO dbo.PnlOverhead (CompanyName, OverheadMonth, CommonLacs, HoLacs, UploadedAt)
    VALUES (N'Plastene India Limited - HO', '2026-06-01', 0.000000, 0.000000, GETDATE());

COMMIT;
-- ROLLBACK;
