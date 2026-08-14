using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public sealed class DmsAttachmentService
{
    private readonly DatabaseService _database;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DmsAttachmentService> _logger;

    private static readonly Regex FySegment = new(@"\d{2}-\d{2}", RegexOptions.Compiled);

    public DmsAttachmentService(
        DatabaseService database,
        IConfiguration configuration,
        ILogger<DmsAttachmentService> logger)
    {
        _database = database;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DmsAttachmentListResponse?> GetAttachmentsAsync(string purchaseCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(purchaseCode))
            return null;

        using var connection = _database.CreateConnection();

        var header = await connection.QueryFirstOrDefaultAsync<DmsHeaderRow>(
            """
            SELECT TOP 1
                pp.PurchaseCode,
                fi.IndentCode
            FROM PurchasePayment pp
            INNER JOIN FactoryInfo fi ON fi.Name = pp.CompanyName
            WHERE pp.PurchaseCode = @PurchaseCode
            """,
            new { PurchaseCode = purchaseCode.Trim() });

        if (header is null || string.IsNullOrWhiteSpace(header.IndentCode))
            return null;

        var refType = ResolveRefType(purchaseCode);
        var refEntryNo = BuildRefEntryNo(header.IndentCode, purchaseCode, refType);

        var files = (await connection.QueryAsync<DmsAttachmentDto>(
            """
            SELECT
                f.FileId,
                f.FileName,
                f.FileSize,
                f.UploadedBy,
                f.UploadedOn
            FROM dms_files f
            INNER JOIN dms_ref r ON r.RefId = f.RefId
            WHERE r.RefType = @RefType
              AND r.RefEntryNo = @RefEntryNo
              AND ISNULL(f.IsDeleted, 0) = 0
            ORDER BY f.UploadedOn DESC, f.FileId DESC
            """,
            new { RefType = refType, RefEntryNo = refEntryNo })).ToList();

        return new DmsAttachmentListResponse
        {
            PurchaseCode = purchaseCode.Trim(),
            RefType = refType,
            RefEntryNo = refEntryNo,
            Files = files
        };
    }

    public async Task<DmsFileRow?> GetFileRowAsync(int fileId, CancellationToken ct = default)
    {
        using var connection = _database.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DmsFileRow>(
            """
            SELECT
                f.FileId,
                f.FileName,
                f.PhysicalFileName,
                f.FileSize,
                r.RefType,
                r.RefEntryNo
            FROM dms_files f
            INNER JOIN dms_ref r ON r.RefId = f.RefId
            WHERE f.FileId = @FileId
              AND ISNULL(f.IsDeleted, 0) = 0
            """,
            new { FileId = fileId });
    }

    public string? ResolvePhysicalPath(DmsFileRow file)
    {
        var roots = GetFileLocationRoots();
        if (roots.Count == 0)
            return null;

        foreach (var root in roots)
        {
            var candidate = Path.Combine(root, file.PhysicalFileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public static string ResolveRefType(string purchaseCode) =>
        purchaseCode.Contains("/SER/", StringComparison.OrdinalIgnoreCase) ? "MM_WO" : "MM_PO";

    public static string BuildRefEntryNo(string indentCode, string purchaseCode, string refType)
    {
        var fy = ExtractFinancialYear(purchaseCode);
        return $"{indentCode}/{fy}/{refType}/{purchaseCode}";
    }

    internal static string ExtractFinancialYear(string purchaseCode)
    {
        var match = FySegment.Match(purchaseCode);
        return match.Success ? match.Value : "";
    }

    private IReadOnlyList<string> GetFileLocationRoots()
    {
        var roots = new List<string>();

        void AddIfValid(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            try
            {
                var full = Path.GetFullPath(path.Trim());
                if (Directory.Exists(full))
                    roots.Add(full);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Skipping invalid DMS file root {Path}", path);
            }
        }

        AddIfValid(_configuration["Dms:FileLocation"]);
        AddIfValid(Environment.GetEnvironmentVariable("DMS_FILE_LOCATION"));

        var extra = _configuration.GetSection("Dms:AdditionalFileLocations").Get<string[]>();
        if (extra is not null)
        {
            foreach (var p in extra)
                AddIfValid(p);
        }

        return roots;
    }

    private sealed class DmsHeaderRow
    {
        public string PurchaseCode { get; set; } = "";
        public string IndentCode { get; set; } = "";
    }
}

public sealed class DmsFileRow
{
    public int FileId { get; set; }
    public string FileName { get; set; } = "";
    public string PhysicalFileName { get; set; } = "";
    public long FileSize { get; set; }
    public string RefType { get; set; } = "";
    public string RefEntryNo { get; set; } = "";
}
