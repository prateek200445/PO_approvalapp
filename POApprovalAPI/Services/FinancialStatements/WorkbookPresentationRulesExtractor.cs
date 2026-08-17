using ClosedXML.Excel;

namespace POApprovalAPI.Services.FinancialStatements;

/// <summary>
/// Reads presentation / reclassification inputs from supplementary workbook tabs
/// (check, short term, 7 to 10, 18 to 24, group) instead of hardcoded JSON overrides.
/// </summary>
public class WorkbookPresentationRulesExtractor
{
    private static readonly string[] CogsPurchaseGroups =
    [
        "Add : Purchase of Raw Material",
        "Add : Purchase of Packing Material",
        "Add : Purchase Expenses"
    ];

    public PresentationRules Extract(XLWorkbook workbook, string companyKey, byte[]? xlsxBytes = null)
    {
        var rules = new PresentationRules
        {
            CompanyKey = companyKey
        };

        if (xlsxBytes != null)
            ExtractCheckSheetFromXml(xlsxBytes, rules);
        else
            ExtractCheckSheet(FindSheet(workbook, "check"), rules);

        ExtractShortTermSheet(FindSheet(workbook, "short term"), rules);
        ExtractTradePayablesSheet(FindSheet(workbook, "7 to 10"), rules);
        ExtractCogsMovementSheet(FindSheet(workbook, "18 to 24"), rules);
        ExtractPlWorkingSheet(FindSheet(workbook, "18 to 24"), rules);
        ExtractOtherExpensesSheet(FindSheet(workbook, "25 to 28"), rules);
        ExtractProvisionsAndLiabilitiesSheet(FindSheet(workbook, "7 to 10"), rules);
        ExtractBorrowingsAndDeferredTaxSheet(FindSheet(workbook, "5 to 6"), rules);
        ExtractOtbAdjustmentsSheet(FindSheet(workbook, "OTB1"), rules);
        ExtractDepreciationSheet(FindSheet(workbook, "IT ") ?? FindSheet(workbook, "IT WORKING"), rules);

        return rules;
    }

    private static void ExtractCheckSheetFromXml(byte[] xlsxBytes, PresentationRules rules)
    {
        Dictionary<(int Row, int Col), string> cells;
        try
        {
            cells = ExcelSheetXmlReader.ReadSheet(xlsxBytes, "check");
        }
        catch
        {
            return;
        }

        var maxRow = cells.Keys.Count > 0 ? cells.Keys.Max(k => k.Row) : 0;
        for (var r = 1; r <= maxRow; r++)
        {
            var group = ExcelSheetXmlReader.GetCell(cells, r, 2).Trim();
            if (string.IsNullOrWhiteSpace(group))
                continue;

            var rawLakhs = ExcelSheetXmlReader.GetDecimal(cells, r, 4);
            var fsLakhs = ExcelSheetXmlReader.GetDecimal(cells, r, 5);
            var adjustment = ExcelSheetXmlReader.GetDecimal(cells, r, 6);

            if (fsLakhs == null)
                continue;

            if (Math.Abs(fsLakhs.Value) < 0.01m)
            {
                if (adjustment.HasValue && Math.Abs(adjustment.Value) > 0.01m)
                    rules.GroupAdjustmentsLakhs[group] = Round2(adjustment.Value);
                continue;
            }

            var fs = Math.Abs(fsLakhs.Value);
            var raw = rawLakhs.HasValue ? Math.Abs(rawLakhs.Value) : (decimal?)null;
            var adj = adjustment ?? 0m;

            if (Math.Abs(adj) > 0.01m || (raw.HasValue && Math.Abs(fs - raw.Value) > 0.01m))
            {
                rules.GroupPresentationLakhs[group] = Round2(fs);
                if (Math.Abs(adj) > 0.01m)
                    rules.GroupAdjustmentsLakhs[group] = Round2(adj);
            }
        }
    }

