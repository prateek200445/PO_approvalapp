using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Stores what the user decided for a potential match (Matched, Mismatch, or Missing) and applies it on every run.
/// A decision is keyed by both sides' bill and amounts, so it stops applying when either side changes.
/// </summary>
public sealed class GstRecoDecisionService
{
    public const string Matched = "Matched";
    public const string Mismatch = "Mismatch";
    public const string Missing = "Missing";
    private const int TimeoutSeconds = 60;
    private static int _tableReady;
    private readonly DatabaseService _database;

    public GstRecoDecisionService(DatabaseService database)
    {
        _database = database;
    }

    public static bool IsDecision(string? value) => value is Matched or Mismatch or Missing;

    public async Task SaveAsync(GstRecoDecisionRequest request, string user)
    {
        var group = (request.CompanyName ?? "").Trim();
        var key = (request.DecisionKey ?? "").Trim().ToLowerInvariant();
        if (group.Length == 0 || group.Length > 150)
            throw new ArgumentException("Company group is required.");
        if (key.Length != 64 || !key.All(Uri.IsHexDigit))
            throw new ArgumentException("This row cannot take a decision.");
        if (!IsDecision(request.Decision))
            throw new ArgumentException("Choose Matched, Mismatch, or Missing.");

        using var connection = _database.CreateConnection();
        await EnsureTableAsync(connection);
        await connection.ExecuteAsync(
            """
            UPDATE dbo.AppGstRecoDecision
            SET Decision = @Decision, BillNo = @BillNo, TwoBBillNo = @TwoBBillNo, GstNo = @GstNo, Reason = @Reason,
                UserName = @User, CreatedAt = GETDATE()
            WHERE GroupName = @Group AND DecisionKey = @Key;
            IF @@ROWCOUNT = 0
                INSERT INTO dbo.AppGstRecoDecision (GroupName, DecisionKey, Decision, BillNo, TwoBBillNo, GstNo, Reason, UserName)
                VALUES (@Group, @Key, @Decision, @BillNo, @TwoBBillNo, @GstNo, @Reason, @User);
            """,
            new
            {
                Group = group,
                Key = key,
                request.Decision,
                BillNo = Clip(request.BillNo, 100),
                TwoBBillNo = Clip(request.TwoBBillNo, 200),
                GstNo = Clip(request.GstNo, 20),
                Reason = Clip(request.Reason, 500),
                User = Clip(user, 100),
            },
            commandTimeout: TimeoutSeconds);
    }

    public async Task DeleteAsync(string companyName, string decisionKey)
    {
        using var connection = _database.CreateConnection();
        await EnsureTableAsync(connection);
        await connection.ExecuteAsync(
            "DELETE FROM dbo.AppGstRecoDecision WHERE GroupName = @Group AND DecisionKey = @Key",
            new { Group = (companyName ?? "").Trim(), Key = (decisionKey ?? "").Trim().ToLowerInvariant() },
            commandTimeout: TimeoutSeconds);
    }

