using Microsoft.Extensions.Options;
using POApprovalAPI.Planning.Bom;
using POApprovalAPI.Planning.Loom.Models;

namespace POApprovalAPI.Planning.Loom;

public sealed class LoomPlanningService
{
    private readonly ILoomPlanningRepository _repository;
    private readonly ILoomPlanningEngine _engine;
    private readonly LoomPlanningOptions _options;

    public LoomPlanningService(
        ILoomPlanningRepository repository,
        ILoomPlanningEngine engine,
        IOptions<LoomPlanningOptions> options)
    {
        _repository = repository;
        _engine = engine;
        _options = options.Value;
    }

    public LoomPlanningConfigDto GetConfig() => new()
    {
        DefaultCompanyName = _options.DefaultCompanyName,
        ReadOnly = !_options.AllowConfirmSave,
        PreviewOnly = !_options.AllowConfirmSave,
        ConfirmSaveEnabled = _options.AllowConfirmSave,
        ReplaceExistingEnabled = _options.AllowReplaceExistingPlan,
        FabricBufferDays = _options.FabricBufferDays,
        MaxPlanningHorizonDays = _options.MaxPlanningHorizonDays,
        MaxDaysPerLoomSegment = _options.MaxDaysPerLoomSegment,
        MaxChangeoversPerDay = _options.MaxChangeoversPerDay,
        DefaultEfficiency = _options.DefaultEfficiency,
    };

    public Task<IReadOnlyList<LoomMasterDto>> GetLoomsAsync(string? companyName, CancellationToken ct = default) =>
        _repository.GetLoomMasterAsync(companyName, ct);

