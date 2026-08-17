using POApprovalAPI.Models;

namespace POApprovalAPI.Services.FinancialStatements;

public class FinancialStatementEngine
{
    private readonly FinancialStatementTemplateStore _templates;

    public FinancialStatementEngine(FinancialStatementTemplateStore templates)
    {
        _templates = templates;
    }

    public FinancialStatementResultDto Generate(
        string companyKey,
        string companyName,
        string periodLabel,
        IReadOnlyList<TrialBalanceRowDto> rows,
        Dictionary<string, string> groupLookup)
    {
        var structure = _templates.GetStructure();
        var divisor = structure.AmountDivisor <= 0 ? 100_000m : structure.AmountDivisor;

        foreach (var row in rows)
        {
            row.MappedGroup = ResolveGroup(row, groupLookup);
        }

        var groupTotals = AggregateByGroup(rows);
        var schedules = BuildSchedules(structure, groupTotals, divisor);
        var balanceSheet = BuildBalanceSheet(structure, groupTotals, divisor);
        var profitAndLoss = BuildProfitAndLoss(structure, groupTotals, divisor);

        var unmapped = rows
            .Where(r => string.IsNullOrWhiteSpace(r.MappedGroup))
            .Select(r => new UnmappedLedgerDto
            {
                Ledger = r.Ledger,
                Closing = r.Closing,
                ClosingLakhs = ToDisplayLakhs(r.Closing, "asset", divisor)
            })
            .OrderByDescending(u => Math.Abs(u.ClosingLakhs))
            .ToList();

        var mappedCount = rows.Count - unmapped.Count;
        var assetTotal = balanceSheet
            .Where(s => s.Title.Contains("Assets", StringComparison.OrdinalIgnoreCase))
            .Sum(s => s.SectionTotalLakhs);
        var liabTotal = balanceSheet
            .Where(s => s.Title.Contains("liabilit", StringComparison.OrdinalIgnoreCase) ||
                        s.Title.Contains("Equity", StringComparison.OrdinalIgnoreCase))
            .Sum(s => s.SectionTotalLakhs);

        return new FinancialStatementResultDto
        {
            CompanyKey = companyKey,
            CompanyName = companyName,
            PeriodLabel = periodLabel,
            TotalLedgers = rows.Count,
            MappedLedgers = mappedCount,
            UnmappedLedgers = unmapped.Count,
            Schedules = schedules,
            BalanceSheet = balanceSheet,
            ProfitAndLoss = profitAndLoss,
            Unmapped = unmapped,
            TotalAssetsLakhs = Round2(assetTotal),
            TotalLiabilitiesAndEquityLakhs = Round2(liabTotal),
            BalanceSheetTotalLakhs = Round2(assetTotal - liabTotal)
        };
    }

    private static string ResolveGroup(TrialBalanceRowDto row, Dictionary<string, string> groupLookup)
    {
        if (!string.IsNullOrWhiteSpace(row.Group))
            return row.Group.Trim();

        if (groupLookup.TryGetValue(row.Ledger.Trim(), out var mapped) && !string.IsNullOrWhiteSpace(mapped))
            return mapped.Trim();

        return "";
    }

