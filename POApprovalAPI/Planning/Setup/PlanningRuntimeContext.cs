using POApprovalAPI.Planning.Fibc;
using POApprovalAPI.Planning.Setup.Models;

namespace POApprovalAPI.Planning.Setup;

/// <summary>
/// Resolved planning inputs for a factory — merges portal setup with ERP fallbacks.
/// </summary>
public sealed class PlanningRuntimeContext
{
    public string CompanyName { get; init; } = "";
    public PlanningFactoryConfigDto Factory { get; init; } = new();
    public IReadOnlyList<PlanningLineConfigDto> Lines { get; init; } = Array.Empty<PlanningLineConfigDto>();
    public IReadOnlyList<PlanningLoomPoolDto> LoomPool { get; init; } = Array.Empty<PlanningLoomPoolDto>();
    public IReadOnlyList<PlanningTeamFactorDto> TeamFactors { get; init; } = Array.Empty<PlanningTeamFactorDto>();
    public IReadOnlyDictionary<string, double> BacklogByLineShift { get; init; } =
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<PlanningDowntimeDto> Downtime { get; init; } = Array.Empty<PlanningDowntimeDto>();

    public bool HasPortalLineConfig => Lines.Any(l => l.LineConfigId.HasValue);

    public static string LineShiftKey(int lineNo, string shift) =>
        $"{lineNo}|{shift.Trim().ToUpperInvariant()}";

    public static string SlotKey(DateTime date, int lineNo, string shift) =>
        $"{date:yyyy-MM-dd}|{lineNo}|{shift.Trim().ToUpperInvariant()}";

    public double GetBacklogReserved(int lineNo, string shift) =>
        BacklogByLineShift.GetValueOrDefault(LineShiftKey(lineNo, shift));

    public double GetTeamFactor(int lineNo, string? shift, string? teamNo)
    {
        if (TeamFactors.Count == 0)
            return 1.0;

        var shiftNorm = shift?.Trim().ToUpperInvariant() ?? "";
        var team = teamNo?.Trim() ?? "";

        PlanningTeamFactorDto? match = null;
        if (!string.IsNullOrEmpty(team))
        {
            match = TeamFactors.FirstOrDefault(f =>
                f.LineNo == lineNo &&
                string.Equals(f.TeamNo, team, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrEmpty(f.Shift) || string.Equals(f.Shift, shiftNorm, StringComparison.OrdinalIgnoreCase)));

            match ??= TeamFactors.FirstOrDefault(f =>
                f.LineNo == lineNo &&
                string.Equals(f.TeamNo, team, StringComparison.OrdinalIgnoreCase));
        }

        match ??= TeamFactors.FirstOrDefault(f =>
            f.LineNo == lineNo &&
            (string.IsNullOrEmpty(f.Shift) || string.Equals(f.Shift, shiftNorm, StringComparison.OrdinalIgnoreCase)));

        return match?.EffectiveFactor > 0 ? match.EffectiveFactor : 1.0;
    }

    public int GetLineCapacity(PlanningLineConfigDto line, string dustLevel)
    {
        var cap = dustLevel switch
        {
            "Single" or "SingleDust" => line.CapacitySingleDust,
            "Double" or "DoubleDust" => line.CapacityDoubleDust,
            "Triple" or "TripleDust" => line.CapacityTripleDust,
            _ => line.CapacityNormal,
        };

        return cap ?? line.CapacityNormal ?? line.ErpBagCapacity ?? 0;
    }

    public double GetDowntimeFactor(DateTime date, int lineNo, string shift)
    {
        var day = date.Date;
        var shiftNorm = shift.Trim().ToUpperInvariant();
        var hits = Downtime.Where(d =>
            d.PlanDate.Date == day &&
            (d.LineNo == 0 || d.LineNo == lineNo) &&
            (string.IsNullOrEmpty(d.Shift) || string.Equals(d.Shift, shiftNorm, StringComparison.OrdinalIgnoreCase)));

        var factor = 1.0;
        foreach (var d in hits)
            factor *= Math.Clamp(d.CapacityFactor, 0, 1);

        return factor;
    }

    public IReadOnlyList<int> GetPreferredLines(string erpFamily)
    {
        var fromPortal = Lines
            .Where(l => l.IsActive)
            .Where(l => l.AllowedBagFamilies.Any(f =>
                f.Equals(erpFamily, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(l => l.PreferenceOrder > 0 ? l.PreferenceOrder : l.LineNo)
            .Select(l => l.LineNo)
            .Distinct()
            .ToList();

        if (fromPortal.Count > 0)
            return fromPortal;

        return LinePreferenceHelper.GetPreferredLines(erpFamily).ToList();
    }

    public bool LineSupportsBagFamily(PlanningLineConfigDto? portalLine, string? erpBagType, string erpFamily)
    {
        if (portalLine is not null)
        {
            if (!portalLine.IsActive)
                return false;
            if (portalLine.AllowedBagFamilies.Count > 0)
                return portalLine.AllowedBagFamilies.Any(f =>
                    f.Equals(erpFamily, StringComparison.OrdinalIgnoreCase));
        }

        return LinePreferenceHelper.LineSupportsBagFamily(erpBagType, erpFamily);
    }

    public HashSet<int> GetPlanningLoomNos()
    {
        var configured = LoomPool.Where(l => l.IncludeInPlanning).Select(l => l.LoomNo).ToHashSet();
        if (configured.Count > 0)
            return configured;

        return LoomPool.Select(l => l.LoomNo).ToHashSet();
    }

    public PlanningLoomPoolDto? GetLoomPoolEntry(int loomNo) =>
        LoomPool.FirstOrDefault(l => l.LoomNo == loomNo);
}

public sealed class PlanningRuntimeContextLoader
{
    private readonly IPlanningSetupRepository _setup;

    public PlanningRuntimeContextLoader(IPlanningSetupRepository setup)
    {
        _setup = setup;
    }

    public async Task<PlanningRuntimeContext> LoadAsync(string companyName, CancellationToken ct = default)
    {
        var company = companyName.Trim();
        var factory = await _setup.GetFactoryConfigAsync(company, ct)
            ?? new PlanningFactoryConfigDto { CompanyName = company, IsPlanningEnabled = true };

        var lines = await _setup.GetMergedLineConfigsAsync(company, ct);
        var looms = await _setup.GetMergedLoomPoolAsync(company, ct);
        var factors = await _setup.GetTeamFactorsAsync(company, ct);
        var backlog = await _setup.GetBacklogAsync(company, "Open", ct);
        var downtime = await _setup.GetDowntimeAsync(company, null, null, ct);

        var backlogMap = backlog
            .GroupBy(b => PlanningRuntimeContext.LineShiftKey(b.LineNo, b.Shift))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.BacklogQty), StringComparer.OrdinalIgnoreCase);

        return new PlanningRuntimeContext
        {
            CompanyName = company,
            Factory = factory,
            Lines = lines,
            LoomPool = looms,
            TeamFactors = factors,
            BacklogByLineShift = backlogMap,
            Downtime = downtime,
        };
    }
}
