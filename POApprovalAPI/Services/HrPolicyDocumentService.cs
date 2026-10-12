using Dapper;

namespace POApprovalAPI.Services;

/// <summary>
/// Company-wide HR documents (HR policies, code of conduct, organogram). HR uploads; every logged-in user can read.
/// A document is either group-wide (no company) or for one company. Files live in the portal DB.
/// </summary>
public sealed class HrPolicyDocumentService
{
    public const int MaxBytes = 20 * 1024 * 1024;

    public static readonly string[] Categories = ["HR Policies", "Code of Conduct", "Organogram"];

    private static readonly Dictionary<string, string> OfficeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    };

    private readonly DatabaseService _database;
    private static bool _tableReady;

    public HrPolicyDocumentService(DatabaseService database)
    {
        _database = database;
    }

    /// <param name="company">Null = every document (HR); otherwise group-wide documents plus that company's.</param>
    public async Task<List<HrPolicyDocumentDto>> ListAsync(string? company, bool all)
    {
        await EnsureTableAsync();
        using var connection = _database.CreateConnection();
        return (await connection.QueryAsync<HrPolicyDocumentDto>(
            """
            SELECT DocId, Category, Title, Company, Description, FileName, ContentType, FileSize, UploadedBy, UploadedAt
            FROM dbo.HrPolicyDocument
            WHERE IsDeleted = 0 AND (@All = 1 OR Company IS NULL OR Company = @Company)
            ORDER BY Category, Title, UploadedAt DESC
            """,
            new { All = all, Company = company ?? "" })).ToList();
    }

    public async Task<HrPolicyDocumentDto> SaveAsync(string category, string title, string? company, string? description,
        string fileName, byte[] content, string uploadedBy)
    {
        category = Categories.FirstOrDefault(c => string.Equals(c, (category ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Choose a category: {string.Join(", ", Categories)}.");
        title = (title ?? "").Trim();
        if (title.Length < 3)
            throw new InvalidOperationException("Enter a title for the document.");
        if (title.Length > 200) title = title[..200];
        if (content.Length == 0)
            throw new InvalidOperationException("The file is empty.");
        if (content.Length > MaxBytes)
            throw new InvalidOperationException("Each document must be 20 MB or smaller.");

        var safeName = Path.GetFileName(fileName ?? "").Trim();
        if (safeName.Length == 0) safeName = "document";
        if (safeName.Length > 255) safeName = safeName[^255..];
        var contentType = DetectType(content, safeName)
            ?? throw new InvalidOperationException("Upload a PDF, JPG, PNG, Word (.docx), Excel (.xlsx) or PowerPoint (.pptx) file.");

        company = string.IsNullOrWhiteSpace(company) ? null : company.Trim();
        if (company is { Length: > 150 }) company = company[..150];
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (description is { Length: > 500 }) description = description[..500];

        await EnsureTableAsync();
        using var connection = _database.CreateConnection();
        var docId = await connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO dbo.HrPolicyDocument (Category, Title, Company, Description, FileName, ContentType, FileSize, Content, UploadedBy)
            VALUES (@Category, @Title, @Company, @Description, @FileName, @ContentType, @FileSize, @Content, @UploadedBy);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """,
            new
            {
                Category = category,
                Title = title,
                Company = company,
                Description = description,
                FileName = safeName,
                ContentType = contentType,
                FileSize = content.Length,
                Content = content,
                UploadedBy = uploadedBy.Length > 100 ? uploadedBy[..100] : uploadedBy,
            });

        return new HrPolicyDocumentDto
        {
            DocId = docId,
            Category = category,
            Title = title,
            Company = company,
            Description = description,
            FileName = safeName,
            ContentType = contentType,
            FileSize = content.Length,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.Now,
        };
    }

    public async Task<(byte[] Content, string ContentType, string FileName, string? Company)?> GetFileAsync(int docId)
    {
        await EnsureTableAsync();
        using var connection = _database.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<FileRow>(
            "SELECT Content, ContentType, FileName, Company FROM dbo.HrPolicyDocument WHERE DocId = @DocId AND IsDeleted = 0",
            new { DocId = docId });
        return row?.Content is null ? null : (row.Content, row.ContentType, row.FileName, row.Company);
    }

    public async Task<bool> DeleteAsync(int docId, string deletedBy)
    {
        await EnsureTableAsync();
        using var connection = _database.CreateConnection();
        return await connection.ExecuteAsync(
            """
            UPDATE dbo.HrPolicyDocument SET IsDeleted = 1, DeletedBy = @DeletedBy, DeletedAt = GETDATE()
            WHERE DocId = @DocId AND IsDeleted = 0
            """,
            new { DocId = docId, DeletedBy = deletedBy.Length > 100 ? deletedBy[..100] : deletedBy }) > 0;
    }

    private static string? DetectType(byte[] b, string fileName)
    {
        var known = HrEmployeeDocumentService.DetectDocumentType(b);
        if (known is not null) return known;
        var isZip = b.Length >= 4 && b[0] == 0x50 && b[1] == 0x4B && b[2] == 0x03 && b[3] == 0x04;
        return isZip && OfficeTypes.TryGetValue(Path.GetExtension(fileName), out var office) ? office : null;
    }

    private async Task EnsureTableAsync()
    {
        if (_tableReady) return;
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(
            """
            IF OBJECT_ID('dbo.HrPolicyDocument', 'U') IS NULL
            BEGIN
              CREATE TABLE dbo.HrPolicyDocument (
                DocId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_HrPolicyDocument PRIMARY KEY,
                Category varchar(40) NOT NULL,
                Title nvarchar(200) NOT NULL,
                Company varchar(150) NULL,
                Description nvarchar(500) NULL,
                FileName nvarchar(255) NOT NULL,
                ContentType varchar(120) NOT NULL,
                FileSize int NOT NULL,
                Content varbinary(max) NOT NULL,
                UploadedBy varchar(100) NULL,
                UploadedAt datetime NOT NULL CONSTRAINT DF_HrPolicyDocument_UploadedAt DEFAULT (GETDATE()),
                IsDeleted bit NOT NULL CONSTRAINT DF_HrPolicyDocument_IsDeleted DEFAULT (0),
                DeletedBy varchar(100) NULL,
                DeletedAt datetime NULL
              );
              CREATE INDEX IX_HrPolicyDocument_Category ON dbo.HrPolicyDocument (IsDeleted, Category);
            END
            """);
        _tableReady = true;
    }

    private sealed class FileRow
    {
        public byte[]? Content { get; set; }
        public string ContentType { get; set; } = "";
        public string FileName { get; set; } = "";
        public string? Company { get; set; }
    }
}

public sealed class HrPolicyDocumentDto
{
    public int DocId { get; set; }
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Company { get; set; }
    public string? Description { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public int FileSize { get; set; }
    public string? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
}
