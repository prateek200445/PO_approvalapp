using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class PlantProductionService
{
    private readonly DatabaseService _db;

    public PlantProductionService(DatabaseService db) => _db = db;

    public async Task<object> GetLoomSetupAsync(string company, DateTime onDate, CancellationToken ct)
    {
        await using var conn = _db.CreateConnection();
        var companyId = await ScalarInt(conn, null, "SELECT TOP 1 srno FROM FactoryInfo WHERE Name = @c", ct, ("@c", company));
        var allocations = new List<object>();
        await using (var cmd = new SqlCommand("""
            SELECT LA.LoomNo, LA.Material, LA.Type, LA.Size, LA.WarpDNR, LA.WeftDNR, LA.WarpMesh, LA.WeftMesh,
                   LA.RFWidth, LA.RFNo, LA.ReqGSM, LA.Sector, LA.PartyName, LA.PONO, LA.Shrinkage,
                   LA.WarpBobin, LA.WeftBobin, LA.FGItemCode,
                   WI.ItemName, WE.ItemName, FG.ItemName
            FROM Prod_LoomAlocationMaster LA
            INNER JOIN prod_LoomformulaMaster LF ON LA.formulaID = LF.formulaID
            LEFT JOIN item WI ON WI.ItemCode = LA.WarpBobin AND WI.CompanyName = @c
            LEFT JOIN item WE ON WE.ItemCode = LA.WeftBobin AND WE.CompanyName = @c
            LEFT JOIN item FG ON FG.ItemCode = LA.FGItemCode AND FG.CompanyName = @c
            WHERE LA.companyId = @id AND LA.isActive = 'Yes' AND LF.isActive = 'Yes'
              AND ((@d BETWEEN LA.allocationdate AND LA.todate) OR (@d >= LA.allocationdate AND LA.todate IS NULL))
            ORDER BY LA.LoomNo
            """, conn))
        {
            cmd.Parameters.AddWithValue("@c", company);
            cmd.Parameters.AddWithValue("@id", companyId);
            cmd.Parameters.AddWithValue("@d", onDate.Date);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var warpMesh = Dec(reader, 6);
                var weftMesh = Dec(reader, 7);
                allocations.Add(new
                {
                    loom = Convert.ToString(reader.GetValue(0)) ?? "",
                    quality = Str(reader, 1),
                    tube = Str(reader, 2).Equals("TUBE", StringComparison.OrdinalIgnoreCase) || Str(reader, 2).Equals("GZT", StringComparison.OrdinalIgnoreCase),
                    size = Dec(reader, 3),
                    warpDnr = Dec(reader, 4),
                    weftDnr = Dec(reader, 5),
                    warpMesh,
                    weftMesh,
                    rfWidth = Dec(reader, 8),
                    rfNo = Dec(reader, 9),
                    gsm = Dec(reader, 10).ToString("0.##"),
                    mesh = $"{warpMesh:0.##} x {weftMesh:0.##}",
                    sector = Str(reader, 11),
                    party = Str(reader, 12),
                    order = Str(reader, 13),
                    shrinkage = Dec(reader, 14),
                    warpCode = Str(reader, 15),
                    weftCode = Str(reader, 16),
                    fg = Str(reader, 17),
                    warp = Str(reader, 18),
                    weft = Str(reader, 19),
                    fgName = Str(reader, 20),
                });
            }
        }

        var bobbins = await Stock(conn, company, "Loom Godown", ct);
        return new { allocations, bobbins };
    }

    public async Task<object> GetLaminationRollsAsync(string company, DateTime onDate, CancellationToken ct)
    {
        await using var conn = _db.CreateConnection();
        await using var cmd = new SqlCommand("dbo.sp_prod_LaminationRollList", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
            CommandTimeout = 60,
        };
        cmd.Parameters.AddWithValue("@vCompanyName", company);
        cmd.Parameters.AddWithValue("@dSysDate", onDate.Date);
        var rows = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var nets = new List<int>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals("NetWt", StringComparison.OrdinalIgnoreCase)) nets.Add(i);
            else map[reader.GetName(i)] = i;
        }
        while (await reader.ReadAsync(ct))
        {
            var roll = Col(reader, map, "rollNo");
            var original = nets.Count > 0 ? DecAt(reader, nets[0]) : 0m;
            var pending = nets.Count > 1 ? DecAt(reader, nets[1]) : original;
            rows.Add(new
            {
                rollNo = roll,
                metre = ColDec(reader, map, "Metre"),
                gross = ColDec(reader, map, "GrossWt"),
                tare = ColDec(reader, map, "TareWt"),
                net = pending,
                originalNet = original,
                quality = Col(reader, map, "Quality"),
                gsm = Col(reader, map, "ReqGSM"),
                outsideRoll = Col(reader, map, "partyRollNo"),
                category = Col(reader, map, "Category"),
                party = Col(reader, map, "Partyname"),
                order = Col(reader, map, "MarketingOrderno"),
                ulCode = Col(reader, map, "FGItemCode"),
                lamCode = Col(reader, map, "FGLAMCode"),
                lamRoll = roll + "L",
            });
        }
        return rows;
    }

    public async Task<List<object>> RecentLoomAsync(string company, DateTime onDate, CancellationToken ct)
    {
        await using var conn = _db.CreateConnection();
        const string sql = """
            SELECT RecordLogid, MIN(Shift), MIN(LoomNo), COUNT(*), SUM(NetWt)
            FROM MISLOOMProductionEntryNew WITH (NOLOCK)
            WHERE CompanyName = @c AND Sysdate = @d AND ISNULL(RecordLogid, 0) <> 0
            GROUP BY RecordLogid
            ORDER BY RecordLogid DESC
            """;
        var rows = new List<object>();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@c", company);
        cmd.Parameters.AddWithValue("@d", onDate.Date);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(new
            {
                id = reader.GetInt32(0),
                shift = Str(reader, 1),
                loom = Convert.ToString(reader.GetValue(2)) ?? "",
                rolls = reader.GetInt32(3),
                net = Dec(reader, 4),
            });
        return rows;
    }

    public async Task<List<object>> LoomEntryAsync(string company, int recordLogId, CancellationToken ct)
    {
        await using var conn = _db.CreateConnection();
        const string sql = """
            SELECT LoomNo, RollNo, Quality, Mesh, ReqGSM, Sector, Metre, GrossWt, TareWt, CoverWt, NetWt,
                   ActualGPM, ActualGSM, Remarks, Partyname, MarketingOrderno, WarpQty, WeftQty,
                   WarpBobbin, weftBobbin, FGItemCode
            FROM MISLOOMProductionEntryNew WITH (NOLOCK)
            WHERE CompanyName = @c AND RecordLogid = @id
            ORDER BY TransId
            """;
        var rows = new List<object>();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@c", company);
        cmd.Parameters.AddWithValue("@id", recordLogId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(new
            {
                loom = Convert.ToString(reader.GetValue(0)) ?? "",
                rollNo = Str(reader, 1),
                quality = Str(reader, 2),
                mesh = Str(reader, 3),
                gsm = Dec(reader, 4),
                sector = Str(reader, 5),
                metre = Dec(reader, 6),
                gross = Dec(reader, 7),
                tare = Dec(reader, 8),
                cover = Dec(reader, 9),
                net = Dec(reader, 10),
                gpm = Dec(reader, 11),
                actualGsm = Dec(reader, 12),
                remarks = Str(reader, 13),
                party = Str(reader, 14),
                order = Str(reader, 15),
                warpQty = Dec(reader, 16),
                weftQty = Dec(reader, 17),
                warpCode = Str(reader, 18),
                weftCode = Str(reader, 19),
                fg = Str(reader, 20),
            });
        return rows;
    }

    public async Task<object> SaveLoomAsync(LoomSaveRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Company)) throw new ArgumentException("Choose a company.");
        var rolls = request.Rolls.Where(r => r.Net > 0).ToList();
        if (rolls.Count == 0) throw new ArgumentException("Enter at least one roll with a net weight.");
        if (rolls.Any(r => r.Metre <= 0)) throw new ArgumentException("Enter metre for every roll with a net weight.");
        if (rolls.Any(r => string.IsNullOrWhiteSpace(r.FgCode))) throw new ArgumentException("Select an FG code.");
        var bobbinTotal = rolls.Sum(r => r.WarpQty + r.WeftQty);
        if (Math.Round(bobbinTotal, 3) != Math.Round(rolls.Sum(r => r.Net), 3))
            throw new ArgumentException("Consumption does not match production.");

        await using var conn = _db.CreateConnection();
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
        try
        {
            await AssertDateOpen(conn, tx, request.Company, request.EntryDate, ct);
            var approved = await IsGodownApprover(conn, tx, request.Company, "Loom Godown", request.UserName, ct);
            var status = approved ? "Approved" : "Pending";
            var recordLogId = request.RecordLogId is > 0
                ? request.RecordLogId.Value
                : await SaveRecordLog(conn, tx, request.UserName, request.Company, "Loom Production Entry", ct);

            if (request.RecordLogId is > 0)
                await ReverseLoom(conn, tx, request.Company, recordLogId, ct);

            var groups = new Dictionary<string, (string name, decimal qty)>(StringComparer.OrdinalIgnoreCase);
            foreach (var roll in rolls)
            {
                roll.WarpCode = await ResolveItem(conn, tx, request.Company, "Loom Godown", roll.WarpCode, roll.WarpName, ct);
                roll.WeftCode = await ResolveItem(conn, tx, request.Company, "Loom Godown", roll.WeftCode, roll.WeftName, ct);
                AddBobbin(groups, roll.WarpCode, roll.WarpName, roll.WarpQty);
                AddBobbin(groups, roll.WeftCode, roll.WeftName, roll.WeftQty);
            }
            foreach (var pair in groups)
            {
                var stock = await ScalarDecimal(conn, tx, """
                    SELECT ROUND(ISNULL(StkInHand, 0), 2) FROM warehouse WITH (UPDLOCK, HOLDLOCK)
                    WHERE CompanyName = @c AND WarehouseName = 'Loom Godown' AND ItemCode = @code
                    """, ct, ("@c", request.Company), ("@code", pair.Key));
                if (pair.Value.qty > stock)
                    throw new InvalidOperationException($"Consumption is above Loom Godown stock for {pair.Value.name}.");
            }

            var prefix = await NextRollPrefix(conn, tx, request.Company, "Loom", request.EntryDate, ct);
            var index = prefix.index;
            var saved = new List<string>();
            foreach (var roll in rolls)
            {
                var rollNo = string.IsNullOrWhiteSpace(roll.RollNo) ? $"{prefix.stem}-{index:00000}" : roll.RollNo.Trim();
                var rollIndex = string.IsNullOrWhiteSpace(roll.RollNo) ? index : IndexOf(rollNo);
                await EnsureWarehouse(conn, tx, request.Company, "Loom Godown", roll.FgCode, ct);
                await Exec(conn, tx, """
                    INSERT INTO MISLOOMProductionEntryNew
                        (LoomNo, RollNo, Quality, Mesh, ReqGSM, Sector, Metre, GrossWt, TareWt, NetWt, ActualGPM, ActualGSM,
                         Remarks, CompanyName, Sysdate, UserName, Shift, RollIndex, Partyname, MarketingOrderno,
                         hodApprove, hodApprovedate, exciseApprove, exciseApprovedate, RecordLogid, CoverWt,
                         WarpQty, WeftQty, WarpBobbin, weftBobbin, FGItemCode)
                    VALUES
                        (@loom, @roll, @quality, @mesh, @gsm, @sector, @metre, @gross, @tare, @net, @gpm, @agsm,
                         @remarks, @c, @d, @user, @shift, @idx, @party, @ord,
                         @status, CASE WHEN @status = 'Approved' THEN CAST(GETDATE() AS date) ELSE NULL END,
                         'Pending', NULL, @log, @cover, @warpQty, @weftQty, @warp, @weft, @fg)
                    """, ct,
                    ("@loom", roll.LoomNo), ("@roll", rollNo), ("@quality", roll.Quality), ("@mesh", roll.Mesh),
                    ("@gsm", roll.Gsm), ("@sector", roll.Sector), ("@metre", roll.Metre), ("@gross", roll.Gross),
                    ("@tare", roll.Tare), ("@net", roll.Net), ("@gpm", roll.Gpm), ("@agsm", roll.ActualGsm),
                    ("@remarks", roll.Remarks), ("@c", request.Company), ("@d", request.EntryDate.Date),
                    ("@user", request.UserName ?? ""), ("@shift", request.Shift), ("@idx", rollIndex),
                    ("@party", roll.Party), ("@ord", roll.Order), ("@status", status), ("@log", recordLogId),
                    ("@cover", roll.Cover), ("@warpQty", roll.WarpQty), ("@weftQty", roll.WeftQty),
                    ("@warp", roll.WarpCode), ("@weft", roll.WeftCode), ("@fg", roll.FgCode));
                var transId = await ScalarInt(conn, tx, "SELECT MAX(transid) FROM MISLOOMProductionEntryNew", ct);
                await Journal(conn, tx, transId, request.EntryDate, request.Company, "Production", roll.FgCode, roll.Net, recordLogId, roll.FgCode, rollNo, "Loom", ct);
                await Journal(conn, tx, transId, request.EntryDate, request.Company, "Consumption", "Bobbin", roll.WarpQty, recordLogId, roll.WarpCode, "..", "Loom", ct);
                await Journal(conn, tx, transId, request.EntryDate, request.Company, "Consumption", "Bobbin", roll.WeftQty, recordLogId, roll.WeftCode, "..", "Loom", ct);
                await Exec(conn, tx, """
                    UPDATE warehouse SET StkInHand = ROUND(StkInHand + @qty, 2)
                    WHERE CompanyName = @c AND WarehouseName = 'Loom Godown' AND ItemCode = @code
                    """, ct, ("@qty", roll.Net), ("@c", request.Company), ("@code", roll.FgCode));
                if (string.IsNullOrWhiteSpace(roll.RollNo)) index++;
                saved.Add(rollNo);
            }

            var groupNo = await ScalarDecimal(conn, tx, "SELECT ISNULL(MAX(fGroupNo) + 1, 2000) FROM Prod_ConsumptionMaster_Bifercation", ct);
            foreach (var pair in groups)
            {
                await Exec(conn, tx, """
                    INSERT INTO Prod_ConsumptionMaster_Bifercation
                        (dsysDate, vItemBiferacation, vItemCode, vItemName, fConsumption, fGroupNo, vPlantName, vCompanyName, vUserName, fRateConsumption, fRateProduction, RecordLogid)
                    VALUES (@d, @name, @code, 'BOBIN', @qty, @grp, 'LOOM', @c, @user, 0, 0, @log)
                    """, ct,
                    ("@d", request.EntryDate.Date), ("@name", pair.Value.name), ("@code", pair.Key), ("@qty", pair.Value.qty),
                    ("@grp", groupNo), ("@c", request.Company), ("@user", request.UserName ?? ""), ("@log", recordLogId));
                await Exec(conn, tx, """
                    UPDATE warehouse SET StkInHand = ROUND(StkInHand - @qty, 2)
                    WHERE CompanyName = @c AND WarehouseName = 'Loom Godown' AND ItemCode = @code
                    """, ct, ("@qty", pair.Value.qty), ("@c", request.Company), ("@code", pair.Key));
            }
            await Exec(conn, tx, """
                INSERT INTO ConsumptionMaster (ItemName, Consumption, GroupNo, Date, PlantName, Companyname, Username, RecordLogid)
                VALUES ('Bobin', @net, @grp, @d, 'Loom', @c, @user, @log)
                """, ct,
                ("@net", rolls.Sum(r => r.Net)), ("@grp", groupNo), ("@d", request.EntryDate.Date),
                ("@c", request.Company), ("@user", request.UserName ?? ""), ("@log", recordLogId));

            await tx.CommitAsync(ct);
            return new { recordLogId, rollNos = saved };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task DeleteLoomAsync(ProductionDeleteRequest request, CancellationToken ct)
    {
        await using var conn = _db.CreateConnection();
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
        try
        {
            await ReverseLoom(conn, tx, request.Company, request.Id, ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<object> SaveNeedleAsync(NeedleSaveRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Company)) throw new ArgumentException("Choose a company.");
        var rolls = request.Rolls.Where(r => r.Net > 0).ToList();
        if (rolls.Count == 0) throw new ArgumentException("Enter at least one roll with a net weight.");
        if (rolls.Any(r => string.IsNullOrWhiteSpace(r.FgCode))) throw new ArgumentException("Select an FG code.");

        await using var conn = _db.CreateConnection();
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
        try
        {
            await AssertDateOpen(conn, tx, request.Company, request.EntryDate, ct);
            var approved = await IsGodownApprover(conn, tx, request.Company, "Needle Loom Godown", request.UserName, ct);
            var status = approved ? "Approved" : "Pending";
            var prefix = await NextRollPrefix(conn, tx, request.Company, "NeedleLoom", request.EntryDate, ct);
            var index = prefix.index;
            var saved = new List<object>();
            foreach (var roll in rolls)
            {
                if (roll.TransId is > 0)
                    await DeleteNeedleRow(conn, tx, request, roll.TransId.Value, ct);
                var rollNo = string.IsNullOrWhiteSpace(roll.RollNo) ? $"{prefix.stem}-{index:00000}" : roll.RollNo.Trim();
                var rollIndex = string.IsNullOrWhiteSpace(roll.RollNo) ? index : IndexOf(rollNo);
                await EnsureWarehouse(conn, tx, request.Company, "Roll Material Godown", roll.FgCode, ct);
                await Exec(conn, tx, """
                    INSERT INTO misneedleloomproductionentry
                        (RollNo, Size, ReqGPM, Color, Marking, Quality, NetWt, Remarks, CompanyName, Sysdate, Shift, RollIndex,
                         LoomNo, PartyName, MarketingOrdNo, HODApprove, HODApproveDate, ExiceApprove, ExiceApproveDate, RollType, UserName, FGITEMCODE, IsCons)
                    VALUES
                        (@roll, @size, @gpm, @colour, @marking, @quality, @net, @remarks, @c, @d, @shift, @idx,
                         @loom, @party, @ord, @status, CASE WHEN @status = 'Approved' THEN CAST(GETDATE() AS date) ELSE NULL END,
                         'Pending', NULL, @type, @user, @fg, 1)
                    """, ct,
                    ("@roll", rollNo), ("@size", roll.Size), ("@gpm", roll.ReqGpm), ("@colour", roll.Colour),
                    ("@marking", roll.Marking), ("@quality", roll.Quality), ("@net", roll.Net), ("@remarks", string.IsNullOrWhiteSpace(roll.Remarks) ? "." : roll.Remarks),
                    ("@c", request.Company), ("@d", request.EntryDate.Date), ("@shift", request.Shift), ("@idx", rollIndex),
                    ("@loom", roll.LoomNo), ("@party", roll.Party), ("@ord", roll.Order), ("@status", status),
                    ("@type", string.IsNullOrWhiteSpace(roll.RollType) ? "PP Webbing & Tie" : roll.RollType),
                    ("@user", request.UserName ?? ""), ("@fg", roll.FgCode));
                var transId = await ScalarInt(conn, tx, "SELECT MAX(transid) FROM misneedleloomproductionentry", ct);
                var fgName = await ScalarString(conn, tx, "SELECT TOP 1 ItemName FROM item WHERE ItemCode = @code AND CompanyName = @c", ct, ("@code", roll.FgCode), ("@c", request.Company));
                await Journal(conn, tx, transId, request.EntryDate, request.Company, "Production", string.IsNullOrWhiteSpace(fgName) ? roll.FgCode : fgName, roll.Net, transId, roll.FgCode, rollNo, "NLoom", ct);
                await Exec(conn, tx, """
                    UPDATE warehouse SET StkInHand = ROUND(StkInHand + @qty, 2)
                    WHERE CompanyName = @c AND WarehouseName = 'Roll Material Godown' AND ItemCode = @code
                    """, ct, ("@qty", roll.Net), ("@c", request.Company), ("@code", roll.FgCode));
                saved.Add(new { transId, rollNo });
                if (string.IsNullOrWhiteSpace(roll.RollNo)) index++;
            }
            await tx.CommitAsync(ct);
            return new { rolls = saved, net = rolls.Sum(r => r.Net) };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task DeleteNeedleAsync(ProductionDeleteRequest request, CancellationToken ct)
    {
        await using var conn = _db.CreateConnection();
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
        try
        {
            await DeleteNeedleRow(conn, tx, new NeedleSaveRequest { Company = request.Company, UserName = request.UserName, EntryDate = request.EntryDate }, request.Id, ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<List<object>> RecentNeedleAsync(string company, DateTime onDate, CancellationToken ct)
    {
        await using var conn = _db.CreateConnection();
        const string sql = """
            SELECT TRANSID, Shift, RollNo, NetWt, LoomNo
            FROM misneedleloomproductionentry WITH (NOLOCK)
            WHERE CompanyName = @c AND Sysdate = @d
            ORDER BY TRANSID DESC
            """;
        var rows = new List<object>();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@c", company);
        cmd.Parameters.AddWithValue("@d", onDate.Date);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(new { id = reader.GetInt32(0), shift = Str(reader, 1), rollNo = Str(reader, 2), net = Dec(reader, 3), loom = Convert.ToString(reader.GetValue(4)) ?? "" });
        return rows;
    }

    public async Task<object> SaveLaminationAsync(LaminationSaveRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Company)) throw new ArgumentException("Choose a company.");
        var rolls = request.Rolls.Where(r => r.LamNet > 0).ToList();
        if (rolls.Count == 0) throw new ArgumentException("Tick at least one roll and enter the laminated net weight.");
        if (rolls.Any(r => string.IsNullOrWhiteSpace(r.LamCode))) throw new ArgumentException("A ticked roll needs an FG LAM code.");
        if (rolls.Any(r => string.IsNullOrWhiteSpace(r.LamRoll))) throw new ArgumentException("A ticked roll needs a laminated roll number.");

        await using var conn = _db.CreateConnection();
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
        try
        {
            await AssertDateOpen(conn, tx, request.Company, request.EntryDate, ct);
            var approved = await IsGodownApprover(conn, tx, request.Company, "Lamination Godown", request.UserName, ct);
            var status = approved ? "Approved" : "Pending";
            var index = await ScalarInt(conn, tx, "SELECT ISNULL(MAX(RollIndex), 0) FROM MISLaminationEntry WHERE Companyname = @c", ct, ("@c", request.Company));
            var saved = new List<object>();
            foreach (var roll in rolls)
            {
                index++;
                await EnsureWarehouse(conn, tx, request.Company, "Lamination Godown", roll.UlCode, ct);
                await EnsureWarehouse(conn, tx, request.Company, "Lamination Godown", roll.LamCode, ct);
                var lamNet = roll.LamGross - roll.LamTare;
                await Exec(conn, tx, """
                    INSERT INTO MISLaminationEntry
                        (URollNo, UMetre, UGrossWt, UTareWt, UNetWt, RollNo, Metre, GrossWt, TareWT, NetWt, ActualGPM, ActualGSM,
                         Remarks, Companyname, Sysdate, shift, Username, Size, GSM, Quality, color, Category, Sector, RollIndex,
                         HodApprove, HodApprovedate, ExciseApprove, ExciseApprovedate, vInternalRoll, FGItemCode, FGLAMCode, IsCons)
                    VALUES
                        (@uRoll, @uMetre, @uGross, @uTare, @uNet, @roll, @metre, @gross, @tare, @net, @gpm, @gsm,
                         @remarks, @c, @d, @shift, @user, @size, @lamGsm, @quality, @colour, @category, @sector, @idx,
                         @status, CASE WHEN @status = 'Approved' THEN CAST(GETDATE() AS date) ELSE NULL END,
                         'Pending', NULL, @internal, @ul, @lam, 1)
                    """, ct,
                    ("@uRoll", roll.SourceRoll), ("@uMetre", roll.SourceMetre), ("@uGross", roll.SourceGross), ("@uTare", roll.SourceTare), ("@uNet", roll.SourceNet),
                    ("@roll", roll.LamRoll), ("@metre", roll.LamMetre), ("@gross", roll.LamGross), ("@tare", roll.LamTare), ("@net", lamNet),
                    ("@gpm", roll.ActualGpm), ("@gsm", roll.ActualGsm), ("@remarks", roll.Remarks), ("@c", request.Company), ("@d", request.EntryDate.Date),
                    ("@shift", request.Shift), ("@user", request.UserName ?? ""), ("@size", roll.LamSize), ("@lamGsm", roll.LamGsm),
                    ("@quality", roll.Quality), ("@colour", roll.Colour), ("@category", roll.Category), ("@sector", roll.Sector), ("@idx", index),
                    ("@status", status), ("@internal", roll.InternalRoll), ("@ul", roll.UlCode), ("@lam", roll.LamCode));
                var transId = await ScalarInt(conn, tx, "SELECT MAX(transid) FROM MISLaminationEntry", ct);
                await Journal(conn, tx, transId, request.EntryDate, request.Company, "Consumption", roll.UlCode, roll.SourceNet, transId, roll.UlCode, roll.SourceRoll, "Lamination", ct);
                await Journal(conn, tx, transId, request.EntryDate, request.Company, "Production", roll.LamCode, lamNet, transId, roll.LamCode, roll.LamRoll, "Lamination", ct);
                await Exec(conn, tx, """
                    UPDATE warehouse SET StkInHand = CASE WHEN ROUND(StkInHand - @qty, 2) < 0 THEN 0 ELSE ROUND(StkInHand - @qty, 2) END
                    WHERE CompanyName = @c AND WarehouseName = 'Lamination Godown' AND ItemCode = @code
                    """, ct, ("@qty", roll.SourceNet), ("@c", request.Company), ("@code", roll.UlCode));
                await Exec(conn, tx, """
                    UPDATE warehouse SET StkInHand = ROUND(StkInHand + @qty, 2)
                    WHERE CompanyName = @c AND WarehouseName = 'Lamination Godown' AND ItemCode = @code
                    """, ct, ("@qty", lamNet), ("@c", request.Company), ("@code", roll.LamCode));
                saved.Add(new { transId, rollNo = roll.LamRoll });
            }
            await tx.CommitAsync(ct);
            return new
            {
                rolls = saved,
                source = rolls.Sum(r => r.SourceNet),
                laminated = rolls.Sum(r => r.LamGross - r.LamTare),
            };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<List<object>> RecentLaminationAsync(string company, DateTime onDate, CancellationToken ct)
    {
        await using var conn = _db.CreateConnection();
        const string sql = """
            SELECT transid, shift, RollNo, UNetWt, NetWt
            FROM MISLaminationEntry WITH (NOLOCK)
            WHERE Companyname = @c AND Sysdate = @d
            ORDER BY transid DESC
            """;
        var rows = new List<object>();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@c", company);
        cmd.Parameters.AddWithValue("@d", onDate.Date);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(new { id = reader.GetInt32(0), shift = Str(reader, 1), rollNo = Str(reader, 2), source = Dec(reader, 3), laminated = Dec(reader, 4) });
        return rows;
    }

    private static async Task ReverseLoom(SqlConnection conn, SqlTransaction tx, string company, int recordLogId, CancellationToken ct)
    {
        await using (var cmd = new SqlCommand("""
            SELECT FGItemCode, NetWt FROM MISLOOMProductionEntryNew WITH (UPDLOCK, HOLDLOCK)
            WHERE CompanyName = @c AND RecordLogid = @id
            """, conn, tx))
        {
            cmd.Parameters.AddWithValue("@c", company);
            cmd.Parameters.AddWithValue("@id", recordLogId);
            var rows = new List<(string code, decimal net)>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                rows.Add((Str(reader, 0), Dec(reader, 1)));
            await reader.CloseAsync();
            foreach (var row in rows)
                await Exec(conn, tx, """
                    UPDATE warehouse SET StkInHand = ROUND(StkInHand - @qty, 2)
                    WHERE CompanyName = @c AND WarehouseName = 'Loom Godown' AND ItemCode = @code
                    """, ct, ("@qty", row.net), ("@c", company), ("@code", row.code));
        }
        await Exec(conn, tx, "DELETE FROM Prod_ConsumptionMaster_Bifercation WHERE RecordLogid = @id AND vPlantName = 'LOOM'", ct, ("@id", recordLogId));
        await Exec(conn, tx, "DELETE FROM MISLOOMProductionEntryNew WHERE CompanyName = @c AND RecordLogid = @id", ct, ("@c", company), ("@id", recordLogId));
        await Exec(conn, tx, "DELETE FROM ConsumptionMaster WHERE Companyname = @c AND RecordLogid = @id AND PlantName = 'Loom'", ct, ("@c", company), ("@id", recordLogId));
        await Exec(conn, tx, "DELETE FROM stockJournalProd WHERE recordLogId = @id AND plantname = 'Loom' AND company_name = @c", ct, ("@id", recordLogId), ("@c", company));
    }

    private static async Task DeleteNeedleRow(SqlConnection conn, SqlTransaction tx, NeedleSaveRequest request, int transId, CancellationToken ct)
    {
        string rollNo = "";
        decimal net = 0;
        string fg = "";
        await using (var cmd = new SqlCommand("""
            SELECT RollNo, NetWt, FGITEMCODE FROM misneedleloomproductionentry WITH (UPDLOCK, HOLDLOCK)
            WHERE TRANSID = @id AND CompanyName = @c
            """, conn, tx))
        {
            cmd.Parameters.AddWithValue("@id", transId);
            cmd.Parameters.AddWithValue("@c", request.Company);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) throw new InvalidOperationException("Needle loom roll was not found.");
            rollNo = Str(reader, 0);
            net = Dec(reader, 1);
            fg = Str(reader, 2);
        }
        await Exec(conn, tx, "DELETE FROM Prod_RMD_InOut WHERE vItemCode = @roll", ct, ("@roll", rollNo));
        await Exec(conn, tx, """
            UPDATE warehouse SET StkInHand = ROUND(StkInHand - @qty, 2)
            WHERE CompanyName = @c AND WarehouseName = 'Roll Material Godown' AND ItemCode = @code
            """, ct, ("@qty", net), ("@c", request.Company), ("@code", fg));
        var name = await ScalarString(conn, tx, "SELECT TOP 1 ItemName FROM item WHERE ItemCode = @code AND CompanyName = @c", ct, ("@code", fg), ("@c", request.Company));
        await Journal(conn, tx, transId, request.EntryDate, request.Company, "Production", string.IsNullOrWhiteSpace(name) ? fg : name, -net, transId, fg, rollNo, "NLoom", ct);
        await Exec(conn, tx, "DELETE FROM misneedleloomproductionentry WHERE TRANSID = @id AND RollNo = @roll", ct, ("@id", transId), ("@roll", rollNo));
    }

    private static void AddBobbin(Dictionary<string, (string name, decimal qty)> groups, string code, string name, decimal qty)
    {
        if (qty <= 0 || string.IsNullOrWhiteSpace(code)) return;
        if (groups.TryGetValue(code, out var existing)) groups[code] = (existing.name, existing.qty + qty);
        else groups[code] = (string.IsNullOrWhiteSpace(name) ? code : name, qty);
    }

    private static async Task<string> ResolveItem(SqlConnection conn, SqlTransaction tx, string company, string warehouse, string code, string name, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(code)) return code.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Each roll needs a warp bobbin and a weft bobbin.");
        var found = await ScalarString(conn, tx, """
            SELECT TOP 1 ItemCode FROM warehouse
            WHERE CompanyName = @c AND WarehouseName = @w AND ItemName = @n
            """, ct, ("@c", company), ("@w", warehouse), ("@n", name.Trim()));
        if (string.IsNullOrWhiteSpace(found)) throw new ArgumentException($"Bobbin {name} is not in {warehouse}.");
        return found;
    }

    private static async Task<(string stem, int index)> NextRollPrefix(SqlConnection conn, SqlTransaction tx, string company, string kind, DateTime date, CancellationToken ct)
    {
        var code = await ScalarString(conn, tx, "SELECT TOP 1 ISNULL(IndentCode, '') FROM FactoryInfo WHERE Name = @c", ct, ("@c", company));
        var month = "ABCDEFGHIJKL"[date.Month - 1];
        var stem = kind == "NeedleLoom"
            ? $"{code}-NL-I{date:yy}{month}"
            : $"{code}-L-I{date:yy}{month}";
        var table = kind == "NeedleLoom" ? "misneedleloomproductionentry" : "misloomproductionentrynew";
        var index = await ScalarInt(conn, tx, $"""
            SELECT ISNULL(MAX(RollIndex), 0) + 1 FROM {table}
            WHERE CompanyName = @c AND Sysdate >= @from AND Sysdate < @to
            """, ct, ("@c", company), ("@from", new DateTime(date.Year, 1, 1)), ("@to", new DateTime(date.Year + 1, 1, 1)));
        if (index < 1) index = 1;
        return (stem, index);
    }

    private static int IndexOf(string rollNo)
    {
        var parts = rollNo.Split('-');
        return int.TryParse(parts.Length == 0 ? "" : parts[^1], out var n) ? n : 0;
    }

    private static async Task EnsureWarehouse(SqlConnection conn, SqlTransaction tx, string company, string warehouse, string itemCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(itemCode)) return;
        var count = await ScalarInt(conn, tx, """
            SELECT COUNT(*) FROM warehouse WHERE ItemCode = @code AND CompanyName = @c AND WarehouseName = @w
            """, ct, ("@code", itemCode), ("@c", company), ("@w", warehouse));
        if (count > 0) return;
        await Exec(conn, tx, """
            INSERT INTO warehouse
                (ItemCode, ItemName, GroupName, Deptt, Minlevel, Maxlevel, ReOrder, Unit, StkInHand, TypeVendor, NameVendor, EmailVendor,
                 QtyOrder, MnthlyConsum, Rate, CompanyName, LoginName, Dummy1, OpeningStock, SubGroupName, WareHouseName)
            SELECT ItemCode, ItemName, GroupName, Deptt, Minlevel, Maxlevel, ReOrder, Unit, 0, TypeVendor, NameVendor, EmailVendor,
                   QtyOrder, MnthlyConsum, Rate, CompanyName, LoginName, Dummy1, OpeningStock, SubGroupName, @w
            FROM item WHERE ItemCode = @code AND CompanyName = @c
            """, ct, ("@w", warehouse), ("@code", itemCode), ("@c", company));
    }

    private static async Task<List<object>> Stock(SqlConnection conn, string company, string warehouse, CancellationToken ct)
    {
        var rows = new List<object>();
        await using var cmd = new SqlCommand("""
            SELECT ItemCode, ItemName, ROUND(StkInHand, 2)
            FROM warehouse WITH (NOLOCK)
            WHERE StkInHand > 0 AND CompanyName = @c AND WarehouseName = @w
            ORDER BY ItemName
            """, conn);
        cmd.Parameters.AddWithValue("@c", company);
        cmd.Parameters.AddWithValue("@w", warehouse);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(new { itemCode = Str(reader, 0), item = Str(reader, 1), stock = Dec(reader, 2) });
        return rows;
    }

    private static async Task AssertDateOpen(SqlConnection conn, SqlTransaction tx, string company, DateTime date, CancellationToken ct)
    {
        var frozen = await ScalarInt(conn, tx, """
            SELECT COUNT(*) FROM freezeProdEntry WITH (NOLOCK)
            WHERE CompanyName = @c
              AND @d <= (SELECT DATEADD(day, -2, MAX(sysdate)) FROM freezeProdEntry WITH (NOLOCK) WHERE companyname = @c)
            """, ct, ("@c", company), ("@d", date.Date));
        if (frozen > 0) throw new InvalidOperationException("Entry on this Date is Freezed. So, You Can't do Entry.");
    }

    private static async Task<bool> IsGodownApprover(SqlConnection conn, SqlTransaction tx, string company, string warehouse, string user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(user)) return false;
        var n = await ScalarInt(conn, tx, """
            SELECT COUNT(*) FROM warehousemaster
            WHERE companyname = @c AND warehousename = @w
              AND @u IN (approvalauth1, approvalauth2, approvalauth3, approvalauth4, approvalauth5)
            """, ct, ("@c", company), ("@w", warehouse), ("@u", user.Trim()));
        return n > 0;
    }

    private static async Task<int> SaveRecordLog(SqlConnection conn, SqlTransaction tx, string user, string company, string remarks, CancellationToken ct)
    {
        await using var cmd = new SqlCommand("dbo.sp_SaveRecordLog", conn, tx) { CommandType = System.Data.CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@RecordLogId", DBNull.Value);
        cmd.Parameters.AddWithValue("@UserName", user ?? "");
        cmd.Parameters.AddWithValue("@Flag", "RG");
        cmd.Parameters.AddWithValue("@Remarks", remarks + " " + company);
        var id = await cmd.ExecuteScalarAsync(ct);
        return id == null || id is DBNull ? 0 : Convert.ToInt32(id);
    }

    private static async Task Journal(
        SqlConnection conn, SqlTransaction tx, int journalNo, DateTime date, string company, string type, string itemName,
        decimal qty, int recordLogId, string itemCode, string rollNo, string plant, CancellationToken ct)
    {
        var start = date.Month >= 4 ? date.Year : date.Year - 1;
        var year = $"{start % 100:D2}-{(start + 1) % 100:D2}";
        await Exec(conn, tx, """
            EXEC dbo.Insert_StockJournal
                @stock_journal_no, @sysdate, @companyname, @type, @itemname, @qty, @rate, @amount, @unit,
                @yr, @recordlogid, @srno, @itemcode, @rollno, @plantname
            """, ct,
            ("@stock_journal_no", journalNo), ("@sysdate", date.Date), ("@companyname", company), ("@type", type),
            ("@itemname", itemName ?? ""), ("@qty", qty), ("@rate", 0m), ("@amount", 0m), ("@unit", "KGS"),
            ("@yr", year), ("@recordlogid", recordLogId), ("@srno", journalNo), ("@itemcode", itemCode ?? ""),
            ("@rollno", rollNo ?? ""), ("@plantname", plant));
    }

    private static async Task Exec(SqlConnection conn, SqlTransaction tx, string sql, CancellationToken ct, params (string name, object? value)[] args)
    {
        await using var cmd = new SqlCommand(sql, conn, tx);
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<int> ScalarInt(SqlConnection conn, SqlTransaction? tx, string sql, CancellationToken ct, params (string name, object? value)[] args)
    {
        await using var cmd = new SqlCommand(sql, conn, tx);
        foreach (var (name, arg) in args) cmd.Parameters.AddWithValue(name, arg ?? DBNull.Value);
        var value = await cmd.ExecuteScalarAsync(ct);
        return value == null || value is DBNull ? 0 : Convert.ToInt32(value);
    }

    private static async Task<decimal> ScalarDecimal(SqlConnection conn, SqlTransaction? tx, string sql, CancellationToken ct, params (string name, object? value)[] args)
    {
        await using var cmd = new SqlCommand(sql, conn, tx);
        foreach (var (name, arg) in args) cmd.Parameters.AddWithValue(name, arg ?? DBNull.Value);
        var value = await cmd.ExecuteScalarAsync(ct);
        return value == null || value is DBNull ? 0m : Convert.ToDecimal(value);
    }

    private static async Task<string> ScalarString(SqlConnection conn, SqlTransaction? tx, string sql, CancellationToken ct, params (string name, object? value)[] args)
    {
        await using var cmd = new SqlCommand(sql, conn, tx);
        foreach (var (name, arg) in args) cmd.Parameters.AddWithValue(name, arg ?? DBNull.Value);
        var value = await cmd.ExecuteScalarAsync(ct);
        return value == null || value is DBNull ? "" : Convert.ToString(value) ?? "";
    }

    private static string Str(SqlDataReader reader, int i) => reader.IsDBNull(i) ? "" : Convert.ToString(reader.GetValue(i)) ?? "";
    private static decimal Dec(SqlDataReader reader, int i) => reader.IsDBNull(i) ? 0m : Convert.ToDecimal(reader.GetValue(i));
    private static decimal DecAt(SqlDataReader reader, int i) => Dec(reader, i);
    private static string Col(SqlDataReader reader, Dictionary<string, int> map, string name) => map.TryGetValue(name, out var i) ? Str(reader, i) : "";
    private static decimal ColDec(SqlDataReader reader, Dictionary<string, int> map, string name) => map.TryGetValue(name, out var i) ? Dec(reader, i) : 0m;
}
