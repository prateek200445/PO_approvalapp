using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class PlantConsumptionService
{
    private static readonly HashSet<string> PpQualities = new(StringComparer.Ordinal)
    {
        "PP", "PP Granuals", "HD", "HDPE Granuals"
    };

    private readonly DatabaseService _db;

    public PlantConsumptionService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<object>> GetCompaniesAsync(CancellationToken ct)
    {
        const string sql = "SELECT srno, Name FROM FactoryInfo ORDER BY Name";
        var rows = new List<object>();
        await using var conn = _db.CreateConnection();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(new { srNo = reader.GetInt32(0), name = reader.GetString(1) });
        return rows;
    }

    public async Task<object> GetLookupsAsync(string company, CancellationToken ct)
    {
        await using var conn = _db.CreateConnection();
        var plants = await Strings(conn, """
            SELECT DISTINCT plantname FROM plantmaster
            WHERE companyname = @c AND plantname IS NOT NULL AND plantname <> ''
            ORDER BY plantname
            """, company, ct);
        var sectors = await Strings(conn, """
            SELECT SectorName FROM sectormaster
            WHERE SectorName IS NOT NULL AND SectorName <> ''
            ORDER BY SectorName
            """, company, ct);
        var buyers = await Buyers(conn, company, ct);
        return new { plants, sectors, buyers };
    }

    public async Task<object> GetPlantSetupAsync(string company, string plant, DateTime onDate, CancellationToken ct)
    {
        await using var conn = _db.CreateConnection();
        var subs = await Strings(conn, """
            SELECT DISTINCT plantsubname FROM plantsubmaster
            WHERE companyname = @c AND plantname = @p AND plantsubname IS NOT NULL
            ORDER BY plantsubname
            """, company, ct, ("@p", plant));
        var fromWh = await Strings(conn, """
            SELECT DISTINCT fromwarehouse FROM plantmaster
            WHERE companyname = @c AND plantname = @p AND fromwarehouse IS NOT NULL AND fromwarehouse <> ''
            ORDER BY fromwarehouse
            """, company, ct, ("@p", plant));
        var toWh = await Strings(conn, """
            SELECT DISTINCT towarehouse FROM plantmaster
            WHERE companyname = @c AND plantname = @p AND towarehouse IS NOT NULL AND towarehouse <> ''
            ORDER BY towarehouse
            """, company, ct, ("@p", plant));
        var products = await Strings(conn, """
            SELECT DISTINCT itemname FROM warehouse WITH (NOLOCK)
            WHERE companyname = @c
              AND itemname IN (
                SELECT DISTINCT plantitemname FROM plantitemmaster WITH (NOLOCK)
                WHERE companyname = @c AND plantname = @p)
              AND itemcode IN (
                SELECT itemcode FROM item
                WHERE companyname = @c
                  AND ((@d BETWEEN FROMDATE AND TODATE) OR (FROMDATE <= @d AND TODATE IS NULL)))
            ORDER BY itemname
            """, company, ct, ("@p", plant), ("@d", onDate.Date));
        return new { subs, fromWarehouses = fromWh, toWarehouses = toWh, products };
    }

    public async Task<List<object>> GetMaterialsAsync(string company, string warehouse, DateTime onDate, CancellationToken ct)
    {
        const string sql = """
            SELECT subgroupname, ItemName, itemcode, ROUND(ISNULL(StkInHand, 0), 2)
            FROM warehouse WITH (NOLOCK)
            WHERE Deptt IN ('RM','SF','FG')
              AND companyname = @c
              AND warehousename = @w
              AND itemcode IN (
                SELECT itemcode FROM item
                WHERE companyname = @c
                  AND ((@d BETWEEN FROMDATE AND TODATE) OR (FROMDATE <= @d AND TODATE IS NULL)))
            ORDER BY subgroupname, ItemName
            """;
        var rows = new List<object>();
        await using var conn = _db.CreateConnection();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@c", company);
        cmd.Parameters.AddWithValue("@w", warehouse);
        cmd.Parameters.AddWithValue("@d", onDate.Date);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new
            {
                quality = reader.IsDBNull(0) ? "" : reader.GetString(0),
                grade = reader.IsDBNull(1) ? "" : reader.GetString(1),
                itemCode = reader.IsDBNull(2) ? "" : reader.GetString(2),
                stock = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3)),
            });
        }
        return rows;
    }

    public async Task<List<object>> GetRecentAsync(string company, string plant, DateTime asOf, CancellationToken ct)
    {
        const string sql = """
            SELECT GroupSrNo,
                   MAX(sysDate),
                   MAX(Shift),
                   MAX(PlantItemName),
                   MAX(ProductionType),
                   SUM(ISNULL(Qty, 0)),
                   MAX(ISNULL(Wastage, 0))
                     + MAX(ISNULL(fTrimWastage, 0))
                     + MAX(ISNULL(fSweepingWastage, 0))
                     + MAX(ISNULL(fTapeLumpsWastage, 0))
                     + MAX(ISNULL(fFabricTrim, 0))
                     + MAX(ISNULL(fFabricwaste, 0))
                     + MAX(ISNULL(fNewLumpsWs, 0)),
                   MAX(buyername),
                   MAX(hodapprove),
                   MAX(exciseapprove)
            FROM TapePlantConsumption WITH (NOLOCK)
            WHERE companyname = @c
              AND plantname = @p
              AND sysDate >= DATEADD(day, -30, @d)
              AND sysDate < DATEADD(day, 1, @d)
            GROUP BY GroupSrNo
            ORDER BY GroupSrNo DESC
            """;
        var rows = new List<object>();
        await using var conn = _db.CreateConnection();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@c", company);
        cmd.Parameters.AddWithValue("@p", plant);
        cmd.Parameters.AddWithValue("@d", asOf.Date);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var consumption = Convert.ToDecimal(reader.GetValue(5));
            var waste = Convert.ToDecimal(reader.GetValue(6));
            rows.Add(new
            {
                groupSrNo = Convert.ToInt32(reader.GetValue(0)),
                entryDate = reader.GetDateTime(1).ToString("yyyy-MM-dd"),
                shift = reader.IsDBNull(2) ? "" : reader.GetString(2),
                product = reader.IsDBNull(3) ? "" : reader.GetString(3),
                productionType = reader.IsDBNull(4) ? "" : reader.GetString(4),
                consumption,
                wastage = waste,
                netProduction = consumption - waste,
                buyer = reader.IsDBNull(7) ? "" : reader.GetString(7),
                hod = reader.IsDBNull(8) ? "" : reader.GetString(8),
                excise = reader.IsDBNull(9) ? "" : reader.GetString(9),
            });
        }
        return rows;
    }

    public async Task<object?> GetEntryAsync(string company, int groupSrNo, CancellationToken ct)
    {
        const string sql = """
            SELECT sysDate, PlantName, PlantSubName, PlantItemName, Quality, Grade, Shift, Sector, Qty,
                   Wastage, InTime, OutTime, buyername, Marketinginvno, buyerdate, itemcode, ProductionType,
                   fTrimWastage, fSweepingWastage, fTapeLumpsWastage, fFabricTrim, fFabricwaste, fNewLumpsWs, hodapprove
            FROM TapePlantConsumption WITH (NOLOCK)
            WHERE companyname = @c AND GroupSrNo = @g
            ORDER BY itemcode
            """;
        await using var conn = _db.CreateConnection();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@c", company);
        cmd.Parameters.AddWithValue("@g", groupSrNo);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        object? header = null;
        var lines = new List<object>();
        while (await reader.ReadAsync(ct))
        {
            header ??= new
            {
                entryDate = reader.GetDateTime(0).ToString("yyyy-MM-dd"),
                plant = Str(reader, 1),
                plantSub = Str(reader, 2),
                product = Str(reader, 3),
                shift = Str(reader, 6),
                sector = Str(reader, 7),
                timeIn = Str(reader, 10),
                timeOut = Str(reader, 11),
                buyer = Str(reader, 12),
                marketingInvoice = Str(reader, 13),
                buyerOrderDate = reader.IsDBNull(14) ? "" : reader.GetDateTime(14).ToString("yyyy-MM-dd"),
                productionType = Str(reader, 16),
                wastage = Dec(reader, 9),
                trimWastage = reader.GetString(1).Equals("Lamination", StringComparison.OrdinalIgnoreCase)
                    ? Dec(reader, 17)
                    : Dec(reader, 19),
                sweepingWastage = Dec(reader, 18),
                fabricTrim = Dec(reader, 20),
                fabricWaste = Dec(reader, 21),
                lumpsWastage = Dec(reader, 22),
            };
            lines.Add(new
            {
                quality = Str(reader, 4),
                grade = Str(reader, 5),
                qty = Dec(reader, 8),
                itemCode = Str(reader, 15),
            });
        }
        if (header == null) return null;
        return new { header, lines };
    }

    public async Task<decimal> GetRollsAsync(string company, string plant, DateTime onDate, string shift, CancellationToken ct)
    {
        await using var conn = _db.CreateConnection();
        return await RollProductionAsync(conn, null, company, plant, onDate, shift, ct);
    }

    public async Task<int> SaveAsync(PlantConsumptionSaveRequest request, CancellationToken ct)
    {
        Validate(request);
        var plant = request.Plant.Trim();
        var isLam = plant.Equals("Lamination", StringComparison.OrdinalIgnoreCase);
        var isTape = plant.Equals("Tape Plant", StringComparison.OrdinalIgnoreCase);
        var lines = PrepareLines(request.Lines, isTape);
        if (lines.Count == 0)
            throw new ArgumentException("Add at least one material with a quantity.");
        if (lines.Any(l => l.grade.Equals(request.Product.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Product Name and Grade should not be same");

        var trim = isLam || isTape ? request.TrimWastage : 0m;
        var sweeping = isLam || isTape ? request.SweepingWastage : 0m;
        var fabricTrim = isLam ? request.FabricTrim : 0m;
        var fabricWaste = isLam ? request.FabricWaste : 0m;
        var lumps = isLam ? request.LumpsWastage : 0m;
        var waste = request.Wastage + trim + sweeping + fabricTrim + fabricWaste + lumps;
        var consumption = lines.Sum(l => l.qty);
        if (consumption - waste < 0)
            throw new ArgumentException("Net Production can't be negative");

        await using var conn = _db.CreateConnection();
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
        try
        {
            await AssertDateOpenAsync(conn, tx, request, ct);

            if (request.GroupSrNo is > 0)
                await ReverseAsync(conn, tx, request.Company, request.Plant, request.FromWarehouse, request.ToWarehouse, request.GroupSrNo.Value, ct);

            foreach (var line in lines)
            {
                var stock = await ScalarDecimal(conn, tx, """
                    SELECT ROUND(ISNULL(StkInHand, 0), 2) FROM warehouse WITH (UPDLOCK, HOLDLOCK)
                    WHERE companyname = @c AND warehousename = @w AND subgroupname = @q AND itemname = @g
                    """, ct,
                    ("@c", request.Company), ("@w", request.FromWarehouse), ("@q", line.quality), ("@g", line.grade));
                if (stock < line.qty)
                    throw new InvalidOperationException($"Not enough stock of {line.grade} in {request.FromWarehouse}. On hand {stock:0.00}, entered {line.qty:0.00}.");
            }

            var group = request.GroupSrNo is > 0
                ? request.GroupSrNo.Value
                : await ScalarInt(conn, tx, "SELECT ISNULL(MAX(GroupSrNo), 0) + 1 FROM TapePlantConsumption WITH (UPDLOCK, HOLDLOCK)", ct);

            var hod = await IsApprover(conn, tx, request, ct) ? "Approved" : "Pending";
            var exciseBlank = await ScalarString(conn, tx, """
                SELECT ISNULL(Approvalauth1, '') FROM WareHouseMaster
                WHERE companyname = @c AND warehousename = @w
                """, ct, ("@c", request.Company), ("@w", request.FromWarehouse));
            var excise = string.IsNullOrWhiteSpace(exciseBlank) ? "Approved" : "Pending";
            DateTime? exciseDate = excise == "Approved" ? DateTime.Today : null;

            if (request.GroupSrNo is not > 0)
                await BlockIfYesterdayPending(conn, tx, request, ct);

            var productCount = await ScalarInt(conn, tx, """
                SELECT COUNT(*) FROM warehouse WITH (NOLOCK)
                WHERE itemname = @n AND companyname = @c AND WareHouseName = @w
                """, ct, ("@n", request.Product.Trim()), ("@c", request.Company), ("@w", request.ToWarehouse));
            if (productCount == 0)
                throw new InvalidOperationException($"{request.Product} is not in {request.ToWarehouse}.");

            await AssertProductionMatch(conn, tx, request, consumption - waste, ct);

            var skipFinishedStock = isLam || plant.Equals("Liner Roll Plant", StringComparison.OrdinalIgnoreCase);
            var first = true;
            foreach (var line in lines)
            {
                var itemCode = await ScalarString(conn, tx, """
                    SELECT TOP 1 itemcode FROM warehouse WITH (NOLOCK)
                    WHERE companyname = @c AND warehousename = @w AND subgroupname = @q AND itemname = @g
                    """, ct,
                    ("@c", request.Company), ("@w", request.FromWarehouse), ("@q", line.quality), ("@g", line.grade));
                if (string.IsNullOrWhiteSpace(itemCode))
                    throw new InvalidOperationException($"{line.grade} is not in {request.FromWarehouse}.");

                await Exec(conn, tx, """
                    INSERT INTO TapePlantConsumption (
                      sysDate, PlantName, PlantSubName, PlantItemName, Quality, Grade, Shift, Sector, Qty, Wastage,
                      GroupSrNo, InTime, OutTime, companyname, buyername, Marketinginvno, buyerdate,
                      hodapprove, hodapproveDate, exciseapprove, exciseapproveDate, itemcode, ProductionType,
                      fTrimWastage, fSweepingWastage, fTapeLumpsWastage, fTapeOthersWastage,
                      RateConsumption, RateWastage, RateProduction, fFabricTrim, fFabricwaste, fNewLumpsWs)
                    VALUES (
                      @date, @plant, @sub, @product, @quality, @grade, @shift, @sector, @qty, @waste,
                      @group, @tin, @tout, @company, @buyer, @inv, @bdate,
                      @hod, NULL, @excise, @edate, @code, @ptype,
                      @trim, @sweep, @lumps, @sweep,
                      0, 15, 0, @ftrim, @fwaste, @flumps)
                    """, ct,
                    ("@date", request.EntryDate.Date),
                    ("@plant", plant),
                    ("@sub", request.PlantSub.Trim()),
                    ("@product", request.Product.Trim()),
                    ("@quality", line.quality),
                    ("@grade", line.grade),
                    ("@shift", request.Shift),
                    ("@sector", request.Sector.Trim()),
                    ("@qty", line.qty),
                    ("@waste", first ? request.Wastage : 0m),
                    ("@group", group),
                    ("@tin", request.TimeIn),
                    ("@tout", request.TimeOut),
                    ("@company", request.Company),
                    ("@buyer", request.Buyer.Trim()),
                    ("@inv", request.MarketingInvoice.Trim()),
                    ("@bdate", (object?)request.BuyerOrderDate?.Date ?? DBNull.Value),
                    ("@hod", hod),
                    ("@excise", excise),
                    ("@edate", (object?)exciseDate ?? DBNull.Value),
                    ("@code", itemCode),
                    ("@ptype", request.ProductionType),
                    ("@trim", first && isLam ? trim : 0m),
                    ("@sweep", first ? sweeping : 0m),
                    ("@lumps", first && isTape ? trim : 0m),
                    ("@ftrim", first ? fabricTrim : 0m),
                    ("@fwaste", first ? fabricWaste : 0m),
                    ("@flumps", first ? lumps : 0m));

                var updated = await Exec(conn, tx, """
                    UPDATE warehouse SET StkInHand = StkInHand - @qty
                    WHERE companyname = @c AND warehousename = @w AND subgroupname = @q AND itemname = @g
                    """, ct,
                    ("@qty", line.qty), ("@c", request.Company), ("@w", request.FromWarehouse),
                    ("@q", line.quality), ("@g", line.grade));
                if (updated != 1)
                    throw new InvalidOperationException($"Could not reduce stock for {line.grade}.");
                first = false;
            }

            var net = consumption - waste;
            if (net > 0)
            {
                var fg = await ScalarString(conn, tx, """
                    SELECT TOP 1 itemcode FROM warehouse WITH (NOLOCK)
                    WHERE companyname = @c AND warehousename = @w AND itemname = @n
                    """, ct, ("@c", request.Company), ("@w", request.ToWarehouse), ("@n", request.Product.Trim()));
                if (string.IsNullOrWhiteSpace(fg))
                    throw new InvalidOperationException($"{request.Product} is not in {request.ToWarehouse}.");
                if (!skipFinishedStock)
                {
                    var updated = await Exec(conn, tx, """
                        UPDATE warehouse SET StkInHand = StkInHand + @qty
                        WHERE companyname = @c AND warehousename = @w AND itemcode = @code
                        """, ct, ("@qty", net), ("@c", request.Company), ("@w", request.ToWarehouse), ("@code", fg));
                    if (updated < 1)
                        throw new InvalidOperationException($"Could not add finished stock in {request.ToWarehouse}.");
                }

                await Exec(conn, tx, """
                    INSERT INTO Prod_Transaction (CompanyName, Qty, FromGodown, ToGodown, ItemName, Sysdate, ItemCode, ProdAmount, tid)
                    VALUES (@c, @qty, @from, @to, @item, @date, @code, 0, @tid)
                    """, ct,
                    ("@c", request.Company), ("@qty", net), ("@from", plant), ("@to", request.ToWarehouse),
                    ("@item", request.Product.Trim()), ("@date", request.EntryDate.Date), ("@code", fg), ("@tid", group));
            }

            await WriteStockJournalAsync(conn, tx, request, lines, net, waste, isLam, ct, group);

            await tx.CommitAsync(ct);
            return group;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task DeleteAsync(PlantConsumptionDeleteRequest request, CancellationToken ct)
    {
        if (request.GroupSrNo <= 0 || string.IsNullOrWhiteSpace(request.Company))
            throw new ArgumentException("Choose an entry to delete.");
        await using var conn = _db.CreateConnection();
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
        try
        {
            await ReverseAsync(conn, tx, request.Company, request.Plant, request.FromWarehouse, request.ToWarehouse, request.GroupSrNo, ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task ReverseAsync(
        SqlConnection conn, SqlTransaction tx, string company, string plant, string fromWh, string toWh, int group, CancellationToken ct)
    {
        const string linesSql = """
            SELECT Quality, Grade, ISNULL(Qty, 0),
                   ISNULL(Wastage, 0) + ISNULL(fTrimWastage, 0) + ISNULL(fSweepingWastage, 0)
                   + ISNULL(fTapeLumpsWastage, 0) + ISNULL(fFabricTrim, 0) + ISNULL(fFabricwaste, 0) + ISNULL(fNewLumpsWs, 0),
                   PlantItemName
            FROM TapePlantConsumption WITH (UPDLOCK, HOLDLOCK)
            WHERE companyname = @c AND GroupSrNo = @g
            """;
        var lines = new List<(string q, string g, decimal qty, decimal waste, string product)>();
        await using (var cmd = new SqlCommand(linesSql, conn, tx))
        {
            cmd.Parameters.AddWithValue("@c", company);
            cmd.Parameters.AddWithValue("@g", group);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                lines.Add((Str(reader, 0), Str(reader, 1), Dec(reader, 2), Dec(reader, 3), Str(reader, 4)));
        }
        if (lines.Count == 0)
            throw new InvalidOperationException("That entry was not found.");

        foreach (var line in lines)
        {
            await Exec(conn, tx, """
                UPDATE warehouse SET StkInHand = StkInHand + @qty
                WHERE companyname = @c AND warehousename = @w AND subgroupname = @q AND itemname = @g
                """, ct, ("@qty", line.qty), ("@c", company), ("@w", fromWh), ("@q", line.q), ("@g", line.g));
        }

        var skipFinishedStock = plant.Equals("Lamination", StringComparison.OrdinalIgnoreCase)
            || plant.Equals("Liner Roll Plant", StringComparison.OrdinalIgnoreCase);
        var net = lines.Sum(l => l.qty) - lines.Max(l => l.waste);
        if (!skipFinishedStock && net > 0 && !string.IsNullOrWhiteSpace(toWh))
        {
            await Exec(conn, tx, """
                UPDATE warehouse SET StkInHand = StkInHand - @qty
                WHERE companyname = @c AND warehousename = @w AND itemname = @n
                """, ct, ("@qty", net), ("@c", company), ("@w", toWh), ("@n", lines[0].product));
        }

        await Exec(conn, tx, "DELETE FROM Prod_Transaction WHERE CompanyName = @c AND tid = @g", ct, ("@c", company), ("@g", group));
        await Exec(conn, tx, "DELETE FROM stockJournalProd WHERE srno = @g AND company_name = @c", ct, ("@c", company), ("@g", group));
        await Exec(conn, tx, "DELETE FROM TapePlantConsumption WHERE companyname = @c AND GroupSrNo = @g", ct, ("@c", company), ("@g", group));
    }

    private static void Validate(PlantConsumptionSaveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Company)) throw new ArgumentException("Choose a company.");
        if (string.IsNullOrWhiteSpace(request.Plant)) throw new ArgumentException("Choose a plant.");
        if (string.IsNullOrWhiteSpace(request.Product)) throw new ArgumentException("Choose the finished product.");
        if (string.IsNullOrWhiteSpace(request.FromWarehouse)) throw new ArgumentException("Choose the From warehouse.");
        if (string.IsNullOrWhiteSpace(request.ToWarehouse)) throw new ArgumentException("Choose the To warehouse.");
        if (string.IsNullOrWhiteSpace(request.ProductionType)) throw new ArgumentException("Please enter Production type Value.");
        if (string.IsNullOrWhiteSpace(request.Buyer)) throw new ArgumentException("Choose Party Name.");
        if (string.IsNullOrWhiteSpace(request.BuyerOrder)) throw new ArgumentException("Choose Buyer Order Number.");
        if (string.IsNullOrWhiteSpace(request.MarketingInvoice)) throw new ArgumentException("Select Marketing Invoice No");
        if (request.EntryDate.Date > DateTime.Today) throw new ArgumentException("The entry date cannot be in the future.");
    }

    private static List<(string quality, string grade, decimal qty)> PrepareLines(List<PlantConsumptionLineRequest> source, bool isTape)
    {
        var rows = source
            .Where(l => !string.IsNullOrWhiteSpace(l.Quality) && !string.IsNullOrWhiteSpace(l.Grade))
            .Select(l => (quality: l.Quality.Trim(), grade: l.Grade.Trim(), qty: l.Qty, bags: l.PpBags, percent: l.Percent))
            .ToList();
        if (!isTape)
            return rows.Where(l => l.qty > 0).Select(l => (l.quality, l.grade, l.qty)).ToList();

        decimal totalBags = 0;
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].bags > 0 && PpQualities.Contains(rows[i].quality))
            {
                totalBags = rows[i].bags * 25m;
                rows[i] = (rows[i].quality, rows[i].grade, totalBags, rows[i].bags, rows[i].percent);
            }
        }
        if (totalBags > 0)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].bags == 0 && !PpQualities.Contains(rows[i].quality))
                {
                    var qty = Math.Round(totalBags * rows[i].percent / 100m, 0, MidpointRounding.AwayFromZero);
                    rows[i] = (rows[i].quality, rows[i].grade, qty, rows[i].bags, rows[i].percent);
                }
            }
        }
        return rows.Where(l => l.qty > 0).Select(l => (l.quality, l.grade, l.qty)).ToList();
    }

    private static async Task AssertProductionMatch(
        SqlConnection conn, SqlTransaction tx, PlantConsumptionSaveRequest request, decimal net, CancellationToken ct)
    {
        var plant = request.Plant.Trim();
        if (plant.Equals("Lamination", StringComparison.OrdinalIgnoreCase))
        {
            var booked = await RollProductionAsync(conn, tx, request.Company, plant, request.EntryDate, request.Shift, ct);
            if (Math.Round(net, 2) != Math.Round(booked, 2))
                throw new InvalidOperationException("Total Consumption Entry for Lamination is not match with Lamination Production Entry");
        }
        else if (plant.Equals("Liner Roll Plant", StringComparison.OrdinalIgnoreCase))
        {
            var booked = await RollProductionAsync(conn, tx, request.Company, plant, request.EntryDate, request.Shift, ct);
            var already = await ScalarDecimal(conn, tx, """
                SELECT ISNULL(SUM(Qty) - (
                    SUM(Wastage) + SUM(fTrimWastage) + SUM(fSweepingWastage) + SUM(fTapeOthersWastage)
                    + SUM(fFabricTrim) + SUM(fFabricwaste) + SUM(fNewLumpsWs)), 0)
                FROM TapePlantConsumption
                WHERE PlantName = @p AND CAST(sysDate AS date) = @d AND companyname = @c
                """, ct, ("@p", plant), ("@d", request.EntryDate.Date), ("@c", request.Company));
            if (Math.Round(net, 2) != Math.Round(booked - already, 2))
                throw new InvalidOperationException("Total Consumption Entry for Liner Roll Plant is not match with Production Entry");
        }
    }

    private static async Task<decimal> RollProductionAsync(
        SqlConnection conn, SqlTransaction? tx, string company, string plant, DateTime onDate, string shift, CancellationToken ct)
    {
        string? sql = null;
        if (plant.Equals("Lamination", StringComparison.OrdinalIgnoreCase))
        {
            sql = """
                SELECT ROUND(ISNULL(SUM(NetWt) - SUM(UNetWt2), 0), 2)
                FROM (
                    SELECT CASE WHEN ROW_NUMBER() OVER (PARTITION BY Urollno ORDER BY Urollno) = 1 THEN TOTALULNETWT ELSE 0 END AS UNetWt2,
                           NetWt
                    FROM (
                        SELECT SUM(UNetWt) OVER (PARTITION BY Urollno ORDER BY Urollno) AS TOTALULNETWT, Urollno, NetWt
                        FROM MISlaminationentry WITH (NOLOCK)
                        WHERE companyname = @c AND CAST(sysdate AS date) = @d AND Shift = @shift
                          AND NetWt > 0 AND ISNULL(IsCons, 0) = 1
                    ) AS S
                ) AS S
                """;
        }
        else if (plant.Equals("Liner Roll Plant", StringComparison.OrdinalIgnoreCase))
        {
            sql = """
                SELECT ISNULL(SUM(NetWt), 0)
                FROM MISOutsideRollEntry WITH (NOLOCK)
                WHERE companyname = @c AND CAST(sysdate AS date) = @d AND storeinwardno = 'Production'
                """;
        }
        if (sql == null) return 0m;

        await using var cmd = tx == null ? new SqlCommand(sql, conn) : new SqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@c", company);
        cmd.Parameters.AddWithValue("@d", onDate.Date);
        cmd.Parameters.AddWithValue("@shift", shift ?? "");
        var value = await cmd.ExecuteScalarAsync(ct);
        return value == null || value is DBNull ? 0m : Convert.ToDecimal(value);
    }

    private static async Task WriteStockJournalAsync(
        SqlConnection conn, SqlTransaction tx, PlantConsumptionSaveRequest request,
        List<(string quality, string grade, decimal qty)> lines, decimal net, decimal waste, bool isLam, CancellationToken ct, int group)
    {
        var journalNo = await ScalarInt(conn, tx, "SELECT ISNULL(MAX(srno), 0) + 1 FROM stockJournalProd WITH (UPDLOCK, HOLDLOCK)", ct);
        var outside = request.ProductionType.Equals("Outside Job", StringComparison.OrdinalIgnoreCase);
        var prodType = outside ? "Outside Production" : "Production";
        var consType = outside ? "Outside consumption" : "consumption";
        var year = ErpYear(request.EntryDate);
        var product = request.Product.Trim();
        var recordLogId = await SaveRecordLogAsync(conn, tx, request, year, ct);

        if (!isLam)
        {
            var fg = await ScalarString(conn, tx, """
                SELECT TOP 1 itemcode FROM warehouse WITH (NOLOCK)
                WHERE companyname = @c AND itemname = @n
                """, ct, ("@c", request.Company), ("@n", product));
            await InsertJournalAsync(conn, tx, journalNo, request, prodType, product, net, fg, year, group, recordLogId, ct);
            await InsertJournalAsync(conn, tx, journalNo, request, prodType, "Tape Wastage", waste, "WIP00031", year, group, recordLogId, ct);
        }
        else
        {
            await InsertJournalAsync(conn, tx, journalNo, request, prodType, "Fabric Wastage", waste, "WIP00031", year, group, recordLogId, ct);
        }

        foreach (var line in lines)
        {
            var code = await ScalarString(conn, tx, """
                SELECT TOP 1 itemcode FROM warehouse WITH (NOLOCK)
                WHERE companyname = @c AND itemname = @n
                """, ct, ("@c", request.Company), ("@n", line.grade));
            await InsertJournalAsync(conn, tx, journalNo, request, consType, line.grade, line.qty, code, year, group, recordLogId, ct);
        }
    }

    private static async Task<int> SaveRecordLogAsync(
        SqlConnection conn, SqlTransaction tx, PlantConsumptionSaveRequest request, string year, CancellationToken ct)
    {
        await using var cmd = new SqlCommand("dbo.sp_SaveRecordLog", conn, tx) { CommandType = System.Data.CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@RecordLogId", DBNull.Value);
        cmd.Parameters.AddWithValue("@UserName", string.IsNullOrWhiteSpace(request.UserName) ? "" : request.UserName.Trim());
        cmd.Parameters.AddWithValue("@Flag", "RG");
        cmd.Parameters.AddWithValue("@Remarks", $"Create: Stock Journal  Saved in {request.Company} for {year}");
        var id = await cmd.ExecuteScalarAsync(ct);
        return id == null || id is DBNull ? 0 : Convert.ToInt32(id);
    }

    private static async Task AssertDateOpenAsync(
        SqlConnection conn, SqlTransaction tx, PlantConsumptionSaveRequest request, CancellationToken ct)
    {
        var frozen = await ScalarInt(conn, tx, """
            SELECT COUNT(*) FROM freezeProdEntry WITH (NOLOCK)
            WHERE CompanyName = @c
              AND @d <= (
                SELECT DATEADD(day, -2, MAX(sysdate))
                FROM freezeProdEntry WITH (NOLOCK)
                WHERE companyname = @c)
            """, ct, ("@c", request.Company), ("@d", request.EntryDate.Date));
        if (frozen > 0)
            throw new InvalidOperationException("Entry on this Date is Freezed. So, You Can't do Entry.");
    }

    private static Task<int> InsertJournalAsync(
        SqlConnection conn, SqlTransaction tx, int journalNo, PlantConsumptionSaveRequest request,
        string type, string itemName, decimal qty, string itemCode, string year, int group, int recordLogId, CancellationToken ct)
    {
        return Exec(conn, tx, """
            EXEC dbo.Insert_StockJournal
                @stock_journal_no, @sysdate, @companyname, @type, @itemname, @qty, @rate, @amount, @unit,
                @yr, @recordlogid, @srno, @itemcode, @rollno, @plantname
            """, ct,
            ("@stock_journal_no", journalNo),
            ("@sysdate", request.EntryDate.Date),
            ("@companyname", request.Company),
            ("@type", type),
            ("@itemname", itemName),
            ("@qty", qty),
            ("@rate", 0m),
            ("@amount", 0m),
            ("@unit", "KGS"),
            ("@yr", year),
            ("@recordlogid", recordLogId),
            ("@srno", group),
            ("@itemcode", itemCode ?? ""),
            ("@rollno", ".."),
            ("@plantname", request.ToWarehouse.Trim()));
    }

    private static string ErpYear(DateTime date)
    {
        var start = date.Month >= 4 ? date.Year : date.Year - 1;
        return $"{start % 100:D2}-{(start + 1) % 100:D2}";
    }

    private static async Task BlockIfYesterdayPending(SqlConnection conn, SqlTransaction tx, PlantConsumptionSaveRequest request, CancellationToken ct)
    {
        var flag = await ScalarInt(conn, tx, """
            SELECT ISNULL(FlagValue, 0) FROM ProfileFlag
            WHERE Flagname = 'IsApproval' AND Companyname = @c
            """, ct, ("@c", request.Company));
        if (flag != 1) return;
        var yesterday = request.EntryDate.Date.AddDays(-1);
        var hod = await ScalarInt(conn, tx, """
            SELECT COUNT(*) FROM TapePlantConsumption
            WHERE hodapprove = 'Pending' AND PlantName = @p AND CAST(sysDate AS date) = @d AND companyname = @c
            """, ct, ("@p", request.Plant.Trim()), ("@d", yesterday), ("@c", request.Company));
        if (hod > 0)
            throw new InvalidOperationException("Plant HOD approval is pending for the previous date.");
        var excise = await ScalarInt(conn, tx, """
            SELECT COUNT(*) FROM TapePlantConsumption
            WHERE exciseapprove = 'Pending' AND PlantName = @p AND CAST(sysDate AS date) = @d AND companyname = @c
            """, ct, ("@p", request.Plant.Trim()), ("@d", yesterday), ("@c", request.Company));
        if (excise > 0)
            throw new InvalidOperationException("Excise approval is pending for the previous date.");
    }

    private static async Task<bool> IsApprover(SqlConnection conn, SqlTransaction tx, PlantConsumptionSaveRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.UserName)) return false;
        var n = await ScalarInt(conn, tx, """
            SELECT COUNT(*) FROM plantmaster
            WHERE companyname = @c AND plantname = @p
              AND (@u IN (approvalauth1, approvalauth2, approvalauth3, approvalauth4, approvalauth5))
            """, ct, ("@c", request.Company), ("@p", request.Plant.Trim()), ("@u", request.UserName.Trim()));
        return n > 0;
    }

    private static async Task<List<string>> Strings(SqlConnection conn, string sql, string company, CancellationToken ct, params (string, object)[] extra)
    {
        var rows = new List<string>();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@c", company);
        foreach (var (name, value) in extra)
            cmd.Parameters.AddWithValue(name, value);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            if (!reader.IsDBNull(0)) rows.Add(reader.GetString(0));
        return rows;
    }

    private static async Task<List<object>> Buyers(SqlConnection conn, string company, CancellationToken ct)
    {
        const string sql = """
            SELECT buyername, buyerorderno, ItemNO
            FROM Despatch.dbo.vw_ProductionCompAndMarkInvNo WITH (NOLOCK)
            WHERE ProductionCompanyName = @c
              AND buyername IS NOT NULL AND buyername <> ''
            """;
        var map = new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        cmd.Parameters.AddWithValue("@c", company);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var buyer = reader.GetString(0).Trim();
            var order = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
            var invoice = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();
            if (!map.TryGetValue(buyer, out var orders))
                map[buyer] = orders = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            if (!orders.TryGetValue(order, out var invoices))
                orders[order] = invoices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (invoice.Length > 0) invoices.Add(invoice);
        }
        return map.OrderBy(p => p.Key).Select(p => (object)new
        {
            name = p.Key,
            orders = p.Value.OrderBy(o => o.Key).Select(o => new
            {
                orderNo = o.Key,
                invoices = o.Value.OrderBy(i => i).ToList(),
            }).ToList(),
        }).ToList();
    }

    private static async Task<int> Exec(SqlConnection conn, SqlTransaction tx, string sql, CancellationToken ct, params (string, object)[] args)
    {
        await using var cmd = new SqlCommand(sql, conn, tx);
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<decimal> ScalarDecimal(SqlConnection conn, SqlTransaction tx, string sql, CancellationToken ct, params (string, object)[] args)
    {
        var value = await Scalar(conn, tx, sql, ct, args);
        return value == null || value is DBNull ? 0m : Convert.ToDecimal(value);
    }

    private static async Task<int> ScalarInt(SqlConnection conn, SqlTransaction tx, string sql, CancellationToken ct, params (string, object)[] args)
    {
        var value = await Scalar(conn, tx, sql, ct, args);
        return value == null || value is DBNull ? 0 : Convert.ToInt32(value);
    }

    private static async Task<string> ScalarString(SqlConnection conn, SqlTransaction tx, string sql, CancellationToken ct, params (string, object)[] args)
    {
        var value = await Scalar(conn, tx, sql, ct, args);
        return value == null || value is DBNull ? "" : Convert.ToString(value) ?? "";
    }

    private static async Task<object?> Scalar(SqlConnection conn, SqlTransaction tx, string sql, CancellationToken ct, params (string, object)[] args)
    {
        await using var cmd = new SqlCommand(sql, conn, tx);
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return await cmd.ExecuteScalarAsync(ct);
    }

    private static string Str(SqlDataReader reader, int i) => reader.IsDBNull(i) ? "" : Convert.ToString(reader.GetValue(i)) ?? "";
    private static decimal Dec(SqlDataReader reader, int i) => reader.IsDBNull(i) ? 0m : Convert.ToDecimal(reader.GetValue(i));
}
