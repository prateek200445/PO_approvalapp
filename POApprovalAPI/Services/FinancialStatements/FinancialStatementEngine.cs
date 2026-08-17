using POApprovalAPI.Models;

namespace POApprovalAPI.Services.FinancialStatements;

public class FinancialStatementEngine
{
    private readonly FinancialStatementTemplateStore _templates;
    private readonly PresentationRulesService _presentationRules;

    public FinancialStatementEngine(
        FinancialStatementTemplateStore templates,
        PresentationRulesService presentationRules)
    {
        _templates = templates;
        _presentationRules = presentationRules;
    }

    public FinancialStatementResultDto Generate(
        string companyKey,
        string companyName,
        string periodLabel,
        IReadOnlyList<TrialBalanceRowDto> rows,
        Dictionary<string, string> groupLookup,
        PresentationRules? rulesOverride = null)
    {
        var structure = _templates.GetStructure();
        var rules = rulesOverride ?? _presentationRules.GetRules(companyKey);
        var divisor = structure.AmountDivisor <= 0 ? 100_000m : structure.AmountDivisor;

        foreach (var row in rows)
        {
            row.MappedGroup = ResolveGroup(row, groupLookup);
        }

        var groupTotals = AggregateByGroup(rows);
        var schedules = BuildSchedules(structure, groupTotals, rules, divisor);
        var profitAndLoss = BuildProfitAndLoss(structure, groupTotals, rules, divisor);
        var balanceSheet = BuildBalanceSheet(structure, groupTotals, rules, divisor);
        ApplyCurrentYearProfitToReserves(balanceSheet, profitAndLoss, rules);

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
        PresentationRules? rules,
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
                var lakhs = ResolveGroupAmount(group, nature, groupTotals, rules, structure, divisor, def.Note);

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

            if (def.Note == "9" && rules?.TradePayables != null)
            {
                noteTotal = Round2(rules.TradePayables.MsmeLakhs +
                                   rules.TradePayables.CreditorsOthersLakhs +
                                   rules.TradePayables.BillOfExchangeLakhs);
            }
            else if (def.Note == "20" && rules?.CogsMovement != null
                     && rules.CogsMovement.OpeningRmPackingLakhs > 0
                     && rules.CogsMovement.ClosingRmPackingLakhs > 0)
            {
                noteTotal = ComputeCogsMovement(rules, groupTotals, divisor);
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
        PresentationRules? rules,
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

                var amount = ResolveLineAmount(lineDef, groupTotals, rules, structure, divisor);
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

    private static void ApplyCurrentYearProfitToReserves(
        List<ReportSectionDto> balanceSheet,
        List<ReportLineDto> profitAndLoss,
        PresentationRules? rules)
    {
        if (rules?.ReservesAndSurplusLakhs is > 0m)
            return;

        var profitForYear = profitAndLoss
            .FirstOrDefault(l => l.Label.StartsWith("Profit for the year", StringComparison.OrdinalIgnoreCase))
            ?.AmountLakhs ?? 0m;

        if (profitForYear == 0m)
            return;

        foreach (var section in balanceSheet)
        {
            if (!section.Title.Contains("liabilit", StringComparison.OrdinalIgnoreCase) &&
                !section.Title.Contains("Equity", StringComparison.OrdinalIgnoreCase))
                continue;

            var reservesLine = section.Lines.FirstOrDefault(l =>
                l.Label.Equals("Reserves and surplus", StringComparison.OrdinalIgnoreCase));
            if (reservesLine == null)
                continue;

            reservesLine.AmountLakhs = Round2(reservesLine.AmountLakhs + profitForYear);
            section.SectionTotalLakhs = Round2(section.Lines.Where(l => !l.IsHeader).Sum(l => l.AmountLakhs));
            break;
        }
    }

    private List<ReportLineDto> BuildProfitAndLoss(
        ReportStructure structure,
        Dictionary<string, GroupAggregate> groupTotals,
        PresentationRules? rules,
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
                amount = Round2(lineDef.SumLabels.Sum(label => GetComputed(computed, label)));
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
                amount = EvaluateFormula(lineDef, computed);
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
                amount = ResolveLineAmount(lineDef, groupTotals, rules, structure, divisor);
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

    private decimal ResolveLineAmount(
        ReportLineDefinition lineDef,
        Dictionary<string, GroupAggregate> groupTotals,
        PresentationRules? rules,
        ReportStructure structure,
        decimal divisor)
    {
        var calculation = (lineDef.Calculation ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(calculation))
            calculation = "sum";

        return calculation switch
        {
            "sumwithpresentation" => SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note, usePresentation: true),
            "cogsmovement" => rules?.CogsLakhs is > 0m
                ? rules.CogsLakhs.Value
                : ResolveCogsMovement(lineDef, groupTotals, rules, structure, divisor),
            "longtermborrowings" => rules?.LongTermBorrowingsLakhs
                ?? ResolveLongTermBorrowings(lineDef, groupTotals, rules, structure, divisor),
            "tradepayablesmsme" => rules?.TradePayables?.MsmeLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note),
            "tradepayablesothers" => rules?.TradePayables != null
                ? Round2(rules.TradePayables.CreditorsOthersLakhs + rules.TradePayables.BillOfExchangeLakhs)
                : SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note),
            "tradereceivables" => rules?.TradeReceivablesLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, "16", usePresentation: true),
            "inventories" => rules?.InventoriesLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, "15", usePresentation: true),
            "othercurrentliabilities" => rules?.OtherCurrentLiabilitiesLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note),
            "depreciation" => rules?.DepreciationLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note),
            "changesininventory" => rules?.ChangesInInventoryLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note),
            "otheroperatingrevenue" => rules?.OtherOperatingRevenueLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note, usePresentation: true),
            "otherincome" => RoundLineAmount(rules?.OtherIncomeLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note)),
            "financecosts" => rules?.FinanceCostsLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note),
            "longtermprovisions" => rules?.LongTermProvisionsLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note),
            "shorttermprovisions" => rules?.ShortTermProvisionsLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note),
            "othernoncurrentassets" => rules?.OtherNonCurrentAssetsLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note),
            "othercurrentassets" => rules?.OtherCurrentAssetsLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note),
            "propertyplantequipment" => rules?.PropertyPlantEquipmentLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note),
            "reservesandsurplus" => rules?.ReservesAndSurplusLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, "4", usePresentation: true),
            "loansandadvancesnoncurrent" => rules?.LoansAndAdvancesNonCurrentLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, "13", usePresentation: true),
            "loansandadvancescurrent" => rules?.LoansAndAdvancesCurrentLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, "13", usePresentation: true),
            "otherexpenses" => rules?.OtherExpensesLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note),
            "insuranceclaimexceptional" => rules?.InsuranceClaimEarlierYearLakhs
                ?? Math.Abs(SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note, usePresentation: true)),
            "currenttax" => rules?.CurrentTaxLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note),
            "cashandbank" => RoundLineAmount(rules?.CashAndBankLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note)),
            "revenuefromoperations" => RoundLineAmount(rules?.RevenueFromOperationsLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note)),
            "employeebenefits" => RoundLineAmount(rules?.EmployeeBenefitsLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note)),
            "deferredtaxliability" => RoundLineAmount(rules?.DeferredTaxLiabilityLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, "6", usePresentation: true)),
            "deferredtax" => RoundLineAmount(rules?.DeferredTaxLakhs
                ?? SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note)),
            _ => RoundLineAmount(SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, lineDef.Note))
        };
    }

    private static decimal ResolveCogsMovement(
        ReportLineDefinition lineDef,
        Dictionary<string, GroupAggregate> groupTotals,
        PresentationRules? rules,
        ReportStructure structure,
        decimal divisor)
    {
        if (rules?.CogsMovement != null
            && rules.CogsMovement.OpeningRmPackingLakhs > 0
            && rules.CogsMovement.ClosingRmPackingLakhs > 0)
        {
            return RoundLineAmount(ComputeCogsMovement(rules, groupTotals, divisor));
        }

        return RoundLineAmount(SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor, "20", usePresentation: true));
    }

    private static decimal RoundLineAmount(decimal amount) => Round2(amount);

    private static decimal ComputeCogsMovement(
        PresentationRules rules,
        Dictionary<string, GroupAggregate> groupTotals,
        decimal divisor)
    {
        var cogs = rules.CogsMovement!;
        decimal purchasesLakhs = 0m;
        foreach (var group in cogs.PurchaseGroups)
        {
            if (rules.GroupPresentationLakhs.TryGetValue(group, out var presentation))
            {
                purchasesLakhs += presentation;
                continue;
            }

            groupTotals.TryGetValue(group, out var agg);
            purchasesLakhs += NormalizeRaw(agg?.RawClosing ?? 0m, "expense") / divisor;
        }

        return Round2(purchasesLakhs + cogs.OpeningRmPackingLakhs - cogs.ClosingRmPackingLakhs);
    }

    private static decimal ResolveLongTermBorrowings(
        ReportLineDefinition lineDef,
        Dictionary<string, GroupAggregate> groupTotals,
        PresentationRules? rules,
        ReportStructure structure,
        decimal divisor)
    {
        if (rules?.LoanMaturityLakhs.Count > 0)
        {
            decimal total = 0m;
            foreach (var group in lineDef.Groups)
            {
                if (rules.LoanMaturityLakhs.TryGetValue(group, out var split))
                {
                    total += split.NonCurrentLakhs;
                    continue;
                }

                total += ResolveGroupAmount(group, "liability", groupTotals, rules, structure, divisor, "5");
            }

            return Round2(total);
        }

        return SumGroups(lineDef.Groups, groupTotals, lineDef.Nature, structure, rules, divisor);
    }

    private static decimal EvaluateFormula(ReportLineDefinition lineDef, Dictionary<string, decimal> computed)
    {
        var formula = (lineDef.Formula ?? "subtract").Trim().ToLowerInvariant();
        var operands = lineDef.Operands.Count > 0 ? lineDef.Operands : lineDef.SumLabels;

        return formula switch
        {
            "subtract" when operands.Count >= 2 =>
                Round2(GetComputed(computed, operands[0]) - GetComputed(computed, operands[1])),
            "add" => Round2(operands.Sum(op => GetComputed(computed, op))),
            _ => Round2(GetComputed(computed, operands.ElementAtOrDefault(0)) -
                      GetComputed(computed, operands.ElementAtOrDefault(1)))
        };
    }

    private static decimal GetComputed(Dictionary<string, decimal> computed, string label)
    {
        if (computed.TryGetValue(label, out var exact))
            return exact;

        var normalized = NormalizeLabel(label);
        foreach (var pair in computed)
        {
            if (NormalizeLabel(pair.Key) == normalized)
                return pair.Value;
        }

        return 0m;
    }

    private static string NormalizeLabel(string label)
        => string.Join(' ', label.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static decimal SumGroups(
        IEnumerable<string> groups,
        Dictionary<string, GroupAggregate> groupTotals,
        string nature,
        ReportStructure structure,
        PresentationRules? rules,
        decimal divisor,
        string note = "",
        bool usePresentation = false)
    {
        decimal total = 0m;
        foreach (var group in groups)
        {
            var resolvedNature = string.IsNullOrWhiteSpace(nature)
                ? ResolveNature(structure, group, note)
                : nature;
            total += usePresentation
                ? ResolveGroupAmount(group, resolvedNature, groupTotals, rules, structure, divisor, note)
                : ToDisplayLakhsUnrounded(groupTotals.GetValueOrDefault(group)?.RawClosing ?? 0m, resolvedNature, divisor);
        }

        return Round2(total);
    }

    private static decimal ResolveGroupAmount(
        string group,
        string nature,
        Dictionary<string, GroupAggregate> groupTotals,
        PresentationRules? rules,
        ReportStructure structure,
        decimal divisor,
        string note)
    {
        if (rules?.GroupPresentationLakhs.TryGetValue(group, out var presentation) == true &&
            ShouldUsePresentationOverride(group, note))
        {
            return Round2(Math.Abs(presentation));
        }

        groupTotals.TryGetValue(group, out var agg);
        var raw = agg?.RawClosing ?? 0m;
        var resolvedNature = string.IsNullOrWhiteSpace(nature)
            ? ResolveNature(structure, group, note)
            : nature;
        return Round2(ToDisplayLakhsUnrounded(raw, resolvedNature, divisor));
    }

    private static bool ShouldUsePresentationOverride(string group, string note)
    {
        if (group.Equals("Profit & Loss Account", StringComparison.OrdinalIgnoreCase))
            return note is "4" or "";

        if (group.Equals("Insurance claim of earlier year", StringComparison.OrdinalIgnoreCase))
            return false;

        if (group.Equals("Deferred tax liability (net)", StringComparison.OrdinalIgnoreCase))
            return note is "6";

        if (group.Equals("Inventory", StringComparison.OrdinalIgnoreCase))
            return note is "15" or "20";

        if (group.Equals("Depreciation Expenses", StringComparison.OrdinalIgnoreCase))
            return note is "11";

        if (group.Equals("Total outstanding dues of creditors other than micro and small enterprise", StringComparison.OrdinalIgnoreCase))
            return note is "9";

        if (group.Equals("Other receivable ( unsecured, considered good)", StringComparison.OrdinalIgnoreCase))
            return note is "16";

        return true;
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
        => Round2(ToDisplayLakhsUnrounded(rawClosing, nature, divisor));

    private static decimal ToDisplayLakhsUnrounded(decimal rawClosing, string nature, decimal divisor)
        => NormalizeRaw(rawClosing, nature) / divisor;

    private static decimal NormalizeRaw(decimal rawClosing, string nature)
        => nature.ToLowerInvariant() switch
        {
            "liability" or "equity" or "income" => -rawClosing,
            _ => rawClosing
        };

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed class GroupAggregate
    {
        public string Group { get; set; } = "";
        public decimal RawClosing { get; set; }
        public int LedgerCount { get; set; }
    }
}