    public Task<LoomAllocationGridResult> GetAllocationGridAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        string? companyName,
        CancellationToken ct = default)
    {
        var to = dateTo?.Date ?? DateTime.Today;
        var from = dateFrom?.Date ?? to.AddDays(-30);
        return _repository.GetAllocationGridAsync(from, to, companyName, ct);
    }

    public Task<LoomProductionMeterGridResult> GetProductionMetersAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        string? companyName,
        CancellationToken ct = default)
    {
        var to = dateTo?.Date ?? DateTime.Today;
        var from = dateFrom?.Date ?? to.AddDays(-30);
        return _repository.GetProductionMetersAsync(from, to, companyName, ct);
    }

    public Task<IReadOnlyList<LoomPpmSpecDto>> GetPpmSpecsAsync(CancellationToken ct = default) =>
        _repository.GetPpmSpecsAsync(ct);

    public async Task<LoomOrderPlanDetailDto?> GetOrderPlanAsync(string orderNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orderNo))
            return null;

        var trimmed = orderNo.Trim();
        var allocationsTask = _repository.GetOrderAllocationsAsync(trimmed, ct);
        var fabricTask = _repository.GetFabricRequirementsAsync(trimmed, ct);
        await Task.WhenAll(allocationsTask, fabricTask);

        var allocations = await allocationsTask;
        var fabric = await fabricTask;
        if (allocations.Count == 0 && fabric.Count == 0)
            return null;

        return new LoomOrderPlanDetailDto
        {
            OrderNo = trimmed,
            Allocations = allocations,
            FabricRequirements = fabric,
        };
    }

    public Task<LoomOrderContextDto?> GetOrderContextAsync(string orderNo, CancellationToken ct = default) =>
        _repository.GetOrderContextAsync(orderNo, ct);

    public Task<LoomOrderAllotmentContextDto?> GetOrderAllotmentContextAsync(string orderNo, CancellationToken ct = default) =>
        _repository.GetOrderAllotmentContextAsync(orderNo, ct);

    public Task<string?> ResolveWeavingCompanyForOrderAsync(string orderNo, CancellationToken ct = default) =>
        _repository.ResolveWeavingCompanyFromAllocationsAsync(orderNo, ct);

    public Task<LoomAllotmentResult> PreviewAllotmentAsync(LoomAllotmentRequest request, CancellationToken ct = default) =>
        _engine.AllotAsync(request, ct);

    public Task<LoomAllotmentConfirmResult> ConfirmAllotmentAsync(LoomAllotmentRequest request, CancellationToken ct = default) =>
        _engine.ConfirmAllotAsync(request, ct);

    public Task<LoomComponentBatchResult> PreviewAllLoomComponentsAsync(
        LoomComponentBatchRequest request,
        CancellationToken ct = default) =>
        RunComponentBatchAsync(request, confirm: false, ct);

    public Task<LoomComponentBatchResult> ConfirmAllLoomComponentsAsync(
        LoomComponentBatchRequest request,
        CancellationToken ct = default) =>
        RunComponentBatchAsync(request, confirm: true, ct);

    private async Task<LoomComponentBatchResult> RunComponentBatchAsync(
        LoomComponentBatchRequest request,
        bool confirm,
        CancellationToken ct)
    {
        var orderNo = request.OrderNo.Trim();
        var context = await _repository.GetOrderAllotmentContextAsync(orderNo, ct);
        if (context is null)
        {
            return new LoomComponentBatchResult
            {
                Success = false,
                Message = "No marketing, BOM, or loom data found for this order.",
                OrderNo = orderNo,
            };
        }

        var eligible = (context.LoomEligibleLines.Count > 0
                ? context.LoomEligibleLines
                : context.FabricLines.Where(f => f.IsLoomEligible).ToList())
            .OrderBy(f => BomComponentClassifier.SortRank(f.Category))
            .ThenBy(f => f.Heading, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (eligible.Count == 0)
        {
            return new LoomComponentBatchResult
            {
                Success = false,
                Message = "No loom-eligible BOM fabric lines found (need Body/Side/Top/Bottom/Baffle/Spout with meters and width).",
                OrderNo = orderNo,
                LoomEligibleCount = 0,
            };
        }

        var savedHeadings = (await _repository.GetOrderAllocationsAsync(orderNo, ct))
            .Select(a => BomComponentClassifier.NormalizeHeading(a.Remarks))
            .Where(h => !string.IsNullOrEmpty(h))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var fabricDate = request.FabricRequirementDate ?? context.FabricRequirementDate;
        var overlayByCompany = new Dictionary<string, List<LoomOccupancyBlockDto>>(StringComparer.OrdinalIgnoreCase);
        var components = new List<LoomAllotmentResult>();
        var warnings = new List<string>
        {
            confirm
                ? "Components are confirmed in sequence. Later fabrics see earlier saves on the same loom pool."
                : "Occupancy is chained within each weaving factory: later fabrics treat earlier proposed segments as occupied.",
        };
        var savedCount = 0;
        var rowsInserted = 0;

        foreach (var line in eligible)
        {
            ct.ThrowIfCancellationRequested();
            var gsm = BomComponentClassifier.ParseGsm(line.Gsm);
            var size = line.FabricSize ?? 0;
            var meters = line.TotalMtr ?? 0;
            if (gsm <= 0 || size <= 0 || meters <= 0)
            {
                components.Add(new LoomAllotmentResult
                {
                    Success = false,
                    OrderNo = orderNo,
                    Heading = line.Heading,
                    Message = $"Skipped {line.Heading}: missing GSM, width, or meters.",
                    RequiredMeters = meters,
                    ReqGsm = gsm,
                    Size = size,
                });
                continue;
            }

            var headingKey = BomComponentClassifier.NormalizeHeading(line.Heading);
            if (confirm && savedHeadings.Contains(headingKey))
            {
                components.Add(new LoomAllotmentConfirmResult
                {
                    Success = true,
                    FullyAllotted = true,
                    Saved = false,
                    OrderNo = orderNo,
                    Heading = line.Heading,
                    RequiredMeters = meters,
                    AllottedMeters = meters,
                    ReqGsm = gsm,
                    Size = size,
                    Message = $"{line.Heading} already has saved loom rows — skipped.",
                });
                continue;
            }

            var allotRequest = new LoomAllotmentRequest
            {
                OrderNo = orderNo,
                CompanyName = request.CompanyName,
                PartyName = request.PartyName ?? context.PartyName,
                Heading = line.Heading,
                ReqGsm = gsm,
                Size = size,
                RequiredMeters = meters,
                FabricRequirementDate = fabricDate ?? line.TargetDate,
            };

            if (confirm)
            {
                var saved = await _engine.ConfirmAllotAsync(allotRequest, ct);
                saved.Heading = line.Heading;
                components.Add(saved);
                if (saved.Saved)
                {
                    savedCount++;
                    rowsInserted += saved.RowsInserted;
                    savedHeadings.Add(headingKey);
                }
                continue;
            }

            if (overlayByCompany.TryGetValue(request.CompanyName ?? _options.DefaultCompanyName, out var knownOverlay))
                allotRequest.OccupancyOverlay = knownOverlay;

            var preview = await _engine.AllotAsync(allotRequest, ct);
            var companyKey = preview.CompanyName ?? request.CompanyName ?? _options.DefaultCompanyName;
            if (overlayByCompany.TryGetValue(companyKey, out var overlay)
                && !ReferenceEquals(allotRequest.OccupancyOverlay, overlay))
            {
                allotRequest.OccupancyOverlay = overlay;
                preview = await _engine.AllotAsync(allotRequest, ct);
            }

            preview.Heading = line.Heading;
            components.Add(preview);
            if (preview.Success && preview.ProposedSegments.Count > 0)
            {
                if (!overlayByCompany.TryGetValue(companyKey, out var list))
                {
                    list = [];
                    overlayByCompany[companyKey] = list;
                }

                foreach (var seg in preview.ProposedSegments)
                {
                    list.Add(new LoomOccupancyBlockDto
                    {
                        LoomNo = seg.LoomNo,
                        FromDate = seg.FromDate,
                        ToDate = seg.ToDate,
                        Heading = line.Heading,
                        ReqGsm = seg.ReqGsm,
                        Size = seg.Size,
                    });
                }
            }
        }

        var fully = components.Count(c => c.Success && c.FullyAllotted);
        var previewedOk = fully == components.Count && components.Count > 0;
        return new LoomComponentBatchResult
        {
            Success = confirm ? savedCount > 0 || previewedOk : previewedOk,
            Message = confirm
                ? $"Confirmed {savedCount} component(s); inserted {rowsInserted} loom row(s). {fully}/{components.Count} fully allotted."
                : $"Previewed {components.Count} loom fabric component(s); {fully} fully allotted (occupancy chained).",
            OrderNo = orderNo,
            LoomEligibleCount = eligible.Count,
            FullyAllottedCount = fully,
            Warnings = warnings,
            Components = components,
            SavedCount = savedCount,
            RowsInserted = rowsInserted,
            Confirmed = confirm,
        };
    }
}
