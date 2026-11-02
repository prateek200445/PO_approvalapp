/* Factory gate register. One row per visit; exit updates the same row. */
IF OBJECT_ID(N'dbo.GateMaster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GateMaster (
        GateId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GateMaster PRIMARY KEY,
        FactoryCode NVARCHAR(20) NOT NULL,
        FactoryName NVARCHAR(200) NOT NULL,
        GateCode NVARCHAR(20) NOT NULL,
        GateName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_GateMaster_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_GateMaster_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT UQ_GateMaster_FactoryGate UNIQUE (FactoryCode, GateCode)
    );
END
GO

IF OBJECT_ID(N'dbo.GateVehiclePurposeMaster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GateVehiclePurposeMaster (
        PurposeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GateVehiclePurposeMaster PRIMARY KEY,
        PurposeCode VARCHAR(40) NOT NULL CONSTRAINT UQ_GateVehiclePurposeMaster_Code UNIQUE,
        PurposeName NVARCHAR(100) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_GateVehiclePurpose_Sort DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_GateVehiclePurpose_Active DEFAULT (1)
    );
END
GO

IF OBJECT_ID(N'dbo.GateEntrySequence', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GateEntrySequence (
        FactoryCode NVARCHAR(20) NOT NULL,
        EntryDate DATE NOT NULL,
        LastSeq INT NOT NULL,
        CONSTRAINT PK_GateEntrySequence PRIMARY KEY (FactoryCode, EntryDate)
    );
END
GO

IF OBJECT_ID(N'dbo.GateVehicleEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GateVehicleEntries (
        EntryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GateVehicleEntries PRIMARY KEY,
        GateEntryNumber NVARCHAR(40) NOT NULL CONSTRAINT UQ_GateVehicleEntries_Number UNIQUE,
        FactoryCode NVARCHAR(20) NOT NULL,
        GateId INT NOT NULL,
        VehicleNumber NVARCHAR(20) NOT NULL,
        DriverName NVARCHAR(120) NOT NULL,
        DriverMobile NVARCHAR(20) NULL,
        PartyName NVARCHAR(200) NULL,
        PurposeId INT NOT NULL,
        Remarks NVARCHAR(500) NULL,
        Status VARCHAR(10) NOT NULL,
        EntryAt DATETIME2 NOT NULL CONSTRAINT DF_GateVehicleEntries_EntryAt DEFAULT (SYSDATETIME()),
        EntryUserId NVARCHAR(80) NOT NULL,
        ExitAt DATETIME2 NULL,
        ExitUserId NVARCHAR(80) NULL,
        ExitGateId INT NULL,
        ExitRemarks NVARCHAR(500) NULL,
        DurationMinutes INT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_GateVehicleEntries_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_GateVehicleEntries_Gate FOREIGN KEY (GateId) REFERENCES dbo.GateMaster (GateId),
        CONSTRAINT FK_GateVehicleEntries_ExitGate FOREIGN KEY (ExitGateId) REFERENCES dbo.GateMaster (GateId),
        CONSTRAINT FK_GateVehicleEntries_Purpose FOREIGN KEY (PurposeId) REFERENCES dbo.GateVehiclePurposeMaster (PurposeId),
        CONSTRAINT CK_GateVehicleEntries_Status CHECK (Status IN (N'INSIDE', N'EXITED'))
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_GateVehicleEntries_ActiveVehicle'
      AND object_id = OBJECT_ID(N'dbo.GateVehicleEntries')
)
    CREATE UNIQUE INDEX UX_GateVehicleEntries_ActiveVehicle
        ON dbo.GateVehicleEntries (FactoryCode, VehicleNumber)
        WHERE Status = 'INSIDE';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_GateVehicleEntries_FactoryStatus'
      AND object_id = OBJECT_ID(N'dbo.GateVehicleEntries')
)
    CREATE INDEX IX_GateVehicleEntries_FactoryStatus
        ON dbo.GateVehicleEntries (FactoryCode, Status, EntryAt);
GO

IF OBJECT_ID(N'dbo.GateVehicleEntryAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GateVehicleEntryAudit (
        AuditId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GateVehicleEntryAudit PRIMARY KEY,
        EntryId BIGINT NOT NULL,
        Action VARCHAR(20) NOT NULL,
        ChangedBy NVARCHAR(80) NOT NULL,
        ChangedAt DATETIME2 NOT NULL CONSTRAINT DF_GateVehicleEntryAudit_ChangedAt DEFAULT (SYSDATETIME()),
        FieldName NVARCHAR(80) NULL,
        OldValue NVARCHAR(500) NULL,
        NewValue NVARCHAR(500) NULL,
        Reason NVARCHAR(500) NULL,
        CONSTRAINT FK_GateVehicleEntryAudit_Entry FOREIGN KEY (EntryId) REFERENCES dbo.GateVehicleEntries (EntryId)
    );
    CREATE INDEX IX_GateVehicleEntryAudit_EntryId ON dbo.GateVehicleEntryAudit (EntryId, AuditId);
END
GO

MERGE dbo.GateVehiclePurposeMaster AS t
USING (VALUES
    ('MATERIAL_IN', N'Material Inward', 1),
    ('FG_DISPATCH', N'Finished Goods Dispatch', 2),
    ('MATERIAL_OUT', N'Material Outward', 3),
    ('VENDOR_VISIT', N'Supplier/Vendor Visit', 4),
    ('CUSTOMER_VISIT', N'Customer Visit', 5),
    ('EMPLOYEE', N'Employee Vehicle', 6),
    ('INTER_FACTORY', N'Inter-factory Transfer', 7),
    ('MAINTENANCE', N'Maintenance/Service', 8),
    ('OTHER', N'Other', 9)
) AS s (PurposeCode, PurposeName, SortOrder)
ON t.PurposeCode = s.PurposeCode
WHEN NOT MATCHED THEN
    INSERT (PurposeCode, PurposeName, SortOrder, IsActive)
    VALUES (s.PurposeCode, s.PurposeName, s.SortOrder, 1);
GO

MERGE dbo.GateMaster AS t
USING (VALUES
    (N'KPW', N'K.P. Woven Private Limited', N'MAIN', N'Main Gate'),
    (N'KPW', N'K.P. Woven Private Limited', N'WB', N'Weighbridge'),
    (N'OEL', N'Oswal Extrusion Limited', N'MAIN', N'Main Gate'),
    (N'PIA', N'Plastene India Limited (Unit-II)', N'MAIN', N'Main Gate'),
    (N'PIL', N'Plastene India Limited', N'MAIN', N'Main Gate'),
    (N'PPL', N'Plastene Polyfilms Limited', N'MAIN', N'Main Gate'),
    (N'HPBL', N'HCP Plastene Bulkpack Ltd', N'MAIN', N'Main Gate'),
    (N'HPBL4', N'HCP Plastene Bulkpack Ltd (Unit-IV)', N'MAIN', N'Main Gate'),
    (N'HPBL5', N'HCP Plastene Bulkpack Ltd (Unit-III)', N'MAIN', N'Main Gate')
) AS s (FactoryCode, FactoryName, GateCode, GateName)
ON t.FactoryCode = s.FactoryCode AND t.GateCode = s.GateCode
WHEN NOT MATCHED THEN
    INSERT (FactoryCode, FactoryName, GateCode, GateName, IsActive)
    VALUES (s.FactoryCode, s.FactoryName, s.GateCode, s.GateName, 1);
