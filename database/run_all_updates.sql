-- Master Database Update Script
-- This script runs all necessary updates in the correct order
-- Run this to add all new pricing, assembly, and integration features

-- Make sure we're using the correct database
USE electrical_contractor_db;

-- 1. First, check if estimating tables exist (from add_estimating_tables.sql)
-- If not, source that file first
-- source add_estimating_tables.sql;

-- 2. Add all pricing and assembly tables
source add_pricing_tables.sql;

-- 3. Add integration columns to existing tables
source add_integration_columns.sql;

-- 4. Verify all tables were created successfully
SELECT 'Checking new tables...' AS Status;

SELECT TABLE_NAME, 'Created' AS Status
FROM information_schema.TABLES 
WHERE TABLE_SCHEMA = 'electrical_contractor_db' 
AND TABLE_NAME IN (
    'Materials',
    'MaterialPriceHistory',
    'AssemblyTemplates',
    'AssemblyComponents',
    'AssemblyVariants',
    'ServiceTypes',
    'DifficultyPresets',
    'LaborAdjustments',
    'EstimateConversions',
    'AssemblyUsageStats'
);

-- 5. Verify views were created
SELECT 'Checking views...' AS Status;

SELECT TABLE_NAME AS View_Name, 'Created' AS Status
FROM information_schema.VIEWS
WHERE TABLE_SCHEMA = 'electrical_contractor_db'
AND TABLE_NAME IN (
    'vw_material_price_changes',
    'vw_assembly_costs',
    'vw_assembly_with_variants',
    'vw_estimate_job_tracking'
);

-- 6. Show counts of default data
SELECT 'Default data loaded:' AS Status;
SELECT COUNT(*) AS ServiceTypes_Count FROM ServiceTypes;
SELECT COUNT(*) AS DifficultyPresets_Count FROM DifficultyPresets;

SELECT 'Database update complete!' AS Status;
