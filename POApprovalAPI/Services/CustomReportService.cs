using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;

namespace POApprovalAPI.Services;

/// <summary>
/// ERP Customize Report → Create (MM / ERP.frmAllTypeofReport).
/// Saved layouts live in MaterialProcessing.dbo.Reportall
/// (ReportName, QueryString = comma-separated columns, TableName, username, IsVisibletoall).
/// </summary>
public sealed class CustomReportService
{
    private const int MaxRows = 5_000;
    private static readonly Regex SafeIdent = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private readonly DatabaseService _db;

    public CustomReportService(DatabaseService db) => _db = db;

    public async Task<IReadOnlyList<string>> ListSourcesAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT name
            FROM (
                SELECT t.name AS name
                FROM sys.tables t
                WHERE t.is_ms_shipped = 0
                UNION
                SELECT v.name
                FROM sys.views v
                WHERE v.is_ms_shipped = 0
            ) x
            ORDER BY name
            """;
        using var conn = _db.CreateConnection();
        var names = (await conn.QueryAsync<string>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
        return names;
    }

    public async Task<IReadOnlyList<CustomReportSaved>> ListSavedAsync(string? username, CancellationToken ct = default)
    {
        const string sql = """
            SELECT ReportName, QueryString, TableName, username AS Username,
                   ISNULL(IsVisibletoall, 'no') AS IsVisibleToAll
            FROM dbo.Reportall WITH (NOLOCK)
            WHERE (@Username IS NULL OR @Username = ''
                   OR username = @Username
                   OR LOWER(LTRIM(RTRIM(ISNULL(IsVisibletoall, 'no')))) IN ('yes', 'y', '1', 'true'))
            ORDER BY ReportName, username
            """;
        using var conn = _db.CreateConnection();
        return (await conn.QueryAsync<CustomReportSaved>(
            new CommandDefinition(sql, new { Username = username?.Trim() }, cancellationToken: ct))).ToList();
    }

    public async Task<IReadOnlyList<string>> GetColumnsAsync(string tableName, CancellationToken ct = default)
    {
        var table = await ResolveObjectAsync(tableName, ct)
            ?? throw new ArgumentException($"Unknown table or view: {tableName}");

        const string sql = """
            SELECT c.name
            FROM sys.columns c
            INNER JOIN sys.objects o ON o.object_id = c.object_id
            WHERE o.schema_id = SCHEMA_ID('dbo') AND o.name = @Table
            ORDER BY c.column_id
            """;
        using var conn = _db.CreateConnection();
        return (await conn.QueryAsync<string>(
            new CommandDefinition(sql, new { Table = table }, cancellationToken: ct))).ToList();
    }

    public async Task<CustomReportResult> RunAsync(CustomReportRunRequest req, CancellationToken ct = default)
    {
        if (req.Columns is null || req.Columns.Count == 0)
            throw new ArgumentException("Select at least one column.");

        var table = await ResolveObjectAsync(req.TableName, ct)
            ?? throw new ArgumentException($"Unknown table or view: {req.TableName}");

        var allowed = (await GetColumnsAsync(table, ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var cols = new List<string>();
        foreach (var raw in req.Columns)
        {
            var c = (raw ?? "").Trim();
            if (c.Length == 0) continue;
            if (!SafeIdent.IsMatch(c) || !allowed.Contains(c))
                throw new ArgumentException($"Invalid column: {c}");
            // keep first occurrence casing from catalog
            var canon = allowed.First(a => a.Equals(c, StringComparison.OrdinalIgnoreCase));
            if (!cols.Contains(canon, StringComparer.OrdinalIgnoreCase))
                cols.Add(canon);
        }
        if (cols.Count == 0)
            throw new ArgumentException("Select at least one column.");

        var selectList = string.Join(", ", cols.Select(c => $"[{c}]"));
        var sql = $"SELECT TOP ({MaxRows}) {selectList} FROM dbo.[{table}] WITH (NOLOCK)";

        using var conn = _db.CreateConnection();
        using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        using var reader = await cmd.ExecuteReaderAsync(ct);

        var headers = cols.ToList();
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                if (val is DateTime dt) val = dt.ToString("yyyy-MM-dd HH:mm:ss");
                else if (val is DateTimeOffset dto) val = dto.ToString("yyyy-MM-dd HH:mm:ss");
                else if (val is byte[] bytes) val = Convert.ToBase64String(bytes);
                row[headers[i]] = val;
            }
            rows.Add(row);
        }

        return new CustomReportResult
        {
            TableName = table,
            Columns = headers,
            Rows = rows,
            RowCount = rows.Count,
            Truncated = rows.Count >= MaxRows,
        };
    }

    public async Task SaveAsync(CustomReportSaveRequest req, CancellationToken ct = default)
    {
        var reportName = (req.ReportName ?? "").Trim();
        var username = (req.Username ?? "").Trim();
        if (reportName.Length == 0) throw new ArgumentException("Report name is required.");
        if (username.Length == 0) throw new ArgumentException("Username is required to save.");
        if (req.Columns is null || req.Columns.Count == 0)
            throw new ArgumentException("Select at least one column.");

        var table = await ResolveObjectAsync(req.TableName, ct)
            ?? throw new ArgumentException($"Unknown table or view: {req.TableName}");

        var allowed = (await GetColumnsAsync(table, ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var cols = new List<string>();
        foreach (var raw in req.Columns)
        {
            var c = (raw ?? "").Trim();
            if (!SafeIdent.IsMatch(c) || !allowed.Contains(c))
                throw new ArgumentException($"Invalid column: {c}");
            var canon = allowed.First(a => a.Equals(c, StringComparison.OrdinalIgnoreCase));
            if (!cols.Contains(canon, StringComparer.OrdinalIgnoreCase))
                cols.Add(canon);
        }

        var queryString = string.Join(",", cols);
        if (queryString.Length > 1000)
            throw new ArgumentException("Too many columns for ERP Reportall.QueryString (max 1000 chars).");

        var visible = string.IsNullOrWhiteSpace(req.IsVisibleToAll) ? "no" : req.IsVisibleToAll.Trim().ToLowerInvariant();
        if (visible is not ("yes" or "no")) visible = "no";

        using var conn = _db.CreateConnection();
        const string existsSql = """
            SELECT COUNT(1) FROM dbo.Reportall WITH (NOLOCK)
            WHERE ReportName = @ReportName AND username = @Username AND TableName = @TableName
            """;
        var exists = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(existsSql, new { ReportName = reportName, Username = username, TableName = table }, cancellationToken: ct));

        if (exists > 0)
        {
            const string upd = """
                UPDATE dbo.Reportall
                SET QueryString = @QueryString, IsVisibletoall = @Visible
                WHERE ReportName = @ReportName AND username = @Username AND TableName = @TableName
                """;
            await conn.ExecuteAsync(new CommandDefinition(upd,
                new { ReportName = reportName, Username = username, TableName = table, QueryString = queryString, Visible = visible },
                cancellationToken: ct));
        }
        else
        {
            const string ins = """
                INSERT INTO dbo.Reportall (ReportName, QueryString, TableName, username, IsVisibletoall)
                VALUES (@ReportName, @QueryString, @TableName, @Username, @Visible)
                """;
            await conn.ExecuteAsync(new CommandDefinition(ins,
                new { ReportName = reportName, QueryString = queryString, TableName = table, Username = username, Visible = visible },
                cancellationToken: ct));
        }
    }

    private async Task<string?> ResolveObjectAsync(string? name, CancellationToken ct)
    {
        var n = (name ?? "").Trim();
        if (n.Length == 0 || !SafeIdent.IsMatch(n)) return null;

        const string sql = """
            SELECT TOP 1 name
            FROM (
                SELECT t.name FROM sys.tables t WHERE t.is_ms_shipped = 0 AND t.name = @Name
                UNION ALL
                SELECT v.name FROM sys.views v WHERE v.is_ms_shipped = 0 AND v.name = @Name
            ) x
            """;
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(sql, new { Name = n }, cancellationToken: ct));
    }
}

public sealed class CustomReportSaved
{
    public string ReportName { get; set; } = "";
    public string QueryString { get; set; } = "";
    public string TableName { get; set; } = "";
    public string Username { get; set; } = "";
    public string IsVisibleToAll { get; set; } = "no";
}

public sealed class CustomReportRunRequest
{
    public string TableName { get; set; } = "";
    public List<string> Columns { get; set; } = [];
}

public sealed class CustomReportSaveRequest
{
    public string ReportName { get; set; } = "";
    public string TableName { get; set; } = "";
    public List<string> Columns { get; set; } = [];
    public string Username { get; set; } = "";
    public string? IsVisibleToAll { get; set; }
}

public sealed class CustomReportResult
{
    public string TableName { get; set; } = "";
    public List<string> Columns { get; set; } = [];
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
    public int RowCount { get; set; }
    public bool Truncated { get; set; }
}
