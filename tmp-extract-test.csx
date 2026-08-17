using ClosedXML.Excel;
using POApprovalAPI.Services.FinancialStatements;

var path = @"c:\Users\Admin\Desktop\approval\PO_approvalapp\docs\accounting\PIL Provisional FS_March-26_14.08.26 - Copy.xlsx";
var bytes = File.ReadAllBytes(path);
using var wb = new XLWorkbook(new MemoryStream(bytes));
var extractor = new WorkbookPresentationRulesExtractor();
var rules = extractor.Extract(wb, "plastene-india-limited", bytes);
Console.WriteLine($"ChangesInInventory: {rules.ChangesInInventoryLakhs}");
Console.WriteLine($"OtherOperatingRevenue: {rules.OtherOperatingRevenueLakhs}");
Console.WriteLine($"FinanceCosts: {rules.FinanceCostsLakhs}");
Console.WriteLine($"OtherExpenses: {rules.OtherExpensesLakhs}");
Console.WriteLine($"CurrentTax: {rules.CurrentTaxLakhs}");
Console.WriteLine($"DeferredTax: {rules.DeferredTaxLakhs}");
Console.WriteLine($"LongTermBorrowings: {rules.LongTermBorrowingsLakhs}");
Console.WriteLine($"OtherCurrentLiabilities: {rules.OtherCurrentLiabilitiesLakhs}");
Console.WriteLine($"Inventories: {rules.InventoriesLakhs}");
Console.WriteLine($"InsuranceClaim: {rules.InsuranceClaimEarlierYearLakhs}");
Console.WriteLine($"GroupPresentation count: {rules.GroupPresentationLakhs.Count}");
