using Dapper;
using POApprovalAPI.Planning.Bom;
using POApprovalAPI.Planning.Execution.Models;
using POApprovalAPI.Planning.Setup;
using POApprovalAPI.Services;

namespace POApprovalAPI.Planning.Execution;

public sealed class ExecutionPlanningService
{
    private const int CommandTimeoutSeconds = 120;
    private const double QtyEpsilon = 0.5;

    private readonly DatabaseService _database;
    private readonly IPlanningSetupRepository _setup;

    public ExecutionPlanningService(DatabaseService database, IPlanningSetupRepository setup)
    {
        _database = database;
        _setup = setup;
    }

    public async Task<OrderExecutionSummaryDto> GetOrderExecutionAsync(
        string orderNo,
        string? companyName,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var order = orderNo.Trim();
        using var connection = _database.CreateConnection();

        var planned = (await connection.QueryAsync<PlanRow>(@"
SELECT
    CAST(linenos AS INT) AS [LineNo],
    LTRIM(RTRIM(CAST([shift] AS NVARCHAR(10)))) AS [Shift],
    CAST(sysdate AS DATE) AS PlanDate,
    SUM(CAST(qty AS FLOAT)) AS Qty
FROM prod_fibcallocationMaster WITH (NOLOCK)
WHERE orderno = @OrderNo
GROUP BY linenos, [shift], CAST(sysdate AS DATE)", new { OrderNo = order }, commandTimeout: CommandTimeoutSeconds)).ToList();

        var company = companyName?.Trim();
        if (string.IsNullOrEmpty(company) && planned.Count > 0)
        {
            company = await connection.ExecuteScalarAsync<string>(@"
SELECT TOP 1 Companyname FROM prod_fibcallocationMaster WITH (NOLOCK) WHERE orderno = @OrderNo",
                new { OrderNo = order }, commandTimeout: CommandTimeoutSeconds) ?? "";
        }
        company ??= "";

        var produced = company.Length > 0
            ? (await connection.QueryAsync<ProdRow>(@"
SELECT
    LTRIM(RTRIM(CAST(TeamNo AS NVARCHAR(50)))) AS TeamNo,
    LTRIM(RTRIM(CAST([Shift] AS NVARCHAR(10)))) AS [Shift],
    CAST(Sysdate AS DATE) AS ProdDate,
    SUM(CAST(BagPCS AS FLOAT)) AS BagPcs
FROM FIBCTeamWiseProduction WITH (NOLOCK)
WHERE PONO = @OrderNo AND CompanyName LIKE @CompanyLike
GROUP BY LTRIM(RTRIM(CAST(TeamNo AS NVARCHAR(50)))), [Shift], CAST(Sysdate AS DATE)",
                new { OrderNo = order, CompanyLike = $"%{company}%" },
                commandTimeout: CommandTimeoutSeconds)).ToList()
            : [];

        var bailedTotal = await GetBailedQuantityAsync(connection, order);

        var productionEntries = produced
            .Select(p => new OrderProductionEntryDto
            {
                LineNo = ExecutionProductionHelper.ParseLineFromTeam(p.TeamNo),
                TeamNo = p.TeamNo?.Trim() ?? "",
                Shift = ExecutionProductionHelper.NormalizeShift(p.Shift),
                ProdDate = p.ProdDate.Date,
                Quantity = p.BagPcs,
            })
            .Where(p => p.Quantity > QtyEpsilon)
            .OrderBy(p => p.ProdDate)
            .ThenBy(p => p.LineNo)
            .ThenBy(p => p.Shift)
            .ToList();

        var producedByLineShiftDate = productionEntries
            .GroupBy(p => ExecutionProductionHelper.LineShiftDateKey(p.LineNo, p.Shift, p.ProdDate))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity), StringComparer.OrdinalIgnoreCase);

        var producedByLineShift = productionEntries
            .Where(p => p.LineNo > 0)
            .GroupBy(p => ExecutionProductionHelper.LineShiftKey(p.LineNo, p.Shift))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity), StringComparer.OrdinalIgnoreCase);

        var plannedTotal = planned.Sum(p => p.Qty);
        var producedTotal = productionEntries.Sum(p => p.Quantity);

        var backlogRowsCleared = 0;
        if (!string.IsNullOrEmpty(company) && plannedTotal > 0 && producedTotal >= plannedTotal - QtyEpsilon)
            backlogRowsCleared = await _setup.ClearOpenBacklogForOrderAsync(company, order, ct);

        var backlog = string.IsNullOrEmpty(company)
            ? []
            : await _setup.GetBacklogAsync(company, "Open", ct);

        var lines = planned.Select(p =>
        {
            var shift = ExecutionProductionHelper.NormalizeShift(p.Shift);
            var lineBacklog = backlog
                .Where(b => b.LineNo == p.LineNo && ExecutionProductionHelper.NormalizeShift(b.Shift).Equals(shift, StringComparison.OrdinalIgnoreCase))
                .Sum(b => b.BacklogQty);

            var slotKey = ExecutionProductionHelper.LineShiftDateKey(p.LineNo, shift, p.PlanDate);
            producedByLineShiftDate.TryGetValue(slotKey, out var prodOnSlot);

            return new OrderExecutionLineDto
            {
                LineNo = p.LineNo,
                Shift = shift,
                PlanDate = p.PlanDate,
                PlannedQty = p.Qty,
                ProducedQty = prodOnSlot,
                BailedQty = 0,
                OpenBacklogQty = lineBacklog,
            };
        }).ToList();

        var plannedByLineShift = planned
            .GroupBy(p => ExecutionProductionHelper.LineShiftKey(p.LineNo, ExecutionProductionHelper.NormalizeShift(p.Shift)))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty), StringComparer.OrdinalIgnoreCase);

        var allLineShiftKeys = plannedByLineShift.Keys
            .Union(producedByLineShift.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var lineShiftTotals = allLineShiftKeys.Select(key =>
        {
            plannedByLineShift.TryGetValue(key, out var plannedQty);
            producedByLineShift.TryGetValue(key, out var producedQty);
            return new OrderExecutionLineShiftSummaryDto
            {
                LineNo = int.Parse(key.Split('|')[0]),
                Shift = key.Split('|')[1],
                PlannedQty = plannedQty,
                ProducedQty = producedQty,
                PendingQty = Math.Max(0, plannedQty - producedQty),
            };
        }).ToList();

        var slotMatchedProduction = lines.Sum(l => l.ProducedQty);
        var suggestions = BuildSuggestions(
            order,
            plannedTotal,
            producedTotal,
            bailedTotal,
            slotMatchedProduction,
            productionEntries,
            planned,
            backlog);

        if (backlogRowsCleared > 0)
            suggestions.Add($"Auto-cleared {backlogRowsCleared} backlog row(s) — production meets or exceeds planned quantity.");

        return new OrderExecutionSummaryDto
        {
            OrderNo = order,
            CompanyName = company,
            PlannedQty = plannedTotal,
            ProducedQty = producedTotal,
            BailedQty = bailedTotal,
            PendingQty = Math.Max(0, plannedTotal - producedTotal),
            BailingGap = Math.Max(0, producedTotal - bailedTotal),
            Lines = lines,
            LineShiftTotals = lineShiftTotals,
            ProductionEntries = productionEntries,
            ReplanSuggestions = suggestions,
            BacklogRowsAutoCleared = backlogRowsCleared,
        };
    }

    public async Task<BailingReconciliationDto> GetBailingReconciliationAsync(
        string orderNo,
        string? companyName,
        CancellationToken ct = default)
    {
        var summary = await GetOrderExecutionAsync(orderNo, companyName, ct);
        var shortfall = Math.Max(0, summary.PlannedQty - summary.BailedQty);

        var ready = summary.PlannedQty > QtyEpsilon
            && summary.BailedQty >= summary.PlannedQty * 0.98
            && summary.BailedQty <= summary.ProducedQty + QtyEpsilon;

        string message;
        if (summary.PlannedQty <= QtyEpsilon)
            message = "No plan rows found for this order.";
        else if (ready)
            message = "Bailed quantity meets planned quantity — dispatch-ready subject to QC.";
        else if (summary.ProducedQty > QtyEpsilon && summary.BailedQty <= QtyEpsilon)
            message = $"{summary.ProducedQty:N0} pcs produced but none bailed yet — {shortfall:N0} pcs still to bail vs plan.";
        else if (shortfall > QtyEpsilon)
            message = $"{shortfall:N0} pcs still to bail vs plan ({summary.BailedQty:N0} of {summary.PlannedQty:N0} bailed).";
        else
            message = "Bailing in progress — review line production before dispatch.";

        return new BailingReconciliationDto
        {
            OrderNo = summary.OrderNo,
            CompanyName = summary.CompanyName,
            PlannedQty = summary.PlannedQty,
            BailedQty = summary.BailedQty,
            Shortfall = shortfall,
            ReadyForDispatch = ready,
            Message = message,
        };
    }

    public async Task<AccessoryMaterialBoardDto> GetAccessoryMaterialsAsync(
        string orderNo,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var order = orderNo.Trim();
        var board = new AccessoryMaterialBoardDto { OrderNo = order };
        if (string.IsNullOrEmpty(order))
            return board;

        var bomLines = await _setup.GetBomComponentLinesAsync(order, ct);
        var accessories = bomLines
            .Where(l => l.PlanningKind == BomComponentClassifier.KindAccessory)
            .ToList();
        board.DispatchDate = await _setup.GetMarketingDispatchDateAsync(order, ct);

        if (accessories.Count == 0)
        {
            board.Warnings = ["No accessory BOM headings found for this order."];
            return board;
        }

        List<IndentLineRow> indents;
        List<MrnLineRow> mrns;
        try
        {
            using var connection = _database.CreateConnection();
            var like = $"%{order}%";
            indents = (await connection.QueryAsync<IndentLineRow>(@"
SELECT TOP 200
    Expr1 AS IndentNo,
    itemcode AS ItemCode,
    itemdesc AS ItemDesc,
    Qty,
    Unit,
    Purpose,
    CompanyName
FROM Vw_StoreDeptt WITH (NOLOCK)
WHERE Purpose LIKE @Like OR itemdesc LIKE @Like",
                new { Like = like }, commandTimeout: CommandTimeoutSeconds)).ToList();

            var indentNos = indents
                .Select(i => i.IndentNo)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            mrns = (await connection.QueryAsync<MrnLineRow>(@"
SELECT TOP 200
    MRNo,
    PONo,
    IndentNo,
    ItemCode,
    ItemName,
    RecdQty,
    AcceptedQty,
    OrderQty,
    PendingQty,
    CompanyName,
    PartyName,
    SysDate
FROM Vw_StoreInwards WITH (NOLOCK)
WHERE PONo = @OrderNo
   OR (@HasIndents = 1 AND IndentNo IN @IndentNos)",
                new
                {
                    OrderNo = order,
                    HasIndents = indentNos.Count > 0,
                    IndentNos = indentNos.Count > 0 ? indentNos : new List<string> { "" },
                },
                commandTimeout: CommandTimeoutSeconds)).ToList();
        }
        catch (Exception ex)
        {
            board.Warnings = [$"Could not read indent/MRN views: {ex.Message}"];
            board.Items = accessories.Select(a => MapAccessory(a, null, [])).ToList();
            return board;
        }

        if (indents.Count == 0 && mrns.Count == 0)
            board.Warnings = ["No indent Purpose/itemdesc or MRN PONo matched this order number."];

        board.Items = accessories.Select(acc =>
        {
            var indent = FindBestIndent(acc, indents);
            var relatedMrns = mrns.Where(m => MatchesAccessory(acc, m.ItemName, m.ItemCode)
                || (indent is not null
                    && ((!string.IsNullOrWhiteSpace(indent.ItemCode)
                            && indent.ItemCode.Equals(m.ItemCode, StringComparison.OrdinalIgnoreCase))
                        || indent.IndentNo.Equals(m.IndentNo, StringComparison.OrdinalIgnoreCase))))
                .ToList();
            return MapAccessory(acc, indent, relatedMrns);
        }).ToList();

        return board;
    }

    public async Task<FactoryExecutionBoardDto> GetFactoryBoardAsync(
        string companyName,
        DateTime? date,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var company = companyName.Trim();
        var boardDate = (date ?? DateTime.Today).Date;

        using var connection = _database.CreateConnection();
        var planned = (await connection.QueryAsync<PlanRow>(@"
SELECT CAST(linenos AS INT) AS [LineNo], LTRIM(RTRIM(CAST([shift] AS NVARCHAR(10)))) AS [Shift],
       CAST(sysdate AS DATE) AS PlanDate, SUM(CAST(qty AS FLOAT)) AS Qty
FROM prod_fibcallocationMaster WITH (NOLOCK)
WHERE Companyname = @Company AND CAST(sysdate AS DATE) = @BoardDate
GROUP BY linenos, [shift], CAST(sysdate AS DATE)", new { Company = company, BoardDate = boardDate },
            commandTimeout: CommandTimeoutSeconds)).ToList();

        var producedRaw = (await connection.QueryAsync<ProdRow>(@"
SELECT LTRIM(RTRIM(CAST(TeamNo AS NVARCHAR(50)))) AS TeamNo,
       LTRIM(RTRIM(CAST([Shift] AS NVARCHAR(10)))) AS [Shift],
       CAST(Sysdate AS DATE) AS ProdDate,
       SUM(CAST(BagPCS AS FLOAT)) AS BagPcs
FROM FIBCTeamWiseProduction WITH (NOLOCK)
WHERE CompanyName LIKE @CompanyLike AND CAST(Sysdate AS DATE) = @BoardDate
GROUP BY LTRIM(RTRIM(CAST(TeamNo AS NVARCHAR(50)))), [Shift], CAST(Sysdate AS DATE)",
            new { CompanyLike = $"%{company}%", BoardDate = boardDate },
            commandTimeout: CommandTimeoutSeconds)).ToList();

        var produced = producedRaw
            .Select(p => new
            {
                LineNo = ExecutionProductionHelper.ParseLineFromTeam(p.TeamNo),
                Shift = ExecutionProductionHelper.NormalizeShift(p.Shift),
                p.BagPcs,
            })
            .Where(p => p.LineNo > 0)
            .GroupBy(p => ExecutionProductionHelper.LineShiftKey(p.LineNo, p.Shift))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.BagPcs), StringComparer.OrdinalIgnoreCase);

        var backlog = await _setup.GetBacklogAsync(company, "Open", ct);

        var rows = planned
            .GroupBy(p => new { p.LineNo, Shift = ExecutionProductionHelper.NormalizeShift(p.Shift) })
            .Select(g =>
            {
                var key = ExecutionProductionHelper.LineShiftKey(g.Key.LineNo, g.Key.Shift);
                produced.TryGetValue(key, out var producedQty);
                var openBacklog = backlog
                    .Where(b => b.LineNo == g.Key.LineNo
                        && ExecutionProductionHelper.NormalizeShift(b.Shift).Equals(g.Key.Shift, StringComparison.OrdinalIgnoreCase))
                    .Sum(b => b.BacklogQty);
                var plannedQty = g.Sum(x => x.Qty);
                return new FactoryExecutionRowDto
                {
                    LineNo = g.Key.LineNo,
                    Shift = g.Key.Shift,
                    PlannedQty = plannedQty,
                    ProducedQty = producedQty,
                    OpenBacklogQty = openBacklog,
                    CapacityGap = Math.Max(0, plannedQty - producedQty),
                };
            })
            .OrderBy(r => r.LineNo)
            .ThenBy(r => r.Shift)
            .ToList();

        return new FactoryExecutionBoardDto
        {
            CompanyName = company,
            BoardDate = boardDate,
            Rows = rows,
        };
    }

    private static async Task<double> GetBailedQuantityAsync(System.Data.IDbConnection connection, string orderNo)
    {
        var exact = await connection.ExecuteScalarAsync<double?>(@"
SELECT SUM(CAST(BailPcs AS FLOAT))
FROM FIBCBailingEntry WITH (NOLOCK)
WHERE LTRIM(RTRIM(MarketingOrdNo)) = @OrderNo",
            new { OrderNo = orderNo },
            commandTimeout: CommandTimeoutSeconds);

        return exact ?? 0;
    }

    private static List<string> BuildSuggestions(
        string order,
        double plannedTotal,
        double producedTotal,
        double bailedTotal,
        double slotMatchedProduction,
        IReadOnlyList<OrderProductionEntryDto> productionEntries,
        IReadOnlyList<PlanRow> planned,
        IReadOnlyList<Planning.Setup.Models.PlanningBacklogDto> backlog)
    {
        var suggestions = new List<string>();

        if (plannedTotal > QtyEpsilon && producedTotal < plannedTotal * 0.9)
        {
            suggestions.Add(
                $"Production at {producedTotal:N0} pcs is below 90% of planned {plannedTotal:N0} pcs — consider shift-wise replan or backlog entry.");
        }

        if (producedTotal > QtyEpsilon && bailedTotal < producedTotal * 0.95)
        {
            suggestions.Add(
                $"Bailing gap: {producedTotal - bailedTotal:N0} pcs produced but not yet bailed — reconcile with bailing desk.");
        }

        if (plannedTotal > QtyEpsilon && producedTotal > QtyEpsilon
            && slotMatchedProduction < producedTotal * 0.5)
        {
            var prodDates = productionEntries.Select(p => p.ProdDate).Distinct().OrderBy(d => d).ToList();
            var planDates = planned.Select(p => p.PlanDate.Date).Distinct().OrderBy(d => d).ToList();
            if (prodDates.Count > 0 && planDates.Count > 0
                && !prodDates.Any(d => planDates.Contains(d)))
            {
                suggestions.Add(
                    $"Production was recorded {prodDates.First():yyyy-MM-dd}–{prodDates.Last():yyyy-MM-dd} but plan slots are {planDates.First():yyyy-MM-dd}–{planDates.Last():yyyy-MM-dd} — dates do not overlap.");
            }
            else
            {
                suggestions.Add(
                    "Production exists for this order but not on the same line/shift/date as plan slots — see ERP production log below.");
            }
        }

        if (backlog.Any(b => b.OrderNo.Equals(order, StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.Add(
                "Open line+shift backlog exists for this order — next allotment will reserve capacity on those slots first.");
        }

        return suggestions;
    }

    private static IndentLineRow? FindBestIndent(
        Planning.Setup.Models.PlanningBomComponentLineDto acc,
        IReadOnlyList<IndentLineRow> indents)
    {
        return indents
            .Select(row => (row, score: MatchScore(acc, row.ItemDesc, row.ItemCode)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Select(x => x.row)
            .FirstOrDefault();
    }

    private static bool MatchesAccessory(
        Planning.Setup.Models.PlanningBomComponentLineDto acc,
        string? itemName,
        string? itemCode) =>
        MatchScore(acc, itemName, itemCode) > 0;

    private static int MatchScore(
        Planning.Setup.Models.PlanningBomComponentLineDto acc,
        string? itemName,
        string? itemCode)
    {
        var text = $"{itemName} {itemCode}".ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(text.Trim()))
            return 0;

        var heading = BomComponentClassifier.NormalizeHeading(acc.Heading).ToUpperInvariant();
        var score = 0;
        if (heading.Length >= 3 && text.Contains(heading, StringComparison.OrdinalIgnoreCase))
            score += 5;
        foreach (var keyword in BomComponentClassifier.MatchKeywords(acc.Category))
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                score += 2;
        }

        return score;
    }

    private static AccessoryMaterialStatusDto MapAccessory(
        Planning.Setup.Models.PlanningBomComponentLineDto acc,
        IndentLineRow? indent,
        IReadOnlyList<MrnLineRow> mrns)
    {
        var received = mrns.Sum(m => m.AcceptedQty > 0 ? m.AcceptedQty : m.RecdQty);
        var pending = mrns.Sum(m => m.PendingQty);
        var required = acc.TotalKg ?? acc.TotalMtr;
        var unit = acc.TotalKg is > 0 ? "kg" : acc.TotalMtr is > 0 ? "m" : "";
        string status;
        if (received > 0.01 && required is > 0 && received >= required.Value * 0.95)
            status = "Received";
        else if (received > 0.01)
            status = "Partial";
        else if (indent is not null)
            status = "Indented";
        else
            status = "NotFound";

        var mrnNo = mrns.Select(m => m.MRNo).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
        var detail = status switch
        {
            "Received" => $"MRN {mrnNo} received {received:N1} {unit}".Trim(),
            "Partial" => $"Received {received:N1} of {required:N1} {unit}".Trim(),
            "Indented" => $"Indent {indent!.IndentNo} — awaiting MRN",
            _ => "No indent/MRN matched this heading yet",
        };

        return new AccessoryMaterialStatusDto
        {
            Heading = acc.Heading,
            Category = acc.Category,
            RequiredQty = required,
            Unit = unit,
            Status = status,
            IndentNo = indent?.IndentNo,
            ItemCode = indent?.ItemCode ?? mrns.FirstOrDefault()?.ItemCode,
            ItemDesc = indent?.ItemDesc ?? mrns.FirstOrDefault()?.ItemName,
            IndentQty = indent?.Qty,
            MrnNo = mrnNo,
            ReceivedQty = received,
            PendingQty = pending,
            CompanyName = indent?.CompanyName ?? mrns.FirstOrDefault()?.CompanyName,
            Detail = detail,
        };
    }

    private sealed class IndentLineRow
    {
        public string IndentNo { get; set; } = "";
        public string? ItemCode { get; set; }
        public string? ItemDesc { get; set; }
        public double Qty { get; set; }
        public string? Unit { get; set; }
        public string? Purpose { get; set; }
        public string? CompanyName { get; set; }
    }

    private sealed class MrnLineRow
    {
        public string? MRNo { get; set; }
        public string? PONo { get; set; }
        public string? IndentNo { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public double RecdQty { get; set; }
        public double AcceptedQty { get; set; }
        public double OrderQty { get; set; }
        public double PendingQty { get; set; }
        public string? CompanyName { get; set; }
        public string? PartyName { get; set; }
        public DateTime? SysDate { get; set; }
    }

    private sealed class PlanRow
    {
        public int LineNo { get; set; }
        public string Shift { get; set; } = "";
        public DateTime PlanDate { get; set; }
        public double Qty { get; set; }
    }

    private sealed class ProdRow
    {
        public string? TeamNo { get; set; }
        public string Shift { get; set; } = "";
        public DateTime ProdDate { get; set; }
        public double BagPcs { get; set; }
    }
}