    public void MergeGroupSheetLookup(XLWorkbook workbook, Dictionary<string, string> lookup)
    {
        var sheet = FindSheet(workbook, "group");
        if (sheet == null)
            return;

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= lastRow; r++)
        {
            var ledger = ReadString(sheet, r, 1);
            var group = ReadString(sheet, r, 2);
            if (string.IsNullOrWhiteSpace(ledger) || string.IsNullOrWhiteSpace(group) || group == "0")
                continue;

            lookup.TryAdd(ledger.Trim(), group.Trim());
        }
    }

    public bool HasSupplementarySheets(XLWorkbook workbook)
        => FindSheet(workbook, "check") != null
           || FindSheet(workbook, "short term") != null
           || FindSheet(workbook, "7 to 10") != null
           || FindSheet(workbook, "18 to 24") != null
           || FindSheet(workbook, "25 to 28") != null
           || FindSheet(workbook, "5 to 6") != null
           || FindSheet(workbook, "OTB1") != null;

    private static void ExtractCheckSheet(IXLWorksheet? sheet, PresentationRules rules)
    {
        if (sheet == null)
            return;

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 1; r <= lastRow; r++)
        {
            var group = ReadString(sheet, r, 2);
            if (string.IsNullOrWhiteSpace(group))
                continue;

            var rawLakhs = TryReadDecimal(sheet, r, 4);
            var fsLakhs = TryReadDecimal(sheet, r, 5);
            var adjustment = TryReadDecimal(sheet, r, 6);

            if (fsLakhs == null)
                continue;

            if (Math.Abs(fsLakhs.Value) < 0.01m)
            {
                if (adjustment.HasValue && Math.Abs(adjustment.Value) > 0.01m)
                    rules.GroupAdjustmentsLakhs[group] = Round2(adjustment.Value);
                continue;
            }

            var fs = Math.Abs(fsLakhs.Value);
            var raw = rawLakhs.HasValue ? Math.Abs(rawLakhs.Value) : (decimal?)null;
            var adj = adjustment ?? 0m;

            if (Math.Abs(adj) > 0.01m || (raw.HasValue && Math.Abs(fs - raw.Value) > 0.01m))
            {
                rules.GroupPresentationLakhs[group] = Round2(fs);
                if (Math.Abs(adj) > 0.01m)
                    rules.GroupAdjustmentsLakhs[group] = Round2(adj);
            }
        }
    }

    private static void ExtractShortTermSheet(IXLWorksheet? sheet, PresentationRules rules)
    {
        if (sheet == null)
            return;

        var currentByGroup = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var nonCurrentByGroup = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        for (var r = 15; r <= 500; r++)
        {
            var group = ReadString(sheet, r, 9);
            if (string.IsNullOrWhiteSpace(group))
                continue;

            var current = TryReadDecimal(sheet, r, 7);
            var nonCurrent = TryReadDecimal(sheet, r, 8);

            if (current.HasValue)
                currentByGroup[group] = currentByGroup.GetValueOrDefault(group) + current.Value;
            if (nonCurrent.HasValue)
                nonCurrentByGroup[group] = nonCurrentByGroup.GetValueOrDefault(group) + nonCurrent.Value;
        }

        foreach (var group in currentByGroup.Keys.Union(nonCurrentByGroup.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var cur = currentByGroup.GetValueOrDefault(group);
            var nc = nonCurrentByGroup.GetValueOrDefault(group);
            rules.LoanMaturityLakhs[group] = new LoanMaturitySplit
            {
                CurrentLakhs = Round2(-cur / 100_000m),
                NonCurrentLakhs = Round2(-nc / 100_000m)
            };
        }
    }

    private static void ExtractTradePayablesSheet(IXLWorksheet? sheet, PresentationRules rules)
    {
        if (sheet == null)
            return;

        var tradePayables = new TradePayablesRules();
        var found = false;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var r = 1; r <= lastRow; r++)
        {
            var label = ReadString(sheet, r, 1);
            if (string.IsNullOrWhiteSpace(label))
                continue;

            var amount = TryReadDecimal(sheet, r, 6);
            if (amount == null)
                continue;

            var lakhs = Round2(Math.Abs(amount.Value));
            found = true;

            if (label.StartsWith("Total outstanding dues of micro and small enterprise", StringComparison.OrdinalIgnoreCase))
                tradePayables.MsmeLakhs = lakhs;
            else if (label.StartsWith("Total outstanding dues of creditors other than micro and small enterprise", StringComparison.OrdinalIgnoreCase))
                tradePayables.CreditorsOthersLakhs = lakhs;
            else if (label.StartsWith("Bill of Exchange", StringComparison.OrdinalIgnoreCase))
                tradePayables.BillOfExchangeLakhs = lakhs;
        }

        if (found)
            rules.TradePayables = tradePayables;
    }

    private static void ExtractCogsMovementSheet(IXLWorksheet? sheet, PresentationRules rules)
    {
        if (sheet == null)
            return;

        rules.CogsMovement ??= new CogsMovementRules { PurchaseGroups = CogsPurchaseGroups.ToList() };
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var foundOpening = false;
        var foundClosing = false;

        for (var r = 1; r <= lastRow; r++)
        {
            var label = ReadString(sheet, r, 1);
            if (string.IsNullOrWhiteSpace(label))
                continue;

            var amount = TryReadDecimal(sheet, r, 3);
            if (amount == null)
                continue;

            if (label.Contains("Inventory of raw and packing material at the beginning", StringComparison.OrdinalIgnoreCase))
            {
                rules.CogsMovement.OpeningRmPackingLakhs = Round2(Math.Abs(amount.Value));
                foundOpening = true;
            }
            else if (label.Contains("Inventory of raw and packing material at the end", StringComparison.OrdinalIgnoreCase))
            {
                rules.CogsMovement.ClosingRmPackingLakhs = Round2(Math.Abs(amount.Value));
                foundClosing = true;
            }
        }

        if (!foundOpening || !foundClosing)
            rules.CogsMovement = null;
    }

    private static void ExtractPlWorkingSheet(IXLWorksheet? sheet, PresentationRules rules)
    {
        if (sheet == null)
            return;

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        string? section = null;

        for (var r = 1; r <= lastRow; r++)
        {
            var label = ReadString(sheet, r, 1);
            if (!string.IsNullOrWhiteSpace(label))
            {
                if (label.Contains("Other operating revenue", StringComparison.OrdinalIgnoreCase))
                    section = "otherOperatingRevenue";
                else if (label.Contains("Revenue from operations", StringComparison.OrdinalIgnoreCase))
                    section = null;
                else if (label.Contains("19. Other income", StringComparison.OrdinalIgnoreCase))
                    section = "otherIncome";
                else if (label.Contains("20. Cost of materials", StringComparison.OrdinalIgnoreCase)
                         || label.Contains("Cost of materials consumed", StringComparison.OrdinalIgnoreCase))
                    section = "cogs";
                else if (label.Contains("21. Purchase of traded goods", StringComparison.OrdinalIgnoreCase))
                    section = "tradedGoods";
                else if (label.Contains("22. Changes in inventories", StringComparison.OrdinalIgnoreCase))
                    section = "changesInInventory";
                else if (label.Contains("23. Employees", StringComparison.OrdinalIgnoreCase))
                    section = "employeeBenefits";
                else if (label.Contains("24. Finance costs", StringComparison.OrdinalIgnoreCase))
                    section = "financeCosts";
            }

            var amount = TryReadDecimal(sheet, r, 3);
            if (amount == null)
                continue;

            var subLabel = ReadString(sheet, r, 2);

            switch (section)
            {
                case "otherOperatingRevenue":
                    if (amount is > 0m and < 5000m)
                    {
                        var lakhs = Round2(Math.Abs(amount.Value));
                        rules.OtherOperatingRevenueLakhs = rules.OtherOperatingRevenueLakhs == null
                            ? lakhs
                            : Math.Max(rules.OtherOperatingRevenueLakhs.Value, lakhs);
                    }
                    break;
                case "otherIncome" when IsSectionTotalRow(label, subLabel) || IsDashTotalRow(label):
                    rules.OtherIncomeLakhs = Round2(Math.Abs(amount.Value));
                    break;
                case "cogs" when IsSectionTotalRow(label, subLabel) && amount > 1000m:
                    rules.CogsLakhs = Round2(Math.Abs(amount.Value));
                    break;
                case "changesInInventory" when IsSectionTotalRow(label, subLabel)
                    && amount is > -5000m and < 5000m:
                    rules.ChangesInInventoryLakhs = Round2(amount.Value);
                    break;
                case "employeeBenefits" when IsSectionTotalRow(label, subLabel) && rules.EmployeeBenefitsLakhs == null:
                    rules.EmployeeBenefitsLakhs = Round2(Math.Abs(amount.Value));
                    break;
                case "financeCosts" when IsSectionTotalRow(label, subLabel):
                    rules.FinanceCostsLakhs = Round2(Math.Abs(amount.Value));
                    break;
            }
        }
    }

    private static void ExtractOtherExpensesSheet(IXLWorksheet? sheet, PresentationRules rules)
    {
        if (sheet == null)
            return;

        var lastRow = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 1, 60);
        decimal? total = null;

        for (var r = 1; r <= lastRow; r++)
        {
            var label = ReadString(sheet, r, 1);
            if (!string.IsNullOrWhiteSpace(label))
                continue;

            var amount = TryReadDecimal(sheet, r, 5);
            if (amount == null || amount <= 100m || amount > 50_000m)
                continue;

            var lakhs = Round2(Math.Abs(amount.Value));
            if (total == null || lakhs > total)
                total = lakhs;
        }

        if (total != null)
            rules.OtherExpensesLakhs = total;
    }

    private static void ExtractDepreciationSheet(IXLWorksheet? sheet, PresentationRules rules)
    {
        if (sheet == null)
            return;

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 1; r <= lastRow; r++)
        {
            var label = ReadString(sheet, r, 2);
            if (!label.Contains("Depreciation as per Companies Act", StringComparison.OrdinalIgnoreCase))
                continue;

            var rupees = TryReadDecimal(sheet, r, 7);
            if (rupees != null)
                rules.DepreciationLakhs = Round2(Math.Abs(rupees.Value) / 100_000m);
            return;
        }
    }

    private static void ExtractProvisionsAndLiabilitiesSheet(IXLWorksheet? sheet, PresentationRules rules)
    {
        if (sheet == null)
            return;

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        string? section = null;

        for (var r = 1; r <= lastRow; r++)
        {
            var label = ReadString(sheet, r, 1);

            if (label.StartsWith("7.", StringComparison.OrdinalIgnoreCase)
                || label.Contains("7.  Provisions", StringComparison.OrdinalIgnoreCase))
            {
                section = "provisions";
            }
            else if (label.StartsWith("8.", StringComparison.OrdinalIgnoreCase)
                     || label.Contains("Short term borrowings", StringComparison.OrdinalIgnoreCase))
            {
                section = "shortTermBorrowings";
            }
            else if (label.StartsWith("9.", StringComparison.OrdinalIgnoreCase)
                     || label.Contains("Trade payables", StringComparison.OrdinalIgnoreCase))
            {
                section = "tradePayables";
            }
            else if (label.StartsWith("10.", StringComparison.OrdinalIgnoreCase)
                     || label.Contains("Other current liabilities", StringComparison.OrdinalIgnoreCase))
            {
                section = "otherCurrentLiabilities";
            }

            if (label.StartsWith("Provision for taxation (net of advance tax and TDS)", StringComparison.OrdinalIgnoreCase))
            {
                var tax = TryReadDecimal(sheet, r, 6);
                if (tax != null)
                    rules.CurrentTaxLakhs = Round2(Math.Abs(tax.Value));
            }
            else if (section == "provisions" && string.IsNullOrWhiteSpace(label))
            {
                var col4 = TryReadDecimal(sheet, r, 4);
                var col6 = TryReadDecimal(sheet, r, 6);

                if (col4 != null && col6 != null
                    && col4 > 50m && col6 > 50m && col6 < 600m)
                {
                    rules.LongTermProvisionsLakhs = Round2(Math.Abs(col4.Value));
                    rules.ShortTermProvisionsLakhs = Round2(Math.Abs(col6.Value));
                }
            }
            else if (section == "otherCurrentLiabilities" && string.IsNullOrWhiteSpace(label))
            {
                var col6 = TryReadDecimal(sheet, r, 6);
                if (col6 != null && col6 > 100m && col6 < 5000m)
                    rules.OtherCurrentLiabilitiesLakhs = Round2(Math.Abs(col6.Value));
            }
        }
    }

    private static void ExtractBorrowingsAndDeferredTaxSheet(IXLWorksheet? sheet, PresentationRules rules)
    {
        if (sheet == null)
            return;

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 1; r <= lastRow; r++)
        {
            var label = ReadString(sheet, r, 1);
            if (label.Equals("Net amount", StringComparison.OrdinalIgnoreCase))
            {
                var ltBorrowings = TryReadDecimal(sheet, r, 6);
                if (ltBorrowings != null)
                    rules.LongTermBorrowingsLakhs = Round2(Math.Abs(ltBorrowings.Value));
            }

            var detail = ReadString(sheet, r, 2);
            if (detail.Contains("Deferred tax liability (net)", StringComparison.OrdinalIgnoreCase))
            {
                var current = TryReadDecimal(sheet, r, 8);
                var prior = TryReadDecimal(sheet, r, 10);
                if (current != null)
                    rules.DeferredTaxLiabilityLakhs = Round2(Math.Abs(current.Value));
                if (current != null && prior != null)
                    rules.DeferredTaxLakhs = Round2(current.Value - prior.Value);
            }
        }
    }

    private static void ExtractOtbAdjustmentsSheet(IXLWorksheet? sheet, PresentationRules rules)
    {
        if (sheet == null)
            return;

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 1; r <= lastRow; r++)
        {
            var label = ReadString(sheet, r, 1);
            var type = ReadString(sheet, r, 5);
            var lakhs = TryReadDecimal(sheet, r, 7) ?? TryReadDecimal(sheet, r, 8);

            if (type.Equals("Inventory", StringComparison.OrdinalIgnoreCase) && lakhs != null)
                rules.InventoriesLakhs = Round2(Math.Abs(lakhs.Value));
            else if (type.Equals("Debtors", StringComparison.OrdinalIgnoreCase) && lakhs != null)
                rules.TradeReceivablesLakhs = Round2(Math.Abs(lakhs.Value));

            if (label.Equals("Insurance claim of earlier year", StringComparison.OrdinalIgnoreCase))
            {
                var rupees = TryReadDecimal(sheet, r, 2);
                if (rupees != null)
                    rules.InsuranceClaimEarlierYearLakhs = Round2(Math.Abs(rupees.Value) / 100_000m);
            }
        }
    }

    private static bool IsDashTotalRow(string label)
    {
        var trimmed = label.Trim();
        return trimmed is "-" or "–" or "—"
               || (trimmed.Length > 0 && trimmed.All(static c => c is '-' or '–' or '—'));
    }

    private static bool IsSectionTotalRow(string label, string subLabel)
        => string.IsNullOrWhiteSpace(label) && string.IsNullOrWhiteSpace(subLabel);

    private static IXLWorksheet? FindSheet(XLWorkbook workbook, string name)
        => workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static string ReadString(IXLWorksheet sheet, int row, int col)
        => sheet.Cell(row, col).GetFormattedString().Trim();

    private static decimal? TryReadDecimal(IXLWorksheet sheet, int row, int col)
    {
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty())
            return null;

        if (cell.TryGetValue(out double d))
            return Convert.ToDecimal(d);

        var text = cell.GetFormattedString().Trim()
            .Replace(",", "")
            .Replace("₹", "");

        if (string.IsNullOrWhiteSpace(text))
            return null;

        return decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static decimal Round2(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public static class PresentationRulesMerger
{
    /// <summary>
    /// Workbook-extracted rules take precedence over static file rules.
    /// Line-level hardcoded overrides from file are dropped when workbook has check data.
    /// </summary>
    public static PresentationRules Merge(PresentationRules? fileRules, PresentationRules? workbookRules)
    {
        if (workbookRules == null)
            return fileRules ?? new PresentationRules();

        if (fileRules == null)
            return workbookRules;

        var merged = Clone(fileRules);

        MergeDictionary(merged.GroupPresentationLakhs, workbookRules.GroupPresentationLakhs);
        MergeDictionary(merged.GroupAdjustmentsLakhs, workbookRules.GroupAdjustmentsLakhs);
        MergeDictionary(merged.LoanMaturityLakhs, workbookRules.LoanMaturityLakhs);

        if (workbookRules.TradePayables != null)
            merged.TradePayables = workbookRules.TradePayables;

        if (workbookRules.CogsMovement != null)
            merged.CogsMovement = workbookRules.CogsMovement;
        else if (merged.CogsMovement == null || merged.CogsMovement.PurchaseGroups.Count == 0)
        {
            merged.CogsMovement = new CogsMovementRules
            {
                PurchaseGroups =
                [
                    "Add : Purchase of Raw Material",
                    "Add : Purchase of Packing Material",
                    "Add : Purchase Expenses"
                ]
            };
        }

        if (workbookRules.GroupPresentationLakhs.Count > 0)
            ClearLineOverrides(merged);

        ApplyWorkbookLineOverrides(merged, workbookRules);

        return merged;
    }

    private static void ApplyWorkbookLineOverrides(PresentationRules merged, PresentationRules workbook)
    {
        CopyIfSet(workbook.TradeReceivablesLakhs, v => merged.TradeReceivablesLakhs = v);
        CopyIfSet(workbook.InventoriesLakhs, v => merged.InventoriesLakhs = v);
        CopyIfSet(workbook.CurrentMaturitiesLakhs, v => merged.CurrentMaturitiesLakhs = v);
        CopyIfSet(workbook.LongTermBorrowingsLakhs, v => merged.LongTermBorrowingsLakhs = v);
        CopyIfSet(workbook.OtherCurrentLiabilitiesLakhs, v => merged.OtherCurrentLiabilitiesLakhs = v);
        CopyIfSet(workbook.DepreciationLakhs, v => merged.DepreciationLakhs = v);
        CopyIfSet(workbook.ChangesInInventoryLakhs, v => merged.ChangesInInventoryLakhs = v);
        CopyIfSet(workbook.OtherOperatingRevenueLakhs, v => merged.OtherOperatingRevenueLakhs = v);
        CopyIfSet(workbook.OtherIncomeLakhs, v => merged.OtherIncomeLakhs = v);
        CopyIfSet(workbook.FinanceCostsLakhs, v => merged.FinanceCostsLakhs = v);
        CopyIfSet(workbook.LongTermProvisionsLakhs, v => merged.LongTermProvisionsLakhs = v);
        CopyIfSet(workbook.ShortTermProvisionsLakhs, v => merged.ShortTermProvisionsLakhs = v);
        CopyIfSet(workbook.OtherNonCurrentAssetsLakhs, v => merged.OtherNonCurrentAssetsLakhs = v);
        CopyIfSet(workbook.OtherCurrentAssetsLakhs, v => merged.OtherCurrentAssetsLakhs = v);
        CopyIfSet(workbook.PropertyPlantEquipmentLakhs, v => merged.PropertyPlantEquipmentLakhs = v);
        CopyIfSet(workbook.ReservesAndSurplusLakhs, v => merged.ReservesAndSurplusLakhs = v);
        CopyIfSet(workbook.LoansAndAdvancesCurrentLakhs, v => merged.LoansAndAdvancesCurrentLakhs = v);
        CopyIfSet(workbook.LoansAndAdvancesNonCurrentLakhs, v => merged.LoansAndAdvancesNonCurrentLakhs = v);
        CopyIfSet(workbook.OtherExpensesLakhs, v => merged.OtherExpensesLakhs = v);
        CopyIfSet(workbook.CogsLakhs, v => merged.CogsLakhs = v);
        CopyIfSet(workbook.RevenueFromOperationsLakhs, v => merged.RevenueFromOperationsLakhs = v);
        CopyIfSet(workbook.EmployeeBenefitsLakhs, v => merged.EmployeeBenefitsLakhs = v);
        CopyIfSet(workbook.DeferredTaxLiabilityLakhs, v => merged.DeferredTaxLiabilityLakhs = v);
        CopyIfSet(workbook.CashAndBankLakhs, v => merged.CashAndBankLakhs = v);
        CopyIfSet(workbook.InsuranceClaimEarlierYearLakhs, v => merged.InsuranceClaimEarlierYearLakhs = v);
        CopyIfSet(workbook.CurrentTaxLakhs, v => merged.CurrentTaxLakhs = v);
        CopyIfSet(workbook.DeferredTaxLakhs, v => merged.DeferredTaxLakhs = v);
    }

    private static void CopyIfSet(decimal? value, Action<decimal> assign)
    {
        if (value.HasValue)
            assign(value.Value);
    }

    private static void ClearLineOverrides(PresentationRules rules)
    {
        rules.TradeReceivablesLakhs = null;
        rules.InventoriesLakhs = null;
        rules.LongTermBorrowingsLakhs = null;
        rules.OtherCurrentLiabilitiesLakhs = null;
        rules.DepreciationLakhs = null;
        rules.ChangesInInventoryLakhs = null;
        rules.OtherOperatingRevenueLakhs = null;
        rules.OtherIncomeLakhs = null;
        rules.FinanceCostsLakhs = null;
        rules.LongTermProvisionsLakhs = null;
        rules.ShortTermProvisionsLakhs = null;
        rules.OtherNonCurrentAssetsLakhs = null;
        rules.OtherCurrentAssetsLakhs = null;
        rules.PropertyPlantEquipmentLakhs = null;
        rules.ReservesAndSurplusLakhs = null;
        rules.LoansAndAdvancesCurrentLakhs = null;
        rules.LoansAndAdvancesNonCurrentLakhs = null;
        rules.OtherExpensesLakhs = null;
        rules.CogsLakhs = null;
        rules.RevenueFromOperationsLakhs = null;
        rules.EmployeeBenefitsLakhs = null;
        rules.DeferredTaxLiabilityLakhs = null;
        rules.CashAndBankLakhs = null;
        rules.InsuranceClaimEarlierYearLakhs = null;
        rules.CurrentTaxLakhs = null;
        rules.DeferredTaxLakhs = null;
        rules.CurrentMaturitiesLakhs = null;
    }

    private static PresentationRules Clone(PresentationRules source)
    {
        return new PresentationRules
        {
            CompanyKey = source.CompanyKey,
            CompanyName = source.CompanyName,
            GroupPresentationLakhs = new Dictionary<string, decimal>(source.GroupPresentationLakhs, StringComparer.OrdinalIgnoreCase),
            GroupAdjustmentsLakhs = new Dictionary<string, decimal>(source.GroupAdjustmentsLakhs, StringComparer.OrdinalIgnoreCase),
            CogsMovement = source.CogsMovement == null ? null : new CogsMovementRules
            {
                PurchaseGroups = source.CogsMovement.PurchaseGroups.ToList(),
                OpeningRmPackingLakhs = source.CogsMovement.OpeningRmPackingLakhs,
                ClosingRmPackingLakhs = source.CogsMovement.ClosingRmPackingLakhs
            },
            LoanMaturityLakhs = source.LoanMaturityLakhs.ToDictionary(
                kvp => kvp.Key,
                kvp => new LoanMaturitySplit
                {
                    CurrentLakhs = kvp.Value.CurrentLakhs,
                    NonCurrentLakhs = kvp.Value.NonCurrentLakhs
                },
                StringComparer.OrdinalIgnoreCase),
            TradePayables = source.TradePayables == null ? null : new TradePayablesRules
            {
                MsmeLakhs = source.TradePayables.MsmeLakhs,
                CreditorsOthersLakhs = source.TradePayables.CreditorsOthersLakhs,
                BillOfExchangeLakhs = source.TradePayables.BillOfExchangeLakhs
            },
            TradeReceivablesLakhs = source.TradeReceivablesLakhs,
            InventoriesLakhs = source.InventoriesLakhs,
            CurrentMaturitiesLakhs = source.CurrentMaturitiesLakhs,
            LongTermBorrowingsLakhs = source.LongTermBorrowingsLakhs,
            OtherCurrentLiabilitiesLakhs = source.OtherCurrentLiabilitiesLakhs,
            DepreciationLakhs = source.DepreciationLakhs,
            ChangesInInventoryLakhs = source.ChangesInInventoryLakhs,
            OtherOperatingRevenueLakhs = source.OtherOperatingRevenueLakhs,
            OtherIncomeLakhs = source.OtherIncomeLakhs,
            FinanceCostsLakhs = source.FinanceCostsLakhs,
            LongTermProvisionsLakhs = source.LongTermProvisionsLakhs,
            ShortTermProvisionsLakhs = source.ShortTermProvisionsLakhs,
            OtherNonCurrentAssetsLakhs = source.OtherNonCurrentAssetsLakhs,
            OtherCurrentAssetsLakhs = source.OtherCurrentAssetsLakhs,
            PropertyPlantEquipmentLakhs = source.PropertyPlantEquipmentLakhs,
            ReservesAndSurplusLakhs = source.ReservesAndSurplusLakhs,
            LoansAndAdvancesCurrentLakhs = source.LoansAndAdvancesCurrentLakhs,
            LoansAndAdvancesNonCurrentLakhs = source.LoansAndAdvancesNonCurrentLakhs,
            OtherExpensesLakhs = source.OtherExpensesLakhs,
            CogsLakhs = source.CogsLakhs,
            RevenueFromOperationsLakhs = source.RevenueFromOperationsLakhs,
            EmployeeBenefitsLakhs = source.EmployeeBenefitsLakhs,
            DeferredTaxLiabilityLakhs = source.DeferredTaxLiabilityLakhs,
            CashAndBankLakhs = source.CashAndBankLakhs,
            InsuranceClaimEarlierYearLakhs = source.InsuranceClaimEarlierYearLakhs,
            CurrentTaxLakhs = source.CurrentTaxLakhs,
            DeferredTaxLakhs = source.DeferredTaxLakhs
        };
    }

    private static void MergeDictionary<T>(Dictionary<string, T> target, Dictionary<string, T> source)
    {
        foreach (var (key, value) in source)
            target[key] = value;
    }
}
