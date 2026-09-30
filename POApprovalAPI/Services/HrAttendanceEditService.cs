using System.Globalization;
using Dapper;

namespace POApprovalAPI.Services;

/// <summary>
/// HR corrections to daily attendance. Machine punches in payroll (tempattendance / Attendancemachine)
/// are never changed; corrections live in the portal DB and are applied on top when reports are built.
/// Every save / revert is also written to an audit log.
/// </summary>
public sealed class HrAttendanceEditService
{
    public static readonly IReadOnlyDictionary<string, decimal> Statuses =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["Present"] = 1m,
            ["Half Day"] = 0.5m,
            ["Absent"] = 0m,
            ["WFH"] = 1m,
            ["Holiday"] = 1m,
        };

    private readonly DatabaseService _database;
    private static bool _tablesReady;

    public HrAttendanceEditService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<Dictionary<DateTime, HrAttendanceEditDto>> GetEditsAsync(string empCode, DateTime from, DateTime to)
    {
        await EnsureTablesAsync();
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<HrAttendanceEditDto>(
            """
            SELECT EmpCode, AttDate, Status, PunchIn, PunchOut, Reason, OriginalStatus, EditedBy, EditedAt
            FROM dbo.HrAttendanceEdit
            WHERE EmpCode = @EmpCode AND AttDate >= @From AND AttDate <= @To
            """,
            new { EmpCode = empCode.Trim(), From = from.Date, To = to.Date });
        return rows.ToDictionary(r => r.AttDate.Date);
    }

    public async Task<HrAttendanceEditDto> SaveAsync(HrAttendanceEditRequest request, string originalStatus, string editedBy)
    {
        var empCode = (request.EmpCode ?? "").Trim();
        if (empCode.Length == 0)
            throw new InvalidOperationException("Employee code is required.");
        var date = ParseDate(request.Date);
        if (date > DateTime.Today)
            throw new InvalidOperationException("Cannot edit attendance for a future date.");

        var status = Statuses.Keys.FirstOrDefault(k => string.Equals(k, (request.Status ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Choose a valid status (Present, Half Day, Absent, WFH, Holiday).");

        var reason = (request.Reason ?? "").Trim();
        if (reason.Length < 3)
            throw new InvalidOperationException("Enter a reason for the change.");
        if (reason.Length > 250)
            reason = reason[..250];

        var punchIn = ParseTime(request.PunchIn, "Punch in");
        var punchOut = ParseTime(request.PunchOut, "Punch out");
        if (punchIn is not null && punchOut is not null && punchOut <= punchIn)
            throw new InvalidOperationException("Punch out must be after punch in.");

        await EnsureTablesAsync();
        using var connection = _database.CreateConnection();
        using var tx = connection.BeginTransaction();

        var parameters = new
        {
            EmpCode = empCode,
            AttDate = date,
            Status = status,
            PunchIn = punchIn,
            PunchOut = punchOut,
            Reason = reason,
            OriginalStatus = Truncate(originalStatus, 20),
            EditedBy = Truncate(editedBy, 100),
        };

        await connection.ExecuteAsync(
            """
            MERGE dbo.HrAttendanceEdit WITH (HOLDLOCK) AS t
            USING (SELECT @EmpCode AS EmpCode, @AttDate AS AttDate) AS s
              ON t.EmpCode = s.EmpCode AND t.AttDate = s.AttDate
            WHEN MATCHED THEN UPDATE SET
              Status = @Status, PunchIn = @PunchIn, PunchOut = @PunchOut, Reason = @Reason,
              EditedBy = @EditedBy, EditedAt = GETDATE()
            WHEN NOT MATCHED THEN INSERT (EmpCode, AttDate, Status, PunchIn, PunchOut, Reason, OriginalStatus, EditedBy)
              VALUES (@EmpCode, @AttDate, @Status, @PunchIn, @PunchOut, @Reason, @OriginalStatus, @EditedBy);
            """,
            parameters, tx);

        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.HrAttendanceEditLog (EmpCode, AttDate, Action, Status, PunchIn, PunchOut, Reason, OriginalStatus, EditedBy)
            VALUES (@EmpCode, @AttDate, 'Edit', @Status, @PunchIn, @PunchOut, @Reason, @OriginalStatus, @EditedBy);
            """,
            parameters, tx);

        tx.Commit();

        return (await GetEditsAsync(empCode, date, date))[date];
    }

    public async Task<bool> RevertAsync(string empCode, string dateText, string editedBy)
    {
        empCode = (empCode ?? "").Trim();
        var date = ParseDate(dateText);
        await EnsureTablesAsync();
        using var connection = _database.CreateConnection();
        using var tx = connection.BeginTransaction();
        var removed = await connection.ExecuteAsync(
            "DELETE FROM dbo.HrAttendanceEdit WHERE EmpCode = @EmpCode AND AttDate = @AttDate",
            new { EmpCode = empCode, AttDate = date }, tx);
        if (removed > 0)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.HrAttendanceEditLog (EmpCode, AttDate, Action, Reason, EditedBy)
                VALUES (@EmpCode, @AttDate, 'Revert', 'Reverted to machine attendance', @EditedBy);
                """,
                new { EmpCode = empCode, AttDate = date, EditedBy = Truncate(editedBy, 100) }, tx);
        }
        tx.Commit();
        return removed > 0;
    }

    public async Task<List<HrAttendanceEditLogDto>> GetHistoryAsync(string empCode, DateTime from, DateTime to)
    {
        await EnsureTablesAsync();
        using var connection = _database.CreateConnection();
        return (await connection.QueryAsync<HrAttendanceEditLogDto>(
            """
            SELECT LogId, EmpCode, AttDate, Action, Status, PunchIn, PunchOut, Reason, OriginalStatus, EditedBy, EditedAt
            FROM dbo.HrAttendanceEditLog
            WHERE EmpCode = @EmpCode AND AttDate >= @From AND AttDate <= @To
            ORDER BY EditedAt DESC, LogId DESC
            """,
            new { EmpCode = empCode.Trim(), From = from.Date, To = to.Date })).ToList();
    }

    private async Task EnsureTablesAsync()
    {
        if (_tablesReady)
            return;
        using var connection = _database.CreateConnection();
        await connection.ExecuteAsync(
            """
            IF OBJECT_ID('dbo.HrAttendanceEdit', 'U') IS NULL
            BEGIN
              CREATE TABLE dbo.HrAttendanceEdit (
                EmpCode varchar(50) NOT NULL,
                AttDate date NOT NULL,
                Status varchar(20) NOT NULL,
                PunchIn time(0) NULL,
                PunchOut time(0) NULL,
                Reason nvarchar(250) NOT NULL,
                OriginalStatus varchar(20) NULL,
                EditedBy varchar(100) NULL,
                EditedAt datetime NOT NULL CONSTRAINT DF_HrAttendanceEdit_EditedAt DEFAULT (GETDATE()),
                CONSTRAINT PK_HrAttendanceEdit PRIMARY KEY (EmpCode, AttDate)
              );
            END

            IF OBJECT_ID('dbo.HrAttendanceEditLog', 'U') IS NULL
            BEGIN
              CREATE TABLE dbo.HrAttendanceEditLog (
                LogId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_HrAttendanceEditLog PRIMARY KEY,
                EmpCode varchar(50) NOT NULL,
                AttDate date NOT NULL,
                Action varchar(10) NOT NULL,
                Status varchar(20) NULL,
                PunchIn time(0) NULL,
                PunchOut time(0) NULL,
                Reason nvarchar(250) NULL,
                OriginalStatus varchar(20) NULL,
                EditedBy varchar(100) NULL,
                EditedAt datetime NOT NULL CONSTRAINT DF_HrAttendanceEditLog_EditedAt DEFAULT (GETDATE())
              );
              CREATE INDEX IX_HrAttendanceEditLog_Emp ON dbo.HrAttendanceEditLog (EmpCode, AttDate);
            END
            """);
        _tablesReady = true;
    }

    private static DateTime ParseDate(string? text)
    {
        if (!DateTime.TryParseExact((text ?? "").Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            throw new InvalidOperationException("Date must be yyyy-MM-dd.");
        return d.Date;
    }

    private static TimeSpan? ParseTime(string? text, string label)
    {
        var t = (text ?? "").Trim();
        if (t.Length == 0)
            return null;
        if (TimeSpan.TryParseExact(t, ["hh\\:mm", "hh\\:mm\\:ss", "h\\:mm"], CultureInfo.InvariantCulture, out var ts)
            && ts >= TimeSpan.Zero && ts < TimeSpan.FromDays(1))
            return ts;
        throw new InvalidOperationException($"{label} must be a time like 09:30.");
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var v = value.Trim();
        return v.Length > max ? v[..max] : v;
    }
}

public sealed class HrAttendanceEditRequest
{
    public string EmpCode { get; set; } = "";
    public string Date { get; set; } = "";
    public string Status { get; set; } = "";
    public string? PunchIn { get; set; }
    public string? PunchOut { get; set; }
    public string? Reason { get; set; }
}

public sealed class HrAttendanceEditDto
{
    public string EmpCode { get; set; } = "";
    public DateTime AttDate { get; set; }
    public string Status { get; set; } = "";
    public TimeSpan? PunchIn { get; set; }
    public TimeSpan? PunchOut { get; set; }
    public string Reason { get; set; } = "";
    public string? OriginalStatus { get; set; }
    public string? EditedBy { get; set; }
    public DateTime EditedAt { get; set; }
}

public sealed class HrAttendanceEditLogDto
{
    public int LogId { get; set; }
    public string EmpCode { get; set; } = "";
    public DateTime AttDate { get; set; }
    public string Action { get; set; } = "";
    public string? Status { get; set; }
    public TimeSpan? PunchIn { get; set; }
    public TimeSpan? PunchOut { get; set; }
    public string? Reason { get; set; }
    public string? OriginalStatus { get; set; }
    public string? EditedBy { get; set; }
    public DateTime EditedAt { get; set; }
}
