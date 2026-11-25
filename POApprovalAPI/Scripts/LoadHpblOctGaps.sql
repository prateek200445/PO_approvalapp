/* HPBL gaps.
   HPBL-4 October provisions from Check Report OCT-2026, sheet Provision and Prepaid Exp.
   Labour is the contractor total. The 0.17 electric cell is omitted.
   HPBL-3 July, August, and October stock copied from September.
   HPBL-1 and HPBL-2 October provisions from Book321 unit columns. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-07-01', p.Category, p.AmountLacs, N'same as September stock', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND p.StockMonth = '2026-09-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-07-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-08-01', p.Category, p.AmountLacs, N'same as September stock', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND p.StockMonth = '2026-09-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-08-01' AND c.Category = p.Category AND c.PlantName IS NULL);

INSERT INTO dbo.PnlStockValue (CompanyName, PlantName, StockMonth, Category, AmountLacs, Remarks, UploadedAt)
SELECT N'HCP Plastene Bulkpack Ltd (Unit - III)', NULL, '2026-10-01', p.Category, p.AmountLacs, N'same as September stock', GETDATE()
FROM dbo.PnlStockValue p
WHERE p.CompanyName = N'HCP Plastene Bulkpack Ltd (Unit - III)' AND p.StockMonth = '2026-09-01' AND p.PlantName IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.PnlStockValue c WHERE c.CompanyName = p.CompanyName AND c.StockMonth = '2026-10-01' AND c.Category = p.Category AND c.PlantName IS NULL);

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - IV)' AND sysdate >= '2026-10-01' AND sysdate < DATEADD(month, 1, '2026-10-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'Labour Charges', 7723508.9500, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'Advocate Fees Expense', 7500.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'C & F  Charges - Export', 341721.0300, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'Freight Outward Expense (With GST)', 2411500.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'Fumigation Charges', 26380.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'House Keeping Expense', 47200.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'Incentive Expense - Production', 11000.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'Mobile & Telephone Expense', 10497.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'P.F. Admin. Charge', 3500.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'P.F. Contribution - Employer', 40943.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'Repairs & Maintenance Exp - Computer & Printers', 4250.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'Testing Charges', 12546.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'Water Charges', 112700.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'Repairs & Maintenance Exp - Plant & Machinery', 13655.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'Freight Inward Expense (RCM)', 9200.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - IV)', '2026-10-01', N'Rent Expense - Factory', 180000.0000, N'Check Report OCT-2026 Provision and Prepaid Exp', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd' AND sysdate >= '2026-10-01' AND sysdate < DATEADD(month, 1, '2026-10-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Internal Transportation Expense / Reimbursement', 26039.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Freight Inward Expense (RCM)', 116500.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Loading & Unloading Expense', 3084.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Power Expense', 2969234.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Postage & Courier Expense', 16870.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Printing & Stationery Expense', 12738.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Security Expenses', 20167.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Repairs & Maintenance Exp - Plant & Machinery', 107129.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'P.F. Contribution - Employer', 200500.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Employers Contribution to ESI', 17500.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Salaries Expense', 3900000.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Staff Welfare Expense', 17135.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Canteen Expense - Factory', 321000.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Conveyance Expenses', 589.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Medical Expense', 25000.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Professional & Consultancy Fees', 353750.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Freight Outward Expense (RCM)', 637750.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Freight Outward Expense (With GST)', 4449747.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Job Charges', 26969587.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd', '2026-10-01', N'Labour Charges', 3667210.0000, N'Book321 October unit split', N'Expense');

DELETE FROM dbo.Provisioning WHERE LTRIM(RTRIM(companyname)) = N'HCP Plastene Bulkpack Ltd (Unit - II)' AND sysdate >= '2026-10-01' AND sysdate < DATEADD(month, 1, '2026-10-01');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-10-01', N'Postage & Courier Expense', 4320.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-10-01', N'Repairs & Maintenance Exp - Plant & Machinery', 7337.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-10-01', N'P.F. Contribution - Employer', 5500.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-10-01', N'Employers Contribution to ESI', 1000.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-10-01', N'Salaries Expense', 400000.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-10-01', N'Conveyance Expenses', 2000.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-10-01', N'Professional & Consultancy Fees', 50000.0000, N'Book321 October unit split', N'Expense');
INSERT INTO dbo.Provisioning (companyname, sysdate, Ledgername, Amount, remarks, EntryType) VALUES (N'HCP Plastene Bulkpack Ltd (Unit - II)', '2026-10-01', N'Labour Charges', 41860.0000, N'Book321 October unit split', N'Expense');

COMMIT;
