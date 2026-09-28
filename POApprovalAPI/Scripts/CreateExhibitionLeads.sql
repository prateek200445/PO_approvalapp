/* Temporary exhibition QR-form submissions. Isolated from ERP tables. */
IF OBJECT_ID(N'dbo.ExhibitionLeads', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExhibitionLeads (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExhibitionLeads PRIMARY KEY,
        FormType VARCHAR(30) NOT NULL,
        CompanyName NVARCHAR(200) NULL,
        PersonName NVARCHAR(200) NOT NULL,
        ContactNumber VARCHAR(30) NOT NULL,
        Email NVARCHAR(200) NULL,
        Address NVARCHAR(500) NULL,
        PostalCode VARCHAR(20) NULL,
        ProductInquiry NVARCHAR(120) NULL,
        Quantity DECIMAL(18,3) NULL,
        ExhibitionName NVARCHAR(200) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ExhibitionLeads_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT CK_ExhibitionLeads_FormType CHECK (FormType IN (N'DealerDistributor', N'ProductInquiry')),
        CONSTRAINT CK_ExhibitionLeads_ProductInquiry CHECK (
            ProductInquiry IS NULL OR ProductInquiry IN (
                N'Shade Net',
                N'Insect Net',
                N'Weed Control / Weed Barrier Fabric',
                N'Greenhouse Skirting / Apron',
                N'Greenhouse Cladding Film / Polyfilm',
                N'Planter Bags / Grow Bags'
            )
        )
    );
END
