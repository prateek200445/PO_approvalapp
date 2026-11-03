using System.Globalization;
using System.Reflection;
using System.Text;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class GateService
{
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static int _schemaReady;

    private readonly DatabaseService _database;
    private readonly IConfiguration _configuration;

    public GateService(DatabaseService database, IConfiguration configuration)
    {
        _database = database;
        _configuration = configuration;
    }

    public int LongStayHours =>
        int.TryParse(_configuration["Gate:LongStayHours"], out var hours) && hours > 0 ? hours : 8;

    public async Task<GateContextDto> GetContextAsync(string? username, CancellationToken ct)
    {
        var access = AccessFor(username);
        if (!access.Any)
            throw new UnauthorizedAccessException("You do not have access to the factory gate.");

        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        var serverDate = await connection.QuerySingleAsync<DateTime>(
            new CommandDefinition("SELECT CAST(SYSDATETIME() AS date)", cancellationToken: ct));
        var gates = (await connection.QueryAsync<GateSiteDto>(
            new CommandDefinition(@"
SELECT GateId, FactoryCode, FactoryName, GateCode, GateName
FROM dbo.GateMaster
WHERE IsActive = 1
ORDER BY FactoryName, GateName", cancellationToken: ct))).ToList();
        var purposes = (await connection.QueryAsync<GatePurposeDto>(
            new CommandDefinition(@"
SELECT PurposeId, PurposeCode, PurposeName
FROM dbo.GateVehiclePurposeMaster
WHERE IsActive = 1
ORDER BY SortOrder, PurposeName", cancellationToken: ct))).ToList();

        return new GateContextDto
        {
            RoleLabel = access.RoleLabel,
            CanOperate = access.CanOperate,
            CanViewRegister = access.CanViewRegister,
            CanCorrect = access.CanCorrect,
            CanManageGates = access.CanManageGates,
            LongStayHours = LongStayHours,
            ServerDate = serverDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Gates = gates,
            Purposes = purposes,
        };
    }

    public async Task<GateEntryDto> CreateEntryAsync(GateEntryCreateRequest request, CancellationToken ct)
    {
        var access = Require(request.Username, a => a.CanOperate, "Only a watchman, factory manager, or admin can register a vehicle.");
        var factory = RequireCode(request.FactoryCode, "Factory");
        var vehicle = NormalizeVehicle(request.VehicleNumber);
        var driver = RequireText(request.DriverName, "Driver name", 120);
        var mobile = NormalizeMobile(request.DriverMobile);
        var party = OptionalText(request.PartyName, 200);
        var remarks = OptionalText(request.Remarks, 500);
        var documentType = (request.DocumentType ?? "").Trim().ToUpperInvariant();
        var documentNo = (request.DocumentNo ?? "").Trim();
        if (documentType is not ("PO" or "WO") || documentNo.Length == 0)
            throw new ArgumentException("Open the PO or work order before registering the vehicle.");
        if (mobile == null)
            throw new ArgumentException("Driver number is required.");
        var purposeCode = "MATERIAL_IN";
        if (request.GateId <= 0)
            throw new ArgumentException("Gate is required.");

        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        using var tx = connection.BeginTransaction();

        var gate = await connection.QuerySingleOrDefaultAsync<GateSiteDto>(
            new CommandDefinition(@"
SELECT GateId, FactoryCode, FactoryName, GateCode, GateName
FROM dbo.GateMaster
WHERE GateId = @GateId AND IsActive = 1",
                new { request.GateId }, tx, cancellationToken: ct));
        if (gate == null || !string.Equals(gate.FactoryCode, factory, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Choose an active gate for this factory.");

        var purposeId = await connection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(@"
SELECT PurposeId FROM dbo.GateVehiclePurposeMaster
WHERE PurposeCode = @PurposeCode AND IsActive = 1",
                new { PurposeCode = purposeCode }, tx, cancellationToken: ct));
        if (purposeId == null)
            throw new ArgumentException("Vehicle purpose is not valid.");

        var document = await LoadDocumentAsync(connection, tx, documentNo, documentType, ct);
        if (document == null)
            throw new ArgumentException($"No {documentType} was found for {documentNo}.");

        var number = await NextNumberAsync(connection, tx, factory, ct);
        long entryId;
        try
        {
            entryId = await connection.QuerySingleAsync<long>(
                new CommandDefinition(@"
INSERT INTO dbo.GateVehicleEntries
    (GateEntryNumber, FactoryCode, GateId, VehicleNumber, DriverName, DriverMobile, PartyName,
     PurposeId, Remarks, Status, EntryUserId, DocumentType, DocumentNo)
OUTPUT INSERTED.EntryId
VALUES
    (@Number, @Factory, @GateId, @Vehicle, @Driver, @Mobile, @Party,
     @PurposeId, @Remarks, N'INSIDE', @User, @DocumentType, @DocumentNo)",
                    new
                    {
                        Number = number,
                        Factory = factory,
                        request.GateId,
                        Vehicle = vehicle,
                        Driver = driver,
                        Mobile = mobile,
                        Party = party,
                        PurposeId = purposeId.Value,
                        Remarks = remarks,
                        User = access.User,
                        DocumentType = document.DocumentType,
                        DocumentNo = document.DocumentNo,
                    }, tx, cancellationToken: ct));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("This vehicle is already inside this factory. Mark the existing entry as exited first.");
        }

        await InsertAuditAsync(connection, tx, entryId, "CREATE", access.User, null, null, null, null, ct);
        tx.Commit();
        return await GetEntryAsync(connection, null, entryId, ct)
            ?? throw new InvalidOperationException("The entry was saved but could not be read back.");
    }

    public async Task<GateDocumentLookupDto> LookupDocumentAsync(string? username, string? number, string? documentType, CancellationToken ct)
    {
        Require(username, a => a.CanOperate, "Only a watchman, factory manager, or admin can open a PO or work order.");
        var documentNo = (number ?? "").Trim();
        if (documentNo.Length < 2)
            throw new ArgumentException("Enter the PO or work order number.");

        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        var requested = NullIfEmpty(documentType)?.ToUpperInvariant();
        if (requested != null && requested is not ("PO" or "WO"))
            throw new ArgumentException("Document type must be PO or WO.");

        var choices = (await connection.QueryAsync<string>(
            new CommandDefinition(@"
SELECT 'PO'
WHERE EXISTS (SELECT 1 FROM ApprovePO WHERE LTRIM(RTRIM(PoNo)) = @No)
   OR EXISTS (SELECT 1 FROM ApprovePOHOD WHERE LTRIM(RTRIM(PoNo)) = @No)
UNION
SELECT 'WO'
WHERE EXISTS (SELECT 1 FROM ApproveWorkOrder WHERE LTRIM(RTRIM(PoNo)) = @No)",
                new { No = documentNo }, cancellationToken: ct))).ToList();

        if (choices.Count == 0)
        {
            var inView = await connection.QuerySingleAsync<int>(
                new CommandDefinition("SELECT CASE WHEN EXISTS (SELECT 1 FROM Vw_PurchaseOrder WHERE LTRIM(RTRIM(PurchaseCode)) = @No) THEN 1 ELSE 0 END",
                    new { No = documentNo }, cancellationToken: ct));
            if (inView == 0)
                throw new KeyNotFoundException("No PO or work order was found for that number.");
            choices.Add("PO");
        }

        if (requested == null && choices.Count > 1)
            return new GateDocumentLookupDto { NeedsChoice = true, Choices = choices };

        var kind = requested ?? choices[0];
        if (!choices.Contains(kind))
            throw new KeyNotFoundException($"No {kind} was found for {documentNo}.");

        var document = await LoadDocumentAsync(connection, null, documentNo, kind, ct)
            ?? throw new KeyNotFoundException($"No {kind} was found for {documentNo}.");
        return new GateDocumentLookupDto { Document = document };
    }

    public async Task<GateEntryDto> ExitAsync(long entryId, GateExitRequest request, CancellationToken ct)
    {
        var access = Require(request.Username, a => a.CanOperate, "Only a watchman, factory manager, or admin can mark an exit.");
        var exitRemarks = OptionalText(request.ExitRemarks, 500);

        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        using var tx = connection.BeginTransaction();

        var current = await GetEntryAsync(connection, tx, entryId, ct)
            ?? throw new KeyNotFoundException("Gate entry was not found.");
        if (!string.Equals(current.Status, "INSIDE", StringComparison.Ordinal))
            throw new InvalidOperationException("This vehicle has already exited.");

        var exitGateId = request.ExitGateId is > 0 ? request.ExitGateId.Value : current.GateId;
        var exitGate = await connection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(@"
SELECT GateId FROM dbo.GateMaster
WHERE GateId = @GateId AND FactoryCode = @Factory AND IsActive = 1",
                new { GateId = exitGateId, Factory = current.FactoryCode }, tx, cancellationToken: ct));
        if (exitGate == null)
            throw new ArgumentException("Exit gate must be an active gate of the same factory.");

        var updated = await connection.QuerySingleOrDefaultAsync<long?>(
            new CommandDefinition(@"
UPDATE dbo.GateVehicleEntries
SET Status = N'EXITED',
    ExitAt = SYSDATETIME(),
    ExitUserId = @User,
    ExitGateId = @ExitGateId,
    ExitRemarks = @ExitRemarks,
    DurationMinutes = DATEDIFF(MINUTE, EntryAt, SYSDATETIME()),
    UpdatedAt = SYSDATETIME()
OUTPUT INSERTED.EntryId
WHERE EntryId = @EntryId AND Status = N'INSIDE'",
                new { EntryId = entryId, User = access.User, ExitGateId = exitGateId, ExitRemarks = exitRemarks },
                tx, cancellationToken: ct));
        if (updated == null)
            throw new InvalidOperationException("This vehicle has already exited.");

        await InsertAuditAsync(connection, tx, entryId, "EXIT", access.User, "Status", "INSIDE", "EXITED", exitRemarks, ct);
        tx.Commit();
        return await GetEntryAsync(connection, null, entryId, ct)
            ?? throw new InvalidOperationException("The exit was saved but could not be read back.");
    }

    public async Task<GateEntryDto> CorrectAsync(long entryId, GateCorrectRequest request, CancellationToken ct)
    {
        var access = Require(request.Username, a => a.CanCorrect, "Only a factory manager or admin can correct a gate entry.");
        var reason = RequireText(request.Reason, "Correction reason", 500);
        var vehicle = NormalizeVehicle(request.VehicleNumber);
        var driver = RequireText(request.DriverName, "Driver name", 120);
        var mobile = NormalizeMobile(request.DriverMobile);
        var party = OptionalText(request.PartyName, 200);
        var remarks = OptionalText(request.Remarks, 500);
        var purposeCode = (request.PurposeCode ?? "").Trim().ToUpperInvariant();
        if (purposeCode.Length == 0)
            throw new ArgumentException("Vehicle purpose is required.");

        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        using var tx = connection.BeginTransaction();
        var current = await GetEntryAsync(connection, tx, entryId, ct)
            ?? throw new KeyNotFoundException("Gate entry was not found.");

        var purpose = await connection.QuerySingleOrDefaultAsync<GatePurposeDto>(
            new CommandDefinition(@"
SELECT PurposeId, PurposeCode, PurposeName
FROM dbo.GateVehiclePurposeMaster
WHERE PurposeCode = @PurposeCode AND IsActive = 1",
                new { PurposeCode = purposeCode }, tx, cancellationToken: ct));
        if (purpose == null)
            throw new ArgumentException("Vehicle purpose is not valid.");

        var changes = new List<(string Field, string? Old, string? New)>();
        Track(changes, "VehicleNumber", current.VehicleNumber, vehicle);
        Track(changes, "DriverName", current.DriverName, driver);
        Track(changes, "DriverMobile", current.DriverMobile, mobile);
        Track(changes, "PartyName", current.PartyName, party);
        Track(changes, "Purpose", current.PurposeName, purpose.PurposeName);
        Track(changes, "Remarks", current.Remarks, remarks);
        if (changes.Count == 0)
            throw new ArgumentException("Nothing changed.");

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(@"
UPDATE dbo.GateVehicleEntries
SET VehicleNumber = @Vehicle,
    DriverName = @Driver,
    DriverMobile = @Mobile,
    PartyName = @Party,
    PurposeId = @PurposeId,
    Remarks = @Remarks,
    UpdatedAt = SYSDATETIME()
WHERE EntryId = @EntryId",
                    new
                    {
                        EntryId = entryId,
                        Vehicle = vehicle,
                        Driver = driver,
                        Mobile = mobile,
                        Party = party,
                        PurposeId = purpose.PurposeId,
                        Remarks = remarks,
                    }, tx, cancellationToken: ct));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("Another active entry already uses this vehicle number at the factory.");
        }

        foreach (var change in changes)
            await InsertAuditAsync(connection, tx, entryId, "CORRECT", access.User, change.Field, change.Old, change.New, reason, ct);

        tx.Commit();
        return await GetEntryAsync(connection, null, entryId, ct)
            ?? throw new InvalidOperationException("The correction was saved but could not be read back.");
    }

    public async Task<IReadOnlyList<GateEntryDto>> ListInsideAsync(string? username, string? factoryCode, string? query, CancellationToken ct)
    {
        Require(username, a => a.CanOperate || a.CanViewRegister, "You do not have access to vehicles inside the factory.");
        var factory = NullCode(factoryCode);
        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        var rows = await connection.QueryAsync<GateEntryDto>(
            new CommandDefinition(SelectSql + @"
WHERE e.Status = N'INSIDE'
  AND (@Factory IS NULL OR e.FactoryCode = @Factory)
  AND (
        @Like IS NULL
        OR e.VehicleNumber LIKE @Like
        OR e.GateEntryNumber LIKE @Like
        OR e.VehicleNumber = @Exact
      )
ORDER BY e.EntryAt ASC, e.EntryId ASC",
                new
                {
                    Factory = factory,
                    Like = Like(query),
                    Exact = ExactVehicle(query),
                    LongStayMinutes = LongStayHours * 60,
                }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<GateSummaryDto> GetSummaryAsync(string? username, string? factoryCode, CancellationToken ct)
    {
        Require(username, a => a.CanOperate || a.CanViewRegister, "You do not have access to the gate dashboard.");
        var factory = NullCode(factoryCode);
        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        var minutes = LongStayHours * 60;
        var summary = await connection.QuerySingleAsync<GateSummaryDto>(
            new CommandDefinition(@"
DECLARE @Today date = CAST(SYSDATETIME() AS date);
SELECT
    ISNULL(SUM(CASE WHEN Status = N'INSIDE' THEN 1 ELSE 0 END), 0) AS InsideCount,
    ISNULL(SUM(CASE WHEN CAST(EntryAt AS date) = @Today THEN 1 ELSE 0 END), 0) AS TodayEntries,
    ISNULL(SUM(CASE WHEN ExitAt IS NOT NULL AND CAST(ExitAt AS date) = @Today THEN 1 ELSE 0 END), 0) AS TodayExits,
    AVG(CASE WHEN ExitAt IS NOT NULL AND CAST(ExitAt AS date) = @Today THEN CAST(DurationMinutes AS decimal(18,2)) END) AS AverageTurnaroundMinutes,
    ISNULL(SUM(CASE WHEN Status = N'INSIDE' AND DATEDIFF(MINUTE, EntryAt, SYSDATETIME()) >= @Minutes THEN 1 ELSE 0 END), 0) AS LongStayCount,
    CONVERT(varchar(10), @Today, 23) AS ServerDate
FROM dbo.GateVehicleEntries
WHERE @Factory IS NULL OR FactoryCode = @Factory",
                new { Factory = factory, Minutes = minutes }, cancellationToken: ct));
        summary.LongStayHours = LongStayHours;
        summary.InsideCount = summary.InsideCount;
        var longStay = await connection.QueryAsync<GateEntryDto>(
            new CommandDefinition(SelectSql + @"
WHERE e.Status = N'INSIDE'
  AND (@Factory IS NULL OR e.FactoryCode = @Factory)
  AND DATEDIFF(MINUTE, e.EntryAt, SYSDATETIME()) >= @LongStayMinutes
ORDER BY e.EntryAt ASC",
                new { Factory = factory, Like = (string?)null, Exact = (string?)null, LongStayMinutes = minutes },
                cancellationToken: ct));
        summary.LongStay = longStay.ToList();
        summary.LongStayCount = summary.LongStay.Count;
        return summary;
    }

    public async Task<GateRegisterPage> RegisterAsync(GateRegisterQuery query, CancellationToken ct)
    {
        Require(query.Username, a => a.CanViewRegister, "You do not have access to the gate register.");
        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        var (where, parameters) = RegisterFilter(query);
        var total = await connection.QuerySingleAsync<int>(
            new CommandDefinition("SELECT COUNT(1) " + FromSql + where, parameters, cancellationToken: ct));
        var rows = await connection.QueryAsync<GateEntryDto>(
            new CommandDefinition(SelectSql + where + " ORDER BY e.EntryAt DESC, e.EntryId DESC OFFSET 0 ROWS FETCH NEXT 500 ROWS ONLY",
                parameters, cancellationToken: ct));
        return new GateRegisterPage { Total = total, Rows = rows.ToList() };
    }

    public async Task<(byte[] Bytes, string FileName)> ExportExcelAsync(GateRegisterQuery query, CancellationToken ct)
    {
        Require(query.Username, a => a.CanViewRegister, "You do not have access to the gate register.");
        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        var (where, parameters) = RegisterFilter(query);
        var rows = (await connection.QueryAsync<GateEntryDto>(
            new CommandDefinition(SelectSql + where + " ORDER BY e.EntryAt DESC, e.EntryId DESC OFFSET 0 ROWS FETCH NEXT 10000 ROWS ONLY",
                parameters, cancellationToken: ct))).ToList();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Gate Register");
        var headers = new[]
        {
            "Gate Entry Number", "Document", "Factory", "Entry Gate", "Vehicle Number", "Driver", "Mobile",
            "Supplier / Customer", "Purpose", "Remarks", "Status", "Entry Time", "Entry User",
            "Exit Time", "Exit User", "Exit Gate", "Exit Remarks", "Minutes Inside", "Long Stay",
        };
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var line = i + 2;
            sheet.Cell(line, 1).Value = row.GateEntryNumber;
            sheet.Cell(line, 2).Value = string.IsNullOrEmpty(row.DocumentNo) ? "" : $"{row.DocumentType} {row.DocumentNo}";
            sheet.Cell(line, 3).Value = row.FactoryName;
            sheet.Cell(line, 4).Value = row.GateName;
            sheet.Cell(line, 5).Value = row.VehicleNumber;
            sheet.Cell(line, 6).Value = row.DriverName;
            sheet.Cell(line, 7).Value = row.DriverMobile ?? "";
            sheet.Cell(line, 8).Value = row.PartyName ?? "";
            sheet.Cell(line, 9).Value = row.PurposeName;
            sheet.Cell(line, 10).Value = row.Remarks ?? "";
            sheet.Cell(line, 11).Value = row.Status;
            sheet.Cell(line, 12).Value = row.EntryAt;
            sheet.Cell(line, 13).Value = row.EntryUserId;
            if (row.ExitAt.HasValue) sheet.Cell(line, 14).Value = row.ExitAt.Value;
            sheet.Cell(line, 15).Value = row.ExitUserId ?? "";
            sheet.Cell(line, 16).Value = row.ExitGateName ?? "";
            sheet.Cell(line, 17).Value = row.ExitRemarks ?? "";
            if (row.DurationMinutes.HasValue) sheet.Cell(line, 18).Value = row.DurationMinutes.Value;
            sheet.Cell(line, 19).Value = row.IsLongStay ? "Yes" : "";
        }

        sheet.Row(1).Style.Font.Bold = true;
        sheet.Column(12).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        sheet.Column(14).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return (stream.ToArray(), $"gate-register-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    public async Task<IReadOnlyList<GateAuditDto>> AuditAsync(string? username, long entryId, CancellationToken ct)
    {
        Require(username, a => a.CanViewRegister || a.CanCorrect, "You do not have access to the gate audit trail.");
        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        var exists = await connection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition("SELECT 1 FROM dbo.GateVehicleEntries WHERE EntryId = @EntryId", new { EntryId = entryId }, cancellationToken: ct));
        if (exists == null)
            throw new KeyNotFoundException("Gate entry was not found.");
        var rows = await connection.QueryAsync<GateAuditDto>(
            new CommandDefinition(@"
SELECT AuditId, EntryId, Action, ChangedBy, ChangedAt, FieldName, OldValue, NewValue, Reason
FROM dbo.GateVehicleEntryAudit
WHERE EntryId = @EntryId
ORDER BY AuditId ASC", new { EntryId = entryId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<GateSiteDto> AddGateAsync(GateCreateRequest request, CancellationToken ct)
    {
        Require(request.Username, a => a.CanManageGates, "Only an admin can add a factory gate.");
        var factory = RequireCode(request.FactoryCode, "Factory code");
        var gateCode = RequireCode(request.GateCode, "Gate code");
        var factoryName = RequireText(request.FactoryName, "Factory name", 200);
        var gateName = RequireText(request.GateName, "Gate name", 100);

        await EnsureSchemaAsync(ct);
        using var connection = _database.CreateConnection();
        try
        {
            return await connection.QuerySingleAsync<GateSiteDto>(
                new CommandDefinition(@"
INSERT INTO dbo.GateMaster (FactoryCode, FactoryName, GateCode, GateName, IsActive)
OUTPUT INSERTED.GateId, INSERTED.FactoryCode, INSERTED.FactoryName, INSERTED.GateCode, INSERTED.GateName
VALUES (@Factory, @FactoryName, @GateCode, @GateName, 1)",
                    new { Factory = factory, FactoryName = factoryName, GateCode = gateCode, GateName = gateName },
                    cancellationToken: ct));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("This factory already has a gate with that code.");
        }
    }

    private (string Where, DynamicParameters Parameters) RegisterFilter(GateRegisterQuery query)
    {
        var status = NullIfEmpty(query.Status)?.ToUpperInvariant();
        if (status != null && status is not ("INSIDE" or "EXITED"))
            throw new ArgumentException("Status must be INSIDE or EXITED.");
        if (query.DateFrom.HasValue && query.DateTo.HasValue && query.DateFrom.Value.Date > query.DateTo.Value.Date)
            throw new ArgumentException("From date cannot be after to date.");

        var parameters = new DynamicParameters();
        parameters.Add("Factory", NullCode(query.FactoryCode));
        parameters.Add("DateFrom", query.DateFrom?.Date);
        parameters.Add("DateTo", query.DateTo?.Date);
        parameters.Add("Vehicle", Like(query.VehicleNumber));
        parameters.Add("Driver", Like(query.Driver));
        parameters.Add("Party", Like(query.Party));
        parameters.Add("Purpose", NullIfEmpty(query.PurposeCode)?.ToUpperInvariant());
        parameters.Add("Status", status);
        parameters.Add("LongStay", query.LongStayOnly ? 1 : 0);
        parameters.Add("LongStayMinutes", LongStayHours * 60);
        parameters.Add("Like", (string?)null);
        parameters.Add("Exact", (string?)null);

        var where = @"
WHERE (@Factory IS NULL OR e.FactoryCode = @Factory)
  AND (@DateFrom IS NULL OR CAST(e.EntryAt AS date) >= @DateFrom)
  AND (@DateTo IS NULL OR CAST(e.EntryAt AS date) <= @DateTo)
  AND (@Vehicle IS NULL OR e.VehicleNumber LIKE @Vehicle OR e.GateEntryNumber LIKE @Vehicle)
  AND (@Driver IS NULL OR e.DriverName LIKE @Driver)
  AND (@Party IS NULL OR e.PartyName LIKE @Party)
  AND (@Purpose IS NULL OR p.PurposeCode = @Purpose)
  AND (@Status IS NULL OR e.Status = @Status)
  AND (@LongStay = 0 OR (e.Status = N'INSIDE' AND DATEDIFF(MINUTE, e.EntryAt, SYSDATETIME()) >= @LongStayMinutes))";
        return (where, parameters);
    }

    private const string FromSql = @"
FROM dbo.GateVehicleEntries e
INNER JOIN dbo.GateMaster g ON g.GateId = e.GateId
INNER JOIN dbo.GateVehiclePurposeMaster p ON p.PurposeId = e.PurposeId
LEFT JOIN dbo.GateMaster xg ON xg.GateId = e.ExitGateId
";

    private const string SelectSql = @"
SELECT
    e.EntryId,
    e.GateEntryNumber,
    e.FactoryCode,
    g.FactoryName,
    e.GateId,
    g.GateName,
    e.VehicleNumber,
    e.DriverName,
    e.DriverMobile,
    e.PartyName,
    e.DocumentType,
    e.DocumentNo,
    p.PurposeCode,
    p.PurposeName,
    e.Remarks,
    e.Status,
    e.EntryAt,
    e.EntryUserId,
    e.ExitAt,
    e.ExitUserId,
    e.ExitGateId,
    xg.GateName AS ExitGateName,
    e.ExitRemarks,
    e.DurationMinutes,
    CASE
        WHEN e.Status = N'INSIDE' AND DATEDIFF(MINUTE, e.EntryAt, SYSDATETIME()) >= @LongStayMinutes THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS IsLongStay
" + FromSql;

    private static async Task<GateDocumentDto?> LoadDocumentAsync(
        SqlConnection connection,
        SqlTransaction? tx,
        string documentNo,
        string documentType,
        CancellationToken ct)
    {
        var rows = (await connection.QueryAsync(
            new CommandDefinition(@"
SELECT
    LTRIM(RTRIM(v.PurchaseCode)) AS PurchaseCode,
    NULLIF(LTRIM(RTRIM(v.FirmName)), N'') AS FirmName,
    NULLIF(LTRIM(RTRIM(v.CompanyName)), N'') AS CompanyName,
    NULLIF(LTRIM(RTRIM(v.DepttName)), N'') AS DepttName,
    NULLIF(LTRIM(RTRIM(v.Currency)), N'') AS Currency,
    NULLIF(LTRIM(RTRIM(v.ItemCode)), N'') AS ItemCode,
    NULLIF(LTRIM(RTRIM(v.ItemDesc)), N'') AS ItemDesc,
    v.Qty,
    v.Rate,
    v.Total,
    v.TotalAmount,
    v.deliverydate
FROM Vw_PurchaseOrder v
WHERE LTRIM(RTRIM(v.PurchaseCode)) = @No",
                new { No = documentNo }, tx, cancellationToken: ct))).ToList();
        if (rows.Count == 0) return null;

        var first = rows[0];
        return new GateDocumentDto
        {
            DocumentType = documentType,
            DocumentNo = TextOf(first.PurchaseCode) ?? documentNo,
            SupplierName = TextOf(first.FirmName),
            CompanyName = TextOf(first.CompanyName),
            Department = TextOf(first.DepttName),
            Currency = TextOf(first.Currency),
            TotalAmount = DecimalOf(first.TotalAmount),
            DeliveryDate = DateOf(first.deliverydate),
            Lines = rows.Take(40).Select(row => new GateDocumentLineDto
            {
                ItemCode = TextOf(row.ItemCode),
                ItemDesc = TextOf(row.ItemDesc),
                Qty = DecimalOf(row.Qty),
                Rate = DecimalOf(row.Rate),
                Total = DecimalOf(row.Total),
            }).ToList(),
        };
    }

    private static string? TextOf(object? value)
    {
        if (value == null || value is DBNull) return null;
        var text = Convert.ToString(value)?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static decimal? DecimalOf(object? value)
    {
        if (value == null || value is DBNull) return null;
        return Convert.ToDecimal(value);
    }

    private static DateTime? DateOf(object? value)
    {
        if (value == null || value is DBNull) return null;
        if (value is DateTime date) return date;
        return DateTime.TryParse(Convert.ToString(value), out var parsed) ? parsed : null;
    }

    private async Task<GateEntryDto?> GetEntryAsync(SqlConnection connection, SqlTransaction? tx, long entryId, CancellationToken ct)
    {
        return await connection.QuerySingleOrDefaultAsync<GateEntryDto>(
            new CommandDefinition(SelectSql + " WHERE e.EntryId = @EntryId",
                new { EntryId = entryId, LongStayMinutes = LongStayHours * 60 }, tx, cancellationToken: ct));
    }

    private async Task<string> NextNumberAsync(SqlConnection connection, SqlTransaction tx, string factory, CancellationToken ct)
    {
        var seq = await connection.QuerySingleAsync<GateSequenceRow>(
            new CommandDefinition(@"
MERGE dbo.GateEntrySequence WITH (HOLDLOCK) AS t
USING (SELECT @Factory AS FactoryCode, CAST(SYSDATETIME() AS date) AS EntryDate) AS s
ON t.FactoryCode = s.FactoryCode AND t.EntryDate = s.EntryDate
WHEN MATCHED THEN UPDATE SET LastSeq = t.LastSeq + 1
WHEN NOT MATCHED THEN INSERT (FactoryCode, EntryDate, LastSeq) VALUES (s.FactoryCode, s.EntryDate, 1)
OUTPUT inserted.EntryDate, inserted.LastSeq;",
                new { Factory = factory }, tx, cancellationToken: ct));
        return $"GE-{factory}-{seq.EntryDate:yyyyMMdd}-{seq.LastSeq:D4}";
    }

    private static async Task InsertAuditAsync(
        SqlConnection connection,
        SqlTransaction tx,
        long entryId,
        string action,
        string user,
        string? field,
        string? oldValue,
        string? newValue,
        string? reason,
        CancellationToken ct)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(@"
INSERT INTO dbo.GateVehicleEntryAudit
    (EntryId, Action, ChangedBy, FieldName, OldValue, NewValue, Reason)
VALUES
    (@EntryId, @Action, @User, @Field, @OldValue, @NewValue, @Reason)",
                new
                {
                    EntryId = entryId,
                    Action = action,
                    User = user,
                    Field = field,
                    OldValue = Clip(oldValue),
                    NewValue = Clip(newValue),
                    Reason = Clip(reason),
                }, tx, cancellationToken: ct));
    }

    private async Task EnsureSchemaAsync(CancellationToken ct)
    {
        if (Volatile.Read(ref _schemaReady) == 1) return;
        await SchemaLock.WaitAsync(ct);
        try
        {
            if (_schemaReady == 1) return;
            var sql = LoadSchemaSql();
            using var connection = _database.CreateConnection();
            foreach (var batch in sql.Split(["\r\nGO\r\n", "\nGO\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (batch.Length == 0) continue;
                await connection.ExecuteAsync(new CommandDefinition(batch, cancellationToken: ct));
            }
            Volatile.Write(ref _schemaReady, 1);
        }
        finally
        {
            SchemaLock.Release();
        }
    }

    private static string LoadSchemaSql()
    {
        var assembly = typeof(GateService).Assembly;
        var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("CreateGateVehicle.sql", StringComparison.Ordinal));
        if (name == null)
            throw new InvalidOperationException("Gate schema script is missing from the API.");
        using var stream = assembly.GetManifestResourceStream(name);
        using var reader = new StreamReader(stream!, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private GateCaller Require(string? username, Func<GateAccess, bool> allowed, string message)
    {
        var user = (username ?? "").Trim();
        var access = AccessFor(user);
        if (!allowed(access))
            throw new UnauthorizedAccessException(message);
        return new GateCaller(user, access);
    }

    private GateAccess AccessFor(string? username)
    {
        var user = (username ?? "").Trim();
        if (user.Length == 0)
            return new GateAccess();

        bool Listed(string section)
        {
            var values = _configuration.GetSection($"Gate:{section}").Get<string[]>() ?? [];
            return values.Any(value => string.Equals(value?.Trim(), user, StringComparison.OrdinalIgnoreCase));
        }

        var admin = Listed("Admins");
        var manager = Listed("FactoryManagers");
        var watchman = Listed("Watchmen");
        var management = Listed("Management");
        var roles = new List<string>();
        if (admin) roles.Add("Admin");
        if (manager) roles.Add("Factory Manager");
        if (watchman) roles.Add("Watchman");
        if (management) roles.Add("Management");

        return new GateAccess
        {
            CanOperate = admin || manager || watchman,
            CanViewRegister = admin || manager || management,
            CanCorrect = admin || manager,
            CanManageGates = admin,
            RoleLabel = roles.Count == 0 ? "" : string.Join(", ", roles),
        };
    }

    private static void Track(List<(string Field, string? Old, string? New)> changes, string field, string? oldValue, string? newValue)
    {
        if (!string.Equals(oldValue ?? "", newValue ?? "", StringComparison.Ordinal))
            changes.Add((field, oldValue, newValue));
    }

    public static string NormalizeVehicle(string? raw)
    {
        var value = new string((raw ?? "").ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (value.Length is < 4 or > 16)
            throw new ArgumentException("Enter a valid vehicle number.");
        return value;
    }

    private static string? NormalizeMobile(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
            digits = digits[2..];
        if (digits.Length == 11 && digits.StartsWith('0'))
            digits = digits[1..];
        if (digits.Length != 10)
            throw new ArgumentException("Driver mobile must be a 10-digit number.");
        return digits;
    }

    private static string RequireCode(string? raw, string label)
    {
        var value = new string((raw ?? "").Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (value.Length is < 2 or > 20)
            throw new ArgumentException($"{label} is required.");
        return value;
    }

    private static string RequireText(string? raw, string label, int max)
    {
        var value = (raw ?? "").Trim();
        if (value.Length == 0)
            throw new ArgumentException($"{label} is required.");
        if (value.Length > max)
            throw new ArgumentException($"{label} is too long.");
        return value;
    }

    private static string? OptionalText(string? raw, int max)
    {
        var value = (raw ?? "").Trim();
        if (value.Length == 0) return null;
        if (value.Length > max)
            throw new ArgumentException("Text is too long.");
        return value;
    }

    private static string? NullCode(string? raw)
    {
        var value = (raw ?? "").Trim().ToUpperInvariant();
        return value.Length == 0 ? null : value;
    }

    private static string? NullIfEmpty(string? raw)
    {
        var value = (raw ?? "").Trim();
        return value.Length == 0 ? null : value;
    }

    private static string? ExactVehicle(string? raw)
    {
        var value = new string((raw ?? "").ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        return value.Length == 0 ? null : value;
    }

    private static string? Like(string? raw)
    {
        var value = (raw ?? "").Trim();
        if (value.Length == 0) return null;
        var escaped = value.Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
        return "%" + escaped + "%";
    }

    private static string? Clip(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= 500) return value;
        return value[..500];
    }

    private sealed class GateSequenceRow
    {
        public DateTime EntryDate { get; set; }
        public int LastSeq { get; set; }
    }

    private sealed record GateCaller(string User, GateAccess Access);
}
