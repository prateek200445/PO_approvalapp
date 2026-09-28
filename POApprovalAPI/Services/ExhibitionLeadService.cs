using ClosedXML.Excel;
using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class ExhibitionLeadService
{
    public static readonly string[] FormTypes = ["DealerDistributor", "ProductInquiry"];

    public static readonly string[] ProductInquiries =
    [
        "Shade Net",
        "Insect Net",
        "Weed Control / Weed Barrier Fabric",
        "Greenhouse Skirting / Apron",
        "Greenhouse Cladding Film / Polyfilm",
        "Planter Bags / Grow Bags",
    ];

    private const string EnsureTableSql = @"
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
END";

    private readonly DatabaseService _database;

    public ExhibitionLeadService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<ExhibitionLead> CreateAsync(ExhibitionLeadCreateRequest request)
    {
        var lead = Normalize(request);
        await EnsureTableAsync();

        using var connection = _database.CreateConnection();
        return await connection.QuerySingleAsync<ExhibitionLead>(@"
INSERT INTO dbo.ExhibitionLeads
    (FormType, CompanyName, PersonName, ContactNumber, Email, Address, PostalCode, ProductInquiry, Quantity, ExhibitionName)
OUTPUT
    INSERTED.Id,
    INSERTED.FormType,
    INSERTED.CompanyName,
    INSERTED.PersonName,
    INSERTED.ContactNumber,
    INSERTED.Email,
    INSERTED.Address,
    INSERTED.PostalCode,
    INSERTED.ProductInquiry,
    INSERTED.Quantity,
    INSERTED.ExhibitionName,
    INSERTED.CreatedAt
VALUES
    (@FormType, @CompanyName, @PersonName, @ContactNumber, @Email, @Address, @PostalCode, @ProductInquiry, @Quantity, @ExhibitionName)",
            lead);
    }

    public async Task<IReadOnlyList<ExhibitionLead>> ListAsync(string? formType, string? exhibitionName)
    {
        var form = NullIfEmpty(formType);
        if (form != null && !FormTypes.Contains(form, StringComparer.Ordinal))
            throw new ArgumentException("FormType must be DealerDistributor or ProductInquiry.");

        await EnsureTableAsync();
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<ExhibitionLead>(@"
SELECT
    Id, FormType, CompanyName, PersonName, ContactNumber, Email, Address,
    PostalCode, ProductInquiry, Quantity, ExhibitionName, CreatedAt
FROM dbo.ExhibitionLeads
WHERE (@FormType IS NULL OR FormType = @FormType)
  AND (@ExhibitionName IS NULL OR ExhibitionName = @ExhibitionName)
ORDER BY CreatedAt DESC, Id DESC",
            new { FormType = form, ExhibitionName = NullIfEmpty(exhibitionName) });
        return rows.ToList();
    }

    public async Task<(byte[] Bytes, string FileName)> ExportExcelAsync(string? formType, string? exhibitionName)
    {
        var rows = await ListAsync(formType, exhibitionName);
        var dealer = string.Equals(formType, "DealerDistributor", StringComparison.Ordinal);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(dealer ? "Dealer Distributor" : "Product Inquiry");
        var headers = dealer
            ? new[] { "Company Name", "Person Name", "Contact Number", "Email", "Address", "Postal Code", "Created At" }
            : new[] { "Name", "Contact Number", "Email", "Product Inquiry", "Quantity", "Created At" };
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var rowIndex = 2;
        foreach (var row in rows)
        {
            if (dealer)
            {
                sheet.Cell(rowIndex, 1).Value = row.CompanyName ?? "";
                sheet.Cell(rowIndex, 2).Value = row.PersonName;
                sheet.Cell(rowIndex, 3).Value = row.ContactNumber;
                sheet.Cell(rowIndex, 4).Value = row.Email ?? "";
                sheet.Cell(rowIndex, 5).Value = row.Address ?? "";
                sheet.Cell(rowIndex, 6).Value = row.PostalCode ?? "";
                sheet.Cell(rowIndex, 7).Value = row.CreatedAt;
            }
            else
            {
                sheet.Cell(rowIndex, 1).Value = row.PersonName;
                sheet.Cell(rowIndex, 2).Value = row.ContactNumber;
                sheet.Cell(rowIndex, 3).Value = row.Email ?? "";
                sheet.Cell(rowIndex, 4).Value = row.ProductInquiry ?? "";
                if (row.Quantity.HasValue)
                    sheet.Cell(rowIndex, 5).Value = row.Quantity.Value;
                sheet.Cell(rowIndex, 6).Value = row.CreatedAt;
            }
            rowIndex++;
        }

        sheet.Row(1).Style.Font.Bold = true;
        sheet.Column(headers.Length).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var filePrefix = dealer ? "dealer-distributor" : "product-inquiry";
        return (stream.ToArray(), $"{filePrefix}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    private async Task EnsureTableAsync()
    {
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(EnsureTableSql);
    }

    private static ExhibitionLead Normalize(ExhibitionLeadCreateRequest request)
    {
        var formType = (request.FormType ?? "").Trim();
        if (!FormTypes.Contains(formType, StringComparer.Ordinal))
            throw new ArgumentException("FormType must be DealerDistributor or ProductInquiry.");

        var personName = (request.PersonName ?? "").Trim();
        if (personName.Length == 0)
            throw new ArgumentException("PersonName is required.");

        var contactNumber = (request.ContactNumber ?? "").Trim();
        if (contactNumber.Length == 0)
            throw new ArgumentException("ContactNumber is required.");

        var productInquiry = NullIfEmpty(request.ProductInquiry);
        if (productInquiry != null && !ProductInquiries.Contains(productInquiry, StringComparer.Ordinal))
            throw new ArgumentException("ProductInquiry is not an allowed value.");

        if (request.Quantity is < 0)
            throw new ArgumentException("Quantity cannot be negative.");

        return new ExhibitionLead
        {
            FormType = formType,
            CompanyName = NullIfEmpty(request.CompanyName),
            PersonName = personName,
            ContactNumber = contactNumber,
            Email = NullIfEmpty(request.Email),
            Address = NullIfEmpty(request.Address),
            PostalCode = NullIfEmpty(request.PostalCode),
            ProductInquiry = productInquiry,
            Quantity = request.Quantity,
            ExhibitionName = NullIfEmpty(request.ExhibitionName),
        };
    }

    private static string? NullIfEmpty(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