    private static Dictionary<string, GroupAggregate> AggregateByGroup(IReadOnlyList<TrialBalanceRowDto> rows)
    {
        var totals = new Dictionary<string, GroupAggregate>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.MappedGroup))
                continue;

            if (!totals.TryGetValue(row.MappedGroup, out var agg))
            {
                agg = new GroupAggregate { Group = row.MappedGroup };
                totals[row.MappedGroup] = agg;
            }

            agg.RawClosing += row.Closing;
            agg.LedgerCount++;
        }

        return totals;
    }

    private List<ScheduleNoteDto> BuildSchedules(
        ReportStructure structure,
        Dictionary<string, GroupAggregate> groupTotals,
        decimal divisor)
    {
        var schedules = new List<ScheduleNoteDto>();

        foreach (var def in structure.Schedules)
        {
            var lines = new List<ScheduleLineDto>();
            decimal noteTotal = 0m;

            foreach (var group in def.Groups)
            {
                groupTotals.TryGetValue(group, out var agg);
                var raw = agg?.RawClosing ?? 0m;
                var nature = ResolveNature(structure, group, def.Note);
                var lakhs = ToDisplayLakhs(raw, nature, divisor);

                lines.Add(new ScheduleLineDto
                {
                    Group = group,
                    Label = group,
                    RawClosing = raw,
                    AmountLakhs = lakhs,
                    LedgerCount = agg?.LedgerCount ?? 0
                });

                noteTotal += lakhs;
            }

            schedules.Add(new ScheduleNoteDto
            {
                Note = def.Note,
                Title = def.Title,
                Lines = lines,
                TotalLakhs = Round2(noteTotal)
            });
        }

        return schedules;
    }

    private List<ReportSectionDto> BuildBalanceSheet(
        ReportStructure structure,
        Dictionary<string, GroupAggregate> groupTotals,
        decimal divisor)
    {
        var sections = new List<ReportSectionDto>();

        foreach (var sectionDef in structure.BalanceSheet.Sections)
        {
            var section = new ReportSectionDto { Title = sectionDef.Title };
            decimal sectionTotal = 0m;

            foreach (var lineDef in sectionDef.Lines)
            {
                if (lineDef.Type == "header")
                {
                    section.Lines.Add(new ReportLineDto
                    {
                        Label = lineDef.Label,
                        LineType = "header",
                        IsHeader = true
                    });
                    continue;
                }

                var amount = SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, divisor);
                sectionTotal += amount;

                section.Lines.Add(new ReportLineDto
                {
                    Label = lineDef.Label,
                    Note = lineDef.Note,
                    AmountLakhs = amount,
                    LineType = "line"
                });
            }

            section.SectionTotalLakhs = Round2(sectionTotal);
            sections.Add(section);
        }

        return sections;
    }

    private List<ReportLineDto> BuildProfitAndLoss(
        ReportStructure structure,
        Dictionary<string, GroupAggregate> groupTotals,
        decimal divisor)
    {
        var lines = new List<ReportLineDto>();
        var computed = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var lineDef in structure.ProfitLoss.Lines)
        {
            if (lineDef.Type == "header")
            {
                lines.Add(new ReportLineDto
                {
                    Label = lineDef.Label,
                    LineType = "header",
                    IsHeader = true
                });
                continue;
            }

            decimal amount;
            if (lineDef.Type == "subtotal")
            {
                amount = Round2(lineDef.SumLabels.Sum(label => computed.GetValueOrDefault(label)));
                lines.Add(new ReportLineDto
                {
                    Label = lineDef.Label,
                    AmountLakhs = amount,
                    LineType = "subtotal",
                    IsSubtotal = true
                });
            }
            else if (lineDef.Type == "formula")
            {
                var revenue = computed.GetValueOrDefault("Total revenue (I+II)");
                var expenses = computed.GetValueOrDefault("Total expenses");
                amount = Round2(revenue - expenses);
                lines.Add(new ReportLineDto
                {
                    Label = lineDef.Label,
                    AmountLakhs = amount,
                    LineType = "formula",
                    IsSubtotal = true
                });
            }
            else
            {
                amount = SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, divisor);
                lines.Add(new ReportLineDto
                {
                    Label = lineDef.Label,
                    Note = lineDef.Note,
                    AmountLakhs = amount,
                    LineType = "line"
                });
            }

            computed[lineDef.Label] = amount;
        }

        return lines;
    }

    private static decimal SumGroups(
        IEnumerable<string> groups,
        Dictionary<string, GroupAggregate> groupTotals,
        string nature,
        ReportStructure structure,
        decimal divisor)
    {
        decimal total = 0m;
        foreach (var group in groups)
        {
            groupTotals.TryGetValue(group, out var agg);
            var raw = agg?.RawClosing ?? 0m;
            var resolvedNature = string.IsNullOrWhiteSpace(nature)
                ? ResolveNature(structure, group, "")
                : nature;
            total += ToDisplayLakhs(raw, resolvedNature, divisor);
        }

        return Round2(total);
    }

    private static string ResolveNature(ReportStructure structure, string group, string note)
    {
        if (structure.GroupNature.TryGetValue(group, out var nature) && !string.IsNullOrWhiteSpace(nature))
            return nature;

        if (note is "18" or "19")
            return "income";
        if (note is "20" or "21" or "22" or "23" or "24" or "25")
            return "expense";
        if (note is "3" or "4")
            return "equity";
        if (note is "5" or "6" or "7" or "8" or "9" or "10")
            return "liability";
        if (note is "11" or "12" or "13" or "14" or "15" or "16" or "17")
            return "asset";

        return "asset";
    }

    private static decimal ToDisplayLakhs(decimal rawClosing, string nature, decimal divisor)
    {
        var normalized = nature.ToLowerInvariant() switch
        {
            "liability" or "equity" or "income" => -rawClosing,
            _ => rawClosing
        };

        return Round2(normalized / divisor);
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed class GroupAggregate
    {
        public string Group { get; set; } = "";
        public decimal RawClosing { get; set; }
        public int LedgerCount { get; set; }
    }
}