    /// <summary>Gives every potential match a decision key and applies the decisions stored for the group.</summary>
    public async Task<List<GstBillRecoRow>> ApplyAsync(string group, List<GstBillRecoRow> rows)
    {
        var candidates = rows.Where(row => row.Status == "Potential match").ToList();
        foreach (var row in candidates)
            row.DecisionKey = KeyFor(row);
        if (candidates.Count == 0)
            return rows;

        using var connection = _database.CreateConnection();
        await EnsureTableAsync(connection);
        var stored = (await connection.QueryAsync<StoredDecision>(
                "SELECT DecisionKey, Decision, UserName FROM dbo.AppGstRecoDecision WHERE GroupName = @Group",
                new { Group = group.Trim() }, commandTimeout: TimeoutSeconds))
            .ToDictionary(item => item.DecisionKey.Trim(), StringComparer.OrdinalIgnoreCase);
        if (stored.Count == 0)
            return rows;

        var output = new List<GstBillRecoRow>(rows.Count);
        var splitPortal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (row.Status != "Potential match" || !stored.TryGetValue(row.DecisionKey, out var decision))
            {
                output.Add(row);
                continue;
            }

            row.Decision = decision.Decision;
            row.DecisionBy = decision.UserName;
            switch (decision.Decision)
            {
                case Matched:
                    row.Status = "Matched";
                    output.Add(row);
                    break;
                case Mismatch:
                    row.Status = "Mismatch";
                    output.Add(row);
                    break;
                default:
                    var portal = PortalSide(group, row);
                    if (splitPortal.Add($"{portal.GstNo}|{portal.BillNo}|{portal.TwoBPeriod}|{portal.TwoBTaxable}"))
                        output.Add(portal);
                    output.Add(ErpSide(row));
                    break;
            }
        }
        return output;
    }

    private static GstBillRecoRow PortalSide(string group, GstBillRecoRow row) => new()
    {
        Status = "Missing in ERP",
        Decision = row.Decision,
        DecisionBy = row.DecisionBy,
        DecisionKey = row.DecisionKey,
        CompanyName = group,
        VoucherType = row.DocumentType,
        BillDate = row.TwoBBillDate ?? row.BillDate,
        BillNo = row.TwoBBillNo,
        Party = row.Party,
        GstNo = row.GstNo,
        TwoBTaxable = row.TwoBTaxable,
        Difference = Math.Round(-Math.Abs(row.TwoBTaxable ?? 0), 2),
        Igst = row.TwoBIgst,
        TwoBIgst = row.TwoBIgst,
        TwoBCgstSgst = row.TwoBCgstSgst,
        MonthAsPer2B = row.MonthAsPer2B,
        TwoBPeriod = row.TwoBPeriod,
        DocumentType = row.DocumentType,
    };

    private static GstBillRecoRow ErpSide(GstBillRecoRow row)
    {
        row.Status = "Missing in 2B";
        row.TwoBBillNo = "";
        row.TwoBBillDate = null;
        row.DateDiffers = false;
        row.TwoBTaxable = null;
        row.TwoBIgst = null;
        row.TwoBCgstSgst = null;
        row.MonthAsPer2B = "";
        row.TwoBPeriod = "";
        row.DocumentType = "";
        row.Difference = Math.Round(Math.Abs(row.ErpTaxable ?? 0), 2);
        return row;
    }

    private static string KeyFor(GstBillRecoRow row)
    {
        var parts = new[]
        {
            row.CompanyName, row.VoucherType, row.VoucherNo, Day(row.VoucherDate), row.BillNo, Day(row.BillDate), row.GstNo,
            row.TwoBBillNo, Day(row.TwoBBillDate), row.TwoBPeriod,
            Money(row.ErpTaxable), Money(row.Cgst), Money(row.Sgst), Money(row.Igst),
            Money(row.TwoBTaxable), Money(row.TwoBIgst), Money(row.TwoBCgstSgst),
        };
        var text = string.Join("|", parts.Select(part => (part ?? "").Trim().ToUpperInvariant()));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    private static string Day(DateTime? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

    private static string Money(decimal? value) => value is null ? "" : Math.Round(value.Value, 2).ToString("0.00", CultureInfo.InvariantCulture);

    private static string? Clip(string? value, int length)
    {
        var text = value?.Trim();
        return string.IsNullOrEmpty(text) ? null : text.Length > length ? text[..length] : text;
    }

    private static async Task EnsureTableAsync(System.Data.IDbConnection connection)
    {
        if (Volatile.Read(ref _tableReady) == 1)
            return;
        await connection.ExecuteAsync(
            """
            IF OBJECT_ID('dbo.AppGstRecoDecision', 'U') IS NULL
            CREATE TABLE dbo.AppGstRecoDecision (
                Id int IDENTITY(1, 1) PRIMARY KEY,
                GroupName varchar(150) NOT NULL,
                DecisionKey char(64) NOT NULL,
                Decision varchar(20) NOT NULL,
                BillNo varchar(100) NULL,
                TwoBBillNo varchar(200) NULL,
                GstNo varchar(20) NULL,
                Reason nvarchar(500) NULL,
                UserName varchar(100) NOT NULL,
                CreatedAt datetime NOT NULL DEFAULT GETDATE(),
                CONSTRAINT UQ_AppGstRecoDecision UNIQUE (GroupName, DecisionKey)
            );
            """, commandTimeout: TimeoutSeconds);
        Volatile.Write(ref _tableReady, 1);
    }

    private sealed class StoredDecision
    {
        public string DecisionKey { get; set; } = "";
        public string Decision { get; set; } = "";
        public string UserName { get; set; } = "";
    }
}
