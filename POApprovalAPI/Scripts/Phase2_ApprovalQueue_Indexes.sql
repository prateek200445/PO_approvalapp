-- =====================================================
-- Phase 2: Approval Queue Performance Optimization
-- Focus: PO, Work Order, and Advance Payment approval queues
-- Expected Impact: Faster pending-list loads and bulk approval lookups
-- =====================================================

USE [MaterialProcessing];
GO

PRINT '========================================';
PRINT 'Starting Phase 2: Approval Queue Index Creation';
PRINT '========================================';

-- =====================================================
-- PO approval queue
-- Optimizes pending PO load and bulk approve lookup paths
-- =====================================================

PRINT 'Creating PO approval indexes...';

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ApprovePO_PendingQueue'
      AND object_id = OBJECT_ID('dbo.ApprovePO')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ApprovePO_PendingQueue
    ON dbo.ApprovePO (Status, ApprovalName)
    INCLUDE (PoNo, PODate, ApprovalDate, TransId);
    PRINT 'Created IX_ApprovePO_PendingQueue';
END
ELSE
BEGIN
    PRINT 'IX_ApprovePO_PendingQueue already exists';
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ApprovePOHOD_PendingQueue'
      AND object_id = OBJECT_ID('dbo.ApprovePOHOD')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ApprovePOHOD_PendingQueue
    ON dbo.ApprovePOHOD (Status, ApprovalName)
    INCLUDE (PoNo, PODate, ApprovalDate, TransId);
    PRINT 'Created IX_ApprovePOHOD_PendingQueue';
END
ELSE
BEGIN
    PRINT 'IX_ApprovePOHOD_PendingQueue already exists';
END
GO

-- =====================================================
-- Work order approval queue
-- Optimizes pending WO load and approval updates by TransId / ApprovalName
-- =====================================================

PRINT 'Creating work order approval index...';

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ApproveWorkOrder_PendingQueue'
      AND object_id = OBJECT_ID('dbo.ApproveWorkOrder')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ApproveWorkOrder_PendingQueue
    ON dbo.ApproveWorkOrder (Status, ApprovalName)
    INCLUDE (PoNo, PODate, ApprovalDate, TransId);
    PRINT 'Created IX_ApproveWorkOrder_PendingQueue';
END
ELSE
BEGIN
    PRINT 'IX_ApproveWorkOrder_PendingQueue already exists';
END
GO

-- =====================================================
-- Approval authority tables
-- Speeds up user-to-company authority lookups
-- =====================================================

PRINT 'Creating authority lookup indexes...';

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_POAllocation_UserCompany'
      AND object_id = OBJECT_ID('dbo.poallocation')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_POAllocation_UserCompany
    ON dbo.poallocation (username, CompanyName)
    INCLUDE (Deptt);
    PRINT 'Created IX_POAllocation_UserCompany';
END
ELSE
BEGIN
    PRINT 'IX_POAllocation_UserCompany already exists';
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ApprovePaymentPlanAllocation_UserCompany'
      AND object_id = OBJECT_ID('dbo.ApprovePaymentPlanAllocation')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ApprovePaymentPlanAllocation_UserCompany
    ON dbo.ApprovePaymentPlanAllocation (username, CompanyName)
    PRINT 'Created IX_ApprovePaymentPlanAllocation_UserCompany';
END
ELSE
BEGIN
    PRINT 'IX_ApprovePaymentPlanAllocation_UserCompany already exists';
END
GO

-- =====================================================
-- Advance payment queue
-- The advance-payment inbox is filtered from Payment using ApprovalStatus + authority allocation.
-- A filtered index keeps the pending subset small and faster to scan.
-- =====================================================

PRINT 'Creating advance payment pending index...';

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Payment_AdvancePending'
      AND object_id = OBJECT_ID('dbo.Payment')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Payment_AdvancePending
    ON dbo.Payment (CompanyName, PaymentNo)
    INCLUDE (
        PaymentDate,
        PaymentType,
        PaymentTYpeNo,
        PaymentAmt,
        Remarks,
        PaymentRef,
        BankCashPayment,
        Currency,
        exchangerate,
        paymentreqno,
        LedgerFrom,
        LedgerTo,
        vendorcode
    )
    WHERE ApprovalStatus = 0;
    PRINT 'Created IX_Payment_AdvancePending';
END
ELSE
BEGIN
    PRINT 'IX_Payment_AdvancePending already exists';
END
GO

-- =====================================================
-- Statistics refresh
-- =====================================================

PRINT 'Updating statistics...';
UPDATE STATISTICS dbo.ApprovePO;
UPDATE STATISTICS dbo.ApprovePOHOD;
UPDATE STATISTICS dbo.ApproveWorkOrder;
UPDATE STATISTICS dbo.poallocation;
UPDATE STATISTICS dbo.ApprovePaymentPlanAllocation;
UPDATE STATISTICS dbo.Payment;

PRINT '========================================';
PRINT 'Phase 2 approval queue indexes complete';
PRINT '========================================';
GO
