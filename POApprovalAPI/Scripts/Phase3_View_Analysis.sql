-- Phase 3: Analyze vw_LedgerSummary view to bypass it entirely
-- This will help us identify the underlying tables and create direct queries

USE [MaterialProcessing];
GO

-- Get the view definition to see what tables it uses
SELECT 
    VIEW_DEFINITION
FROM INFORMATION_SCHEMA.VIEWS 
WHERE TABLE_NAME = 'vw_LedgerSummary';

-- Alternative method to get view definition
SELECT 
    definition
FROM sys.sql_modules sm
INNER JOIN sys.objects so ON sm.object_id = so.object_id
WHERE so.name = 'vw_LedgerSummary' AND so.type = 'V';

-- Get all tables referenced by the view
SELECT DISTINCT
    TABLE_NAME,
    COLUMN_NAME
FROM INFORMATION_SCHEMA.VIEW_COLUMN_USAGE 
WHERE VIEW_NAME = 'vw_LedgerSummary'
ORDER BY TABLE_NAME;