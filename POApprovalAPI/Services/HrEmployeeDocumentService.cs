using Dapper;

namespace POApprovalAPI.Services;

/// <summary>
/// Employee photo lives in ERP payroll <c>empinfo.images</c> / <c>imagename</c> (what the ERP form shows).
/// Other documents (Aadhaar, PAN, certificates …) have no ERP table, so they are kept in the portal DB.
/// </summary>
public sealed class HrEmployeeDocumentService
{
    public const int MaxPhotoBytes = 2 * 1024 * 1024;
    public const int MaxDocumentBytes = 5 * 1024 * 1024;

    public static readonly string[] DocumentTypes =
    [
        "Aadhaar Card",
        "PAN Card",
        "Bank Passbook / Cheque",
        "Educational Certificate",
        "Experience / Relieving Letter",
        "Resume",
        "Offer / Appointment Letter",
        "Address Proof",
        "Other",
    ];

    private readonly DatabaseService _database;
    private static bool _tableReady;

    public HrEmployeeDocumentService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<string> SavePhotoAsync(string empCode, byte[] content)
    {
        empCode = NormalizeCode(empCode);
        if (content.Length == 0)
            throw new InvalidOperationException("Photo file is empty.");
        if (content.Length > MaxPhotoBytes)
            throw new InvalidOperationException("Photo must be 2 MB or smaller.");
        if (DetectImageType(content) is null)
            throw new InvalidOperationException("Photo must be a JPG, PNG or BMP image.");

        using var connection = _database.CreatePayrollLoginEntryConnection();
        var updated = await connection.ExecuteAsync(
            "UPDATE empinfo SET images = @Content, imagename = @Name WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode",
            new { Content = content, Name = empCode.Length > 50 ? empCode[..50] : empCode, EmpCode = empCode });
        if (updated == 0)
            throw new InvalidOperationException($"Employee {empCode} not found in ERP.");
        return $"Photo saved for {empCode}.";
    }

    public async Task<(byte[] Content, string ContentType)?> GetPhotoAsync(string empCode)
    {
        empCode = NormalizeCode(empCode);
        using var connection = _database.CreatePayrollLoginEntryConnection();
        var bytes = await connection.QueryFirstOrDefaultAsync<byte[]?>(
            "SELECT TOP 1 CAST(images AS varbinary(max)) FROM empinfo WITH (NOLOCK) WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode",
            new { EmpCode = empCode });
        if (bytes is null || bytes.Length == 0)
            return null;
        var type = DetectImageType(bytes);
        return type is null ? null : (bytes, type);
    }

    public async Task<List<HrEmployeeDocumentDto>> ListDocumentsAsync(string empCode)
    {
        empCode = NormalizeCode(empCode);
        await EnsureTableAsync();
        using var connection = _database.CreateConnection();
        return (await connection.QueryAsync<HrEmployeeDocumentDto>(
            """
            SELECT DocId, EmpCode, DocType, FileName, ContentType, FileSize, UploadedBy, UploadedAt
            FROM dbo.HrEmployeeDocument
            WHERE EmpCode = @EmpCode AND IsDeleted = 0
            ORDER BY UploadedAt DESC, DocId DESC
            """,
            new { EmpCode = empCode })).ToList();
    }

    public async Task<HrEmployeeDocumentDto> SaveDocumentAsync(
        string empCode, string docType, string fileName, byte[] content, string uploadedBy)
    {
        empCode = NormalizeCode(empCode);
        docType = (docType ?? "").Trim();
        if (!DocumentTypes.Contains(docType, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose a valid document type.");
        docType = DocumentTypes.First(t => string.Equals(t, docType, StringComparison.OrdinalIgnoreCase));
        if (content.Length == 0)
            throw new InvalidOperationException("Document file is empty.");
        if (content.Length > MaxDocumentBytes)
            throw new InvalidOperationException("Each document must be 5 MB or smaller.");
        var contentType = DetectDocumentType(content)
            ?? throw new InvalidOperationException("Documents must be PDF, JPG or PNG files.");

        await EnsureEmployeeExistsAsync(empCode);
        await EnsureTableAsync();

        var safeName = Path.GetFileName(fileName ?? "").Trim();
        if (safeName.Length == 0)
            safeName = "document";
        if (safeName.Length > 255)
            safeName = safeName[^255..];

        using var connection = _database.CreateConnection();
        var docId = await connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO dbo.HrEmployeeDocument (EmpCode, DocType, FileName, ContentType, FileSize, Content, UploadedBy)
            VALUES (@EmpCode, @DocType, @FileName, @ContentType, @FileSize, @Content, @UploadedBy);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """,
            new
            {
                EmpCode = empCode,
                DocType = docType,
                FileName = safeName,
                ContentType = contentType,
                FileSize = content.Length,
                Content = content,
                UploadedBy = Truncate(uploadedBy, 100),
            });

        return new HrEmployeeDocumentDto
        {
            DocId = docId,
            EmpCode = empCode,
            DocType = docType,
            FileName = safeName,
            ContentType = contentType,
            FileSize = content.Length,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.Now,
        };
    }

    public async Task<(byte[] Content, string ContentType, string FileName)?> GetDocumentAsync(string empCode, int docId)
    {
        empCode = NormalizeCode(empCode);
        await EnsureTableAsync();
        using var connection = _database.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<DocumentContentRow>(
            """
            SELECT Content, ContentType, FileName FROM dbo.HrEmployeeDocument
            WHERE DocId = @DocId AND EmpCode = @EmpCode AND IsDeleted = 0
            """,
            new { DocId = docId, EmpCode = empCode });
        return row?.Content is null ? null : (row.Content, row.ContentType, row.FileName);
    }

    public async Task<bool> DeleteDocumentAsync(string empCode, int docId, string deletedBy)
    {
        empCode = NormalizeCode(empCode);
        await EnsureTableAsync();
        using var connection = _database.CreateConnection();
        var n = await connection.ExecuteAsync(
            """
            UPDATE dbo.HrEmployeeDocument
            SET IsDeleted = 1, DeletedBy = @DeletedBy, DeletedAt = GETDATE()
            WHERE DocId = @DocId AND EmpCode = @EmpCode AND IsDeleted = 0
            """,
            new { DocId = docId, EmpCode = empCode, DeletedBy = Truncate(deletedBy, 100) });
        return n > 0;
    }

    private async Task EnsureEmployeeExistsAsync(string empCode)
    {
        using var connection = _database.CreatePayrollLoginEntryConnection();
        var exists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM empinfo WITH (NOLOCK) WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode",
            new { EmpCode = empCode });
        if (exists == 0)
            throw new InvalidOperationException($"Employee {empCode} not found in ERP.");
    }

    private async Task EnsureTableAsync()
    {
        if (_tableReady)
            return;
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(
            """
            IF OBJECT_ID('dbo.HrEmployeeDocument', 'U') IS NULL
            BEGIN
              CREATE TABLE dbo.HrEmployeeDocument (
                DocId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_HrEmployeeDocument PRIMARY KEY,
                EmpCode varchar(50) NOT NULL,
                DocType varchar(60) NOT NULL,
                FileName nvarchar(255) NOT NULL,
                ContentType varchar(100) NOT NULL,
                FileSize int NOT NULL,
                Content varbinary(max) NOT NULL,
                UploadedBy varchar(100) NULL,
                UploadedAt datetime NOT NULL CONSTRAINT DF_HrEmployeeDocument_UploadedAt DEFAULT (GETDATE()),
                IsDeleted bit NOT NULL CONSTRAINT DF_HrEmployeeDocument_IsDeleted DEFAULT (0),
                DeletedBy varchar(100) NULL,
                DeletedAt datetime NULL
              );
              CREATE INDEX IX_HrEmployeeDocument_EmpCode ON dbo.HrEmployeeDocument (EmpCode, IsDeleted);
            END
            """);
        _tableReady = true;
    }

    private static string NormalizeCode(string empCode)
    {
        var code = (empCode ?? "").Trim();
        if (code.Length == 0)
            throw new InvalidOperationException("Employee code is required.");
        if (code.Length > 50)
            throw new InvalidOperationException("Employee code is too long.");
        return code;
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var v = value.Trim();
        return v.Length > max ? v[..max] : v;
    }

    internal static string? DetectImageType(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            return "image/jpeg";
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
            return "image/png";
        if (b.Length >= 2 && b[0] == 0x42 && b[1] == 0x4D)
            return "image/bmp";
        return null;
    }

    internal static string? DetectDocumentType(byte[] b)
    {
        if (b.Length >= 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46)
            return "application/pdf";
        var image = DetectImageType(b);
        return image is "image/jpeg" or "image/png" ? image : null;
    }

    private sealed class DocumentContentRow
    {
        public byte[]? Content { get; set; }
        public string ContentType { get; set; } = "";
        public string FileName { get; set; } = "";
    }
}

public sealed class HrEmployeeDocumentDto
{
    public int DocId { get; set; }
    public string EmpCode { get; set; } = "";
    public string DocType { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public int FileSize { get; set; }
    public string? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
}
